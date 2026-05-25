using System.Windows;
using System.Windows.Controls;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.UI.Components.Tags.Controls;
using ClosetApp.UI.Components.Shared.Editor;
using ClosetApp.UI.Components.Shared.Modal;
using ClosetApp.UI.Services;
using ClosetApp.UI.States;
using ClosetApp.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ClosetApp.UI.Views;

public partial class TagsTab : UserControl
{
    private readonly TagsViewModel _viewModel;

    public TagsTab()
    {
        _viewModel = App.Services.GetRequiredService<TagsViewModel>();
        InitializeComponent();
        DataContext = _viewModel;
        Loaded += async (s, e) => await LoadTagsAsync();
    }

    private async Task LoadTagsAsync()
    {
        await _viewModel.LoadTagsAsync();
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

        _viewModel.SetSelectedCategory(category);
    }

    private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox { SelectedItem: ComboBoxItem item })
            return;

        var sortBy = item.Tag?.ToString() switch
        {
            "Name" => TagSortBy.Name,
            "LeastUsed" => TagSortBy.LeastUsed,
            _ => TagSortBy.MostUsed
        };

        _viewModel.SetSortBy(sortBy);
    }

    private void ClearFilters_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ClearFilters();

        if (CategoryFilterComboBox.Items.Count > 0)
            CategoryFilterComboBox.SelectedIndex = 0;

        if (SortComboBox.Items.Count > 0)
            SortComboBox.SelectedIndex = 0;
    }

    private void AddTag_Click(object sender, RoutedEventArgs e)
    {
        EditorModal.Show(new TagEditorPanel(), async result =>
        {
            if (result.Type == EditorResultType.Saved)
            {
                try
                {
                    await _viewModel.AddTagAsync(result.Entity!);
                    ToastService.Instance.ShowSuccess($"已添加标签「{result.Entity!.Name}」", "现在可以把它用在衣服筛选和整理里了。");
                }
                catch (Exception ex)
                {
                    ToastService.Instance.ShowError("添加标签失败", ex.Message);
                }
            }
        });
    }

    private void EditTag_Click(object sender, RoutedEventArgs e)
    {
        if (TryResolveTagFromContextMenu(sender, out var tag))
            OpenEditPanel(tag);
    }

    private void OpenEditPanel(Tag tag)
    {
        EditorModal.Show(new TagEditorPanel(tag), async result =>
        {
            if (result.Type == EditorResultType.Saved)
            {
                try
                {
                    await _viewModel.UpdateTagAsync(result.Entity!);
                    ToastService.Instance.ShowSuccess($"已更新标签「{result.Entity!.Name}」", "标签修改已经同步到列表。");
                }
                catch (Exception ex)
                {
                    ToastService.Instance.ShowError("更新标签失败", ex.Message);
                }
            }
        });
    }

    private async void DeleteTag_Click(object sender, RoutedEventArgs e)
    {
        if (TryResolveTagFromContextMenu(sender, out var tag))
            await DeleteTagAsync(tag);
    }

    private void MoreButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { ContextMenu: { } menu } button)
            return;

        menu.PlacementTarget = button;
        menu.IsOpen = true;
        e.Handled = true;
    }

    // 标签卡片和右键菜单共用一套删除流程，避免两边文案和行为漂移。
    private async Task DeleteTagAsync(Tag tag)
    {
        try
        {
            var confirmed = await ConfirmModal.ShowDeleteAsync(
                $"确定删除标签「{tag.Name}」？衣服上的此标签将被移除。");
            if (!confirmed)
                return;

            await _viewModel.DeleteTagAsync(tag);
            ToastService.Instance.ShowSuccess($"已删除标签「{tag.Name}」", "衣服上的对应标签也已经移除。");
        }
        catch (Exception ex)
        {
            var feedback = WardrobeActionErrorPresenter.ForTagDelete(ex, tag.Name);
            ToastService.Instance.ShowError(feedback.Title, feedback.Detail);
        }
    }

    private static bool TryResolveTag(object sender, out Tag tag)
    {
        if (sender is FrameworkElement { Tag: Tag resolvedTag })
        {
            tag = resolvedTag;
            return true;
        }

        tag = null!;
        return false;
    }

    private static bool TryResolveTagFromContextMenu(object sender, out Tag tag)
    {
        if (sender is MenuItem { Parent: ContextMenu { PlacementTarget: FrameworkElement target } })
            return TryResolveTag(target, out tag);

        tag = null!;
        return false;
    }
}
