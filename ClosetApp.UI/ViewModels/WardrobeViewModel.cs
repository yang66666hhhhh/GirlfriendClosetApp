using ClosetApp.Application.DTOs;
using ClosetApp.Application.Interfaces;
using ClosetApp.Application.UseCases.Clothing;
using ClosetApp.UI.Components.Clothing;
using ClosetApp.UI.Components.Shared;
using ClosetApp.UI.Components.Tags.Models;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.UI.States;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Serilog;

namespace ClosetApp.UI.ViewModels;

public partial class WardrobeViewModel : ViewModelBase
{
    private static readonly string[] StatePropertyNames =
    [
        nameof(AvailableTags),
        nameof(TagFilters),
        nameof(HasAvailableTags),
        nameof(AllClothes),
        nameof(FilteredClothes),
        nameof(DisplayedClothes),
        nameof(HasMoreClothes),
        nameof(IsLoading),
        nameof(IsEmpty),
        nameof(TotalCount),
        nameof(FilteredCount),
        nameof(SelectedType),
        nameof(ActiveQueueFilter),
        nameof(FilterSummary),
        nameof(FilterResultText),
        nameof(HasActiveFilters),
        nameof(CollectionSectionTitle),
        nameof(CollectionSectionBody),
        nameof(SelectedSeason),
        nameof(SelectedTagIds),
        nameof(FavoriteOnly),
        nameof(FilterHint),
        nameof(FilterToggleText),
        // Radio button properties - notified by EnumRadioGroup
        nameof(IsCategoryAllSelected),
        nameof(IsCategoryTopSelected),
        nameof(IsCategoryOuterwearSelected),
        nameof(IsCategoryBottomSelected),
        nameof(IsCategorySkirtSelected),
        nameof(IsCategoryDressSelected),
        nameof(IsCategoryShoesSelected),
        nameof(IsCategoryAccessorySelected),
        nameof(IsCategoryUnspecifiedSelected),
        nameof(IsSeasonAllSelected),
        nameof(IsSeasonSpringSelected),
        nameof(IsSeasonSummerSelected),
        nameof(IsSeasonAutumnSelected),
        nameof(IsSeasonWinterSelected),
        nameof(IsQueueAllSelected),
        nameof(IsQueueUnnamedSelected),
        nameof(IsQueueUncategorizedSelected),
        nameof(IsQueueUnseasonedSelected),
        nameof(IsQueueUntaggedSelected),
        nameof(IsQueueMissingBrandOrColorSelected),
        nameof(IsQueueRecentlyImportedSelected),
        nameof(QueueAllText),
        nameof(QueueUnnamedText),
        nameof(QueueUncategorizedText),
        nameof(QueueUnseasonedText),
        nameof(QueueUntaggedText),
        nameof(QueueMissingBrandOrColorText),
        nameof(QueueRecentlyImportedText),
        nameof(CanBatchCompleteCurrentQueue),
        nameof(CanClearCurrentCategory),
        nameof(ClearCurrentCategoryText),
        nameof(ActiveQueueLabel),
        nameof(ShowRecentImportSummary),
        nameof(RecentlyImportedCount),
        nameof(RecentlyImportedUnnamedCount),
        nameof(RecentlyImportedUncategorizedCount),
        nameof(RecentlyImportedUnseasonedCount),
        nameof(RecentlyImportedUntaggedCount),
        nameof(RecentlyImportedMissingBrandOrColorCount),
        nameof(RecentImportSummaryTitle),
        nameof(RecentImportSummaryBody)
    ];

    private readonly IClothingService _clothingService;
    private readonly ITagService _tagService;
    private readonly IImageStorageService _imageStorageService;
    private readonly CompleteClothingMetadataBatch _completeClothingMetadataBatch;
    private readonly ClearWardrobeByTypes _clearWardrobeByTypes;
    private readonly ImportClothesFromImages _importClothesFromImages;
    private readonly ClothesTabState _state = new();
    private IReadOnlyList<Tag> _availableTags = [];
    private readonly ObservableCollection<SelectableTag> _tagFilters = [];
    private bool _isSyncingTagFilters;

    private string _searchText = string.Empty;

    private bool _isFilterExpanded;
    private int _displayedClothingCount = 20;
    private const int PageSize = 20;

    public IReadOnlyList<Tag> AvailableTags => _availableTags;
    public ObservableCollection<SelectableTag> TagFilters => _tagFilters;
    public bool HasAvailableTags => _tagFilters.Count > 0;
    public IReadOnlyList<Clothing> AllClothes => _state.AllClothes;
    public IReadOnlyList<Clothing> FilteredClothes => _state.FilteredClothes;
    public IReadOnlyList<Clothing> DisplayedClothes => _state.FilteredClothes.Take(_displayedClothingCount).ToList();
    public bool HasMoreClothes => _state.FilteredClothes.Count > _displayedClothingCount;
    public bool IsLoading => _state.IsLoading;
    public bool IsEmpty => _state.IsEmpty;
    public int TotalCount => _state.AllClothes.Count;
    public int FilteredCount => _state.FilteredCount;
    public ClothingType? SelectedType => _state.SelectedType;
    public WardrobeQueueFilter? ActiveQueueFilter => _state.ActiveQueueFilter;

    public IReadOnlyList<WardrobeSortBy> SortOptions { get; } = Enum.GetValues<WardrobeSortBy>();

    public string GetSortLabel(WardrobeSortBy sort) => sort switch
    {
        WardrobeSortBy.Newest => "最新添加",
        WardrobeSortBy.Oldest => "最早添加",
        WardrobeSortBy.Name => "名称",
        WardrobeSortBy.Brand => "品牌",
        WardrobeSortBy.Type => "分类",
        WardrobeSortBy.FavoriteLevel => "收藏度",
        _ => sort.ToString()
    };
    public string FilterSummary => _state.FilterSummary;
    public string FilterResultText => $"{FilterSummary} · {FilteredCount} 件结果";
    public bool HasActiveFilters => _state.HasActiveFilters;
    public string CollectionSectionTitle => HasActiveFilters ? "当前结果" : "全部衣服";
    public string CollectionSectionBody => HasActiveFilters
        ? $"{FilterSummary}，现在一共筛出 {FilteredCount} 件。"
        : "悬停卡片可编辑、删除，或继续补齐待整理资料。";
    public Season? SelectedSeason => _state.SelectedSeason;
    public IReadOnlyCollection<Guid> SelectedTagIds => _state.SelectedTagIds;
    public bool FavoriteOnly
    {
        get => _state.FavoriteOnly;
        set
        {
            if (_state.FavoriteOnly == value)
                return;

            _state.SetFavoriteOnly(value);
            NotifyStateChanged();
        }
    }

    public WardrobeSortBy SortBy
    {
        get => _state.SortBy;
        set
        {
            if (_state.SortBy == value)
                return;

            _state.SetSortBy(value);
            NotifyStateChanged();
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (!SetProperty(ref _searchText, value))
                return;

            _state.SetSearchText(value);
            NotifyStateChanged();
        }
    }
    public bool IsFilterExpanded
    {
        get => _isFilterExpanded;
        private set
        {
            if (!SetProperty(ref _isFilterExpanded, value))
                return;

            OnPropertyChanged(nameof(FilterToggleText));
        }
    }
    public string FilterToggleText => IsFilterExpanded ? "收起筛选" : "展开筛选";
    public string FilterHint => HasActiveFilters
        ? "当前已应用组合筛选；点「清除」可以回到完整衣柜。"
        : "分类、季节、标签和收藏都可以叠加筛选。";

    // ── EnumRadioGroup：集中管理 RadioButton 选择状态 ──

    public EnumRadioGroup<ClothingType> CategoryFilter { get; }
    public EnumRadioGroup<Season> SeasonFilter { get; }
    public EnumRadioGroup<WardrobeQueueFilter> QueueFilter { get; }

    // ── 委托属性：保持 XAML 绑定兼容 ──

    public bool IsCategoryAllSelected
    {
        get => CategoryFilter.IsAllSelected;
        set => CategoryFilter.IsAllSelected = value;
    }
    public bool IsCategoryTopSelected
    {
        get => CategoryFilter.IsSelected(ClothingType.Top);
        set { if (value) CategoryFilter.Select(ClothingType.Top); }
    }
    public bool IsCategoryOuterwearSelected
    {
        get => CategoryFilter.IsSelected(ClothingType.Outerwear);
        set { if (value) CategoryFilter.Select(ClothingType.Outerwear); }
    }
    public bool IsCategoryBottomSelected
    {
        get => CategoryFilter.IsSelected(ClothingType.Bottom);
        set { if (value) CategoryFilter.Select(ClothingType.Bottom); }
    }
    public bool IsCategorySkirtSelected
    {
        get => CategoryFilter.IsSelected(ClothingType.Skirt);
        set { if (value) CategoryFilter.Select(ClothingType.Skirt); }
    }
    public bool IsCategoryDressSelected
    {
        get => CategoryFilter.IsSelected(ClothingType.Dress);
        set { if (value) CategoryFilter.Select(ClothingType.Dress); }
    }
    public bool IsCategoryShoesSelected
    {
        get => CategoryFilter.IsSelected(ClothingType.Shoes);
        set { if (value) CategoryFilter.Select(ClothingType.Shoes); }
    }
    public bool IsCategoryAccessorySelected
    {
        get => CategoryFilter.IsSelected(ClothingType.Accessory);
        set { if (value) CategoryFilter.Select(ClothingType.Accessory); }
    }
    public bool IsCategoryUnspecifiedSelected
    {
        get => CategoryFilter.IsSelected(ClothingType.Unspecified);
        set { if (value) CategoryFilter.Select(ClothingType.Unspecified); }
    }
    public bool IsSeasonAllSelected
    {
        get => SeasonFilter.IsAllSelected;
        set => SeasonFilter.IsAllSelected = value;
    }
    public bool IsSeasonSpringSelected
    {
        get => SeasonFilter.IsSelected(Season.Spring);
        set { if (value) SeasonFilter.Select(Season.Spring); }
    }
    public bool IsSeasonSummerSelected
    {
        get => SeasonFilter.IsSelected(Season.Summer);
        set { if (value) SeasonFilter.Select(Season.Summer); }
    }
    public bool IsSeasonAutumnSelected
    {
        get => SeasonFilter.IsSelected(Season.Autumn);
        set { if (value) SeasonFilter.Select(Season.Autumn); }
    }
    public bool IsSeasonWinterSelected
    {
        get => SeasonFilter.IsSelected(Season.Winter);
        set { if (value) SeasonFilter.Select(Season.Winter); }
    }
    public bool IsQueueAllSelected
    {
        get => QueueFilter.IsAllSelected;
        set => QueueFilter.IsAllSelected = value;
    }
    public bool IsQueueUnnamedSelected
    {
        get => QueueFilter.IsSelected(WardrobeQueueFilter.Unnamed);
        set { if (value) QueueFilter.Select(WardrobeQueueFilter.Unnamed); }
    }
    public bool IsQueueUncategorizedSelected
    {
        get => QueueFilter.IsSelected(WardrobeQueueFilter.Uncategorized);
        set { if (value) QueueFilter.Select(WardrobeQueueFilter.Uncategorized); }
    }
    public bool IsQueueUnseasonedSelected
    {
        get => QueueFilter.IsSelected(WardrobeQueueFilter.Unseasoned);
        set { if (value) QueueFilter.Select(WardrobeQueueFilter.Unseasoned); }
    }
    public bool IsQueueUntaggedSelected
    {
        get => QueueFilter.IsSelected(WardrobeQueueFilter.Untagged);
        set { if (value) QueueFilter.Select(WardrobeQueueFilter.Untagged); }
    }
    public bool IsQueueMissingBrandOrColorSelected
    {
        get => QueueFilter.IsSelected(WardrobeQueueFilter.MissingBrandOrColor);
        set { if (value) QueueFilter.Select(WardrobeQueueFilter.MissingBrandOrColor); }
    }
    public bool IsQueueRecentlyImportedSelected
    {
        get => QueueFilter.IsSelected(WardrobeQueueFilter.RecentlyImported);
        set { if (value) QueueFilter.Select(WardrobeQueueFilter.RecentlyImported); }
    }

    public string QueueAllText => "全部";
    public string QueueUnnamedText => BuildQueueText("未命名", WardrobeQueueFilter.Unnamed);
    public string QueueUncategorizedText => BuildQueueText("未分类", WardrobeQueueFilter.Uncategorized);
    public string QueueUnseasonedText => BuildQueueText("未设置季节", WardrobeQueueFilter.Unseasoned);
    public string QueueUntaggedText => BuildQueueText("无标签", WardrobeQueueFilter.Untagged);
    public string QueueMissingBrandOrColorText => BuildQueueText("无品牌/无颜色", WardrobeQueueFilter.MissingBrandOrColor);
    public string QueueRecentlyImportedText => BuildQueueText("刚导入", WardrobeQueueFilter.RecentlyImported);
    public bool CanBatchCompleteCurrentQueue => ActiveQueueFilter.HasValue && FilteredCount > 0;
    public bool CanClearCurrentCategory => SelectedType != null && FilteredCount > 0;
    public string ClearCurrentCategoryText => SelectedType switch
    {
        ClothingType.Top => "清空当前上衣",
        ClothingType.Outerwear => "清空当前外套",
        ClothingType.Bottom => "清空当前裤装",
        ClothingType.Skirt => "清空当前半裙",
        ClothingType.Dress => "清空当前连衣裙",
        ClothingType.Shoes => "清空当前鞋子",
        ClothingType.Accessory => "清空当前配饰",
        ClothingType.Unspecified => "清空当前待分类",
        _ => "清空当前分类"
    };
    public string ActiveQueueLabel => ActiveQueueFilter switch
    {
        WardrobeQueueFilter.Unnamed => "未命名",
        WardrobeQueueFilter.Uncategorized => "未分类",
        WardrobeQueueFilter.Unseasoned => "未设置季节",
        WardrobeQueueFilter.Untagged => "无标签",
        WardrobeQueueFilter.MissingBrandOrColor => "无品牌/无颜色",
        WardrobeQueueFilter.RecentlyImported => "刚导入",
        _ => "当前结果"
    };
    public bool ShowRecentImportSummary => ActiveQueueFilter == WardrobeQueueFilter.RecentlyImported && RecentlyImportedCount > 0;
    public int RecentlyImportedCount => _state.GetQueueCount(WardrobeQueueFilter.RecentlyImported);
    public int RecentlyImportedUnnamedCount => CountRecentlyImported(clothing =>
        string.IsNullOrWhiteSpace(clothing.Name) || clothing.Name == "未命名");
    public int RecentlyImportedUncategorizedCount => CountRecentlyImported(clothing => clothing.Type == ClothingType.Unspecified);
    public int RecentlyImportedUnseasonedCount => CountRecentlyImported(clothing => clothing.Season == Season.Unspecified);
    public int RecentlyImportedUntaggedCount => CountRecentlyImported(clothing => clothing.ClothingTags.Count == 0);
    public int RecentlyImportedMissingBrandOrColorCount => CountRecentlyImported(clothing =>
        string.IsNullOrWhiteSpace(clothing.Brand) || string.IsNullOrWhiteSpace(clothing.Color));
    public string RecentImportSummaryTitle => $"这批刚导入了 {RecentlyImportedCount} 件衣服";
    public string RecentImportSummaryBody => "先从未命名、未分类和未设置季节开始补，会最快把这批衣服整理到可用状态。";

    public WardrobeViewModel(
        IClothingService clothingService,
        ITagService tagService,
        IImageStorageService imageStorageService,
        CompleteClothingMetadataBatch completeClothingMetadataBatch,
        ClearWardrobeByTypes clearWardrobeByTypes,
        ImportClothesFromImages importClothesFromImages)
    {
        _clothingService = clothingService;
        _tagService = tagService;
        _imageStorageService = imageStorageService;
        _completeClothingMetadataBatch = completeClothingMetadataBatch;
        _clearWardrobeByTypes = clearWardrobeByTypes;
        _importClothesFromImages = importClothesFromImages;

        CategoryFilter = new EnumRadioGroup<ClothingType>(OnCategoryFilterChanged);
        SeasonFilter = new EnumRadioGroup<Season>(OnSeasonFilterChanged);
        QueueFilter = new EnumRadioGroup<WardrobeQueueFilter>(OnQueueFilterChanged);
    }

    // ── EnumRadioGroup 回调 ──

    private void OnCategoryFilterChanged(ClothingType? value)
    {
        _state.SetSelectedType(value);
        NotifyStateChanged();
    }

    private void OnSeasonFilterChanged(Season? value)
    {
        _state.SetSelectedSeason(value);
        NotifyStateChanged();
    }

    private void OnQueueFilterChanged(WardrobeQueueFilter? value)
    {
        _state.SetQueueFilter(value);
        NotifyStateChanged();
    }

    public async Task LoadClothesAsync()
    {
        _state.BeginLoad();
        NotifyStateChanged();

        try
        {
            if (_availableTags.Count == 0)
            {
                _availableTags = (await _tagService.GetStyleTagsAsync()).ToList();
                BuildTagFilters(_availableTags);
            }

            var clothes = await _clothingService.GetAllClothesAsync();
            _state.SetClothes(clothes);
            SyncTagFiltersFromState();
            Log.Debug("Loaded clothes. Total={TotalCount}, Filtered={FilteredCount}", TotalCount, FilteredCount);
        }
        finally
        {
            NotifyStateChanged();
        }
    }

    public void SetSelectedType(ClothingType? type)
    {
        CategoryFilter.Selected = type;
    }

    public void SetSelectedSeason(Season? season)
    {
        SeasonFilter.Selected = season;
    }

    public void SetQueueFilter(WardrobeQueueFilter? queueFilter)
    {
        QueueFilter.Selected = queueFilter;
    }

    public void ToggleTag(Guid tagId, bool isSelected)
    {
        var selected = _state.SelectedTagIds.ToHashSet();
        if (isSelected)
            selected.Add(tagId);
        else
            selected.Remove(tagId);

        _state.SetSelectedTagIds(selected);
        SyncTagFiltersFromState();
        NotifyStateChanged();
    }

    public void ClearFilters()
    {
        QueueFilter.Selected = null;
        CategoryFilter.Selected = null;
        SeasonFilter.Selected = null;
        _state.SetSelectedTagIds([]);
        _state.SetFavoriteOnly(false);
        SearchText = string.Empty;
        _displayedClothingCount = PageSize;
        SyncTagFiltersFromState();
        NotifyStateChanged();
    }

    public void LoadMoreClothes()
    {
        _displayedClothingCount += PageSize;
        NotifyStateChanged();
    }

    public void ToggleFilterExpanded() => IsFilterExpanded = !IsFilterExpanded;

    public async Task AddClothingAsync(Clothing clothing)
    {
        await _clothingService.AddClothingAsync(clothing);
        await LoadClothesAsync();
    }

    public async Task AddClothesAsync(IEnumerable<Clothing> clothes)
    {
        await _clothingService.AddClothesAsync(clothes);
        await LoadClothesAsync();
    }

    public async Task<BatchClothingImportResult> ImportClothesAsync(BatchClothingImportRequest request)
    {
        var result = await _importClothesFromImages.ExecuteAsync(request);
        ClearFilters();
        _state.SetRecentlyImportedClothingIds(result.Clothes.Select(clothing => clothing.Id));
        _state.SetQueueFilter(WardrobeQueueFilter.RecentlyImported);
        await LoadClothesAsync();
        return result;
    }

    public async Task<BatchClothingImportSummary> ImportClothesAndBuildSummaryAsync(BatchClothingImportRequest request)
    {
        var result = await ImportClothesAsync(request);
        return BatchClothingImportSummaryBuilder.Build(request, result.Clothes);
    }

    public async Task<BatchClothingCompletionResult> CompleteCurrentQueueAsync(BatchClothingCompletionRequest request)
    {
        var result = await _completeClothingMetadataBatch.ExecuteAsync(request);
        await LoadClothesAsync();
        return result;
    }

    public async Task<string> CompleteCurrentQueueAndBuildSuccessMessageAsync(BatchClothingCompletionRequest request)
    {
        var result = await CompleteCurrentQueueAsync(request);
        return $"已补全 {result.UpdatedCount} 件衣服";
    }

    public async Task<BatchWardrobeClearResult> ClearWardrobeByTypesAsync(BatchWardrobeClearRequest request)
    {
        var result = await _clearWardrobeByTypes.ExecuteAsync(request);
        await LoadClothesAsync();
        return result;
    }

    public async Task<string> ClearWardrobeByTypesAndBuildSuccessMessageAsync(BatchWardrobeClearRequest request)
    {
        var result = await ClearWardrobeByTypesAsync(request);
        return result.DeletedCount == 0
            ? "选中的分类里没有可清空的衣服。"
            : $"已清空 {result.DeletedCount} 件衣服";
    }

    public async Task UpdateClothingAsync(Clothing clothing, string? oldImagePath)
    {
        await _clothingService.UpdateClothingAsync(clothing);
        await DeleteReplacedImageAsync(oldImagePath, clothing.ImagePath);
        await LoadClothesAsync();
    }

    public async Task<string> UpdateFavoriteAsync(Clothing clothing)
    {
        await UpdateClothingAsync(clothing, clothing.ImagePath);
        return clothing.FavoriteLevel >= 4 ? $"已收藏「{clothing.Name}」" : $"已取消收藏「{clothing.Name}」";
    }

    public async Task DeleteClothingAsync(Clothing clothing)
    {
        Log.Information("Deleting clothing {ClothingId} ({ClothingName})", clothing.Id, clothing.Name);
        await _clothingService.DeleteClothingAsync(clothing.Id);
        await _imageStorageService.TryDeleteImageAsync(clothing.ImagePath);
        await LoadClothesAsync();
    }

    private async Task DeleteReplacedImageAsync(string? oldImagePath, string? newImagePath)
    {
        if (string.Equals(oldImagePath, newImagePath, StringComparison.OrdinalIgnoreCase))
            return;

        await _imageStorageService.TryDeleteImageAsync(oldImagePath);
    }

    private void NotifyStateChanged()
    {
        NotifyPropertiesChanged(StatePropertyNames);
    }

    private void BuildTagFilters(IEnumerable<Tag> tags)
    {
        foreach (var filter in _tagFilters)
            filter.PropertyChanged -= OnTagFilterPropertyChanged;

        _tagFilters.Clear();
        foreach (var tag in tags)
        {
            var filter = new SelectableTag(tag);
            filter.PropertyChanged += OnTagFilterPropertyChanged;
            _tagFilters.Add(filter);
        }

        SyncTagFiltersFromState();
    }

    private void OnTagFilterPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isSyncingTagFilters || e.PropertyName != nameof(SelectableTag.IsSelected))
            return;

        _state.SetSelectedTagIds(_tagFilters.Where(tag => tag.IsSelected).Select(tag => tag.Tag.Id));
        RefreshTagFilterOpacity();
        NotifyStateChanged();
    }

    private void SyncTagFiltersFromState()
    {
        _isSyncingTagFilters = true;
        var selectedIds = _state.SelectedTagIds.ToHashSet();
        foreach (var filter in _tagFilters)
            filter.IsSelected = selectedIds.Contains(filter.Tag.Id);

        RefreshTagFilterOpacity();
        _isSyncingTagFilters = false;
    }

    private void RefreshTagFilterOpacity()
    {
        var count = _tagFilters.Count(tag => tag.IsSelected);
        double opacity = count switch
        {
            <= 5 => 1.0,
            <= 8 => 0.82,
            _ => 0.58
        };

        foreach (var tag in _tagFilters.Where(tag => !tag.IsSelected))
            tag.UnselectedOpacity = opacity;
    }

    private string BuildQueueText(string label, WardrobeQueueFilter queueFilter)
    {
        var count = _state.GetQueueCount(queueFilter);
        return count > 0 ? $"{label} {count}" : label;
    }

    private int CountRecentlyImported(Func<Clothing, bool> predicate)
    {
        return _state.GetQueueItems(WardrobeQueueFilter.RecentlyImported).Count(predicate);
    }
}