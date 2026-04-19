using System.Globalization;

namespace FitApp.Converters;

public class WeightToHeightConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double weight)
            return Math.Max(10, weight * 0.8); // масштаб для графика
        return 10;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}