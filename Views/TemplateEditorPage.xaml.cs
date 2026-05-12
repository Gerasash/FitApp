using FitApp.Data;
using FitApp.Models;
using FitApp.ViewModels;

namespace FitApp.Views;

public partial class TemplateEditorPage : ContentPage
{
    private readonly WorkoutDataBase _database;
    private TemplateEditorViewModel _viewModel = default!;

    public TemplateEditorPage(WorkoutDataBase database)
    {
        InitializeComponent();
        _database = database;
    }

    public void Init(WorkoutTemplate template)
    {
        _viewModel = new TemplateEditorViewModel(_database, template);
        BindingContext = _viewModel;
        Title = template.Id == 0 ? "Новый шаблон" : "Редактор шаблона";
    }

    private async void OnAddExerciseClicked(object sender, EventArgs e)
    {
        var page = new ExercisePage(_database);
        page.SetCallback(async exercise =>
        {
            await _viewModel.AddExerciseAsync(exercise);
            if (Navigation.ModalStack.Count > 0)
                await Navigation.PopModalAsync();
        });
        await Navigation.PushModalAsync(page);
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
