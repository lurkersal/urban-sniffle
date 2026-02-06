using System;

namespace IndexEditor.Shared
{
    // Simple static toast request broadcaster. UI components can subscribe to ShowRequested.
    public static class ToastService
    {
        public static event Action<string>? ShowRequested;
        public static void Show(string message) => ShowRequested?.Invoke(message);
    }
}
