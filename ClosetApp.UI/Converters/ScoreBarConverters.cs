using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ClosetApp.UI.Converters;

public class ScoreToColorConverter : IValueConverter
{
    private static readonly Brush PositiveBrush = new SolidColorBrush(Color.FromRgb(0x58, 0x81, 0xD6));
    private static readonly Brush NegativeBrush = new SolidColorBrush(Color.FromRgb(0xD6, 0x58, 0x58));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not int score)
            return Brushes.Gray;

        return score >= 0 ? PositiveBrush : NegativeBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class ScoreToSignConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not int score)
            return "";
        return score >= 0 ? "+" : "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
