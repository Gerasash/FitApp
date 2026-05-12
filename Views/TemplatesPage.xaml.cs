using FitApp.Data;
using FitApp.Models;
using FitApp.ViewModels;

namespace FitApp.Views;

public partial class TemplatesPage : ContentPage
{
    private readonly WorkoutDataBase _database;
    private readonly TemplatesViewModel _viewModel;

    public TemplatesPage(WorkoutDataBase database)
    {
        InitializeComponent();
        _database = database;
        _viewModel = new TemplatesViewModel(database);
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Чтобы счётчики, новые/изменённые шаблоны подтягивались при возврате
        await _viewModel.LoadAsync();
    }

    private async void OnTemplateTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not TemplateCard card) return;
        var page = new TemplateEditorPage(_database);
        page.Init(card.Template);
        await Navigation.PushAsync(page);
    }

    private async void OnNewTemplateClicked(object sender, EventArgs e)
    {
        var name = await DisplayPromptAsync("Новый шаблон", "Название:", "Создать", "Отмена",
            placeholder: "Например, My Push Day");
        if (string.IsNullOrWhiteSpace(name)) return;

        var template = new WorkoutTemplate
        {
            Name = name.Trim(),
            FolderName = "Мои",
            IsBuiltIn = false,
            CreatedAt = DateTime.UtcNow
        };
        await _database.SaveTemplateAsync(template);

        var page = new TemplateEditorPage(_database);
        page.Init(template);
        await Navigation.PushAsync(page);
    }
}
