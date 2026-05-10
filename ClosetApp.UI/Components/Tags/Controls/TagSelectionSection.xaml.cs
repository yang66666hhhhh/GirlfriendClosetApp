using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ClosetApp.Domain.Entities;
using ClosetApp.UI.Components.Tags.Models;

namespace ClosetApp.UI.Components.Tags.Controls;

public partial class TagSelectionSection : UserControl
{
    private readonly ObservableCollection<SelectableTag> _tags = new();

    public static readonly DependencyProperty TagsProperty =
        DependencyProperty.Register(
            nameof(Tags),
            typeof(ObservableCollection<SelectableTag>),
            typeof(TagSelectionSection),
            new PropertyMetadata(null));

    public ObservableCollection<SelectableTag> Tags
    {
        get => (ObservableCollection<SelectableTag>)GetValue(TagsProperty);
        set => SetValue(TagsProperty, value);
    }

    public IEnumerable<Tag> SelectedTags => _tags.Where(t => t.IsSelected).Select(t => t.Tag);

    public TagSelectionSection()
    {
        InitializeComponent();
        Tags = _tags;
    }

    public void LoadTags(IEnumerable<Tag> styleTags)
    {
        _tags.Clear();
        foreach (var tag in styleTags)
            _tags.Add(new SelectableTag(tag));
    }

    public void Preselect(IEnumerable<Tag> selected)
    {
        var ids = selected.Select(t => t.Id).ToHashSet();
        foreach (var t in _tags)
            t.IsSelected = ids.Contains(t.Tag.Id);
        RefreshVisualState();
    }

    private void RefreshVisualState()
    {
        var count = _tags.Count(t => t.IsSelected);
        double opacity = count switch
        {
            <= 5 => 1.0,
            <= 8 => 0.82,
            _ => 0.58
        };

        foreach (var tag in _tags.Where(t => !t.IsSelected))
            tag.UnselectedOpacity = opacity;
    }
}
