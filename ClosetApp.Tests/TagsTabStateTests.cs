using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.UI.Logic.States;
using Xunit;

namespace ClosetApp.Tests;

public class TagsTabStateTests
{
    [Fact]
    public void SetTags_DefaultSort_KeepsCategoryGroupingAndAlphabeticalOrder()
    {
        var state = new TagsTabState();

        state.SetTags(
        [
            CreateTag("秋冬", TagCategory.Season),
            CreateTag("约会", TagCategory.Scene),
            CreateTag("通勤", TagCategory.Style),
            CreateTag("极简", TagCategory.Style)
        ]);

        Assert.True(new[] { "极简", "通勤", "约会", "秋冬" }.SequenceEqual(state.Tags.Select(tag => tag.Name)));
        Assert.True(new[] { "极简", "通勤" }.SequenceEqual(state.StyleTags.Select(tag => tag.Name)));
        Assert.True(new[] { "约会" }.SequenceEqual(state.SceneTags.Select(tag => tag.Name)));
        Assert.True(new[] { "秋冬" }.SequenceEqual(state.SeasonTags.Select(tag => tag.Name)));
    }

    [Fact]
    public void ApplyFilters_WithSearchAndCategory_UpdatesSummaryAndVisibleSections()
    {
        var state = new TagsTabState();

        state.SetTags(
        [
            CreateTag("通勤", TagCategory.Style),
            CreateTag("约会", TagCategory.Scene),
            CreateTag("冬季", TagCategory.Season)
        ]);

        state.SetSearchText("通");
        state.SetSelectedCategory(TagCategory.Style);

        var match = Assert.Single(state.Tags);
        Assert.Equal("通勤", match.Name);
        Assert.Equal("搜索“通” · 风格标签 · 1 个结果", state.FilterSummary);
        Assert.True(state.ShowStyleSection);
        Assert.False(state.ShowSceneSection);
        Assert.False(state.ShowSeasonSection);
    }

    [Fact]
    public void SetSortBy_LeastUsed_PrefersUnusedTagsInsideCategory()
    {
        var state = new TagsTabState();

        state.SetTags(
        [
            CreateTag("韩系", TagCategory.Style, usageCount: 2),
            CreateTag("极简", TagCategory.Style, usageCount: 0),
            CreateTag("通勤", TagCategory.Style, usageCount: 1),
            CreateTag("约会", TagCategory.Scene, usageCount: 0)
        ]);

        state.SetSortBy(TagSortBy.LeastUsed);

        Assert.True(new[] { "极简", "通勤", "韩系", "约会" }.SequenceEqual(state.Tags.Select(tag => tag.Name)));
        Assert.Equal("已用 2 · 待整理 2", state.UsageSummaryText);
    }

    private static Tag CreateTag(string name, TagCategory category, int usageCount = 0)
    {
        var tag = new Tag
        {
            Name = name,
            Color = "#FFFFFF",
            Category = category
        };

        for (var index = 0; index < usageCount; index++)
        {
            tag.ClothingTags.Add(new ClothingTag
            {
                ClothingId = Guid.NewGuid(),
                TagId = tag.Id
            });
        }

        return tag;
    }
}
