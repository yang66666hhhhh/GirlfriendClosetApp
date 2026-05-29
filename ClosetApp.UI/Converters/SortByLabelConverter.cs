using System.Globalization;
using System.Windows.Data;
using ClosetApp.UI.Logic.States;

namespace ClosetApp.UI.Converters;

public class SortByLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is WardrobeSortBy sort)
        {
            return sort switch
            {
                WardrobeSortBy.Newest => "最新添加",
                WardrobeSortBy.Oldest => "最早添加",
                WardrobeSortBy.Name => "名称",
                WardrobeSortBy.Brand => "品牌",
                WardrobeSortBy.Type => "分类",
                WardrobeSortBy.FavoriteLevel => "收藏度",
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
