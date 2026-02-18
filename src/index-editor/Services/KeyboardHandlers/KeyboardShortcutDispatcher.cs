using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Input;

namespace IndexEditor.Services.KeyboardHandlers;

/// <summary>
/// Dispatches keyboard events to registered handlers.
/// Handlers are invoked in priority order until one handles the event.
/// </summary>
public class KeyboardShortcutDispatcher
{
    private readonly List<IKeyboardShortcutHandler> _handlers = new();

    public KeyboardShortcutDispatcher(IEnumerable<IKeyboardShortcutHandler> handlers)
    {
        _handlers = handlers.OrderByDescending(h => h.Priority).ToList();
    }

    /// <summary>
    /// Dispatches the keyboard event to registered handlers.
    /// </summary>
    /// <param name="e">The keyboard event arguments</param>
    /// <returns>True if any handler processed the event, false otherwise</returns>
    public bool Dispatch(KeyEventArgs e)
    {
        foreach (var handler in _handlers)
        {
            try
            {
                if (handler.TryHandle(e))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Shared.DebugLogger.LogException($"KeyboardShortcutDispatcher: Handler {handler.GetType().Name} failed", ex);
            }
        }

        return false;
    }
}

