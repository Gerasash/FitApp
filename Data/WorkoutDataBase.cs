using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

            await _connection.CreateTableAsync<Workout>();
            await _connection.CreateTableAsync<Exercise>();
            await _connection.CreateTableAsync<MuscleGroup>();
            await _connection.CreateTableAsync<WorkoutMuscleGroup>();
            await _connection.CreateTableAsync<WorkoutExercise>();
            await _connection.CreateTableAsync<ExerciseSet>();
            await _connection.CreateTableAsync<AIPrediction>();

            _initialized = true;
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
