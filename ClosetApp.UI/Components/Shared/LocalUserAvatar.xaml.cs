using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ClosetApp.Infrastructure;

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

    private static void OnAvatarPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((LocalUserAvatar)d).RefreshAvatar();
    }

    private void RefreshAvatar()
    {
        InitialText.Text = BuildInitial(Initial);
        StatusDot.Visibility = ShowStatus && IsCurrent ? Visibility.Visible : Visibility.Collapsed;

        var resolvedPath = ResolveAvatarPath(AvatarPath);
        if (string.IsNullOrWhiteSpace(resolvedPath) || !File.Exists(resolvedPath))
        {
            AvatarPhoto.Fill = null;
            AvatarPhoto.Visibility = Visibility.Collapsed;
            InitialHost.Visibility = Visibility.Visible;
            return;
        }

        AvatarPhoto.Fill = new ImageBrush(LoadBitmap(resolvedPath))
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

    private static string? ResolveAvatarPath(string? avatarPath)
    {
        if (string.IsNullOrWhiteSpace(avatarPath))
            return null;

        return Path.IsPathRooted(avatarPath)
            ? avatarPath
            : Path.Combine(AppPaths.AiProfileDir, avatarPath);
    }

    private static BitmapImage LoadBitmap(string path)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.UriSource = new Uri(path);
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.DecodePixelWidth = 160;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }
}
