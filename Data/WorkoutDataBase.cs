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

        public WorkoutDataBase()
        {
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, DbName);
            _connection = new SQLiteAsyncConnection(dbPath);

            _connection.CreateTableAsync<Workout>().Wait();
            _connection.CreateTableAsync<MuscleGroup>().Wait();
            _connection.CreateTableAsync<WorkoutMuscleGroup>().Wait();


            _connection.CreateTableAsync<Exercise>().Wait();
            _connection.CreateTableAsync<WorkoutExercise>().Wait();
            _connection.CreateTableAsync<ExerciseSet>().Wait();


        }
        public Task<List<Exercise>> GetExercisesAsync()
        {
            return _connection.Table<Exercise>().ToListAsync();
        }

        // Поиск упражнений (для SearchBar)
        public Task<List<Exercise>> SearchExercisesAsync(string searchText)
        {
            return _connection.Table<Exercise>()
                            .Where(x => x.Name.ToLower().Contains(searchText.ToLower()))
                            .ToListAsync();
        }

        public Task<int> SaveExerciseAsync(Exercise exercise)
        {
            if (exercise.Id != 0)
                return _connection.UpdateAsync(exercise);
            else
                return _connection.InsertAsync(exercise);
        }

        // --- Методы для WorkoutExercise (Связка тренировки и упражнения) ---

        public async Task<List<WorkoutExercise>> GetExercisesForWorkoutAsync(int workoutId)
        {
            // Получаем связки
            var workoutExercises = await _connection.Table<WorkoutExercise>()
                                                  .Where(x => x.WorkoutId == workoutId)
                                                  .OrderBy(x => x.OrderIndex)
                                                  .ToListAsync();

            // Для каждой связки подгружаем название упражнения и сеты
            // (Это "ручной" JOIN, так как SQLite-net простой)
            foreach (var item in workoutExercises)
            {
                var exerciseDef = await _connection.FindWithQueryAsync<Exercise>("SELECT * FROM Exercises WHERE Id = ?", item.ExerciseId);
                if (exerciseDef != null)
                    item.ExerciseName = exerciseDef.Name;

                item.Sets = await _connection.Table<ExerciseSet>()
                                           .Where(s => s.WorkoutExerciseId == item.Id)
                                           .OrderBy(s => s.SetNumber)
                                           .ToListAsync();
            }

            return workoutExercises;
        }

        public Task<int> AddExerciseToWorkoutAsync(WorkoutExercise item)
        {
            return _connection.InsertAsync(item);
        }

        // --- Методы для Sets (Подходы) ---

        public Task<int> SaveSetAsync(ExerciseSet set)
        {
            if (set.Id != 0)
                return _connection.UpdateAsync(set);
            else
                return _connection.InsertAsync(set);
        }

        public Task<int> DeleteSetAsync(ExerciseSet set)
        {
            return _connection.DeleteAsync(set);
        }

        // Методы для работы с таблицей Workout
        public Task<List<Workout>> GetWorkouts()
        {
            return _connection.Table<Workout>().ToListAsync();
        }
        public  Task<Workout> GetItemAsync(int id)
        {
            return _connection.Table<Workout>().Where(i => i.Id == id).FirstOrDefaultAsync();
        }
        public Task<int> SaveWorkout(Workout workout)
        {
            if (workout.Id == 0)
                return _connection.InsertAsync(workout);
            else
                return _connection.UpdateAsync(workout);
        }
        public Task<int> DeleteWorkout(Workout workout)
        {
            return _connection.DeleteAsync(workout);
        }

        // Методы для работы с таблицей MuscleGroup
        public Task<List<MuscleGroup>> GetMuscleGroupsAsync()
        {
            return _connection.Table<MuscleGroup>().ToListAsync();
        }
        public Task<MuscleGroup> GetMuscleGroupAsync(int id)
        {
            return _connection.Table<MuscleGroup>().Where(m => m.Id == id).FirstOrDefaultAsync();
        }
        public Task<int> SaveMuscleGroupAsync(MuscleGroup muscleGroup)
        {
            if (muscleGroup.Id == 0)
                return _connection.InsertAsync(muscleGroup);
            else
                return _connection.UpdateAsync(muscleGroup);
        }
        public Task<int> DeleteMuscleGroupAsync(MuscleGroup muscleGroup)
        {
            return _connection.DeleteAsync(muscleGroup);
        }

        // Методы для работы с таблицей WorkoutMuscleGroup
        public Task<List<WorkoutMuscleGroup>> GetWorkoutMuscleGroupsAsync()
        {
            return _connection.Table<WorkoutMuscleGroup>().ToListAsync();
        }
        public Task<List<WorkoutMuscleGroup>> GetWorkoutMuscleGroupsForWorkoutAsync(int workoutId)
        {
            return _connection.Table<WorkoutMuscleGroup>().Where(wm => wm.WorkoutId == workoutId).ToListAsync();
        }
        public Task<int> SaveWorkoutMuscleGroupAsync(WorkoutMuscleGroup workoutMuscleGroup)
        {
            if (workoutMuscleGroup.Id == 0)
                return _connection.InsertAsync(workoutMuscleGroup);
            else
                return _connection.UpdateAsync(workoutMuscleGroup);
        }
        public Task<int> DeleteWorkoutMuscleGroupAsync(WorkoutMuscleGroup workoutMuscleGroup)
        {
            return _connection.DeleteAsync(workoutMuscleGroup);
        }

        // Метод для получения групп мышц, связанных с определенной тренировкой
        public Task<List<MuscleGroup>> GetMuscleGroupsForWorkoutAsync(int workoutId)
        {
            return _connection.QueryAsync<MuscleGroup>(@"
                SELECT mg.*
                FROM MuscleGroups mg
                INNER JOIN WorkoutMuscleGroups wmg ON mg.Id = wmg.MuscleGroupId
                WHERE wmg.WorkoutId = ?", workoutId);
        }

        // Методы (примеры):
        public Task<List<Exercise>> GetAllExercisesAsync()
        {
            return _connection.Table<Exercise>().ToListAsync();
        }

        public Task<int> AddSetAsync(ExerciseSet set)
        {
            return _connection.InsertAsync(set);
        }
    }
}
