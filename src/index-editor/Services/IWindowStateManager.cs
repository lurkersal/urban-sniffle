using Avalonia.Controls;

namespace IndexEditor.Services;

/// <summary>
/// Abstraction for window state management, allowing for testability.
/// </summary>
public interface IWindowStateManager
{
    WindowState WindowState { get; set; }
}

