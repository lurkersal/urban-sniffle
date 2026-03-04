namespace IndexEditor.Services;

/// <summary>
/// Service for managing delete confirmation dialogs.
/// </summary>
public interface IDeleteConfirmationService
{
    /// <summary>
    /// Shows the delete confirmation overlay for an article.
    /// </summary>
    void ShowDeleteConfirmation(string articleTitle);
    
    /// <summary>
    /// Closes the delete confirmation overlay.
    /// </summary>
    void CloseDeleteConfirmation();
    
    /// <summary>
    /// Checks if the delete confirmation overlay is currently visible.
    /// </summary>
    bool IsDeleteConfirmationVisible();
}

