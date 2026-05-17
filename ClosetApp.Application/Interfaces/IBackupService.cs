using ClosetApp.Application.DTOs;

namespace ClosetApp.Application.Interfaces;

public interface IBackupService
{
    Task<BackupValidationResult> ValidateExportAsync(string filePath);
    Task<BackupExportResult> ExportAsync(string filePath);
    Task<BackupImportResult> ImportAsync(string filePath);
    Task<IReadOnlyList<BackupHistoryItem>> GetHistoryAsync(int maxCount = 8);
}
