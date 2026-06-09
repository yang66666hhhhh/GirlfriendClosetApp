using System.Windows;
using System.IO;
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
using System.Windows.Controls;
using System.Windows.Input;
using ClosetApp.UI.Views;

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
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        EventManager.RegisterClassHandler(typeof(ComboBox), UIElement.PreviewMouseWheelEvent, new MouseWheelEventHandler(OnComboBoxPreviewMouseWheel), true);
        var themeService = Services.GetRequiredService<ThemeService>();
        themeService.InitializeAsync().GetAwaiter().GetResult();
        StartBackgroundInitialization();
        ShowLoginWindow();
        Log.Information("Application startup prepared");
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
        services.AddTransient(sp => sp.GetRequiredService<IDbContextFactory<ClosetDbContext>>().CreateDbContext());

        services.AddTransient<ILocalUserRepository, LocalUserRepository>();
        services.AddTransient<IClothingRepository, ClothingRepository>();
        services.AddTransient<IOutfitRepository, OutfitRepository>();
        services.AddTransient<ITagRepository, TagRepository>();
        services.AddTransient<IFavoriteRepository, FavoriteRepository>();
        services.AddTransient<IOutfitWornRecordRepository, OutfitWornRecordRepository>();
        services.AddTransient<IPersonalProfileRepository, PersonalProfileRepository>();
        services.AddTransient<IOutfitGeneratedImageRepository, OutfitGeneratedImageRepository>();

        services.AddTransient<IClothingService, ClothingService>();
        services.AddTransient<IOutfitService, OutfitService>();
        services.AddTransient<ITagService, TagService>();
        services.AddTransient<IOutfitRecommendationService, OutfitRecommendationService>();
        services.AddTransient<IPersonalProfileService, PersonalProfileService>();
        services.AddTransient<ILocalUserService, LocalUserService>();
        services.AddTransient<GetWardrobeOverview>();
        services.AddTransient<CompleteClothingMetadataBatch>();
        services.AddTransient<ClearWardrobeByTypes>();
        services.AddTransient<ImportClothesFromImages>();
        services.AddTransient<GetOutfitHistorySummary>();
        services.AddTransient<GetWardrobeInsights>();
        services.AddTransient<GetAnnualOutfitReport>();
        services.AddTransient<RecordOutfitWorn>();
        services.AddTransient<GetRecommendationReadinessSummary>();
        services.AddTransient<GetTodayRecommendations>();
        services.AddTransient<GetTagsForSelection>();
        services.AddTransient<GetAiGenerationReadiness>();
        services.AddTransient<GetOutfitGeneratedImages>();
        services.AddTransient<SetPrimaryOutfitGeneratedImage>();
        services.AddTransient<DeleteOutfitGeneratedImage>();
        services.AddTransient<GenerateOutfitEffectImage>();
        services.AddTransient<SaveUploadedOutfitGeneratedImage>();
        services.AddMemoryCache();
        services.AddSingleton<IBackupService, BackupService>();
        services.AddSingleton<IImageMaintenanceService, ImageMaintenanceService>();
        services.AddSingleton<IImageStorageService, ImageStorageService>();
        services.AddSingleton<IImageAssetResolver, ImageAssetResolver>();
        services.AddSingleton<IAiAssetStorageService, AiAssetStorageService>();
        services.AddSingleton<IAuthSessionContext, AuthSessionContext>();
        services.AddSingleton<ICurrentUserContext>(sp =>
            new CurrentUserContext(authSessionContext: sp.GetRequiredService<IAuthSessionContext>()));
        services.AddTransient<ILocalAuthService, LocalAuthService>();
        services.AddSingleton<IAiGenerationPreferencesService>(sp =>
            new AiGenerationPreferencesService(currentUserContext: sp.GetRequiredService<ICurrentUserContext>()));
        services.AddSingleton<IWeatherPreferencesService>(sp =>
            new WeatherPreferencesService(currentUserContext: sp.GetRequiredService<ICurrentUserContext>()));
        services.AddSingleton<IRecommendationPreferencesService>(sp =>
            new RecommendationPreferencesService(currentUserContext: sp.GetRequiredService<ICurrentUserContext>()));
        services.AddSingleton<IAiImageGenerationService, OpenAiCompatibleImageGenerationService>();
        services.AddHttpClient<IWeatherService, WeatherService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("GirlfriendClosetApp/1.0");
        });
        services.AddSingleton<ToastService>();
        services.AddSingleton<ModalService>();
        services.AddSingleton(sp => new ThemePreferencesService(currentUserContext: sp.GetRequiredService<ICurrentUserContext>()));
        services.AddSingleton(sp => new OutfitDisplayPreferencesService(currentUserContext: sp.GetRequiredService<ICurrentUserContext>()));
        services.AddSingleton<ThemeService>();
        services.AddSingleton<AppStartupCoordinator>();

        services.AddTransient<ViewModels.MainViewModel>();
        services.AddTransient<ViewModels.WardrobeViewModel>();
        services.AddTransient<ViewModels.OutfitsViewModel>();
        services.AddTransient<ViewModels.TagsViewModel>();
        services.AddTransient<ViewModels.SettingsViewModel>();

        Services = services.BuildServiceProvider();
    }

    private void StartBackgroundInitialization()
    {
        try
        {
            _ = Services.GetRequiredService<AppStartupCoordinator>().EnsureStartedAsync();
            Log.Information("Application startup completed");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application startup failed");
            MessageBox.Show($"启动失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(-1);
        }
    }

    public void ShowLoginWindow()
    {
        var loginWindow = new LoginWindow();
        MainWindow = loginWindow;
        loginWindow.Show();
    }

    private static void OnComboBoxPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        ComboBoxWheelGuard.HandlePreviewMouseWheel(e);
    }
}
