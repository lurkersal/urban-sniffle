using System;
using System.Linq;
using Avalonia.Controls;
using IndexEditor.Shared;
using IndexEditor.Views;
using Common.Shared;

namespace IndexEditor.Services;

/// <summary>
/// Service responsible for handling article deletion logic.
/// Extracted from MainWindow.axaml.cs to improve separation of concerns.
/// </summary>
public interface IArticleDeletionService
{
    /// <summary>
    /// Shows the delete confirmation dialog for the specified article.
    /// </summary>
    void ShowDeleteConfirmation(ArticleLine? article);

    /// <summary>
    /// Deletes the currently selected article and updates the UI.
    /// </summary>
    void DeleteSelectedArticle();

    /// <summary>
    /// Checks if article deletion is currently allowed.
    /// </summary>
    bool CanDeleteArticle();
}

public class ArticleDeletionService : IArticleDeletionService
{
    private readonly Window _window;
    private readonly IEditorState _editorState;
    private readonly IEditorActions? _editorActions;

    public ArticleDeletionService(
        Window window,
        IEditorState editorState,
        IEditorActions? editorActions = null)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
        _editorState = editorState ?? throw new ArgumentNullException(nameof(editorState));
        _editorActions = editorActions;
    }

    public bool CanDeleteArticle()
    {
        // Don't delete if ArticleEditor has focus
        if (_editorState.IsArticleEditorFocused)
        {
            return false;
        }

        // Don't allow deletion while active segment exists
        var activeSeg = _editorState.ActiveSegment;
        if (activeSeg != null && activeSeg.IsActive)
        {
            return false;
        }

        // Check if there's an article selected
        var vm = _window.DataContext as EditorStateViewModel;
        return vm?.SelectedArticle != null || _editorState.ActiveArticle != null;
    }

    public void ShowDeleteConfirmation(ArticleLine? article)
    {
        try
        {
            // Don't allow deletion while active segment exists
            var activeSeg = _editorState.ActiveSegment;
            if (activeSeg != null && activeSeg.IsActive)
            {
                ToastService.Show("End or cancel the active segment before deleting an article");
                return;
            }

            var delOverlay = _window.FindControl<Border>("DeleteArticleConfirmOverlay");
            if (delOverlay != null && article != null)
            {
                // Update delete confirmation text
                var titleLbl = _window.FindControl<TextBlock>("DeleteArticleTitleLabel");
                if (titleLbl != null)
                {
                    titleLbl.Text = article.DisplayTitle ?? "(no title)";
                }

                delOverlay.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("ArticleDeletionService.ShowDeleteConfirmation", ex);
        }
    }

    public void DeleteSelectedArticle()
    {
        try
        {
            DebugLogger.Log("ArticleDeletionService.DeleteSelectedArticle called");

            var vm = _window.DataContext as EditorStateViewModel;
            ArticleLine? toDelete = null;
            if (vm != null) toDelete = vm.SelectedArticle;
            if (toDelete == null) toDelete = _editorState.ActiveArticle;
            if (toDelete == null) return;

            DebugLogger.Log($"Deleting article: {toDelete.Title}");

            // Use EditorActions service if available
            if (_editorActions != null)
            {
                try
                {
                    var newSelection = _editorActions.DeleteArticle(toDelete);
                    if (vm != null && newSelection != null)
                    {
                        vm.SelectedArticle = newSelection;
                        DebugLogger.Log($"SelectedArticle changed to: {newSelection.Title}");
                    }
                    else if (vm != null)
                    {
                        vm.SelectedArticle = null;
                    }
                }
                catch (Exception ex)
                {
                    DebugLogger.LogException("ArticleDeletionService: EditorActions.DeleteArticle failed", ex);
                }
            }
            else
            {
                // Legacy fallback path
                DeleteArticleLegacy(vm, toDelete);
            }

            ToastService.Show("Article deleted");
            DebugLogger.Log("Article deletion completed");

            // Hide the delete confirmation overlay
            HideDeleteConfirmationOverlay();
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("ArticleDeletionService.DeleteSelectedArticle", ex);
        }
    }

    private void DeleteArticleLegacy(EditorStateViewModel? vm, ArticleLine toDelete)
    {
        int oldIndex = -1;
        try
        {
            if (vm != null)
                oldIndex = vm.Articles.IndexOf(toDelete);
            else if (_editorState.Articles != null)
                oldIndex = _editorState.Articles.IndexOf(toDelete);
        }
        catch { }

        _editorState.Articles?.Remove(toDelete);
        _editorState.HasUnsavedChanges = true;

        if (vm != null)
        {
            try { vm.Articles.Remove(toDelete); } catch { }
            if (vm.Articles.Count > 0)
            {
                int newIndex = Math.Min(Math.Max(0, oldIndex), vm.Articles.Count - 1);
                vm.SelectedArticle = vm.Articles[newIndex];
                DebugLogger.Log($"SelectedArticle changed to index {newIndex}: {vm.SelectedArticle?.Title}");
            }
            else
            {
                vm.SelectedArticle = null;
            }
        }

        if (_editorState.ActiveArticle == toDelete)
            _editorState.ActiveArticle = null;

        _editorState.NotifyStateChanged();
    }

    private void HideDeleteConfirmationOverlay()
    {
        try
        {
            var delOverlay = _window.FindControl<Border>("DeleteArticleConfirmOverlay");
            if (delOverlay != null)
            {
                delOverlay.IsVisible = false;
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("ArticleDeletionService.HideDeleteConfirmationOverlay", ex);
        }
    }
}

