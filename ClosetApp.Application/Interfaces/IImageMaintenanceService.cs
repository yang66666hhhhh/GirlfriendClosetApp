namespace ClosetApp.Application.Interfaces;

public interface IImageMaintenanceService
{
    Task<int> CountMissingImagesAsync();
    Task<int> RelinkMissingImagesAsync(string sourceDirectory);
}
