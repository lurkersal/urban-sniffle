using Avalonia.Input;

namespace IndexEditor.Services;

/// <summary>
/// Service for registering and dispatching keyboard commands.
/// Extracts complex keyboard handling logic from MainWindow.
/// This is a simplified approach - the actual KeyboardShortcutDispatcher
/// already exists and should be enhanced instead.
/// </summary>
public interface IKeyboardCommandService
{
    /// <summary>
    /// Register a keyboard command handler.
    /// </summary>
    /// <param name="key">Key to handle</param>
    /// <param name="modifiers">Key modifiers (Ctrl, Shift, etc.)</param>
    /// <param name="handler">Action to execute</param>
    void RegisterCommand(Key key, KeyModifiers modifiers, System.Action handler);
    
    /// <summary>
    /// Handle a key event by dispatching to registered handlers.
    /// </summary>
    /// <param name="e">Key event args</param>
    /// <returns>True if handled, false otherwise</returns>
    bool HandleKeyEvent(KeyEventArgs e);
}

