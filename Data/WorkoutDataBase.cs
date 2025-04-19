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
        }

        public Task<List<Workout>> GetItems()
        {
            return _connection.Table<Workout>().ToListAsync();
        }
        public Task<int> SaveItem(Workout item)
        {
            if (item.Id == 0)
                return _connection.InsertAsync(item);
            else
                return _connection.UpdateAsync(item);
        }
        public Task<int> DeleteItem(Workout item)
        {
            return _connection.DeleteAsync(item);
        }
    }
}
