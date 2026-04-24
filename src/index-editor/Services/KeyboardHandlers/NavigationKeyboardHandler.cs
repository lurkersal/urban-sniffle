using System;
using Avalonia.Controls;
using Avalonia.Input;
using IndexEditor.Shared;
using IndexEditor.Views;

namespace IndexEditor.Services.KeyboardHandlers;

/// <summary>
/// Handles keyboard shortcuts related to page and article navigation.
/// - Left Arrow: Previous page
/// - Right Arrow: Next page
/// - Ctrl+Up: Previous article
/// - Ctrl+Down: Next article
/// </summary>
public class NavigationKeyboardHandler : IKeyboardShortcutHandler
{
    private readonly Window? _window;
    private readonly IEditorState _editorState;
    private readonly IArticleNavigationService? _articleNavigationService;

    public NavigationKeyboardHandler(Window? window, IEditorState editorState)
    {
        _window = window;
        _editorState = editorState ?? throw new ArgumentNullException(nameof(editorState));
        
        // Create article navigation service if we have a window
        if (_window != null)
        {
            _articleNavigationService = new ArticleNavigationService(_window, _editorState);
        }
    }

    public int Priority => 50; // Lower priority than article/segment operations

    public bool TryHandle(KeyEventArgs e)
    {
        // When the article editor has focus, don't handle any navigation keys
        // to allow normal text editing (cursor movement, dropdown navigation, etc.)
        if (_editorState.IsArticleEditorFocused)
        {
            return false;
        }

        // Ctrl+Up: Previous article
        if (e.Key == Key.Up && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            return _articleNavigationService?.NavigateToPreviousArticle() ?? false;
        }

        // Ctrl+Down: Next article
        if (e.Key == Key.Down && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            return _articleNavigationService?.NavigateToNextArticle() ?? false;
        }

        if (e.Key == Key.Left)
        {
            HandleLeftArrow(e);
            return true;
        }

        if (e.Key == Key.Right)
        {
            HandleRightArrow(e);
            return true;
        }

        return false;
    }

    private void HandleLeftArrow(KeyEventArgs e)
    {
        try
        {

            // Use PageController's MoveLeft if available (respects available pages list)
            if (_window != null)
            {
                var pageController = _window.FindControl<PageControllerView>("PageControllerControl");
                if (pageController != null)
                {
                    pageController.MoveLeft();
                    e.Handled = true;
                    return;
                }
            }

            // Fallback: Simple decrement with minimum of 1
            _editorState.CurrentPage = Math.Max(1, _editorState.CurrentPage - 1);
            _editorState.NotifyStateChanged();
            
            e.Handled = true;
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("NavigationKeyboardHandler: Left arrow", ex);
        }
    }

    private void HandleRightArrow(KeyEventArgs e)
    {
        try
        {

            // Use PageController's MoveRight if available (respects available pages list)
            if (_window != null)
            {
                var pageController = _window.FindControl<PageControllerView>("PageControllerControl");
                if (pageController != null)
                {
                    pageController.MoveRight();
                    e.Handled = true;
                    return;
                }
            }

            // Fallback: Simple increment
            _editorState.CurrentPage = _editorState.CurrentPage + 1;
            _editorState.NotifyStateChanged();
            
            e.Handled = true;
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("NavigationKeyboardHandler: Right arrow", ex);
        }
    }
}

