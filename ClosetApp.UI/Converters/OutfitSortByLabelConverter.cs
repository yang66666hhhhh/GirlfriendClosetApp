using System.Globalization;
using System.Windows.Data;
using ClosetApp.UI.Logic.States;

namespace ClosetApp.UI.Converters;

public class OutfitSortByLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is OutfitSortBy sort)
        {
            return sort switch
            {
                OutfitSortBy.Newest => "最新创建",
                OutfitSortBy.Oldest => "最早创建",
                OutfitSortBy.Name => "名称",
                OutfitSortBy.Rating => "评分",
                OutfitSortBy.WearCount => "穿着次数",
                OutfitSortBy.LastWorn => "最近穿着",
                _ => sort.ToString()
            };
        }
        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return ConverterBackResult.DoNothing;
    }
}
