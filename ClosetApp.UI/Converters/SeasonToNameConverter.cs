using System.Globalization;
using System.Windows.Data;
using ClosetApp.Domain.Enums;

namespace ClosetApp.UI.Converters;

public class SeasonToNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Season season)
        {
            return season switch
            {
                Season.Unspecified => "待设置",
                Season.Spring => "春",
                Season.Summer => "夏",
                Season.Autumn => "秋",
                Season.Winter => "冬",
                Season.AllSeason => "四季",
                _ => "未知"
            };
        }
        return "未知";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
