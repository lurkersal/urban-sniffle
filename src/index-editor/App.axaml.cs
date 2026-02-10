using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
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
        catch (Exception ex)
        {
            // Ensure any initialization errors are logged
            try { DebugLogger.LogException("App.OnFrameworkInitializationCompleted: initialize logging", ex); } catch (Exception logEx) { Console.WriteLine("Failed to log during App init: " + logEx); }
        }

        // Setup DI
        var services = new ServiceCollection();
        services.AddSingleton<IndexEditor.Shared.IToastService, IndexEditor.Shared.DefaultToastService>();
        // Register ViewModels and other services
        services.AddSingleton<Views.EditorStateViewModel>();
        services.AddSingleton<Views.MainWindowViewModel>();
        // PageControllerBridge will be set by the MainWindow when created; register factory placeholder
        services.AddSingleton<Views.IPageControllerBridge?>(provider => null);
        // Build provider
        var serviceProvider = services.BuildServiceProvider();

        // Set static provider for backwards-compatible static API
        ToastService.Provider = serviceProvider.GetRequiredService<IndexEditor.Shared.IToastService>();

        // App initialization completed
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            string? folderToOpen = null;
            if (desktop.Args is { Length: > 0 })
            {
                var args = desktop.Args.ToList();
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

            // Resolve MainWindow and viewmodels via DI
            var mainWindow = new MainWindow(folderToOpen);
            try
            {
                var editorVm = serviceProvider.GetRequiredService<Views.EditorStateViewModel>();
                mainWindow.DataContext = editorVm; // preserve existing expectation for child controls
            }
            catch (Exception ex) { DebugLogger.LogException("App: resolve EditorStateViewModel", ex); }
            try
            {
                var mainVm = serviceProvider.GetRequiredService<Views.MainWindowViewModel>();
                mainWindow.MainViewModel = mainVm; // assign auxiliary main VM
            }
            catch (Exception ex) { DebugLogger.LogException("App: resolve MainWindowViewModel", ex); }

            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }
}