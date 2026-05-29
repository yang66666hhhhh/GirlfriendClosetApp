using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.UI.Logic.States;
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
    public void OutfitsTabState_SetRecentWornRecords_UpdatesHistorySummary()
    {
        var state = new OutfitsTabState();
        var outfit = new Outfit { Name = "Workday" };

        state.SetRecentWornRecords(
        [
            new OutfitWornRecord
            {
                WornDate = DateTime.Today,
                Outfit = outfit
            }
        ]);

        Assert.Single(state.RecentWornRecords);
        Assert.Equal("1 条最近记录", state.HistoryQuickText);
        Assert.Contains("最近 1 条穿着记录", state.HistorySummaryText);
    }

    [Fact]
    public void OutfitsTabState_SetCalendarRecords_BuildsCalendarAndSummary()
    {
        var state = new OutfitsTabState();
        var outfit = new Outfit { Name = "Date Night" };
        var currentMonthDate = state.CalendarMonth.AddDays(2);

        state.SetCalendarRecords(
        [
            new OutfitWornRecord
            {
                WornDate = currentMonthDate,
                Outfit = outfit
            }
        ]);

        Assert.Equal(42, state.CalendarDays.Count);
        Assert.Contains("本月 1 次记录", state.CalendarSummaryText);
        Assert.Contains(state.CalendarDays, day => day.Date.Date == currentMonthDate.Date && day.HasRecords);
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

    [Fact]
    public void TagsTabState_SetTags_ComputesCategorySummaryAndSortsTags()
    {
        var state = new TagsTabState();

        state.SetTags(
        [
            new Tag { Name = "约会", Color = "#FFFFFF", Category = TagCategory.Scene },
            new Tag { Name = "通勤", Color = "#FFFFFF", Category = TagCategory.Style },
            new Tag { Name = "极简", Color = "#FFFFFF", Category = TagCategory.Style }
        ]);

        Assert.Equal(3, state.TagCount);
        Assert.Equal("风格 2 · 场景 1", state.StyleCountText.Replace(" 个", ""));
        Assert.Equal("极简", state.Tags[0].Name);
        Assert.Equal("通勤", state.Tags[1].Name);
    }
}
