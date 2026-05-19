using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using FitApp.Models;
using SQLite;

namespace FitApp.Data
{
    public class WorkoutDataBase
    {
        private const string DbName = "Workout.db";
        private readonly SQLiteAsyncConnection _connection;
        private readonly Task _initTask;
        private bool _initialized;

        public WorkoutDataBase()
        {
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, DbName);
            _connection = new SQLiteAsyncConnection(dbPath);
            _initTask = InitAsync();
        }

        private async Task InitAsync()
        {
            if (_initialized) return;

            await _connection.CreateTableAsync<User>();
            await _connection.CreateTableAsync<Workout>();
            await _connection.CreateTableAsync<Exercise>();
            await _connection.CreateTableAsync<MuscleGroup>();
            await _connection.CreateTableAsync<WorkoutMuscleGroup>();
            await _connection.CreateTableAsync<WorkoutExercise>();
            await _connection.CreateTableAsync<ExerciseSet>();
            await _connection.CreateTableAsync<AIPrediction>();
            await _connection.CreateTableAsync<ExerciseMuscleGroup>();
            await _connection.CreateTableAsync<WorkoutTemplate>();
            await _connection.CreateTableAsync<TemplateExercise>();

            await EnsureLocalUserAsync();
            await SeedBuiltInExercisesAsync();
            await SeedBuiltInTemplatesAsync();

            _initialized = true;
        }

        // --- Профиль пользователя ---

        // Id локального профиля. Пока нет аутентификации — всегда 1.
        // Когда подключим Web API, отсюда же будет возвращаться Id залогиненного юзера.
        public const int LocalUserId = 1;

        // Создаёт локальный профиль при первом запуске и привязывает к нему уже
        // существующие тренировки/шаблоны, у которых UserId=0 (старые записи до миграции).
        private async Task EnsureLocalUserAsync()
        {
            var existing = await _connection.FindAsync<User>(LocalUserId);
            if (existing == null)
            {
                await _connection.InsertAsync(new User
                {
                    Id = LocalUserId,
                    DisplayName = "Я",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            // Бэкфилл UserId на старых записях, созданных до появления поля.
            await _connection.ExecuteAsync(
                "UPDATE Workouts SET UserId = ? WHERE UserId = 0", LocalUserId);
            await _connection.ExecuteAsync(
                "UPDATE WorkoutTemplates SET UserId = ? WHERE UserId = 0 AND IsBuiltIn = 0",
                LocalUserId);
        }

        public async Task<User> GetCurrentUserAsync()
        {
            await EnsureInitializedAsync();
            var user = await _connection.FindAsync<User>(LocalUserId);
            return user ?? new User { Id = LocalUserId, DisplayName = "Я" };
        }

        public async Task<int> SaveUserAsync(User user)
        {
            await EnsureInitializedAsync();
            user.UpdatedAt = DateTime.UtcNow;
            if (user.Id == 0) user.Id = LocalUserId;
            var existing = await _connection.FindAsync<User>(user.Id);
            return existing == null
                ? await _connection.InsertAsync(user)
                : await _connection.UpdateAsync(user);
        }

        // --- Seed встроенной базы упражнений (один раз при первом запуске) ---

        private const string SeedFileName = "exercises_seed.json";

        private async Task SeedBuiltInExercisesAsync()
        {
            try
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync(SeedFileName);
                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync();
                var seed = JsonSerializer.Deserialize<SeedRoot>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                if (seed == null) return;

                // 1) Группы мышц — find-or-create по имени (восстанавливаются, если юзер их удалил)
                var allMg = await _connection.Table<MuscleGroup>().ToListAsync();
                var mgByName = allMg.ToDictionary(m => m.Name.Trim().ToLowerInvariant(), m => m);
                foreach (var name in seed.MuscleGroups ?? Enumerable.Empty<string>())
                {
                    var key = name.Trim().ToLowerInvariant();
                    if (mgByName.ContainsKey(key)) continue;
                    var mg = new MuscleGroup(name);
                    await _connection.InsertAsync(mg);
                    mgByName[key] = mg;
                }

                // 2) Упражнения — find-or-create по имени; связи EMG пересоздаём, чтобы Id всегда были валидные.
                var existingExercises = await _connection.Table<Exercise>().ToListAsync();
                var exByName = existingExercises
                    .GroupBy(e => e.Name.Trim().ToLowerInvariant())
                    .ToDictionary(g => g.Key, g => g.First());

                foreach (var ex in seed.Exercises ?? Enumerable.Empty<SeedExercise>())
                {
                    int primaryMgId = 0;
                    if (ex.Muscles is { Count: > 0 })
                    {
                        var primary = ex.Muscles.FirstOrDefault(m => m.Role == 0) ?? ex.Muscles[0];
                        if (mgByName.TryGetValue(primary.Name.Trim().ToLowerInvariant(), out var pmg))
                            primaryMgId = pmg.Id;
                    }

                    var key = ex.Name.Trim().ToLowerInvariant();
                    Exercise entity;
                    if (exByName.TryGetValue(key, out var existing))
                    {
                        entity = existing;
                        // Подновляем мета-данные на случай, если они обновились в JSON
                        entity.NameEn = ex.NameEn;
                        entity.EquipmentType = ex.Equipment;
                        entity.Category = ex.Category;
                        entity.Mechanic = ex.Mechanic;
                        entity.Instructions = ex.Instructions;
                        if (primaryMgId != 0) entity.PrimaryMuscleGroupId = primaryMgId;
                        await _connection.UpdateAsync(entity);
                    }
                    else
                    {
                        entity = new Exercise
                        {
                            Name = ex.Name,
                            NameEn = ex.NameEn,
                            PrimaryMuscleGroupId = primaryMgId,
                            EquipmentType = ex.Equipment,
                            Category = ex.Category,
                            Mechanic = ex.Mechanic,
                            Instructions = ex.Instructions,
                            IsCustom = false,
                            IsArchived = false,
                            IsFavorite = false,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _connection.InsertAsync(entity);
                    }

                    // Пересобираем связи мышц для этого упражнения
                    await _connection.ExecuteAsync(
                        "DELETE FROM ExerciseMuscleGroups WHERE ExerciseId = ?", entity.Id);
                    if (ex.Muscles != null)
                    {
                        foreach (var m in ex.Muscles)
                        {
                            if (!mgByName.TryGetValue(m.Name.Trim().ToLowerInvariant(), out var mg)) continue;
                            await _connection.InsertAsync(new ExerciseMuscleGroup
                            {
                                ExerciseId = entity.Id,
                                MuscleGroupId = mg.Id,
                                Role = m.Role
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Seed] Ошибка загрузки базы упражнений: {ex}");
            }
        }

        private sealed class SeedRoot
        {
            [JsonPropertyName("version")] public int Version { get; set; }
            [JsonPropertyName("muscleGroups")] public List<string>? MuscleGroups { get; set; }
            [JsonPropertyName("exercises")] public List<SeedExercise>? Exercises { get; set; }
        }

        private sealed class SeedExercise
        {
            [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
            [JsonPropertyName("nameEn")] public string? NameEn { get; set; }
            [JsonPropertyName("equipment")] public int Equipment { get; set; }
            [JsonPropertyName("category")] public int Category { get; set; }
            [JsonPropertyName("mechanic")] public int Mechanic { get; set; }
            [JsonPropertyName("instructions")] public string? Instructions { get; set; }
            [JsonPropertyName("muscles")] public List<SeedMuscle>? Muscles { get; set; }
        }

        private sealed class SeedMuscle
        {
            [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
            [JsonPropertyName("role")] public int Role { get; set; }
        }

        // --- Seed встроенных шаблонов тренировок ---

        private const string TemplatesSeedFileName = "templates_seed.json";

        private async Task SeedBuiltInTemplatesAsync()
        {
            try
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync(TemplatesSeedFileName);
                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync();
                var seed = JsonSerializer.Deserialize<TemplateSeedRoot>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                if (seed?.Templates == null) return;

                // Карта упражнений по имени (lowercase) — чтобы привязывать TemplateExercise по имени из JSON
                var allExercises = await _connection.Table<Exercise>().ToListAsync();
                var exByName = allExercises
                    .GroupBy(e => e.Name.Trim().ToLowerInvariant())
                    .ToDictionary(g => g.Key, g => g.First());

                // Карта существующих встроенных шаблонов по имени
                var existingTemplates = await _connection.Table<WorkoutTemplate>()
                    .Where(t => t.IsBuiltIn)
                    .ToListAsync();
                var tplByName = existingTemplates
                    .GroupBy(t => t.Name.Trim().ToLowerInvariant())
                    .ToDictionary(g => g.Key, g => g.First());

                int order = 0;
                foreach (var t in seed.Templates)
                {
                    var key = t.Name.Trim().ToLowerInvariant();
                    WorkoutTemplate entity;
                    if (tplByName.TryGetValue(key, out var existing))
                    {
                        entity = existing;
                        entity.Description = t.Description;
                        entity.FolderName = t.Folder;
                        entity.OrderIndex = order;
                        await _connection.UpdateAsync(entity);
                    }
                    else
                    {
                        entity = new WorkoutTemplate
                        {
                            Name = t.Name,
                            Description = t.Description,
                            FolderName = t.Folder,
                            IsBuiltIn = true,
                            OrderIndex = order,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _connection.InsertAsync(entity);
                    }
                    order++;

                    // Пересобираем список упражнений шаблона
                    await _connection.ExecuteAsync(
                        "DELETE FROM TemplateExercises WHERE TemplateId = ?", entity.Id);

                    if (t.Exercises != null)
                    {
                        int idx = 0;
                        foreach (var te in t.Exercises)
                        {
                            if (!exByName.TryGetValue(te.Exercise.Trim().ToLowerInvariant(), out var ex))
                            {
                                System.Diagnostics.Debug.WriteLine(
                                    $"[Seed] Шаблон '{t.Name}': не нашёл упражнение '{te.Exercise}', пропуск");
                                continue;
                            }
                            await _connection.InsertAsync(new TemplateExercise
                            {
                                TemplateId = entity.Id,
                                ExerciseId = ex.Id,
                                OrderIndex = idx++,
                                TargetSets = te.Sets > 0 ? te.Sets : 3,
                                RepsMin = te.RepsMin,
                                RepsMax = te.RepsMax,
                                RestSeconds = te.Rest,
                                Notes = te.Notes
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Seed] Ошибка загрузки шаблонов: {ex}");
            }
        }

        private sealed class TemplateSeedRoot
        {
            [JsonPropertyName("templates")] public List<TemplateSeed>? Templates { get; set; }
        }

        private sealed class TemplateSeed
        {
            [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
            [JsonPropertyName("folder")] public string? Folder { get; set; }
            [JsonPropertyName("description")] public string? Description { get; set; }
            [JsonPropertyName("exercises")] public List<TemplateExerciseSeed>? Exercises { get; set; }
        }

        private sealed class TemplateExerciseSeed
        {
            [JsonPropertyName("exercise")] public string Exercise { get; set; } = string.Empty;
            [JsonPropertyName("sets")] public int Sets { get; set; } = 3;
            [JsonPropertyName("repsMin")] public int? RepsMin { get; set; }
            [JsonPropertyName("repsMax")] public int? RepsMax { get; set; }
            [JsonPropertyName("rest")] public int? Rest { get; set; }
            [JsonPropertyName("notes")] public string? Notes { get; set; }
        }

        // --- Шаблоны: CRUD + запуск тренировки ---

        public async Task<List<WorkoutTemplate>> GetTemplatesAsync(bool includeArchived = false)
        {
            await EnsureInitializedAsync();
            var q = _connection.Table<WorkoutTemplate>();
            if (!includeArchived) q = q.Where(t => !t.IsArchived);
            return await q.OrderBy(t => t.OrderIndex).ToListAsync();
        }

        public async Task<WorkoutTemplate?> GetTemplateAsync(int id)
        {
            await EnsureInitializedAsync();
            return await _connection.FindAsync<WorkoutTemplate>(id);
        }

        public async Task<List<TemplateExercise>> GetTemplateExercisesAsync(int templateId)
        {
            await EnsureInitializedAsync();
            var items = await _connection.Table<TemplateExercise>()
                .Where(te => te.TemplateId == templateId)
                .OrderBy(te => te.OrderIndex)
                .ToListAsync();
            foreach (var it in items)
            {
                var ex = await _connection.FindAsync<Exercise>(it.ExerciseId);
                it.ExerciseName = ex?.Name;
            }
            return items;
        }

        public async Task<int> SaveTemplateAsync(WorkoutTemplate template)
        {
            await EnsureInitializedAsync();
            return template.Id == 0
                ? await _connection.InsertAsync(template)
                : await _connection.UpdateAsync(template);
        }

        public async Task DeleteTemplateAsync(WorkoutTemplate template)
        {
            await EnsureInitializedAsync();
            if (template.IsBuiltIn)
            {
                // Встроенные не удаляем — архивируем (восстановятся как видимые при сбросе IsArchived)
                template.IsArchived = true;
                await _connection.UpdateAsync(template);
            }
            else
            {
                await _connection.ExecuteAsync(
                    "DELETE FROM TemplateExercises WHERE TemplateId = ?", template.Id);
                await _connection.DeleteAsync(template);
            }
        }

        public async Task<int> AddTemplateExerciseAsync(TemplateExercise te)
        {
            await EnsureInitializedAsync();
            return te.Id == 0
                ? await _connection.InsertAsync(te)
                : await _connection.UpdateAsync(te);
        }

        public async Task DeleteTemplateExerciseAsync(TemplateExercise te)
        {
            await EnsureInitializedAsync();
            await _connection.DeleteAsync(te);
        }

        // Создаёт новую тренировку на основе шаблона. Возвращает её Id.
        // Подходы не пред-создаются — юзер добавляет по факту.
        public async Task<int> StartWorkoutFromTemplateAsync(int templateId, DateTime? startTime = null)
        {
            await EnsureInitializedAsync();
            var template = await _connection.FindAsync<WorkoutTemplate>(templateId);
            if (template == null) throw new InvalidOperationException("Шаблон не найден");

            var templateExercises = await _connection.Table<TemplateExercise>()
                .Where(te => te.TemplateId == templateId)
                .OrderBy(te => te.OrderIndex)
                .ToListAsync();

            // 1) Создаём тренировку
            var workout = new Workout
            {
                Name = template.Name,
                Description = template.Description ?? string.Empty,
                StartTime = startTime ?? DateTime.Now
            };
            await _connection.InsertAsync(workout);

            // 2) Копируем упражнения
            int order = 1;
            var muscleIds = new HashSet<int>();
            foreach (var te in templateExercises)
            {
                var we = new WorkoutExercise
                {
                    WorkoutId = workout.Id,
                    ExerciseId = te.ExerciseId,
                    OrderIndex = order++
                };
                await _connection.InsertAsync(we);

                // Пред-создаём пустые подходы по TargetSets из шаблона,
                // чтобы юзер сразу видел плановые сеты и заполнял вес/повторы.
                int targetSets = te.TargetSets > 0 ? te.TargetSets : 3;
                int suggestedReps = te.RepsMax ?? te.RepsMin ?? 0;
                for (int i = 1; i <= targetSets; i++)
                {
                    await _connection.InsertAsync(new ExerciseSet
                    {
                        WorkoutExerciseId = we.Id,
                        SetNumber = i,
                        Weight = 0,
                        Reps = suggestedReps,
                        RPE = 0
                    });
                }

                // Собираем мышцы упражнения для подвязки к тренировке
                var emgs = await _connection.Table<ExerciseMuscleGroup>()
                    .Where(x => x.ExerciseId == te.ExerciseId)
                    .ToListAsync();
                foreach (var emg in emgs) muscleIds.Add(emg.MuscleGroupId);
            }

            // 3) Подвязываем мышцы к тренировке
            foreach (var mid in muscleIds)
            {
                await _connection.InsertAsync(new WorkoutMuscleGroup(workout.Id, mid));
            }

            // 4) Обновляем статистику шаблона
            template.TimesUsed += 1;
            template.LastUsedAt = DateTime.UtcNow;
            await _connection.UpdateAsync(template);

            return workout.Id;
        }

        // Создаёт пользовательский шаблон из текущей тренировки.
        // TargetSets = фактическое число подходов упражнения; RepsMin/RepsMax — диапазон из реальных повторений.
        public async Task<int> CreateTemplateFromWorkoutAsync(int workoutId, string name, string? folder = "Мои")
        {
            await EnsureInitializedAsync();
            var workout = await _connection.FindAsync<Workout>(workoutId);
            if (workout == null) throw new InvalidOperationException("Тренировка не найдена");

            var template = new WorkoutTemplate
            {
                Name = string.IsNullOrWhiteSpace(name) ? workout.Name : name.Trim(),
                Description = workout.Description,
                FolderName = string.IsNullOrWhiteSpace(folder) ? "Мои" : folder,
                IsBuiltIn = false,
                CreatedAt = DateTime.UtcNow
            };
            await _connection.InsertAsync(template);

            var workoutExercises = await _connection.Table<WorkoutExercise>()
                .Where(we => we.WorkoutId == workoutId)
                .OrderBy(we => we.OrderIndex)
                .ToListAsync();

            int order = 0;
            foreach (var we in workoutExercises)
            {
                var sets = await _connection.Table<ExerciseSet>()
                    .Where(s => s.WorkoutExerciseId == we.Id)
                    .ToListAsync();
                int targetSets = sets.Count > 0 ? sets.Count : 3;
                int? repsMin = sets.Count > 0 ? sets.Min(s => s.Reps) : null;
                int? repsMax = sets.Count > 0 ? sets.Max(s => s.Reps) : null;

                await _connection.InsertAsync(new TemplateExercise
                {
                    TemplateId = template.Id,
                    ExerciseId = we.ExerciseId,
                    OrderIndex = order++,
                    TargetSets = targetSets,
                    RepsMin = repsMin,
                    RepsMax = repsMax
                });
            }

            return template.Id;
        }

        // --- ExerciseMuscleGroup helpers ---

        public async Task<List<MuscleGroup>> GetMusclesForExerciseAsync(int exerciseId)
        {
            await EnsureInitializedAsync();
            return await _connection.QueryAsync<MuscleGroup>(@"
                SELECT mg.* FROM MuscleGroups mg
                INNER JOIN ExerciseMuscleGroups emg ON mg.Id = emg.MuscleGroupId
                WHERE emg.ExerciseId = ?
                ORDER BY emg.Role ASC", exerciseId);
        }

        public async Task<List<ExerciseMuscleGroup>> GetExerciseMusclesAsync(int exerciseId)
        {
            await EnsureInitializedAsync();
            return await _connection.Table<ExerciseMuscleGroup>()
                .Where(x => x.ExerciseId == exerciseId)
                .ToListAsync();
        }

        // Все связи упражнение↔мышца одним запросом (для построения списка с тегами без N+1)
        public async Task<List<ExerciseMuscleGroup>> GetAllExerciseMusclesAsync()
        {
            await EnsureInitializedAsync();
            return await _connection.Table<ExerciseMuscleGroup>().ToListAsync();
        }

        public async Task ToggleExerciseFavoriteAsync(int exerciseId)
        {
            await EnsureInitializedAsync();
            var ex = await _connection.FindAsync<Exercise>(exerciseId);
            if (ex == null) return;
            ex.IsFavorite = !ex.IsFavorite;
            await _connection.UpdateAsync(ex);
        }

        public async Task<List<Exercise>> GetExercisesByMuscleAsync(int muscleGroupId, bool primaryOnly = false)
        {
            await EnsureInitializedAsync();
            var sql = primaryOnly
                ? @"SELECT DISTINCT e.* FROM Exercises e
                    INNER JOIN ExerciseMuscleGroups emg ON e.Id = emg.ExerciseId
                    WHERE emg.MuscleGroupId = ? AND emg.Role = 0 AND e.IsArchived = 0
                    ORDER BY e.Name"
                : @"SELECT DISTINCT e.* FROM Exercises e
                    INNER JOIN ExerciseMuscleGroups emg ON e.Id = emg.ExerciseId
                    WHERE emg.MuscleGroupId = ? AND e.IsArchived = 0
                    ORDER BY e.Name";
            return await _connection.QueryAsync<Exercise>(sql, muscleGroupId);
        }

        private Task EnsureInitializedAsync() => _initTask;

        // --- Упражнения ---

        public async Task<List<Exercise>> GetExercisesAsync()
        {
            await EnsureInitializedAsync();
            return await _connection.Table<Exercise>().ToListAsync();
        }

        public async Task<List<Exercise>> GetAllExercisesAsync()
        {
            await EnsureInitializedAsync();
            return await _connection.Table<Exercise>().ToListAsync();
        }

        public async Task<List<Exercise>> SearchExercisesAsync(string searchText)
        {
            await EnsureInitializedAsync();
            return await _connection.Table<Exercise>()
                .Where(x => x.Name.ToLower().Contains(searchText.ToLower()))
                .ToListAsync();
        }

        public async Task<Exercise?> GetExerciseByIdAsync(int exerciseId)
        {
            await EnsureInitializedAsync();
            return await _connection.FindAsync<Exercise>(exerciseId);
        }

        /// <summary>Имя первичной мышечной группы упражнения (Role=0).
        /// Используется ML-сервисом для кодирования категориальной фичи.
        /// Реализовано через типизированный API (без сырого SQL), потому что
        /// имя таблицы MuscleGroup в БД — без 's', а старый SQL в проекте
        /// ссылается на 'MuscleGroups'.</summary>
        public async Task<string?> GetPrimaryMuscleNameAsync(int exerciseId)
        {
            await EnsureInitializedAsync();

            // 1) Ищем связь Primary (Role=0). Если её нет — берём любую первую.
            var emg = await _connection.Table<ExerciseMuscleGroup>()
                .Where(x => x.ExerciseId == exerciseId)
                .OrderBy(x => x.Role)
                .FirstOrDefaultAsync();

            // 2) Fallback: на старых упражнениях связи могло не быть — тогда
            // берём PrimaryMuscleGroupId из самого Exercise.
            int? muscleId = emg?.MuscleGroupId;
            if (muscleId == null || muscleId == 0)
            {
                var ex = await _connection.FindAsync<Exercise>(exerciseId);
                if (ex == null || ex.PrimaryMuscleGroupId == 0) return null;
                muscleId = ex.PrimaryMuscleGroupId;
            }

            var mg = await _connection.FindAsync<MuscleGroup>(muscleId.Value);
            return mg?.Name;
        }

        public async Task<int> SaveExerciseAsync(Exercise exercise)
        {
            await EnsureInitializedAsync();
            if (exercise.Id != 0)
                return await _connection.UpdateAsync(exercise);
            else
                return await _connection.InsertAsync(exercise);
        }

        // --- Тренировки ---

        public async Task<List<Workout>> GetWorkouts()
        {
            await EnsureInitializedAsync();
            return await _connection.Table<Workout>().ToListAsync();
        }

        public async Task<Workout> GetItemAsync(int id)
        {
            await EnsureInitializedAsync();
            return await _connection.Table<Workout>().Where(i => i.Id == id).FirstOrDefaultAsync();
        }

        public async Task<int> SaveWorkout(Workout workout)
        {
            await EnsureInitializedAsync();
            if (workout.Id == 0)
                return await _connection.InsertAsync(workout);
            else
                return await _connection.UpdateAsync(workout);
        }

        public async Task<int> DeleteWorkout(Workout workout)
        {
            await EnsureInitializedAsync();
            return await _connection.DeleteAsync(workout);
        }

        public async Task<List<Workout>> GetWorkoutsByMuscleGroupAsync(int muscleGroupId)
        {
            await EnsureInitializedAsync();
            return await _connection.QueryAsync<Workout>(@"
                SELECT w.*
                FROM Workouts w
                INNER JOIN WorkoutMuscleGroups wmg ON w.Id = wmg.workout_id
                WHERE wmg.muscle_group_id = ?
                ORDER BY w.StartTime DESC", muscleGroupId);
        }

        // --- Группы мышц ---

        public async Task<List<MuscleGroup>> GetMuscleGroupsAsync()
        {
            await EnsureInitializedAsync();
            return await _connection.Table<MuscleGroup>().ToListAsync();
        }

        public async Task<MuscleGroup> GetMuscleGroupAsync(int id)
        {
            await EnsureInitializedAsync();
            return await _connection.Table<MuscleGroup>().Where(m => m.Id == id).FirstOrDefaultAsync();
        }

        public async Task<int> SaveMuscleGroupAsync(MuscleGroup muscleGroup)
        {
            await EnsureInitializedAsync();
            if (muscleGroup.Id == 0)
                return await _connection.InsertAsync(muscleGroup);
            else
                return await _connection.UpdateAsync(muscleGroup);
        }

        public async Task<int> DeleteMuscleGroupAsync(MuscleGroup muscleGroup)
        {
            await EnsureInitializedAsync();
            return await _connection.DeleteAsync(muscleGroup);
        }

        public async Task<List<MuscleGroup>> GetMuscleGroupsForWorkoutAsync(int workoutId)
        {
            await EnsureInitializedAsync();
            return await _connection.QueryAsync<MuscleGroup>(@"
                SELECT mg.*
                FROM MuscleGroups mg
                INNER JOIN WorkoutMuscleGroups wmg ON mg.Id = wmg.MuscleGroupId
                WHERE wmg.WorkoutId = ?", workoutId);
        }

        // --- Связка тренировки и групп мышц ---

        public async Task<List<WorkoutMuscleGroup>> GetWorkoutMuscleGroupsAsync()
        {
            await EnsureInitializedAsync();
            return await _connection.Table<WorkoutMuscleGroup>().ToListAsync();
        }

        public async Task<List<WorkoutMuscleGroup>> GetWorkoutMuscleGroupsForWorkoutAsync(int workoutId)
        {
            await EnsureInitializedAsync();
            return await _connection.Table<WorkoutMuscleGroup>()
                .Where(wm => wm.WorkoutId == workoutId)
                .ToListAsync();
        }

        public async Task<int> SaveWorkoutMuscleGroupAsync(WorkoutMuscleGroup workoutMuscleGroup)
        {
            await EnsureInitializedAsync();
            if (workoutMuscleGroup.Id == 0)
                return await _connection.InsertAsync(workoutMuscleGroup);
            else
                return await _connection.UpdateAsync(workoutMuscleGroup);
        }

        public async Task<int> DeleteWorkoutMuscleGroupAsync(WorkoutMuscleGroup workoutMuscleGroup)
        {
            await EnsureInitializedAsync();
            return await _connection.DeleteAsync(workoutMuscleGroup);
        }

        // --- Упражнения в тренировке ---

        public async Task<List<WorkoutExercise>> GetExercisesForWorkoutAsync(int workoutId)
        {
            await EnsureInitializedAsync();

            var workoutExercises = await _connection.Table<WorkoutExercise>()
                .Where(x => x.WorkoutId == workoutId)
                .OrderBy(x => x.OrderIndex)
                .ToListAsync();

            // Ручной JOIN — sqlite-net-pcl не поддерживает навигационные свойства
            foreach (var item in workoutExercises)
            {
                var exerciseDef = await _connection.FindWithQueryAsync<Exercise>(
                    "SELECT * FROM Exercises WHERE Id = ?", item.ExerciseId);
                if (exerciseDef != null)
                    item.ExerciseName = exerciseDef.Name;

                item.Sets = await _connection.Table<ExerciseSet>()
                    .Where(s => s.WorkoutExerciseId == item.Id)
                    .OrderBy(s => s.SetNumber)
                    .ToListAsync();
                ExerciseSet.RecomputeBadges(item.Sets);
            }

            return workoutExercises;
        }

        public async Task<int> AddExerciseToWorkoutAsync(WorkoutExercise item)
        {
            await EnsureInitializedAsync();
            return await _connection.InsertAsync(item);
        }

        public async Task DeleteWorkoutExerciseAsync(WorkoutExercise we)
        {
            await EnsureInitializedAsync();
            await _connection.DeleteAsync(we);
        }

        // --- Подходы (сеты) ---

        public async Task<List<ExerciseSet>> GetSetsForWorkoutExerciseAsync(int workoutExerciseId)
        {
            await EnsureInitializedAsync();
            return await _connection.Table<ExerciseSet>()
                .Where(s => s.WorkoutExerciseId == workoutExerciseId)
                .OrderBy(s => s.SetNumber)
                .ToListAsync();
        }

        public async Task<int> SaveSetAsync(ExerciseSet set)
        {
            await EnsureInitializedAsync();
            if (set.Id != 0)
                return await _connection.UpdateAsync(set);
            else
                return await _connection.InsertAsync(set);
        }

        public async Task<int> AddSetAsync(ExerciseSet set)
        {
            await EnsureInitializedAsync();
            return await _connection.InsertAsync(set);
        }

        public async Task<int> DeleteSetAsync(ExerciseSet set)
        {
            await EnsureInitializedAsync();
            return await _connection.DeleteAsync(set);
        }

        public async Task AddSetToWorkoutExerciseAsync(int workoutExerciseId, double weight, int reps, double rpe)
        {
            await EnsureInitializedAsync();

            var existing = await GetSetsForWorkoutExerciseAsync(workoutExerciseId);
            var set = new ExerciseSet
            {
                WorkoutExerciseId = workoutExerciseId,
                SetNumber = existing.Count + 1,
                Weight = weight,
                Reps = reps,
                RPE = rpe
            };

            await _connection.InsertAsync(set);
        }

        // --- История подходов по упражнению (для AI) ---

        public async Task<List<ExerciseSet>> GetSetHistoryForExerciseAsync(int exerciseId, int limit = 30)
        {
            await EnsureInitializedAsync();
            var query = @"
                SELECT es.* FROM ExerciseSets es
                JOIN WorkoutExercises we ON es.WorkoutExerciseId = we.Id
                JOIN Workouts w ON we.WorkoutId = w.id
                WHERE we.ExerciseId = ?
                ORDER BY w.StartTime DESC
                LIMIT ?";
            return await _connection.QueryAsync<ExerciseSet>(query, exerciseId, limit);
        }

        // --- История на уровне ТРЕНИРОВКИ для ML-сервиса ---

        /// <summary>
        /// Возвращает агрегированную историю упражнения у пользователя: одна
        /// строка на тренировку с готовыми статистиками (top 1RM по Эпли,
        /// средние RPE, повторения и т.д.). Сортировка ASC по дате — удобно
        /// для построения лагов и наклонов на стороне ML-сервиса.
        /// Исключаются разминочные сеты (Kind = 1 = SetType.Warmup), чтобы
        /// фичи совпадали с теми, на которых обучалась модель.
        /// </summary>
        public async Task<List<ExerciseWorkoutHistoryRow>> GetExerciseWorkoutHistoryAsync(
            int userId, int exerciseId, int limit = 30)
        {
            await EnsureInitializedAsync();
            const string sql = @"
                SELECT w.StartTime as Date,
                       MAX(es.Weight * (1.0 + es.Reps / 30.0)) as TopEpley1Rm,
                       MAX(es.Weight) as TopWeight,
                       COUNT(es.Id) as NSets,
                       AVG(es.RPE) as AvgRpe,
                       MIN(es.RPE) as MinRpe,
                       MAX(es.RPE) as MaxRpe,
                       AVG(CAST(es.Reps AS REAL)) as AvgReps
                FROM ExerciseSets es
                JOIN WorkoutExercises we ON es.WorkoutExerciseId = we.Id
                JOIN Workouts w ON we.WorkoutId = w.id
                WHERE we.ExerciseId = ?
                  AND w.UserId = ?
                  AND es.Kind <> 1     -- исключаем разминочные сеты
                  AND es.Weight > 0    -- незаполненные подходы пропускаем
                GROUP BY w.id
                ORDER BY w.StartTime ASC
                LIMIT ?";
            return await _connection.QueryAsync<ExerciseWorkoutHistoryRow>(
                sql, exerciseId, userId, limit);
        }

        // --- AI предсказания ---

        public async Task SavePredictionAsync(AIPrediction prediction)
        {
            await EnsureInitializedAsync();
            prediction.CreatedAt = DateTime.UtcNow;
            var existing = await _connection.Table<AIPrediction>()
                .Where(p => p.ExerciseId == prediction.ExerciseId)
                .FirstOrDefaultAsync();
            if (existing != null)
            {
                prediction.Id = existing.Id;
                await _connection.UpdateAsync(prediction);
            }
            else
            {
                await _connection.InsertAsync(prediction);
            }
        }

        public async Task<AIPrediction?> GetPredictionAsync(int exerciseId)
        {
            await EnsureInitializedAsync();
            return await _connection.Table<AIPrediction>()
                .Where(p => p.ExerciseId == exerciseId)
                .FirstOrDefaultAsync();
        }
    }
}
