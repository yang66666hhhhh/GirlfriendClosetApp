using System.IO;
using System.Reflection;
using System.Threading;
using ClosetApp.UI.Logic.Services;
using ClosetApp.UI.Services;
using Xunit;

namespace ClosetApp.Tests;

public class OutfitDisplayPreferencesServiceTests
{
    [Fact]
    public async Task GetAsync_WithMissingFile_ReturnsDefaultPreferences()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var service = new OutfitDisplayPreferencesService(Path.Combine(tempDir, "outfit-display-settings.json"));

            var result = await service.GetAsync();

            Assert.Equal(OutfitCardDisplayMode.OutfitFirst, result.DefaultCardDisplayMode);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task GetAsync_WithInvalidEnumValue_NormalizesToDefault()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var filePath = Path.Combine(tempDir, "outfit-display-settings.json");
            await File.WriteAllTextAsync(filePath, "{\n  \"DefaultCardDisplayMode\": 99\n}");
            var service = new OutfitDisplayPreferencesService(filePath);

            var result = await service.GetAsync();

            Assert.Equal(OutfitCardDisplayMode.OutfitFirst, result.DefaultCardDisplayMode);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_ThenGetAsync_PersistsDisplayMode()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var filePath = Path.Combine(tempDir, "outfit-display-settings.json");
            var service = new OutfitDisplayPreferencesService(filePath);

            await service.SaveAsync(new OutfitDisplayPreferences
            {
                DefaultCardDisplayMode = OutfitCardDisplayMode.EffectImageFirst
            });

            var result = await service.GetAsync();

            Assert.Equal(OutfitCardDisplayMode.EffectImageFirst, result.DefaultCardDisplayMode);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_RaisesPreferenceChangedOnCapturedSynchronizationContext()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var originalContext = SynchronizationContext.Current;
        try
        {
            var context = new RecordingSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(context);
            var service = new OutfitDisplayPreferencesService(Path.Combine(tempDir, "outfit-display-settings.json"));
            var gate = GetGate();
            await gate.WaitAsync();

            SynchronizationContext? callbackContext = null;
            service.PreferenceChanged += (_, _) => callbackContext = SynchronizationContext.Current;

            var saveTask = service.SaveAsync(new OutfitDisplayPreferences
            {
                DefaultCardDisplayMode = OutfitCardDisplayMode.EffectImageFirst
            });
            await Task.Delay(50);
            gate.Release();
            await saveTask;

            Assert.True(context.PostCallCount >= 1);
            Assert.Same(context, callbackContext);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(originalContext);
            Directory.Delete(tempDir, recursive: true);
        }
    }

    private sealed class RecordingSynchronizationContext : SynchronizationContext
    {
        public int PostCallCount { get; private set; }

        public override void Post(SendOrPostCallback d, object? state)
        {
            PostCallCount++;
            var previous = Current;
            try
            {
                SetSynchronizationContext(this);
                d(state);
            }
            finally
            {
                SetSynchronizationContext(previous);
            }
        }
    }

    private static SemaphoreSlim GetGate()
    {
        var field = typeof(OutfitDisplayPreferencesService).GetField("Gate", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return Assert.IsType<SemaphoreSlim>(field!.GetValue(null));
    }
}
