using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using ClosetApp.Application.Images;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.UI.Components.Shared;
using ClosetApp.UI.Components.Shared.Editor;
using ClosetApp.UI.Components.Shared.Modal;
using ClosetApp.UI.Logic.Services;
using ClosetApp.UI.Services;
using Microsoft.Win32;
using Microsoft.Extensions.DependencyInjection;

namespace ClosetApp.UI.Components.Clothing;

public partial class ClothingEditorPanel : UserControl, IEditorPanel<global::ClosetApp.Domain.Entities.Clothing>
{
    public event EventHandler<EditorResult<global::ClosetApp.Domain.Entities.Clothing>>? EditorCompleted;

    private readonly bool _isEditMode;
    private readonly global::ClosetApp.Domain.Entities.Clothing? _existingClothing;
    private readonly IImageStorageService _imageStorage;
    private readonly ITagService _tagService;

    private string? _selectedImagePath;
    private bool _imageChanged;
    private ClothingType _selectedType = ClothingType.Top;
    private Season _selectedSeason = Season.AllSeason;
    private int _favoriteLevel;
    private bool _isSubmitting;

    public bool IsDirty { get; private set; }

    public ClothingEditorPanel()
    {
        InitializeComponent();
        _imageStorage = App.Services.GetRequiredService<IImageStorageService>();
        _tagService = App.Services.GetRequiredService<ITagService>();
        _isEditMode = false;
        Loaded += OnLoaded;
    }

    public ClothingEditorPanel(global::ClosetApp.Domain.Entities.Clothing clothing) : this()
    {
        _isEditMode = true;
        _existingClothing = clothing;
        LoadData();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _ = LoadTagsSafelyAsync();
        UpdateCardClip();

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

    private void CardClip_SizeChanged(object sender, SizeChangedEventArgs e) => UpdateCardClip();

    private void UpdateCardClip()
    {
        if (CardClip.ActualWidth <= 0 || CardClip.ActualHeight <= 0)
            return;

        CardClip.Clip = new RectangleGeometry(
            new Rect(0, 0, CardClip.ActualWidth, CardClip.ActualHeight),
            24,
            24);
    }

    private async Task LoadTagsAsync()
    {
        var styleTags = await _tagService.GetStyleTagsAsync();
        TagSelection.LoadTags(styleTags);
        if (_isEditMode && _existingClothing != null)
            TagSelection.Preselect(_existingClothing.ClothingTags.Select(ct => ct.Tag));
    }

    private async Task LoadTagsSafelyAsync()
    {
        try
        {
            await LoadTagsAsync();
        }
        catch (Exception ex)
        {
            var feedback = WardrobeActionErrorPresenter.ForClothingEditorLoad(ex);
            ToastService.Instance.ShowError(feedback.Title, feedback.Detail);
        }
    }

    private void LoadData()
    {
        if (_existingClothing == null) return;

        HeaderAdd.Visibility = Visibility.Collapsed;
        HeaderEdit.Visibility = Visibility.Visible;

        TxtName.Text = _existingClothing.Name;
        TxtColor.Text = _existingClothing.Color ?? "";
        TxtBrand.Text = _existingClothing.Brand ?? "";
        TxtNotes.Text = _existingClothing.Notes ?? "";

        SelectCategory(_existingClothing.Type);
        SelectSeason(_existingClothing.Season);
        SelectFavoriteLevel(_existingClothing.FavoriteLevel);

        if (!string.IsNullOrEmpty(_existingClothing.ImagePath))
            LoadPreviewImage(_existingClothing.ImagePath);

        BtnDelete.Visibility = Visibility.Visible;
    }

    private void SelectCategory(ClothingType type)
    {
        _selectedType = type;
        foreach (var child in CategoryPanel.Children)
        {
            if (child is RadioButton rb)
            {
                var tag = rb.Tag?.ToString();
                var matches = tag switch
                {
                    "Top" => type == ClothingType.Top,
                    "Bottom" => type == ClothingType.Bottom,
                    "Outerwear" => type == ClothingType.Outerwear,
                    "Dress" => type == ClothingType.Dress,
                    "Skirt" => type == ClothingType.Skirt,
                    "Shoes" => type == ClothingType.Shoes,
                    "Accessory" => type == ClothingType.Accessory,
                    _ => false
                };
                if (matches)
                {
                    rb.IsChecked = true;
                    break;
                }
            }
        }
    }

    private void SelectSeason(Season season)
    {
        _selectedSeason = season;
        foreach (var child in SeasonPanel.Children)
        {
            if (child is RadioButton rb)
            {
                var tag = rb.Tag?.ToString();
                var matches = tag switch
                {
                    "Spring" => season == Season.Spring,
                    "Summer" => season == Season.Summer,
                    "Autumn" => season == Season.Autumn,
                    "Winter" => season == Season.Winter,
                    _ => false
                };
                if (matches)
                {
                    rb.IsChecked = true;
                    break;
                }
            }
        }
    }

    private void SelectFavoriteLevel(int level)
    {
        _favoriteLevel = level;
        foreach (var child in FavoritePanel.Children)
        {
            if (child is RadioButton rb && rb.Tag?.ToString() == level.ToString())
            {
                rb.IsChecked = true;
                break;
            }
        }
        UpdateFavHint(level);
    }

    private void LoadPreviewImage(string? imagePath)
    {
        var image = ClothingImageLoader.Load(imagePath, ImageVariant.Display, 700);
        PreviewImage.Source = image;
        EmptyState.Visibility = image == null ? Visibility.Visible : Visibility.Collapsed;
        PreviewState.Visibility = image == null ? Visibility.Collapsed : Visibility.Visible;
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
            if (sender is Border b) b.Background = (SolidColorBrush)FindResource("PrimaryLightBrush");
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void ImageArea_DragLeave(object sender, DragEventArgs e)
    {
        if (sender is Border b) b.Background = (SolidColorBrush)FindResource("SurfaceHeroBrush");
    }

    private void ImageArea_Drop(object sender, DragEventArgs e)
    {
        try
        {
            if (sender is Border b) b.Background = (SolidColorBrush)FindResource("SurfaceHeroBrush");

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
        catch (Exception ex)
        {
            var feedback = WardrobeActionErrorPresenter.ForClothingImageLoad(ex);
            ToastService.Instance.ShowError(feedback.Title, feedback.Detail);
        }
    }

    private void Upload_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dlg = new OpenFileDialog
            {
                Filter = "图片文件|*.jpg;*.jpeg;*.png;*.webp;*.gif;*.bmp",
                Title = "选择衣服图片"
            };
            if (dlg.ShowDialog() == true)
                LoadImage(dlg.FileName);
        }
        catch (Exception ex)
        {
            var feedback = WardrobeActionErrorPresenter.ForClothingImageLoad(ex);
            ToastService.Instance.ShowError(feedback.Title, feedback.Detail);
        }
    }

    private void LoadImage(string path)
    {
        if (!File.Exists(path))
            return;

        _selectedImagePath = path;
        _imageChanged = true;
        IsDirty = true;

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

            EmptyState.Visibility = Visibility.Collapsed;
            PreviewState.Visibility = Visibility.Visible;
            PreviewState.Opacity = 0;
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(350))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            PreviewState.BeginAnimation(OpacityProperty, fadeIn);
        }
        catch (Exception ex)
        {
            var feedback = WardrobeActionErrorPresenter.ForClothingImageLoad(ex);
            ToastService.Instance.ShowError(feedback.Title, feedback.Detail);
        }
    }

    private void EmptyState_HoverEnter(object sender, MouseEventArgs e)
    {
        EmptyState.Background = (SolidColorBrush)FindResource("PrimaryLightBrush");
    }

    private void EmptyState_HoverLeave(object sender, MouseEventArgs e)
    {
        EmptyState.Background = (SolidColorBrush)FindResource("SurfaceHeroBrush");
    }

    private void DeleteImage_Click(object sender, RoutedEventArgs e)
    {
        _selectedImagePath = null;
        _imageChanged = true;
        IsDirty = true;
        PreviewImage.Source = null;
        PreviewState.Visibility = Visibility.Collapsed;
        EmptyState.Visibility = Visibility.Visible;
    }

    private void Category_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton btn) return;

        _selectedType = btn.Tag?.ToString() switch
        {
            "Top" => ClothingType.Top,
            "Bottom" => ClothingType.Bottom,
            "Outerwear" => ClothingType.Outerwear,
            "Dress" => ClothingType.Dress,
            "Skirt" => ClothingType.Skirt,
            "Shoes" => ClothingType.Shoes,
            "Accessory" => ClothingType.Accessory,
            _ => ClothingType.Top
        };
        IsDirty = true;
    }

    private void Season_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton btn) return;

        _selectedSeason = btn.Tag?.ToString() switch
        {
            "Spring" => Season.Spring,
            "Summer" => Season.Summer,
            "Autumn" => Season.Autumn,
            "Winter" => Season.Winter,
            _ => Season.AllSeason
        };
        IsDirty = true;
    }

    private void FavLevel_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton btn || !int.TryParse(btn.Tag?.ToString(), out var level))
            return;

        _favoriteLevel = level;
        IsDirty = true;
        UpdateFavHint(level);
    }

    private void UpdateFavHint(int level)
    {
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

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        if (_isSubmitting)
            return;

        EditorCompleted?.Invoke(this, new EditorResult<global::ClosetApp.Domain.Entities.Clothing>(EditorResultType.Cancelled));
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (_isSubmitting || _existingClothing == null)
            return;

        var confirmed = await ConfirmModal.ShowDeleteAsync(
            $"确定删除「{_existingClothing.Name}」吗？",
            title: "删除衣服");
        if (!confirmed)
            return;

        EditorCompleted?.Invoke(this, new EditorResult<global::ClosetApp.Domain.Entities.Clothing>(EditorResultType.Deleted, _existingClothing));
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_isSubmitting)
            return;

        if (string.IsNullOrWhiteSpace(TxtName.Text))
        {
            AnimationHelper.Shake(TxtName);
            TxtName.Focus();
            ToastService.Instance.ShowInfo("先给这件衣服起个名字吧。");
            return;
        }

        try
        {
            SetSubmitting(true);
            var clothing = await BuildClothingFromFormAsync();
            EditorCompleted?.Invoke(this, new EditorResult<global::ClosetApp.Domain.Entities.Clothing>(EditorResultType.Saved, clothing));
        }
        catch (Exception ex)
        {
            var feedback = WardrobeActionErrorPresenter.ForClothingSave(ex, _isEditMode);
            ToastService.Instance.ShowError(feedback.Title, feedback.Detail);
        }
        finally
        {
            SetSubmitting(false);
        }
    }

    private async Task<global::ClosetApp.Domain.Entities.Clothing> BuildClothingFromFormAsync()
    {
        var name = TxtName.Text.Trim();
        var color = string.IsNullOrWhiteSpace(TxtColor.Text) ? null : TxtColor.Text.Trim();
        var brand = string.IsNullOrWhiteSpace(TxtBrand.Text) ? null : TxtBrand.Text.Trim();
        var notes = string.IsNullOrWhiteSpace(TxtNotes.Text) ? null : TxtNotes.Text.Trim();
        var selectedTagIds = TagSelection.SelectedTags.Select(t => t.Id).ToHashSet();

        if (_isEditMode && _existingClothing != null)
        {
            _existingClothing.Name = name;
            _existingClothing.Type = _selectedType;
            _existingClothing.Season = _selectedSeason;
            _existingClothing.Color = color;
            _existingClothing.Brand = brand;
            _existingClothing.Notes = notes;
            _existingClothing.FavoriteLevel = _favoriteLevel;

            if (_imageChanged)
                _existingClothing.ImagePath = await ResolveImagePathAsync();

            ApplyTagChanges(_existingClothing, selectedTagIds);
            return _existingClothing;
        }

        return new global::ClosetApp.Domain.Entities.Clothing
        {
            Name = name,
            Type = _selectedType,
            Season = _selectedSeason,
            Color = color,
            Brand = brand,
            Notes = notes,
            ImagePath = await ResolveImagePathAsync(),
            FavoriteLevel = _favoriteLevel,
            ClothingTags = selectedTagIds
                .Select(id => new ClothingTag { TagId = id })
                .ToList()
        };
    }

    private async Task<string> ResolveImagePathAsync()
    {
        if (string.IsNullOrEmpty(_selectedImagePath) || !File.Exists(_selectedImagePath))
            return _isEditMode ? (_existingClothing?.ImagePath ?? "") : "";

        return await _imageStorage.SaveImageAsync(_selectedImagePath);
    }

    private static void ApplyTagChanges(
        global::ClosetApp.Domain.Entities.Clothing clothing,
        HashSet<Guid> selectedTagIds)
    {
        var toRemove = clothing.ClothingTags
            .Where(ct => !selectedTagIds.Contains(ct.TagId))
            .ToList();
        foreach (var item in toRemove)
            clothing.ClothingTags.Remove(item);

        var existingIds = clothing.ClothingTags.Select(ct => ct.TagId).ToHashSet();
        foreach (var id in selectedTagIds.Except(existingIds))
            clothing.ClothingTags.Add(new ClothingTag { ClothingId = clothing.Id, TagId = id });
    }

    private void SetSubmitting(bool isSubmitting)
    {
        _isSubmitting = isSubmitting;
        BtnSave.IsEnabled = !isSubmitting;
        BtnHeaderCancel.IsEnabled = !isSubmitting;
        BtnFooterCancel.IsEnabled = !isSubmitting;
        BtnDelete.IsEnabled = !isSubmitting;
        BtnSave.Content = isSubmitting ? "正在保存..." : "保存衣服 ♥";
    }
}
