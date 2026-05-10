using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ClosetApp.Domain.Entities;

namespace ClosetApp.UI.Views;

public partial class RecordOutfitDialog : Window
{
    private readonly List<Outfit> _outfits;
    private Outfit? _selectedOutfit;

    public Outfit? SelectedOutfit => _selectedOutfit;

    public RecordOutfitDialog(List<Outfit> outfits)
    {
        InitializeComponent();
        _outfits = outfits;
        OutfitsList.ItemsSource = _outfits;
    }

    private void OutfitBorder_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is Outfit outfit)
        {
            _selectedOutfit = outfit;

            foreach (var item in OutfitsList.Items)
            {
                if (OutfitsList.ItemContainerGenerator.ContainerFromItem(item) is ContentPresenter cp)
                {
                    var border = cp.ContentTemplate.FindName("OutfitBorder", cp) as Border;
                    if (border != null)
                    {
                        border.BorderBrush = item == outfit
                            ? new SolidColorBrush(Color.FromRgb(102, 126, 234))
                            : Brushes.Transparent;
                    }
                }
            }
        }
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedOutfit == null)
        {
            MessageBox.Show("请选择一个搭配", "提示");
            return;
        }
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}