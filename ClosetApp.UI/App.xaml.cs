using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using ClosetApp.Application.Interfaces;
using ClosetApp.Application.Services;
using ClosetApp.Domain.Interfaces;
using ClosetApp.Infrastructure.Data;
using ClosetApp.Infrastructure.Repositories;
using ClosetApp.Infrastructure.Services;
using ClosetApp.UI.Services;

namespace ClosetApp.UI;

public partial class App : System.Windows.Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    private bool _errorShown;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            MessageBox.Show($"发生错误: {ex?.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        };

        DispatcherUnhandledException += (s, args) =>
        {
            if (!_errorShown)
            {
                _errorShown = true;
                MessageBox.Show($"发生错误: {args.Exception.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            args.Handled = true;
        };

        TaskScheduler.UnobservedTaskException += (s, args) =>
        {
            if (!_errorShown)
            {
                _errorShown = true;
                MessageBox.Show($"发生错误: {args.Exception?.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            args.SetObserved();
        };

        ConfigureServices();

        var dbContext = Services.GetRequiredService<ClosetDbContext>();
        dbContext.Database.EnsureCreated();
    }

    private void ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddDbContext<ClosetDbContext>();

        services.AddScoped<IClothingRepository, ClothingRepository>();
        services.AddScoped<IOutfitRepository, OutfitRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<IFavoriteRepository, FavoriteRepository>();
        services.AddScoped<IOutfitWornRecordRepository, OutfitWornRecordRepository>();

        services.AddScoped<IClothingService, ClothingService>();
        services.AddScoped<IOutfitService, OutfitService>();
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<IOutfitRecommendationService, OutfitRecommendationService>();
        services.AddSingleton<IImageStorageService, ImageStorageService>();
        services.AddSingleton<IWeatherService, WeatherService>();
        services.AddSingleton<ToastService>();
        services.AddSingleton<ModalService>();

        services.AddTransient<ViewModels.MainViewModel>();
        services.AddTransient<ViewModels.WardrobeViewModel>();
        services.AddTransient<ViewModels.HomeViewModel>();

        Services = services.BuildServiceProvider();
    }
}