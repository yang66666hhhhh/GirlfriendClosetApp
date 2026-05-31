using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ClosetApp.UI.Components.Tags.Controls;

public partial class TagSectionPanel : UserControl
{
    public TagSectionPanel()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty SectionTitleProperty =
        DependencyProperty.Register(nameof(SectionTitle), typeof(string), typeof(TagSectionPanel), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty SectionDescriptionProperty =
        DependencyProperty.Register(nameof(SectionDescription), typeof(string), typeof(TagSectionPanel), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty CountTextProperty =
        DependencyProperty.Register(nameof(CountText), typeof(string), typeof(TagSectionPanel), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty BadgeBackgroundProperty =
        DependencyProperty.Register(nameof(BadgeBackground), typeof(Brush), typeof(TagSectionPanel));

    public static readonly DependencyProperty BadgeBorderBrushProperty =
        DependencyProperty.Register(nameof(BadgeBorderBrush), typeof(Brush), typeof(TagSectionPanel));

    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(TagSectionPanel));

    public static readonly DependencyProperty ItemTemplateProperty =
        DependencyProperty.Register(nameof(ItemTemplate), typeof(DataTemplate), typeof(TagSectionPanel));

    public static readonly DependencyProperty SectionVisibilityProperty =
        DependencyProperty.Register(nameof(SectionVisibility), typeof(Visibility), typeof(TagSectionPanel), new PropertyMetadata(Visibility.Visible));

    public string SectionTitle
    {
        get => (string)GetValue(SectionTitleProperty);
        set => SetValue(SectionTitleProperty, value);
    }

    public string SectionDescription
    {
        get => (string)GetValue(SectionDescriptionProperty);
        set => SetValue(SectionDescriptionProperty, value);
    }

    public string CountText
    {
        get => (string)GetValue(CountTextProperty);
        set => SetValue(CountTextProperty, value);
    }

    public Brush BadgeBackground
    {
        get => (Brush)GetValue(BadgeBackgroundProperty);
        set => SetValue(BadgeBackgroundProperty, value);
    }

    public Brush BadgeBorderBrush
    {
        get => (Brush)GetValue(BadgeBorderBrushProperty);
        set => SetValue(BadgeBorderBrushProperty, value);
    }

    public IEnumerable ItemsSource
    {
        get => (IEnumerable)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public DataTemplate ItemTemplate
    {
        get => (DataTemplate)GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    public Visibility SectionVisibility
    {
        get => (Visibility)GetValue(SectionVisibilityProperty);
        set => SetValue(SectionVisibilityProperty, value);
    }
}
