using ClosetApp.Domain.Entities;

namespace ClosetApp.UI.States;

public sealed class OutfitsTabState
{
    private List<Outfit> _outfits = new();

    public IReadOnlyList<Outfit> Outfits => _outfits;
    public bool IsLoading { get; private set; }
    public bool IsEmpty => _outfits.Count == 0;

    public void BeginLoad() => IsLoading = true;

    public void SetOutfits(IEnumerable<Outfit> outfits)
    {
        _outfits = outfits.ToList();
        IsLoading = false;
    }
}
