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

    private bool TryGetViewModel(out TagsViewModel? viewModel)
    {
        viewModel = DataContext as TagsViewModel;
        return viewModel != null;
    }

    private void CategoryFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!TryGetViewModel(out var viewModel))
            return;

        if (sender is not ComboBox { SelectedItem: ComboBoxItem item })
            return;

        ArgumentNullException.ThrowIfNull(viewModel);
        var category = item.Tag?.ToString() switch
        {
            "Style" => TagCategory.Style,
            "Scene" => TagCategory.Scene,
            _ => (TagCategory?)null
        };

        viewModel.SetSelectedCategory(category);
    }

    private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!TryGetViewModel(out var viewModel))
            return;

        if (sender is not ComboBox { SelectedItem: ComboBoxItem item })
            return;

        ArgumentNullException.ThrowIfNull(viewModel);
        var sortBy = item.Tag?.ToString() switch
        {
            "Name" => TagSortBy.Name,
            "LeastUsed" => TagSortBy.LeastUsed,
            "Newest" => TagSortBy.Newest,
            _ => TagSortBy.MostUsed
        };

        viewModel.SetSortBy(sortBy);
    }

    private void UsageFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!TryGetViewModel(out var viewModel))
            return;

        if (sender is not ComboBox { SelectedItem: ComboBoxItem item })
            return;

        ArgumentNullException.ThrowIfNull(viewModel);
        var usageFilter = item.Tag?.ToString() switch
        {
            "Used" => TagUsageFilter.Used,
            "Unused" => TagUsageFilter.Unused,
            _ => TagUsageFilter.All
        };

        viewModel.SetUsageFilter(usageFilter);
    }

    private void ClearFilters_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetViewModel(out var viewModel))
            return;

        ArgumentNullException.ThrowIfNull(viewModel);
        viewModel.ClearFilters();

        if (CategoryFilterComboBox.Items.Count > 0)
            CategoryFilterComboBox.SelectedIndex = 0;

        if (SortComboBox.Items.Count > 0)
            SortComboBox.SelectedIndex = 0;

        if (UsageFilterComboBox.Items.Count > 0)
            UsageFilterComboBox.SelectedIndex = 0;
    }
}
