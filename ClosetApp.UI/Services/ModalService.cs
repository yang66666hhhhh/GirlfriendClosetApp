using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ClosetApp.UI.Services;

public class ModalService
{
    private static readonly Lazy<ModalService> _instance = new(() => new ModalService());
    public static ModalService Instance => _instance.Value;

    private readonly Dictionary<Type, Func<UserControl>> _registry = new();
    private readonly Dictionary<Type, UserControl> _cachedViews = new();
    private readonly Stack<UserControl> _modalStack = new();

    public event Action<UserControl>? ModalShowRequested;
    public event Action? ModalHideRequested;

    public void Register<T>(Func<UserControl> factory) where T : UserControl
    {
        _registry[typeof(T)] = factory;
    }

    public void Show<T>() where T : UserControl
    {
        if (_registry.TryGetValue(typeof(T), out var factory))
            Show(factory());
    }

    public void ShowCached<T>() where T : UserControl, new()
    {
        Show(GetOrCreateCachedView<T>());
    }

    public void PrewarmCached<T>() where T : UserControl, new()
    {
        _ = GetOrCreateCachedView<T>();
    }

    public void Show(UserControl content)
    {
        _modalStack.Push(content);
        ModalShowRequested?.Invoke(content);
        ActivateContentWhenReady(content);
    }

    public void Hide()
    {
        if (_modalStack.Count == 0)
            return;

        _modalStack.Pop();
        if (_modalStack.TryPeek(out var previousContent))
        {
            ModalShowRequested?.Invoke(previousContent);
            return;
        }

        ModalHideRequested?.Invoke();
    }

    private T GetOrCreateCachedView<T>() where T : UserControl, new()
    {
        if (_cachedViews.TryGetValue(typeof(T), out var cached))
            return (T)cached;

        var created = new T();
        _cachedViews[typeof(T)] = created;
        return created;
    }

    private static void ActivateContentWhenReady(UserControl content)
    {
        if (content is not IModalActivationAware activationAware)
            return;

        var dispatcher = global::System.Windows.Application.Current?.Dispatcher ?? content.Dispatcher;
        dispatcher.BeginInvoke(
            DispatcherPriority.Loaded,
            new Action(() => _ = activationAware.OnModalActivatedAsync()));
    }
}
