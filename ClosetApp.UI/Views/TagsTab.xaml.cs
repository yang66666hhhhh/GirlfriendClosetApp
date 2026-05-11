using System.Windows;
using System.Windows.Controls;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using ClosetApp.UI.Components.Tags.Controls;
using ClosetApp.UI.Components.Shared.Editor;
using ClosetApp.UI.States;
using Microsoft.Extensions.DependencyInjection;

namespace ClosetApp.UI.Views;

public partial class TagsTab : UserControl
{
    private readonly ITagService _tagService;
    private readonly TagsTabState _state = new();

    public TagsTab()
    {
        InitializeComponent();
        _tagService = App.Services.GetRequiredService<ITagService>();
        Loaded += async (s, e) => await LoadTagsAsync();
    }

    private async Task LoadTagsAsync()
    {
        _state.BeginLoad();
        var tags = await _tagService.GetAllTagsAsync();
        _state.SetTags(tags);
        TagsList.ItemsSource = _state.Tags;
        TxtEmpty.Visibility = _state.IsEmpty ? Visibility.Visible : Visibility.Collapsed;
    }

    private void AddTag_Click(object sender, RoutedEventArgs e)
    {
        EditorModal.Show(new TagEditorPanel(), async result =>
        {
            if (result.Type == EditorResultType.Saved)
            {
                await _tagService.AddTagAsync(result.Entity!);
                await LoadTagsAsync();
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
                await _tagService.UpdateTagAsync(result.Entity!);
                await LoadTagsAsync();
            }
        });
    }

    private async void DeleteTag_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem mi && mi.Parent is ContextMenu cm && cm.PlacementTarget is Border border && border.Tag is Tag tag)
        {
            var confirmed = MessageBox.Show(
                $"确定删除标签「{tag.Name}」？衣服上的此标签将被移除。",
                "确认删除",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Warning);

            if (confirmed == MessageBoxResult.OK)
            {
                await _tagService.DeleteTagAsync(tag.Id);
                await LoadTagsAsync();
            }
        }
    }
}
