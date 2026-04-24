using Avalonia.Controls;

namespace IndexEditor.Services;

/// <summary>
/// Window-based implementation of IWindowStateManager.
/// </summary>
public class WindowStateManager : IWindowStateManager
{
    private readonly Window _window;

    public WindowStateManager(Window window)
    {
        _window = window ?? throw new System.ArgumentNullException(nameof(window));
    }

    public WindowState WindowState
    {
        get => _window.WindowState;
        set => _window.WindowState = value;
    }
}

