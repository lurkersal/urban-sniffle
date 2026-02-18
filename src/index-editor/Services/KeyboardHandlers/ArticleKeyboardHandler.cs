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
/// - Up/Down: Navigate article list
/// </summary>
public class ArticleKeyboardHandler : IKeyboardShortcutHandler
{
    private readonly Window _window;

    public ArticleKeyboardHandler(Window window)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
    }

    public int Priority => 90; // Slightly lower than segment operations

    public bool TryHandle(KeyEventArgs e)
    {
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
            var activeSeg = EditorState.ActiveSegment;
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
            if (EditorState.IsArticleEditorFocused)
            {
                return false;
            }

            var vm = _window.DataContext as Views.EditorStateViewModel;
            var list = vm?.Articles.ToList() ?? EditorState.Articles?.ToList();

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
            
            if (curIndex == -1 && EditorState.ActiveArticle != null)
            {
                curIndex = list.IndexOf(EditorState.ActiveArticle);
            }
            
            // If still -1, try to find article containing current page
            if (curIndex == -1)
            {
                curIndex = list.FindIndex(a => a.Pages != null && a.Pages.Contains(EditorState.CurrentPage));
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
                var activeSeg = EditorState.ActiveSegment;
                var activeArticle = EditorState.ActiveArticle;
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
                    try { EditorState.ActiveArticle = targetArticle; } catch { }
                    
                    try
                    {
                        int? pick = null;
                        try
                        {
                            var folder = EditorState.CurrentFolder ?? string.Empty;
                            var startPage = targetArticle.Pages != null && targetArticle.Pages.Count > 0 ? targetArticle.Pages.Min() : 1;
                            pick = ImageHelper.FindFirstImageInFolder(folder, startPage);
                        }
                        catch { }
                        
                        if (pick.HasValue)
                        {
                            EditorState.CurrentPage = pick.Value;
                        }
                        else if (targetArticle.Pages != null && targetArticle.Pages.Count > 0)
                        {
                            EditorState.CurrentPage = targetArticle.Pages.Min();
                        }
                        
                        EditorState.NotifyStateChanged();
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
}

