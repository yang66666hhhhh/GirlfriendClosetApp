using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ClosetApp.UI.Components.Shared;
using ClosetApp.UI.Components.Shared.Editor;
using ClosetApp.UI.Components.Shared.Modal;

namespace ClosetApp.UI.Views;

    public partial class NavigationSidebar : UserControl
{
    public event EventHandler<int>? NavigationChanged;
    public event EventHandler<bool>? CollapseStateChanged;
    public event EventHandler? PersonalProfileRequested;

    private bool _isCollapsed;

    public bool IsCollapsed => _isCollapsed;

    public NavigationSidebar()
    {
        InitializeComponent();
    }

    public void SetClothingCount(int count)
    {
        TxtClothingCount.Text = $"{count} 件衣服";
    }

    public void SetSelectedTab(int tabIndex)
    {
        switch (tabIndex)
        {
            case 0:
                NavWardrobe.IsChecked = true;
                break;
            case 1:
                NavOutfits.IsChecked = true;
                break;
            case 2:
                NavTags.IsChecked = true;
                break;
            case 3:
                NavSettings.IsChecked = true;
                break;
        }
    }

    private void NavItem_Checked(object sender, RoutedEventArgs e)
    {
        if (sender == NavWardrobe)
            NavigationChanged?.Invoke(this, 0);
        else if (sender == NavOutfits)
            NavigationChanged?.Invoke(this, 1);
        else if (sender == NavTags)
            NavigationChanged?.Invoke(this, 2);
        else if (sender == NavSettings)
            NavigationChanged?.Invoke(this, 3);
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

    private void Profile_Click(object sender, RoutedEventArgs e)
    {
        PersonalProfileRequested?.Invoke(this, EventArgs.Empty);
        EditorModal.Show(new PersonalProfileEditorPanel(), _ => Task.CompletedTask);
    }
}
