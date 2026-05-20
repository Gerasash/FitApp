using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FitApp.Data;
using FitApp.Models;
using SQLite;

namespace FitApp.Services.Sync;

/// <summary>
/// Клиентская синхронизация с FitApp.Api. Алгоритм одного цикла:
///
/// 1. Собрать в локальной БД все Workouts/WorkoutExercises/ExerciseSets,
///    у которых UpdatedAt > LastSyncUtc — это «push-батч».
/// 2. POST /sync с (LastSyncUtc, push-батч). Сервер применяет push
///    (last-write-wins), потом отдаёт всё, что у него новее LastSyncUtc.
/// 3. Применить серверный ответ к локальной БД: для каждой сущности — найти
///    по SyncId; если есть и серверный UpdatedAt новее — обновить;
///    если нет — вставить.
/// 4. Сохранить новый LastSyncUtc = serverTime из ответа.
/// </summary>
public class SyncService
{
    private const string LastSyncKey = "fitapp.sync.lastUtc";

    private readonly HttpClient _http;
    private readonly AuthClient _auth;
    private readonly WorkoutDataBase _db;

    public SyncService(HttpClient http, AuthClient auth, WorkoutDataBase db)
    {
        _http = http;
        _auth = auth;
        _db = db;
    }

    public DateTime? LastSyncUtc =>
        DateTime.TryParse(Preferences.Default.Get(LastSyncKey, ""), out var t) ? t : null;

    /// <summary>
    /// Сбрасывает курсор LastSyncUtc — следующая синхронизация заберёт всю
    /// локальную БД целиком и так же забёрет всё с сервера. Вызывается
    /// при Logout (новый аккаунт = новый курсор) и для ручного «полного ресинка».
    /// </summary>
    public void ResetLastSyncUtc()
    {
        Preferences.Default.Remove(LastSyncKey);
    }

    /// <summary>
    /// Одна итерация синхронизации. Возвращает краткую статистику для UI.
    /// Бросает исключение, если юзер не залогинен или сервер недоступен.
    /// </summary>
    public async Task<SyncStats> RunOnceAsync()
    {
        var token = await _auth.GetTokenAsync()
                    ?? throw new InvalidOperationException("Не залогинен — токен отсутствует.");

        var since = LastSyncUtc;
        var push = await BuildPushBatchAsync(since);

        using var req = new HttpRequestMessage(HttpMethod.Post, "sync")
        {
            Content = JsonContent.Create(push)
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var resp = await _http.SendAsync(req);
        if (resp.StatusCode == HttpStatusCode.Unauthorized)
        {
            await _auth.LogoutAsync();
            throw new InvalidOperationException("Токен отклонён сервером. Войдите заново.");
        }
        resp.EnsureSuccessStatusCode();
        var pulled = await resp.Content.ReadFromJsonAsync<SyncResponse>()
                     ?? throw new InvalidOperationException("Пустой ответ /sync");

        var applied = await ApplyServerResponseAsync(pulled);

        Preferences.Default.Set(LastSyncKey, pulled.ServerTimeUtc.ToString("o"));

        return new SyncStats(
            Pushed: push.Workouts.Count + push.WorkoutExercises.Count + push.ExerciseSets.Count,
            PulledNew: applied.inserted,
            PulledUpdated: applied.updated);
    }

    // --- Сбор push-батча ---

    private async Task<SyncRequest> BuildPushBatchAsync(DateTime? since)
    {
        var batch = new SyncRequest { LastSyncUtc = since };
        var conn = await _db.GetConnectionAsync();

        var workouts = await QueryChanged<Workout>(conn, since);
        foreach (var w in workouts)
        {
            batch.Workouts.Add(new WorkoutSyncDto
            {
                SyncId = w.SyncId,
                Name = w.Name,
                Description = w.Description,
                StartTime = w.StartTime,
                UpdatedAtUtc = w.UpdatedAt,
                IsDeleted = w.IsDeleted
            });
        }

        var wes = await QueryChanged<WorkoutExercise>(conn, since);
        foreach (var we in wes)
        {
            batch.WorkoutExercises.Add(new WorkoutExerciseSyncDto
            {
                SyncId = we.SyncId,
                WorkoutSyncId = we.WorkoutSyncId,
                ExerciseRefId = we.ExerciseId,
                OrderIndex = we.OrderIndex,
                UpdatedAtUtc = we.UpdatedAt,
                IsDeleted = we.IsDeleted
            });
        }

        var sets = await QueryChanged<ExerciseSet>(conn, since);
        foreach (var s in sets)
        {
            batch.ExerciseSets.Add(new ExerciseSetSyncDto
            {
                SyncId = s.SyncId,
                WorkoutExerciseSyncId = s.WorkoutExerciseSyncId,
                SetNumber = s.SetNumber,
                Weight = s.Weight,
                Reps = s.Reps,
                RPE = s.RPE,
                IsAssisted = s.IsAssisted,
                Kind = (int)s.Kind,
                UpdatedAtUtc = s.UpdatedAt,
                IsDeleted = s.IsDeleted
            });
        }

        return batch;
    }

    private static async Task<List<T>> QueryChanged<T>(SQLiteAsyncConnection conn, DateTime? since)
        where T : new()
    {
        if (since == null)
            return await conn.Table<T>().ToListAsync();
        // sqlite-net хранит DateTime как ticks или ISO — сравнение через
        // динамический where на конкретном поле UpdatedAt.
        return await conn.QueryAsync<T>(
            $"SELECT * FROM {GetTableName<T>()} WHERE UpdatedAt > ?", since.Value);
    }

    private static string GetTableName<T>()
    {
        var attr = typeof(T).GetCustomAttributes(typeof(SQLite.TableAttribute), false)
            .FirstOrDefault() as SQLite.TableAttribute;
        return attr?.Name ?? typeof(T).Name;
    }

    // --- Применение серверного ответа ---

    private async Task<(int inserted, int updated)> ApplyServerResponseAsync(SyncResponse resp)
    {
        var conn = await _db.GetConnectionAsync();
        int ins = 0, upd = 0;

        foreach (var dto in resp.Workouts)
        {
            var existing = (await conn.QueryAsync<Workout>(
                "SELECT * FROM Workouts WHERE SyncId = ? LIMIT 1", dto.SyncId)).FirstOrDefault();
            if (existing == null)
            {
                var w = new Workout
                {
                    Name = dto.Name ?? "",
                    Description = dto.Description ?? "",
                    StartTime = dto.StartTime,
                    UpdatedAt = dto.UpdatedAtUtc,
                    IsDeleted = dto.IsDeleted,
                    SyncId = dto.SyncId,
                    UserId = WorkoutDataBase.LocalUserId
                };
                await conn.InsertAsync(w);
                ins++;
            }
            else if (dto.UpdatedAtUtc > existing.UpdatedAt)
            {
                existing.Name = dto.Name ?? "";
                existing.Description = dto.Description ?? "";
                existing.StartTime = dto.StartTime;
                existing.UpdatedAt = dto.UpdatedAtUtc;
                existing.IsDeleted = dto.IsDeleted;
                await conn.UpdateAsync(existing);
                upd++;
            }
        }

        foreach (var dto in resp.WorkoutExercises)
        {
            var existing = (await conn.QueryAsync<WorkoutExercise>(
                "SELECT * FROM WorkoutExercises WHERE SyncId = ? LIMIT 1", dto.SyncId)).FirstOrDefault();
            // Резолвим локальный WorkoutId по WorkoutSyncId.
            var parent = (await conn.QueryAsync<Workout>(
                "SELECT * FROM Workouts WHERE SyncId = ? LIMIT 1", dto.WorkoutSyncId)).FirstOrDefault();
            if (parent == null) continue; // родитель ещё не пришёл — пропускаем

            if (existing == null)
            {
                var we = new WorkoutExercise
                {
                    SyncId = dto.SyncId,
                    WorkoutSyncId = dto.WorkoutSyncId,
                    WorkoutId = parent.Id,
                    ExerciseId = dto.ExerciseRefId,
                    OrderIndex = dto.OrderIndex,
                    UpdatedAt = dto.UpdatedAtUtc,
                    IsDeleted = dto.IsDeleted
                };
                await conn.InsertAsync(we);
                ins++;
            }
            else if (dto.UpdatedAtUtc > existing.UpdatedAt)
            {
                existing.WorkoutSyncId = dto.WorkoutSyncId;
                existing.WorkoutId = parent.Id;
                existing.ExerciseId = dto.ExerciseRefId;
                existing.OrderIndex = dto.OrderIndex;
                existing.UpdatedAt = dto.UpdatedAtUtc;
                existing.IsDeleted = dto.IsDeleted;
                await conn.UpdateAsync(existing);
                upd++;
            }
        }

        foreach (var dto in resp.ExerciseSets)
        {
            var existing = (await conn.QueryAsync<ExerciseSet>(
                "SELECT * FROM ExerciseSets WHERE SyncId = ? LIMIT 1", dto.SyncId)).FirstOrDefault();
            var parent = (await conn.QueryAsync<WorkoutExercise>(
                "SELECT * FROM WorkoutExercises WHERE SyncId = ? LIMIT 1", dto.WorkoutExerciseSyncId)).FirstOrDefault();
            if (parent == null) continue;

            if (existing == null)
            {
                var s = new ExerciseSet
                {
                    SyncId = dto.SyncId,
                    WorkoutExerciseSyncId = dto.WorkoutExerciseSyncId,
                    WorkoutExerciseId = parent.Id,
                    SetNumber = dto.SetNumber,
                    Weight = dto.Weight,
                    Reps = dto.Reps,
                    RPE = dto.RPE,
                    IsAssisted = dto.IsAssisted,
                    Kind = (SetType)dto.Kind,
                    UpdatedAt = dto.UpdatedAtUtc,
                    IsDeleted = dto.IsDeleted
                };
                await conn.InsertAsync(s);
                ins++;
            }
            else if (dto.UpdatedAtUtc > existing.UpdatedAt)
            {
                existing.WorkoutExerciseSyncId = dto.WorkoutExerciseSyncId;
                existing.WorkoutExerciseId = parent.Id;
                existing.SetNumber = dto.SetNumber;
                existing.Weight = dto.Weight;
                existing.Reps = dto.Reps;
                existing.RPE = dto.RPE;
                existing.IsAssisted = dto.IsAssisted;
                existing.Kind = (SetType)dto.Kind;
                existing.UpdatedAt = dto.UpdatedAtUtc;
                existing.IsDeleted = dto.IsDeleted;
                await conn.UpdateAsync(existing);
                upd++;
            }
        }

        return (ins, upd);
    }
}

public record SyncStats(int Pushed, int PulledNew, int PulledUpdated);
