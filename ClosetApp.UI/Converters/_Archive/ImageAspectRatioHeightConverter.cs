using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace ClosetApp.UI.Converters;

public class ImageAspectRatioHeightConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2) return 280.0;

        string? imagePath = values[0] as string;
        double cardWidth = values[1] is double w && w > 0 ? w : 280.0;

        if (string.IsNullOrEmpty(imagePath)) return 280.0;

        try
        {
            string? resolvedPath = null;
            if (File.Exists(imagePath))
                resolvedPath = imagePath;
            else
            {
                var fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, imagePath);
                if (File.Exists(fullPath))
                    resolvedPath = fullPath;
            }

            if (resolvedPath == null) return 280.0;

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(resolvedPath, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.DecodePixelWidth = 400;
            bitmap.EndInit();
            bitmap.Freeze();

            double aspectRatio = (double)bitmap.PixelHeight / bitmap.PixelWidth;
            double clampedRatio = Math.Max(0.75, Math.Min(aspectRatio, 1.8));

            return cardWidth * clampedRatio;
        }
        catch
        {
            return 280.0;
        }
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
