using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;
namespace FitApp.Models
{
    public class ToDoItem
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Title { get; set; } = "New ToDoItem";
        public bool IsCompleted { get; set; } = false;

    }
}
