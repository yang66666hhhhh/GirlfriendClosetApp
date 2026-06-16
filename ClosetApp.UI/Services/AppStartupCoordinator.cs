using ClosetApp.Infrastructure.Data;
using ClosetApp.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace ClosetApp.UI.Services;

/// <summary>
/// 协调应用的延后启动准备，优先让主窗口显示，再完成数据库可用性初始化。
/// </summary>
public sealed class AppStartupCoordinator
{
    private readonly IServiceProvider _services;
    private readonly TaskCompletionSource _startupReadyTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private Task? _startupTask;

    public AppStartupCoordinator(IServiceProvider services)
    {
        _services = services;
    }

    public bool IsReady => _startupReadyTcs.Task.IsCompletedSuccessfully;

    public Task EnsureStartedAsync()
    {
        _startupTask ??= Task.Run(InitializeCoreAsync);
        return _startupTask;
    }

    public async Task WaitUntilReadyAsync(CancellationToken cancellationToken = default)
    {
        await EnsureStartedAsync();
        await _startupReadyTcs.Task.WaitAsync(cancellationToken);
    }

    private async Task InitializeCoreAsync()
    {
        try
        {
            using var scope = _services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ClosetDbContext>();

            Log.Information("Initializing database migration chain in background");
            await ClosetDatabaseInitializer.InitializeAsync(dbContext, CancellationToken.None).ConfigureAwait(false);
            await scope.ServiceProvider.GetRequiredService<ILocalUserService>().EnsureInitializedAsync().ConfigureAwait(false);

            // 一次性迁移：将旧的全局图片目录文件复制到当前用户的隔离目录。
            var imageStorage = scope.ServiceProvider.GetRequiredService<IImageStorageService>();
            await imageStorage.MigrateGlobalImagesAsync().ConfigureAwait(false);

            var aiAssetStorage = scope.ServiceProvider.GetService<IAiAssetStorageService>();
            if (aiAssetStorage != null)
                await aiAssetStorage.MigrateGlobalAiAssetsAsync().ConfigureAwait(false);

            // 配置静态图片加载器使用用户作用域目录（优先查找用户目录，回退到全局目录）。
            ClothingImageLoader.Configure(
                imageStorage.GetOriginalsDirectory(),
                imageStorage.GetDisplayDirectory(),
                imageStorage.GetThumbnailsDirectory());

            if (aiAssetStorage != null)
            {
                OutfitGeneratedImageDisplayHelper.Configure(
                    aiAssetStorage.GetAiRendersDisplayDirectory(),
                    aiAssetStorage.GetAiRendersThumbnailsDirectory());
            }

            Log.Information("Background startup initialization completed");
            _startupReadyTcs.TrySetResult();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Background startup initialization failed");
            _startupReadyTcs.TrySetException(ex);
            throw;
        }
    }
}
