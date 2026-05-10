using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ClosetApp.UI.Views;

public partial class NavigationSidebar : UserControl
{
    public event EventHandler<int>? NavigationChanged;
    public event EventHandler<bool>? CollapseStateChanged;

    private bool _isCollapsed;
    private double _expandedWidth = 220;
    private double _collapsedWidth = 72;

    public bool IsCollapsed => _isCollapsed;

    public NavigationSidebar()
    {
        InitializeComponent();
    }

    public void SetClothingCount(int count)
    {
        TxtClothingCount.Text = $"{count} 件衣服";
    }

    private void NavItem_Checked(object sender, RoutedEventArgs e)
    {
        if (sender == NavWardrobe)
            NavigationChanged?.Invoke(this, 0);
        else if (sender == NavOutfits)
            NavigationChanged?.Invoke(this, 1);
        else if (sender == NavTags)
            NavigationChanged?.Invoke(this, 2);
    }

    private void Collapse_Click(object sender, RoutedEventArgs e)
    {
        ToggleCollapse();
    }

    public void ToggleCollapse()
    {
        _isCollapsed = !_isCollapsed;
        CollapseStateChanged?.Invoke(this, _isCollapsed);

        var rotateTarget = _isCollapsed ? 180.0 : 0.0;
        var rotateAnim = new DoubleAnimation(rotateTarget, TimeSpan.FromMilliseconds(220))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        var widthAnim = new DoubleAnimation(_isCollapsed ? 72 : 220, TimeSpan.FromMilliseconds(220))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        BeginAnimation(WidthProperty, widthAnim);

        CollapseRotate.BeginAnimation(RotateTransform.AngleProperty, rotateAnim);
    }

    public void Expand()
    {
        if (_isCollapsed)
            ToggleCollapse();
    }

    public void Collapse()
    {
        if (!_isCollapsed)
            ToggleCollapse();
    }
}

public class GridLengthAnimation : AnimationTimeline
{
    public static readonly DependencyProperty FromProperty =
        DependencyProperty.Register("From", typeof(GridLength), typeof(GridLengthAnimation));

    public static readonly DependencyProperty ToProperty =
        DependencyProperty.Register("To", typeof(GridLength), typeof(GridLengthAnimation));

    public GridLength From
    {
        get => (GridLength)GetValue(FromProperty);
        set => SetValue(FromProperty, value);
    }

    public GridLength To
    {
        get => (GridLength)GetValue(ToProperty);
        set => SetValue(ToProperty, value);
    }

    public IEasingFunction? EasingFunction { get; set; }

    public override Type TargetPropertyType => typeof(GridLength);

    protected override Freezable CreateInstanceCore() => new GridLengthAnimation();

    public override object GetCurrentValue(object defaultOriginValue, object defaultDestinationValue, AnimationClock animationClock)
    {
        var progress = animationClock.CurrentProgress ?? 0;
        var eased = EasingFunction != null ? EasingFunction.Ease(progress) : progress;
        var fromVal = From.Value;
        var toVal = To.Value;
        return new GridLength(fromVal + (toVal - fromVal) * eased, GridUnitType.Pixel);
    }
}
