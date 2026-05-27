using ClosetApp.Application.DTOs;

namespace ClosetApp.Application.Interfaces;

public interface IImageMaintenanceService
{
    Task<int> CountMissingImagesAsync();
    Task<int> CountMissingThumbnailsAsync();
    Task<ThumbnailRebuildResult> RebuildMissingThumbnailsAsync(int maxSize = 200);
    Task<int> RelinkMissingImagesAsync(string sourceDirectory);
    Task<OrphanOriginalsResult> AnalyzeOrphanOriginalsAsync();
    Task<OrphanOriginalsCleanupResult> CleanupOrphanOriginalsAsync();

    Task CleanupLogsAsync();
    Task CleanupImageCacheAsync();
    Task<int> CountFilesAsync(string directory);
    Task<long> GetDirectorySizeAsync(string directory);
}
