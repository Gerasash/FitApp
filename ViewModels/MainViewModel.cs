using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitApp.Data;
using FitApp.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
namespace FitApp.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly ToDoDataBase _database;

        [ObservableProperty]
        private ObservableCollection<ToDoItem> toDoItems;

        [ObservableProperty]
        private string newTaskTitle;
        public MainViewModel()
        {
            _database = new ToDoDataBase();
            LoadItems();
        }

        [RelayCommand]
        private async void LoadItems()
        {
            var items = await _database.GetItems();
            ToDoItems = new ObservableCollection<ToDoItem>(items);
        }

        [RelayCommand]
        private async void AddItem()
        {
            if (!string.IsNullOrEmpty(NewTaskTitle))
            {
                var newItem = new ToDoItem { Title = NewTaskTitle, IsCompleted = false };
                await _database.SaveItem(newItem);
                NewTaskTitle = "";
                LoadItems();
            }
        }
        [RelayCommand]
        private async void DeleteItem(ToDoItem item)
        {
            await _database.DeleteItem(item);
            LoadItems();
        }
    }
}
