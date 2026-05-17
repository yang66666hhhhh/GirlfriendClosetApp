using System.Windows;
using System.Windows.Controls;
using ClosetApp.Domain.Entities;
using ClosetApp.UI.Components.Tags.Controls;
using ClosetApp.UI.Components.Shared.Editor;
using ClosetApp.UI.Components.Shared.Modal;
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
                await _viewModel.AddTagAsync(result.Entity!);
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
                await _viewModel.UpdateTagAsync(result.Entity!);
        });
    }

    private async void DeleteTag_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem mi && mi.Parent is ContextMenu cm && cm.PlacementTarget is Border border && border.Tag is Tag tag)
        {
            var confirmed = await ConfirmModal.ShowDeleteAsync(
                $"确定删除标签「{tag.Name}」？衣服上的此标签将被移除。");
            if (!confirmed)
                return;

            await _viewModel.DeleteTagAsync(tag);
        }
    }
}
