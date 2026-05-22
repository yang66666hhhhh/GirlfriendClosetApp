using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;

namespace ClosetApp.UI.States;

public enum WardrobeQueueFilter
{
    Unnamed,
    Uncategorized,
    Unseasoned,
    Untagged,
    MissingBrandOrColor,
    RecentlyImported
}

public enum WardrobeSortBy
{
    Newest,
    Oldest,
    Name,
    Brand,
    Type,
    FavoriteLevel
}

public sealed class ClothesTabState
{
    private List<Clothing> _allClothes = new();
    private ClothingType? _selectedType;
    private Season? _selectedSeason;
    private HashSet<Guid> _selectedTagIds = [];
    private HashSet<Guid> _recentlyImportedClothingIds = [];
    private WardrobeQueueFilter? _activeQueueFilter;
    private bool? _favoriteOnly;
    private string _searchText = string.Empty;
    private WardrobeSortBy _sortBy = WardrobeSortBy.Newest;

    public IReadOnlyList<Clothing> AllClothes => _allClothes;
    public IReadOnlyList<Clothing> FilteredClothes { get; private set; } = [];
    public bool IsLoading { get; private set; }
    public bool IsEmpty => _allClothes.Count == 0;
    public int FilteredCount => FilteredClothes.Count;
    public ClothingType? SelectedType => _selectedType;
    public Season? SelectedSeason => _selectedSeason;
    public IReadOnlyCollection<Guid> SelectedTagIds => _selectedTagIds;
    public WardrobeQueueFilter? ActiveQueueFilter => _activeQueueFilter;
    public bool FavoriteOnly => _favoriteOnly == true;
    public WardrobeSortBy SortBy => _sortBy;
    public bool HasActiveFilters =>
        !string.IsNullOrWhiteSpace(_searchText) ||
        _selectedType.HasValue ||
        _selectedSeason.HasValue ||
        _selectedTagIds.Count > 0 ||
        _activeQueueFilter.HasValue ||
        _favoriteOnly == true;

    public string FilterSummary
    {
        get
        {
            if (!HasActiveFilters)
                return "全部衣服";

            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(_searchText))
                parts.Add($"搜索「{_searchText}」");
            if (_selectedType.HasValue)
                parts.Add("分类");
            if (_selectedSeason.HasValue)
                parts.Add("季节");
            if (_selectedTagIds.Count > 0)
                parts.Add("标签");
            if (_activeQueueFilter.HasValue)
                parts.Add(GetQueueFilterName(_activeQueueFilter.Value));
            if (_favoriteOnly == true)
                parts.Add("收藏");

            return string.Join(" + ", parts);
        }
    }

    public void BeginLoad() => IsLoading = true;

    public void SetClothes(IEnumerable<Clothing> clothes)
    {
        _allClothes = clothes.ToList();
        IsLoading = false;
        ApplyFilter();
    }

    public void SetSearchText(string value)
    {
        _searchText = value.Trim();
        ApplyFilter();
    }

    public void SetSelectedType(ClothingType? type)
    {
        _selectedType = type;
        ApplyFilter();
    }

    public void SetSelectedSeason(Season? season)
    {
        _selectedSeason = season;
        ApplyFilter();
    }

    public void SetSelectedTagIds(IEnumerable<Guid> tagIds)
    {
        _selectedTagIds = tagIds.ToHashSet();
        ApplyFilter();
    }

    public void SetFavoriteOnly(bool favoriteOnly)
    {
        _favoriteOnly = favoriteOnly ? true : null;
        ApplyFilter();
    }

    public void SetSortBy(WardrobeSortBy sortBy)
    {
        _sortBy = sortBy;
        ApplyFilter();
    }

    public void SetQueueFilter(WardrobeQueueFilter? queueFilter)
    {
        _activeQueueFilter = queueFilter;
        ApplyFilter();
    }

    public void SetRecentlyImportedClothingIds(IEnumerable<Guid> clothingIds)
    {
        _recentlyImportedClothingIds = clothingIds.ToHashSet();
        ApplyFilter();
    }

    public int GetQueueCount(WardrobeQueueFilter queueFilter)
    {
        return _allClothes.Count(clothing => MatchesQueueFilter(clothing, queueFilter));
    }

    public IReadOnlyList<Clothing> GetQueueItems(WardrobeQueueFilter queueFilter)
    {
        return _allClothes
            .Where(clothing => MatchesQueueFilter(clothing, queueFilter))
            .ToList();
    }

    private void ApplyFilter()
    {
        IEnumerable<Clothing> filtered = _allClothes;

        if (_activeQueueFilter.HasValue)
        {
            filtered = filtered.Where(c => MatchesQueueFilter(c, _activeQueueFilter.Value));
        }

        if (_selectedType.HasValue)
        {
            filtered = filtered.Where(c => c.Type == _selectedType.Value);
        }

        if (_selectedSeason.HasValue)
        {
            filtered = filtered.Where(c => c.Season == _selectedSeason.Value || c.Season == Season.AllSeason);
        }

        if (_selectedTagIds.Count > 0)
        {
            filtered = filtered.Where(c => c.ClothingTags.Any(ct => _selectedTagIds.Contains(ct.TagId)));
        }

        if (_favoriteOnly == true)
        {
            filtered = filtered.Where(c => c.IsFavorite || c.FavoriteLevel >= 4);
        }

        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            filtered = filtered.Where(c =>
                c.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                c.Type.ToString().Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                c.Season.ToString().Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                (c.Color?.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (c.Brand?.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (c.Notes?.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                c.ClothingTags.Any(ct => ct.Tag.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase)));
        }

        FilteredClothes = ApplySorting(filtered).ToList();
    }

    private IEnumerable<Clothing> ApplySorting(IEnumerable<Clothing> items)
    {
        return _sortBy switch
        {
            WardrobeSortBy.Newest => items.OrderByDescending(c => c.CreatedAt),
            WardrobeSortBy.Oldest => items.OrderBy(c => c.CreatedAt),
            WardrobeSortBy.Name => items.OrderBy(c => c.Name ?? string.Empty),
            WardrobeSortBy.Brand => items.OrderBy(c => c.Brand ?? string.Empty),
            WardrobeSortBy.Type => items.OrderBy(c => c.Type),
            WardrobeSortBy.FavoriteLevel => items.OrderByDescending(c => c.FavoriteLevel),
            _ => items.OrderByDescending(c => c.CreatedAt)
        };
    }

    private bool MatchesQueueFilter(Clothing clothing, WardrobeQueueFilter queueFilter)
    {
        return queueFilter switch
        {
            WardrobeQueueFilter.Unnamed => string.IsNullOrWhiteSpace(clothing.Name) || clothing.Name == "未命名",
            WardrobeQueueFilter.Uncategorized => clothing.Type == ClothingType.Unspecified,
            WardrobeQueueFilter.Unseasoned => clothing.Season == Season.Unspecified,
            WardrobeQueueFilter.Untagged => clothing.ClothingTags.Count == 0,
            WardrobeQueueFilter.MissingBrandOrColor =>
                string.IsNullOrWhiteSpace(clothing.Brand) || string.IsNullOrWhiteSpace(clothing.Color),
            WardrobeQueueFilter.RecentlyImported => _recentlyImportedClothingIds.Contains(clothing.Id),
            _ => false
        };
    }

    private static string GetQueueFilterName(WardrobeQueueFilter queueFilter)
    {
        return queueFilter switch
        {
            WardrobeQueueFilter.Unnamed => "未命名",
            WardrobeQueueFilter.Uncategorized => "未分类",
            WardrobeQueueFilter.Unseasoned => "未设置季节",
            WardrobeQueueFilter.Untagged => "无标签",
            WardrobeQueueFilter.MissingBrandOrColor => "无品牌/无颜色",
            WardrobeQueueFilter.RecentlyImported => "刚导入",
            _ => "待整理"
        };
    }
}
