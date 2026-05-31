using System.Windows;
using System.Windows.Controls;
using ClosetApp.Domain.Enums;
using ClosetApp.UI.Logic.States;
using ClosetApp.UI.ViewModels;

namespace ClosetApp.UI.Components.Tags.Controls;

public partial class TagsFilterPanel : UserControl
{
    public TagsFilterPanel()
    {
        InitializeComponent();
    }

    private TagsViewModel ViewModel => (TagsViewModel)DataContext;

    private void CategoryFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox { SelectedItem: ComboBoxItem item })
            return;

        var category = item.Tag?.ToString() switch
        {
            "Style" => TagCategory.Style,
            "Scene" => TagCategory.Scene,
            _ => (TagCategory?)null
        };

        ViewModel.SetSelectedCategory(category);
    }

    private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox { SelectedItem: ComboBoxItem item })
            return;

        var sortBy = item.Tag?.ToString() switch
        {
            "Name" => TagSortBy.Name,
            "LeastUsed" => TagSortBy.LeastUsed,
            "Newest" => TagSortBy.Newest,
            _ => TagSortBy.MostUsed
        };

        ViewModel.SetSortBy(sortBy);
    }

    private void UsageFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox { SelectedItem: ComboBoxItem item })
            return;

        var usageFilter = item.Tag?.ToString() switch
        {
            "Used" => TagUsageFilter.Used,
            "Unused" => TagUsageFilter.Unused,
            _ => TagUsageFilter.All
        };

        ViewModel.SetUsageFilter(usageFilter);
    }

    private void ClearFilters_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ClearFilters();

        if (CategoryFilterComboBox.Items.Count > 0)
            CategoryFilterComboBox.SelectedIndex = 0;

        if (SortComboBox.Items.Count > 0)
            SortComboBox.SelectedIndex = 0;

        if (UsageFilterComboBox.Items.Count > 0)
            UsageFilterComboBox.SelectedIndex = 0;
    }
}
