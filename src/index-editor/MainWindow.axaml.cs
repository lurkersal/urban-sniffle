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

    private IndexEditor.Shared.IKeyboardShortcutService? _shortcutService;
    private Services.KeyboardHandlers.KeyboardShortcutDispatcher? _keyboardDispatcher;
    private Services.FullscreenImageService? _fullscreenService;
    private Services.OverlayManager? _overlayManager;
    private Services.LinkDiscoveryService? _linkDiscoveryService;
    private Dictionary<int, List<Common.Shared.MagazineLink>> _discoveredLinks = new();
    private readonly Services.IIndexFileService? _indexFileService;
    private Services.IDialogService? _dialogService;
    private readonly IndexEditor.Shared.IEditorState? _editorState;
    private readonly IndexEditor.Shared.IEditorActions? _editorActions;

    /// <summary>
    /// Set the dialog service (called from App.axaml.cs after MainWindow is created)
    /// </summary>
    public void SetDialogService(Services.IDialogService dialogService)
    {
        _dialogService = dialogService;
    }

    /// <summary>
    /// Get all discovered links as a flat list for saving
    /// </summary>
    public List<Common.Shared.MagazineLink> GetDiscoveredLinks()
    {
        return _discoveredLinks.Values.SelectMany(l => l).ToList();
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

        InitializeComponent();
        
        // Initialize services
        try
        {
            _fullscreenService = new Services.FullscreenImageService(this);
            _overlayManager = new Services.OverlayManager(this);
            _linkDiscoveryService = new Services.LinkDiscoveryService();
            
            // Wire up link discovery events
            _linkDiscoveryService.ProgressChanged += OnLinkDiscoveryProgress;
            _linkDiscoveryService.LinkDiscovered += OnLinkDiscovered;
            _linkDiscoveryService.DiscoveryCompleted += OnLinkDiscoveryCompleted;
        }
        catch (Exception ex) { DebugLogger.LogException("MainWindow ctor: init services", ex); }
        // After InitializeComponent, wire view-specific bridges
        try
        {
            var pcControl = this.FindControl<IndexEditor.Views.PageControllerView>("PageControllerControl");
            if (pcControl != null)
            {
                // Create bridge implementation and assign to VM (SetBridge call is unnecessary)
                var bridge = new PageControllerBridge(pcControl);
                try { if (this.DataContext is Views.MainWindowViewModel mwvm) mwvm.PageControllerBridge = bridge; } catch (Exception ex) { DebugLogger.LogException("MainWindow ctor: assign bridge to VM", ex); }
            }
        }
        catch (Exception ex) { DebugLogger.LogException("MainWindow ctor: wire PageControllerBridge", ex); }

        // Initialize keyboard shortcut dispatcher with handlers
        try
        {
            // Use injected _editorState if available, otherwise create a new instance for backward compatibility
            var editorStateForHandlers = _editorState ?? new IndexEditor.Shared.EditorStateService();
            
            var handlers = new List<Services.KeyboardHandlers.IKeyboardShortcutHandler>
            {
                new Services.KeyboardHandlers.SegmentKeyboardHandler(this),
                new Services.KeyboardHandlers.ArticleKeyboardHandler(this, editorStateForHandlers),
                new Services.KeyboardHandlers.FileKeyboardHandler(this, LoadArticlesFromFolder, _overlayManager),
                new Services.KeyboardHandlers.NavigationKeyboardHandler(this)
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
                            try { DebugLogger.Log("DeleteArticleConfirmBtn.Click invoked"); } catch {}
                            
                            // Get article to delete
                            var vm = this.DataContext as IndexEditor.Views.EditorStateViewModel;
                            Common.Shared.ArticleLine? toDelete = null;
                            if (vm != null) toDelete = vm.SelectedArticle;
                            if (toDelete == null) toDelete = IndexEditor.Shared.EditorState.ActiveArticle;
                            
                            try { DebugLogger.Log($"toDelete={(toDelete==null?"<null>":toDelete.Title)}"); } catch {}
                            
                            if (toDelete != null)
                            {
                                try
                                {
                                    // Use service if available, otherwise use legacy logic
                                    if (_editorActions != null)
                                    {
                                        var newSelection = _editorActions.DeleteArticle(toDelete);
                                        if (vm != null && newSelection != null)
                                        {
                                            vm.SelectedArticle = newSelection;
                                            try { DebugLogger.Log($"SelectedArticle changed to: {newSelection.Title}"); } catch {}
                                        }
                                        else if (vm != null)
                                        {
                                            vm.SelectedArticle = null;
                                        }
                                    }
                                    else
                                    {
                                        // Legacy fallback path
                                        int oldIndex = -1;
                                        try { if (vm != null) oldIndex = vm.Articles.IndexOf(toDelete); else if (IndexEditor.Shared.EditorState.Articles != null) oldIndex = IndexEditor.Shared.EditorState.Articles.IndexOf(toDelete); } catch { }
                                        
                                        IndexEditor.Shared.EditorState.Articles?.Remove(toDelete);
                                        IndexEditor.Shared.EditorState.HasUnsavedChanges = true;
                                        
                                        if (vm != null)
                                        {
                                            try { vm.Articles.Remove(toDelete); } catch { }
                                            if (vm.Articles.Count > 0)
                                            {
                                                int newIndex = Math.Min(Math.Max(0, oldIndex), vm.Articles.Count - 1);
                                                vm.SelectedArticle = vm.Articles[newIndex];
                                                try { DebugLogger.Log($"SelectedArticle changed to index {newIndex}: {vm.SelectedArticle?.Title}"); } catch {}
                                            }
                                            else vm.SelectedArticle = null;
                                        }
                                        
                                        if (IndexEditor.Shared.EditorState.ActiveArticle == toDelete) 
                                            IndexEditor.Shared.EditorState.ActiveArticle = null;
                                        IndexEditor.Shared.EditorState.NotifyStateChanged();
                                    }
                                    
                                    IndexEditor.Shared.ToastService.Show("Article deleted");
                                    try { DebugLogger.Log("Article deletion completed"); } catch {}
                                }
                                catch (Exception ex) { DebugLogger.LogException("DeleteArticleConfirmBtn.Click: delete", ex); }
                            }
                            
                            try { delOverlay.IsVisible = false; } catch (Exception ex) { DebugLogger.LogException("DeleteArticleConfirmBtn.Click: hide overlay", ex); }
                            try { DebugLogger.Log("Delete overlay hidden after confirm"); } catch {}
                        }
                        catch (Exception ex) { DebugLogger.LogException("DeleteArticleConfirmBtn.Click: outer", ex); }
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
                            _discoveredLinks.Clear();
                            var pcView = this.FindControl<Views.PageControllerView>("PageControllerControl");
                            pcView?.UpdateDiscoveredLinks(_discoveredLinks);
                            
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
        // Opened event fired
        try
        {
            // Restore window size/state
            try
            {
                var st = IndexEditor.Shared.WindowStateStore.GetWindowState();
                if (st != null)
                {
                    if (st.IsMaximized) this.WindowState = WindowState.Maximized; // remember maximized
                    else
                    {
                        this.Width = st.Width > 0 ? st.Width : this.Width;
                        this.Height = st.Height > 0 ? st.Height : this.Height;
                    }
                }
            }
            catch (Exception ex) { DebugLogger.LogException("OnWindowOpened: restore window state", ex); }

            this.Activate();
            this.Topmost = true;
            this.Topmost = false;
            // MainWindow activated

            try
            {
                // skip diag
            }
            catch (Exception ex) { DebugLogger.LogException("OnWindowOpened: screen diag", ex); /* swallow screen exception diag */ }

            // Ensure keyboard focus is on the window or the articles list so global shortcuts work immediately
            try
            {
                // Delay focus to allow the window to finish opening
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        this.Focus();
                        // Also focus the invisible host so it receives keyboard input for shortcuts
                        try { var host = this.FindControl<Border>("KeyboardFocusHost"); if (host != null) host.Focus(); } catch (Exception ex) { DebugLogger.LogException("OnWindowOpened: focus host", ex); }
                        // Try to focus the articles listbox
                        var articleList = this.FindControl<IndexEditor.Views.ArticleList>("ArticleListControl");
                        if (articleList != null)
                        {
                            try { var lb = articleList.FindControl<ListBox>("ArticlesListBox"); if (lb != null) lb.Focus(); } catch (Exception ex) { DebugLogger.LogException("OnWindowOpened: focus ArticlesListBox", ex); }
                        }
                    }
                    catch (Exception ex) { DebugLogger.LogException("OnWindowOpened: UI post", ex); }
                });
            }
            catch (Exception ex) { DebugLogger.LogException("OnWindowOpened: outer focus", ex); }

        }
        catch (Exception ex) { DebugLogger.LogException("OnWindowOpened: outer", ex); WriteDiagFile("[TRACE] Exception raising window"); }
    }

    private async void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        try
        {
            // Check for unsaved changes and prompt user
            if (IndexEditor.Shared.EditorState.HasUnsavedChanges)
            {
                try
                {
                    e.Cancel = true; // Cancel the close temporarily
                    
                    // Use DialogService if available, otherwise fall back to ConfirmDialog
                    bool result;
                    if (_dialogService != null)
                    {
                        result = await _dialogService.ShowConfirmationAsync(
                            "You have unsaved changes. Do you want to save before quitting?",
                            "Unsaved Changes");
                    }
                    else
                    {
                        // Fallback to direct dialog (backward compatibility)
                        result = await IndexEditor.Views.ConfirmDialog.ShowDialog(
                            this, 
                            "You have unsaved changes. Do you want to save before quitting?");
                    }
                    
                    if (result)
                    {
                        // User wants to save
                        try
                        {
                            var folder = IndexEditor.Shared.EditorState.CurrentFolder;
                            if (!string.IsNullOrWhiteSpace(folder))
                            {
                                IndexEditor.Shared.IndexSaver.SaveIndex(folder, GetDiscoveredLinks());
                                IndexEditor.Shared.ToastService.Show("Index saved");
                            }
                        }
                        catch (Exception ex)
                        {
                            DebugLogger.LogException("OnWindowClosing: save failed", ex);
                            IndexEditor.Shared.ToastService.Show("Failed to save index");
                        }
                    }
                    
                    // Clear the unsaved changes flag so we can quit without being prompted again
                    IndexEditor.Shared.EditorState.HasUnsavedChanges = false;
                    
                    // Now close the window
                    this.Close();
                }
                catch (Exception ex) { DebugLogger.LogException("OnWindowClosing: prompt save", ex); }
                return;
            }
            
            try
            {
                var isMax = this.WindowState == WindowState.Maximized;
                // If maximized, store previous normal size so restore works later; Avalonia provides ClientSize
                var width = this.Width;
                var height = this.Height;
                if (double.IsNaN(width) || double.IsInfinity(width) || width <= 0) width = 1024;
                if (double.IsNaN(height) || double.IsInfinity(height) || height <= 0) height = 768;
                IndexEditor.Shared.WindowStateStore.SetWindowState(width, height, isMax);
            }
            catch (Exception ex) { DebugLogger.LogException("OnWindowClosing: save window state", ex); }
        }
        catch (Exception ex) { DebugLogger.LogException("OnWindowClosing: outer", ex); }
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
        // Use ImageHelper.ImageExists to check all possible filename patterns
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
            // Don't delete if ArticleEditor has focus
            if (IndexEditor.Shared.EditorState.IsArticleEditorFocused)
            {
                return;
            }

            // Don't allow deletion while active segment exists
            var activeSeg = IndexEditor.Shared.EditorState.ActiveSegment;
            if (activeSeg != null && activeSeg.IsActive)
            {
                IndexEditor.Shared.ToastService.Show("End or cancel the active segment before deleting an article");
                e.Handled = true;
                return;
            }

            // Show delete confirmation if article is selected
            var vm = this.DataContext as IndexEditor.Views.EditorStateViewModel;
            var hasSelection = vm?.SelectedArticle != null || IndexEditor.Shared.EditorState.ActiveArticle != null;

            if (hasSelection)
            {
                var delOverlay = this.FindControl<Border>("DeleteArticleConfirmOverlay");
                if (delOverlay != null)
                {
                    // Update delete confirmation text
                    var article = vm?.SelectedArticle ?? IndexEditor.Shared.EditorState.ActiveArticle;
                    var titleLbl = this.FindControl<TextBlock>("DeleteArticleTitleLabel");
                    if (titleLbl != null && article != null)
                    {
                        titleLbl.Text = article.DisplayTitle ?? "(no title)";
                    }
                    
                    delOverlay.IsVisible = true;
                    e.Handled = true;
                }
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
        try
        {
            if (string.IsNullOrWhiteSpace(folder)) return;
            
            DebugLogger.Log($"LoadArticlesFromFolder: Input folder: '{folder}'");
            
            // Normalize to absolute path to avoid issues with relative paths like './'
            try
            {
                var originalFolder = folder;
                folder = System.IO.Path.GetFullPath(folder);
                DebugLogger.Log($"LoadArticlesFromFolder: Normalized '{originalFolder}' to '{folder}'");
            }
            catch (Exception ex) 
            { 
                DebugLogger.LogException("LoadArticlesFromFolder: GetFullPath", ex); 
            }

            // Clear discovered links from previous folder
            _discoveredLinks.Clear();

            // Use IIndexFileService to load index data
            string fileMag, fileVol, fileNum, fileYear;
            List<Common.Shared.ArticleLine> articles;
            List<Common.Shared.MagazineLink>? loadedLinks;

            if (_indexFileService == null)
            {
                DebugLogger.Log("LoadArticlesFromFolder: ERROR - IIndexFileService is null, cannot load folder");
                IndexEditor.Shared.ToastService.Show("Service initialization error - cannot load folder");
                return;
            }

            try
            {
                (fileMag, fileVol, fileNum, fileYear, articles, loadedLinks) = _indexFileService.LoadFromFolder(folder);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("LoadArticlesFromFolder: IIndexFileService.LoadFromFolder failed", ex);
                IndexEditor.Shared.ToastService.Show("Failed to load index file: " + ex.Message);
                return;
            }

            // Process loaded links into UI-friendly dictionary
            if (loadedLinks != null && loadedLinks.Count > 0)
            {
                foreach (var link in loadedLinks)
                {
                    if (!_discoveredLinks.ContainsKey(link.Page))
                    {
                        _discoveredLinks[link.Page] = new List<Common.Shared.MagazineLink>();
                    }
                    
                    // Check if this exact link already exists for this page (deduplicate)
                    bool isDuplicate = _discoveredLinks[link.Page].Any(l => 
                        l.Magazine == link.Magazine && 
                        l.Volume == link.Volume && 
                        l.Issue == link.Issue);
                    
                    if (!isDuplicate)
                    {
                        _discoveredLinks[link.Page].Add(link);
                    }
                }
                
                var totalLinks = _discoveredLinks.Values.Sum(list => list.Count);
                DebugLogger.Log($"Loaded {totalLinks} unique links from JSON (deduplicated from {loadedLinks.Count})");
            }

            // Set EditorState metadata and articles
            IndexEditor.Shared.EditorState.CurrentMagazine = fileMag ?? string.Empty;
            IndexEditor.Shared.EditorState.CurrentVolume = fileVol ?? string.Empty;
            IndexEditor.Shared.EditorState.CurrentNumber = fileNum ?? string.Empty;
            IndexEditor.Shared.EditorState.CurrentYear = fileYear ?? string.Empty;
            IndexEditor.Shared.EditorState.Articles = articles.Where(a => a.Pages != null && a.Pages.Count > 0).OrderBy(a => a.Pages.Min()).ToList();

            // Validate segments for missing pages
            try
            {
                foreach (var article in IndexEditor.Shared.EditorState.Articles)
                {
                    article.ValidateSegments(folder, ImageExistsInFolder);
                }
            }
            catch (Exception ex) { DebugLogger.LogException("LoadArticlesFromFolder: validate segments", ex); }

            // Update VM collection if present
            try
            {
                var vm = this.DataContext as IndexEditor.Views.EditorStateViewModel;
                if (vm != null)
                {
                    vm.Articles.Clear();
                    foreach (var a in IndexEditor.Shared.EditorState.Articles) vm.Articles.Add(a);
                    // If nothing is selected yet, select the first article so the editor shows content
                    if (vm.SelectedArticle == null && vm.Articles.Count > 0)
                    {
                        try { vm.SelectedArticle = vm.Articles[0]; } catch (Exception ex) { DebugLogger.LogException("LoadArticlesFromFolder: set SelectedArticle", ex); }
                    }
                }
            }
            catch (Exception ex) { DebugLogger.LogException("LoadArticlesFromFolder: update VM", ex); }

            DebugLogger.Log($"LoadArticlesFromFolder: Setting CurrentFolder to '{folder}'");
            IndexEditor.Shared.EditorState.CurrentFolder = folder;

            // Choose first existing image page (prefer 1)
            try
            {
                int? firstImage = IndexEditor.Shared.ImageHelper.FindFirstImageInFolder(folder, 1, 2000);
                if (!firstImage.HasValue)
                {
                    var allPages = IndexEditor.Shared.EditorState.Articles?.SelectMany(a => a.Pages ?? new List<int>()).Distinct().OrderBy(p => p).ToList();
                    if (allPages != null && allPages.Count > 0)
                    {
                        foreach (var p in allPages)
                        {
                            try { if (IndexEditor.Shared.ImageHelper.ImageExists(folder, p)) { firstImage = p; break; } } catch (Exception ex) { DebugLogger.LogException("LoadArticlesFromFolder: ImageExists check", ex); }
                        }
                    }
                }
                IndexEditor.Shared.EditorState.CurrentPage = firstImage ?? 1;
            }
            catch (Exception ex) { DebugLogger.LogException("LoadArticlesFromFolder: choose first image", ex); IndexEditor.Shared.EditorState.CurrentPage = 1; }

            IndexEditor.Shared.EditorState.NotifyStateChanged();
            
            // Clear unsaved changes flag since we just loaded from disk
            IndexEditor.Shared.EditorState.HasUnsavedChanges = false;
            
            // Persist the folder as the most-recently opened so future runs can default to it
            try { IndexEditor.Shared.RecentFolderStore.SetLastOpenedFolder(folder); } catch (Exception ex) { DebugLogger.LogException("LoadArticlesFromFolder: persist recent folder", ex); }
            
            // Update PageControllerView with loaded links
            try
            {
                var pcView = this.FindControl<Views.PageControllerView>("PageControllerControl");
                pcView?.UpdateDiscoveredLinks(_discoveredLinks);
            }
            catch (Exception ex) { DebugLogger.LogException("LoadArticlesFromFolder: update PageController with loaded links", ex); }
            
            // Check if links exist - if so, show button instead of auto-starting discovery
            try
            {
                if (_discoveredLinks.Count > 0)
                {
                    // Links already exist, show button to optionally re-scan
                    var startBtn = this.FindControl<Button>("StartLinkDiscoveryBtn");
                    var statusText = this.FindControl<TextBlock>("StatusText");
                    
                    if (startBtn != null)
                    {
                        startBtn.IsVisible = true;
                        startBtn.Content = "Re-scan for Links";
                    }
                    
                    if (statusText != null)
                    {
                        var linkCount = _discoveredLinks.Values.Sum(l => l.Count);
                        statusText.Text = $"Loaded {linkCount} link(s) from index file";
                    }
                }
                else
                {
                    // No links exist, auto-start discovery
                    var magazineName = IndexEditor.Shared.EditorState.CurrentMagazine ?? "Unknown";
                    _linkDiscoveryService?.StartDiscovery(folder, magazineName);
                }
            }
            catch (Exception ex) { DebugLogger.LogException("LoadArticlesFromFolder: link discovery logic", ex); }
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("LoadArticlesFromFolder failed", ex);
            Console.WriteLine("[ERROR] LoadArticlesFromFolder failed: " + ex);
        }
    }

    private void OnLinkDiscoveryProgress(object? sender, Services.LinkDiscoveryProgressEventArgs e)
    {
        try
        {
            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    var progress = this.FindControl<ProgressBar>("LinkDiscoveryProgress");
                    var status = this.FindControl<TextBlock>("LinkDiscoveryStatus");
                    var statusText = this.FindControl<TextBlock>("StatusText");
                    
                    if (progress != null && status != null)
                    {
                        progress.IsVisible = true;
                        status.IsVisible = true;
                        progress.Value = e.PercentComplete;
                        status.Text = $"Scanning for links: {e.ProcessedPages}/{e.TotalPages} pages";
                    }
                    
                    if (statusText != null)
                    {
                        statusText.Text = $"Discovering links... {e.PercentComplete}%";
                    }
                }
                catch (Exception ex) { DebugLogger.LogException("OnLinkDiscoveryProgress UI update", ex); }
            });
        }
        catch (Exception ex) { DebugLogger.LogException("OnLinkDiscoveryProgress", ex); }
    }

    private void OnLinkDiscovered(object? sender, Services.LinkDiscoveredEventArgs e)
    {
        try
        {
            // Add to discovered links dictionary
            if (!_discoveredLinks.ContainsKey(e.Page))
            {
                _discoveredLinks[e.Page] = new List<Common.Shared.MagazineLink>();
            }
            
            // Check if this exact link already exists for this page (deduplicate)
            bool isDuplicate = _discoveredLinks[e.Page].Any(l => 
                l.Magazine == e.Magazine && 
                l.Volume == e.Volume && 
                l.Issue == e.Issue);
            
            if (!isDuplicate)
            {
                _discoveredLinks[e.Page].Add(new Common.Shared.MagazineLink
                {
                    Page = e.Page,
                    Magazine = e.Magazine,
                    Volume = e.Volume,
                    Issue = e.Issue
                });
                
                DebugLogger.Log($"Link discovered on page {e.Page}: {e.Magazine} Vol.{e.Volume} No.{e.Issue}");
                
                // Update PageControllerView with current links
                Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        var pcView = this.FindControl<Views.PageControllerView>("PageControllerControl");
                        pcView?.UpdateDiscoveredLinks(_discoveredLinks);
                    }
                    catch (Exception ex) { DebugLogger.LogException("OnLinkDiscovered: update PageController", ex); }
                });
            }
            else
            {
                DebugLogger.Log($"Duplicate link skipped on page {e.Page}: {e.Magazine} Vol.{e.Volume} No.{e.Issue}");
            }
        }
        catch (Exception ex) { DebugLogger.LogException("OnLinkDiscovered", ex); }
    }

    private void OnLinkDiscoveryCompleted(object? sender, EventArgs e)
    {
        try
        {
            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    var progress = this.FindControl<ProgressBar>("LinkDiscoveryProgress");
                    var status = this.FindControl<TextBlock>("LinkDiscoveryStatus");
                    var statusText = this.FindControl<TextBlock>("StatusText");
                    
                    if (progress != null && status != null)
                    {
                        progress.IsVisible = false;
                        status.IsVisible = false;
                    }
                    
                    if (statusText != null)
                    {
                        var linkCount = _discoveredLinks.Values.Sum(list => list.Count);
                        statusText.Text = linkCount > 0 
                            ? $"Found {linkCount} link(s) on {_discoveredLinks.Count} page(s)" 
                            : "Ready";
                        
                        // Mark index as modified if links were discovered
                        if (linkCount > 0)
                        {
                            IndexEditor.Shared.EditorState.HasUnsavedChanges = true;
                            DebugLogger.Log($"Index file marked as modified due to {linkCount} discovered link(s)");
                        }
                    }
                    
                    DebugLogger.Log($"Link discovery completed. Found {_discoveredLinks.Count} pages with links.");
                }
                catch (Exception ex) { DebugLogger.LogException("OnLinkDiscoveryCompleted UI update", ex); }
            });
        }
        catch (Exception ex) { DebugLogger.LogException("OnLinkDiscoveryCompleted", ex); }
    }

    // Helper: delete currently selected article (VM selected or EditorState active) and close delete overlay
    private void DeleteSelectedArticleAndCloseOverlay()
    {
        try
        {
            try { DebugLogger.Log("DeleteSelectedArticleAndCloseOverlay called"); } catch {}
             var vm = this.DataContext as IndexEditor.Views.EditorStateViewModel;
             Common.Shared.ArticleLine? toDelete = null;
             if (vm != null) toDelete = vm.SelectedArticle;
             if (toDelete == null) toDelete = IndexEditor.Shared.EditorState.ActiveArticle;
             if (toDelete == null) return;

            int oldIndex = -1;
            try { if (vm != null) oldIndex = vm.Articles.IndexOf(toDelete); else if (IndexEditor.Shared.EditorState.Articles != null) oldIndex = IndexEditor.Shared.EditorState.Articles.IndexOf(toDelete); } catch { }
            IndexEditor.Shared.EditorState.Articles?.Remove(toDelete);
            if (vm != null)
            {
                try { vm.Articles.Remove(toDelete); } catch { }
                if (vm.Articles.Count > 0)
                {
                    int newIndex = Math.Min(Math.Max(0, oldIndex), vm.Articles.Count - 1);
                    vm.SelectedArticle = vm.Articles[newIndex];
                }
                else vm.SelectedArticle = null;
            }

            if (IndexEditor.Shared.EditorState.ActiveArticle == toDelete) IndexEditor.Shared.EditorState.ActiveArticle = null;
            IndexEditor.Shared.EditorState.NotifyStateChanged();
            IndexEditor.Shared.ToastService.Show("Article deleted");

            try { var delOverlay = this.FindControl<Border>("DeleteArticleConfirmOverlay"); if (delOverlay != null) delOverlay.IsVisible = false; } catch { }
        }
        catch (Exception ex) { DebugLogger.LogException("DeleteSelectedArticleAndCloseOverlay", ex); }
    }

    // Handle Ctrl+Up shortcut: navigate to the previous article
    private bool HandleCtrlUpShortcut(KeyEventArgs ke)
    {
        try
        {
            DebugLogger.Log("Shortcut Ctrl+Up invoked");
            if (IndexEditor.Shared.EditorState.IsArticleEditorFocused) { DebugLogger.Log("ArticleEditor focused - ignoring Ctrl+Up"); return false; }
            var vm = this.DataContext as IndexEditor.Views.EditorStateViewModel;
            List<Common.Shared.ArticleLine>? list = null;
            if (vm != null) list = vm.Articles.ToList();
            else if (IndexEditor.Shared.EditorState.Articles != null) list = new List<Common.Shared.ArticleLine>(IndexEditor.Shared.EditorState.Articles);
            if (list == null || list.Count == 0) { DebugLogger.Log("No articles to navigate"); return true; }
            int curIndex = -1;
            if (vm != null && vm.SelectedArticle != null) curIndex = list.IndexOf(vm.SelectedArticle);
            if (curIndex == -1 && IndexEditor.Shared.EditorState.ActiveArticle != null) curIndex = list.IndexOf(IndexEditor.Shared.EditorState.ActiveArticle);
            if (curIndex == -1) curIndex = list.FindIndex(a => a.Pages != null && a.Pages.Contains(IndexEditor.Shared.EditorState.CurrentPage));
            if (curIndex == -1) curIndex = 0;
            int target = Math.Max(0, curIndex - 1);
            if (target == curIndex) { DebugLogger.Log("Already at first article"); return true; }
            var targetArticle = list[target];
            if (vm != null)
            {
                try { vm.SelectedArticle = targetArticle; } catch { }
                try { vm.NavigateToArticle(targetArticle); } catch (Exception ex) { DebugLogger.LogException("MainWindow: NavigateToArticle (Ctrl+Up)", ex); }
            }
            else
            {
                try { IndexEditor.Shared.EditorState.ActiveArticle = targetArticle; } catch { }
                try { int? pick = IndexEditor.Shared.ImageHelper.FindFirstImageInFolder(IndexEditor.Shared.EditorState.CurrentFolder ?? string.Empty, targetArticle.Pages != null && targetArticle.Pages.Count > 0 ? targetArticle.Pages.Min() : 1, 2000); if (pick.HasValue) IndexEditor.Shared.EditorState.CurrentPage = pick.Value; else if (targetArticle.Pages != null && targetArticle.Pages.Count > 0) IndexEditor.Shared.EditorState.CurrentPage = targetArticle.Pages.Min(); IndexEditor.Shared.EditorState.NotifyStateChanged(); } catch (Exception ex) { DebugLogger.LogException("MainWindow: Ctrl+Up fallback navigation", ex); }
            }
            DebugLogger.Log($"Ctrl+Up navigated to article index {target}");
        }
        catch (Exception ex) { DebugLogger.LogException("MainWindow: Ctrl+Up handler", ex); }
        return true;
    }

    // Handle Ctrl+Down shortcut: navigate to the next article
    private bool HandleCtrlDownShortcut(KeyEventArgs ke)
    {
        try
        {
            DebugLogger.Log("Shortcut Ctrl+Down invoked");
            if (IndexEditor.Shared.EditorState.IsArticleEditorFocused) { DebugLogger.Log("ArticleEditor focused - ignoring Ctrl+Down"); return false; }
            var vm = this.DataContext as IndexEditor.Views.EditorStateViewModel;
            List<Common.Shared.ArticleLine>? list = null;
            if (vm != null) list = vm.Articles.ToList();
            else if (IndexEditor.Shared.EditorState.Articles != null) list = new List<Common.Shared.ArticleLine>(IndexEditor.Shared.EditorState.Articles);
            if (list == null || list.Count == 0) { DebugLogger.Log("No articles to navigate"); return true; }
            int curIndex = -1;
            if (vm != null && vm.SelectedArticle != null) curIndex = list.IndexOf(vm.SelectedArticle);
            if (curIndex == -1 && IndexEditor.Shared.EditorState.ActiveArticle != null) curIndex = list.IndexOf(IndexEditor.Shared.EditorState.ActiveArticle);
            if (curIndex == -1) curIndex = list.FindIndex(a => a.Pages != null && a.Pages.Contains(IndexEditor.Shared.EditorState.CurrentPage));
            if (curIndex == -1) curIndex = 0;
            int target = Math.Min(list.Count - 1, curIndex + 1);
            if (target == curIndex) { DebugLogger.Log("Already at last article"); return true; }
            var targetArticle = list[target];
            if (vm != null)
            {
                try { vm.SelectedArticle = targetArticle; } catch { }
                try { vm.NavigateToArticle(targetArticle); } catch (Exception ex) { DebugLogger.LogException("MainWindow: NavigateToArticle (Ctrl+Down)", ex); }
            }
            else
            {
                try { IndexEditor.Shared.EditorState.ActiveArticle = targetArticle; } catch { }
                try { int? pick = IndexEditor.Shared.ImageHelper.FindFirstImageInFolder(IndexEditor.Shared.EditorState.CurrentFolder ?? string.Empty, targetArticle.Pages != null && targetArticle.Pages.Count > 0 ? targetArticle.Pages.Min() : 1, 2000); if (pick.HasValue) IndexEditor.Shared.EditorState.CurrentPage = pick.Value; else if (targetArticle.Pages != null && targetArticle.Pages.Count > 0) IndexEditor.Shared.EditorState.CurrentPage = targetArticle.Pages.Min(); IndexEditor.Shared.EditorState.NotifyStateChanged(); } catch (Exception ex) { DebugLogger.LogException("MainWindow: Ctrl+Down fallback navigation", ex); }
            }
            DebugLogger.Log($"Ctrl+Down navigated to article index {target}");
        }
        catch (Exception ex) { DebugLogger.LogException("MainWindow: Ctrl+Down handler", ex); }
        return true;
    }
}
