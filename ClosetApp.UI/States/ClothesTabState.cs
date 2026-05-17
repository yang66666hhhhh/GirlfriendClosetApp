using ClosetApp.Domain.Clothing;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;

namespace ClosetApp.UI.States;

public sealed class ClothesTabState
{
    private List<Clothing> _allClothes = new();
    private IEnumerable<DisplayCategory>? _selectedCategories;
    private Season? _selectedSeason;
    private HashSet<Guid> _selectedTagIds = [];
    private bool? _favoriteOnly;
    private string _searchText = string.Empty;

    public IReadOnlyList<Clothing> AllClothes => _allClothes;
    public IReadOnlyList<Clothing> FilteredClothes { get; private set; } = [];
    public bool IsLoading { get; private set; }
    public bool IsEmpty => _allClothes.Count == 0;
    public int FilteredCount => FilteredClothes.Count;
    public Season? SelectedSeason => _selectedSeason;
    public IReadOnlyCollection<Guid> SelectedTagIds => _selectedTagIds;
    public bool FavoriteOnly => _favoriteOnly == true;
    public bool HasActiveFilters =>
        !string.IsNullOrWhiteSpace(_searchText) ||
        _selectedCategories != null ||
        _selectedSeason.HasValue ||
        _selectedTagIds.Count > 0 ||
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
            if (_selectedCategories != null)
                parts.Add("分类");
            if (_selectedSeason.HasValue)
                parts.Add("季节");
            if (_selectedTagIds.Count > 0)
                parts.Add("标签");
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

    public void SetSelectedCategories(IEnumerable<DisplayCategory>? categories)
    {
        _selectedCategories = categories;
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

    private void ApplyFilter()
    {
        IEnumerable<Clothing> filtered = _allClothes;

        if (_selectedCategories != null)
        {
            filtered = filtered.Where(c => _selectedCategories.Any(cat => ResolveDisplayCategory(c) == cat));
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

        FilteredClothes = filtered.ToList();
    }

    private static DisplayCategory ResolveDisplayCategory(Clothing clothing)
    {
        if (clothing.GarmentType.HasValue)
            return ClothingMappings.GetDisplayCategory(clothing.GarmentType.Value);
        return ClothingMappings.GetDisplayCategory(ClothingMappings.InferGarmentType(clothing.Type));
    }
}
