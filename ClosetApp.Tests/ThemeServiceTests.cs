using System.IO;
using System.Windows;
using ClosetApp.Application.Interfaces;
using ClosetApp.UI.Services;
using Xunit;

namespace ClosetApp.Tests;

public class ThemeServiceTests
{
    [Fact]
    public async Task ApplyFontSizeAsync_Large_UpdatesTypographyResources()
    {
        await RunStaAsync(() =>
        {
            EnsureApplicationResources();
            var service = new ThemeService(new ThemePreferencesService(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json")));

            service.InitializeAsync().GetAwaiter().GetResult();
            service.ApplyFontSizeAsync(AppFontSizeLevel.Large).GetAwaiter().GetResult();

            Assert.Equal(AppFontSizeLevel.Large, service.CurrentFontSizeLevel);
            Assert.Equal(16.2, System.Windows.Application.Current!.Resources["FontSize.Body"]);
            Assert.Equal(23.2, System.Windows.Application.Current.Resources["FontSize.PageTitle"]);
        });
    }

    [Fact]
    public async Task ApplyFontSizeAsync_DoesNotChangeCurrentTheme()
    {
        await RunStaAsync(() =>
        {
            EnsureApplicationResources();
            var service = new ThemeService(new ThemePreferencesService(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json")));

            service.InitializeAsync().GetAwaiter().GetResult();
            service.ApplyThemeAsync(AppThemeKind.Blue).GetAwaiter().GetResult();
            service.ApplyFontSizeAsync(AppFontSizeLevel.ExtraLarge).GetAwaiter().GetResult();

            Assert.Equal(AppThemeKind.Blue, service.CurrentTheme);
            Assert.Equal(AppFontSizeLevel.ExtraLarge, service.CurrentFontSizeLevel);
        });
    }

    [Fact]
    public async Task CurrentUserChanged_ReloadsUserScopedFontSize()
    {
        await RunStaAsync(() =>
        {
            EnsureApplicationResources();
            var baseDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            var globalFilePath = Path.Combine(baseDirectory, "theme-settings.json");
            var userId = Guid.NewGuid();
            var currentUserContext = new FakeCurrentUserContext();
            var preferencesService = new ThemePreferencesService(globalFilePath, currentUserContext);
            var service = new ThemeService(preferencesService, currentUserContext);

            preferencesService.SaveAsync(new ThemePreferences
            {
                Theme = AppThemeKind.Rose,
                FontSizeLevel = AppFontSizeLevel.Standard
            }).GetAwaiter().GetResult();

            currentUserContext.SetUserWithoutEvent(userId);
            preferencesService.SaveAsync(new ThemePreferences
            {
                Theme = AppThemeKind.Blue,
                FontSizeLevel = AppFontSizeLevel.Large
            }).GetAwaiter().GetResult();
            currentUserContext.ClearUserWithoutEvent();

            service.InitializeAsync().GetAwaiter().GetResult();
            Assert.Equal(AppFontSizeLevel.Standard, service.CurrentFontSizeLevel);

            currentUserContext.SetCurrentUserIdAsync(userId).GetAwaiter().GetResult();

            Assert.Equal(AppThemeKind.Blue, service.CurrentTheme);
            Assert.Equal(AppFontSizeLevel.Large, service.CurrentFontSizeLevel);
        });
    }

    private static void EnsureApplicationResources()
    {
        if (System.Windows.Application.Current == null)
            _ = new System.Windows.Application();

        var resources = System.Windows.Application.Current!.Resources;
        resources["FontSize.Hero"] = 28d;
        resources["FontSize.PageTitle"] = 20d;
        resources["FontSize.SectionTitle"] = 18d;
        resources["FontSize.Section"] = 11d;
        resources["FontSize.Label"] = 13d;
        resources["FontSize.Body"] = 14d;
        resources["FontSize.Input"] = 15d;
        resources["FontSize.Hint"] = 12d;
        resources["FontSize.Meta"] = 11d;
        resources["FontSize.Tiny"] = 10d;
        resources["Button.FontSize.Small"] = 12d;
        resources["Button.FontSize.Medium"] = 14d;
        resources["Button.FontSize.Large"] = 16d;
    }

    private static Task RunStaAsync(Action action)
    {
        var tcs = new TaskCompletionSource<object?>();
        var thread = new Thread(() =>
        {
            try
            {
                action();
                tcs.SetResult(null);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return tcs.Task;
    }

    private sealed class FakeCurrentUserContext : ICurrentUserContext
    {
        private Guid? _currentUserId;

        public event EventHandler<CurrentUserChangedEventArgs>? CurrentUserChanged;

        public Task<Guid?> GetCurrentUserIdAsync() => Task.FromResult(_currentUserId);

        public Task<Guid> GetRequiredCurrentUserIdAsync()
        {
            return Task.FromResult(_currentUserId ?? throw new InvalidOperationException("当前用户尚未初始化。"));
        }

        public Task<Guid> GetRequiredStoredUserIdAsync() => GetRequiredCurrentUserIdAsync();

        public Task SetCurrentUserIdAsync(Guid userId)
        {
            SetUserWithoutEvent(userId);
            CurrentUserChanged?.Invoke(this, new CurrentUserChangedEventArgs(userId));
            return Task.CompletedTask;
        }

        public void SetUserWithoutEvent(Guid userId)
        {
            _currentUserId = userId;
        }

        public void ClearUserWithoutEvent()
        {
            _currentUserId = null;
        }
    }
}
