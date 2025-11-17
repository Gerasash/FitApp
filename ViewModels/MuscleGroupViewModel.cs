using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitApp.Data;
using FitApp.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FitApp;
namespace FitApp.ViewModels
{
    public partial class MuscleGroupViewModel : ObservableObject
    {
        private readonly WorkoutDataBase _database;
        [ObservableProperty]
        public ObservableCollection<MuscleGroup> _muscleGroups = new ();
        [ObservableProperty]
        private MuscleGroup _selectedMuscleGroup = new MuscleGroup();
        
        [ObservableProperty]
        private string _muscleGroupName;

        public MuscleGroupViewModel(WorkoutDataBase database)
        {
            _database = database;
            LoadMuscleGroupsAsync();
        }

        public async Task LoadMuscleGroupsAsync()
        {
            MuscleGroups.Clear();
            var muscleGroups = await _database.GetMuscleGroupsAsync();
            foreach (var group in muscleGroups)
            {
                MuscleGroups.Add(group);
            }
        }

        [RelayCommand]
        private async Task AddMuscleGroup()
        {
            if (!string.IsNullOrWhiteSpace(MuscleGroupName))
            {
                var newGroup = new MuscleGroup { Name = MuscleGroupName };
                await _database.SaveMuscleGroupAsync(newGroup);
                MuscleGroups.Add(newGroup);
                MuscleGroupName = string.Empty;
                await LoadMuscleGroupsAsync();
            }
        }

        [RelayCommand]
        private async Task DeleteMuscleGroup(MuscleGroup muscleGroup)
        {
            if (muscleGroup != null)
            {
                await _database.DeleteMuscleGroupAsync(muscleGroup);
                MuscleGroups.Remove(muscleGroup);
            }
            await LoadMuscleGroupsAsync();
        }
    }
}
