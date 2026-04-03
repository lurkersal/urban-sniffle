using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using IndexEditor.Shared;
using IndexEditor.Views;

namespace IndexEditor.Services;

/// <summary>
/// Service responsible for article navigation (Ctrl+Up/Down to move between articles).
/// Extracted from MainWindow.axaml.cs to improve separation of concerns.
/// </summary>
public interface IArticleNavigationService
{
    /// <summary>
    /// Navigate to the previous article in the list.
    /// </summary>
    bool NavigateToPreviousArticle();

    /// <summary>
    /// Navigate to the next article in the list.
    /// </summary>
    bool NavigateToNextArticle();
}

public class ArticleNavigationService : IArticleNavigationService
{
    private readonly Window _window;
    private readonly IEditorState _editorState;

    public ArticleNavigationService(Window window, IEditorState editorState)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
        _editorState = editorState ?? throw new ArgumentNullException(nameof(editorState));
    }

    public bool NavigateToPreviousArticle()
    {
        try
        {
            DebugLogger.Log("ArticleNavigationService: Navigate to previous article");
            
            if (_editorState.IsArticleEditorFocused)
            {
                DebugLogger.Log("ArticleEditor focused - ignoring navigation");
                return false;
            }

            var vm = _window.DataContext as EditorStateViewModel;
            List<Common.Shared.ArticleLine>? list = GetArticleList(vm);
            
            if (list == null || list.Count == 0)
            {
                DebugLogger.Log("No articles to navigate");
                return true;
            }

            int curIndex = GetCurrentArticleIndex(vm, list);
            int target = Math.Max(0, curIndex - 1);
            
            if (target == curIndex)
            {
                DebugLogger.Log("Already at first article");
                return true;
            }

            var targetArticle = list[target];
            NavigateToArticle(vm, targetArticle);
            
            DebugLogger.Log($"Navigated to article index {target}");
            return true;
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("ArticleNavigationService: Navigate to previous", ex);
            return false;
        }
    }

    public bool NavigateToNextArticle()
    {
        try
        {
            DebugLogger.Log("ArticleNavigationService: Navigate to next article");
            
            if (_editorState.IsArticleEditorFocused)
            {
                DebugLogger.Log("ArticleEditor focused - ignoring navigation");
                return false;
            }

            var vm = _window.DataContext as EditorStateViewModel;
            List<Common.Shared.ArticleLine>? list = GetArticleList(vm);
            
            if (list == null || list.Count == 0)
            {
                DebugLogger.Log("No articles to navigate");
                return true;
            }

            int curIndex = GetCurrentArticleIndex(vm, list);
            int target = Math.Min(list.Count - 1, curIndex + 1);
            
            if (target == curIndex)
            {
                DebugLogger.Log("Already at last article");
                return true;
            }

            var targetArticle = list[target];
            NavigateToArticle(vm, targetArticle);
            
            DebugLogger.Log($"Navigated to article index {target}");
            return true;
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("ArticleNavigationService: Navigate to next", ex);
            return false;
        }
    }

    private List<Common.Shared.ArticleLine>? GetArticleList(EditorStateViewModel? vm)
    {
        if (vm != null)
        {
            return vm.Articles.ToList();
        }
        else if (_editorState.Articles != null)
        {
            return new List<Common.Shared.ArticleLine>(_editorState.Articles);
        }
        return null;
    }

    private int GetCurrentArticleIndex(EditorStateViewModel? vm, List<Common.Shared.ArticleLine> list)
    {
        int curIndex = -1;
        
        // Try to find current article by VM selection
        if (vm != null && vm.SelectedArticle != null)
        {
            curIndex = list.IndexOf(vm.SelectedArticle);
        }
        
        // Fallback to EditorState active article
        if (curIndex == -1 && _editorState.ActiveArticle != null)
        {
            curIndex = list.IndexOf(_editorState.ActiveArticle);
        }
        
        // Fallback to current page
        if (curIndex == -1)
        {
            curIndex = list.FindIndex(a => a.Pages != null && a.Pages.Contains(_editorState.CurrentPage));
        }
        
        // Default to first article
        if (curIndex == -1)
        {
            curIndex = 0;
        }
        
        return curIndex;
    }

    private void NavigateToArticle(EditorStateViewModel? vm, Common.Shared.ArticleLine targetArticle)
    {
        if (vm != null)
        {
            try
            {
                vm.SelectedArticle = targetArticle;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ArticleNavigationService: Set SelectedArticle", ex);
            }
            
            try
            {
                vm.NavigateToArticle(targetArticle);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ArticleNavigationService: VM.NavigateToArticle", ex);
            }
        }
        else
        {
            // Fallback to EditorState
            try
            {
                _editorState.ActiveArticle = targetArticle;
                
                // Navigate to first page of the article
                int? pick = ImageHelper.FindFirstImageInFolder(
                    _editorState.CurrentFolder ?? string.Empty,
                    targetArticle.Pages != null && targetArticle.Pages.Count > 0 ? targetArticle.Pages.Min() : 1,
                    2000);
                    
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
            catch (Exception ex)
            {
                DebugLogger.LogException("ArticleNavigationService: Fallback navigation", ex);
            }
        }
    }
}

