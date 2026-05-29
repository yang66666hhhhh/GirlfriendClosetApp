using System.Globalization;
using System.Windows.Data;
using ClosetApp.Domain.Enums;

namespace ClosetApp.UI.Converters;

public class ClothingTypeToNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ClothingType type)
        {
            return type switch
            {
                ClothingType.Unspecified => "待分类",
                ClothingType.Top => "上衣",
                ClothingType.Bottom => "裤装",
                ClothingType.Dress => "连衣裙",
                ClothingType.Skirt => "半身裙",
                ClothingType.Outerwear => "外套",
                ClothingType.Shoes => "鞋子",
                ClothingType.Accessory => "配饰",
                _ => "未知"
            };
        }
        return "未知";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return ConverterBackResult.DoNothing;
    }
}
