using System.Globalization;
using System.Windows.Data;
using ClosetApp.Application.Images;
using ClosetApp.UI.Services;

namespace ClosetApp.UI.Converters;

public class ImagePathConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var (variant, decodePixelWidth, trimLightPadding) = ParseParameter(parameter);

        return value is string path
            ? ClothingImageLoader.Load(path, variant, decodePixelWidth, trimLightPadding)
            : null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    private static (ImageVariant Variant, int DecodePixelWidth, bool TrimLightPadding) ParseParameter(object? parameter)
    {
        const int defaultWidth = 400;
        if (parameter == null)
            return (ImageVariant.Display, defaultWidth, false);

        var text = parameter.ToString();
        if (string.IsNullOrWhiteSpace(text))
            return (ImageVariant.Display, defaultWidth, false);

        var parts = text.Split(':', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1 &&
            int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedWidth) &&
            parsedWidth > 0)
        {
            return (ImageVariant.Display, parsedWidth, false);
        }

        var variant = Enum.TryParse<ImageVariant>(parts[0], ignoreCase: true, out var parsedVariant)
            ? parsedVariant
            : ImageVariant.Display;

        var width = parts.Length > 1 &&
                    int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedWidth) &&
                    parsedWidth > 0
            ? parsedWidth
            : defaultWidth;

        var trim = parts.Skip(2).Any(part =>
            part.Equals("trim", StringComparison.OrdinalIgnoreCase) ||
            part.Equals("trimlightpadding", StringComparison.OrdinalIgnoreCase));

        return (variant, width, trim);
    }
}
