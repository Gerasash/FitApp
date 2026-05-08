using Microsoft.Maui.Controls;
using System.Globalization;

namespace FitApp.Converters
{
    public class NullOrEmptyConverter : IValueConverter
    {
        public static readonly NullOrEmptyConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null || (value is System.Collections.IEnumerable list && !list.Cast<object>().Any());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
