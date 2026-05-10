using System;
using System.Globalization;
using System.Windows.Data;

namespace ClosetApp.UI.Converters;

public class FavoriteLevelToStarsConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length >= 2 && values[0] is int level && values[1] is bool isFavorite)
        {
            if (!isFavorite || level == 0)
                return "☆☆☆";
            return level switch
            {
                1 => "★☆☆",
                2 => "★★☆",
                3 => "★★★",
                _ => "☆☆☆"
            };
        }
        return "☆☆☆";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}