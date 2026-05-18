using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.Infrastructure.Services;
using ClosetApp.UI.Components.Shared.Editor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace ClosetApp.UI.Components.Clothing;

public partial class BatchClothingImportPanel : UserControl, IEditorPanel<IReadOnlyList<global::ClosetApp.Domain.Entities.Clothing>>
{
    private static readonly string[] ImageExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp"];

    private readonly IImageStorageService _imageStorage;
    private readonly ITagService _tagService;
    private readonly List<string> _selectedFiles = [];
    private ClothingType _selectedType = ClothingType.Unspecified;
    private Season _selectedSeason = Season.Unspecified;
    private int _favoriteLevel;

    public event EventHandler<EditorResult<IReadOnlyList<global::ClosetApp.Domain.Entities.Clothing>>>? EditorCompleted;

    public BatchClothingImportPanel()
    {
        InitializeComponent();
        _imageStorage = App.Services.GetRequiredService<IImageStorageService>();
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
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            return;

        var files = (string[]?)e.Data.GetData(DataFormats.FileDrop);
        if (files != null)
            SetSelectedFiles(files);

        e.Handled = true;
    }

    private void SetSelectedFiles(IEnumerable<string> files)
    {
        _selectedFiles.Clear();
        _selectedFiles.AddRange(files
            .Where(IsSupportedImage)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase));
        RefreshSelectedFiles();
    }

    private static bool IsSupportedImage(string file)
    {
        return File.Exists(file) &&
            ImageExtensions.Contains(Path.GetExtension(file).ToLowerInvariant(), StringComparer.OrdinalIgnoreCase);
    }

    private void RefreshSelectedFiles()
    {
        var fileNames = _selectedFiles.Select(Path.GetFileName).ToList();
        SelectedFilesList.ItemsSource = fileNames;
        TxtSelectedCount.Text = $"已选择 {_selectedFiles.Count} 张图片";
        TxtImportHint.Text = _selectedFiles.Count == 0
            ? "先选择图片；其他信息不确定就留空。"
            : $"将导入 {_selectedFiles.Count} 件衣服，名称为“未命名”，未填写的信息保持待整理。";

        ImageEmptyState.Visibility = _selectedFiles.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        ImageListState.Visibility = _selectedFiles.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
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
        if (_selectedFiles.Count == 0)
        {
            MessageBox.Show("先选择要导入的图片。", "批量导入", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var options = new BatchClothingImportOptions(
            _selectedType,
            _selectedSeason,
            TxtColor.Text,
            TxtBrand.Text,
            TxtNotes.Text,
            _favoriteLevel,
            TagSelection.SelectedTags.ToList());

        var clothes = new List<global::ClosetApp.Domain.Entities.Clothing>();
        foreach (var file in _selectedFiles)
        {
            var storedImagePath = await _imageStorage.SaveImageAsync(file);
            clothes.Add(BatchClothingImportBuilder.CreateClothing(storedImagePath, options));
        }

        EditorCompleted?.Invoke(
            this,
            new EditorResult<IReadOnlyList<global::ClosetApp.Domain.Entities.Clothing>>(
                EditorResultType.Saved,
                clothes));
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        EditorCompleted?.Invoke(
            this,
            new EditorResult<IReadOnlyList<global::ClosetApp.Domain.Entities.Clothing>>(EditorResultType.Cancelled));
    }

    private void Card_MouseDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
    }
}
