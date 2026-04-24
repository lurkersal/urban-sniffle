namespace IndexEditor.Services;

/// <summary>
/// Service for managing the help overlay.
/// </summary>
public interface IHelpOverlayService
{
    /// <summary>
    /// Shows the help overlay.
    /// </summary>
    void ShowHelpOverlay();
    
    /// <summary>
    /// Closes the help overlay.
    /// </summary>
    void CloseHelpOverlay();
    
    /// <summary>
    /// Toggles the help overlay visibility.
    /// </summary>
    void ToggleHelpOverlay();
    
    /// <summary>
    /// Checks if the help overlay is currently visible.
    /// </summary>
    bool IsHelpOverlayVisible();
}

