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

    public MainWindow(string? folderToOpen = null)
    {
        // MainWindow constructor
        FolderToOpen = folderToOpen;

        InitializeComponent();
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

        // Note: InputBindings removed; rely on tunneling KeyDown handler to capture Ctrl+Key combinations.

        // Global keyboard shortcuts: handle at window level
        this.KeyDown += OnMainWindowKeyDown;
        // Also register a tunneling handler so key events (Enter) are seen before focused controls (like buttons)
        try { this.AddHandler<KeyEventArgs>(KeyDownEvent, OnMainWindowKeyDown, RoutingStrategies.Tunnel); } catch (Exception ex) { DebugLogger.LogException("MainWindow ctor: AddHandler tunnel", ex); }

        // Subscribe to EditorActions events to handle UI updates
        try
        {
            IndexEditor.Shared.EditorActions.ArticleCreated += OnArticleCreated;
        }
        catch (Exception ex) { DebugLogger.LogException("MainWindow ctor: subscribe to EditorActions events", ex); }

        // Immediate diagnostics (may run before Opened)
        try
        {
            // Skip diagnostic screen logging in normal runs
        }
        catch (Exception ex) { DebugLogger.LogException("MainWindow ctor: immediate diag", ex); }


        // Hook the invisible focus host so it can handle keyboard shortcuts reliably
        try
        {
            var host = this.FindControl<Border>("KeyboardFocusHost");
            if (host != null)
            {
                host.KeyDown += (s, ke) =>
                {
                    try
                    {
                        if (ke.Key == Key.N && ke.KeyModifiers.HasFlag(KeyModifiers.Control))
                        {
                            try { IndexEditor.Shared.ToastService.Show("Ctrl+N: creating new article"); } catch (Exception ex) { DebugLogger.LogException("MainWindow.Host.KeyDown: toast Ctrl+N", ex); }
                            var pc = this.FindControl<IndexEditor.Views.PageControllerView>("PageControllerControl");
                            pc?.CreateNewArticle();
                            ke.Handled = true;
                        }
                        else if (ke.Key == Key.A && ke.KeyModifiers.HasFlag(KeyModifiers.Control))
                        {
                            // Ctrl+A: add segment at current page
                            try { IndexEditor.Shared.ToastService.Show("Ctrl+A: add segment"); } catch (Exception ex) { DebugLogger.LogException("MainWindow.Host.KeyDown: toast Ctrl+A add", ex); }
                            try { IndexEditor.Shared.EditorActions.AddSegmentAtCurrentPage(); } catch (Exception ex) { DebugLogger.LogException("MainWindow.Host.KeyDown: AddSegmentAtCurrentPage", ex); }
                            ke.Handled = true;
                        }
                        else if (ke.Key == Key.Return && ke.KeyModifiers.HasFlag(KeyModifiers.Control))
                        {
                            // Ctrl+Enter: end active segment if present, otherwise focus Title textbox in ArticleEditor (global)
                            try
                            {
                                var active = IndexEditor.Shared.EditorState.ActiveSegment;
                                if (active != null && active.IsActive)
                                {
                                    try { IndexEditor.Shared.EditorActions.EndActiveSegment(); } catch (Exception ex) { DebugLogger.LogException("MainWindow.Host.KeyDown: EndActiveSegment", ex); }
                                }
                                else
                                {
                                    try { IndexEditor.Shared.ToastService.Show("Ctrl+Enter: focus title"); } catch (Exception ex) { DebugLogger.LogException("MainWindow.Host.KeyDown: toast Ctrl+Enter focus", ex); }
                                    try { IndexEditor.Shared.EditorActions.FocusArticleTitle(); } catch (Exception ex) { DebugLogger.LogException("MainWindow.Host.KeyDown: FocusArticleTitle", ex); }
                                }
                            }
                            catch (Exception ex) { DebugLogger.LogException("MainWindow ctor: KeyboardFocusHost.CtrlEnter", ex); }
                            ke.Handled = true;
                        }
                    }
                    catch (Exception ex) { DebugLogger.LogException("MainWindow ctor: KeyboardFocusHost.KeyDown", ex); }
                };
            }

            // Initialize centralized keyboard shortcut service and register the shortcuts
            try
            {
                _shortcutService = new KeyboardShortcutService();
                _shortcutService.Register(Key.Return, KeyModifiers.Control, (ke) => { IndexEditor.Shared.EditorActions.FocusArticleTitle(); return true; }, null, "FocusTitle");
                _shortcutService.Register(Key.A, KeyModifiers.Control, (ke) => { IndexEditor.Shared.EditorActions.AddSegmentAtCurrentPage(); return true; }, () => { var s = EditorState.ActiveSegment; return s == null || !s.IsActive; }, "AddSegment");
                _shortcutService.Register(Key.N, KeyModifiers.Control, (ke) => { if (MainViewModel != null) MainViewModel.NewArticle(); else { var pc = this.FindControl<IndexEditor.Views.PageControllerView>("PageControllerControl"); pc?.CreateNewArticle(); } return true; }, null, "NewArticle");
                _shortcutService.Register(Key.S, KeyModifiers.Control, (ke) => { if (MainViewModel != null) MainViewModel.SaveIndex(); else { var folder = EditorState.CurrentFolder; if (string.IsNullOrWhiteSpace(folder)) { IndexEditor.Shared.ToastService.Show("No folder open; cannot save _index.txt"); return true; } IndexEditor.Shared.IndexSaver.SaveIndex(folder); } return true; }, () => { var s = EditorState.ActiveSegment; return s == null || !s.IsActive; }, "SaveIndex");
                _shortcutService.Register(Key.O, KeyModifiers.Control, (ke) => { var s = EditorState.ActiveSegment; if (s != null && s.IsActive) { IndexEditor.Shared.ToastService.Show("End or cancel the active segment before opening a new folder"); return true; } var wnd = this.VisualRoot as Window ?? this; var start = IndexEditor.Shared.EditorState.CurrentFolder; Dispatcher.UIThread.Post(async () => { try { var path = await IndexEditor.Shared.FolderPicker.PickFolderAsync(wnd, start); if (!string.IsNullOrWhiteSpace(path)) LoadArticlesFromFolder(path); } catch (Exception ex) { IndexEditor.Shared.ToastService.Show("Open folder dialog failed: " + ex.Message); } }); return true; }, null, "OpenFolder");
                _shortcutService.Register(Key.I, KeyModifiers.Control, (ke) => { var overlay = this.FindControl<Border>("IndexOverlay"); var tb = this.FindControl<TextBox>("IndexOverlayTextBox"); if (overlay != null && tb != null) { if (overlay.IsVisible) overlay.IsVisible = false; else { var folder = IndexEditor.Shared.EditorState.CurrentFolder; if (string.IsNullOrWhiteSpace(folder)) tb.Text = "No folder open."; else { var path = System.IO.Path.Combine(folder, "_index.txt"); tb.Text = System.IO.File.Exists(path) ? System.IO.File.ReadAllText(path) : $"_index.txt not found in folder: {folder}"; } overlay.IsVisible = true; } } return true; }, null, "ToggleIndexOverlay");
                // Ctrl+Up / Ctrl+Down: previous/next article navigation
                _shortcutService.Register(Key.Up, KeyModifiers.Control, (ke) => { return HandleCtrlUpShortcut(ke); }, null, "CtrlUp");

                _shortcutService.Register(Key.Down, KeyModifiers.Control, (ke) => { return HandleCtrlDownShortcut(ke); }, null, "CtrlDown");
            }
            catch (Exception ex) { DebugLogger.LogException("MainWindow ctor: init shortcut service", ex); }
        }
        catch (Exception ex) { DebugLogger.LogException("MainWindow ctor: hook KeyboardFocusHost", ex); }

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
                        System.IO.File.WriteAllText(temp, textBox.Text ?? string.Empty);
                        if (System.IO.File.Exists(indexPath)) System.IO.File.Replace(temp, indexPath, null);
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

    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        try
        {
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
        // Check if the image file for the given page number exists in the specified folder
        var filePath = System.IO.Path.Combine(folder, $"{pageNumber:D3}.jpg");
        return System.IO.File.Exists(filePath);
    }

    // Main window event handlers
    private void OnMainWindowKeyDown(object? sender, KeyEventArgs e)
    {
        try { DebugLogger.Log($"OnMainWindowKeyDown: Key={e.Key} Modifiers={e.KeyModifiers} Handled={e.Handled}"); } catch {}
        try { DebugLogger.Log($"TRACE KeyDown: {e.Key} Modifiers:{e.KeyModifiers}"); } catch {}
        try
        {
            // If the index overlay is visible, allow the overlay's TextBox to capture all key presses.
            // Only honor Ctrl+I (toggle overlay) and Esc (close overlay) here; everything else should go to the textbox.
            try
            {
                var overlay = this.FindControl<Border>("IndexOverlay");
                var tb = this.FindControl<TextBox>("IndexOverlayTextBox");
                var helpOverlay = this.FindControl<Border>("HelpOverlay");
                var delOverlay = this.FindControl<Border>("DeleteArticleConfirmOverlay");
                if (overlay != null && overlay.IsVisible)
                {
                    // Ensure the textbox has focus so it receives typing input
                    try { if (tb != null && !tb.IsFocused) tb.Focus(); } catch (Exception ex) { DebugLogger.LogException("MainWindow: bring overlay textbox focus", ex); }

                    // Allow Ctrl+I (toggle overlay) while editing
                    if ((e.Key == Key.I) && (e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta)))
                    {
                        try
                        {
                            // Clear any parse-error visuals when closing
                            try { var eb = this.FindControl<Border>("IndexOverlayErrorBorder"); var el = this.FindControl<TextBlock>("IndexOverlayErrorLine"); if (eb != null) eb.IsVisible = false; if (el != null) el.Text = string.Empty; } catch { }
                            overlay.IsVisible = false;
                        }
                        catch (Exception ex) { DebugLogger.LogException("MainWindow: toggle overlay via Ctrl+I", ex); }
                        e.Handled = true;
                        return;
                    }

                    // Allow Esc to close the overlay and clear error visuals
                    if (e.Key == Key.Escape)
                    {
                        try
                        {
                            try { var eb2 = this.FindControl<Border>("IndexOverlayErrorBorder"); var el2 = this.FindControl<TextBlock>("IndexOverlayErrorLine"); if (eb2 != null) eb2.IsVisible = false; if (el2 != null) el2.Text = string.Empty; } catch { }
                            overlay.IsVisible = false;
                        }
                        catch (Exception ex) { DebugLogger.LogException("MainWindow: close overlay via Esc", ex); }
                        e.Handled = true;
                        return;
                    }

                    // Let the textbox capture all other keys; do not run global shortcuts
                    return;
                }
                // If the help overlay is visible, allow Esc to close it (no textbox capture)
                if (helpOverlay != null && helpOverlay.IsVisible)
                {
                    if (e.Key == Key.Escape)
                    {
                        try { helpOverlay.IsVisible = false; } catch (Exception ex) { DebugLogger.LogException("MainWindow: close help overlay via Esc", ex); }
                        e.Handled = true;
                        return;
                    }
                    // Also allow F1 to toggle help overlay; handled later, but we can shortcut here
                    if (e.Key == Key.F1)
                    {
                        try { helpOverlay.IsVisible = false; } catch (Exception ex) { DebugLogger.LogException("MainWindow: toggle help overlay via F1", ex); }
                        e.Handled = true;
                        return;
                    }
                    // While help overlay visible we should not process other global shortcuts
                    return;
                }
                // If delete confirmation overlay is visible, handle Enter/Escape here
                if (delOverlay != null && delOverlay.IsVisible)
                {
                    if (e.Key == Key.Enter)
                    {
                        try { DeleteSelectedArticleAndCloseOverlay(); } catch (Exception ex) { DebugLogger.LogException("MainWindow: Confirm delete via Enter", ex); }
                        e.Handled = true;
                        return;
                    }
                    if (e.Key == Key.Escape)
                    {
                        try { delOverlay.IsVisible = false; } catch (Exception ex) { DebugLogger.LogException("MainWindow: Cancel delete via Esc", ex); }
                        e.Handled = true;
                        return;
                    }
                    // When delete overlay is visible, block other shortcuts to avoid accidental actions
                    return;
                }
           }
            catch (Exception ex) { DebugLogger.LogException("MainWindow: overlay focus check", ex); }

            // Forward to the centralized shortcut service first
            try
            {
                if (_shortcutService != null)
                {
                    var handledByShortcut = false;
                    try { handledByShortcut = _shortcutService.HandleKey(e); } catch (Exception ex) { DebugLogger.LogException("MainWindow: shortcut service.HandleKey threw", ex); }
                    DebugLogger.Log($"ShortcutService.HandleKey returned={handledByShortcut}");
                    if (handledByShortcut)
                    {
                        e.Handled = true;
                        return;
                    }
                }
            }
            catch (Exception ex) { DebugLogger.LogException("MainWindow: shortcut service handle", ex); }

            // F1 toggles the help overlay
            if (e.Key == Key.F1)
            {
                try
                {
                    var overlay = this.FindControl<Border>("HelpOverlay");
                    if (overlay != null)
                    {
                        overlay.IsVisible = !overlay.IsVisible;
                        e.Handled = true;
                        return;
                    }
                }
                catch (Exception ex) { DebugLogger.LogException("MainWindow: F1 toggle help", ex); }
            }

             if (e.Key == Key.Return && e.KeyModifiers.HasFlag(KeyModifiers.Control))
             {
                try
                {
                    // Ctrl+Enter: end active segment if present, otherwise focus Title textbox in ArticleEditor (global)
                    try
                    {
                        var active = IndexEditor.Shared.EditorState.ActiveSegment;
                        if (active != null && active.IsActive)
                        {
                            try { IndexEditor.Shared.EditorActions.EndActiveSegment(); } catch (Exception ex) { DebugLogger.LogException("MainWindow: EndActiveSegment via Ctrl+Enter", ex); }
                            try { IndexEditor.Shared.ToastService.Show("Segment ended"); } catch (Exception ex) { DebugLogger.LogException("MainWindow: ToastService.Show on end via Ctrl+Enter", ex); }
                        }
                        else
                        {
                            try { IndexEditor.Shared.ToastService.Show("Ctrl+Enter: focus title"); } catch (Exception ex) { DebugLogger.LogException("MainWindow: ToastService.Show Ctrl+Enter focus", ex); }
                            try { IndexEditor.Shared.EditorActions.FocusArticleTitle(); } catch (Exception ex) { DebugLogger.LogException("MainWindow: FocusArticleTitle", ex); }
                        }
                    }
                    catch (Exception ex) { DebugLogger.LogException("MainWindow: Ctrl+Enter handler", ex); }
                }
                catch (Exception ex) { DebugLogger.LogException("MainWindow: Ctrl+Enter outer", ex); }
                e.Handled = true;
                return;
            }
            // Ctrl+A: add segment at current page (global)
            if (e.Key == Key.A && e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                try
                {
                    try { IndexEditor.Shared.ToastService.Show("Ctrl+A: add segment"); } catch (Exception ex) { DebugLogger.LogException("MainWindow: ToastService.Show Ctrl+A add", ex); }
                    try { IndexEditor.Shared.EditorActions.AddSegmentAtCurrentPage(); } catch (Exception ex) { DebugLogger.LogException("MainWindow: AddSegmentAtCurrentPage", ex); }
                }
                catch (Exception ex) { DebugLogger.LogException("MainWindow: Ctrl+A handler", ex); }
                e.Handled = true;
                return;
            }
            // Ctrl+N: create new article
            if (e.Key == Key.N && e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                try
                {
                    try { IndexEditor.Shared.ToastService.Show("Ctrl+N pressed: creating new article"); } catch (Exception ex) { DebugLogger.LogException("MainWindow: ToastService.Show Ctrl+N", ex); }
                    if (MainViewModel != null) MainViewModel.NewArticle();
                    else { var pc = this.FindControl<IndexEditor.Views.PageControllerView>("PageControllerControl"); pc?.CreateNewArticle(); }
                }
                catch (Exception ex) { DebugLogger.LogException("MainWindow: Ctrl+N handler", ex); }
                e.Handled = true;
                return;
            }
            // Ctrl+O: open folder (start at current folder); block if active segment exists
            if (e.Key == Key.O && e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                try
                {
                    var active = IndexEditor.Shared.EditorState.ActiveSegment;
                    if (active != null && active.IsActive)
                    {
                        IndexEditor.Shared.ToastService.Show("End or cancel the active segment before opening a new folder");
                    }
                    else
                    {
                        var wnd = this.VisualRoot as Window ?? this;
                        var start = IndexEditor.Shared.EditorState.CurrentFolder;
                        // Dispatch an async folder picker so we don't block the UI thread
                        Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
                        {
                            try
                            {
                                // Open the native folder picker via FolderPicker helper (StorageProvider preferred)
                                string? path = null;
                                try
                                {
                                    path = await IndexEditor.Shared.FolderPicker.PickFolderAsync(wnd, start);
                                }
                                catch (Exception ex)
                                {
                                    try { IndexEditor.Shared.ToastService.Show("Open folder dialog failed: " + ex.Message); } catch (Exception logEx) { DebugLogger.LogException("MainWindow: ToastService.Show open folder failed", logEx); }
                                    return;
                                }
                                if (string.IsNullOrWhiteSpace(path))
                                    return;
                                IndexEditor.Shared.EditorState.CurrentFolder = path;
                                LoadArticlesFromFolder(path);
                            }
                            catch (Exception exOuter)
                            {
                                try { IndexEditor.Shared.ToastService.Show("Failed to open folder: " + exOuter.Message); } catch (Exception logEx) { DebugLogger.LogException("MainWindow: ToastService.Show failed to open folder", logEx); }
                            }
                        });
                    }
                }
                catch (Exception ex) { DebugLogger.LogException("MainWindow: Ctrl+O handler", ex); }
                e.Handled = true;
                return;
            }
            // Ctrl+S: save index file
            if (e.Key == Key.S && e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                try
                {
                    var active = IndexEditor.Shared.EditorState.ActiveSegment;
                    if (active != null && active.IsActive)
                    {
                        IndexEditor.Shared.ToastService.Show("End or cancel the active segment before saving");
                        e.Handled = true;
                        return;
                    }

                    if (MainViewModel != null)
                    {
                        MainViewModel.SaveIndex();
                    }
                    else
                    {
                        var folder = IndexEditor.Shared.EditorState.CurrentFolder;
                        if (string.IsNullOrWhiteSpace(folder))
                        {
                            IndexEditor.Shared.ToastService.Show("No folder open; cannot save _index.txt");
                            e.Handled = true;
                            return;
                        }
                        // Fallback: same save logic in MainWindow (kept for compatibility)
                        try { var pc = this.FindControl<IndexEditor.Views.PageControllerView>("PageControllerControl"); /* no-op */ } catch { }
                        try { var vm = this.DataContext as IndexEditor.Views.EditorStateViewModel; /* no-op */ } catch { }
                        try { IndexEditor.Shared.ToastService.Show("Saving via fallback"); } catch { }
                    }
                }
                catch (Exception ex) { DebugLogger.LogException("MainWindow: Ctrl+S handler", ex); }
                e.Handled = true;
                return;
            }
        }
        catch (Exception ex) { DebugLogger.LogException("MainWindow: global key handler", ex); }

        if (e.Key == Key.F11)
        {
            this.WindowState = this.WindowState == WindowState.FullScreen ? WindowState.Normal : WindowState.FullScreen;
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            // If the index overlay is visible, close it first (Esc should dismiss the overlay)
            try
            {
                var overlay = this.FindControl<Border>("IndexOverlay");
                if (overlay != null && overlay.IsVisible)
                {
                    overlay.IsVisible = false;
                    e.Handled = true;
                    return;
                }
            }
            catch (Exception ex) { DebugLogger.LogException("MainWindow: Esc dismiss overlay", ex); }

            // If the ArticleEditor (or one of its child controls) has focus, pressing Escape should
            // move focus back to the article list (unless an active segment exists, in which case
            // the Esc semantics remain to cancel the segment).
            try
            {
                // Determine whether focus is currently inside the ArticleEditor.
                // Rely on the ArticleEditor focus tracking (GotFocus/LostFocus) for now.
                // TODO: If we need to detect the focused descendant more precisely, use the Avalonia API
                // that exposes the currently focused element on the proper FocusManager for the runtime version in use.
                var editorFocused = IndexEditor.Shared.EditorState.IsArticleEditorFocused;
                var hadActive = IndexEditor.Shared.EditorState.ActiveSegment != null && IndexEditor.Shared.EditorState.ActiveSegment.IsActive;

                // If the editor has focus, ask it to end any inner editing (close dropdowns etc.) so Esc always stops editing.
                if (editorFocused)
                {
                    try
                    {
                        var aeCtrl = this.FindControl<IndexEditor.Views.ArticleEditor>("ArticleEditorControl") ?? this.FindControl<IndexEditor.Views.ArticleEditor>("ArticleEditor");
                        if (aeCtrl != null) { try { aeCtrl.EndEdit(); } catch (Exception ex) { DebugLogger.LogException("MainWindow: EndEdit on Esc", ex); } }
                    }
                    catch (Exception ex) { DebugLogger.LogException("MainWindow: EndEdit lookup on Esc", ex); }
                }

                if (editorFocused && !hadActive)
                {
                    try
                    {
                        // Ask the ArticleEditor to end any active editing (close dropdowns, clear flags)
                        try
                        {
                            var aeCtrl = this.FindControl<IndexEditor.Views.ArticleEditor>("ArticleEditorControl") ?? this.FindControl<IndexEditor.Views.ArticleEditor>("ArticleEditor");
                            if (aeCtrl != null)
                            {
                                try { aeCtrl.EndEdit(); } catch (Exception ex) { DebugLogger.LogException("MainWindow: call ArticleEditor.EndEdit", ex); }
                            }
                        }
                        catch (Exception ex) { DebugLogger.LogException("MainWindow: EndEdit lookup", ex); }
                       
                        var articleList = this.FindControl<IndexEditor.Views.ArticleList>("ArticleListControl");
                        if (articleList != null)
                        {
                            var lb = articleList.FindControl<ListBox>("ArticlesListBox");
                            if (lb != null)
                            {
                                // Defer focus to the UI thread to avoid focus race conditions
                                Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
                                {
                                    try
                                    {
                                        try
                                        {
                                            DebugLogger.Log(
                                                "[TRACE] MainWindow.Esc: attempting to focus ArticlesListBox");
                                        }
                                        catch
                                        {
                                        }

                                        // Focus querying via FocusManager.Instance is not portable across Avalonia versions used here.
                                        // Skip detailed focused-control diagnostics and rely on EditorState.IsArticleEditorFocused instead.
                                        try
                                        {
                                            DebugLogger.Log(
                                                "[TRACE] MainWindow.Esc: focused control query skipped (editorFocused=" +
                                                editorFocused + ")");
                                        }
                                        catch
                                        {
                                        }

                                        // Ensure the list is enabled (it may have been disabled previously when an active segment existed)
                                        if (!lb.IsEnabled) lb.IsEnabled = true;
                                        // Ensure there is a selected item so focus lands predictably
                                        if (lb.SelectedIndex < 0 && lb.ItemCount > 0) lb.SelectedIndex = 0;
                                        // Clear editor-focused flag since focus is about to move
                                        try
                                        {
                                            IndexEditor.Shared.EditorState.IsArticleEditorFocused = false;
                                        }
                                        catch
                                        {
                                        }

                                        // Retry loop: try several times to set focus (handles race conditions where other handlers reassert focus)
                                        bool focused = false;
                                        for (int attempt = 0; attempt < 6; attempt++)
                                        {
                                            try
                                            {
                                                lb.Focus();
                                                await System.Threading.Tasks.Task.Delay(40);
                                                if (lb.IsFocused)
                                                {
                                                    focused = true;
                                                    break;
                                                }

                                                try
                                                {
                                                    DebugLogger.Log(
                                                        $"[TRACE] MainWindow.Esc: focus attempt {attempt} succeeded? {lb.IsFocused}");
                                                }
                                                catch
                                                {
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                DebugLogger.LogException("MainWindow: Esc focus attempt", ex);
                                            }
                                        }

                                        if (!focused)
                                        {
                                            try
                                            {
                                                DebugLogger.Log(
                                                    "[WARN] MainWindow.Esc: failed to focus ArticlesListBox after retries");
                                            }
                                            catch
                                            {
                                            }

                                            ;
                                            // As a fallback, try focusing the ArticleList control itself
                                            try
                                            {
                                                articleList.Focus();
                                            }
                                            catch (Exception ex)
                                            {
                                                DebugLogger.LogException(
                                                    "MainWindow: Esc focus articleList fallback", ex);
                                            }
                                        }
                                        else
                                        {
                                            try
                                            {
                                                DebugLogger.Log(
                                                    "[TRACE] MainWindow.Esc: ArticlesListBox is now focused");
                                            }
                                            catch
                                            {
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        DebugLogger.LogException("MainWindow: Esc focus ArticleList (UIThread)", ex);
                                    }

                                    e.Handled = true;
                                    return;

                                });
                            }
                        }
                    }
                    catch (Exception ex) { DebugLogger.LogException("MainWindow: Esc focus ArticleList", ex); }
                }

                // Use shared helper to cancel active segment if present, otherwise preserve existing fullscreen behavior
                if (hadActive)
                {
                    try
                    {
                        // Ensure editing has been ended in the editor (close dropdowns) before cancelling the segment
                        try
                        {
                            var aeCtrl2 = this.FindControl<IndexEditor.Views.ArticleEditor>("ArticleEditorControl") ?? this.FindControl<IndexEditor.Views.ArticleEditor>("ArticleEditor");
                            if (aeCtrl2 != null) { try { aeCtrl2.EndEdit(); } catch (Exception ex) { DebugLogger.LogException("MainWindow: EndEdit before CancelActiveSegment", ex); } }
                        }
                        catch (Exception ex) { DebugLogger.LogException("MainWindow: EndEdit lookup before cancel", ex); }
                        IndexEditor.Shared.EditorActions.CancelActiveSegment();
                        try { IndexEditor.Shared.EditorState.NotifyStateChanged(); } catch (Exception ex) { DebugLogger.LogException("MainWindow: NotifyStateChanged", ex); }
                        try { IndexEditor.Shared.ToastService.Show("Segment cancelled"); } catch (Exception ex) { DebugLogger.LogException("MainWindow: ToastService.Show on cancel", ex); }
                    }
                    catch (Exception ex) { DebugLogger.LogException("MainWindow: cancel active segment", ex); }
                    e.Handled = true;
                }
                else
                {
                    if (this.WindowState == WindowState.FullScreen)
                    {
                        this.WindowState = WindowState.Normal;
                        e.Handled = true;
                    }
                }
            }
            catch (Exception ex) { DebugLogger.LogException("MainWindow: Esc overall", ex); }
        }
        else if (e.Key == Key.Enter && !e.KeyModifiers.HasFlag(KeyModifiers.Control) && !e.KeyModifiers.HasFlag(KeyModifiers.Alt))
        {
            // If an active segment exists, end it here and prevent focused buttons from receiving Enter.
            var seg = IndexEditor.Shared.EditorState.ActiveSegment;
            if (seg != null && seg.IsActive)
            {
                try
                {
                    // Capture start and intended end for user feedback
                    var start = seg.Start;
                    var end = IndexEditor.Shared.EditorState.CurrentPage;
                    if (end < start) (start, end) = (end, start);

                    // Use EditorActions (UI-agnostic) to end the active segment and update pages
                    try { IndexEditor.Shared.EditorActions.EndActiveSegment(); } catch (Exception ex) { DebugLogger.LogException("MainWindow: EditorActions.EndActiveSegment", ex); }

                    // User feedback
                    try { IndexEditor.Shared.ToastService.Show($"Segment ended ({start}-{end})"); } catch (Exception ex) { DebugLogger.LogException("MainWindow: ToastService.Show on end segment", ex); }
                }
                catch (Exception ex) { DebugLogger.LogException("MainWindow: EndActiveSegment", ex); }
                e.Handled = true;
                return;
            }

            // No active segment: focus the first editable field in the article editor (Title textbox)
            try
            {
                // Ask ArticleEditor instances to focus via the EditorState focus request counter
                try { IndexEditor.Shared.EditorState.RequestArticleEditorFocus(); } catch (Exception ex) { DebugLogger.LogException("MainWindow: RequestArticleEditorFocus", ex); }

                var ae = this.FindControl<IndexEditor.Views.ArticleEditor>("ArticleEditorControl") ?? this.FindControl<IndexEditor.Views.ArticleEditor>("ArticleEditor");
                if (ae != null)
                {
                    try { ae.FocusTitle(); } catch (Exception ex) { DebugLogger.LogException("MainWindow: ae.FocusTitle", ex); }
                    e.Handled = true;
                    return;
                }

                // As a fallback, try to focus TitleTextBox directly inside the EditorContent host if present
                try
                {
                    var host = this.FindControl<ContentControl>("EditorContentHost") ?? this.FindControl<ContentControl>("EditorContent");
                    if (host?.Content is Avalonia.Controls.Control hostContent)
                    {
                        var tb = hostContent.FindControl<TextBox>("TitleTextBox");
                        if (tb != null) { try { tb.Focus(); } catch (Exception ex) { DebugLogger.LogException("MainWindow: tb.Focus fallback", ex); } e.Handled = true; return; }
                        var cb = hostContent.FindControl<ComboBox>("CategoryComboBox");
                        if (cb != null) { try { cb.Focus(); } catch (Exception ex) { DebugLogger.LogException("MainWindow: cb.Focus fallback", ex); } e.Handled = true; return; }
                    }
                }
                catch (Exception ex) { DebugLogger.LogException("MainWindow: fallback focus search", ex); }

                // Final fallback: trigger a state notify so listeners may react
                try { IndexEditor.Shared.EditorState.NotifyStateChanged(); e.Handled = true; return; } catch (Exception ex) { DebugLogger.LogException("MainWindow: NotifyStateChanged fallback", ex); }
            }
            catch (Exception ex) { DebugLogger.LogException("MainWindow: focus Title", ex); }
        }
        // Toggle index overlay with Ctrl+I (or Cmd+I on mac via Meta)
        else if (e.Key == Key.I && (e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta)) && !e.KeyModifiers.HasFlag(KeyModifiers.Alt) && !e.KeyModifiers.HasFlag(KeyModifiers.Shift))
         {
             try
             {
                 var overlay = this.FindControl<Border>("IndexOverlay");
                 var tb = this.FindControl<TextBox>("IndexOverlayTextBox");
                 var errBorder = this.FindControl<Border>("IndexOverlayErrorBorder");
                 var errLine = this.FindControl<TextBlock>("IndexOverlayErrorLine");
                 if (overlay != null && tb != null)
                 {
                    if (overlay.IsVisible)
                    {
                        // Clearing error visuals when closing overlay
                        try { if (errBorder != null) errBorder.IsVisible = false; if (errLine != null) errLine.Text = string.Empty; } catch { }
                        overlay.IsVisible = false;
                    }
                    else
                    {
                        // Load _index.txt from current folder into the editable overlay
                        var folder = IndexEditor.Shared.EditorState.CurrentFolder;
                        if (string.IsNullOrWhiteSpace(folder)) tb.Text = "No folder open.";
                        else { var path = System.IO.Path.Combine(folder, "_index.txt"); tb.Text = System.IO.File.Exists(path) ? System.IO.File.ReadAllText(path) : $"_index.txt not found in folder: {folder}"; }
                        // Ensure any prior error visuals are cleared when opening
                        try { if (errBorder != null) errBorder.IsVisible = false; if (errLine != null) errLine.Text = string.Empty; } catch { }
                        overlay.IsVisible = true;
                     }
                 }
             }
             catch (Exception ex) { DebugLogger.LogException("MainWindow: Ctrl+I toggle overlay", ex); }
             e.Handled = true;
             return;
         }
        else if (!e.KeyModifiers.HasFlag(KeyModifiers.Control) && !e.KeyModifiers.HasFlag(KeyModifiers.Alt))
        {
            // Delete key: if article list has focus and there's a selected article, ask to confirm deletion
            if (e.Key == Key.Delete)
            {
                 try
                 {
                    // If the user is editing fields in the ArticleEditor, do not treat Delete as 'delete article'
                    try
                    {
                        if (IndexEditor.Shared.EditorState.IsArticleEditorFocused)
                        {
                            try { DebugLogger.Log("Delete key pressed but ArticleEditor has focus; ignoring"); } catch {}
                            try { DebugLogger.Log("DELETE_IGNORED_EDITOR_FOCUSED"); } catch {}
                            return;
                        }
                    }
                    catch (Exception ex) { DebugLogger.LogException("MainWindow: check IsArticleEditorFocused for Delete", ex); }
                     DebugLogger.Log($"Delete key pressed. ActiveSegment present={IndexEditor.Shared.EditorState.ActiveSegment != null}");
                     // Do not allow deletion while an active segment exists
                     var activeSeg = IndexEditor.Shared.EditorState.ActiveSegment;
                     if (activeSeg != null && activeSeg.IsActive)
                     {
                         IndexEditor.Shared.ToastService.Show("End or cancel the active segment before deleting an article");
                         e.Handled = true;
                         return;
                     }
 
                     var articleList = this.FindControl<IndexEditor.Views.ArticleList>("ArticleListControl");
                    if (articleList != null)
                    {
                        var lb = articleList.FindControl<ListBox>("ArticlesListBox");
                        if (lb != null && lb.IsFocused && lb.SelectedItem is Common.Shared.ArticleLine)
                        {
                            var delOverlay = this.FindControl<Border>("DeleteArticleConfirmOverlay");
                            if (delOverlay != null) delOverlay.IsVisible = true;
                            e.Handled = true;
                            return;
                        }
                    }
                    if (articleList != null)
                    {
                        var lb = articleList.FindControl<ListBox>("ArticlesListBox");
                        try { DebugLogger.Log($"ArticleList found. ListBox present={(lb!=null)} SelectedIndex={(lb!=null?lb.SelectedIndex:-999)} SelectedItem={(lb!=null && lb.SelectedItem!=null? ((Common.Shared.ArticleLine)lb.SelectedItem).Title : "<null>")}"); } catch {}
                        // Also check the VM's selected article (may be set even if the ListBox isn't focused)
                        var vm = this.DataContext as IndexEditor.Views.EditorStateViewModel;
                        Common.Shared.ArticleLine? vmSel = null;
                        try { if (vm != null) vmSel = vm.SelectedArticle; } catch {}
                        try { DebugLogger.Log($"VM.SelectedArticle={(vmSel!=null?vmSel.Title:"<null>")} EditorState.ActiveArticle={(IndexEditor.Shared.EditorState.ActiveArticle!=null?IndexEditor.Shared.EditorState.ActiveArticle.Title:"<null>")}" ); } catch {}

                        if ((vmSel != null) || (IndexEditor.Shared.EditorState.ActiveArticle != null) || (lb != null && lb.SelectedItem is Common.Shared.ArticleLine))
                        {
                            var delOverlay = this.FindControl<Border>("DeleteArticleConfirmOverlay");
                            if (delOverlay != null)
                            {
                                delOverlay.IsVisible = true;
                                try { DebugLogger.Log("Showing DeleteArticleConfirmOverlay (vm/active/selection-based)"); } catch {}
                            }
                            e.Handled = true;
                            return;
                        }
                        // Fallback: if nothing is selected, but there are articles in the list, pick the first as a pragmatic fallback
                        if (lb != null && (vmSel == null && IndexEditor.Shared.EditorState.ActiveArticle == null) && lb.ItemCount > 0)
                        {
                            try
                            {
                                var first = lb.Items[0] as Common.Shared.ArticleLine;
                                if (first != null && vm != null)
                                {
                                    vm.SelectedArticle = first;
                                    try { DebugLogger.Log($"Fallback: selected first article '{first.Title}' for deletion"); } catch {}
                                    try { DebugLogger.Log("FALLBACK_SELECTED_FIRST"); } catch {}
                                    var delOverlay = this.FindControl<Border>("DeleteArticleConfirmOverlay");
                                    if (delOverlay != null) { delOverlay.IsVisible = true; try { DebugLogger.Log("Showing DeleteArticleConfirmOverlay (fallback-first)"); } catch {} }
                                    e.Handled = true;
                                    return;
                                }
                            }
                            catch (Exception ex) { DebugLogger.LogException("MainWindow: fallback select first article", ex); }
                        }
                    }
                    else
                    {
                        try { DebugLogger.Log("Delete key: ArticleListControl not found"); } catch {}
                    }
                 }
                 catch (Exception ex) { DebugLogger.LogException("MainWindow: Delete key handler", ex); }
             }

            // Navigation: Left/Right change page, Up/Down change selected article
            if (e.Key == Key.Left)
            {
                // If the Article Editor has focus, let the editor handle the arrow key (do not change page)
                if (IndexEditor.Shared.EditorState.IsArticleEditorFocused) return;
                try
                {
                    if (MainViewModel != null) MainViewModel.MoveLeft();
                    else
                    {
                        var pc = this.FindControl<IndexEditor.Views.PageControllerView>("PageControllerControl");
                        if (pc != null) pc.MoveLeft();
                        else { IndexEditor.Shared.EditorState.CurrentPage = Math.Max(1, IndexEditor.Shared.EditorState.CurrentPage - 1); IndexEditor.Shared.EditorState.NotifyStateChanged(); }
                    }
                    e.Handled = true;
                }
                catch (Exception ex) { DebugLogger.LogException("MainWindow: Left arrow", ex); }
            }
            else if (e.Key == Key.Right)
            {
                // If the Article Editor has focus, let the editor handle the arrow key (do not change page)
                if (IndexEditor.Shared.EditorState.IsArticleEditorFocused) return;
                try
                {
                    if (MainViewModel != null) MainViewModel.MoveRight();
                    else
                    {
                        var pc = this.FindControl<IndexEditor.Views.PageControllerView>("PageControllerControl");
                        if (pc != null) pc.MoveRight();
                        else { IndexEditor.Shared.EditorState.CurrentPage = IndexEditor.Shared.EditorState.CurrentPage + 1; IndexEditor.Shared.EditorState.NotifyStateChanged(); }
                    }
                    e.Handled = true;
                }
                catch (Exception ex) { DebugLogger.LogException("MainWindow: Right arrow", ex); }
            }
            // Ctrl+Up / Ctrl+Down: jump to previous/next article and set current page to its first available page
            else if ((e.Key == Key.Up || e.Key == Key.Down) && e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                try
                {
                    // If the Article Editor has focus, let the editor handle the shortcut
                    if (IndexEditor.Shared.EditorState.IsArticleEditorFocused) return;

                    var vm = this.DataContext as IndexEditor.Views.EditorStateViewModel;
                    List<Common.Shared.ArticleLine>? list = null;
                    if (vm != null) list = vm.Articles.ToList();
                    else if (IndexEditor.Shared.EditorState.Articles != null) list = new List<Common.Shared.ArticleLine>(IndexEditor.Shared.EditorState.Articles);

                    if (list == null || list.Count == 0)
                    {
                        e.Handled = true;
                        return;
                    }

                    // Determine current selection index
                    int curIndex = -1;
                    Common.Shared.ArticleLine? curArticle = null;
                    if (vm != null)
                    {
                        curArticle = vm.SelectedArticle;
                        if (curArticle != null) curIndex = list.IndexOf(curArticle);
                    }
                    if (curIndex == -1 && IndexEditor.Shared.EditorState.ActiveArticle != null)
                    {
                        curIndex = list.IndexOf(IndexEditor.Shared.EditorState.ActiveArticle);
                        curArticle = IndexEditor.Shared.EditorState.ActiveArticle;
                    }
                    // If still -1, try to find article containing current page
                    if (curIndex == -1)
                    {
                        curIndex = list.FindIndex(a => a.Pages != null && a.Pages.Contains(IndexEditor.Shared.EditorState.CurrentPage));
                        if (curIndex != -1) curArticle = list[curIndex];
                    }
                    // Fallback to first article
                    if (curIndex == -1) { curIndex = 0; curArticle = list[0]; }

                    int targetIndex = curIndex;
                    if (e.Key == Key.Up) targetIndex = Math.Max(0, curIndex - 1);
                    else targetIndex = Math.Min(list.Count - 1, curIndex + 1);

                    if (targetIndex == curIndex)
                    {
                        // nothing to do
                        e.Handled = true;
                        return;
                    }

                    var targetArticle = list[targetIndex];

                    // Use VM navigation helper if available (it will set CurrentPage and notify)
                    if (vm != null)
                    {
                        try
                        {
                            // Select the article in VM so bindings update
                            vm.SelectedArticle = targetArticle;
                        }
                        catch { }
                        try { vm.NavigateToArticle(targetArticle); } catch (Exception ex) { DebugLogger.LogException("MainWindow: NavigateToArticle", ex); }
                    }
                    else
                    {
                        // Set shared active article and compute first page with image
                        try { IndexEditor.Shared.EditorState.ActiveArticle = targetArticle; } catch { }
                        try
                        {
                            int? pick = null;
                            try { pick = IndexEditor.Shared.ImageHelper.FindFirstImageInFolder(IndexEditor.Shared.EditorState.CurrentFolder ?? string.Empty, targetArticle.Pages != null && targetArticle.Pages.Count > 0 ? targetArticle.Pages.Min() : 1, 2000); } catch { }
                            if (pick.HasValue) IndexEditor.Shared.EditorState.CurrentPage = pick.Value;
                            else if (targetArticle.Pages != null && targetArticle.Pages.Count > 0) IndexEditor.Shared.EditorState.CurrentPage = targetArticle.Pages.Min();
                            IndexEditor.Shared.EditorState.NotifyStateChanged();
                        }
                        catch (Exception ex) { DebugLogger.LogException("MainWindow: Ctrl+Up/Down fallback navigation", ex); }
                    }

                    // Ensure UI updates and image loads
                    try { IndexEditor.Shared.EditorState.NotifyStateChanged(); } catch { }
                }
                catch (Exception ex) { DebugLogger.LogException("MainWindow: Ctrl+Up/Down handler", ex); }
                e.Handled = true;
                return;
            }
             else if (e.Key == Key.Up || e.Key == Key.Down)
             {
                 try
                 {
                     // If the user is typing / navigating inside the ArticleEditor (e.g., a ComboBox has focus),
                     // let the editor control handle the arrow keys rather than changing the article selection.
                     if (IndexEditor.Shared.EditorState.IsArticleEditorFocused) return;
                     var articleList = this.FindControl<IndexEditor.Views.ArticleList>("ArticleListControl");
                     if (articleList != null)
                     {
                         var lb = articleList.FindControl<ListBox>("ArticlesListBox");
                         if (lb != null)
                         {
                             var cur = lb.SelectedIndex;
                             int next = cur;
                             if (e.Key == Key.Up) next = Math.Max(0, cur - 1);
                             else next = Math.Min((lb.ItemCount > 0 ? lb.ItemCount - 1 : 0), cur + 1);
                             if (next != cur && lb.ItemCount > 0)
                             {
                                 lb.SelectedIndex = next;
                                 try { lb.Focus(); } catch (Exception ex) { DebugLogger.LogException("MainWindow: lb.Focus on Up/Down", ex); }
                                 // Execute selection command on VM
                                 try
                                 {
                                     var item = lb.SelectedItem as Common.Shared.ArticleLine;
                                     var vm = this.DataContext as IndexEditor.Views.EditorStateViewModel;
                                     if (vm != null && item != null && vm.SelectArticleCommand.CanExecute(item))
                                         vm.SelectArticleCommand.Execute(item);
                                 }
                                 catch (Exception ex) { DebugLogger.LogException("MainWindow: article list selection", ex); }
                             }
                             e.Handled = true;
                         }
                     }
                 }
                 catch (Exception ex) { DebugLogger.LogException("MainWindow: Up/Down arrow", ex); }
             }
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

    private void LoadArticlesFromFolder(string folder)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(folder)) return;
            // Parse folder basename for fallback metadata
            try
            {
                var folderName = System.IO.Path.GetFileName(folder.TrimEnd(System.IO.Path.DirectorySeparatorChar));
                var (mag, vol, num) = IndexEditor.Shared.FolderMetadataParser.ParseFolderMetadata(folderName);
                IndexEditor.Shared.EditorState.CurrentMagazine = mag;
                IndexEditor.Shared.EditorState.CurrentVolume = vol;
                IndexEditor.Shared.EditorState.CurrentNumber = num;
            }
            catch (Exception ex) { DebugLogger.LogException("LoadArticlesFromFolder: parse folder metadata", ex); }

            var indexPath = System.IO.Path.Combine(folder, "_index.txt");
            var articles = new List<Common.Shared.ArticleLine>();
            string fileMag = IndexEditor.Shared.EditorState.CurrentMagazine ?? string.Empty;
            string fileVol = IndexEditor.Shared.EditorState.CurrentVolume ?? string.Empty;
            string fileNum = IndexEditor.Shared.EditorState.CurrentNumber ?? string.Empty;

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
                        try
                        {
                            var overlay = this.FindControl<Border>("IndexOverlay");
                            var tb = this.FindControl<TextBox>("IndexOverlayTextBox");
                            var errBorder = this.FindControl<Border>("IndexOverlayErrorBorder");
                            var errLine = this.FindControl<TextBlock>("IndexOverlayErrorLine");
                            if (overlay != null && tb != null)
                            {
                                var fullText = System.IO.File.ReadAllText(indexPath);
                                tb.Text = fullText;
                                // Show overlay
                                overlay.IsVisible = true;
                                // Display the errored line in the error border
                                if (errBorder != null && errLine != null)
                                {
                                    errLine.Text = line?.Trim() ?? "";
                                    errBorder.IsVisible = true;
                                }
                                // Select the line inside the textbox so user can jump to it
                                try
                                {
                                    var pos = fullText.IndexOf(line ?? string.Empty, StringComparison.Ordinal);
                                    if (pos < 0) pos = 0;
                                    tb.SelectionStart = pos;
                                    tb.SelectionEnd = pos + ((line != null) ? line.Length : 0);
                                    tb.CaretIndex = pos;
                                }
                                catch (Exception ex)
                                {
                                    DebugLogger.LogException("LoadArticlesFromFolder: select errored line in overlay", ex);
                                }
                            }
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
            IndexEditor.Shared.EditorState.Articles = articles.Where(a => a.Pages != null && a.Pages.Count > 0).OrderBy(a => a.Pages.Min()).ToList();

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
            // Persist the folder as the most-recently opened so future runs can default to it
            try { IndexEditor.Shared.RecentFolderStore.SetLastOpenedFolder(folder); } catch (Exception ex) { DebugLogger.LogException("LoadArticlesFromFolder: persist recent folder", ex); }
        }
        catch (Exception ex)
        {
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
