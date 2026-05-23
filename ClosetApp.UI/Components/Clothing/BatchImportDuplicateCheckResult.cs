namespace ClosetApp.UI.Components.Clothing;

public sealed record BatchImportDuplicateCheckResult(
    bool HasDuplicateFileNameInSelection,
    bool HasDuplicateSignatureInSelection,
    bool HasExistingFileNameMatch,
    bool HasExistingSignatureMatch,
    IReadOnlySet<string> RiskFilePaths,
    IReadOnlyDictionary<string, string> RiskReasons)
{
    public bool HasAnyDuplicateRisk =>
        HasDuplicateFileNameInSelection ||
        HasDuplicateSignatureInSelection ||
        HasExistingFileNameMatch ||
        HasExistingSignatureMatch;

    public string Summary
    {
        get
        {
            var parts = new List<string>();
            if (HasDuplicateFileNameInSelection)
                parts.Add("当前选择里有同文件名");
            if (HasDuplicateSignatureInSelection)
                parts.Add("当前选择里有同尺寸和文件大小");
            if (HasExistingFileNameMatch)
                parts.Add("衣柜里已有同文件名图片");
            if (HasExistingSignatureMatch)
                parts.Add("衣柜里已有同尺寸和文件大小图片");

            return parts.Count == 0
                ? "没有发现明显重复"
                : string.Join("；", parts);
        }
    }

    public int RiskItemCount => RiskFilePaths.Count;

    public string? GetRiskReason(string filePath)
    {
        return RiskReasons.TryGetValue(filePath, out var reason)
            ? reason
            : null;
    }
}
