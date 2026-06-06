using System.Windows.Controls;

namespace ClosetApp.UI.Services;

public class ModalService
{
    private static readonly Lazy<ModalService> _instance = new(() => new ModalService());
    public static ModalService Instance => _instance.Value;

    private readonly Dictionary<Type, Func<UserControl>> _registry = new();
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

    public void Show(UserControl content)
    {
        _modalStack.Push(content);
        ModalShowRequested?.Invoke(content);
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
}
