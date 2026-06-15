using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ClosetApp.Infrastructure;
using ClosetApp.UI.Services;

namespace ClosetApp.UI.Components.Shared;

public partial class LocalUserAvatar : UserControl
{
    public static readonly DependencyProperty AvatarPathProperty =
        DependencyProperty.Register(
            nameof(AvatarPath),
            typeof(string),
            typeof(LocalUserAvatar),
            new PropertyMetadata(null, OnAvatarPropertyChanged));

    public static readonly DependencyProperty InitialProperty =
        DependencyProperty.Register(
            nameof(Initial),
            typeof(string),
            typeof(LocalUserAvatar),
            new PropertyMetadata("衣", OnAvatarPropertyChanged));

    public static readonly DependencyProperty IsCurrentProperty =
        DependencyProperty.Register(
            nameof(IsCurrent),
            typeof(bool),
            typeof(LocalUserAvatar),
            new PropertyMetadata(false, OnAvatarPropertyChanged));

    public static readonly DependencyProperty ShowStatusProperty =
        DependencyProperty.Register(
            nameof(ShowStatus),
            typeof(bool),
            typeof(LocalUserAvatar),
            new PropertyMetadata(true, OnAvatarPropertyChanged));

    public static readonly DependencyProperty AvatarAssetResolverProperty =
        DependencyProperty.Register(
            nameof(AvatarAssetResolver),
            typeof(Func<string, string>),
            typeof(LocalUserAvatar),
            new PropertyMetadata(null, OnAvatarPropertyChanged));

    public LocalUserAvatar()
    {
        InitializeComponent();
        RefreshAvatar();
    }

    public string? AvatarPath
    {
        get => (string?)GetValue(AvatarPathProperty);
        set => SetValue(AvatarPathProperty, value);
    }

    public string Initial
    {
        get => (string)GetValue(InitialProperty);
        set => SetValue(InitialProperty, value);
    }

    public bool IsCurrent
    {
        get => (bool)GetValue(IsCurrentProperty);
        set => SetValue(IsCurrentProperty, value);
    }

    public bool ShowStatus
    {
        get => (bool)GetValue(ShowStatusProperty);
        set => SetValue(ShowStatusProperty, value);
    }

    public Func<string, string>? AvatarAssetResolver
    {
        get => (Func<string, string>?)GetValue(AvatarAssetResolverProperty);
        set => SetValue(AvatarAssetResolverProperty, value);
    }

    /// <summary>
    /// 强制重新加载头像图片，用于文件内容已替换但路径未变化的场景。
    /// </summary>
    public void Refresh() => RefreshAvatar();

    private static void OnAvatarPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((LocalUserAvatar)d).RefreshAvatar();
    }

    private void RefreshAvatar()
    {
        InitialText.Text = BuildInitial(Initial);
        StatusDot.Visibility = ShowStatus && IsCurrent ? Visibility.Visible : Visibility.Collapsed;
        CurrentRing.Opacity = IsCurrent ? 0.56 : 0.2;
        CurrentRing.BorderThickness = IsCurrent ? new Thickness(1.4) : new Thickness(1.2);
        AvatarSurface.BorderBrush = (Brush)FindResource(IsCurrent ? "PrimaryLightBrush" : "BorderLightBrush");
        AvatarSurface.BorderThickness = IsCurrent ? new Thickness(0.9) : new Thickness(0.6);
        AvatarBody.BorderBrush = (Brush)FindResource(IsCurrent ? "PrimaryBrush" : "BorderLightBrush");
        AvatarBody.BorderThickness = IsCurrent ? new Thickness(1) : new Thickness(0.8);
        AvatarPhoto.Width = double.NaN;
        AvatarPhoto.Height = double.NaN;
        AvatarPhoto.HorizontalAlignment = HorizontalAlignment.Stretch;
        AvatarPhoto.VerticalAlignment = VerticalAlignment.Stretch;

        var resolvedPath = ResolveAvatarPath(AvatarPath, AvatarAssetResolver);
        if (string.IsNullOrWhiteSpace(resolvedPath) || !File.Exists(resolvedPath))
        {
            AvatarPhoto.Fill = null;
            AvatarPhoto.Visibility = Visibility.Collapsed;
            InitialHost.Visibility = Visibility.Visible;
            return;
        }

        var bitmap = PreviewImageSourceFactory.TryCreateBitmapSource(resolvedPath, decodePixelWidth: 160);
        if (bitmap == null)
        {
            AvatarPhoto.Fill = null;
            AvatarPhoto.Visibility = Visibility.Collapsed;
            InitialHost.Visibility = Visibility.Visible;
            return;
        }

        AvatarPhoto.Fill = new ImageBrush(bitmap)
        {
            Stretch = Stretch.UniformToFill,
            AlignmentX = AlignmentX.Center,
            AlignmentY = AlignmentY.Center
        };
        AvatarPhoto.Visibility = Visibility.Visible;
        InitialHost.Visibility = Visibility.Collapsed;
    }

    private static string BuildInitial(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "衣"
            : value.Trim()[0].ToString();
    }

    private static string? ResolveAvatarPath(string? avatarPath, Func<string, string>? avatarAssetResolver)
    {
        if (string.IsNullOrWhiteSpace(avatarPath))
            return null;

        if (Path.IsPathRooted(avatarPath))
            return avatarPath;

        if (avatarAssetResolver != null)
            return avatarAssetResolver(avatarPath);

        return Path.Combine(AppPaths.AiProfileDir, avatarPath);
    }

}
