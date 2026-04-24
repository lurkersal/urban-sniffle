using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;

namespace IndexEditor.Services;

/// <summary>
/// Implementation of IDialogService for showing dialogs to the user.
/// Centralizes dialog management logic extracted from MainWindow.
/// </summary>
public class DialogService : IDialogService
{
    private readonly Window _owner;

    public DialogService(Window owner)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public async Task<bool> ShowConfirmationAsync(string message, string title = "Confirm")
    {
        return await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            return await Views.ConfirmDialog.ShowDialog(_owner, message);
        });
    }

    public async Task ShowMessageAsync(string message, string title = "Information")
    {
        // Use ConfirmDialog for now with only an OK button (user can press Y or Enter)
        await ShowConfirmationAsync(message, title);
    }

    public async Task ShowErrorAsync(string message, string title = "Error")
    {
        // Use ConfirmDialog for error messages too
        await ShowConfirmationAsync(message, title);
    }

    public async Task<bool> PromptSaveChangesAsync()
    {
        // Check if there are unsaved changes
        #pragma warning disable CS0618 // Type or member is obsolete
        var hasChanges = IndexEditor.Shared.EditorState.HasUnsavedChanges;
        #pragma warning restore CS0618
        
        if (!hasChanges)
        {
            return true; // No changes, proceed
        }

        // Show save confirmation dialog
        var result = await ShowConfirmationAsync(
            "You have unsaved changes. Do you want to save before continuing?",
            "Unsaved Changes");
        
        if (result)
        {
            // User wants to save - trigger save operation
            try
            {
                #pragma warning disable CS0618 // Type or member is obsolete
                var folder = IndexEditor.Shared.EditorState.CurrentFolder;
                #pragma warning restore CS0618
                
                if (!string.IsNullOrWhiteSpace(folder))
                {
                    #pragma warning disable CS0618 // Type or member is obsolete
                    IndexEditor.Shared.IndexSaver.SaveIndex(folder);
                    IndexEditor.Shared.ToastService.Show("Index saved");
                    IndexEditor.Shared.EditorState.HasUnsavedChanges = false;
                    #pragma warning restore CS0618
                    return true;
                }
            }
            catch (Exception ex)
            {
                IndexEditor.Shared.DebugLogger.LogException("DialogService.PromptSaveChangesAsync", ex);
                await ShowErrorAsync("Failed to save index file.", "Save Error");
                return false;
            }
        }
        
        // User clicked No/Cancel - clear unsaved flag and proceed
        #pragma warning disable CS0618 // Type or member is obsolete
        IndexEditor.Shared.EditorState.HasUnsavedChanges = false;
        #pragma warning restore CS0618
        return true;
    }
}


