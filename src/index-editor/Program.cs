using Avalonia;
using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using IndexEditor.Tools;

namespace IndexEditor;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // Parse log level from command line arguments
        var logLevel = IndexEditor.Shared.LogLevel.Info; // Default to Info
        for (int i = 0; i < args.Length; i++)
        {
            if ((args[i] == "--log-level" || args[i] == "-l") && i + 1 < args.Length)
            {
                var levelArg = args[i + 1].ToLowerInvariant();
                logLevel = levelArg switch
                {
                    "debug" => IndexEditor.Shared.LogLevel.Debug,
                    "info" => IndexEditor.Shared.LogLevel.Info,
                    "warning" => IndexEditor.Shared.LogLevel.Warning,
                    "error" => IndexEditor.Shared.LogLevel.Error,
                    "none" => IndexEditor.Shared.LogLevel.None,
                    _ => IndexEditor.Shared.LogLevel.Info
                };
                break;
            }
        }

        // Set the minimum log level
        IndexEditor.Shared.DebugLogger.SetMinimumLogLevel(logLevel);

        // If invoked with --demo, run the console demo runner and exit
        if (args.Length > 0 && args.Contains("--demo"))
        {
            DemoRunner.Run();
            return;
        }

        // Initialize logging so DebugLogger uses Microsoft.Extensions.Logging with Console provider (if available)
        try
        {
            // Map our LogLevel to Microsoft.Extensions.Logging.LogLevel
            var msLogLevel = logLevel switch
            {
                IndexEditor.Shared.LogLevel.Debug => LogLevel.Debug,
                IndexEditor.Shared.LogLevel.Info => LogLevel.Information,
                IndexEditor.Shared.LogLevel.Warning => LogLevel.Warning,
                IndexEditor.Shared.LogLevel.Error => LogLevel.Error,
                IndexEditor.Shared.LogLevel.None => LogLevel.None,
                _ => LogLevel.Information
            };

            var factory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(msLogLevel);
            });
            IndexEditor.Shared.DebugLogger.Initialize(factory);
            IndexEditor.Shared.DebugLogger.Info($"IndexEditor starting with log level: {logLevel}");
        }
        catch (Exception ex)
        {
            // If initialization fails, fallback to DebugLogger's internal fallback
            try { IndexEditor.Shared.DebugLogger.LogException("Program.Main: logging init", ex); } catch { }
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
