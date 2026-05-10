using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using ClosetApp.UI.Services;

namespace ClosetApp.UI.Components;

public partial class ModalContainer : UserControl
{
    private bool _isAnimating;

    public ModalContainer()
    {
        InitializeComponent();
        ModalService.Instance.ModalShowRequested += OnShow;
        ModalService.Instance.ModalHideRequested += OnHide;
    }

    private void OnShow(UserControl content)
    {
        if (_isAnimating) return;

        ModalContent.Content = content;
        OverlayRoot.Visibility = Visibility.Visible;
        OverlayRoot.Opacity = 0;

        var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        OverlayRoot.BeginAnimation(OpacityProperty, fade);
    }

    private void OnHide()
    {
        if (_isAnimating || OverlayRoot.Visibility == Visibility.Collapsed) return;

        _isAnimating = true;
        var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(180))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        fade.Completed += (_, _) =>
        {
            OverlayRoot.Visibility = Visibility.Collapsed;
            ModalContent.Content = null;
            _isAnimating = false;
        };
        OverlayRoot.BeginAnimation(OpacityProperty, fade);
    }

    private void Overlay_Click(object sender, MouseButtonEventArgs e)
    {
        ModalService.Instance.Hide();
    }
}
