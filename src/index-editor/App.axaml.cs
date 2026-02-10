using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System.Linq;
using Microsoft.Extensions.Logging;
using IndexEditor.Shared;

namespace IndexEditor;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Configure logging for application
        try
        {
            var factory = LoggerFactory.Create(builder =>
            {
                builder.AddSimpleConsole(options =>
                {
                    options.SingleLine = true;
                    options.TimestampFormat = "HH:mm:ss ";
                });
                builder.SetMinimumLevel(LogLevel.Debug);
            });
            DebugLogger.Initialize(factory);
            DebugLogger.Log("Logging initialized");
        }
        catch { }

        // App initialization completed
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            string? folderToOpen = null;
            if (desktop.Args is { Length: > 0 })
            {
                // Support optional flag --no-images and optional folder path. Example invocations:
                // dotnet run -- --no-images /path/to/folder
                // dotnet run -- /path/to/folder
                var args = desktop.Args.ToList();
                // If args contains --no-images, set the flag and remove the arg
                if (args.Contains("--no-images"))
                {
                    IndexEditor.Shared.EditorState.ShowImages = false;
                    args = args.Where(a => a != "--no-images").ToList();
                }

                if (args.Count > 0)
                {
                    folderToOpen = args[0];
                }
            }
            desktop.MainWindow = new MainWindow(folderToOpen);
        }

        base.OnFrameworkInitializationCompleted();
    }
}