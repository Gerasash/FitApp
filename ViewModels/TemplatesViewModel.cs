using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitApp.Data;
using FitApp.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace FitApp.ViewModels;

// Карточка шаблона для CollectionView
public partial class TemplateCard : ObservableObject
{
    public WorkoutTemplate Template { get; }
    public string Name => Template.Name;
    public string Folder => Template.FolderName ?? "Мои";
    public string Description => Template.Description ?? string.Empty;

    // "5 упражнений · использован 3 раза"
    public string MetaLine { get; }

    // Превью первых 3 упражнений: "Жим лёжа · Тяга штанги · Армейский жим"
    public string ExercisesPreview { get; }

    public TemplateCard(WorkoutTemplate template, IReadOnlyList<TemplateExercise> exercises)
    {
        Template = template;
        var count = exercises.Count;
        var word = count switch
        {
            1 => "упражнение",
            >= 2 and <= 4 => "упражнения",
            _ => "упражнений"
        };
        var meta = $"{count} {word}";
        if (template.TimesUsed > 0) meta += $" · использован {template.TimesUsed}×";
        if (template.LastUsedAt != null)
            meta += $" · {template.LastUsedAt.Value.ToLocalTime():dd.MM}";
        MetaLine = meta;

        var preview = exercises
            .Take(3)
            .Select(te => te.ExerciseName ?? "?")
            .ToList();
        if (exercises.Count > 3) preview.Add("…");
        ExercisesPreview = string.Join(" · ", preview);
    }
}

public partial class TemplatesViewModel : ObservableObject
{
    private readonly WorkoutDataBase _database;

    [ObservableProperty] private ObservableCollection<TemplateCard> templates = new();
    [ObservableProperty] private ObservableCollection<string> folders = new();
    [ObservableProperty] private string? selectedFolder; // null = все
    [ObservableProperty] private bool isRefreshing;       // для RefreshView pull-to-refresh

    private List<TemplateCard> _allCards = new();

    public TemplatesViewModel(WorkoutDataBase database)
    {
        _database = database;
        _ = LoadAsync();
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        try
        {
            var tpls = await _database.GetTemplatesAsync();
        var cards = new List<TemplateCard>();
        foreach (var t in tpls)
        {
            var exes = await _database.GetTemplateExercisesAsync(t.Id);
            cards.Add(new TemplateCard(t, exes));
        }
        _allCards = cards;

        var foldersList = new List<string> { "Все" };
        foldersList.AddRange(cards
            .Select(c => c.Folder)
            .Where(f => !string.IsNullOrWhiteSpace(f))
            .Distinct()
            .OrderBy(f => f));
        Folders = new ObservableCollection<string>(foldersList);

        SelectedFolder ??= "Все";
        ApplyFolder();
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private void SelectFolder(string folder)
    {
        SelectedFolder = folder;
        ApplyFolder();
    }

    private void ApplyFolder()
    {
        IEnumerable<TemplateCard> q = _allCards;
        if (!string.IsNullOrEmpty(SelectedFolder) && SelectedFolder != "Все")
            q = q.Where(c => c.Folder == SelectedFolder);

        var ordered = q
            .OrderByDescending(c => c.Template.LastUsedAt ?? DateTime.MinValue)
            .ThenBy(c => c.Template.OrderIndex)
            .ToList();
        Templates = new ObservableCollection<TemplateCard>(ordered);
    }

    [RelayCommand]
    private async Task StartWorkout(TemplateCard card)
    {
        if (card == null) return;
        var confirm = await Shell.Current.DisplayAlert(
            "Начать тренировку",
            $"Создать новую тренировку по шаблону «{card.Name}»?",
            "Да", "Отмена");
        if (!confirm) return;

        try
        {
            var workoutId = await _database.StartWorkoutFromTemplateAsync(card.Template.Id);
            await LoadAsync(); // обновить TimesUsed/LastUsedAt в UI

            // Переход на список тренировок: пусть юзер увидит созданную тренировку и откроет её
            await Shell.Current.GoToAsync("//WorkoutListPage");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Ошибка", ex.Message, "OK");
        }
    }

    [RelayCommand]
    private async Task DeleteTemplate(TemplateCard card)
    {
        if (card == null) return;
        var msg = card.Template.IsBuiltIn
            ? $"Скрыть встроенный шаблон «{card.Name}»? Его можно вернуть, удалив БД."
            : $"Удалить шаблон «{card.Name}»?";
        var confirm = await Shell.Current.DisplayAlert("Подтверждение", msg, "Да", "Отмена");
        if (!confirm) return;

        await _database.DeleteTemplateAsync(card.Template);
        await LoadAsync();
    }
}
