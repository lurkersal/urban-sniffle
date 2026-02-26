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

    public MainWindow(string? folderToOpen = null)
    {
        // MainWindow constructor
        FolderToOpen = folderToOpen;

        InitializeComponent();
        
        // Initialize services
        try
        {
            _fullscreenService = new Services.FullscreenImageService(this);
            _overlayManager = new Services.OverlayManager(this);
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
            var handlers = new List<Services.KeyboardHandlers.IKeyboardShortcutHandler>
            {
                new Services.KeyboardHandlers.SegmentKeyboardHandler(this),
                new Services.KeyboardHandlers.ArticleKeyboardHandler(this),
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
                             // Perform deletion of selected article
                             var vm = this.DataContext as IndexEditor.Views.EditorStateViewModel;
                             Common.Shared.ArticleLine? toDelete = null;
                             if (vm != null) toDelete = vm.SelectedArticle;
                             if (toDelete == null) toDelete = IndexEditor.Shared.EditorState.ActiveArticle;
                            try { DebugLogger.Log($"toDelete={(toDelete==null?"<null>":toDelete.Title)}"); } catch {}
                             if (toDelete != null)
                             {
                                 try
                                 {
                                     // Compute selected index first so we can pick the next sensible selection
                                     int oldIndex = -1;
                                     try { if (vm != null) oldIndex = vm.Articles.IndexOf(toDelete); else if (IndexEditor.Shared.EditorState.Articles != null) oldIndex = IndexEditor.Shared.EditorState.Articles.IndexOf(toDelete); } catch { }
                                     // Remove from shared state
                                     IndexEditor.Shared.EditorState.Articles?.Remove(toDelete);
                                     // Mark that we have unsaved changes
                                     IndexEditor.Shared.EditorState.HasUnsavedChanges = true;
                                     // Update VM list if present
                                     if (vm != null)
                                     {
                                         try { vm.Articles.Remove(toDelete); } catch { }
                                         // Choose next selection: prefer previous index if possible, otherwise clamp
                                         if (vm.Articles.Count > 0)
                                         {
                                             int newIndex = Math.Min(Math.Max(0, oldIndex), vm.Articles.Count - 1);
                                             vm.SelectedArticle = vm.Articles[newIndex];
                                            try { DebugLogger.Log($"SelectedArticle changed to index {newIndex}: {vm.SelectedArticle?.Title}"); } catch {}
                                         }
                                         else vm.SelectedArticle = null;
                                     }
                                      // Clear active article/segment if it referred to deleted article
                                      if (IndexEditor.Shared.EditorState.ActiveArticle == toDelete) IndexEditor.Shared.EditorState.ActiveArticle = null;
                                      IndexEditor.Shared.EditorState.NotifyStateChanged();
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
             }
             catch (Exception ex) { DebugLogger.LogException("MainWindow ctor: wire delete confirmation buttons", ex); }

            var closeBtn = this.FindControl<Button>("IndexOverlayCloseBtn");
            var saveBtn = this.FindControl<Button>("IndexOverlaySaveBtn");
            var overlay = this.FindControl<Border>("IndexOverlay");
            var textBox = this.FindControl<TextBox>("IndexOverlayTextBox");
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

            if (saveBtn != null && overlay != null && textBox != null)
            {
                saveBtn.Click += (s, e) =>
                {
                    try
                    {
                        var folder = IndexEditor.Shared.EditorState.CurrentFolder;
                        if (string.IsNullOrWhiteSpace(folder))
                        {
                            IndexEditor.Shared.ToastService.Show("No folder open; cannot save _index.txt");
                            return;
                        }
                        var indexPath = System.IO.Path.Combine(folder, "_index.txt");
                        // Atomic write
                        var temp = indexPath + ".tmp";
                        var backupPath = indexPath + "~";
                        System.IO.File.WriteAllText(temp, textBox.Text ?? string.Empty);
                        if (System.IO.File.Exists(indexPath))
                        {
                            // Create backup before replacing
                            if (System.IO.File.Exists(backupPath))
                            {
                                System.IO.File.Delete(backupPath);
                            }
                            System.IO.File.Copy(indexPath, backupPath);
                            System.IO.File.Replace(temp, indexPath, null);
                        }
                        else System.IO.File.Move(temp, indexPath);
                        IndexEditor.Shared.ToastService.Show("_index.txt saved");
                        // Reload articles from folder to reflect edits
                        LoadArticlesFromFolder(folder);
                    }
                    catch (Exception ex)
                    {
                        IndexEditor.Shared.ToastService.Show("Failed to save _index.txt");
                        DebugLogger.LogException("MainWindow.SaveIndex: saving _index.txt", ex);
                    }
                };
            }
            // Allow pressing 'i' again to close overlay; handled in OnMainWindowKeyDown
        }
        catch (Exception ex) { DebugLogger.LogException("MainWindow ctor: wire up overlay buttons", ex); }

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
                    var result = await IndexEditor.Views.ConfirmDialog.ShowDialog(
                        this, 
                        "You have unsaved changes. Do you want to save before quitting?");
                    
                    if (result)
                    {
                        // User wants to save
                        try
                        {
                            var folder = IndexEditor.Shared.EditorState.CurrentFolder;
                            if (!string.IsNullOrWhiteSpace(folder))
                            {
                                IndexEditor.Shared.IndexSaver.SaveIndex(folder);
                                IndexEditor.Shared.ToastService.Show("Index saved");
                            }
                        }
                        catch (Exception ex)
                        {
                            DebugLogger.LogException("OnWindowClosing: save failed", ex);
                            IndexEditor.Shared.ToastService.Show("Failed to save index");
                        }
                    }
                    
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

    // Parsing helpers
    private Common.Shared.ArticleLine? ParseArticleLine(string line)
    {
        try
        {
            // Delegate parsing to the centralized parser to keep behaviour consistent
            return IndexEditor.Shared.IndexFileParser.ParseArticleLine(line);
        }
        catch (FormatException)
        {
            // Bubble up format exceptions for the caller to handle (overlay UI)
            throw;
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("MainWindow.ParseArticleLine: delegate parser threw", ex);
            return null;
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
            var overlay = this.FindControl<Border>("IndexOverlay");
            var tb = this.FindControl<TextBox>("IndexOverlayTextBox");

            // Index overlay: capture all keys except Ctrl+I, Esc, and Ctrl+S
            if (overlay != null && overlay.IsVisible)
            {
                try { if (tb != null && !tb.IsFocused) tb.Focus(); } 
                catch (Exception ex) { DebugLogger.LogException("MainWindow: overlay textbox focus", ex); }

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
                if (e.Key == Key.Enter)
                {
                    try { DeleteSelectedArticleAndCloseOverlay(); }
                    catch (Exception ex) { DebugLogger.LogException("MainWindow: confirm delete", ex); }
                    e.Handled = true;
                    return true;
                }
                if (e.Key == Key.Escape)
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
            
            // Parse folder basename for fallback metadata
            try
            {
                var folderName = System.IO.Path.GetFileName(folder.TrimEnd(System.IO.Path.DirectorySeparatorChar));
                var (mag, vol, num, year) = IndexEditor.Shared.FolderMetadataParser.ParseFolderMetadata(folderName);
                IndexEditor.Shared.EditorState.CurrentMagazine = mag;
                IndexEditor.Shared.EditorState.CurrentVolume = vol;
                IndexEditor.Shared.EditorState.CurrentNumber = num;
                IndexEditor.Shared.EditorState.CurrentYear = year;
            }
            catch (Exception ex) { DebugLogger.LogException("LoadArticlesFromFolder: parse folder metadata", ex); }

            var indexPath = System.IO.Path.Combine(folder, "_index.txt");
            var articles = new List<Common.Shared.ArticleLine>();
            string fileMag = IndexEditor.Shared.EditorState.CurrentMagazine ?? string.Empty;
            string fileVol = IndexEditor.Shared.EditorState.CurrentVolume ?? string.Empty;
            string fileNum = IndexEditor.Shared.EditorState.CurrentNumber ?? string.Empty;
            string fileYear = IndexEditor.Shared.EditorState.CurrentYear ?? string.Empty;

            if (System.IO.File.Exists(indexPath))
            {
                var lines = System.IO.File.ReadAllLines(indexPath);
                int articleStartIndex = 0;
                for (int i = 0; i < lines.Length; i++)
                {
                    var raw = lines[i];
                    if (string.IsNullOrWhiteSpace(raw)) { articleStartIndex = i + 1; continue; }
                    var trimmed = raw.Trim();
                    if (trimmed.StartsWith("#"))
                    {
                        var content = trimmed.TrimStart('#').Trim();
                        if (content.StartsWith("Magazine:", StringComparison.OrdinalIgnoreCase))
                            fileMag = content.Substring("Magazine:".Length).Trim();
                        else if (content.StartsWith("Volume:", StringComparison.OrdinalIgnoreCase) || content.StartsWith("Vol:", StringComparison.OrdinalIgnoreCase))
                        {
                            var val = content.Contains(":" ) ? content.Substring(content.IndexOf(':') + 1).Trim() : content;
                            fileVol = val.Replace("Volume:", string.Empty).Replace("Vol:", string.Empty).Trim();
                        }
                        else if (content.StartsWith("Number:", StringComparison.OrdinalIgnoreCase) || content.StartsWith("No:", StringComparison.OrdinalIgnoreCase))
                        {
                            var val = content.Contains(":") ? content.Substring(content.IndexOf(':') + 1).Trim() : content;
                            fileNum = val.Replace("Number:", string.Empty).Replace("No:", string.Empty).Trim();
                        }
                        else if (content.StartsWith("Year:", StringComparison.OrdinalIgnoreCase))
                        {
                            fileYear = content.Substring("Year:".Length).Trim();
                        }
                        articleStartIndex = i + 1;
                        continue;
                    }
                    // First non-comment line: try CSV metadata
                    var parts = IndexFileParser.SplitRespectingEscapedCommas(trimmed);
                    if (parts.Count >= 3)
                    {
                        string Unescape(string s) => s.Replace("\\,", ",");
                        fileMag = Unescape(parts[0]);
                        fileVol = Unescape(parts[1]);
                        fileNum = Unescape(parts[2]);
                        if (parts.Count >= 4)
                        {
                            fileYear = Unescape(parts[3]);
                        }
                        articleStartIndex = i + 1;
                    }
                    else
                    {
                        articleStartIndex = i;
                    }
                    break;
                }

                for (int i = articleStartIndex; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#")) continue;
                    try
                    {
                        var parsed = ParseArticleLine(line);
                        if (parsed != null) articles.Add(parsed);
                    }
                    catch (FormatException fx)
                    {
                        // Show error and open index overlay with full file contents for user correction
                        try { IndexEditor.Shared.ToastService.Show("_index.txt format error: " + fx.Message); } catch { }
                        
                        // Use overlay manager to show the error
                        try
                        {
                            var fullText = System.IO.File.ReadAllText(indexPath);
                            _overlayManager?.ShowIndexOverlayError(line ?? string.Empty, fullText);
                        }
                        catch (Exception ex)
                        {
                            DebugLogger.LogException("LoadArticlesFromFolder: show overlay on format error", ex);
                        }

                        // Stop further parsing; let the user fix the file
                        break;
                    }
                }
            }

            // Set EditorState metadata and articles
            IndexEditor.Shared.EditorState.CurrentMagazine = fileMag;
            IndexEditor.Shared.EditorState.CurrentVolume = fileVol;
            IndexEditor.Shared.EditorState.CurrentNumber = fileNum;
            IndexEditor.Shared.EditorState.CurrentYear = fileYear;
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
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("LoadArticlesFromFolder failed", ex);
            Console.WriteLine("[ERROR] LoadArticlesFromFolder failed: " + ex);
        }
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
