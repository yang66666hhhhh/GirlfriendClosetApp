using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;

namespace ClosetApp.UI.Components;

public partial class PremiumClothingCard : UserControl
{
    public static readonly RoutedEvent CardClickedEvent = EventManager.RegisterRoutedEvent(
        "CardClicked", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(PremiumClothingCard));

    public event RoutedEventHandler CardClicked
    {
        add => AddHandler(CardClickedEvent, value);
        remove => RemoveHandler(CardClickedEvent, value);
    }

    public static readonly RoutedEvent EditClickedEvent = EventManager.RegisterRoutedEvent(
        "EditClicked", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(PremiumClothingCard));

    public event RoutedEventHandler EditClicked
    {
        add => AddHandler(EditClickedEvent, value);
        remove => RemoveHandler(EditClickedEvent, value);
    }

    public static readonly RoutedEvent DeleteClickedEvent = EventManager.RegisterRoutedEvent(
        "DeleteClicked", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(PremiumClothingCard));

    public event RoutedEventHandler DeleteClicked
    {
        add => AddHandler(DeleteClickedEvent, value);
        remove => RemoveHandler(DeleteClickedEvent, value);
    }

    private bool _isMenuOpen;
    private Point _mouseDownPos;
    private bool _heightApplied;

    private static readonly string ImageFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ClosetApp", "images");

    public PremiumClothingCard()
    {
        InitializeComponent();
        DataContextChanged += (s, e) =>
        {
            if (e.NewValue is Clothing)
            {
                _heightApplied = false;
                ApplyCard();
                StartLoadAnimation();
            }
        };
    }

    private void ApplyCard()
    {
        if (DataContext is not Clothing c) return;

        CardImage.Stretch = Stretch.Uniform;

        double colWidth = FindMasonryColumnWidth();
        if (colWidth > 0)
        {
            double imgH = CalcImageHeight(c, colWidth);
            CardImage.Height = imgH;
            Height = imgH + 48;
            _heightApplied = true;
        }
    }

    protected override Size MeasureOverride(Size constraint)
    {
        if (!_heightApplied && DataContext is Clothing c)
        {
            double colWidth = FindMasonryColumnWidth();
            if (colWidth > 0)
            {
                double imgH = CalcImageHeight(c, colWidth);
                CardImage.Height = imgH;
                Height = imgH + 48;
                _heightApplied = true;
            }
        }
        return base.MeasureOverride(constraint);
    }

    private double FindMasonryColumnWidth()
    {
        var parent = VisualTreeHelper.GetParent(this) as FrameworkElement;
        while (parent != null)
        {
            if (parent is MasonryPanel mp)
                return mp.ColumnWidth;
            parent = VisualTreeHelper.GetParent(parent) as FrameworkElement;
        }
        return 0;
    }

    private double CalcImageHeight(Clothing c, double cardWidth)
    {
        string? path = c.ImagePath;
        if (string.IsNullOrEmpty(path))
            return cardWidth * 1.25;

        try
        {
            string? resolved = ResolveImagePath(path);
            if (resolved == null) return cardWidth * 1.25;

            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(resolved, UriKind.Absolute);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.DecodePixelWidth = 400;
            bmp.EndInit();
            bmp.Freeze();

            double ratio = (double)bmp.PixelHeight / bmp.PixelWidth;
            ratio = Math.Max(0.6, Math.Min(ratio, 2.0));
            return cardWidth * ratio;
        }
        catch
        {
            return cardWidth * 1.25;
        }
    }

    private static string? ResolveImagePath(string path)
    {
        if (File.Exists(path)) return path;

        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        var full = Path.Combine(appDir, path);
        if (File.Exists(full)) return full;

        var local = Path.Combine(ImageFolder, path);
        if (File.Exists(local)) return local;

        return null;
    }

    public void StartLoadAnimation()
    {
        var anim = (Storyboard)Resources["CardLoadAnim"];
        anim?.Begin();
    }

    private void CardLoadAnim_Completed(object? sender, EventArgs e) { }

    private void Card_MouseEnter(object sender, MouseEventArgs e)
    {
        var anim = (Storyboard)Resources["HoverEnterAnim"];
        anim?.Begin();
    }

    private void Card_MouseLeave(object sender, MouseEventArgs e)
    {
        var anim = (Storyboard)Resources["HoverLeaveAnim"];
        anim?.Begin();
        HideMoreMenu();
    }

    private void Card_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _mouseDownPos = e.GetPosition(this);
        var anim = (Storyboard)Resources["PressAnim"];
        anim?.Begin();
    }

    private void Card_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_isMenuOpen)
        {
            e.Handled = true;
            return;
        }
        var pos = e.GetPosition(this);
        if (Math.Abs(pos.X - _mouseDownPos.X) < 5 && Math.Abs(pos.Y - _mouseDownPos.Y) < 5)
        {
            RaiseEvent(new RoutedEventArgs(CardClickedEvent, this));
        }
    }

    private void Card_Click(object sender, MouseButtonEventArgs e)
    {
        if (_isMenuOpen) return;
        RaiseEvent(new RoutedEventArgs(CardClickedEvent, this));
    }

    private void Favorite_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not Clothing) return;

        var heart = FavoriteBtn.Template.FindName("HeartIcon", FavoriteBtn) as FrameworkElement;
        if (heart == null) return;

        var expand = new DoubleAnimation(20, TimeSpan.FromMilliseconds(120))
        { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
        var shrink = new DoubleAnimation(14, TimeSpan.FromMilliseconds(180))
        { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };

        expand.Completed += (s, args) =>
        {
            heart.BeginAnimation(WidthProperty, shrink);
            heart.BeginAnimation(HeightProperty, shrink);
        };

        heart.BeginAnimation(WidthProperty, expand);
        heart.BeginAnimation(HeightProperty, expand);

        e.Handled = true;
    }

    private void More_Click(object sender, RoutedEventArgs e)
    {
        if (_isMenuOpen)
            HideMoreMenu();
        else
            ShowMoreMenu();
        e.Handled = true;
    }

    private void ShowMoreMenu()
    {
        MoreMenu.Visibility = Visibility.Visible;
        _isMenuOpen = true;
    }

    private void HideMoreMenu()
    {
        MoreMenu.Visibility = Visibility.Collapsed;
        _isMenuOpen = false;
    }

    private void MenuEdit_Click(object sender, RoutedEventArgs e)
    {
        HideMoreMenu();
        RaiseEvent(new RoutedEventArgs(EditClickedEvent, this));
        e.Handled = true;
    }

    private void MenuDelete_Click(object sender, RoutedEventArgs e)
    {
        HideMoreMenu();
        RaiseEvent(new RoutedEventArgs(DeleteClickedEvent, this));
        e.Handled = true;
    }
}
