using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace IndexEditor.Shared
{
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
        None = 4
    }

    internal static class DebugLogger
    {
        private static ILoggerFactory? _factory;
        private static ILogger? _logger;
        private static readonly object _initLock = new object();
        private static string? _logPath;
        private static LogLevel _minLogLevel = LogLevel.Info; // Default to Info

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

        /// <summary>
        /// Set the minimum log level. Messages below this level will be suppressed.
        /// </summary>
        public static void SetMinimumLogLevel(LogLevel level)
        {
            _minLogLevel = level;
        }

        /// <summary>
        /// Get the current minimum log level.
        /// </summary>
        public static LogLevel GetMinimumLogLevel() => _minLogLevel;

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

        private static bool ShouldLog(LogLevel level)
        {
            return level >= _minLogLevel;
        }

        public static void Debug(string message)
        {
            if (!ShouldLog(LogLevel.Debug)) return;
            LogInternal(message, Microsoft.Extensions.Logging.LogLevel.Debug);
        }

        public static void Log(string message)
        {
            Info(message); // Default Log() to Info level
        }

        public static void Info(string message)
        {
            if (!ShouldLog(LogLevel.Info)) return;
            LogInternal(message, Microsoft.Extensions.Logging.LogLevel.Information);
        }

        public static void Warning(string message)
        {
            if (!ShouldLog(LogLevel.Warning)) return;
            LogInternal(message, Microsoft.Extensions.Logging.LogLevel.Warning);
        }

        public static void Error(string message)
        {
            if (!ShouldLog(LogLevel.Error)) return;
            LogInternal(message, Microsoft.Extensions.Logging.LogLevel.Error);
        }

        private static void LogInternal(string message, Microsoft.Extensions.Logging.LogLevel level)
        {
            try
            {
                if (_logger == null) EnsureFallbackLogger();
                if (_logger != null)
                {
                    _logger.Log(level, message);
                }
                else
                {
                    var levelStr = level.ToString().ToUpper();
                    var line = $"[{DateTime.Now:O}] [{levelStr}] {message}";
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
            if (!ShouldLog(LogLevel.Error)) return;
            
            try
            {
                if (_logger == null) EnsureFallbackLogger();
                if (_logger != null)
                {
                    _logger.LogError(ex, "{Context}", context);
                }
                else
                {
                    var text = $"[{DateTime.Now:O}] [ERROR] EXCEPTION in {context}: {ex.GetType().FullName}: {ex.Message}\n{ex.StackTrace}";
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
