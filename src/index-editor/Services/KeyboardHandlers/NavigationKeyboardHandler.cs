using System;
using Avalonia.Input;
using IndexEditor.Shared;

namespace IndexEditor.Services.KeyboardHandlers;

/// <summary>
/// Handles keyboard shortcuts related to page navigation.
/// - Left Arrow: Previous page
/// - Right Arrow: Next page
/// </summary>
public class NavigationKeyboardHandler : IKeyboardShortcutHandler
{
    public int Priority => 50; // Lower priority than article/segment operations

    public bool TryHandle(KeyEventArgs e)
    {
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
            // If the Article Editor has focus, let the editor handle the arrow key (do not change page)
            // UNLESS there's an active segment — then we want to navigate pages even from editor fields
            var hasActiveSegment = EditorState.ActiveSegment != null 
                                && EditorState.ActiveSegment.IsActive;
                                
            if (EditorState.IsArticleEditorFocused && !hasActiveSegment)
            {
                return; // Let the textbox handle it
            }

            // Simple decrement with minimum of 1
            EditorState.CurrentPage = Math.Max(1, EditorState.CurrentPage - 1);
            EditorState.NotifyStateChanged();
            
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
            // If the Article Editor has focus, let the editor handle the arrow key (do not change page)
            // UNLESS there's an active segment — then we want to navigate pages even from editor fields
            var hasActiveSegment = EditorState.ActiveSegment != null 
                                && EditorState.ActiveSegment.IsActive;
                                
            if (EditorState.IsArticleEditorFocused && !hasActiveSegment)
            {
                return; // Let the textbox handle it
            }

            // Simple increment
            EditorState.CurrentPage = EditorState.CurrentPage + 1;
            EditorState.NotifyStateChanged();
            
            e.Handled = true;
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("NavigationKeyboardHandler: Right arrow", ex);
        }
    }
}

