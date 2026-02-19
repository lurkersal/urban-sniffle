using Avalonia.Controls;

namespace IndexEditor.Services;

/// <summary>
/// Abstraction for finding controls, allowing for testability.
/// </summary>
public interface IControlFinder
{
    T? FindControl<T>(string name) where T : Control;
}


