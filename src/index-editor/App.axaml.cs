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
        // Setup DI (must happen before logging initialization so we can register logging)
        var services = new ServiceCollection();
        
        // Register logging services FIRST (required by other services)
        services.AddLogging(builder =>
        {
            builder.AddSimpleConsole(options =>
            {
                options.SingleLine = true;
                options.TimestampFormat = "HH:mm:ss ";
            });
            builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug);
        });
        
        // Core services
        services.AddSingleton<IndexEditor.Shared.IEditorState, IndexEditor.Shared.EditorStateService>();
        services.AddSingleton<IndexEditor.Shared.IEditorActions, IndexEditor.Shared.EditorActionsService>();
        services.AddSingleton<IndexEditor.Shared.IToastService, IndexEditor.Shared.DefaultToastService>();
        
        // File services (new - eliminates duplication)
        services.AddSingleton<Services.IIndexFileService, Services.IndexFileService>();
        
        // Page and image services (extracted from PageControllerView)
        services.AddSingleton<Services.IPageNavigationService, Services.PageNavigationService>();
        services.AddSingleton<Services.IImageLoadingService, Services.ImageLoadingService>();
        services.AddSingleton<Services.ILinkManagementService, Services.LinkManagementService>();
        
        // Article display and management services (Phase 1 refactoring)
        services.AddSingleton<Services.IArticleCardRenderer, Services.ArticleCardRenderer>();
        services.AddSingleton<Services.IArticleDisplayCoordinator, Services.ArticleDisplayCoordinator>();
        services.AddSingleton<Services.IArticleFocusManager, Services.ArticleFocusManager>();
        
        // Segment and navigation services (Phase 2 refactoring)
        services.AddSingleton<Services.ISegmentManagementService, Services.SegmentManagementService>();
        services.AddSingleton<Services.IPageNavigationCoordinator, Services.PageNavigationCoordinator>();
        
        // Register ViewModels and other services
        services.AddSingleton<Views.EditorStateViewModel>();
        services.AddSingleton<Views.MainWindowViewModel>();
        
        // Register a null/placeholder bridge; MainWindow will replace this with the real bridge at runtime.
        services.AddSingleton<Views.IPageControllerBridge, Views.NullPageControllerBridge>();
        
        // Build provider
        var serviceProvider = services.BuildServiceProvider();
        
        // Initialize DebugLogger with the DI-configured logging factory
        try
        {
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            DebugLogger.Initialize(loggerFactory);
            DebugLogger.Log("Logging initialized");
        }
        catch (Exception ex)
        {
            // Fallback logging if DebugLogger initialization fails
            Console.WriteLine($"Failed to initialize DebugLogger: {ex.Message}");
        }

        // Set static providers for backwards-compatible static API
        ToastService.Provider = serviceProvider.GetRequiredService<IndexEditor.Shared.IToastService>();
        
        // Set EditorState singleton instance for backward compatibility
#pragma warning disable CS0618 // Type or member is obsolete
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
#pragma warning restore CS0618 // Type or member is obsolete
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
            var indexFileService = serviceProvider.GetRequiredService<Services.IIndexFileService>();
            // editorState and editorActions already retrieved above for backward compatibility
            var mainWindow = new MainWindow(folderToOpen, indexFileService, editorState, editorActions);
            
            // Create DialogService with MainWindow as owner (after window is created)
            var dialogService = new Services.DialogService(mainWindow);
            
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

            // Inject DialogService into MainWindow
            mainWindow.SetDialogService(dialogService);

            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }
}