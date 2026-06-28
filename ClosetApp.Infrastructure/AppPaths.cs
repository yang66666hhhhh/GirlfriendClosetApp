using System;
using System.IO;

namespace ClosetApp.Infrastructure;

public static class AppPaths
{
    private static readonly string _baseDir;

    static AppPaths()
    {
        _baseDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ClosetApp");

        EnsureDirectories();
    }

    public static string BaseDir => _baseDir;

    public static string DatabasePath => Path.Combine(_baseDir, "closet.db");

    public static string LogsDir => Path.Combine(_baseDir, "logs");

    public static string BackupsDir => Path.Combine(_baseDir, "backups");

    public static string AiDir => Path.Combine(_baseDir, "ai");

    public static string AiProfileDir => Path.Combine(AiDir, "profile");

    public static string AiRendersDir => Path.Combine(AiDir, "renders");

    public static string AiRendersOriginalsDir => Path.Combine(AiRendersDir, "originals");

    public static string AiRendersDisplayDir => Path.Combine(AiRendersDir, "display");

    public static string AiRendersThumbnailsDir => Path.Combine(AiRendersDir, "thumbnails");

    private static void EnsureDirectories()
    {
        // 图片目录已按 LocalUserId 隔离到 images/{userId}/，由 ImageStorageService 按需创建；
        // AI 资产目录已按 LocalUserId 隔离到 ai/{userId}/，由 AiAssetStorageService 按需创建；
        // 这里不再创建全局 images 和 ai 子目录，避免留下空目录或重复资产。
        Directory.CreateDirectory(_baseDir);
        Directory.CreateDirectory(LogsDir);
        Directory.CreateDirectory(BackupsDir);
        Directory.CreateDirectory(AiDir);
    }
}
