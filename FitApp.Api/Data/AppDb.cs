using Microsoft.Data.Sqlite;

namespace FitApp.Api.Data;

/// <summary>
/// Минималистичный слой доступа к SQLite на сервере. Без EF Core — те же
/// сырые SQL-запросы, что в MAUI (Data/WorkoutDataBase.cs), только через
/// Microsoft.Data.Sqlite вместо sqlite-net-pcl.
///
/// База создаётся при старте приложения (InitAsync), путь к файлу берём из
/// конфигурации (Database:Path). На Render подложим persistent disk, локально
/// — файл в корне проекта.
/// </summary>
public class AppDb
{
    private readonly string _connectionString;

    public AppDb(IConfiguration config)
    {
        var path = config["Database:Path"] ?? "fitapp.db";
        _connectionString = $"Data Source={path}";
    }

    public SqliteConnection OpenConnection()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }

    /// <summary>
    /// Инициализация схемы. Сейчас — только проверочная таблица, на шаге 2
    /// добавим Users, на шаге 3 — все сущности FitApp.
    /// </summary>
    public async Task InitAsync()
    {
        await using var conn = OpenConnection();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS HealthCheck (
                Id INTEGER PRIMARY KEY,
                StartedAtUtc TEXT NOT NULL
            );
            INSERT OR IGNORE INTO HealthCheck (Id, StartedAtUtc)
            VALUES (1, datetime('now'));

            CREATE TABLE IF NOT EXISTS Users (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Email TEXT NOT NULL UNIQUE COLLATE NOCASE,
                PasswordHash TEXT NOT NULL,
                CreatedAtUtc TEXT NOT NULL
            );

            -- Тренировки. SyncId — глобально уникальный GUID, выдаётся
            -- клиентом при создании; именно по нему сервер опознаёт запись
            -- при последующих синхронизациях. (UserId, SyncId) уникальны.
            CREATE TABLE IF NOT EXISTS Workouts (
                SyncId TEXT NOT NULL PRIMARY KEY,
                UserId INTEGER NOT NULL,
                Name TEXT,
                Description TEXT,
                StartTime TEXT,
                UpdatedAtUtc TEXT NOT NULL,
                IsDeleted INTEGER NOT NULL DEFAULT 0,
                FOREIGN KEY (UserId) REFERENCES Users(Id)
            );
            CREATE INDEX IF NOT EXISTS IX_Workouts_User_Updated
                ON Workouts(UserId, UpdatedAtUtc);

            -- Упражнения в тренировке. ExerciseRefId — ссылка на встроенный
            -- справочник упражнений (одинаковый на всех клиентах).
            CREATE TABLE IF NOT EXISTS WorkoutExercises (
                SyncId TEXT NOT NULL PRIMARY KEY,
                UserId INTEGER NOT NULL,
                WorkoutSyncId TEXT NOT NULL,
                ExerciseRefId INTEGER NOT NULL,
                OrderIndex INTEGER NOT NULL DEFAULT 0,
                UpdatedAtUtc TEXT NOT NULL,
                IsDeleted INTEGER NOT NULL DEFAULT 0,
                FOREIGN KEY (UserId) REFERENCES Users(Id)
            );
            CREATE INDEX IF NOT EXISTS IX_WorkoutExercises_User_Updated
                ON WorkoutExercises(UserId, UpdatedAtUtc);
            CREATE INDEX IF NOT EXISTS IX_WorkoutExercises_Workout
                ON WorkoutExercises(WorkoutSyncId);

            -- Подходы.
            CREATE TABLE IF NOT EXISTS ExerciseSets (
                SyncId TEXT NOT NULL PRIMARY KEY,
                UserId INTEGER NOT NULL,
                WorkoutExerciseSyncId TEXT NOT NULL,
                SetNumber INTEGER NOT NULL,
                Weight REAL NOT NULL,
                Reps INTEGER NOT NULL,
                RPE REAL NOT NULL DEFAULT 0,
                IsAssisted INTEGER NOT NULL DEFAULT 0,
                Kind INTEGER NOT NULL DEFAULT 0,
                UpdatedAtUtc TEXT NOT NULL,
                IsDeleted INTEGER NOT NULL DEFAULT 0,
                FOREIGN KEY (UserId) REFERENCES Users(Id)
            );
            CREATE INDEX IF NOT EXISTS IX_ExerciseSets_User_Updated
                ON ExerciseSets(UserId, UpdatedAtUtc);
            CREATE INDEX IF NOT EXISTS IX_ExerciseSets_WorkoutExercise
                ON ExerciseSets(WorkoutExerciseSyncId);
        ";
        await cmd.ExecuteNonQueryAsync();
    }
}
