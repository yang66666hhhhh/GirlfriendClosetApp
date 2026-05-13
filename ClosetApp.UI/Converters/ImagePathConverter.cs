using System.Globalization;
using System.Windows.Data;
using ClosetApp.UI.Services;

namespace ClosetApp.UI.Converters;

public class ImagePathConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is string path
            ? ClothingImageLoader.Load(path)
            : null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
