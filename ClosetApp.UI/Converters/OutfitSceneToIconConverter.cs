using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ClosetApp.Domain.Enums;

namespace ClosetApp.UI.Converters;

public class OutfitSceneToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is OutfitScene scene)
        {
            return scene switch
            {
                OutfitScene.Work => "M20 6h-4V4c0-1.11-.89-2-2-2h-4c-1.11 0-2 .89-2 2v2H4c-1.11 0-1.99.89-1.99 2L2 19c0 1.11.89 2 2 2h16c1.11 0 2-.89 2-2V8c0-1.11-.89-2-2-2zm-6 0h-4V4h4v2z",
                OutfitScene.Date => "M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z",
                OutfitScene.Travel => "M21 16v-2l-8-5V3.5c0-.83-.67-1.5-1.5-1.5S10 2.67 10 3.5V9l-8 5v2l8-2.5V19l-2 1.5V22l3.5-1 3.5 1v-1.5L13 19v-5.5l8 2.5z",
                OutfitScene.Party => "M12 6c1.11 0 2-.9 2-2s-.89-2-2-2-2 .9-2 2 .9 2 2 2zm6 7.11V10H6V7.11L8 9v2.89l2 1V10h4v1.89l2-1V9l2-2.11zM16 18v-1.5c0-1.67-3.33-2.5-5-2.5s-5 .83-5 2.5V18H2v2h20v-2h-6z",
                OutfitScene.Casual => "M12 6c1.1 0 2 .9 2 2s-.9 2-2 2-2-.9-2-2 .9-2 2-2z",
                _ => "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z"
            };
        }
        return "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return ConverterBackResult.DoNothing;
    }
}
