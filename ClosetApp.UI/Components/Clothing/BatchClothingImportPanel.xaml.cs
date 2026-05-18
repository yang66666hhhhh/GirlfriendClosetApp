using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ClosetApp.Application.DTOs;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Enums;
using ClosetApp.UI.Components.Shared.Editor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace ClosetApp.UI.Components.Clothing;

public partial class BatchClothingImportPanel : UserControl, IEditorPanel<BatchClothingImportRequest>
{
    private static readonly string[] ImageExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp"];

    private readonly ITagService _tagService;
    private readonly List<BatchClothingImportPreviewItem> _previewItems = [];
    private ClothingType _selectedType = ClothingType.Unspecified;
    private Season _selectedSeason = Season.Unspecified;
    private int _favoriteLevel;
    private bool _isSubmitting;

    public event EventHandler<EditorResult<BatchClothingImportRequest>>? EditorCompleted;

    public BatchClothingImportPanel()
    {
        InitializeComponent();
        _tagService = App.Services.GetRequiredService<ITagService>();
        Loaded += OnLoaded;
        RefreshSelectedFiles();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var styleTags = await _tagService.GetStyleTagsAsync();
        TagSelection.LoadTags(styleTags);
    }

    private void PickImages_Click(object sender, RoutedEventArgs e)
    {
        if (_isSubmitting)
            return;

        var dialog = new OpenFileDialog
        {
            Filter = "图片文件|*.jpg;*.jpeg;*.png;*.webp;*.gif;*.bmp",
            Title = "选择要批量导入的衣服图片",
            Multiselect = true
        };

        if (dialog.ShowDialog() == true)
            SetSelectedFiles(dialog.FileNames);
    }

    private void Files_DragEnter(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void Files_DragLeave(object sender, DragEventArgs e)
    {
        e.Handled = true;
    }

    private void Files_Drop(object sender, DragEventArgs e)
    {
        if (_isSubmitting)
            return;

        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            return;

        var files = (string[]?)e.Data.GetData(DataFormats.FileDrop);
        if (files != null)
            SetSelectedFiles(files);

        e.Handled = true;
    }

    private void SetSelectedFiles(IEnumerable<string> files)
    {
        _previewItems.Clear();
        _previewItems.AddRange(files
            .Where(IsSupportedImage)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .Select(file => new BatchClothingImportPreviewItem(
                file,
                Path.GetFileName(file),
                BuildDefaultName(file))));
        RefreshSelectedFiles();
    }

    private static bool IsSupportedImage(string file)
    {
        return File.Exists(file) &&
            ImageExtensions.Contains(Path.GetExtension(file).ToLowerInvariant(), StringComparer.OrdinalIgnoreCase);
    }

    private void RefreshSelectedFiles()
    {
        SelectedFilesList.ItemsSource = null;
        SelectedFilesList.ItemsSource = _previewItems;
        TxtSelectedCount.Text = $"已选择 {_previewItems.Count} 张图片";
        TxtImportHint.Text = _previewItems.Count == 0
            ? "先选择图片；其他信息不确定就留空。"
            : $"将导入 {_previewItems.Count} 件衣服；可先在左侧逐件改名或移除。";

        ImageEmptyState.Visibility = _previewItems.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        ImageListState.Visibility = _previewItems.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
    }

    private void Category_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton btn)
            return;

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
    }

    private void Season_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton btn)
            return;

        _selectedSeason = btn.Tag?.ToString() switch
        {
            "Spring" => Season.Spring,
            "Summer" => Season.Summer,
            "Autumn" => Season.Autumn,
            "Winter" => Season.Winter,
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
            _ => "可不选，之后逐件补也行"
        };
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_isSubmitting)
            return;

        if (_previewItems.Count == 0)
        {
            MessageBox.Show("先选择要导入的图片。", "批量导入", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        SetSubmitting(true);

        var request = new BatchClothingImportRequest(
            _previewItems
                .Select(item => new BatchClothingImportItem(item.FilePath, item.Name))
                .ToList(),
            _selectedType,
            _selectedSeason,
            TxtColor.Text,
            TxtBrand.Text,
            TxtNotes.Text,
            _favoriteLevel,
            TagSelection.SelectedTags.Select(tag => tag.Id).ToList());

        EditorCompleted?.Invoke(
            this,
            new EditorResult<BatchClothingImportRequest>(
                EditorResultType.Saved,
                request));
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        if (_isSubmitting)
            return;

        EditorCompleted?.Invoke(
            this,
            new EditorResult<BatchClothingImportRequest>(EditorResultType.Cancelled));
    }

    private void RemovePreviewItem_Click(object sender, RoutedEventArgs e)
    {
        if (_isSubmitting)
            return;

        if (sender is FrameworkElement { DataContext: BatchClothingImportPreviewItem item })
        {
            _previewItems.Remove(item);
            RefreshSelectedFiles();
        }
    }

    private void SetSubmitting(bool isSubmitting)
    {
        _isSubmitting = isSubmitting;
        BtnSave.IsEnabled = !isSubmitting;
        BtnCancel.IsEnabled = !isSubmitting;
        BtnFooterCancel.IsEnabled = !isSubmitting;
        BtnPickImages.IsEnabled = !isSubmitting;
        SelectedFilesList.IsEnabled = !isSubmitting;
        BtnSave.Content = isSubmitting ? "正在导入..." : "导入这一批";
        TxtImportHint.Text = isSubmitting
            ? "正在保存图片并写入衣柜，请稍等。"
            : $"将导入 {_previewItems.Count} 件衣服；可先在左侧逐件改名或移除。";
    }

    private static string BuildDefaultName(string filePath)
    {
        var name = Path.GetFileNameWithoutExtension(filePath);
        return string.IsNullOrWhiteSpace(name) ? BatchClothingImportBuilder.DefaultName : name.Trim();
    }

    private void Card_MouseDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
    }
}
