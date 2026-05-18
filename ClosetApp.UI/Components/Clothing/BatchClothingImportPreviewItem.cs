namespace ClosetApp.UI.Components.Clothing;

public sealed class BatchClothingImportPreviewItem
{
    public BatchClothingImportPreviewItem(string filePath, string fileName, string name)
    {
        FilePath = filePath;
        FileName = fileName;
        Name = name;
    }

    public string FilePath { get; }
    public string FileName { get; }
    public string Name { get; set; }
}
