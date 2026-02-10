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
                            try { IndexEditor.Shared.ToastService.Show("Ctrl+A: adding segment"); } catch (Exception ex) { DebugLogger.LogException("MainWindow.Host.KeyDown: toast Ctrl+A", ex); }
                            var pc = this.FindControl<IndexEditor.Views.PageControllerView>("PageControllerControl");
                            pc?.AddSegmentAtCurrentPage();
                            ke.Handled = true;
                        }
                    }
                    catch (Exception ex) { DebugLogger.LogException("MainWindow ctor: KeyboardFocusHost.KeyDown", ex); }
                };
            }
        }
        catch (Exception ex) { DebugLogger.LogException("MainWindow ctor: hook KeyboardFocusHost", ex); }

        // Configure startup and Opened handler
        try
        {
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.Opened += OnWindowOpened;
            WriteDiagFile("[TRACE] Opened handler attached");
        }
        catch (Exception ex) { DebugLogger.LogException("MainWindow ctor: Opened handler", ex); }

        // Wire up overlay buttons if present
        try
        {
            var closeBtn = this.FindControl<Button>("IndexOverlayCloseBtn");
            var saveBtn = this.FindControl<Button>("IndexOverlaySaveBtn");
            var overlay = this.FindControl<Border>("IndexOverlay");
            var textBox = this.FindControl<TextBox>("IndexOverlayTextBox");
            if (closeBtn != null && overlay != null)
            {
                closeBtn.Click += (s, e) => { try { overlay.IsVisible = false; } catch (Exception ex) { DebugLogger.LogException("IndexOverlayCloseBtn.Click", ex); } };
            }
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
                        Console.WriteLine("[ERROR] saving _index.txt: " + ex);
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

    private void OnWindowOpened(object? sender, EventArgs e)
    {
        // Opened event fired
        try
        {
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

    // Parsing helpers
    private Common.Shared.ArticleLine? ParseArticleLine(string line)
    {
        var parts = IndexFileParser.SplitRespectingEscapedCommas(line);
        if (parts.Count < 2)
            return null;
        var article = new Common.Shared.ArticleLine();
        article.Pages = IndexFileParser.ParsePageNumbers(parts[0], out bool hasError);
        article.HasPageNumberError = hasError;
        if (article.Pages.Count == 0)
            return null;

        // Populate segments from pages so UI shows per-part segments
        try
        {
            var pages = article.Pages;
            try { article.Segments.Clear(); } catch (Exception ex) { DebugLogger.LogException("MainWindow.ParseArticleLine: clear segments", ex); }
            if (pages != null && pages.Count > 0)
            {
                pages.Sort();
                int i = 0;
                while (i < pages.Count)
                {
                    int start = pages[i];
                    int end = start;
                    i++;
                    while (i < pages.Count && pages[i] == end + 1)
                    {
                        end = pages[i];
                        i++;
                    }
                    try { article.Segments.Add(new Common.Shared.Segment(start, end)); } catch (Exception ex) { DebugLogger.LogException("MainWindow.ParseArticleLine: add segment", ex); }
                }
            }
        }
        catch (Exception ex) { DebugLogger.LogException("MainWindow.ParseArticleLine: outer", ex); }

        article.Category = parts.Count > 1 ? parts[1] : "";
        if (string.IsNullOrWhiteSpace(article.Category))
            return null;
        if (article.Category.Equals("Contents", StringComparison.OrdinalIgnoreCase))
            article.Category = "Index";
        article.Title = parts.Count > 2 ? parts[2] : "";
        if (parts.Count > 3)
            article.ModelNames = parts[3].Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        if (parts.Count > 4)
        {
            var ageParts = parts[4].Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var ages = new List<int?>();
            foreach (var ap in ageParts)
            {
                if (int.TryParse(ap, out int a))
                    ages.Add(a);
                else
                    ages.Add(null);
            }
            article.Ages = ages;
        }
        if (parts.Count > 5 && !string.IsNullOrWhiteSpace(parts[5]))
            article.Photographers = parts[5].Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        // Common formats vary: some index files have 7 columns (pages,category,title,models,ages,photographers,measurements)
        // while others use 8 columns with an authors column before measurements.
        if (parts.Count == 7)
        {
            // 7-column format: treat parts[6] as Measurements
            if (!string.IsNullOrWhiteSpace(parts[6]))
                article.Measurements = parts[6].Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        }
        else
        {
            // 8+ column format: parts[6]=Authors, parts[7]=Measurements
            if (parts.Count > 6 && !string.IsNullOrWhiteSpace(parts[6]))
                article.Authors = parts[6].Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            if (parts.Count > 7 && !string.IsNullOrWhiteSpace(parts[7]))
                article.Measurements = parts[7].Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        }

        // Notify bindings in case the UI is already bound to this instance
        try
        {
            article.NotifyPropertyChanged(nameof(Common.Shared.ArticleLine.Authors));
            article.NotifyPropertyChanged(nameof(Common.Shared.ArticleLine.Author0));
            article.NotifyPropertyChanged(nameof(Common.Shared.ArticleLine.FormattedCardText));
        }
        catch (Exception ex) { DebugLogger.LogException("MainWindow.ParseArticleLine: notify bindings", ex); }

        if (article.ModelNames == null || article.ModelNames.Count == 0)
            article.ModelNames = new List<string> { string.Empty };
        if (article.Photographers == null || article.Photographers.Count == 0)
            article.Photographers = new List<string> { string.Empty };
        if (article.Measurements == null || article.Measurements.Count == 0)
            article.Measurements = new List<string> { string.Empty };
        if (article.Ages == null || article.Ages.Count == 0)
            article.Ages = new List<int?> { null };

        // Fallback: some older index files place the author in the photographers column for Humour entries.
        try
        {
            var cat = (article.Category ?? string.Empty).Trim().ToLowerInvariant();
            bool authorsEmpty = article.Authors == null || article.Authors.All(a => string.IsNullOrWhiteSpace(a));
            bool photographersHave = article.Photographers != null && article.Photographers.Any(p => !string.IsNullOrWhiteSpace(p));
            if (cat == "humour" && authorsEmpty && photographersHave)
            {
                article.Authors = article.Photographers.Select(p => p).ToList();
            }
        }
        catch (Exception ex) { DebugLogger.LogException("MainWindow.ParseArticleLine: fallback authors", ex); }

        // Ensure UI bindings update if the article instance is already bound
        try
        {
            article.NotifyPropertyChanged(nameof(Common.Shared.ArticleLine.Authors));
            article.NotifyPropertyChanged(nameof(Common.Shared.ArticleLine.Author0));
            article.NotifyPropertyChanged(nameof(Common.Shared.ArticleLine.FormattedCardText));
        }
        catch (Exception ex) { DebugLogger.LogException("MainWindow.ParseArticleLine: notify bindings (post-fallback)", ex); }

        return article;
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
        // KeyDown received
        // Handle global key down events here
        // For example, toggle fullscreen on F11
        // Ctrl+A: add segment at current page (if possible)
        try
        {
            if (e.Key == Key.A && e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                try
                {
                    if (MainViewModel != null) MainViewModel.AddSegment();
                    else
                    {
                        var pc = this.FindControl<IndexEditor.Views.PageControllerView>("PageControllerControl");
                        pc?.AddSegmentAtCurrentPage();
                    }
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

            // Use shared helper to cancel active segment if present, otherwise preserve existing fullscreen behavior
            var hadActive = IndexEditor.Shared.EditorState.ActiveSegment != null && IndexEditor.Shared.EditorState.ActiveSegment.IsActive;
            if (hadActive)
            {
                try
                {
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

                    var pc = this.FindControl<IndexEditor.Views.PageControllerView>("PageControllerControl");
                    if (pc != null)
                        pc.EndActiveSegment();
                    else
                    {
                        // Fallback: set seg.End to current page and clear active segment
                        seg.End = IndexEditor.Shared.EditorState.CurrentPage;
                        seg.WasNew = false;
                        try { seg.CurrentPreviewEnd = null; } catch (Exception ex) { DebugLogger.LogException("MainWindow: clear CurrentPreviewEnd", ex); }
                        IndexEditor.Shared.EditorState.ActiveSegment = null;
                        IndexEditor.Shared.EditorState.NotifyStateChanged();
                    }

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
                var ae = this.FindControl<IndexEditor.Views.ArticleEditor>("ArticleEditorControl") ?? this.FindControl<IndexEditor.Views.ArticleEditor>("ArticleEditor");
                if (ae != null)
                {
                    ae.FocusTitle();
                    e.Handled = true;
                    return;
                }

                // As a final fallback, trigger the EditorState.StateChanged hook which ArticleEditor listens to
                try
                {
                    IndexEditor.Shared.EditorState.NotifyStateChanged();
                    e.Handled = true;
                    return;
                }
                catch (Exception ex) { DebugLogger.LogException("MainWindow: NotifyStateChanged", ex); }
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
                 if (overlay != null && tb != null)
                 {
                    if (overlay.IsVisible)
                    {
                        overlay.IsVisible = false;
                    }
                    else
                    {
                        // Load _index.txt from current folder into the editable overlay
                        var folder = IndexEditor.Shared.EditorState.CurrentFolder;
                        if (string.IsNullOrWhiteSpace(folder))
                        {
                            tb.Text = "No folder open.";
                        }
                        else
                        {
                            var path = System.IO.Path.Combine(folder, "_index.txt");
                            if (System.IO.File.Exists(path))
                            {
                                try { tb.Text = System.IO.File.ReadAllText(path); } catch (Exception ex) { tb.Text = $"Error reading file: {ex.Message}"; }
                            }
                            else
                            {
                                tb.Text = $"_index.txt not found in folder: {folder}";
                            }
                        }
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
            else if (e.Key == Key.Up || e.Key == Key.Down)
            {
                try
                {
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
                    var parsed = ParseArticleLine(line);
                    if (parsed != null) articles.Add(parsed);
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
        }
        catch (Exception ex)
        {
            Console.WriteLine("[ERROR] LoadArticlesFromFolder failed: " + ex);
        }
    }
}
