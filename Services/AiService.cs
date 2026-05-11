using System.Net.Http.Json;
using FitApp.Models;

namespace FitApp.Services;

public class AiService
{
    private static readonly HttpClient _http = new()
    {
        BaseAddress = new Uri("http://localhost:8000"),
        Timeout = TimeSpan.FromSeconds(2)
    };

    public async Task<AiPredictionResult?> PredictAsync(int exerciseId, List<ExerciseSet> sets)
    {
        if (sets == null || sets.Count == 0)
            return null;

        // sets отсортированы от новых к старым (DESC из БД), разворачиваем
        var ordered = sets.AsEnumerable().Reverse().ToList();
        var history = ordered.Select((s, i) => new
        {
            weight = s.Weight,
            reps = s.Reps,
            rpe = s.RPE > 0 ? s.RPE : 7.0,
            date = DateTime.UtcNow.AddDays(i - ordered.Count).ToString("o")
        }).ToList();

        var payload = new { exercise_id = exerciseId, history };

        try
        {
            var response = await _http.PostAsJsonAsync("/predict", payload);
            var body = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"[AI HTTP] status={response.StatusCode} body={body}");
            if (!response.IsSuccessStatusCode) return null;
            return await System.Text.Json.JsonSerializer.DeserializeAsync<AiPredictionResult>(
                new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(body)));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AI HTTP] exception={ex.Message}");
            return null;
        }
    }
}

public record AiPredictionResult(
    double predicted_weight,
    int predicted_reps,
    string trend,
    string text
);
