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
        // If invoked with --demo, run the console demo runner and exit
        if (args.Length > 0 && args.Contains("--demo"))
        {
            DemoRunner.Run();
            return;
        }

        // Initialize logging so DebugLogger uses Microsoft.Extensions.Logging with Console provider (if available)
        try
        {
            var factory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });
            IndexEditor.Shared.DebugLogger.Initialize(factory);
            IndexEditor.Shared.DebugLogger.Log("IndexEditor starting");
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
