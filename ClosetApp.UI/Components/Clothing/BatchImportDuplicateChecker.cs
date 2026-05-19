using System.IO;
using ClosetApp.Domain.Entities;

namespace ClosetApp.UI.Components.Clothing;

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

        foreach (var group in duplicateNameGroups)
        {
            foreach (var item in group.Skip(1))
                riskFilePaths.Add(item.FilePath);
        }

        foreach (var group in duplicateSignatureGroups)
        {
            foreach (var item in group.Skip(1))
                riskFilePaths.Add(item.Item.FilePath);
        }

        foreach (var item in selectedItems.Where(item => existingFileNameSet.Contains(item.FileName)))
            riskFilePaths.Add(item.FilePath);

        foreach (var item in itemMetadata.Where(item => item.Signature.HasValue && existingSignatures.Contains(item.Signature.Value)))
            riskFilePaths.Add(item.Item.FilePath);

        return new BatchImportDuplicateCheckResult(
            duplicateFileNameInSelection,
            duplicateSignatureInSelection,
            existingFileNameMatch,
            existingSignatureMatch,
            riskFilePaths);
    }
}
