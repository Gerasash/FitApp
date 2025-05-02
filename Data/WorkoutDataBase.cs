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
        private const string DB_NAME = "Workout.db";
        private readonly SQLiteAsyncConnection _connection;

        public WorkoutDataBase()
        {
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, DB_NAME);
            _connection = new SQLiteAsyncConnection(dbPath);

            _connection.CreateTableAsync<Workout>().Wait();
            //_connection.CreateTableAsync<MuscleGroup>().Wait();
            //_connection.CreateTableAsync<WorkoutMuscleGroup>().Wait();
        }

        public Task<List<Workout>> GetWorkouts()
        {
            return _connection.Table<Workout>().ToListAsync();
        }
        public Task<int> SaveWorkout(Workout Workout)
        {
            if (Workout.Id == 0)
                return _connection.InsertAsync(Workout);
            else
                return _connection.UpdateAsync(Workout);
        }
        public Task<int> DeleteWorkout(Workout Workout)
        {
            return _connection.DeleteAsync(Workout);
        }
    }
}
