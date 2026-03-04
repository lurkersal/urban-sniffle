namespace IndexEditor.Services;

/// <summary>
/// Service for managing the index file editor overlay.
/// Handles display, editing, validation, and saving of index files.
/// </summary>
public interface IIndexOverlayService
{
    /// <summary>
    /// Shows the index file editor overlay with the content of _index.json or _index.txt.
    /// Prefers JSON format if it exists.
    /// </summary>
    void ShowIndexOverlay(string? folder);
    
    /// <summary>
    /// Closes the index file editor overlay.
    /// </summary>
    void CloseIndexOverlay();
    
    /// <summary>
    /// Toggles the index file editor overlay visibility.
    /// </summary>
    void ToggleIndexOverlay(string? folder);
    
    /// <summary>
    /// Shows an error in the index overlay for a specific line.
    /// </summary>
    void ShowIndexOverlayError(string errorLine, string fullText);
    
    /// <summary>
    /// Checks if the index overlay is currently visible.
    /// </summary>
    bool IsIndexOverlayVisible();
    
    /// <summary>
    /// Expand all JSON nodes in the tree viewer.
    /// </summary>
    void ExpandAllJsonNodes();
    
    /// <summary>
    /// Collapse all JSON nodes in the tree viewer.
    /// </summary>
    void CollapseAllJsonNodes();
    
    /// <summary>
    /// Toggle between View Mode (collapsible tree) and Edit Mode (editable TextBox).
    /// </summary>
    void ToggleEditMode(string? folder);
    
    /// <summary>
    /// Saves the index file content from the overlay back to disk.
    /// Works in Edit Mode (from TextBox).
    /// </summary>
    void SaveIndexFromOverlay(string folder);
    
    /// <summary>
    /// Validates JSON content. Returns (isValid, errorMessage).
    /// </summary>
    (bool isValid, string? errorMessage) ValidateJsonContent(string content);
}

