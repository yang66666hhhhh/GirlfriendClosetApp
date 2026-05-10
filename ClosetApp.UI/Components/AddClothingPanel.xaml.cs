using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.Infrastructure.Services;
using ClosetApp.UI.Services;
using Microsoft.Win32;
using Microsoft.Extensions.DependencyInjection;

namespace ClosetApp.UI.Components;

public partial class AddClothingPanel : UserControl
{
    public event EventHandler<Clothing>? Saved;
    public event EventHandler? Cancelled;

    private readonly IImageStorageService _imageStorage;
    private string? _selectedImagePath;
    private ClothingType _selectedType = ClothingType.Top;
    private Season _selectedSeason = Season.AllSeason;
    private int _favoriteLevel;

    public AddClothingPanel()
    {
        InitializeComponent();
        _imageStorage = App.Services.GetRequiredService<IImageStorageService>();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var scaleX = new DoubleAnimation(0.96, 1, TimeSpan.FromMilliseconds(220))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        var scaleY = new DoubleAnimation(0.96, 1, TimeSpan.FromMilliseconds(220))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        var scale = Card.RenderTransform as ScaleTransform;
        scale?.BeginAnimation(ScaleTransform.ScaleXProperty, scaleX);
        scale?.BeginAnimation(ScaleTransform.ScaleYProperty, scaleY);

        var slideUp = new DoubleAnimation(8, 0, TimeSpan.FromMilliseconds(300))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
            BeginTime = TimeSpan.FromMilliseconds(80)
        };
        var translate = FormPanel.RenderTransform as TranslateTransform;
        translate?.BeginAnimation(TranslateTransform.YProperty, slideUp);
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Cancelled?.Invoke(this, EventArgs.Empty);
    }

    private void Card_MouseDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
    }

    private void ImageArea_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
            ImageArea.Background = (SolidColorBrush)FindResource("AccentLight");
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void ImageArea_DragLeave(object sender, DragEventArgs e)
    {
        ImageArea.Background = (SolidColorBrush)FindResource("ImageBg");
    }

    private void ImageArea_Drop(object sender, DragEventArgs e)
    {
        ImageArea.Background = (SolidColorBrush)FindResource("ImageBg");

        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            return;

        var files = (string[]?)e.Data.GetData(DataFormats.FileDrop);
        if (files == null || files.Length == 0)
            return;

        var ext = Path.GetExtension(files[0]).ToLowerInvariant();
        if (ext is ".jpg" or ".jpeg" or ".png" or ".webp" or ".gif" or ".bmp")
            LoadImage(files[0]);

        e.Handled = true;
    }

    private void Upload_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "图片文件|*.jpg;*.jpeg;*.png;*.webp;*.gif;*.bmp",
            Title = "选择衣服图片"
        };
        if (dlg.ShowDialog() == true)
            LoadImage(dlg.FileName);
    }

    private void LoadImage(string path)
    {
        if (!File.Exists(path))
            return;

        _selectedImagePath = path;

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(path);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.DecodePixelWidth = 700;
            bitmap.EndInit();
            bitmap.Freeze();
            PreviewImage.Source = bitmap;

            var info = new FileInfo(path);
            var sizeKb = info.Length / 1024.0;
            TxtImageInfo.Text = sizeKb > 1024
                ? $"已上传 · {sizeKb / 1024:F1} MB"
                : $"已上传 · {sizeKb:F0} KB";

            EmptyState.Visibility = Visibility.Collapsed;
            PreviewState.Visibility = Visibility.Visible;
            PreviewState.Opacity = 0;
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(400))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            PreviewState.BeginAnimation(OpacityProperty, fadeIn);
        }
        catch
        {
            MessageBox.Show("图片加载失败，请重试", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void DeleteImage_Click(object sender, RoutedEventArgs e)
    {
        _selectedImagePath = null;
        PreviewImage.Source = null;
        PreviewState.Visibility = Visibility.Collapsed;
        EmptyState.Visibility = Visibility.Visible;
    }

    private void Category_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton btn) return;

        _selectedType = btn.Content?.ToString() switch
        {
            "上衣" => ClothingType.Top,
            "下装" => ClothingType.Bottom,
            "外套" => ClothingType.Outerwear,
            "裙子" => ClothingType.Dress,
            "鞋子" => ClothingType.Shoes,
            "配饰" => ClothingType.Accessory,
            _ => ClothingType.Top
        };
    }

    private void Season_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton btn) return;

        _selectedSeason = btn.Content?.ToString() switch
        {
            "春" => Season.Spring,
            "夏" => Season.Summer,
            "秋" => Season.Autumn,
            "冬" => Season.Winter,
            _ => Season.AllSeason
        };
    }

    private void FavLevel_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton btn || !int.TryParse(btn.Tag?.ToString(), out var level))
            return;

        _favoriteLevel = level;
        TxtFavHint.Text = level switch
        {
            1 => "一般般",
            2 => "还不错",
            3 => "挺喜欢",
            4 => "很喜欢！",
            5 => "超级爱！",
            _ => "选一个表情吧"
        };
    }

    private void EmotionTag_Click(object sender, RoutedEventArgs e)
    {
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtName.Text))
        {
            ShakeElement(TxtName);
            TxtName.Focus();
            return;
        }

        string imagePath = string.Empty;
        if (!string.IsNullOrEmpty(_selectedImagePath) && File.Exists(_selectedImagePath))
        {
            try
            {
                imagePath = await _imageStorage.SaveImageAsync(_selectedImagePath);
            }
            catch
            {
                imagePath = string.Empty;
            }
        }

        var clothing = new Clothing
        {
            Name = TxtName.Text.Trim(),
            Type = _selectedType,
            Color = string.IsNullOrWhiteSpace(TxtColor.Text) ? null : TxtColor.Text.Trim(),
            Brand = string.IsNullOrWhiteSpace(TxtBrand.Text) ? null : TxtBrand.Text.Trim(),
            Season = _selectedSeason,
            ImagePath = imagePath,
            FavoriteLevel = _favoriteLevel,
            IsFavorite = _favoriteLevel >= 4
        };

        Saved?.Invoke(this, clothing);
    }

    private void ShakeElement(UIElement element)
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
