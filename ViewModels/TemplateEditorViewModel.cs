using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitApp.Data;
using FitApp.Models;
using System.Collections.ObjectModel;

namespace FitApp.ViewModels;

// Обёртка над TemplateExercise чтобы инлайн-редактируемые поля были observable
public partial class TemplateExerciseRow : ObservableObject
{
    public TemplateExercise Source { get; }
    public string ExerciseName => Source.ExerciseName ?? "?";

    [ObservableProperty] private int targetSets;
    [ObservableProperty] private int? repsMin;
    [ObservableProperty] private int? repsMax;
    [ObservableProperty] private int? restSeconds;

    public TemplateExerciseRow(TemplateExercise src)
    {
        Source = src;
        TargetSets = src.TargetSets;
        RepsMin = src.RepsMin;
        RepsMax = src.RepsMax;
        RestSeconds = src.RestSeconds;
    }

    // Записываем обратно перед сохранением
    public void CommitToSource()
    {
        Source.TargetSets = TargetSets > 0 ? TargetSets : 1;
        Source.RepsMin = RepsMin;
        Source.RepsMax = RepsMax;
        Source.RestSeconds = RestSeconds;
    }
}

public partial class TemplateEditorViewModel : ObservableObject
{
    private readonly WorkoutDataBase _database;
    public WorkoutTemplate Template { get; private set; }

    [ObservableProperty] private string name = "";
    [ObservableProperty] private string folder = "Мои";
    [ObservableProperty] private string description = "";
    [ObservableProperty] private ObservableCollection<TemplateExerciseRow> rows = new();

    public bool IsBuiltIn => Template?.IsBuiltIn == true;

    public TemplateEditorViewModel(WorkoutDataBase database, WorkoutTemplate template)
    {
        _database = database;
        Template = template;
        Name = template.Name;
        Folder = template.FolderName ?? "Мои";
        Description = template.Description ?? "";
        _ = LoadAsync();
    }

    public async Task LoadAsync()
    {
        var list = await _database.GetTemplateExercisesAsync(Template.Id);
        Rows = new ObservableCollection<TemplateExerciseRow>(list.Select(te => new TemplateExerciseRow(te)));
    }

    [RelayCommand]
    private async Task Save()
    {
        Template.Name = string.IsNullOrWhiteSpace(Name) ? "Без названия" : Name.Trim();
        Template.FolderName = string.IsNullOrWhiteSpace(Folder) ? "Мои" : Folder.Trim();
        Template.Description = Description;
        await _database.SaveTemplateAsync(Template);

        foreach (var r in Rows)
        {
            r.CommitToSource();
            await _database.AddTemplateExerciseAsync(r.Source);
        }

        await Shell.Current.DisplayAlert("Сохранено", "Шаблон обновлён", "OK");
    }

    [RelayCommand]
    private async Task DeleteRow(TemplateExerciseRow row)
    {
        if (row == null) return;
        await _database.DeleteTemplateExerciseAsync(row.Source);
        Rows.Remove(row);
        // Пересчитаем OrderIndex у оставшихся, чтобы порядок был чистым
        int i = 0;
        foreach (var r in Rows)
        {
            r.Source.OrderIndex = i++;
            await _database.AddTemplateExerciseAsync(r.Source);
        }
    }

    [RelayCommand]
    private async Task MoveUp(TemplateExerciseRow row)
    {
        if (row == null) return;
        var idx = Rows.IndexOf(row);
        if (idx <= 0) return;
        Rows.Move(idx, idx - 1);
        await PersistOrderAsync();
    }

    [RelayCommand]
    private async Task MoveDown(TemplateExerciseRow row)
    {
        if (row == null) return;
        var idx = Rows.IndexOf(row);
        if (idx < 0 || idx >= Rows.Count - 1) return;
        Rows.Move(idx, idx + 1);
        await PersistOrderAsync();
    }

    private async Task PersistOrderAsync()
    {
        int i = 0;
        foreach (var r in Rows)
        {
            r.Source.OrderIndex = i++;
            await _database.AddTemplateExerciseAsync(r.Source);
        }
    }

    // Добавление упражнения извне (после возврата из ExercisePage)
    public async Task AddExerciseAsync(Exercise exercise)
    {
        var te = new TemplateExercise
        {
            TemplateId = Template.Id,
            ExerciseId = exercise.Id,
            OrderIndex = Rows.Count,
            TargetSets = 3,
            RepsMin = 8,
            RepsMax = 12,
            ExerciseName = exercise.Name
        };
        await _database.AddTemplateExerciseAsync(te);
        Rows.Add(new TemplateExerciseRow(te));
    }
}
