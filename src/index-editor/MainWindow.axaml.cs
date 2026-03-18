using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.Interactivity;
using IndexEditor.Shared;
using IndexEditor.Views;

#pragma warning disable CS0618 // Intentional use of backward-compatible static wrappers (EditorState, EditorActions)
using WindowState = Avalonia.Controls.WindowState;

namespace IndexEditor;

public partial class MainWindow : Window
{
    public static MainWindow? Instance { get; private set; }
    
    public string? FolderToOpen { get; }

    // Service fields
    private IndexEditor.Shared.IKeyboardShortcutService? _shortcutService;
    private Services.KeyboardHandlers.KeyboardShortcutDispatcher? _keyboardDispatcher;
    private Services.FullscreenImageService? _fullscreenService;
    private Services.OverlayManager? _overlayManager;
    private Services.LinkDiscoveryService? _linkDiscoveryService;
    private Services.LinkDiscoveryUIService? _linkDiscoveryUIService;
    private Services.FolderLoadingService? _folderLoadingService;
    private Services.ArticleDeletionService? _articleDeletionService;
    private Services.WindowLifecycleService? _windowLifecycleService;
    private readonly Services.IIndexFileService? _indexFileService;
    private Services.IDialogService? _dialogService;
    private readonly IndexEditor.Shared.IEditorState? _editorState;
    private readonly IndexEditor.Shared.IEditorActions? _editorActions;
    private Services.IFileOperationsService? _fileOperationsService;

    private Views.MainWindowViewModel? _mainViewModel;
    public Views.MainWindowViewModel? MainViewModel
    {
        get => _mainViewModel;
        set
        {
            _mainViewModel = value;
            try
            {
                var pcControl = this.FindControl<IndexEditor.Views.PageControllerView>("PageControllerControl");
                if (pcControl != null && _mainViewModel != null)
                {
                    _mainViewModel.PageControllerBridge = new Views.PageControllerBridge(pcControl);
                }
            }
            catch (Exception ex) { DebugLogger.LogException("MainWindow.MainViewModel.set: assign bridge", ex); }
        }
    }

    private static void WriteDiagFile(string text)
    {
        // no-op in non-debug builds: diagnostic output suppressed
    }

    public MainWindow() : this(null) { }

    /// <summary>
    /// Set the dialog service (called from App.axaml.cs after MainWindow is created)
    /// </summary>
    public void SetDialogService(Services.IDialogService dialogService)
    {
        _dialogService = dialogService;
        
        // Reinitialize WindowLifecycleService with the DialogService
        _windowLifecycleService = new Services.WindowLifecycleService(
            this,
            _editorState ?? new IndexEditor.Shared.EditorStateService(),
            _dialogService,
            _linkDiscoveryService,
            GetDiscoveredLinks);
    }

    /// <summary>
    /// Get all discovered links as a flat list for saving
    /// </summary>
    public List<Common.Shared.MagazineLink> GetDiscoveredLinks()
    {
        return _linkDiscoveryUIService?.DiscoveredLinks.Values.SelectMany(l => l).ToList() 
            ?? new List<Common.Shared.MagazineLink>();
    }

    public MainWindow(string? folderToOpen = null) : this(folderToOpen, null, null, null) { }

    public MainWindow(string? folderToOpen, Services.IIndexFileService? indexFileService) : this(folderToOpen, indexFileService, null, null) { }

    public MainWindow(string? folderToOpen, Services.IIndexFileService? indexFileService, IndexEditor.Shared.IEditorState? editorState) 
        : this(folderToOpen, indexFileService, editorState, null) { }

    public MainWindow(string? folderToOpen, Services.IIndexFileService? indexFileService, IndexEditor.Shared.IEditorState? editorState, IndexEditor.Shared.IEditorActions? editorActions)
    {
        // MainWindow constructor
        Instance = this;
        FolderToOpen = folderToOpen;
        _indexFileService = indexFileService;
        _editorState = editorState;
        _editorActions = editorActions;

        // Initialize FileOperationsService if we have an IIndexFileService
        if (_indexFileService != null)
        {
            _fileOperationsService = new Services.FileOperationsService(_indexFileService, _editorState);
        }

        InitializeComponent();
        
        // Initialize services
        try
        {
            _fullscreenService = new Services.FullscreenImageService(this);
            _overlayManager = new Services.OverlayManager(this);
            _linkDiscoveryService = new Services.LinkDiscoveryService();
            _linkDiscoveryUIService = new Services.LinkDiscoveryUIService(this, _editorState ?? new IndexEditor.Shared.EditorStateService());
            
            // Wire up link discovery events
            _linkDiscoveryService.ProgressChanged += _linkDiscoveryUIService.OnLinkDiscoveryProgress;
            _linkDiscoveryService.LinkDiscovered += _linkDiscoveryUIService.OnLinkDiscovered;
            _linkDiscoveryService.DiscoveryCompleted += _linkDiscoveryUIService.OnLinkDiscoveryCompleted;
            
            // Initialize FolderLoadingService
            _folderLoadingService = new Services.FolderLoadingService(
                this,
                _editorState ?? new IndexEditor.Shared.EditorStateService(),
                _fileOperationsService,
                _indexFileService,
                _linkDiscoveryUIService,
                _linkDiscoveryService,
                ImageExistsInFolder);
            
            // Initialize ArticleDeletionService
            _articleDeletionService = new Services.ArticleDeletionService(
                this,
                _editorState ?? new IndexEditor.Shared.EditorStateService(),
                _editorActions);
            
            // Initialize WindowLifecycleService (will be set with DialogService later)
            _windowLifecycleService = new Services.WindowLifecycleService(
                this,
                _editorState ?? new IndexEditor.Shared.EditorStateService(),
                _dialogService,
                _linkDiscoveryService,
                GetDiscoveredLinks);
        }
        catch (Exception ex) { DebugLogger.LogException("MainWindow ctor: init services", ex); }
        // After InitializeComponent, wire view-specific bridges
        try
        {
            var pcControl = this.FindControl<IndexEditor.Views.PageControllerView>("PageControllerControl");
            if (pcControl != null)
            {
                // Inject EditorState if available
                if (_editorState != null)
                {
                    pcControl.SetEditorState(_editorState);
                }
                
                // Inject all services from DI container or create defaults
                // Note: These will be from DI container when services are properly registered in App.axaml.cs
                var pageNav = new Services.PageNavigationService();
                var imageLoad = new Services.ImageLoadingService();
                var linkMgmt = new Services.LinkManagementService();
                var articleCardRenderer = new Services.ArticleCardRenderer();
                var articleDisplayCoord = new Services.ArticleDisplayCoordinator(
                    _editorState ?? new IndexEditor.Shared.EditorStateService(), 
                    articleCardRenderer);
                var articleFocusMgr = new Services.ArticleFocusManager(
                    _editorState ?? new IndexEditor.Shared.EditorStateService());
                var segmentMgmt = new Services.SegmentManagementService(
                    _editorState ?? new IndexEditor.Shared.EditorStateService());
                var pageNavCoord = new Services.PageNavigationCoordinator(
                    _editorState ?? new IndexEditor.Shared.EditorStateService(), 
                    pageNav);
                
                pcControl.SetServices(
                    pageNav,
                    imageLoad,
                    linkMgmt,
                    articleCardRenderer,
                    articleDisplayCoord,
                    articleFocusMgr,
                    segmentMgmt,
                    pageNavCoord);
                
                // Create bridge implementation and assign to VM (SetBridge call is unnecessary)
                var bridge = new PageControllerBridge(pcControl);
                try { if (this.DataContext is Views.MainWindowViewModel mwvm) mwvm.PageControllerBridge = bridge; } catch (Exception ex) { DebugLogger.LogException("MainWindow ctor: assign bridge to VM", ex); }
            }
        }
        catch (Exception ex) { DebugLogger.LogException("MainWindow ctor: wire PageControllerBridge", ex); }

        // Inject EditorState into ArticleList
        try
        {
            var articleList = this.FindControl<IndexEditor.Views.ArticleList>("ArticleListControl");
            if (articleList != null && _editorState != null)
            {
                articleList.SetEditorState(_editorState);
            }
        }
        catch (Exception ex) { DebugLogger.LogException("MainWindow ctor: inject ArticleList EditorState", ex); }

        // Initialize keyboard shortcut dispatcher with handlers
        try
        {
            // Use injected _editorState if available, otherwise create a new instance for backward compatibility
            var editorStateForHandlers = _editorState ?? new IndexEditor.Shared.EditorStateService();
            
            var handlers = new List<Services.KeyboardHandlers.IKeyboardShortcutHandler>
            {
                new Services.KeyboardHandlers.SegmentKeyboardHandler(this, editorStateForHandlers),
                new Services.KeyboardHandlers.ArticleKeyboardHandler(this, editorStateForHandlers),
                new Services.KeyboardHandlers.FileKeyboardHandler(this, editorStateForHandlers, LoadArticlesFromFolder, _overlayManager, _linkDiscoveryService),
                new Services.KeyboardHandlers.NavigationKeyboardHandler(this, editorStateForHandlers)
            };
            _keyboardDispatcher = new Services.KeyboardHandlers.KeyboardShortcutDispatcher(handlers);
        }
        catch (Exception ex) { DebugLogger.LogException("MainWindow ctor: init keyboard dispatcher", ex); }

        // Global keyboard shortcuts: handle at window level
        this.KeyDown += OnMainWindowKeyDown;
        // Also register a tunneling handler so key events are seen before focused controls
        // handledEventsToo: true ensures we capture Ctrl-A even when TextBox handles it for "select all"
        try { this.AddHandler<KeyEventArgs>(KeyDownEvent, OnMainWindowKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true); } catch (Exception ex) { DebugLogger.LogException("MainWindow ctor: AddHandler tunnel", ex); }

        // Subscribe to EditorActions events to handle UI updates
        try
        {
            IndexEditor.Shared.EditorActions.ArticleCreated += OnArticleCreated;
        }
        catch (Exception ex) { DebugLogger.LogException("MainWindow ctor: subscribe to EditorActions events", ex); }


        // Configure startup and Opened handler
        try
        {
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.Opened += OnWindowOpened;
            this.Closing += OnWindowClosing;
            WriteDiagFile("[TRACE] Opened handler attached");
        }
        catch (Exception ex) { DebugLogger.LogException("MainWindow ctor: Opened handler", ex); }

        // Wire up overlay buttons if present
        try
        {
            // Delete article confirmation buttons wiring
            try
            {
                var delConfirm = this.FindControl<Button>("DeleteArticleConfirmBtn");
                var delCancel = this.FindControl<Button>("DeleteArticleCancelBtn");
                var delOverlay = this.FindControl<Border>("DeleteArticleConfirmOverlay");
                if (delConfirm != null && delOverlay != null)
                {
                    delConfirm.Click += (s, e) =>
                    {
                        try
                        {
                            _articleDeletionService?.DeleteSelectedArticle();
                        }
                        catch (Exception ex) { DebugLogger.LogException("DeleteArticleConfirmBtn.Click", ex); }
                    };
                }
                 if (delCancel != null && delOverlay != null)
                 {
                    delCancel.Click += (s, e) => { try { delOverlay.IsVisible = false; } catch (Exception ex) { DebugLogger.LogException("DeleteArticleCancelBtn.Click", ex); } };
                    delCancel.Click += (s, e) => { try { delOverlay.IsVisible = false; DebugLogger.Log("DeleteArticleCancelBtn.Click - overlay hidden"); } catch (Exception ex) { DebugLogger.LogException("DeleteArticleCancelBtn.Click", ex); } };
                 }
                 
                 // Add keyboard handling for Delete overlay - Enter to confirm, Escape to cancel
                 // This handles the keys only when the overlay is visible
                 if (delOverlay != null && delConfirm != null && delCancel != null)
                 {
                     delOverlay.KeyDown += (s, e) =>
                     {
                         try
                         {
                             if (delOverlay.IsVisible)
                             {
                                 if (e.Key == Avalonia.Input.Key.Enter)
                                 {
                                     delConfirm.Command?.Execute(delConfirm.CommandParameter);
                                     // Trigger click event
                                     delConfirm.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                                     e.Handled = true;
                                 }
                                 else if (e.Key == Avalonia.Input.Key.Escape)
                                 {
                                     delOverlay.IsVisible = false;
                                     e.Handled = true;
                                 }
                             }
                         }
                         catch (Exception ex) { DebugLogger.LogException("DeleteArticleConfirmOverlay.KeyDown", ex); }
                     };
                     
                     // Make the overlay focusable so it can receive keyboard input
                     delOverlay.Focusable = true;
                     
                     // When overlay becomes visible, focus it so it receives keyboard input
                     delOverlay.PropertyChanged += (s, e) =>
                     {
                         try
                         {
                             if (e.Property.Name == "IsVisible" && e.NewValue is bool isVisible && isVisible)
                             {
                                 // Overlay just became visible, focus it
                                 Dispatcher.UIThread.Post(() =>
                                 {
                                     try { delOverlay.Focus(); }
                                     catch (Exception ex2) { DebugLogger.LogException("DeleteArticleConfirmOverlay focus on visible", ex2); }
                                 }, Avalonia.Threading.DispatcherPriority.Loaded);
                             }
                         }
                         catch (Exception ex) { DebugLogger.LogException("DeleteArticleConfirmOverlay.PropertyChanged", ex); }
                     };
                 }
             }
             catch (Exception ex) { DebugLogger.LogException("MainWindow ctor: wire delete confirmation buttons", ex); }

            var closeBtn = this.FindControl<Button>("IndexOverlayCloseBtn");
            var saveBtn = this.FindControl<Button>("IndexOverlaySaveBtn");
            var overlay = this.FindControl<Border>("IndexOverlay");
            
            // Note: Overlay is now editable TextBox for JSON
            
            if (closeBtn != null && overlay != null)
            {
                closeBtn.Click += (s, e) => {
                    try
                    {
                        // Clear any shown parse error state when closing
                        try { var errBorder = this.FindControl<Border>("IndexOverlayErrorBorder"); var errLine = this.FindControl<TextBlock>("IndexOverlayErrorLine"); if (errBorder != null) errBorder.IsVisible = false; if (errLine != null) errLine.Text = string.Empty; } catch { }
                        overlay.IsVisible = false;
                    }
                    catch (Exception ex) { DebugLogger.LogException("IndexOverlayCloseBtn.Click", ex); }
                };
            }
            
            if (saveBtn != null)
            {
                saveBtn.Click += (s, e) =>
                {
                    try
                    {
                        var folder = IndexEditor.Shared.EditorState.CurrentFolder;
                        _overlayManager?.SaveIndexFromOverlay(folder);
                        // Reload the folder after save
                        if (!string.IsNullOrWhiteSpace(folder))
                        {
                            LoadArticlesFromFolder(folder);
                        }
                    }
                    catch (Exception ex) { DebugLogger.LogException("IndexOverlaySaveBtn.Click", ex); }
                };
            }
            
            // Wire up toggle mode button
            var toggleModeBtn = this.FindControl<Button>("IndexOverlayToggleModeBtn");
            if (toggleModeBtn != null)
            {
                toggleModeBtn.Click += (s, e) =>
                {
                    try
                    {
                        var folder = IndexEditor.Shared.EditorState.CurrentFolder;
                        _overlayManager?.ToggleEditMode(folder);
                    }
                    catch (Exception ex) { DebugLogger.LogException("IndexOverlayToggleModeBtn.Click", ex); }
                };
            }
            
            // Wire up expand/collapse buttons for JSON tree viewer
            try
            {
                var expandAllBtn = this.FindControl<Button>("IndexOverlayExpandAllBtn");
                var collapseAllBtn = this.FindControl<Button>("IndexOverlayCollapseAllBtn");
                
                if (expandAllBtn != null)
                {
                    expandAllBtn.Click += (s, e) => 
                    {
                        try
                        {
                            _overlayManager?.ExpandAllJsonNodes();
                        }
                        catch (Exception ex) { DebugLogger.LogException("IndexOverlayExpandAllBtn.Click", ex); }
                    };
                }
                
                if (collapseAllBtn != null)
                {
                    collapseAllBtn.Click += (s, e) =>
                    {
                        try
                        {
                            _overlayManager?.CollapseAllJsonNodes();
                        }
                        catch (Exception ex) { DebugLogger.LogException("IndexOverlayCollapseAllBtn.Click", ex); }
                    };
                }
            }
            catch (Exception ex) { DebugLogger.LogException("MainWindow ctor: wire expand/collapse buttons", ex); }
            
            // Help overlay close wiring
            try
            {
                var helpClose = this.FindControl<Button>("HelpOverlayCloseBtn");
                var helpOverlay = this.FindControl<Border>("HelpOverlay");
                if (helpClose != null && helpOverlay != null)
                {
                    helpClose.Click += (s, e) => { try { helpOverlay.IsVisible = false; } catch (Exception ex) { DebugLogger.LogException("HelpOverlayCloseBtn.Click", ex); } };
                }
            }
            catch (Exception ex) { DebugLogger.LogException("MainWindow ctor: wire HelpOverlayCloseBtn", ex); }

            // Parser output overlay close wiring
            try
            {
                var parserClose = this.FindControl<Button>("ParserOutputCloseBtn");
                var parserOverlay = this.FindControl<Border>("ParserOutputOverlay");
                if (parserClose != null && parserOverlay != null)
                {
                    parserClose.Click += (s, e) => { try { parserOverlay.IsVisible = false; } catch (Exception ex) { DebugLogger.LogException("ParserOutputCloseBtn.Click", ex); } };
                }
            }
            catch (Exception ex) { DebugLogger.LogException("MainWindow ctor: wire ParserOutputCloseBtn", ex); }

            // Fullscreen image overlay close wiring
            try
            {
                var fullscreenClose = this.FindControl<Button>("FullscreenCloseBtn");
                var fullscreenOverlay = this.FindControl<Border>("FullscreenImageOverlay");
                if (fullscreenClose != null && fullscreenOverlay != null)
                {
                    fullscreenClose.Click += (s, e) => { try { CloseFullscreenImage(); } catch (Exception ex) { DebugLogger.LogException("FullscreenCloseBtn.Click", ex); } };
                }
            }
            catch (Exception ex) { DebugLogger.LogException("MainWindow ctor: wire FullscreenCloseBtn", ex); }
        }
        catch (Exception ex) { DebugLogger.LogException("MainWindow ctor: wire up overlay buttons", ex); }

        // Wire up Start Link Discovery button
        try
        {
            var startBtn = this.FindControl<Button>("StartLinkDiscoveryBtn");
            if (startBtn != null)
            {
                startBtn.Click += (s, e) =>
                {
                    try
                    {
                        var folder = IndexEditor.Shared.EditorState.CurrentFolder;
                        var magazineName = IndexEditor.Shared.EditorState.CurrentMagazine ?? "Unknown";
                        if (!string.IsNullOrWhiteSpace(folder))
                        {
                            startBtn.IsVisible = false;
                            
                            // Clear existing links when user manually starts/re-scans
                            _linkDiscoveryUIService?.ClearDiscoveredLinks();
                            var pcView = this.FindControl<Views.PageControllerView>("PageControllerControl");
                            pcView?.UpdateDiscoveredLinks(_linkDiscoveryUIService?.DiscoveredLinks ?? new Dictionary<int, List<Common.Shared.MagazineLink>>());
                            
                            _linkDiscoveryService?.StartDiscovery(folder, magazineName);
                        }
                    }
                    catch (Exception ex) { DebugLogger.LogException("StartLinkDiscoveryBtn.Click", ex); }
                };
            }
        }
        catch (Exception ex) { DebugLogger.LogException("MainWindow ctor: wire up start discovery button", ex); }

        // Load articles from folder if provided
        if (!string.IsNullOrWhiteSpace(FolderToOpen))
        {
            LoadArticlesFromFolder(FolderToOpen);
        }

        // MainWindow constructor finished
    }

    // Event handler for when a new article is created via EditorActions
    private void OnArticleCreated(Common.Shared.ArticleLine article)
    {
        try
        {
            // Select the new article in the ViewModel on UI thread
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    var vm = this.DataContext as IndexEditor.Views.EditorStateViewModel;
                    if (vm != null)
                    {
                        // Find the article in the VM's collection (it should match by reference or pages)
                        var inList = vm.Articles.FirstOrDefault(a => object.ReferenceEquals(a, article))
                                  ?? vm.Articles.FirstOrDefault(a => a.Pages != null && article.Pages != null && a.Pages.SequenceEqual(article.Pages));
                        var toSelect = inList ?? article;
                        
                        // Select the article
                        if (vm.SelectArticleCommand != null && vm.SelectArticleCommand.CanExecute(toSelect))
                            vm.SelectArticleCommand.Execute(toSelect);
                        vm.SelectedArticle = toSelect;
                        
                        DebugLogger.Log($"MainWindow.OnArticleCreated: Selected article in ViewModel");
                        
                        // Focus the title field so user can start typing immediately
                        try
                        {
                            IndexEditor.Shared.EditorActions.FocusArticleTitle();
                            DebugLogger.Log($"MainWindow.OnArticleCreated: Requested focus on title field");
                        }
                        catch (Exception ex) { DebugLogger.LogException("MainWindow.OnArticleCreated: focus title", ex); }
                    }
                }
                catch (Exception ex) { DebugLogger.LogException("MainWindow.OnArticleCreated: select article in VM", ex); }
            }, Avalonia.Threading.DispatcherPriority.Background);
        }
        catch (Exception ex) { DebugLogger.LogException("MainWindow.OnArticleCreated: outer", ex); }
    }

    private void OnWindowOpened(object? sender, EventArgs e)
    {
        _windowLifecycleService?.OnWindowOpened();
    }

    private async void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_windowLifecycleService == null) return;

        // Cancel the close temporarily to allow async processing
        e.Cancel = true;

        try
        {
            // Let the service handle the closing logic
            bool canClose = await _windowLifecycleService.OnWindowClosingAsync();

            if (canClose)
            {
                // Actually close the window (this time without canceling)
                this.Closing -= OnWindowClosing; // Unsubscribe to prevent recursion
                this.Close();
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("MainWindow.OnWindowClosing", ex);
            // Close anyway on error
            this.Closing -= OnWindowClosing;
            this.Close();
        }
    }

    private List<int> ParsePageNumbers(string pageStr, out bool hasError)
    {
        var pages = new List<int>();
        hasError = false;
        if (string.IsNullOrWhiteSpace(pageStr))
        {
            hasError = true;
            return pages;
        }

        var parts = pageStr.Split('|');
        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                hasError = true;
                continue;
            }
            if (trimmed.Contains('-'))
            {
                var range = trimmed.Split('-');
                if (range.Length == 2 && int.TryParse(range[0], out int start) && int.TryParse(range[1], out int end) && start <= end)
                {
                    for (int i = start; i <= end; i++)
                        pages.Add(i);
                }
                else
                {
                    hasError = true;
                }
            }
            else if (int.TryParse(trimmed, out int page))
            {
                pages.Add(page);
            }
            else
            {
                hasError = true;
            }
        }
        pages = pages.Distinct().OrderBy(p => p).ToList();
        return pages;
    }

    private bool ImageExistsInFolder(string folder, int pageNumber)
    {
        // Use FileOperationsService if available
        if (_fileOperationsService != null)
        {
            return _fileOperationsService.ImageExists(folder, pageNumber);
        }

        // Fallback: Use ImageHelper.ImageExists to check all possible filename patterns
        return IndexEditor.Shared.ImageHelper.ImageExists(folder, pageNumber);
    }

    // Main window event handlers
    // Main window keyboard event handler - delegates to specialized handlers
    private void OnMainWindowKeyDown(object? sender, KeyEventArgs e)
    {
        try
        {
            // Special handling for overlays that capture all input
            if (HandleOverlayKeyboard(e))
            {
                return;
            }

            // Dispatch to keyboard handlers
            if (_keyboardDispatcher != null)
            {
                try
                {
                    if (_keyboardDispatcher.Dispatch(e))
                    {
                        return; // Handler processed the event
                    }
                }
                catch (Exception ex)
                {
                    DebugLogger.LogException("MainWindow: keyboard dispatcher", ex);
                }
            }

            // Handle F1 for help overlay (not in handlers to avoid dependency on overlay)
            if (e.Key == Key.F1)
            {
                _overlayManager?.ToggleHelpOverlay();
                e.Handled = true;
            }

            // Handle Delete key for article deletion
            if (e.Key == Key.Delete && !e.KeyModifiers.HasFlag(KeyModifiers.Control) && !e.KeyModifiers.HasFlag(KeyModifiers.Alt))
            {
                HandleDeleteKey(e);
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("MainWindow: OnMainWindowKeyDown", ex);
        }
    }

    // Handle keyboard input when overlays are visible
    private bool HandleOverlayKeyboard(KeyEventArgs e)
    {
        try
        {
            // Fullscreen image overlay: Esc to close (highest priority)
            if (_fullscreenService?.IsFullscreenVisible() ?? false)
            {
                if (e.Key == Key.Escape)
                {
                    CloseFullscreenImage();
                    e.Handled = true;
                    return true;
                }
                // Block other shortcuts while fullscreen is visible
                return true;
            }

            var overlay = this.FindControl<Border>("IndexOverlay");

            // Index overlay: capture Ctrl+I and Esc to close
            if (overlay != null && overlay.IsVisible)
            {

                // Ctrl+I: Toggle overlay
                if (e.Key == Key.I && (e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta)))
                {
                    _overlayManager?.CloseIndexOverlay();
                    e.Handled = true;
                    return true;
                }

                // Esc: Close overlay
                if (e.Key == Key.Escape)
                {
                    _overlayManager?.CloseIndexOverlay();
                    e.Handled = true;
                    return true;
                }

                // Ctrl+S: Save overlay content (handled by FileKeyboardHandler but needs early return)
                if (e.Key == Key.S && e.KeyModifiers.HasFlag(KeyModifiers.Control))
                {
                    // Let the FileKeyboardHandler handle this
                    return false;
                }

                // All other keys go to the textbox
                return true;
            }

            // Help overlay: Esc or F1 to close
            if (_overlayManager?.IsHelpOverlayVisible() ?? false)
            {
                if (e.Key == Key.Escape || e.Key == Key.F1)
                {
                    _overlayManager?.CloseHelpOverlay();
                    e.Handled = true;
                    return true;
                }
                return true; // Block other shortcuts while help is visible
            }

            // Delete confirmation overlay: Enter to confirm, Esc to cancel
            if (_overlayManager?.IsDeleteConfirmationVisible() ?? false)
            {
                if (e.Key == Key.Enter || e.Key == Key.Y)
                {
                    try { DeleteSelectedArticleAndCloseOverlay(); }
                    catch (Exception ex) { DebugLogger.LogException("MainWindow: confirm delete", ex); }
                    e.Handled = true;
                    return true;
                }
                if (e.Key == Key.Escape || e.Key == Key.N)
                {
                    _overlayManager?.CloseDeleteConfirmation();
                    e.Handled = true;
                    return true;
                }
                return true; // Block other shortcuts while delete confirmation is visible
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("MainWindow: HandleOverlayKeyboard", ex);
        }

        return false;
    }

    // Handle Delete key for article deletion
    private void HandleDeleteKey(KeyEventArgs e)
    {
        try
        {
            if (_articleDeletionService == null) return;
            
            if (!_articleDeletionService.CanDeleteArticle())
            {
                // Check if we should show a message about active segment
                var activeSeg = IndexEditor.Shared.EditorState.ActiveSegment;
                if (activeSeg != null && activeSeg.IsActive)
                {
                    IndexEditor.Shared.ToastService.Show("End or cancel the active segment before deleting an article");
                    e.Handled = true;
                }
                return;
            }

            // Get the article to show in the confirmation dialog
            var vm = this.DataContext as IndexEditor.Views.EditorStateViewModel;
            var article = vm?.SelectedArticle ?? IndexEditor.Shared.EditorState.ActiveArticle;
            
            if (article != null)
            {
                _articleDeletionService.ShowDeleteConfirmation(article);
                e.Handled = true;
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("MainWindow: HandleDeleteKey", ex);
        }
    }
    private async void OnOpenButtonClick(object sender, RoutedEventArgs e)
    {
        // Open folder using FolderBrowserWindow.ShowDialogAsync and update EditorState on UI thread
        try
        {
            var wnd = this.VisualRoot as Window ?? this;
            var start = IndexEditor.Shared.EditorState.CurrentFolder;
            string? path = null;
            try
            {
                path = await IndexEditor.Shared.FolderPicker.PickFolderAsync(wnd, start);
            }
            catch (Exception ex) { DebugLogger.LogException("MainWindow: folder picker", ex); }
            if (!string.IsNullOrWhiteSpace(path))
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    IndexEditor.Shared.EditorState.CurrentFolder = path;
                    LoadArticlesFromFolder(path);
                });
            }
        }
        catch (Exception ex) { DebugLogger.LogException("MainWindow: OnOpenButtonClick", ex); }
    }

    // Show fullscreen image overlay
    private void ShowFullscreenImage(Avalonia.Media.IImage imageSource, int pageNumber)
    {
        _fullscreenService?.ShowFullscreen(imageSource, pageNumber);
    }

    // Close fullscreen image overlay
    private void CloseFullscreenImage()
    {
        _fullscreenService?.CloseFullscreen();
    }

    private void LoadArticlesFromFolder(string folder)
    {
        _folderLoadingService?.LoadFolder(folder);
    }

    // Helper: delete currently selected article (VM selected or EditorState active) and close delete overlay
    private void DeleteSelectedArticleAndCloseOverlay()
    {
        _articleDeletionService?.DeleteSelectedArticle();
    }
}
