using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;

namespace IndexEditor;

public partial class MainWindow : Window
{
    public string? FolderToOpen { get; }

    private static void WriteDiagFile(string text)
    {
        try
        {
            var path = "/tmp/indexeditor-diagnostic.log";
            System.IO.File.AppendAllText(path, DateTime.UtcNow.ToString("o") + " " + text + "\n");
        }
        catch { }
    }

    public MainWindow() : this(null) { }

    public MainWindow(string? folderToOpen = null)
    {
        Console.WriteLine($"[TRACE] MainWindow ctor called with folderToOpen='{folderToOpen}'");
        WriteDiagFile("[TRACE] MainWindow ctor starting");
        FolderToOpen = folderToOpen;

        InitializeComponent();
        Console.WriteLine("[TRACE] MainWindow InitializeComponent completed");

        // Immediate diagnostics (may run before Opened)
        try
        {
            var bounds = this.Bounds;
            var state = this.WindowState;
            var diag = $"[DIAG] After InitializeComponent: Bounds={bounds}, WindowState={state}, IsVisible={this.IsVisible}, Topmost={this.Topmost}";
            Console.WriteLine(diag);
            WriteDiagFile(diag);

            try
            {
                var screens = this.Screens;
                if (screens != null)
                {
                    var sc = $"[DIAG] After Init: Screens.Count = {screens.ScreenCount}";
                    Console.WriteLine(sc);
                    WriteDiagFile(sc);
                }
                else
                {
                    Console.WriteLine("[DIAG] After Init: Screens not available");
                    WriteDiagFile("[DIAG] After Init: Screens not available");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DIAG] Exception reading Screens after init: {ex.Message}");
                WriteDiagFile($"[DIAG] Exception reading Screens after init: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DIAG] Exception during immediate diagnostics: {ex.Message}");
            WriteDiagFile($"[DIAG] Exception during immediate diagnostics: {ex.Message}");
        }

        this.DataContext = new IndexEditor.Views.EditorStateViewModel();
        Console.WriteLine("[TRACE] MainWindow DataContext assigned");
        WriteDiagFile("[TRACE] MainWindow DataContext assigned");

        // Configure startup and Opened handler
        try
        {
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.Opened += OnWindowOpened;
            WriteDiagFile("[TRACE] Opened handler attached");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TRACE] Failed to set WindowStartupLocation or Opened handler: {ex.Message}");
            WriteDiagFile($"[TRACE] Failed to set WindowStartupLocation or Opened handler: {ex.Message}");
        }

        // Load articles from folder if provided
        if (!string.IsNullOrWhiteSpace(FolderToOpen))
        {
            var indexPath = System.IO.Path.Combine(FolderToOpen, "_index.txt");
            if (System.IO.File.Exists(indexPath))
            {
                try
                {
                    var lines = System.IO.File.ReadAllLines(indexPath);
                    var articles = new List<Common.Shared.ArticleLine>();
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                            continue;
                        var parsed = ParseArticleLine(line);
                        if (parsed != null)
                        {
                            articles.Add(parsed);
                            Console.WriteLine($"[DEBUG] Parsed article: Category='{parsed.Category}', Title='{parsed.Title}', Pages=[{string.Join(",", parsed.Pages)}]");
                        }
                        else
                        {
                            Console.WriteLine($"[DEBUG] Skipped line: '{line}'");
                        }
                    }
                    Console.WriteLine($"[DEBUG] Total articles parsed: {articles.Count}");

                    // Order by first page
                    articles = articles.Where(a => a.Pages != null && a.Pages.Count > 0)
                                       .OrderBy(a => a.Pages.Min())
                                       .ToList();

                    IndexEditor.Shared.EditorState.Articles = articles;
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
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[DEBUG] Error while finding first page image: {ex.Message}");
                    }

                    IndexEditor.Shared.EditorState.NotifyStateChanged();

                    // Do not auto-select the first article; selection will happen when user requests it
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading _index.txt: {ex.Message}");
                    WriteDiagFile($"Error reading _index.txt: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"Index file not found: {indexPath}");
                WriteDiagFile($"Index file not found: {indexPath}");
            }
        }

        Console.WriteLine("[TRACE] MainWindow constructor finished");
        WriteDiagFile("[TRACE] MainWindow constructor finished");
    }

    private void OnWindowOpened(object? sender, EventArgs e)
    {
        Console.WriteLine("[TRACE] MainWindow Opened event fired; attempting to activate and raise window");
        WriteDiagFile("[TRACE] MainWindow Opened event fired");
        try
        {
            this.Activate();
            this.Topmost = true;
            this.Topmost = false;
            Console.WriteLine("[TRACE] MainWindow activated and Topmost toggled");
            WriteDiagFile("[TRACE] MainWindow activated and Topmost toggled");

            try
            {
                var bounds = this.Bounds;
                var state = this.WindowState;
                var diag = $"[DIAG] Window.Bounds = {bounds}, WindowState = {state}";
                Console.WriteLine(diag);
                WriteDiagFile(diag);

                try
                {
                    var screens = this.Screens;
                    if (screens != null)
                    {
                        var sc = $"[DIAG] Screens.Count = {screens.ScreenCount}";
                        Console.WriteLine(sc);
                        WriteDiagFile(sc);
                        int idx = 0;
                        foreach (var screen in screens.All)
                        {
                            var sline = $"[DIAG] Screen[{idx}] WorkingArea={screen.WorkingArea}, Bounds={screen.Bounds}, IsPrimary={screen.IsPrimary}";
                            Console.WriteLine(sline);
                            WriteDiagFile(sline);
                            idx++;
                        }
                    }
                    else
                    {
                        Console.WriteLine("[DIAG] Screens not available");
                        WriteDiagFile("[DIAG] Screens not available");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DIAG] Exception reading Screens in Opened handler: {ex.Message}");
                    WriteDiagFile($"[DIAG] Exception reading Screens in Opened handler: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DIAG] Exception dumping screen info: {ex.Message}");
                WriteDiagFile($"[DIAG] Exception dumping screen info: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TRACE] Exception raising window: {ex.Message}");
            WriteDiagFile($"[TRACE] Exception raising window: {ex.Message}");
        }
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
            else if (int.TryParse(trimmed, out int singlePage))
            {
                pages.Add(singlePage);
            }
            else
            {
                hasError = true;
            }
        }
        return pages;
    }

    private bool ImageExistsInFolder(string folder, int pageNumber)
    {
        var candidates = new List<string>
        {
            System.IO.Path.Combine(folder, pageNumber.ToString() + ".jpg"),
            System.IO.Path.Combine(folder, pageNumber.ToString() + ".png"),
            System.IO.Path.Combine(folder, pageNumber.ToString("D2") + ".jpg"),
            System.IO.Path.Combine(folder, pageNumber.ToString("D2") + ".png"),
            System.IO.Path.Combine(folder, pageNumber.ToString("D3") + ".jpg"),
            System.IO.Path.Combine(folder, pageNumber.ToString("D3") + ".png"),
            System.IO.Path.Combine(folder, "page-" + pageNumber.ToString() + ".jpg"),
            System.IO.Path.Combine(folder, "p" + pageNumber.ToString() + ".jpg")
        };
        foreach (var p in candidates)
            if (System.IO.File.Exists(p)) return true;
        return false;
    }
}
