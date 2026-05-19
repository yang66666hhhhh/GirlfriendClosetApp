using System.Windows;
using System.Windows.Controls;
using ClosetApp.Domain.Entities;
using ClosetApp.UI.Components.Tags.Controls;
using ClosetApp.UI.Components.Shared.Editor;
using ClosetApp.UI.Components.Shared.Modal;
using ClosetApp.UI.Services;
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
        if (sender is MenuItem mi && mi.Parent is ContextMenu cm && cm.PlacementTarget is Border border && border.Tag is Tag tag)
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
        if (sender is MenuItem mi && mi.Parent is ContextMenu cm && cm.PlacementTarget is Border border && border.Tag is Tag tag)
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
    }
}
