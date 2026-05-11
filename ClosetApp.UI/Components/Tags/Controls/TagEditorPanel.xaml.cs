using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;

namespace ClosetApp.UI.Components.Tags.Controls;

public partial class TagEditorPanel : UserControl
{
    private readonly Tag? _existingTag;
    private readonly bool _isEditMode;
    private string _selectedColor = "#D8B7A3";

    public event EventHandler<TagEditorResult>? EditorCompleted;

    public TagEditorPanel()
    {
        InitializeComponent();
        Color1.IsChecked = true;
        UpdatePreview();
    }

    public TagEditorPanel(Tag tag) : this()
    {
        _existingTag = tag;
        _isEditMode = true;
        TxtTitle.Text = "编辑标签";
        TxtName.Text = tag.Name;
        _selectedColor = tag.Color;

        if (tag.Category == TagCategory.Scene) RbScene.IsChecked = true;
        else if (tag.Category == TagCategory.Season) RbSeason.IsChecked = true;
        else RbStyle.IsChecked = true;

        SelectColorByValue(tag.Color);
        UpdatePreview();
    }

    private void SelectColorByValue(string color)
    {
        foreach (var child in ((WrapPanel)Color1.Parent).Children)
        {
            if (child is RadioButton rb && rb.Tag?.ToString() == color)
            {
                rb.IsChecked = true;
                break;
            }
        }
    }

    private void Color_Click(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is string color)
        {
            _selectedColor = color;
            UpdatePreview();
        }
    }

    private void UpdatePreview()
    {
        var name = string.IsNullOrWhiteSpace(TxtName.Text) ? "标签名" : TxtName.Text;
        TxtPreview.Text = name;
        if (PreviewChip.Child is StackPanel sp && sp.Children[0] is System.Windows.Shapes.Ellipse ellipse)
        {
            ellipse.Fill = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(_selectedColor));
        }
    }

    private void TxtName_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdatePreview();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtName.Text))
        {
            TxtName.Focus();
            return;
        }

        var category = RbScene.IsChecked == true ? TagCategory.Scene
            : RbSeason.IsChecked == true ? TagCategory.Season
            : TagCategory.Style;

        Tag tag;
        if (_isEditMode && _existingTag != null)
        {
            tag = _existingTag;
            tag.Name = TxtName.Text.Trim();
            tag.Color = _selectedColor;
            tag.Category = category;
        }
        else
        {
            tag = new Tag
            {
                Name = TxtName.Text.Trim(),
                Color = _selectedColor,
                Category = category
            };
        }

        EditorCompleted?.Invoke(this, new TagEditorResult(TagEditorResultType.Saved, tag));
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        EditorCompleted?.Invoke(this, new TagEditorResult(TagEditorResultType.Cancelled, null));
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        EditorCompleted?.Invoke(this, new TagEditorResult(TagEditorResultType.Cancelled, null));
    }
}