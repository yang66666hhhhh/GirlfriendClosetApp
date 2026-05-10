using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ClosetApp.UI.Converters;

public class ColorToBrushConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        try
        {
            if (value is string colorString && !string.IsNullOrEmpty(colorString))
            {
                if (colorString.StartsWith("#"))
                {
                    var color = (Color)ColorConverter.ConvertFromString(colorString);
                    return new SolidColorBrush(color);
                }
                return new SolidColorBrush(Colors.Gray);
            }
            return new SolidColorBrush(Colors.LightGray);
        }
        catch
        {
            return new SolidColorBrush(Colors.LightGray);
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}