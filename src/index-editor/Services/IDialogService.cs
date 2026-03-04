using System.Threading.Tasks;

namespace IndexEditor.Services;

/// <summary>
/// Service for showing dialogs and prompts to the user.
/// Extracts dialog management responsibility from MainWindow.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Shows a confirmation dialog with Yes/No buttons.
    /// </summary>
    /// <param name="message">Message to display</param>
    /// <param name="title">Dialog title</param>
    /// <returns>True if user clicked Yes, false otherwise</returns>
    Task<bool> ShowConfirmationAsync(string message, string title = "Confirm");
    
    /// <summary>
    /// Shows an information message to the user.
    /// </summary>
    /// <param name="message">Message to display</param>
    /// <param name="title">Dialog title</param>
    Task ShowMessageAsync(string message, string title = "Information");
    
    /// <summary>
    /// Shows an error message to the user.
    /// </summary>
    /// <param name="message">Error message to display</param>
    /// <param name="title">Dialog title</param>
    Task ShowErrorAsync(string message, string title = "Error");
    
    /// <summary>
    /// Prompts user to save changes before closing or opening new folder.
    /// </summary>
    /// <returns>True if should proceed (saved or discarded), false if cancelled</returns>
    Task<bool> PromptSaveChangesAsync();
}

