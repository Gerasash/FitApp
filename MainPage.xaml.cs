//Файл MainPage
using SQLite;
using System.Collections.ObjectModel;
using System.Security.AccessControl;
using Microsoft.Maui.Controls;
using System.Reflection;
using System.Diagnostics;
using FitApp.Models;
using FitApp.ViewModels;
//using HealthKit;

namespace FitApp
{
    public partial class MainPage : ContentPage
    {
        private readonly LocalDBService _dbService;
        private int _editUserId = 0;
        // Коллекция для хранения тренировок
        public ObservableCollection<Workout> Workouts { get; set; } = new ObservableCollection<Workout>();

        public MainPage(LocalDBService dbService)
        {
            InitializeComponent();
            //BindingContext = new MainViewModel();

            _dbService = dbService;
            Task.Run(async () => listView.ItemsSource = await _dbService.GetUser());
            
            // Привязка данных к ListView
            WorkoutListView.ItemsSource = Workouts;
            // Добавляем обработчик события выбора тренировки
            WorkoutListView.ItemSelected += OnWorkoutSelected;
        }

        private async void saveButton_Clicked(object sender, EventArgs e)
        {
            //Если одно из полей пустое то 
            if (string.IsNullOrEmpty(nameEntryField.Text) &&
                string.IsNullOrEmpty(emailEntryField.Text) &&
                string.IsNullOrEmpty(mobileEntryField.Text))
            {
                await DisplayAlert("Alert", "Пустые поля", "OK");
            }
            else
            {
                if (_editUserId == 0)
                {
                    //add user
                    await _dbService.Create(new User
                    {
                        UserName = nameEntryField.Text,
                        Email = emailEntryField.Text,
                        Mobile = mobileEntryField.Text
                    });
                    listView.ItemsSource = await _dbService.GetUser();
                }
                else
                {
                    //edit user
                    await _dbService.Update(new User
                    {
                        Id = _editUserId,
                        UserName = nameEntryField.Text,
                        Email = emailEntryField.Text,
                        Mobile = mobileEntryField.Text
                    });

                    _editUserId = 0;
                    nameEntryField.Text = string.Empty;
                    emailEntryField.Text = string.Empty;
                    mobileEntryField.Text = string.Empty;
                    listView.ItemsSource = await _dbService.GetUser();

                }
            }
        }

        private async void listView_ItemTapped(object sender, ItemTappedEventArgs e)
        {
            var user = (User)e.Item;
            var action = await DisplayActionSheet("Action", "Cancel", null, "Edit", "Delete");
            switch (action)
            {
                case "Edit":
                    _editUserId = user.Id;
                    nameEntryField.Text = user.UserName;
                    emailEntryField.Text = user.Email;
                    mobileEntryField.Text = user.Mobile;
                    break;
                case "Delete":
                    await _dbService.Delete(user);
                    listView.ItemsSource = await _dbService.GetUser();
                    break;
            }
        }
        // Обработчик нажатия на кнопку "Добавить тренировку"

        private void OnAddWorkoutClicked(object sender, System.EventArgs e)
        {
            DateTime selectedDate = WorkoutDatePicker.Date;
            TimeSpan selectedTime = WorkoutTimePicker.Time;
            selectedDate += selectedTime;

            string workoutName = WorkoutNameEntry.Text;
            /*if (workoutName == null) workoutName = "";*/

            // Добавляем новую тренировку в коллекцию
            Workouts.Add(new Workout(workoutName, selectedDate));

            // Очищаем поле ввода
            WorkoutNameEntry.Text = string.Empty;

        }

        // Обработчик события выбора тренировки
        private async void OnWorkoutSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem == null)
                return;

            var selectedWorkout = e.SelectedItem as Workout;

            await Navigation.PushAsync(new WorkoutPage(selectedWorkout));

            WorkoutListView.SelectedItem = null;
        }
        
        private async void OnDeleteWorkoutClicked(object sender, EventArgs e)
        {
            // Получаем выбранную тренировку из CommandParameter
            var menuItem = sender as MenuItem;
            var workout = menuItem.CommandParameter as Workout;

            if (workout == null)
                return;

            // Запрос подтверждения удаления
            bool result = await DisplayAlert("Удаление", $"Удалить тренировку '{workout.Name}'?", "Да", "Нет");

            if (result)
            {
                Workouts.Remove(workout);
            }
        }

        private async void GoToTaskList(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new TaskListPage());
        }
    }
}