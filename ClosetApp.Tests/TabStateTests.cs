using ClosetApp.Domain.Entities;
using ClosetApp.UI.States;
using Xunit;

namespace ClosetApp.Tests;

public class TabStateTests
{
    [Fact]
    public void OutfitsTabState_SetOutfits_UpdatesListAndLoadFlag()
    {
        var state = new OutfitsTabState();
        state.BeginLoad();

        state.SetOutfits([new Outfit { Name = "Workday" }]);

        Assert.False(state.IsLoading);
        Assert.False(state.IsEmpty);
        Assert.Single(state.Outfits);
    }

    [Fact]
    public void TagsTabState_SetTags_UpdatesListAndLoadFlag()
    {
        var state = new TagsTabState();
        state.BeginLoad();

        state.SetTags([new Tag { Name = "约会", Color = "#FFFFFF" }]);

        Assert.False(state.IsLoading);
        Assert.False(state.IsEmpty);
        Assert.Single(state.Tags);
    }
}
