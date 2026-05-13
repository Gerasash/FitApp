using CommunityToolkit.Mvvm.ComponentModel;

namespace FitApp.ViewModels;

// Один пункт RPE-шкалы в bottom sheet
public partial class RpeOption : ObservableObject
{
    public double Value { get; }
    public string Label { get; }

    [ObservableProperty] private bool isSelected;

    public RpeOption(double value, string label)
    {
        Value = value;
        Label = label;
    }
}
