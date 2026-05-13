using ClosetApp.Domain.Clothing;
using ClosetApp.Domain.Entities;

namespace ClosetApp.UI.States;

public sealed class ClothesTabState
{
    private List<Clothing> _allClothes = new();
    private IEnumerable<DisplayCategory>? _selectedCategories;
    private string _searchText = string.Empty;

    public IReadOnlyList<Clothing> AllClothes => _allClothes;
    public IReadOnlyList<Clothing> FilteredClothes { get; private set; } = [];
    public bool IsLoading { get; private set; }
    public bool IsEmpty => _allClothes.Count == 0;
    public int FilteredCount => FilteredClothes.Count;
    public bool HasActiveFilters => !string.IsNullOrWhiteSpace(_searchText) || _selectedCategories != null;
    public string FilterSummary
    {
        get
        {
            if (!HasActiveFilters)
                return "全部衣服";
            if (!string.IsNullOrWhiteSpace(_searchText) && _selectedCategories != null)
                return $"搜索「{_searchText}」+ 分类筛选";
            if (!string.IsNullOrWhiteSpace(_searchText))
                return $"搜索「{_searchText}」";
            return "分类筛选";
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

    private void ApplyFilter()
    {
        IEnumerable<Clothing> filtered = _allClothes;

        if (_selectedCategories != null)
        {
            filtered = filtered.Where(c => _selectedCategories.Any(cat => ResolveDisplayCategory(c) == cat));
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
