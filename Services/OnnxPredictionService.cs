using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using FitApp.Data;
using FitApp.Models;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace FitApp.Services;

/// <summary>
/// Локальный (offline) ML-предиктор 1ПМ через 28 дней. Загружает обученную
/// в Python модель LightGBM, экспортированную в ONNX, и выполняет inference
/// прямо на устройстве через Microsoft.ML.OnnxRuntime. Не требует ни сети,
/// ни Python-сервиса.
///
/// Pipeline построения фичей повторяет логику ml/notebooks/02_lightgbm.py —
/// порядок и формулы фичей должны совпадать, иначе модель даст мусор.
/// </summary>
public class OnnxPredictionService : IDisposable
{
    private const string ModelFileName = "lightgbm_1rm_28d.onnx";
    private const string MetaFileName = "lightgbm_1rm_28d.meta.json";

    private readonly WorkoutDataBase _db;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    private InferenceSession? _session;
    private string _inputName = "input";
    private string _outputName = "variable";
    private OnnxMeta? _meta;
    private Dictionary<string, int>? _muscleToCode;

    public OnnxPredictionService(WorkoutDataBase db)
    {
        _db = db;
    }

    /// <summary>
    /// Возвращает прогноз 1ПМ через <see cref="OnnxMeta.HorizonDays"/> дней
    /// от текущего момента, либо null если данных недостаточно (нет ни одной
    /// завершённой тренировки этого упражнения у юзера) либо если модель
    /// не удалось загрузить.
    /// </summary>
    public async Task<OnnxPredictionResult?> PredictAsync(int exerciseId)
    {
        try
        {
            await EnsureLoadedAsync();
            if (_session == null || _meta == null) return null;

            var user = await _db.GetCurrentUserAsync();
            var history = await _db.GetExerciseWorkoutHistoryAsync(
                user.Id, exerciseId, limit: 30);
            if (history.Count == 0) return null;

            var exercise = await _db.GetExerciseByIdAsync(exerciseId);
            if (exercise == null) return null;
            var primaryMuscleName = await _db.GetPrimaryMuscleNameAsync(exerciseId);

            var features = BuildFeatures(user, exercise, primaryMuscleName, history);
            if (features == null) return null;

            var inputTensor = new DenseTensor<float>(features, new[] { 1, features.Length });
            var inputs = NamedOnnxValue.CreateFromTensor(_inputName, inputTensor);
            using var results = _session.Run(new[] { inputs });

            var output = results.First(r => r.Name == _outputName);
            var prediction = output.AsTensor<float>().First();

            return new OnnxPredictionResult(
                Predicted1RmKg: Math.Round(prediction, 1),
                HorizonDays: _meta.HorizonDays,
                Text: $"Через {_meta.HorizonDays} дн.: ~{prediction:0.#} кг (1ПМ)"
            );
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Onnx] PredictAsync failed: {ex.Message}");
            return null;
        }
    }

    // --- Загрузка модели и метаданных из MauiAsset (Resources/Raw) ---

    private async Task EnsureLoadedAsync()
    {
        if (_session != null && _meta != null) return;
        await _initLock.WaitAsync();
        try
        {
            if (_session != null && _meta != null) return;

            // Меты
            using (var metaStream = await FileSystem.OpenAppPackageFileAsync(MetaFileName))
            using (var reader = new StreamReader(metaStream))
            {
                var json = await reader.ReadToEndAsync();
                _meta = JsonSerializer.Deserialize<OnnxMeta>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (_meta == null)
                {
                    System.Diagnostics.Debug.WriteLine("[Onnx] Failed to parse meta JSON");
                    return;
                }
                _muscleToCode = _meta.MuscleClasses
                    .Select((name, idx) => (name, idx))
                    .ToDictionary(x => x.name, x => x.idx);
            }

            // Модель: читаем в byte[] и создаём сессию из памяти (приложение
            // не имеет прямого доступа к bundle-файлам на Android иначе как
            // через OpenAppPackageFileAsync).
            byte[] modelBytes;
            using (var modelStream = await FileSystem.OpenAppPackageFileAsync(ModelFileName))
            using (var ms = new MemoryStream())
            {
                await modelStream.CopyToAsync(ms);
                modelBytes = ms.ToArray();
            }
            _session = new InferenceSession(modelBytes);
            _inputName = _session.InputMetadata.Keys.First();
            _outputName = _session.OutputMetadata.Keys.First();
            System.Diagnostics.Debug.WriteLine(
                $"[Onnx] Loaded: input='{_inputName}', output='{_outputName}', " +
                $"features={_meta.FeatureColumns.Length}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Onnx] EnsureLoadedAsync failed: {ex.Message}");
        }
        finally
        {
            _initLock.Release();
        }
    }

    // --- Построение вектора фичей ---
    //
    // Порядок ОБЯЗАН совпадать с _meta.FeatureColumns, иначе модель даст
    // невалидные результаты. См. ml/notebooks/02_lightgbm.py — там тот же
    // порядок и те же формулы.

    private float[]? BuildFeatures(
        User user,
        Exercise exercise,
        string? primaryMuscleName,
        IList<ExerciseWorkoutHistoryRow> history)
    {
        if (_meta == null || _muscleToCode == null) return null;
        if (history.Count == 0) return null;

        // "Текущая" тренировка для модели — последняя завершённая
        // (история отсортирована ASC по дате, см. SQL).
        var current = history[^1];
        var prior = history.Take(history.Count - 1).ToList();   // всё до current

        // Лаги top_1rm: lag_K = top_1rm на K тренировок назад от current
        double? Lag(int k) =>
            prior.Count >= k ? prior[prior.Count - k].TopEpley1Rm : (double?)null;

        var lag1 = Lag(1);
        var lag2 = Lag(2);
        var lag3 = Lag(3);
        var lag5 = Lag(5);
        var diff1 = (lag1.HasValue && lag2.HasValue) ? lag1 - lag2 : null;

        // Скользящие средние/наклоны — по последним W ТРЕНИРОВКАМ ДО current
        var last5_1rm = prior.Skip(Math.Max(0, prior.Count - 5))
                             .Select(r => r.TopEpley1Rm).ToArray();
        var last5_rpe = prior.Skip(Math.Max(0, prior.Count - 5))
                             .Select(r => r.AvgRpe).ToArray();
        var last3_1rm = prior.Skip(Math.Max(0, prior.Count - 3))
                             .Select(r => r.TopEpley1Rm).ToArray();

        double? meanIfEnough(double[] arr) =>
            arr.Length >= 2 ? arr.Average() :
            arr.Length == 1 ? arr[0] :
            null;
        var mean_1rm_5 = meanIfEnough(last5_1rm);
        var mean_rpe_5 = meanIfEnough(last5_rpe);

        double? slope_5 = LinregSlope(last5_1rm);
        double? slope_3 = LinregSlope(last3_1rm);

        int n_history = prior.Count;
        double days_since_first = history.Count > 1
            ? (current.Date - history[0].Date).TotalDays
            : 0;
        double? days_since_last = prior.Count >= 1
            ? (current.Date - prior[^1].Date).TotalDays
            : null;

        // Анкета атлета
        int sex_male = user.Sex == 1 ? 1 : 0;
        double bodyweight = user.Bodyweight > 0 ? user.Bodyweight : 75.0; // дефолт «средний мужчина»
        double age = user.Age > 0 ? user.Age : 28;
        double training_age_months = user.ExperienceStartDate.HasValue
            ? Math.Max(0, (DateTime.UtcNow - user.ExperienceStartDate.Value).TotalDays / 30.44)
            : 0;
        double target_rpe = user.TargetRpe > 0 ? user.TargetRpe : 7.5;

        // Описание упражнения
        int exercise_id = exercise.Id;
        int is_compound = exercise.Category == (int)ExerciseCategory.Compound ? 1 : 0;
        int equipment = exercise.EquipmentType;
        int primary_muscle_code =
            primaryMuscleName != null && _muscleToCode.TryGetValue(primaryMuscleName, out var code)
                ? code
                : -1;   // мышца, которой не было в обучении — пусть модель сама разберётся

        // NaN-замены: LightGBM нативно умеет с NaN, но через ONNX FloatTensorType
        // приходит float32. NaN в float32 работает (он распознаётся как
        // отсутствующее значение в LightGBM-ONNX-конвертере).
        float Nan(double? v) => v.HasValue ? (float)v.Value : float.NaN;

        // Финальный вектор — порядок ровно как в meta.FeatureColumns
        var features = new float[]
        {
            Nan(lag1), Nan(lag2), Nan(lag3), Nan(lag5),
            Nan(diff1),
            Nan(mean_1rm_5), Nan(mean_rpe_5),
            Nan(slope_3), Nan(slope_5),
            (float)n_history,
            (float)days_since_first,
            Nan(days_since_last),
            (float)current.TopEpley1Rm,
            (float)current.TopWeight,
            (float)current.NSets,
            (float)current.AvgRpe,
            (float)current.MinRpe,
            (float)current.MaxRpe,
            (float)current.AvgReps,
            sex_male,
            (float)bodyweight,
            (float)age,
            (float)training_age_months,
            (float)target_rpe,
            exercise_id,
            is_compound,
            equipment,
            primary_muscle_code,
        };

        if (features.Length != _meta.FeatureColumns.Length)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[Onnx] Feature count mismatch: built {features.Length}, " +
                $"meta expects {_meta.FeatureColumns.Length}");
            return null;
        }
        return features;
    }

    /// <summary>Наклон прямой y = m*x + b методом наименьших квадратов
    /// по индексам 0..n-1. NaN, если меньше 2 точек.</summary>
    private static double? LinregSlope(double[] y)
    {
        if (y.Length < 2) return null;
        double x_mean = (y.Length - 1) / 2.0;
        double y_mean = y.Average();
        double num = 0, den = 0;
        for (int i = 0; i < y.Length; i++)
        {
            double dx = i - x_mean;
            num += dx * (y[i] - y_mean);
            den += dx * dx;
        }
        return den == 0 ? (double?)null : num / den;
    }

    public void Dispose()
    {
        _session?.Dispose();
        _initLock.Dispose();
    }
}

/// <summary>Результат предсказания, готовый для отображения в UI.</summary>
public record OnnxPredictionResult(double Predicted1RmKg, int HorizonDays, string Text);

/// <summary>Десериализация meta.json модели — список фичей, порядок
/// категориальных колонок, список мышечных групп с порядком, в котором
/// они были закодированы в обучении.</summary>
public sealed class OnnxMeta
{
    // JSON-имена в snake_case (как пишет Python). PropertyNameCaseInsensitive
    // не справляется со snake→Pascal, поэтому явные JsonPropertyName.
    [JsonPropertyName("horizon_days")] public int HorizonDays { get; set; }
    [JsonPropertyName("feature_columns")] public string[] FeatureColumns { get; set; } = Array.Empty<string>();
    [JsonPropertyName("categorical_columns")] public string[] CategoricalColumns { get; set; } = Array.Empty<string>();
    [JsonPropertyName("target_column")] public string TargetColumn { get; set; } = string.Empty;
    [JsonPropertyName("muscle_classes")] public string[] MuscleClasses { get; set; } = Array.Empty<string>();
}
