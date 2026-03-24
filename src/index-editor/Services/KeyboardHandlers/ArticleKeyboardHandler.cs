using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using IndexEditor.Shared;
using IndexEditor.Views;

namespace IndexEditor.Services.KeyboardHandlers;

/// <summary>
/// Handles keyboard shortcuts related to article operations.
/// - Ctrl+N: Create new article
/// - Ctrl+D: Delete selected article
/// - Up/Down: Navigate article list (previous/next)
/// - Ctrl+Up: Jump to first article (top of list)
/// - Ctrl+Down: Jump to last article (bottom of list)
/// 
/// Now uses IEditorState via dependency injection instead of static EditorState.
/// </summary>
public class ArticleKeyboardHandler : IKeyboardShortcutHandler
{
    private readonly Window _window;
    private readonly IEditorState _editorState;

    public ArticleKeyboardHandler(Window window, IEditorState editorState)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
        _editorState = editorState ?? throw new ArgumentNullException(nameof(editorState));
    }

    public int Priority => 90; // Slightly lower than segment operations

    public bool TryHandle(KeyEventArgs e)
    {
        // When the article editor has focus, don't handle any shortcuts
        // to allow normal text editing (except those explicitly allowed in FileKeyboardHandler)
        if (_editorState.IsArticleEditorFocused)
        {
            return false;
        }

        // Ctrl+N: Create new article
        if (e.Key == Key.N && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            HandleCtrlN(e);
            return true;
        }

        // Ctrl+D: Delete selected article
        if (e.Key == Key.D && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            HandleCtrlD(e);
            return true;
        }

        // Ctrl+Up: Navigate to previous article
        if (e.Key == Key.Up && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            HandleCtrlUp(e);
            return true;
        }

        // Ctrl+Down: Navigate to next article
        if (e.Key == Key.Down && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            HandleCtrlDown(e);
            return true;
        }

        // Ctrl+B: Check Babepedia for the selected article's model
        if (e.Key == Key.B && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            HandleCtrlB(e);
            return true;
        }

        // Up/Down arrows: Navigate article list (when article list has focus)
        if (e.Key == Key.Up || e.Key == Key.Down)
        {
            return HandleUpDownArrows(e);
        }

        return false;
    }

    private void HandleCtrlN(KeyEventArgs e)
    {
        try
        {
            try { ToastService.Show("Ctrl+N pressed: creating new article"); } 
            catch (Exception ex) { DebugLogger.LogException("ArticleKeyboardHandler: ToastService.Show Ctrl+N", ex); }
            
            var mainViewModel = _window.DataContext as Views.MainWindowViewModel;
            if (mainViewModel != null)
            {
                mainViewModel.NewArticle();
            }
            else
            {
                // Fallback to PageControllerView
                var pc = _window.FindControl<PageControllerView>("PageControllerControl");
                pc?.CreateNewArticle();
            }
        }
        catch (Exception ex) { DebugLogger.LogException("ArticleKeyboardHandler: Ctrl+N handler", ex); }
        
        e.Handled = true;
    }

    private void HandleCtrlD(KeyEventArgs e)
    {
        try
        {
            // Block deletion if there's an active segment
            var activeSeg = _editorState.ActiveSegment;
            if (activeSeg != null && activeSeg.IsActive)
            {
                try { ToastService.Show("Finish or cancel the open segment first"); }
                catch (Exception ex) { DebugLogger.LogException("ArticleKeyboardHandler: ToastService.Show Ctrl+D blocked", ex); }
                e.Handled = true;
                return;
            }

            // Show deletion confirmation overlay
            var overlay = _window.FindControl<Border>("DeleteArticleConfirmOverlay");
            if (overlay != null)
            {
                try
                {
                    var vm = _window.DataContext as Views.EditorStateViewModel;
                    if (vm != null && vm.SelectedArticle != null)
                    {
                        var titleLbl = _window.FindControl<TextBlock>("DeleteArticleTitleLabel");
                        if (titleLbl != null)
                        {
                            titleLbl.Text = vm.SelectedArticle.DisplayTitle ?? "(no title)";
                        }
                        overlay.IsVisible = true;
                    }
                    else
                    {
                        try { ToastService.Show("No article selected to delete"); }
                        catch (Exception ex) { DebugLogger.LogException("ArticleKeyboardHandler: ToastService.Show no article", ex); }
                    }
                }
                catch (Exception ex) { DebugLogger.LogException("ArticleKeyboardHandler: Ctrl+D show overlay", ex); }
            }
        }
        catch (Exception ex) { DebugLogger.LogException("ArticleKeyboardHandler: Ctrl+D handler", ex); }
        
        e.Handled = true;
    }

    private bool HandleUpDownArrows(KeyEventArgs e)
    {
        try
        {
            // If the Article Editor has focus, let it handle the arrows
            if (_editorState.IsArticleEditorFocused)
            {
                return false;
            }

            var vm = _window.DataContext as Views.EditorStateViewModel;
            var list = vm?.Articles.ToList() ?? _editorState.Articles?.ToList();

            if (list == null || list.Count == 0)
            {
                e.Handled = true;
                return true;
            }

            // Determine current selection index
            int curIndex = -1;
            
            if (vm != null)
            {
                var curArticle = vm.SelectedArticle;
                if (curArticle != null) curIndex = list.IndexOf(curArticle);
            }
            
            if (curIndex == -1 && _editorState.ActiveArticle != null)
            {
                curIndex = list.IndexOf(_editorState.ActiveArticle);
            }
            
            // If still -1, try to find article containing current page
            if (curIndex == -1)
            {
                curIndex = list.FindIndex(a => a.Pages != null && a.Pages.Contains(_editorState.CurrentPage));
            }
            
            // Fallback to first article
            if (curIndex == -1)
            {
                curIndex = 0;
            }

            // Compute next index based on key
            int nextIndex = curIndex;
            if (e.Key == Key.Up)
            {
                nextIndex = Math.Max(0, curIndex - 1);
            }
            else if (e.Key == Key.Down)
            {
                nextIndex = Math.Min(list.Count - 1, curIndex + 1);
            }

            // If index changed, select the new article
            if (nextIndex != curIndex)
            {
                var targetArticle = list[nextIndex];
                
                // Only allow changing article if there's no active segment on a different article
                var activeSeg = _editorState.ActiveSegment;
                var activeArticle = _editorState.ActiveArticle;
                if (activeSeg != null && activeSeg.IsActive && activeArticle != null && !ReferenceEquals(targetArticle, activeArticle))
                {
                    try { ToastService.Show("Finish or cancel the open segment first"); }
                    catch (Exception ex) { DebugLogger.LogException("ArticleKeyboardHandler: ToastService.Show blocked nav", ex); }
                    e.Handled = true;
                    return true;
                }

                // Update selection in ViewModel
                if (vm != null)
                {
                    var lb = _window.FindControl<Views.ArticleList>("ArticleListControl")?.FindControl<ListBox>("ArticlesListBox");
                    if (lb != null)
                    {
                        try
                        {
                            lb.SelectedIndex = nextIndex;
                            var item = lb.SelectedItem as Common.Shared.ArticleLine;
                            if (item != null && vm.SelectArticleCommand.CanExecute(item))
                            {
                                vm.SelectArticleCommand.Execute(item);
                            }
                        }
                        catch (Exception ex) { DebugLogger.LogException("ArticleKeyboardHandler: article list selection", ex); }
                    }
                }
                else
                {
                    // Set shared active article and compute first page with image
                    try { _editorState.ActiveArticle = targetArticle; } catch { }
                    
                    try
                    {
                        int? pick = null;
                        try
                        {
                            var folder = _editorState.CurrentFolder ?? string.Empty;
                            var startPage = targetArticle.Pages != null && targetArticle.Pages.Count > 0 ? targetArticle.Pages.Min() : 1;
                            pick = ImageHelper.FindFirstImageInFolder(folder, startPage);
                        }
                        catch { }
                        
                        if (pick.HasValue)
                        {
                            _editorState.CurrentPage = pick.Value;
                        }
                        else if (targetArticle.Pages != null && targetArticle.Pages.Count > 0)
                        {
                            _editorState.CurrentPage = targetArticle.Pages.Min();
                        }
                        
                        _editorState.NotifyStateChanged();
                    }
                    catch (Exception ex) { DebugLogger.LogException("ArticleKeyboardHandler: update current page", ex); }
                }
            }
            
            e.Handled = true;
            return true;
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("ArticleKeyboardHandler: Up/Down arrow", ex);
            return false;
        }
    }

    private void HandleCtrlUp(KeyEventArgs e)
    {
        try
        {
            DebugLogger.Log("Ctrl+Up: Navigate to first article (top of list)");

            // Don't handle if article editor has focus
            if (_editorState.IsArticleEditorFocused)
            {
                DebugLogger.Log("Article editor focused - ignoring Ctrl+Up");
                return;
            }

            var vm = _window.DataContext as EditorStateViewModel;
            var articles = vm?.Articles?.ToList() ?? _editorState.Articles?.ToList();
            
            if (articles == null || articles.Count == 0)
            {
                DebugLogger.Log("No articles to navigate");
                e.Handled = true;
                return;
            }

            // Navigate to first article (index 0)
            var targetArticle = articles[0];
            NavigateToArticle(vm, targetArticle);
            
            DebugLogger.Log("Navigated to first article (top of list)");
            e.Handled = true;
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("ArticleKeyboardHandler: Ctrl+Up", ex);
        }
    }

    private void HandleCtrlDown(KeyEventArgs e)
    {
        try
        {
            DebugLogger.Log("Ctrl+Down: Navigate to last article (bottom of list)");

            // Don't handle if article editor has focus
            if (_editorState.IsArticleEditorFocused)
            {
                DebugLogger.Log("Article editor focused - ignoring Ctrl+Down");
                return;
            }

            var vm = _window.DataContext as EditorStateViewModel;
            var articles = vm?.Articles?.ToList() ?? _editorState.Articles?.ToList();
            
            if (articles == null || articles.Count == 0)
            {
                DebugLogger.Log("No articles to navigate");
                e.Handled = true;
                return;
            }

            // Navigate to last article (index = count - 1)
            var targetArticle = articles[articles.Count - 1];
            NavigateToArticle(vm, targetArticle);
            
            DebugLogger.Log("Navigated to last article (bottom of list)");
            e.Handled = true;
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("ArticleKeyboardHandler: Ctrl+Down", ex);
        }
    }

    private void NavigateToArticle(EditorStateViewModel? vm, Common.Shared.ArticleLine article)
    {
        try
        {
            if (vm != null)
            {
                // Use ViewModel
                vm.SelectedArticle = article;
                try
                {
                    vm.NavigateToArticle(article);
                }
                catch (Exception ex)
                {
                    DebugLogger.LogException("ArticleKeyboardHandler: NavigateToArticle", ex);
                }
            }
            else
            {
                // Fallback to _editorState
                _editorState.ActiveArticle = article;
                
                // Set current page to first page of article
                if (article.Pages != null && article.Pages.Count > 0)
                {
                    var firstPage = article.Pages.Min();
                    var folder = _editorState.CurrentFolder;
                    
                    if (!string.IsNullOrWhiteSpace(folder))
                    {
                        // Find first image in the article's page range
                        var pick = ImageHelper.FindFirstImageInFolder(folder, firstPage, 2000);
                        _editorState.CurrentPage = pick ?? firstPage;
                    }
                    else
                    {
                        _editorState.CurrentPage = firstPage;
                    }
                }
                
                _editorState.NotifyStateChanged();
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("ArticleKeyboardHandler: NavigateToArticle inner", ex);
        }
    }

    private void HandleCtrlB(KeyEventArgs e)
    {
        try
        {
            DebugLogger.Log("Ctrl+B: Check Babepedia for selected article");

            var vm = _window.DataContext as EditorStateViewModel;
            var article = vm?.SelectedArticle ?? _editorState.ActiveArticle;

            if (article == null)
            {
                Services.BottomBarService.ShowMessage("No article selected", false);
                e.Handled = true;
                return;
            }

            // Only check Model and Cover categories
            if (article.Category != "Model" && article.Category != "Cover")
            {
                Services.BottomBarService.ShowMessage($"Article '{article.DisplayTitle}' is not a Model or Cover article", false);
                e.Handled = true;
                return;
            }

            // Get the first model name
            var modelName = article.ModelNames?.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(modelName))
            {
                Services.BottomBarService.ShowMessage($"No model name found for '{article.DisplayTitle}'", false);
                e.Handled = true;
                return;
            }

            // Show "Checking..." message
            Services.BottomBarService.ShowMessage($"Checking Babepedia for {modelName}...", false);

            // Perform the check asynchronously
            System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    var (exists, url) = await IndexEditor.Services.BabepediaService.CheckModelPageAsync(modelName);

                    // The BottomBarService handles UI thread dispatching
                    if (exists && !string.IsNullOrWhiteSpace(url))
                    {
                        Services.BottomBarService.ShowMessageWithLink($"✓ Babepedia: {modelName}", url, true);
                        DebugLogger.Info($"Babepedia link found: {url}");
                    }
                    else
                    {
                        Services.BottomBarService.ShowMessage($"✗ Babepedia: {modelName} not found", false);
                        DebugLogger.Debug($"Babepedia: {modelName} not found");
                    }
                }
                catch (Exception ex)
                {
                    DebugLogger.LogException("ArticleKeyboardHandler: Babepedia check task", ex);
                    Services.BottomBarService.ShowMessage($"Error checking Babepedia for {modelName}", false);
                }
            });

            e.Handled = true;
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("ArticleKeyboardHandler: Ctrl+B handler", ex);
            Services.BottomBarService.ShowMessage("Error performing Babepedia check", false);
        }
    }

    private void UpdateStatusBar(string message, string? clickableUrl)
    {
        try
        {
            var statusText = _window.FindControl<TextBlock>("StatusText");
            var statusLink = _window.FindControl<TextBlock>("StatusLink");
            
            if (statusText != null)
            {
                statusText.Text = message;
            }

            if (statusLink != null)
            {
                if (!string.IsNullOrWhiteSpace(clickableUrl))
                {
                    // Show clickable link
                    statusLink.Text = clickableUrl;
                    statusLink.IsVisible = true;
                    
                    // Remove any existing handlers to avoid duplicates
                    statusLink.PointerPressed -= OnStatusLinkClicked;
                    
                    // Store the URL as a tag for the click handler
                    statusLink.Tag = clickableUrl;
                    
                    // Wire up click handler
                    statusLink.PointerPressed += OnStatusLinkClicked;
                }
                else
                {
                    // Hide link
                    statusLink.IsVisible = false;
                    statusLink.Text = string.Empty;
                    statusLink.Tag = null;
                    statusLink.PointerPressed -= OnStatusLinkClicked;
                }
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("ArticleKeyboardHandler: UpdateStatusBar", ex);
        }
    }

    private void OnStatusLinkClicked(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        try
        {
            if (sender is TextBlock tb && tb.Tag is string url && !string.IsNullOrWhiteSpace(url))
            {
                DebugLogger.Log($"Opening Babepedia URL: {url}");
                
                // Open URL in default browser
                try
                {
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    };
                    System.Diagnostics.Process.Start(psi);
                    
                    DebugLogger.Info($"Opened Babepedia URL in browser: {url}");
                }
                catch (Exception ex)
                {
                    DebugLogger.LogException("ArticleKeyboardHandler: Open URL in browser", ex);
                    ToastService.Show($"Failed to open URL: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("ArticleKeyboardHandler: OnStatusLinkClicked", ex);
        }
    }
}

