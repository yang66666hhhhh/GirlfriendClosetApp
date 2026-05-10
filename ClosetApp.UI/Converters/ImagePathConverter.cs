using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace ClosetApp.UI.Converters;

public class ImagePathConverter : IValueConverter
{
    private static readonly string ImageFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ClosetApp", "images");

    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        try
        {
            if (value is string path && !string.IsNullOrEmpty(path))
            {
                if (File.Exists(path))
                    return new BitmapImage(new Uri(path, UriKind.Absolute));

                var appDir = AppDomain.CurrentDomain.BaseDirectory;
                var fullPath = Path.Combine(appDir, path);
                if (File.Exists(fullPath))
                    return new BitmapImage(new Uri(fullPath, UriKind.Absolute));

                var localPath = Path.Combine(ImageFolder, path);
                if (File.Exists(localPath))
                    return new BitmapImage(new Uri(localPath, UriKind.Absolute));
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
