using System.Globalization;
using System.Windows.Data;
using ClosetApp.Application.Images;
using ClosetApp.UI.Services;

namespace ClosetApp.UI.Converters;

public class ImagePathConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var (variant, decodePixelWidth) = ParseParameter(parameter);

        return value is string path
            ? ClothingImageLoader.Load(path, variant, decodePixelWidth)
            : null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    private static (ImageVariant Variant, int DecodePixelWidth) ParseParameter(object? parameter)
    {
        const int defaultWidth = 400;
        if (parameter == null)
            return (ImageVariant.Display, defaultWidth);

        var text = parameter.ToString();
        if (string.IsNullOrWhiteSpace(text))
            return (ImageVariant.Display, defaultWidth);

        var parts = text.Split(':', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1 &&
            int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedWidth) &&
            parsedWidth > 0)
        {
            return (ImageVariant.Display, parsedWidth);
        }

        var variant = Enum.TryParse<ImageVariant>(parts[0], ignoreCase: true, out var parsedVariant)
            ? parsedVariant
            : ImageVariant.Display;

        var width = parts.Length > 1 &&
                    int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedWidth) &&
                    parsedWidth > 0
            ? parsedWidth
            : defaultWidth;

        return (variant, width);
    }
}
