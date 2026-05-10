using System;
using System.Globalization;
using System.Windows.Data;
using ClosetApp.Domain.Enums;

namespace ClosetApp.UI.Converters;

public class ClothingTypeToHeightConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ClothingType type)
        {
            return type switch
            {
                ClothingType.Dress => 340,
                ClothingType.Skirt => 320,
                ClothingType.Outerwear => 320,
                ClothingType.Top => 280,
                ClothingType.Bottom => 260,
                ClothingType.Shoes => 240,
                ClothingType.Accessory => 220,
                _ => 280
            };
        }
        return 280;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}