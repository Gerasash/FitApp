using Npgsql;

namespace FitApp.Api.Data;

/// <summary>
/// Слой доступа к PostgreSQL. Подключение берём из переменной окружения
/// DATABASE_URL (формат Neon/Render: postgres://user:pass@host/db?sslmode=require).
/// Схема создаётся идемпотентно через CREATE TABLE IF NOT EXISTS.
/// </summary>
public class AppDb
{
    private readonly string _connectionString;

    public AppDb(IConfiguration config)
    {
        var raw = config["DATABASE_URL"]
                  ?? config["ConnectionStrings:Postgres"]
                  ?? throw new InvalidOperationException(
                      "PostgreSQL connection string not found. Set DATABASE_URL env var.");

        _connectionString = BuildConnectionString(raw);
    }

    /// <summary>
    /// Конвертирует URI-формат (postgres://user:pass@host/db?sslmode=require)
    /// в формат ключ-значение, который понимает Npgsql.
    /// Если строка уже в формате ADO.NET — возвращает как есть.
    /// </summary>
    private static string BuildConnectionString(string rawUrl)
    {
        if (rawUrl.Contains("Host=", StringComparison.OrdinalIgnoreCase) ||
            rawUrl.Contains("Server=", StringComparison.OrdinalIgnoreCase))
            return rawUrl;

        // Нормализуем схему: Npgsql умеет разбирать Uri только как Uri, но
        // System.Uri принимает оба варианта (postgres:// и postgresql://).
        var normalized = rawUrl.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            ? "postgresql://" + rawUrl["postgres://".Length..]
            : rawUrl;

        var uri = new Uri(normalized);
        var userInfo = uri.UserInfo.Split(':');

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "",
            Database = uri.AbsolutePath.TrimStart('/'),
            SslMode = SslMode.Require,
            TrustServerCertificate = true
        };

        return builder.ToString();
    }

    public NpgsqlConnection OpenConnection()
    {
        var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        return conn;
    }

    public async Task InitAsync()
    {
        await using var conn = OpenConnection();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Users (
                Id         BIGSERIAL PRIMARY KEY,
                Email      TEXT NOT NULL UNIQUE,
                PasswordHash TEXT NOT NULL,
                CreatedAtUtc TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Workouts (
                SyncId       TEXT NOT NULL PRIMARY KEY,
                UserId       BIGINT NOT NULL,
                Name         TEXT,
                Description  TEXT,
                StartTime    TEXT,
                UpdatedAtUtc TEXT NOT NULL,
                IsDeleted    INTEGER NOT NULL DEFAULT 0
            );
            CREATE INDEX IF NOT EXISTS IX_Workouts_User_Updated
                ON Workouts(UserId, UpdatedAtUtc);

            CREATE TABLE IF NOT EXISTS WorkoutExercises (
                SyncId        TEXT NOT NULL PRIMARY KEY,
                UserId        BIGINT NOT NULL,
                WorkoutSyncId TEXT NOT NULL,
                ExerciseRefId INTEGER NOT NULL,
                OrderIndex    INTEGER NOT NULL DEFAULT 0,
                UpdatedAtUtc  TEXT NOT NULL,
                IsDeleted     INTEGER NOT NULL DEFAULT 0
            );
            CREATE INDEX IF NOT EXISTS IX_WorkoutExercises_User_Updated
                ON WorkoutExercises(UserId, UpdatedAtUtc);
            CREATE INDEX IF NOT EXISTS IX_WorkoutExercises_Workout
                ON WorkoutExercises(WorkoutSyncId);

            CREATE TABLE IF NOT EXISTS ExerciseSets (
                SyncId                TEXT NOT NULL PRIMARY KEY,
                UserId                BIGINT NOT NULL,
                WorkoutExerciseSyncId TEXT NOT NULL,
                SetNumber             INTEGER NOT NULL,
                Weight                DOUBLE PRECISION NOT NULL DEFAULT 0,
                Reps                  INTEGER NOT NULL DEFAULT 0,
                RPE                   DOUBLE PRECISION NOT NULL DEFAULT 0,
                IsAssisted            INTEGER NOT NULL DEFAULT 0,
                Kind                  INTEGER NOT NULL DEFAULT 0,
                UpdatedAtUtc          TEXT NOT NULL,
                IsDeleted             INTEGER NOT NULL DEFAULT 0
            );
            CREATE INDEX IF NOT EXISTS IX_ExerciseSets_User_Updated
                ON ExerciseSets(UserId, UpdatedAtUtc);
            CREATE INDEX IF NOT EXISTS IX_ExerciseSets_WorkoutExercise
                ON ExerciseSets(WorkoutExerciseSyncId);
        ";
        await cmd.ExecuteNonQueryAsync();
    }
}
