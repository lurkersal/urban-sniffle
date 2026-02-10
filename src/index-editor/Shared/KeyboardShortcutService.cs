using System;
using System.Collections.Generic;
using Avalonia.Input;

namespace IndexEditor.Shared
{
    public interface IKeyboardShortcutService
    {
        // Register a shortcut: returns a disposable to unregister
        IDisposable Register(Key key, KeyModifiers modifiers, Func<KeyEventArgs, bool> handler, Func<bool>? canExecute = null, string? name = null);

        // Handle an incoming KeyEvent; returns true if handled
        bool HandleKey(KeyEventArgs e);
    }

    public class KeyboardShortcutService : IKeyboardShortcutService
    {
        private readonly Dictionary<ShortcutKey, List<ShortcutEntry>> _map = new();

        private record ShortcutKey(Key Key, KeyModifiers Modifiers);
        private class ShortcutEntry
        {
            public Func<KeyEventArgs, bool> Handler { get; init; }
            public Func<bool>? CanExecute { get; init; }
            public string? Name { get; init; }
        }

        public IDisposable Register(Key key, KeyModifiers modifiers, Func<KeyEventArgs, bool> handler, Func<bool>? canExecute = null, string? name = null)
        {
            var sk = new ShortcutKey(key, modifiers);
            var entry = new ShortcutEntry { Handler = handler, CanExecute = canExecute, Name = name };
            lock (_map)
            {
                if (!_map.TryGetValue(sk, out var list))
                {
                    list = new List<ShortcutEntry>();
                    _map[sk] = list;
                }
                list.Add(entry);
            }
            return new Unregister(this, sk, entry);
        }

        public bool HandleKey(KeyEventArgs e)
        {
            try
            {
                var sk = new ShortcutKey(e.Key, e.KeyModifiers);
                List<ShortcutEntry>? list = null;
                lock (_map)
                {
                    if (_map.TryGetValue(sk, out var l)) list = new List<ShortcutEntry>(l);
                }
                if (list == null || list.Count == 0) return false;
                foreach (var entry in list)
                {
                    try
                    {
                        if (entry.CanExecute != null && !entry.CanExecute())
                        {
                            // If blocked, optionally show a toast? Keep responsibility to caller.
                            continue;
                        }
                        // Handler returns true if it handled the event
                        var handled = entry.Handler(e);
                        if (handled) return true;
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogException("KeyboardShortcutService.HandleKey: entry handler", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("KeyboardShortcutService.HandleKey: outer", ex);
            }
            return false;
        }

        private void UnregisterInternal(ShortcutKey key, ShortcutEntry entry)
        {
            lock (_map)
            {
                if (_map.TryGetValue(key, out var list))
                {
                    list.Remove(entry);
                    if (list.Count == 0) _map.Remove(key);
                }
            }
        }

        private class Unregister : IDisposable
        {
            private readonly KeyboardShortcutService _svc;
            private readonly ShortcutKey _key;
            private readonly ShortcutEntry _entry;
            private bool _disposed = false;
            public Unregister(KeyboardShortcutService svc, ShortcutKey key, ShortcutEntry entry)
            {
                _svc = svc; _key = key; _entry = entry;
            }
            public void Dispose()
            {
                if (_disposed) return; _disposed = true; _svc.UnregisterInternal(_key, _entry);
            }
        }
    }
}

