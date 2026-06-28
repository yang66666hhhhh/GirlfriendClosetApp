using System.IO;
using ClosetApp.Application.Interfaces;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.File;

namespace ClosetApp.Infrastructure.Services;

/// <summary>
/// 监听用户切换事件，动态配置 Serilog 同时写入全局日志和用户子目录日志。
/// 全局日志保留启动、登录等系统级事件，用户操作日志写入 logs/{userId}/ 子目录。
/// </summary>
public sealed class UserLogConfigurator : IDisposable
{
    private const string OutputTemplate =
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}";

    private readonly ICurrentUserContext _currentUserContext;
    private Guid? _currentUserId;

    public UserLogConfigurator(ICurrentUserContext currentUserContext)
    {
        _currentUserContext = currentUserContext;
        _currentUserContext.CurrentUserChanged += OnCurrentUserChanged;
    }

    /// <summary>
    /// 当前登录用户的日志子目录（未登录时为 null）。
    /// </summary>
    public string? UserLogsDir =>
        _currentUserId.HasValue ? Path.Combine(AppPaths.LogsDir, _currentUserId.Value.ToString("N")) : null;

    private void OnCurrentUserChanged(object? sender, CurrentUserChangedEventArgs e)
    {
        var userId = e.UserId;
        _currentUserId = userId;

        if (userId != Guid.Empty)
        {
            var userLogDir = Path.Combine(AppPaths.LogsDir, userId.ToString("N"));
            Directory.CreateDirectory(userLogDir);
            ReconfigureLogger(userLogDir);
            Log.Information("Per-user logging configured for {UserId}", userId.ToString("N"));
        }
        else
        {
            ReconfigureGlobalOnly();
        }
    }

    private static void ReconfigureLogger(string userLogDir)
    {
        try
        {
            var newLogger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.File(
                    Path.Combine(AppPaths.LogsDir, "closet-.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 14,
                    shared: true,
                    outputTemplate: OutputTemplate)
                .WriteTo.File(
                    Path.Combine(userLogDir, "closet-.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 14,
                    shared: true,
                    outputTemplate: OutputTemplate)
                .CreateLogger();

            var oldLogger = Log.Logger;
            Log.Logger = newLogger;
            (oldLogger as IDisposable)?.Dispose();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to configure per-user logging");
        }
    }

    private static void ReconfigureGlobalOnly()
    {
        try
        {
            var newLogger = CreateGlobalLogger();
            var oldLogger = Log.Logger;
            Log.Logger = newLogger;
            (oldLogger as IDisposable)?.Dispose();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to reconfigure global-only logging");
        }
    }

    internal static ILogger CreateGlobalLogger()
    {
        return new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.File(
                Path.Combine(AppPaths.LogsDir, "closet-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                shared: true,
                outputTemplate: OutputTemplate)
            .CreateLogger();
    }

    public void Dispose()
    {
        _currentUserContext.CurrentUserChanged -= OnCurrentUserChanged;
    }
}
