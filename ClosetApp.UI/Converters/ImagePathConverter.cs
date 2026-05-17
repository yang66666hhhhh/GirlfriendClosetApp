using System.Globalization;
using System.Windows.Data;
using ClosetApp.UI.Services;

namespace ClosetApp.UI.Converters;

public class ImagePathConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var decodePixelWidth = 400;
        if (parameter != null &&
            int.TryParse(parameter.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedWidth) &&
            parsedWidth > 0)
        {
            decodePixelWidth = parsedWidth;
        }

        return value is string path
            ? ClothingImageLoader.Load(path, decodePixelWidth)
            : null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
