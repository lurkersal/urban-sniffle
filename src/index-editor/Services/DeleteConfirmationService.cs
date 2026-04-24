using System;
using Avalonia.Controls;
using IndexEditor.Shared;

namespace IndexEditor.Services;

/// <summary>
/// Service for managing delete confirmation dialogs.
/// </summary>
public class DeleteConfirmationService : IDeleteConfirmationService
{
    private readonly IControlFinder _controlFinder;

    public DeleteConfirmationService(IControlFinder controlFinder)
    {
        _controlFinder = controlFinder ?? throw new ArgumentNullException(nameof(controlFinder));
    }

    /// <summary>
    /// Shows the delete confirmation overlay for an article.
    /// </summary>
    public void ShowDeleteConfirmation(string articleTitle)
    {
        try
        {
            var overlay = _controlFinder.FindControl<Border>("DeleteArticleConfirmOverlay");
            var titleLabel = _controlFinder.FindControl<TextBlock>("DeleteArticleTitleLabel");

            if (overlay != null)
            {
                if (titleLabel != null)
                {
                    titleLabel.Text = articleTitle;
                }
                overlay.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("DeleteConfirmationService.ShowDeleteConfirmation", ex);
        }
    }

    /// <summary>
    /// Closes the delete confirmation overlay.
    /// </summary>
    public void CloseDeleteConfirmation()
    {
        try
        {
            var overlay = _controlFinder.FindControl<Border>("DeleteArticleConfirmOverlay");
            if (overlay != null)
            {
                overlay.IsVisible = false;
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("DeleteConfirmationService.CloseDeleteConfirmation", ex);
        }
    }

    /// <summary>
    /// Checks if the delete confirmation overlay is currently visible.
    /// </summary>
    public bool IsDeleteConfirmationVisible()
    {
        try
        {
            var overlay = _controlFinder.FindControl<Border>("DeleteArticleConfirmOverlay");
            return overlay?.IsVisible ?? false;
        }
        catch
        {
            return false;
        }
    }
}

