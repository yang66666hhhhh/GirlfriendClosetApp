using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using ClosetApp.Application.DTOs;
using ClosetApp.Domain.Enums;
using ClosetApp.UI.Components.Shared.Editor;

namespace ClosetApp.UI.Components.Clothing;

public partial class BatchWardrobeClearPanel : UserControl, IEditorPanel<BatchWardrobeClearRequest>
{
    private readonly ObservableCollection<CategoryOption> _categoryOptions = [];
    private bool _isSyncingSelectAll;
    private bool _isSubmitting;

    public event EventHandler<EditorResult<BatchWardrobeClearRequest>>? EditorCompleted;

    public BatchWardrobeClearPanel(
        IReadOnlyList<global::ClosetApp.Domain.Entities.Clothing> clothes,
        ClothingType? initialType = null)
    {
        InitializeComponent();

        foreach (var option in BuildCategoryOptions(clothes))
        {
            option.PropertyChanged += CategoryOption_PropertyChanged;
            _categoryOptions.Add(option);
        }

        CategoryList.ItemsSource = _categoryOptions;

        if (initialType.HasValue)
        {
            var option = _categoryOptions.FirstOrDefault(item => item.Type == initialType.Value);
            if (option != null)
                option.IsSelected = true;
        }

        RefreshSelectionState();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_isSubmitting)
            return;

        var selectedTypes = _categoryOptions
            .Where(option => option.IsSelected)
            .Select(option => option.Type)
            .ToList();

        if (selectedTypes.Count == 0)
        {
            ShowValidation("先选一个要清空的分类。");
            return;
        }

        if (ChkUnderstand.IsChecked != true)
        {
            ShowValidation("请先确认你知道这个操作不可撤销。");
            return;
        }

        HideValidation();
        _isSubmitting = true;
        BtnSave.IsEnabled = false;
        BtnCancel.IsEnabled = false;
        BtnClose.IsEnabled = false;
        CategoryList.IsEnabled = false;
        ChkSelectAll.IsEnabled = false;
        ChkUnderstand.IsEnabled = false;
        BtnSave.Content = "正在清空...";
        TxtFooterHint.Text = "正在删除衣服和图片，请稍等。";

        EditorCompleted?.Invoke(
            this,
            new EditorResult<BatchWardrobeClearRequest>(
                EditorResultType.Saved,
                new BatchWardrobeClearRequest(selectedTypes)));
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        if (_isSubmitting)
            return;

        EditorCompleted?.Invoke(
            this,
            new EditorResult<BatchWardrobeClearRequest>(EditorResultType.Cancelled));
    }

    private void SelectAll_Checked(object sender, RoutedEventArgs e)
    {
        if (_isSyncingSelectAll)
            return;

        var shouldSelectAll = ChkSelectAll.IsChecked == true;
        foreach (var option in _categoryOptions)
            option.IsSelected = shouldSelectAll;

        RefreshSelectionState();
    }

    private void Acknowledge_Checked(object sender, RoutedEventArgs e)
    {
        if (ChkUnderstand.IsChecked == true)
            HideValidation();
    }

    private void CategoryOption_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(CategoryOption.IsSelected))
            return;

        RefreshSelectionState();
    }

    private void RefreshSelectionState()
    {
        var selectedCount = _categoryOptions.Count(option => option.IsSelected);
        var selectedClothingCount = _categoryOptions
            .Where(option => option.IsSelected)
            .Sum(option => option.Count);
        var totalCount = _categoryOptions.Sum(option => option.Count);

        _isSyncingSelectAll = true;
        ChkSelectAll.IsChecked = selectedCount == _categoryOptions.Count && _categoryOptions.Count > 0;
        _isSyncingSelectAll = false;

        TxtSelectAllHint.Text = selectedCount == _categoryOptions.Count && _categoryOptions.Count > 0
            ? $"会一次清空全部 {totalCount} 件衣服。"
            : "不勾这里的话，就只会清空你选中的分类。";
        TxtSummary.Text = selectedCount == 0
            ? "会删除选中分类里的衣服，并一起清掉对应图片。"
            : $"当前会清空 {selectedClothingCount} 件衣服，覆盖 {selectedCount} 个分类。";
        TxtFooterHint.Text = selectedCount == 0
            ? "先选要清空的分类，再做最后确认。"
            : $"准备清空 {selectedClothingCount} 件衣服。";

        if (selectedCount > 0)
            HideValidation();
    }

    private void ShowValidation(string message)
    {
        TxtValidation.Text = message;
        TxtValidation.Visibility = Visibility.Visible;
    }

    private void HideValidation()
    {
        TxtValidation.Text = string.Empty;
        TxtValidation.Visibility = Visibility.Collapsed;
    }

    private static IEnumerable<CategoryOption> BuildCategoryOptions(IReadOnlyList<global::ClosetApp.Domain.Entities.Clothing> clothes)
    {
        return
        [
            new CategoryOption(ClothingType.Top, "上衣", Count(clothes, ClothingType.Top)),
            new CategoryOption(ClothingType.Bottom, "裤装", Count(clothes, ClothingType.Bottom)),
            new CategoryOption(ClothingType.Outerwear, "外套", Count(clothes, ClothingType.Outerwear)),
            new CategoryOption(ClothingType.Dress, "连衣裙", Count(clothes, ClothingType.Dress)),
            new CategoryOption(ClothingType.Skirt, "半裙", Count(clothes, ClothingType.Skirt)),
            new CategoryOption(ClothingType.Shoes, "鞋子", Count(clothes, ClothingType.Shoes)),
            new CategoryOption(ClothingType.Accessory, "配饰", Count(clothes, ClothingType.Accessory)),
            new CategoryOption(ClothingType.Unspecified, "待分类", Count(clothes, ClothingType.Unspecified))
        ];
    }

    private static int Count(IReadOnlyList<global::ClosetApp.Domain.Entities.Clothing> clothes, ClothingType type)
    {
        return clothes.Count(clothing => clothing.Type == type);
    }

    private sealed class CategoryOption : INotifyPropertyChanged
    {
        private bool _isSelected;

        public CategoryOption(ClothingType type, string label, int count)
        {
            Type = type;
            Label = label;
            Count = count;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ClothingType Type { get; }
        public string Label { get; }
        public int Count { get; }
        public string CountText => $"{Count} 件";

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value)
                    return;

                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }
    }
}
