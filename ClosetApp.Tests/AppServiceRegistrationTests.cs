using System.IO;
using Xunit;

namespace ClosetApp.Tests;

public class AppServiceRegistrationTests
{
    [Fact]
    public void DesktopDatabaseServices_DoNotUseRootScopedDbContext()
    {
        var code = File.ReadAllText(FindProjectFile("ClosetApp.UI/App.xaml.cs"));

        Assert.Contains("services.AddTransient(sp => sp.GetRequiredService<IDbContextFactory<ClosetDbContext>>().CreateDbContext());", code);
        Assert.DoesNotContain("services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<ClosetDbContext>>().CreateDbContext());", code);
        Assert.DoesNotContain("services.AddScoped<ILocalUserRepository", code);
        Assert.DoesNotContain("services.AddScoped<ILocalUserService", code);
        Assert.DoesNotContain("services.AddScoped<ILocalAuthService", code);
    }

    [Fact]
    public void NavigationSidebar_UserRefreshIsSerialized()
    {
        var code = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/NavigationSidebar.xaml.cs"));

        Assert.Contains("SemaphoreSlim _refreshUserGate", code);
        Assert.Contains("await _refreshUserGate.WaitAsync()", code);
        Assert.Contains("_refreshUserGate.Release()", code);
    }

    private static string FindProjectFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Cannot locate {relativePath} from {AppContext.BaseDirectory}.");
    }
}
