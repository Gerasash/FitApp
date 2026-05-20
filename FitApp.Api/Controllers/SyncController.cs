using System.Security.Claims;
using FitApp.Api.Data;
using FitApp.Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace FitApp.Api.Controllers;

/// <summary>
/// Единый sync endpoint — клиент шлёт батч изменений, сервер применяет
/// last-write-wins по UpdatedAtUtc и возвращает то, что клиент ещё не видел.
///
/// Все запросы требуют JWT. UserId берём из claim sub, никогда из тела
/// запроса — иначе любой авторизованный пользователь смог бы писать в чужой
/// аккаунт.
/// </summary>
[ApiController]
[Authorize]
[Route("sync")]
public class SyncController : ControllerBase
{
    private readonly AppDb _db;
    private readonly ILogger<SyncController> _log;

    public SyncController(AppDb db, ILogger<SyncController> log)
    {
        _db = db;
        _log = log;
    }

    [HttpPost]
    public async Task<ActionResult<SyncResponse>> Sync([FromBody] SyncRequest req)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        await using var conn = _db.OpenConnection();
        await using var tx = await conn.BeginTransactionAsync();

        // 1. Применяем входящие изменения клиента.
        await UpsertWorkouts(conn, tx, userId.Value, req.Workouts);
        await UpsertWorkoutExercises(conn, tx, userId.Value, req.WorkoutExercises);
        await UpsertExerciseSets(conn, tx, userId.Value, req.ExerciseSets);

        // 2. Фиксируем «время сервера сейчас», чтобы вернуть его клиенту
        //    ДО того как начнём селектить — иначе вторая половина транзакции
        //    могла бы пропустить записи, попавшие в неё после select.
        var serverTime = DateTime.UtcNow;

        // 3. Вычитываем серверные изменения, которые клиент ещё не видел.
        var resp = new SyncResponse
        {
            ServerTimeUtc = serverTime,
            Workouts = await SelectWorkouts(conn, tx, userId.Value, req.LastSyncUtc),
            WorkoutExercises = await SelectWorkoutExercises(conn, tx, userId.Value, req.LastSyncUtc),
            ExerciseSets = await SelectExerciseSets(conn, tx, userId.Value, req.LastSyncUtc)
        };

        await tx.CommitAsync();

        _log.LogInformation(
            "Sync user={UserId} pushed W={W} WE={WE} ES={ES} pulled W={pW} WE={pWE} ES={pES}",
            userId, req.Workouts.Count, req.WorkoutExercises.Count, req.ExerciseSets.Count,
            resp.Workouts.Count, resp.WorkoutExercises.Count, resp.ExerciseSets.Count);

        return Ok(resp);
    }

    private long? GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue("sub");
        return long.TryParse(sub, out var id) ? id : null;
    }

    // ---------- UPSERT (push) ----------
    // Логика одна и та же: если запись новее существующей по UpdatedAtUtc —
    // перезаписываем; иначе игнорируем (last-write-wins).

    private static async Task UpsertWorkouts(NpgsqlConnection conn, NpgsqlTransaction tx,
        long userId, List<WorkoutSyncDto> items)
    {
        foreach (var w in items)
        {
            await using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"
                INSERT INTO Workouts (SyncId, UserId, Name, Description, StartTime, UpdatedAtUtc, IsDeleted)
                VALUES (@sid, @uid, @name, @desc, @start, @upd, @del)
                ON CONFLICT(SyncId) DO UPDATE SET
                    Name = EXCLUDED.Name,
                    Description = EXCLUDED.Description,
                    StartTime = EXCLUDED.StartTime,
                    UpdatedAtUtc = EXCLUDED.UpdatedAtUtc,
                    IsDeleted = EXCLUDED.IsDeleted
                WHERE EXCLUDED.UpdatedAtUtc > Workouts.UpdatedAtUtc
                  AND Workouts.UserId = @uid;";
            cmd.Parameters.AddWithValue("@sid", w.SyncId);
            cmd.Parameters.AddWithValue("@uid", userId);
            cmd.Parameters.AddWithValue("@name", (object?)w.Name ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@desc", (object?)w.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@start", w.StartTime.ToString("o"));
            cmd.Parameters.AddWithValue("@upd", w.UpdatedAtUtc.ToString("o"));
            cmd.Parameters.AddWithValue("@del", w.IsDeleted ? 1 : 0);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    private static async Task UpsertWorkoutExercises(NpgsqlConnection conn, NpgsqlTransaction tx,
        long userId, List<WorkoutExerciseSyncDto> items)
    {
        foreach (var we in items)
        {
            await using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"
                INSERT INTO WorkoutExercises (SyncId, UserId, WorkoutSyncId, ExerciseRefId, OrderIndex, UpdatedAtUtc, IsDeleted)
                VALUES (@sid, @uid, @wid, @eid, @ord, @upd, @del)
                ON CONFLICT(SyncId) DO UPDATE SET
                    WorkoutSyncId = EXCLUDED.WorkoutSyncId,
                    ExerciseRefId = EXCLUDED.ExerciseRefId,
                    OrderIndex = EXCLUDED.OrderIndex,
                    UpdatedAtUtc = EXCLUDED.UpdatedAtUtc,
                    IsDeleted = EXCLUDED.IsDeleted
                WHERE EXCLUDED.UpdatedAtUtc > WorkoutExercises.UpdatedAtUtc
                  AND WorkoutExercises.UserId = @uid;";
            cmd.Parameters.AddWithValue("@sid", we.SyncId);
            cmd.Parameters.AddWithValue("@uid", userId);
            cmd.Parameters.AddWithValue("@wid", we.WorkoutSyncId);
            cmd.Parameters.AddWithValue("@eid", we.ExerciseRefId);
            cmd.Parameters.AddWithValue("@ord", we.OrderIndex);
            cmd.Parameters.AddWithValue("@upd", we.UpdatedAtUtc.ToString("o"));
            cmd.Parameters.AddWithValue("@del", we.IsDeleted ? 1 : 0);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    private static async Task UpsertExerciseSets(NpgsqlConnection conn, NpgsqlTransaction tx,
        long userId, List<ExerciseSetSyncDto> items)
    {
        foreach (var s in items)
        {
            await using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"
                INSERT INTO ExerciseSets (SyncId, UserId, WorkoutExerciseSyncId, SetNumber, Weight, Reps, RPE, IsAssisted, Kind, UpdatedAtUtc, IsDeleted)
                VALUES (@sid, @uid, @weid, @num, @w, @reps, @rpe, @assist, @kind, @upd, @del)
                ON CONFLICT(SyncId) DO UPDATE SET
                    WorkoutExerciseSyncId = EXCLUDED.WorkoutExerciseSyncId,
                    SetNumber = EXCLUDED.SetNumber,
                    Weight = EXCLUDED.Weight,
                    Reps = EXCLUDED.Reps,
                    RPE = EXCLUDED.RPE,
                    IsAssisted = EXCLUDED.IsAssisted,
                    Kind = EXCLUDED.Kind,
                    UpdatedAtUtc = EXCLUDED.UpdatedAtUtc,
                    IsDeleted = EXCLUDED.IsDeleted
                WHERE EXCLUDED.UpdatedAtUtc > ExerciseSets.UpdatedAtUtc
                  AND ExerciseSets.UserId = @uid;";
            cmd.Parameters.AddWithValue("@sid", s.SyncId);
            cmd.Parameters.AddWithValue("@uid", userId);
            cmd.Parameters.AddWithValue("@weid", s.WorkoutExerciseSyncId);
            cmd.Parameters.AddWithValue("@num", s.SetNumber);
            cmd.Parameters.AddWithValue("@w", s.Weight);
            cmd.Parameters.AddWithValue("@reps", s.Reps);
            cmd.Parameters.AddWithValue("@rpe", s.RPE);
            cmd.Parameters.AddWithValue("@assist", s.IsAssisted ? 1 : 0);
            cmd.Parameters.AddWithValue("@kind", s.Kind);
            cmd.Parameters.AddWithValue("@upd", s.UpdatedAtUtc.ToString("o"));
            cmd.Parameters.AddWithValue("@del", s.IsDeleted ? 1 : 0);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    // ---------- SELECT (pull) ----------

    private static async Task<List<WorkoutSyncDto>> SelectWorkouts(NpgsqlConnection conn,
        NpgsqlTransaction tx, long userId, DateTime? since)
    {
        var list = new List<WorkoutSyncDto>();
        await using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = @"SELECT SyncId, Name, Description, StartTime, UpdatedAtUtc, IsDeleted
                            FROM Workouts
                            WHERE UserId = @uid
                              AND (@since::text IS NULL OR UpdatedAtUtc > @since)
                            ORDER BY UpdatedAtUtc";
        cmd.Parameters.AddWithValue("@uid", userId);
        cmd.Parameters.AddWithValue("@since", (object?)since?.ToString("o") ?? DBNull.Value);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            list.Add(new WorkoutSyncDto
            {
                SyncId = r.GetString(0),
                Name = r.IsDBNull(1) ? null : r.GetString(1),
                Description = r.IsDBNull(2) ? null : r.GetString(2),
                StartTime = DateTime.Parse(r.GetString(3)),
                UpdatedAtUtc = DateTime.Parse(r.GetString(4)),
                IsDeleted = r.GetInt32(5) != 0
            });
        }
        return list;
    }

    private static async Task<List<WorkoutExerciseSyncDto>> SelectWorkoutExercises(NpgsqlConnection conn,
        NpgsqlTransaction tx, long userId, DateTime? since)
    {
        var list = new List<WorkoutExerciseSyncDto>();
        await using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = @"SELECT SyncId, WorkoutSyncId, ExerciseRefId, OrderIndex, UpdatedAtUtc, IsDeleted
                            FROM WorkoutExercises
                            WHERE UserId = @uid
                              AND (@since::text IS NULL OR UpdatedAtUtc > @since)
                            ORDER BY UpdatedAtUtc";
        cmd.Parameters.AddWithValue("@uid", userId);
        cmd.Parameters.AddWithValue("@since", (object?)since?.ToString("o") ?? DBNull.Value);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            list.Add(new WorkoutExerciseSyncDto
            {
                SyncId = r.GetString(0),
                WorkoutSyncId = r.GetString(1),
                ExerciseRefId = r.GetInt32(2),
                OrderIndex = r.GetInt32(3),
                UpdatedAtUtc = DateTime.Parse(r.GetString(4)),
                IsDeleted = r.GetInt32(5) != 0
            });
        }
        return list;
    }

    private static async Task<List<ExerciseSetSyncDto>> SelectExerciseSets(NpgsqlConnection conn,
        NpgsqlTransaction tx, long userId, DateTime? since)
    {
        var list = new List<ExerciseSetSyncDto>();
        await using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = @"SELECT SyncId, WorkoutExerciseSyncId, SetNumber, Weight, Reps, RPE, IsAssisted, Kind, UpdatedAtUtc, IsDeleted
                            FROM ExerciseSets
                            WHERE UserId = @uid
                              AND (@since::text IS NULL OR UpdatedAtUtc > @since)
                            ORDER BY UpdatedAtUtc";
        cmd.Parameters.AddWithValue("@uid", userId);
        cmd.Parameters.AddWithValue("@since", (object?)since?.ToString("o") ?? DBNull.Value);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            list.Add(new ExerciseSetSyncDto
            {
                SyncId = r.GetString(0),
                WorkoutExerciseSyncId = r.GetString(1),
                SetNumber = r.GetInt32(2),
                Weight = r.GetDouble(3),
                Reps = r.GetInt32(4),
                RPE = r.GetDouble(5),
                IsAssisted = r.GetInt32(6) != 0,
                Kind = r.GetInt32(7),
                UpdatedAtUtc = DateTime.Parse(r.GetString(8)),
                IsDeleted = r.GetInt32(9) != 0
            });
        }
        return list;
    }
}
