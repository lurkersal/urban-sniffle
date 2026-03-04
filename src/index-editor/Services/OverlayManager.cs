using System;
using Avalonia.Controls;
using IndexEditor.Shared;

namespace IndexEditor.Services;

/// <summary>
/// Service for managing UI overlays (index file editor, help, delete confirmation).
/// Centralizes overlay show/hide logic and state management.
/// REFACTORED: Now uses composition to delegate to specialized services.
/// </summary>
public class OverlayManager
{
    private readonly IIndexOverlayService _indexOverlayService;
    private readonly IHelpOverlayService _helpOverlayService;
    private readonly IDeleteConfirmationService _deleteConfirmationService;

    public OverlayManager(Window window)
        : this(new WindowControlFinder(window))
    {
    }

    public OverlayManager(IControlFinder controlFinder)
    {
        if (controlFinder == null) throw new ArgumentNullException(nameof(controlFinder));
        
        _indexOverlayService = new IndexOverlayService(controlFinder);
        _helpOverlayService = new HelpOverlayService(controlFinder);
        _deleteConfirmationService = new DeleteConfirmationService(controlFinder);
    }
    
    /// <summary>
    /// Constructor for testing with injected services.
    /// </summary>
    public OverlayManager(IIndexOverlayService indexOverlayService, 
        IHelpOverlayService helpOverlayService, 
        IDeleteConfirmationService deleteConfirmationService)
    {
        _indexOverlayService = indexOverlayService ?? throw new ArgumentNullException(nameof(indexOverlayService));
        _helpOverlayService = helpOverlayService ?? throw new ArgumentNullException(nameof(helpOverlayService));
        _deleteConfirmationService = deleteConfirmationService ?? throw new ArgumentNullException(nameof(deleteConfirmationService));
    }


    #region Index Overlay Methods

    /// <summary>
    /// Shows the index file editor overlay with the content of _index.json or _index.txt.
    /// Phase 1: Prefers JSON format if it exists.
    /// </summary>
    /// <param name="folder">The folder containing the index file</param>
    public void ShowIndexOverlay(string? folder) => _indexOverlayService.ShowIndexOverlay(folder);

    /// <summary>
    /// Closes the index file editor overlay.
    /// </summary>
    public void CloseIndexOverlay() => _indexOverlayService.CloseIndexOverlay();

    /// <summary>
    /// Toggles the index file editor overlay visibility.
    /// </summary>
    /// <param name="folder">The folder containing _index.txt</param>
    public void ToggleIndexOverlay(string? folder) => _indexOverlayService.ToggleIndexOverlay(folder);

    /// <summary>
    /// Shows an error in the index overlay for a specific line.
    /// </summary>
    /// <param name="errorLine">The line that caused the error</param>
    /// <param name="fullText">The full text of the index file</param>
    public void ShowIndexOverlayError(string errorLine, string fullText) 
        => _indexOverlayService.ShowIndexOverlayError(errorLine, fullText);

    /// <summary>
    /// Checks if the index overlay is currently visible.
    /// </summary>
    public bool IsIndexOverlayVisible() => _indexOverlayService.IsIndexOverlayVisible();

    /// <summary>
    /// Expand all JSON nodes in the tree viewer.
    /// </summary>
    public void ExpandAllJsonNodes() => _indexOverlayService.ExpandAllJsonNodes();

    /// <summary>
    /// Collapse all JSON nodes in the tree viewer.
    /// </summary>
    public void CollapseAllJsonNodes() => _indexOverlayService.CollapseAllJsonNodes();

    /// <summary>
    /// Toggle between View Mode (collapsible tree) and Edit Mode (editable TextBox).
    /// </summary>
    /// <param name="folder">Current folder to get the JSON content</param>
    public void ToggleEditMode(string? folder) => _indexOverlayService.ToggleEditMode(folder);

    /// <summary>
    /// Saves the index file content from the overlay back to disk.
    /// Works in Edit Mode (from TextBox).
    /// </summary>
    public void SaveIndexFromOverlay(string folder) => _indexOverlayService.SaveIndexFromOverlay(folder);

    /// <summary>
    /// Validates JSON content. Returns (isValid, errorMessage).
    /// Phase 2 - Task 2: JSON validation
    /// </summary>
    public (bool isValid, string? errorMessage) ValidateJsonContent(string content) 
        => _indexOverlayService.ValidateJsonContent(content);

    #endregion


    #region Help Overlay Methods

    /// <summary>
    /// Shows the help overlay.
    /// </summary>
    public void ShowHelpOverlay() => _helpOverlayService.ShowHelpOverlay();

    /// <summary>
    /// Closes the help overlay.
    /// </summary>
    public void CloseHelpOverlay() => _helpOverlayService.CloseHelpOverlay();

    /// <summary>
    /// Toggles the help overlay visibility.
    /// </summary>
    public void ToggleHelpOverlay() => _helpOverlayService.ToggleHelpOverlay();

    /// <summary>
    /// Checks if the help overlay is currently visible.
    /// </summary>
    public bool IsHelpOverlayVisible() => _helpOverlayService.IsHelpOverlayVisible();

    #endregion


    #region Delete Confirmation Methods

    /// <summary>
    /// Shows the delete confirmation overlay for an article.
    /// </summary>
    /// <param name="articleTitle">The title of the article to delete</param>
    public void ShowDeleteConfirmation(string articleTitle) 
        => _deleteConfirmationService.ShowDeleteConfirmation(articleTitle);

    /// <summary>
    /// Closes the delete confirmation overlay.
    /// </summary>
    public void CloseDeleteConfirmation() => _deleteConfirmationService.CloseDeleteConfirmation();

    /// <summary>
    /// Checks if the delete confirmation overlay is currently visible.
    /// </summary>
    public bool IsDeleteConfirmationVisible() => _deleteConfirmationService.IsDeleteConfirmationVisible();

    #endregion
}
