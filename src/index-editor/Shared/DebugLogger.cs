using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace IndexEditor.Shared
{
    internal static class DebugLogger
    {
        private static ILoggerFactory? _factory;
        private static ILogger? _logger;
        private static readonly object _initLock = new object();
        private static string? _logPath;

        static DebugLogger()
        {
            try
            {
                _logPath = Path.Combine(Path.GetTempPath(), "index-editor-debug.log");
            }
            catch
            {
                _logPath = null;
            }

            // Defer creating ILoggerFactory until Initialize is called; keep console fallback.
        }

        public static void Initialize(ILoggerFactory factory)
        {
            if (factory == null) return;
            lock (_initLock)
            {
                _factory = factory;
                _logger = _factory.CreateLogger("IndexEditor");
            }
        }

        private static void EnsureFallbackLogger()
        {
            // If not initialized, create a simple console logger factory so Log calls still go somewhere
            if (_logger != null) return;
            lock (_initLock)
            {
                if (_logger != null) return;
                try
                {
                    _factory = LoggerFactory.Create(builder => builder.AddConsole());
                    _logger = _factory.CreateLogger("IndexEditor.Fallback");
                }
                catch
                {
                    // If even this fails, leave _logger null and fall back to direct Console.WriteLine in methods
                }
            }
        }

        public static void Log(string message)
        {
            try
            {
                if (_logger == null) EnsureFallbackLogger();
                if (_logger != null)
                {
                    _logger.LogInformation(message);
                }
                else
                {
                    var line = $"[{DateTime.Now:O}] {message}";
                    Console.WriteLine(line);
                    if (_logPath != null)
                    {
                        lock (_initLock) File.AppendAllText(_logPath, line + Environment.NewLine);
                    }
                }
            }
            catch
            {
                // swallow to avoid throwing from logging
            }
        }

        public static void LogException(string context, Exception ex)
        {
            try
            {
                if (_logger == null) EnsureFallbackLogger();
                if (_logger != null)
                {
                    _logger.LogError(ex, "{Context}", context);
                }
                else
                {
                    var text = $"[{DateTime.Now:O}] EXCEPTION in {context}: {ex.GetType().FullName}: {ex.Message}\n{ex.StackTrace}";
                    Console.WriteLine(text);
                    if (_logPath != null)
                    {
                        lock (_initLock) File.AppendAllText(_logPath, text + Environment.NewLine);
                    }
                }
            }
            catch
            {
                // swallow
            }
        }
    }
}
