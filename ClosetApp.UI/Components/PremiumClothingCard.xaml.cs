using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.UI.Services;

namespace ClosetApp.UI.Components;

public partial class PremiumClothingCard : UserControl
{
    private const double ImageStageChromeHeight = 68;
    private const double InfoAreaHeight = 76;

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

    public PremiumClothingCard()
    {
        InitializeComponent();
        DataContextChanged += (s, e) =>
        {
            if (e.NewValue is global::ClosetApp.Domain.Entities.Clothing)
            {
                _heightApplied = false;
                ApplyCard();
                StartLoadAnimation();
            }
        };
    }

    private void ApplyCard()
    {
        if (DataContext is not global::ClosetApp.Domain.Entities.Clothing c) return;

        CardImage.Stretch = Stretch.Uniform;

        double colWidth = FindMasonryColumnWidth();
        if (colWidth > 0)
        {
            double imgH = CalcImageHeight(c, colWidth);
            CardImage.Height = imgH;
            Height = imgH + ImageStageChromeHeight + InfoAreaHeight;
            _heightApplied = true;
        }
    }

    protected override Size MeasureOverride(Size constraint)
    {
        if (!_heightApplied && DataContext is global::ClosetApp.Domain.Entities.Clothing c)
        {
            double colWidth = FindMasonryColumnWidth();
            if (colWidth > 0)
            {
                double imgH = CalcImageHeight(c, colWidth);
                CardImage.Height = imgH;
                Height = imgH + ImageStageChromeHeight + InfoAreaHeight;
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

    private double CalcImageHeight(global::ClosetApp.Domain.Entities.Clothing c, double cardWidth)
    {
        string? path = c.ImagePath;
        if (string.IsNullOrEmpty(path))
            return cardWidth * 1.08;

        try
        {
            var imageSize = ClothingImageLoader.GetDisplaySize(path);
            if (imageSize == null || imageSize.Value.Width <= 0)
                return cardWidth * 1.08;

            double ratio = imageSize.Value.Height / imageSize.Value.Width;
            ratio = Math.Clamp(ratio, 0.78, 1.35);
            return cardWidth * ratio;
        }
        catch
        {
            return cardWidth * 1.08;
        }
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
        if (DataContext is not global::ClosetApp.Domain.Entities.Clothing) return;

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
