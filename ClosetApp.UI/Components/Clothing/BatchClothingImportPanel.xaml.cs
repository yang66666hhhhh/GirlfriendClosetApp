using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ClosetApp.Application.Images;
using ClosetApp.Application.DTOs;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.UI.Components.Shared.Editor;
using ClosetApp.UI.Components.Shared.Modal;
using ClosetApp.UI.Logic.Components.Clothing;
using ClosetApp.UI.Logic.Services;
using ClosetApp.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace ClosetApp.UI.Components.Clothing;

public partial class BatchClothingImportPanel : UserControl, IEditorPanel<BatchClothingImportRequest>
{
    private static readonly string[] ImageExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp"];

    private readonly IClothingService _clothingService;
    private readonly ITagService _tagService;
    private readonly List<BatchClothingImportPreviewItem> _previewItems = [];
    private ClothingType _selectedType = ClothingType.Unspecified;
    private Season _selectedSeason = Season.Unspecified;
    private int _favoriteLevel;
    private bool _isSubmitting;
    private bool _awaitingDuplicateConfirmation;
    private IReadOnlyList<global::ClosetApp.Domain.Entities.Clothing> _existingClothes = [];

    public event EventHandler<EditorResult<BatchClothingImportRequest>>? EditorCompleted;

    public BatchClothingImportPanel()
    {
        InitializeComponent();
        _clothingService = App.Services.GetRequiredService<IClothingService>();
        _tagService = App.Services.GetRequiredService<ITagService>();
        Loaded += OnLoaded;
        RefreshSelectedFiles();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _existingClothes = (await _clothingService.GetAllClothesAsync()).ToList();
            var styleTags = await _tagService.GetStyleTagsAsync();
            TagSelection.LoadTags(styleTags);
            RefreshSelectedFiles();
        }
        catch (Exception ex)
        {
            HandlePanelError("批量导入面板初始化失败", ex);
        }
    }

    private void PickImages_Click(object sender, RoutedEventArgs e)
    {
        if (_isSubmitting)
            return;

        try
        {
            var dialog = new OpenFileDialog
            {
                Filter = "图片文件|*.jpg;*.jpeg;*.png;*.webp;*.gif;*.bmp",
                Title = "选择要批量导入的衣服图片",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
                SetSelectedFiles(dialog.FileNames);
        }
        catch (Exception ex)
        {
            HandlePanelError("选择图片失败", ex);
        }
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

        try
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return;

            var files = (string[]?)e.Data.GetData(DataFormats.FileDrop);
            if (files != null)
                SetSelectedFiles(files);
        }
        catch (Exception ex)
        {
            HandlePanelError("拖入图片失败", ex);
        }

        e.Handled = true;
    }

    private void SetSelectedFiles(IEnumerable<string> files)
    {
        try
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
        catch (Exception ex)
        {
            HandlePanelError("整理图片列表失败", ex);
        }
    }

    private static bool IsSupportedImage(string file)
    {
        return File.Exists(file) &&
            ImageExtensions.Contains(Path.GetExtension(file).ToLowerInvariant(), StringComparer.OrdinalIgnoreCase);
    }

    private void RefreshSelectedFiles()
    {
        try
        {
            _awaitingDuplicateConfirmation = false;
            ConfirmDuplicateState.Visibility = Visibility.Collapsed;

            foreach (var item in _previewItems)
            {
                item.IsDuplicateRisk = false;
                item.DuplicateReason = null;
            }

            var duplicateCheck = _previewItems.Count == 0
                ? null
                : BatchImportDuplicateChecker.Analyze(_previewItems, _existingClothes, GetImageMetadata);

            if (duplicateCheck?.HasAnyDuplicateRisk == true)
            {
                foreach (var item in _previewItems.Where(item => duplicateCheck.RiskFilePaths.Contains(item.FilePath)))
                {
                    item.IsDuplicateRisk = true;
                    item.DuplicateReason = duplicateCheck.GetRiskReason(item.FilePath) ?? "可疑重复";
                }
            }

            SelectedFilesList.ItemsSource = null;
            SelectedFilesList.ItemsSource = _previewItems;
            TxtSelectedCount.Text = $"已选择 {_previewItems.Count} 张图片";
            TxtImportHint.Text = _previewItems.Count == 0
                ? "先选择图片；其他信息不确定就留空。"
                : $"将导入 {_previewItems.Count} 件衣服；可先在左侧逐件改名或移除。";

            ImageEmptyState.Visibility = _previewItems.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            ImageListState.Visibility = _previewItems.Count == 0 ? Visibility.Collapsed : Visibility.Visible;

            DuplicateWarningState.Visibility = duplicateCheck?.HasAnyDuplicateRisk == true
                ? Visibility.Visible
                : Visibility.Collapsed;
            TxtDuplicateWarning.Text = duplicateCheck?.HasAnyDuplicateRisk == true
                ? $"{duplicateCheck.Summary}；已标出 {duplicateCheck.RiskItemCount} 项，建议先移除或逐项确认。"
                : string.Empty;
            BtnRemoveDuplicateItems.Visibility = duplicateCheck?.HasAnyDuplicateRisk == true
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            HandlePanelError("刷新导入预览失败", ex);
        }
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

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_isSubmitting)
                return;

            if (_previewItems.Count == 0)
            {
                await ConfirmModal.ShowMessageAsync(
                    "还没有选择图片",
                    "先把要导入的衣服图片选进来。",
                    "可以点击“选择图片”，也可以直接把图片拖进这个面板里。",
                    confirmText: "去选图片");
                return;
            }

            var duplicateCheck = BatchImportDuplicateChecker.Analyze(_previewItems, _existingClothes, GetImageMetadata);
            if (duplicateCheck.HasAnyDuplicateRisk && !_awaitingDuplicateConfirmation)
            {
                _awaitingDuplicateConfirmation = true;
                ConfirmDuplicateState.Visibility = Visibility.Visible;
                ConfirmDuplicateDetail.Text = $"{duplicateCheck.Summary}。当前还有 {duplicateCheck.RiskItemCount} 项可疑重复。";
                return;
            }

            ConfirmDuplicateState.Visibility = Visibility.Collapsed;
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
        catch (Exception ex)
        {
            SetSubmitting(false);
            HandlePanelError("提交导入失败", ex);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        if (_isSubmitting)
            return;

        ConfirmDuplicateState.Visibility = Visibility.Collapsed;
        _awaitingDuplicateConfirmation = false;
        EditorCompleted?.Invoke(
            this,
            new EditorResult<BatchClothingImportRequest>(EditorResultType.Cancelled));
    }

    private void RemovePreviewItem_Click(object sender, RoutedEventArgs e)
    {
        if (_isSubmitting)
            return;

        try
        {
            if (sender is FrameworkElement { DataContext: BatchClothingImportPreviewItem item })
            {
                _previewItems.Remove(item);
                RefreshSelectedFiles();
            }
        }
        catch (Exception ex)
        {
            HandlePanelError("移除预览项失败", ex);
        }
    }

    private void ConfirmDuplicateContinue_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _awaitingDuplicateConfirmation = true;
            Save_Click(sender, e);
        }
        catch (Exception ex)
        {
            SetSubmitting(false);
            HandlePanelError("继续导入失败", ex);
        }
    }

    private void ConfirmDuplicateBack_Click(object sender, RoutedEventArgs e)
    {
        _awaitingDuplicateConfirmation = false;
        ConfirmDuplicateState.Visibility = Visibility.Collapsed;
    }

    private void RemoveDuplicateItems_Click(object sender, RoutedEventArgs e)
    {
        if (_isSubmitting || _previewItems.Count == 0)
            return;

        try
        {
            var duplicateCheck = BatchImportDuplicateChecker.Analyze(_previewItems, _existingClothes, GetImageMetadata);
            if (!duplicateCheck.HasAnyDuplicateRisk)
                return;

            _previewItems.RemoveAll(item => duplicateCheck.RiskFilePaths.Contains(item.FilePath));
            RefreshSelectedFiles();
            TxtImportHint.Text = _previewItems.Count == 0
                ? "可疑重复项已移除完，当前没有待导入图片。"
                : $"已移除 {duplicateCheck.RiskItemCount} 项可疑重复，还剩 {_previewItems.Count} 件可导入。";
        }
        catch (Exception ex)
        {
            HandlePanelError("移除可疑重复项失败", ex);
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
        _ = filePath;
        return BatchClothingImportBuilder.DefaultName;
    }

    private static (long Length, int Width, int Height)? GetImageMetadata(string path)
    {
        try
        {
            var resolved = File.Exists(path) ? path : ClothingImageLoader.ResolvePath(path, ImageVariant.Original);
            if (string.IsNullOrWhiteSpace(resolved) || !File.Exists(resolved))
                return null;

            var fileInfo = new FileInfo(resolved);
            var decoder = System.Windows.Media.Imaging.BitmapDecoder.Create(
                new Uri(resolved, UriKind.Absolute),
                System.Windows.Media.Imaging.BitmapCreateOptions.IgnoreColorProfile,
                System.Windows.Media.Imaging.BitmapCacheOption.OnLoad);
            var frame = decoder.Frames.FirstOrDefault();
            if (frame == null)
                return null;

            return (fileInfo.Length, frame.PixelWidth, frame.PixelHeight);
        }
        catch
        {
            return null;
        }
    }

    private void Card_MouseDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
    }

    private void HandlePanelError(string title, Exception ex)
    {
        var feedback = WardrobeActionErrorPresenter.ForImport(ex);
        ToastService.Instance.ShowError(title, feedback.Detail);
    }
}
