using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FitApp.Models;
using SQLite;
namespace FitApp.Data
{
    public class ToDoDataBase
    {
        private const string DB_NAME = "Todo";
        private readonly SQLiteAsyncConnection _connection;
        public ToDoDataBase()
        {
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, DB_NAME);
            _connection = new SQLiteAsyncConnection(dbPath);
            _connection.CreateTableAsync<ToDoItem>().Wait();
        }

        public Task<List<ToDoItem>> GetItems()
        {
            return _connection.Table<ToDoItem>().ToListAsync();
        }
        public Task<int> SaveItem(ToDoItem item)
        {
            if (item.Id == 0)
                return _connection.InsertAsync(item);
            else
                return _connection.UpdateAsync(item);
        }
        public Task<int> DeleteItem(ToDoItem item)
        {
            return _connection.DeleteAsync(item);
        }
    }
}
