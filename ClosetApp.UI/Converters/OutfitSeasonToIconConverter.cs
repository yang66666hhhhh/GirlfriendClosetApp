using System.Globalization;
using System.Windows.Data;
using ClosetApp.Domain.Enums;

namespace ClosetApp.UI.Converters;

public class OutfitSeasonToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Season season)
        {
            return season switch
            {
                Season.Spring => "M17 8c.7 0 1.38.1 2 .29V5c0-1.1-.9-2-2-2H7c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h5.68c-.43-.91-.68-1.93-.68-3 0-3.87 3.13-7 7-7z",
                Season.Summer => "M12 7c-2.76 0-5 2.24-5 5s2.24 5 5 5 5-2.24 5-5-2.24-5-5-5zM2 13h2c.55 0 1-.45 1-1s-.45-1-1-1H2c-.55 0-1 .45-1 1s.45 1 1 1zm18 0h2c.55 0 1-.45 1-1s-.45-1-1-1h-2c-.55 0-1 .45-1 1s.45 1 1 1z",
                Season.Autumn => "M6 19c0 1.1.9 2 2 2h8c1.1 0 2-.9 2-2v-7H6v7zM8 9h8v1H8V9zm7-4l-1.5 2H10l-1.5 2h9L15 5z",
                Season.Winter => "M22 11h-4.17l3.24-3.24-1.41-1.41L15 11h-2V9l4.66-4.66-1.41-1.41L13 6.17V2h-2v4.17L7.76 2.93 6.34 4.34 11 9v2H9L4.34 6.34 2.93 7.76 6.17 11H2v2h4.17l-3.24 3.24 1.41 1.41L9 13h2v2l-4.66 4.66 1.41 1.41L11 17.83V22h2v-4.17l3.24 3.24 1.41-1.41L13 15v-2h2l4.66 4.66 1.41-1.41L17.83 13H22z",
                Season.AllSeason => "M12 3c-4.97 0-9 4.03-9 9s4.03 9 9 9 9-4.03 9-9-4.03-9-9-9z",
                _ => "M12 3c-4.97 0-9 4.03-9 9s4.03 9 9 9 9-4.03 9-9-4.03-9-9-9z"
            };
        }
        return "M12 3c-4.97 0-9 4.03-9 9s4.03 9 9 9 9-4.03 9-9-4.03-9-9-9z";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}