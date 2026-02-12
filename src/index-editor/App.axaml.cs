using System;
using System.IO;
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
            try { DebugLogger.LogException("App.OnFrameworkInitializationCompleted: initialize logging", ex); } catch (Exception logEx) { try { DebugLogger.Log($"Failed to log during App init: {logEx}"); } catch {} }
        }

        // Setup DI
        var services = new ServiceCollection();
        
        // Core services
        services.AddSingleton<IndexEditor.Shared.IEditorState, IndexEditor.Shared.EditorStateService>();
        services.AddSingleton<IndexEditor.Shared.IEditorActions, IndexEditor.Shared.EditorActionsService>();
        services.AddSingleton<IndexEditor.Shared.IToastService, IndexEditor.Shared.DefaultToastService>();
        
        // Register ViewModels and other services
        services.AddSingleton<Views.EditorStateViewModel>();
        services.AddSingleton<Views.MainWindowViewModel>();
        
        // Register a null/placeholder bridge; MainWindow will replace this with the real bridge at runtime.
        services.AddSingleton<Views.IPageControllerBridge, Views.NullPageControllerBridge>();
        
        // Build provider
        var serviceProvider = services.BuildServiceProvider();

        // Set static providers for backwards-compatible static API
        ToastService.Provider = serviceProvider.GetRequiredService<IndexEditor.Shared.IToastService>();
        
        // Set EditorState singleton instance for backward compatibility
        var editorState = serviceProvider.GetRequiredService<IndexEditor.Shared.IEditorState>();
        IndexEditor.Shared.EditorState.SetInstance(editorState);
        
        // Set EditorActions singleton instance for backward compatibility
        var editorActions = serviceProvider.GetRequiredService<IndexEditor.Shared.IEditorActions>();
        IndexEditor.Shared.EditorActions.SetInstance(editorActions);

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

            // If no folder was supplied on the command line, attempt to open the most recently opened folder
            if (string.IsNullOrWhiteSpace(folderToOpen))
            {
                try
                {
                    var recent = RecentFolderStore.GetLastOpenedFolder();
                    if (!string.IsNullOrWhiteSpace(recent) && Directory.Exists(recent))
                        folderToOpen = recent;
                }
                catch (Exception ex) { DebugLogger.LogException("App: RecentFolderStore lookup", ex); }
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