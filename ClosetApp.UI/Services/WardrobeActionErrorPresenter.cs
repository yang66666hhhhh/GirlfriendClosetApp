using System.IO;

namespace ClosetApp.UI.Services;

public static class WardrobeActionErrorPresenter
{
    public static (string Title, string Detail) ForImport(Exception exception)
    {
        if (TryGetValidationMessage(exception, out var validationMessage))
        {
            return ("导入失败", $"这批图片还没有写进衣柜。{validationMessage}");
        }

        if (IsDatabaseBusy(exception))
        {
            return ("导入失败", "衣柜数据库正在忙，暂时还没写入这批图片。先关闭另一处正在改衣柜的窗口，稍等一下再试。");
        }

        if (IsFileBusy(exception))
        {
            return ("导入失败", "有图片正在被别的程序占用，这批图片还没有写进衣柜。先关闭看图工具、同步盘或编辑器后再试。");
        }

        if (IsPermissionIssue(exception))
        {
            return ("导入失败", "当前没有权限读取或写入这批图片。先确认原图文件夹和应用数据目录可访问，再重新导入。");
        }

        return ("导入失败", "这批图片还没有写进衣柜。先检查原图是否还在、文件是否完整，再重新导入。");
    }

    public static (string Title, string Detail) ForBatchComplete(Exception exception)
    {
        if (TryGetValidationMessage(exception, out var validationMessage))
        {
            return ("批量补全失败", validationMessage);
        }

        if (IsDatabaseBusy(exception))
        {
            return ("批量补全失败", "这次没有改动现有衣服资料。数据库正在忙，先关闭另一处正在编辑衣柜的窗口，稍等一下再试。");
        }

        return ("批量补全失败", "这次没有改动现有衣服资料。可以先缩小当前结果范围，再重试。");
    }

    public static (string Title, string Detail) ForBatchClear(Exception exception)
    {
        if (TryGetValidationMessage(exception, out var validationMessage))
        {
            return ("批量清空失败", validationMessage);
        }

        if (IsDatabaseBusy(exception))
        {
            return ("批量清空失败", "衣柜内容还保留着。数据库正在忙，先关闭另一处正在改衣柜的窗口，稍等一下再重试。");
        }

        if (IsFileBusy(exception))
        {
            return ("批量清空失败", "衣柜记录可能已经在处理，但图片文件有一部分正被占用。先关闭看图工具或同步程序，再重新尝试清空。");
        }

        if (IsPermissionIssue(exception))
        {
            return ("批量清空失败", "衣柜内容还保留着。当前没有权限删除相关图片或数据，先确认文件夹权限后再重试。");
        }

        return ("批量清空失败", "衣柜内容还保留着。可以先关闭可能占用图片或数据库的程序，再重试。");
    }

    public static (string Title, string Detail) ForSingleDelete(Exception exception, string clothingName)
    {
        if (TryGetValidationMessage(exception, out var validationMessage))
        {
            return ("删除失败", validationMessage);
        }

        if (IsDatabaseBusy(exception))
        {
            return ("删除失败", $"「{clothingName}」还留在衣柜里。数据库正在忙，先稍等一下，再试一次。");
        }

        if (IsFileBusy(exception))
        {
            return ("删除失败", $"「{clothingName}」的图片可能正被别的程序占用。先关闭看图工具或同步程序，再试一次。");
        }

        return ("删除失败", $"「{clothingName}」还没有删掉。可以稍后再试一次。");
    }

    // Known validation failures should still surface directly because they are already user-facing.
    private static bool TryGetValidationMessage(Exception exception, out string message)
    {
        var candidate = Unwrap(exception);
        if (candidate is InvalidOperationException && !string.IsNullOrWhiteSpace(candidate.Message))
        {
            message = candidate.Message.Trim();
            return true;
        }

        message = string.Empty;
        return false;
    }

    private static bool IsDatabaseBusy(Exception exception)
    {
        var message = BuildSearchText(exception);
        return message.Contains("database is locked", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("sqlite error 5", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("database is busy", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("数据库", StringComparison.OrdinalIgnoreCase) && message.Contains("锁", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsFileBusy(Exception exception)
    {
        if (Unwrap(exception) is IOException)
            return true;

        var message = BuildSearchText(exception);
        return message.Contains("being used by another process", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("used by another process", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("cannot access the file", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("文件", StringComparison.OrdinalIgnoreCase) && message.Contains("占用", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPermissionIssue(Exception exception)
    {
        if (Unwrap(exception) is UnauthorizedAccessException)
            return true;

        var message = BuildSearchText(exception);
        return message.Contains("access to the path", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("access is denied", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("拒绝访问", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("没有权限", StringComparison.OrdinalIgnoreCase);
    }

    private static Exception Unwrap(Exception exception)
    {
        return exception.GetBaseException();
    }

    private static string BuildSearchText(Exception exception)
    {
        var current = exception;
        var messages = new List<string>();

        while (current != null)
        {
            messages.Add(current.Message);
            current = current.InnerException!;
        }

        return string.Join(" | ", messages);
    }
}
