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
