using System;
using Avalonia.Controls;

namespace IndexEditor.Services;

/// <summary>
/// Window-based implementation of IControlFinder.
/// </summary>
public class WindowControlFinder : IControlFinder
{
    private readonly Window _window;

    public WindowControlFinder(Window window)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
    }

    public T? FindControl<T>(string name) where T : Control
    {
        return _window.FindControl<T>(name);
    }
}



