using System.Windows;
using System.Windows.Controls;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
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

    private async void AddTag_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AddTagDialog();
        if (dialog.ShowDialog() == true && dialog.Result != null)
        {
            await _tagService.AddTagAsync(dialog.Result);
            await LoadTagsAsync();
        }
    }
}