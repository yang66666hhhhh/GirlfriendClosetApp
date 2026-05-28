using System.Windows;
using System.IO;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using ClosetApp.Application.Interfaces;
using ClosetApp.Application.Images;
using ClosetApp.Application.Services;
using ClosetApp.Application.UseCases.Clothing;
using ClosetApp.Application.UseCases.Insights;
using ClosetApp.Application.UseCases.Outfits;
using ClosetApp.Application.UseCases.Tags;
using ClosetApp.Domain.Interfaces;
using ClosetApp.Infrastructure;
using ClosetApp.Infrastructure.Data;
using ClosetApp.Infrastructure.Repositories;
using ClosetApp.Infrastructure.Services;
using ClosetApp.UI.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;

namespace ClosetApp.UI;

public partial class App : System.Windows.Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    private bool _errorShown;

    protected override void OnStartup(StartupEventArgs e)
    {
        ConfigureLogging();
        base.OnStartup(e);
        Log.Information("Application starting");

        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            Log.Fatal(ex, "Unhandled AppDomain exception. IsTerminating={IsTerminating}", args.IsTerminating);
            MessageBox.Show($"发生错误: {ex?.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        };

        DispatcherUnhandledException += (s, args) =>
        {
            Log.Error(args.Exception, "Unhandled dispatcher exception");
            if (!_errorShown)
            {
                _errorShown = true;
                MessageBox.Show($"发生错误: {args.Exception.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            args.Handled = true;
        };

        TaskScheduler.UnobservedTaskException += (s, args) =>
        {
            Log.Error(args.Exception, "Unobserved task exception");
            if (!_errorShown)
            {
                _errorShown = true;
                MessageBox.Show($"发生错误: {args.Exception?.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            args.SetObserved();
        };

        ConfigureServices();
        Services.GetRequiredService<ThemeService>().InitializeAsync().GetAwaiter().GetResult();

        var dbContext = Services.GetRequiredService<ClosetDbContext>();
        Log.Information("Initializing database migration chain");
        ClosetDatabaseInitializer.InitializeAsync(dbContext).GetAwaiter().GetResult();
        Log.Information("Application startup completed");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("Application exiting with code {ExitCode}", e.ApplicationExitCode);
        Log.CloseAndFlush();
        base.OnExit(e);
    }

    private static void ConfigureLogging()
    {
        Directory.CreateDirectory(AppPaths.LogsDir);
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.File(
                Path.Combine(AppPaths.LogsDir, "closet-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                shared: true,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }

    private void ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddDbContextFactory<ClosetDbContext>();
        services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<ClosetDbContext>>().CreateDbContext());

        services.AddScoped<IClothingRepository, ClothingRepository>();
        services.AddScoped<IOutfitRepository, OutfitRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<IFavoriteRepository, FavoriteRepository>();
        services.AddScoped<IOutfitWornRecordRepository, OutfitWornRecordRepository>();

        services.AddScoped<IClothingService, ClothingService>();
        services.AddScoped<IOutfitService, OutfitService>();
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<IOutfitRecommendationService, OutfitRecommendationService>();
        services.AddScoped<GetWardrobeOverview>();
        services.AddScoped<CompleteClothingMetadataBatch>();
        services.AddScoped<ClearWardrobeByTypes>();
        services.AddScoped<ImportClothesFromImages>();
        services.AddScoped<GetOutfitHistorySummary>();
        services.AddScoped<GetWardrobeInsights>();
        services.AddScoped<RecordOutfitWorn>();
        services.AddScoped<GetRecommendationReadinessSummary>();
        services.AddScoped<GetTodayRecommendations>();
        services.AddScoped<GetTagsForSelection>();
        services.AddMemoryCache();
        services.AddSingleton<IBackupService, BackupService>();
        services.AddSingleton<IImageMaintenanceService, ImageMaintenanceService>();
        services.AddSingleton<IImageStorageService, ImageStorageService>();
        services.AddSingleton<IImageAssetResolver, ImageAssetResolver>();
        services.AddSingleton<IWeatherPreferencesService, WeatherPreferencesService>();
        services.AddSingleton<IRecommendationPreferencesService, RecommendationPreferencesService>();
        services.AddHttpClient<IWeatherService, WeatherService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("GirlfriendClosetApp/1.0");
        });
        services.AddSingleton<ToastService>();
        services.AddSingleton<ModalService>();
        services.AddSingleton<ThemePreferencesService>();
        services.AddSingleton<ThemeService>();

        services.AddTransient<ViewModels.MainViewModel>();
        services.AddTransient<ViewModels.WardrobeViewModel>();
        services.AddTransient<ViewModels.OutfitsViewModel>();
        services.AddTransient<ViewModels.TagsViewModel>();
        services.AddTransient<ViewModels.SettingsViewModel>();

        Services = services.BuildServiceProvider();
    }
}
