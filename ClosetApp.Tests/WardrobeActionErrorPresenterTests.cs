using System.IO;
using ClosetApp.UI.Services;
using Xunit;

namespace ClosetApp.Tests;

public class WardrobeActionErrorPresenterTests
{
    [Fact]
    public void ForImport_WithValidationError_PreservesUserFacingMessage()
    {
        var feedback = WardrobeActionErrorPresenter.ForImport(
            new InvalidOperationException("请选择要导入的图片。"));

        Assert.Equal("导入失败", feedback.Title);
        Assert.Contains("请选择要导入的图片", feedback.Detail);
    }

    [Fact]
    public void ForImport_WithFileLock_GivesFriendlyRetryGuidance()
    {
        var feedback = WardrobeActionErrorPresenter.ForImport(
            new IOException("The process cannot access the file because it is being used by another process."));

        Assert.Equal("导入失败", feedback.Title);
        Assert.Contains("别的程序占用", feedback.Detail);
    }

    [Fact]
    public void ForBatchClear_WithDatabaseLock_GivesBusyMessage()
    {
        var feedback = WardrobeActionErrorPresenter.ForBatchClear(
            new Exception("SQLite Error 5: 'database is locked'."));

        Assert.Equal("批量清空失败", feedback.Title);
        Assert.Contains("数据库正在忙", feedback.Detail);
    }

    [Fact]
    public void ForSingleDelete_WithGenericError_UsesClothingName()
    {
        var feedback = WardrobeActionErrorPresenter.ForSingleDelete(
            new Exception("boom"),
            "奶白外套");

        Assert.Equal("删除失败", feedback.Title);
        Assert.Contains("奶白外套", feedback.Detail);
    }
}
