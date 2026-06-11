using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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
        Overlay.Opacity = 0;
        ModalContent.Opacity = 0;
        ModalContent.RenderTransform = new TransformGroup
        {
            Children =
            {
                new ScaleTransform(0.988, 0.988),
                new TranslateTransform(0, 12)
            }
        };

        var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        var overlayFade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        var contentFade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        var contentLift = new DoubleAnimation(12, 0, TimeSpan.FromMilliseconds(220))
        {
            EasingFunction = new BackEase { Amplitude = 0.28, EasingMode = EasingMode.EaseOut }
        };
        var contentScale = new DoubleAnimation(0.988, 1, TimeSpan.FromMilliseconds(200))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        OverlayRoot.BeginAnimation(OpacityProperty, fade);
        Overlay.BeginAnimation(OpacityProperty, overlayFade);
        ModalContent.BeginAnimation(OpacityProperty, contentFade);
        if (ModalContent.RenderTransform is TransformGroup showTransforms &&
            showTransforms.Children.Count >= 2 &&
            showTransforms.Children[0] is ScaleTransform showScale &&
            showTransforms.Children[1] is TranslateTransform showTranslate)
        {
            showScale.BeginAnimation(ScaleTransform.ScaleXProperty, contentScale);
            showScale.BeginAnimation(ScaleTransform.ScaleYProperty, contentScale);
            showTranslate.BeginAnimation(TranslateTransform.YProperty, contentLift);
        }
    }

    private void OnHide()
    {
        if (_isAnimating || OverlayRoot.Visibility == Visibility.Collapsed) return;

        _isAnimating = true;
        var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(170))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        var overlayFade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(140))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        var contentFade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(150))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        var contentDrop = new DoubleAnimation(0, 8, TimeSpan.FromMilliseconds(160))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        var contentScale = new DoubleAnimation(1, 0.992, TimeSpan.FromMilliseconds(150))
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
        Overlay.BeginAnimation(OpacityProperty, overlayFade);
        ModalContent.BeginAnimation(OpacityProperty, contentFade);
        if (ModalContent.RenderTransform is TransformGroup hideTransforms &&
            hideTransforms.Children.Count >= 2 &&
            hideTransforms.Children[0] is ScaleTransform hideScale &&
            hideTransforms.Children[1] is TranslateTransform hideTranslate)
        {
            hideScale.BeginAnimation(ScaleTransform.ScaleXProperty, contentScale);
            hideScale.BeginAnimation(ScaleTransform.ScaleYProperty, contentScale);
            hideTranslate.BeginAnimation(TranslateTransform.YProperty, contentDrop);
        }
    }

    private void Overlay_Click(object sender, MouseButtonEventArgs e)
    {
        ModalService.Instance.Hide();
    }
}
