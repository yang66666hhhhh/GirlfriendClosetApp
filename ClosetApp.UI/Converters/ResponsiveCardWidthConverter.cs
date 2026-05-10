using System;
using System.Globalization;
using System.Windows.Data;

namespace ClosetApp.UI.Converters;

public class ResponsiveCardWidthConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length == 0 || values[0] == null)
            return 280.0;

        double containerWidth = (double)values[0];
        if (containerWidth <= 0)
            return 280.0;

        int columns = CalculateColumns(containerWidth);
        double gap = 20.0;
        double availableWidth = containerWidth - (gap * (columns - 1));
        double cardWidth = Math.Floor(availableWidth / columns);

        return Math.Max(cardWidth, 200.0);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    private static int CalculateColumns(double containerWidth)
    {
        if (containerWidth >= 1400) return 5;
        if (containerWidth >= 1200) return 4;
        if (containerWidth >= 900) return 3;
        if (containerWidth >= 600) return 2;
        return 1;
    }
}

public class WindowWidthToColumnCountConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double width)
        {
            if (width >= 1400) return 5;
            if (width >= 1200) return 4;
            if (width >= 900) return 3;
            if (width >= 600) return 2;
            return 1;
        }
        return 4;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
