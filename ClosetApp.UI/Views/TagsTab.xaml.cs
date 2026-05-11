using System.Windows;
using System.Windows.Controls;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using ClosetApp.UI.Components.Tags.Controls;
using ClosetApp.UI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ClosetApp.UI.Views;

public partial class TagsTab : UserControl
{
    private readonly ITagService _tagService;

    public TagsTab()
    {
        InitializeComponent();
        _tagService = App.Services.GetRequiredService<ITagService>();
        Loaded += async (s, e) => await LoadTagsAsync();
    }

    private async Task LoadTagsAsync()
    {
        var tags = await _tagService.GetAllTagsAsync();
        var list = tags.ToList();
        TagsList.ItemsSource = list;
        TxtEmpty.Visibility = list.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void AddTag_Click(object sender, RoutedEventArgs e)
    {
        var panel = new TagEditorPanel();
        panel.EditorCompleted += async (s, result) =>
        {
            if (result.Type == TagEditorResultType.Saved)
            {
                await _tagService.AddTagAsync(result.Tag!);
                await LoadTagsAsync();
            }
            ModalService.Instance.Hide();
        };
        ModalService.Instance.Show(panel);
    }

    private void EditTag_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem mi && mi.Parent is ContextMenu cm && cm.PlacementTarget is Border border && border.Tag is Tag tag)
            OpenEditPanel(tag);
    }

    private void OpenEditPanel(Tag tag)
    {
        var panel = new TagEditorPanel(tag);
        panel.EditorCompleted += async (s, result) =>
        {
            if (result.Type == TagEditorResultType.Saved)
            {
                await _tagService.UpdateTagAsync(result.Tag!);
                await LoadTagsAsync();
            }
            ModalService.Instance.Hide();
        };
        ModalService.Instance.Show(panel);
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