using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.Interactivity;

namespace IndexEditor;

public partial class MainWindow : Window
{
    public string? FolderToOpen { get; }

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
        // InitializeComponent completed

        // Note: InputBindings removed; rely on tunneling KeyDown handler to capture Ctrl+Key combinations.

        // Global keyboard shortcuts: handle at window level
        this.KeyDown += OnMainWindowKeyDown;
        // Also register a tunneling handler so key events (Enter) are seen before focused controls (like buttons)
        try { this.AddHandler<KeyEventArgs>(KeyDownEvent, OnMainWindowKeyDown, RoutingStrategies.Tunnel); } catch { }

        // Immediate diagnostics (may run before Opened)
        try
        {
            // Skip diagnostic screen logging in normal runs
        }
        catch { }

        this.DataContext = new IndexEditor.Views.EditorStateViewModel();
        // DataContext assigned

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
                            try { IndexEditor.Shared.ToastService.Show("Ctrl+N: creating new article"); } catch { }
                            var pc = this.FindControl<IndexEditor.Views.PageControllerView>("PageControllerControl");
                            pc?.CreateNewArticle();
                            ke.Handled = true;
                        }
                        else if (ke.Key == Key.A && ke.KeyModifiers.HasFlag(KeyModifiers.Control))
                        {
                            try { IndexEditor.Shared.ToastService.Show("Ctrl+A: adding segment"); } catch { }
                            var pc = this.FindControl<IndexEditor.Views.PageControllerView>("PageControllerControl");
                            pc?.AddSegmentAtCurrentPage();
                            ke.Handled = true;
                        }
                    }
                    catch { }
                };
            }
        }
        catch { }

        // Configure startup and Opened handler
        try
        {
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.Opened += OnWindowOpened;
            WriteDiagFile("[TRACE] Opened handler attached");
        }
        catch { }

        // Load articles from folder if provided
        if (!string.IsNullOrWhiteSpace(FolderToOpen))
        {
            var indexPath = System.IO.Path.Combine(FolderToOpen, "_index.txt");
            if (System.IO.File.Exists(indexPath))
            {
                try
                {
                    var lines = System.IO.File.ReadAllLines(indexPath);
                    // First pass: parse optional header metadata lines starting with '#'
                    foreach (var raw in lines)
                    {
                        var line = raw.Trim();
                        if (!line.StartsWith("#")) break; // headers are expected at the top
                        var content = line.TrimStart('#').Trim();
                        if (content.StartsWith("Magazine:", StringComparison.OrdinalIgnoreCase))
                        {
                            IndexEditor.Shared.EditorState.CurrentMagazine = content.Substring("Magazine:".Length).Trim();
                        }
                        else if (content.StartsWith("Volume:", StringComparison.OrdinalIgnoreCase) || content.StartsWith("Vol:", StringComparison.OrdinalIgnoreCase))
                        {
                            var val = content.Contains(":") ? content.Substring(content.IndexOf(':') + 1).Trim() : content;
                            IndexEditor.Shared.EditorState.CurrentVolume = val.Replace("Volume:", string.Empty).Replace("Vol:", string.Empty).Trim();
                        }
                        else if (content.StartsWith("Number:", StringComparison.OrdinalIgnoreCase) || content.StartsWith("No:", StringComparison.OrdinalIgnoreCase))
                        {
                            var val = content.Contains(":") ? content.Substring(content.IndexOf(':') + 1).Trim() : content;
                            IndexEditor.Shared.EditorState.CurrentNumber = val.Replace("Number:", string.Empty).Replace("No:", string.Empty).Trim();
                        }
                        // continue reading header lines; article parsing happens below
                    }
                    var articles = new List<Common.Shared.ArticleLine>();
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                            continue;
                        var parsed = ParseArticleLine(line);
                        if (parsed != null)
                        {
                            articles.Add(parsed);
                            var segs = parsed.Segments != null ? string.Join(",", parsed.Segments.Select(s => s.Display)) : string.Empty;
                            // parsed article
                        }
                        else
                        {
                            // skipped line
                        }
                    }
                    // total articles parsed

                    // Order by first page
                    articles = articles.Where(a => a.Pages != null && a.Pages.Count > 0)
                                       .OrderBy(a => a.Pages.Min())
                                       .ToList();

                    IndexEditor.Shared.EditorState.Articles = articles;
                    // Also ensure the current view-model's ObservableCollection is populated so the UI updates reliably
                    try
                    {
                        var vm = this.DataContext as IndexEditor.Views.EditorStateViewModel;
                        if (vm != null)
                        {
                            try
                            {
                                vm.Articles.Clear();
                                foreach (var a in articles)
                                    vm.Articles.Add(a);
                            }
                            catch { }
                        }
                    }
                    catch { }

                    // Store the opened folder so controllers can load page images
                    IndexEditor.Shared.EditorState.CurrentFolder = FolderToOpen;

                    // On startup, set CurrentPage to the lowest page number that has an image in the opened folder
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(FolderToOpen))
                        {
                            var allPages = articles.Where(a => a.Pages != null).SelectMany(a => a.Pages).Distinct().OrderBy(p => p);
                            int? firstWithImage = null;
                            foreach (var p in allPages)
                            {
                                if (ImageExistsInFolder(FolderToOpen, p))
                                {
                                    firstWithImage = p;
                                    break;
                                }
                            }
                            if (firstWithImage.HasValue)
                                IndexEditor.Shared.EditorState.CurrentPage = firstWithImage.Value;

                            // Also select the article that starts at that page so it opens in the editor
                            var vm = this.DataContext as IndexEditor.Views.EditorStateViewModel;
                            if (vm != null && firstWithImage.HasValue)
                            {
                                var match = IndexEditor.Shared.EditorState.Articles.FirstOrDefault(a => a.Pages != null && a.Pages.Count > 0 && a.Pages.Min() == firstWithImage.Value);
                                if (match != null)
                                {
                                    try
                                    {
                                        if (vm.SelectArticleCommand.CanExecute(match))
                                            vm.SelectArticleCommand.Execute(match);
                                        // Scroll the ArticleList to show the selected item (if the control is available)
                                        try
                                        {
                                            if (this.FindControl<IndexEditor.Views.ArticleList>("ArticleListControl") is IndexEditor.Views.ArticleList articleList)
                                            {
                                                articleList.ScrollToArticle(match);
                                                // Set highlight on UI thread
                                                Avalonia.Threading.Dispatcher.UIThread.Post(() => { try { match.WasAutoHighlighted = true; } catch { } });
                                                // Clear highlight after a short delay (clear on UI thread)
                                                _ = System.Threading.Tasks.Task.Run(async () => { await System.Threading.Tasks.Task.Delay(800); Avalonia.Threading.Dispatcher.UIThread.Post(() => { try { match.WasAutoHighlighted = false; } catch { } }); });
                                                // Focus the ListBox so keyboard navigation is immediate (on UI thread)
                                                try { var lb = articleList.FindControl<ListBox>("ArticlesListBox"); if (lb != null) Avalonia.Threading.Dispatcher.UIThread.Post(() => lb.Focus()); } catch { }
                                            }
                                            else
                                            {
                                                // Try to find via visual tree: the ArticleList is in the first column Border
                                                var articleListControl = this.FindControl<IndexEditor.Views.ArticleList>("ArticlesListBox");
                                                if (articleListControl != null)
                                                {
                                                    articleListControl.ScrollToArticle(match);
                                                    Avalonia.Threading.Dispatcher.UIThread.Post(() => { try { match.WasAutoHighlighted = true; } catch { } });
                                                    _ = System.Threading.Tasks.Task.Run(async () => { await System.Threading.Tasks.Task.Delay(800); Avalonia.Threading.Dispatcher.UIThread.Post(() => { try { match.WasAutoHighlighted = false; } catch { } }); });
                                                    try { var lb = articleListControl.FindControl<ListBox>("ArticlesListBox"); if (lb != null) Avalonia.Threading.Dispatcher.UIThread.Post(() => lb.Focus()); } catch { }
                                                }
                                            }
                                        }
                                        catch { }
                                    }
                                    catch { }
                                }
                            }
                        }
                    }
                    catch { }

                    IndexEditor.Shared.EditorState.NotifyStateChanged();

                    // Do not auto-select the first article; selection will happen when user requests it
                }
                catch { WriteDiagFile("Error reading _index.txt"); }
            }
            else
            {
                // Index file not found
            }
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
            catch { /* swallow screen exception diag */ }

            // Ensure keyboard focus is on the window or the articles list so global shortcuts work immediately
            try
            {
                // Delay focus to allow the window to finish opening
                Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
                {
                    try
                    {
                        this.Focus();
                        // Also focus the invisible host so it receives keyboard input for shortcuts
                        try { var host = this.FindControl<Border>("KeyboardFocusHost"); if (host != null) host.Focus(); } catch { }
                        // Try to focus the articles listbox
                        var articleList = this.FindControl<IndexEditor.Views.ArticleList>("ArticleListControl");
                        if (articleList != null)
                        {
                            try { var lb = articleList.FindControl<ListBox>("ArticlesListBox"); if (lb != null) lb.Focus(); } catch { }
                        }
                    }
                    catch { }
                });
            }
            catch { }

        }
        catch { WriteDiagFile("[TRACE] Exception raising window"); }
    }

    // Parsing helpers
    private Common.Shared.ArticleLine? ParseArticleLine(string line)
    {
        var parts = SplitRespectingEscapedCommas(line);
        if (parts.Count < 2)
            return null;
        var article = new Common.Shared.ArticleLine();
        article.Pages = ParsePageNumbers(parts[0], out bool hasError);
        article.HasPageNumberError = hasError;
        if (article.Pages.Count == 0)
            return null;

        // Populate segments from pages so UI shows per-part segments
        try
        {
            var pages = article.Pages;
            try { article.Segments.Clear(); } catch { }
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
                    try { article.Segments.Add(new Common.Shared.Segment(start, end)); } catch { }
                }
            }
        }
        catch { }

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
        if (parts.Count > 6)
            article.Measurements = parts[6].Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        if (article.ModelNames == null || article.ModelNames.Count == 0)
            article.ModelNames = new List<string> { string.Empty };
        if (article.Photographers == null || article.Photographers.Count == 0)
            article.Photographers = new List<string> { string.Empty };
        if (article.Measurements == null || article.Measurements.Count == 0)
            article.Measurements = new List<string> { string.Empty };
        if (article.Ages == null || article.Ages.Count == 0)
            article.Ages = new List<int?> { null };

        return article;
    }

    private List<string> SplitRespectingEscapedCommas(string line)
    {
        var parts = new List<string>();
        var currentPart = new System.Text.StringBuilder();
        bool escaped = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (escaped)
            {
                currentPart.Append(c);
                escaped = false;
            }
            else if (c == '\\' && i + 1 < line.Length)
            {
                escaped = true;
            }
            else if (c == ',')
            {
                parts.Add(currentPart.ToString().Trim());
                currentPart.Clear();
            }
            else
            {
                currentPart.Append(c);
            }
        }
        parts.Add(currentPart.ToString().Trim());
        return parts;
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
    private void OnMainWindowKeyDown(object sender, KeyEventArgs e)
    {
        try { Console.WriteLine($"[KEY] KeyDown received: Key={e.Key} Mods={e.KeyModifiers}"); } catch { }
        // Handle global key down events here
        // For example, toggle fullscreen on F11
        // Ctrl+A: add segment at current page (if possible)
        try
        {
            if (e.Key == Key.A && e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                try
                {
                    var pc = this.FindControl<IndexEditor.Views.PageControllerView>("PageControllerControl");
                    if (pc != null)
                    {
                        var ok = pc.AddSegmentAtCurrentPage();
                        if (ok) { try { Console.WriteLine("[DEBUG] Ctrl+A: added segment"); } catch { } }
                    }
                }
                catch { }
                e.Handled = true;
                return;
            }
            // Ctrl+N: create new article
            if (e.Key == Key.N && e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                try
                {
                    try { IndexEditor.Shared.ToastService.Show("Ctrl+N pressed: creating new article"); } catch { }
                    Console.WriteLine("[DEBUG] KeyDown: Ctrl+N received");
                    var pc = this.FindControl<IndexEditor.Views.PageControllerView>("PageControllerControl");
                    if (pc != null)
                    {
                        pc.CreateNewArticle();
                        try { Console.WriteLine("[DEBUG] Ctrl+N: CreateNewArticle invoked"); } catch { }
                    }
                }
                catch { }
                e.Handled = true;
                return;
            }
        }
        catch { }

        if (e.Key == Key.F11)
        {
            this.WindowState = this.WindowState == WindowState.FullScreen ? WindowState.Normal : WindowState.FullScreen;
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            // Exit fullscreen on Escape
            if (this.WindowState == WindowState.FullScreen)
            {
                this.WindowState = WindowState.Normal;
                e.Handled = true;
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
                    var pc = this.FindControl<IndexEditor.Views.PageControllerView>("PageControllerControl");
                    if (pc != null)
                        pc.EndActiveSegment();
                    else
                    {
                        // Fallback: set seg.End to current page and clear active segment
                        seg.End = IndexEditor.Shared.EditorState.CurrentPage;
                        seg.WasNew = false;
                        IndexEditor.Shared.EditorState.ActiveSegment = null;
                        IndexEditor.Shared.EditorState.NotifyStateChanged();
                    }
                    try { Console.WriteLine("[DEBUG] Enter pressed: ended active segment"); } catch { }
                }
                catch { }
                e.Handled = true;
                return;
            }
        }
        else if (!e.KeyModifiers.HasFlag(KeyModifiers.Control) && !e.KeyModifiers.HasFlag(KeyModifiers.Alt))
        {
            // Navigation: Left/Right change page, Up/Down change selected article
            if (e.Key == Key.Left)
            {
                try { IndexEditor.Shared.EditorState.CurrentPage = Math.Max(1, IndexEditor.Shared.EditorState.CurrentPage - 1); IndexEditor.Shared.EditorState.NotifyStateChanged(); e.Handled = true; }
                catch { }
            }
            else if (e.Key == Key.Right)
            {
                try { IndexEditor.Shared.EditorState.CurrentPage = IndexEditor.Shared.EditorState.CurrentPage + 1; IndexEditor.Shared.EditorState.NotifyStateChanged(); e.Handled = true; }
                catch { }
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
                                try { lb.Focus(); } catch { }
                                // Execute selection command on VM
                                try
                                {
                                    var item = lb.SelectedItem as Common.Shared.ArticleLine;
                                    var vm = this.DataContext as IndexEditor.Views.EditorStateViewModel;
                                    if (vm != null && item != null && vm.SelectArticleCommand.CanExecute(item))
                                        vm.SelectArticleCommand.Execute(item);
                                }
                                catch { }
                            }
                            e.Handled = true;
                        }
                    }
                }
                catch { }
            }
        }
    }

    private void OnOpenButtonClick(object sender, RoutedEventArgs e)
    {
        // Open folder using OpenFolderDialog and update EditorState on UI thread
        var dlg = new Avalonia.Controls.OpenFolderDialog();
        var wnd = this.VisualRoot as Window ?? this;
        _ = dlg.ShowAsync(wnd).ContinueWith(t =>
        {
            var path = t.Result;
            if (!string.IsNullOrWhiteSpace(path))
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    IndexEditor.Shared.EditorState.CurrentFolder = path;
                    LoadArticlesFromFolder(path);
                });
            }
        });
    }

    private void LoadArticlesFromFolder(string folder)
    {
        // TODO: Implement loading articles from the specified folder
        // This should update IndexEditor.Shared.EditorState.Articles and notify the UI
    }
}

