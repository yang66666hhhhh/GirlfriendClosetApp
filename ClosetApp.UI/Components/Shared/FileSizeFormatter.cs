namespace ClosetApp.UI.Components.Shared;

public static class FileSizeFormatter
{
    private static readonly string[] Units = ["B", "KB", "MB", "GB"];

    public static string Format(long bytes)
    {
        var size = (double)bytes;
        var unitIndex = 0;
        while (size >= 1024 && unitIndex < Units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }
        return $"{size:0.#} {Units[unitIndex]}";
    }
}