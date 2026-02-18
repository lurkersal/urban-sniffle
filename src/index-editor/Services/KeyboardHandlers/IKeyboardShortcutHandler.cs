using Avalonia.Input;

namespace IndexEditor.Services.KeyboardHandlers;

/// <summary>
/// Interface for keyboard shortcut handlers.
/// Each handler is responsible for a specific set of keyboard shortcuts.
/// </summary>
public interface IKeyboardShortcutHandler
{
    /// <summary>
    /// Attempts to handle the keyboard event.
    /// </summary>
    /// <param name="e">The keyboard event arguments</param>
    /// <returns>True if the event was handled, false otherwise</returns>
    bool TryHandle(KeyEventArgs e);
    
    /// <summary>
    /// Gets the priority of this handler. Higher priority handlers are invoked first.
    /// </summary>
    int Priority { get; }
}

