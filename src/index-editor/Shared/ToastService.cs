using System;

namespace IndexEditor.Shared
{
    public interface IToastService
    {
        void Show(string message);
    }

    public class DefaultToastService : IToastService
    {
        public void Show(string message)
        {
            // default behavior: forward to the static broadcaster
            ToastService.RaiseShowRequested(message);
        }
    }

    // Simple static toast request broadcaster. UI components can subscribe to ShowRequested.
    public static class ToastService
    {
        // Backing provider (set by DI in App)
        public static IToastService? Provider { get; set; }

        public static event Action<string>? ShowRequested;

        public static void Show(string message)
        {
            if (Provider != null)
            {
                try { Provider.Show(message); } catch { ShowRequested?.Invoke(message); }
            }
            else
            {
                ShowRequested?.Invoke(message);
            }
        }

        // Internal helper to raise the event from other classes in this assembly
        internal static void RaiseShowRequested(string message)
        {
            try { ShowRequested?.Invoke(message); } catch { /* swallow */ }
        }
    }
}
