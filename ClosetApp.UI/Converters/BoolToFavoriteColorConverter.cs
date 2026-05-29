using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ClosetApp.UI.Converters;

public class BoolToFavoriteColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isFavorite && isFavorite)
        {
            return new SolidColorBrush(Color.FromRgb(232, 141, 141));
        }
        return new SolidColorBrush(Color.FromRgb(180, 180, 180));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return ConverterBackResult.DoNothing;
    }
}
