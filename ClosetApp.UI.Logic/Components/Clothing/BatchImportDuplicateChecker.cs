using System.IO;
using ClosetApp.Domain.Entities;

namespace ClosetApp.UI.Logic.Components.Clothing;

public static class BatchImportDuplicateChecker
{
    public static BatchImportDuplicateCheckResult Analyze(
        IEnumerable<BatchClothingImportPreviewItem> previewItems,
        IEnumerable<global::ClosetApp.Domain.Entities.Clothing> existingClothes,
        Func<string, (long Length, int Width, int Height)?> getImageMetadata)
    {
        var selectedItems = previewItems.ToList();
        var existingItems = existingClothes
            .Where(clothing => !string.IsNullOrWhiteSpace(clothing.ImagePath))
            .Select(clothing => clothing.ImagePath!)
            .ToList();

        var duplicateNameGroups = selectedItems
            .GroupBy(item => item.FileName, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .ToList();
        var duplicateFileNameInSelection = duplicateNameGroups.Count > 0;

        var itemMetadata = selectedItems
            .Select(item => new
            {
                Item = item,
                Signature = getImageMetadata(item.FilePath)
            })
            .ToList();

        var selectionSignatures = itemMetadata
            .Select(item => item.Signature)
            .Where(metadata => metadata.HasValue)
            .Select(metadata => metadata!.Value)
            .ToList();
        var duplicateSignatureGroups = itemMetadata
            .Where(item => item.Signature.HasValue)
            .GroupBy(item => item.Signature!.Value)
            .Where(group => group.Count() > 1)
            .ToList();
        var duplicateSignatureInSelection = duplicateSignatureGroups.Count > 0;

        var selectedFileNames = selectedItems
            .Select(item => item.FileName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var existingFileNameSet = existingItems
            .Select(Path.GetFileName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var existingFileNameMatch = selectedFileNames.Any(existingFileNameSet.Contains);

        var existingSignatures = existingItems
            .Select(getImageMetadata)
            .Where(metadata => metadata.HasValue)
            .Select(metadata => metadata!.Value)
            .ToHashSet();
        var existingSignatureMatch = selectionSignatures.Any(existingSignatures.Contains);

        var riskFilePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var riskReasons = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in duplicateNameGroups)
        {
            foreach (var item in group.Skip(1))
                AddRisk(item.FilePath, "本批里有同文件名", riskFilePaths, riskReasons);
        }

        foreach (var group in duplicateSignatureGroups)
        {
            foreach (var item in group.Skip(1))
                AddRisk(item.Item.FilePath, "本批里有同尺寸/大小", riskFilePaths, riskReasons);
        }

        foreach (var item in selectedItems.Where(item => existingFileNameSet.Contains(item.FileName)))
            AddRisk(item.FilePath, "衣柜已有同文件名", riskFilePaths, riskReasons);

        foreach (var item in itemMetadata.Where(item => item.Signature.HasValue && existingSignatures.Contains(item.Signature.Value)))
            AddRisk(item.Item.FilePath, "衣柜已有同尺寸/大小", riskFilePaths, riskReasons);

        return new BatchImportDuplicateCheckResult(
            duplicateFileNameInSelection,
            duplicateSignatureInSelection,
            existingFileNameMatch,
            existingSignatureMatch,
            riskFilePaths,
            riskReasons.ToDictionary(
                pair => pair.Key,
                pair => string.Join("；", pair.Value),
                StringComparer.OrdinalIgnoreCase));
    }

    private static void AddRisk(
        string filePath,
        string reason,
        HashSet<string> riskFilePaths,
        Dictionary<string, List<string>> riskReasons)
    {
        riskFilePaths.Add(filePath);
        if (!riskReasons.TryGetValue(filePath, out var reasons))
        {
            reasons = [];
            riskReasons[filePath] = reasons;
        }

        if (!reasons.Contains(reason))
            reasons.Add(reason);
    }
}
