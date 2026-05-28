using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ClosetApp.UI.Components.Shared;

public static class AnimationHelper
{
    public static void Shake(UIElement element)
    {
        var transform = element.RenderTransform as TranslateTransform ?? new TranslateTransform();
        element.RenderTransform = transform;

        var anim = new DoubleAnimationUsingKeyFrames();
        anim.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromPercent(0)));
        anim.KeyFrames.Add(new EasingDoubleKeyFrame(-5, KeyTime.FromPercent(0.15)) { EasingFunction = new QuadraticEase() });
        anim.KeyFrames.Add(new EasingDoubleKeyFrame(5, KeyTime.FromPercent(0.35)) { EasingFunction = new QuadraticEase() });
        anim.KeyFrames.Add(new EasingDoubleKeyFrame(-3, KeyTime.FromPercent(0.55)) { EasingFunction = new QuadraticEase() });
        anim.KeyFrames.Add(new EasingDoubleKeyFrame(3, KeyTime.FromPercent(0.75)) { EasingFunction = new QuadraticEase() });
        anim.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromPercent(1)));

        transform.BeginAnimation(TranslateTransform.XProperty, anim);
    }
}