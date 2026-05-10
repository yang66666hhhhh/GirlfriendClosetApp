using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using ClosetApp.Domain.Entities;

namespace ClosetApp.UI.Converters;

public class OutfitImagesConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        try
        {
            if (value is IEnumerable<OutfitClothing> outfitClothes)
            {
                var images = new ObservableCollection<string>();
                foreach (var oc in outfitClothes.Take(4))
                {
                    if (!string.IsNullOrEmpty(oc.Clothing?.ImagePath))
                        images.Add(oc.Clothing.ImagePath);
                }
                return images;
            }
        }
        catch { }
        return new ObservableCollection<string>();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}