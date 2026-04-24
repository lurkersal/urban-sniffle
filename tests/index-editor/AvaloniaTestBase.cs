using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;

namespace IndexEditor.Tests
{
    /// <summary>
    /// Base class for tests that require Avalonia UI infrastructure.
    /// Initializes the headless platform for testing.
    /// </summary>
    public abstract class AvaloniaTestBase : IDisposable
    {
        private static bool _isInitialized = false;
        private static readonly object _initLock = new object();

        protected AvaloniaTestBase()
        {
            InitializeAvalonia();
        }

        private static void InitializeAvalonia()
        {
            lock (_initLock)
            {
                if (_isInitialized)
                    return;

                try
                {
                    // Initialize Avalonia headless platform
                    AppBuilder.Configure<App>()
                        .UseHeadless(new AvaloniaHeadlessPlatformOptions())
                        .SetupWithoutStarting();

                    _isInitialized = true;
                }
                catch (Exception ex)
                {
                    // Silently handle initialization errors - tests may not always need full UI
                    // Console output disabled to avoid cluttering test results
                }
            }
        }

        /// <summary>
        /// Runs an action on the UI thread (required for Avalonia controls)
        /// </summary>
        protected void RunOnUIThread(Action action)
        {
            Dispatcher.UIThread.InvokeAsync(action).Wait();
        }

        /// <summary>
        /// Runs a function on the UI thread and returns the result
        /// </summary>
        protected T RunOnUIThread<T>(Func<T> func)
        {
            return Dispatcher.UIThread.InvokeAsync(func).Result;
        }

        public virtual void Dispose()
        {
            // Cleanup if needed
        }
    }

    /// <summary>
    /// Simple App class for headless testing
    /// </summary>
    public class App : Application
    {
    }
}


