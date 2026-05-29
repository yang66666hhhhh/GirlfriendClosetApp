using System.Windows;
using System.Windows.Controls;
using ClosetApp.Domain.Enums;

namespace ClosetApp.UI.Components.Tags.Controls;

public partial class TagFilterPanel : UserControl
{
    public TagFilterPanel()
    {
        InitializeComponent();
    }

    public event EventHandler<TagCategory?>? CategoryChanged;
    public event EventHandler? ClearFiltersRequested;

    public void ResetCategoryFilter()
    {
        if (CategoryFilterComboBox.Items.Count > 0)
            CategoryFilterComboBox.SelectedIndex = 0;
    }

    private void CategoryFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox { SelectedItem: ComboBoxItem item })
            return;

        var category = item.Tag?.ToString() switch
        {
            "Style" => TagCategory.Style,
            "Scene" => TagCategory.Scene,
            "Season" => TagCategory.Season,
            _ => (TagCategory?)null
        };

        CategoryChanged?.Invoke(this, category);
    }

    private void ClearFilters_Click(object sender, RoutedEventArgs e)
    {
        ClearFiltersRequested?.Invoke(this, EventArgs.Empty);
    }
}
