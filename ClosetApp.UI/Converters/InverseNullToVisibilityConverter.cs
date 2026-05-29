using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ClosetApp.UI.Converters;

public class InverseNullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value == null ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return ConverterBackResult.DoNothing;
    }
}
