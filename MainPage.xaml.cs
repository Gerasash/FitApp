//Файл MainPage
using SQLite;
using System.Collections.ObjectModel;
using System.Security.AccessControl;
using Microsoft.Maui.Controls;
using System.Reflection;
using System.Diagnostics;
using FitApp.Models;
using FitApp.ViewModels;
using FitApp.Data;
//using HealthKit;

namespace FitApp
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        // ✅ ЕДИНЫЙ стиль навигации (Shell)
        private async void GoToWorkoutList(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//WorkoutListPage");
        }

        private async void GoToMuscleGroupsListPage(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//MuscleGroupsListPage");
        }

        private async void GoExercisePage(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//ExercisePage");
        }
    }
}