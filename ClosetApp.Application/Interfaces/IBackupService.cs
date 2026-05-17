namespace ClosetApp.Application.Interfaces;

public interface IBackupService
{
    Task ExportAsync(string filePath);
    Task ImportAsync(string filePath);
}
