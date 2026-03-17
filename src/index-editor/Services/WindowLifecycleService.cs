using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using IndexEditor.Shared;
using IndexEditor.Views;

namespace IndexEditor.Services;

/// <summary>
/// Service responsible for window lifecycle events (opened, closing, state management).
/// Extracted from MainWindow.axaml.cs to improve separation of concerns.
/// </summary>
public interface IWindowLifecycleService
{
    /// <summary>
    /// Handles window opened event - restores state and sets initial focus.
    /// </summary>
    void OnWindowOpened();

    /// <summary>
    /// Handles window closing event - prompts for unsaved changes and saves window state.
    /// </summary>
    Task<bool> OnWindowClosingAsync();
}

public class WindowLifecycleService : IWindowLifecycleService
{
    private readonly Window _window;
    private readonly IEditorState _editorState;
    private readonly IDialogService? _dialogService;
    private readonly Func<System.Collections.Generic.List<Common.Shared.MagazineLink>>? _getDiscoveredLinks;

    public WindowLifecycleService(
        Window window,
        IEditorState editorState,
        IDialogService? dialogService = null,
        Func<System.Collections.Generic.List<Common.Shared.MagazineLink>>? getDiscoveredLinks = null)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
        _editorState = editorState ?? throw new ArgumentNullException(nameof(editorState));
        _dialogService = dialogService;
        _getDiscoveredLinks = getDiscoveredLinks;
    }

    public void OnWindowOpened()
    {
        try
        {
            // Restore window size/state
            RestoreWindowState();

            // Activate window
            _window.Activate();
            _window.Topmost = true;
            _window.Topmost = false;

            // Set initial focus
            SetInitialFocus();
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("WindowLifecycleService.OnWindowOpened", ex);
        }
    }

    public async Task<bool> OnWindowClosingAsync()
    {
        try
        {
            // Check for unsaved changes and prompt user
            if (_editorState.HasUnsavedChanges)
            {
                bool shouldSave = await PromptForSaveAsync();
                
                if (shouldSave)
                {
                    await SaveIndexAsync();
                }

                // Clear the unsaved changes flag so we can quit without being prompted again
                _editorState.HasUnsavedChanges = false;
            }

            // Save window state
            SaveWindowState();

            return true; // Allow window to close
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("WindowLifecycleService.OnWindowClosingAsync", ex);
            return true; // Allow window to close even on error
        }
    }

    private void RestoreWindowState()
    {
        try
        {
            var st = WindowStateStore.GetWindowState();
            if (st != null)
            {
                if (st.IsMaximized)
                {
                    _window.WindowState = Avalonia.Controls.WindowState.Maximized;
                }
                else
                {
                    _window.Width = st.Width > 0 ? st.Width : _window.Width;
                    _window.Height = st.Height > 0 ? st.Height : _window.Height;
                }
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("WindowLifecycleService.RestoreWindowState", ex);
        }
    }

    private void SetInitialFocus()
    {
        try
        {
            // Delay focus to allow the window to finish opening
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    _window.Focus();

                    // Also focus the invisible host so it receives keyboard input for shortcuts
                    try
                    {
                        var host = _window.FindControl<Border>("KeyboardFocusHost");
                        if (host != null) host.Focus();
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogException("WindowLifecycleService.SetInitialFocus: focus host", ex);
                    }

                    // Try to focus the articles listbox
                    var articleList = _window.FindControl<ArticleList>("ArticleListControl");
                    if (articleList != null)
                    {
                        try
                        {
                            var lb = articleList.FindControl<ListBox>("ArticlesListBox");
                            if (lb != null) lb.Focus();
                        }
                        catch (Exception ex)
                        {
                            DebugLogger.LogException("WindowLifecycleService.SetInitialFocus: focus ArticlesListBox", ex);
                        }
                    }
                }
                catch (Exception ex)
                {
                    DebugLogger.LogException("WindowLifecycleService.SetInitialFocus: UI post", ex);
                }
            });
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("WindowLifecycleService.SetInitialFocus", ex);
        }
    }

    private async Task<bool> PromptForSaveAsync()
    {
        try
        {
            bool result;
            if (_dialogService != null)
            {
                result = await _dialogService.ShowConfirmationAsync(
                    "You have unsaved changes. Do you want to save before quitting?",
                    "Unsaved Changes");
            }
            else
            {
                // Fallback to direct dialog (backward compatibility)
                result = await ConfirmDialog.ShowDialog(
                    _window,
                    "You have unsaved changes. Do you want to save before quitting?");
            }
            return result;
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("WindowLifecycleService.PromptForSaveAsync", ex);
            return false;
        }
    }

    private async Task SaveIndexAsync()
    {
        try
        {
            var folder = _editorState.CurrentFolder;
            if (!string.IsNullOrWhiteSpace(folder))
            {
                var links = _getDiscoveredLinks?.Invoke() ?? new System.Collections.Generic.List<Common.Shared.MagazineLink>();
                await Task.Run(() => IndexSaver.SaveIndex(folder, links));
                ToastService.Show("Index saved");
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("WindowLifecycleService.SaveIndexAsync", ex);
            ToastService.Show("Failed to save index");
        }
    }

    private void SaveWindowState()
    {
        try
        {
            var isMax = _window.WindowState == Avalonia.Controls.WindowState.Maximized;
            var width = _window.Width;
            var height = _window.Height;

            if (double.IsNaN(width) || double.IsInfinity(width) || width <= 0)
                width = 1024;
            if (double.IsNaN(height) || double.IsInfinity(height) || height <= 0)
                height = 768;

            WindowStateStore.SetWindowState(width, height, isMax);
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("WindowLifecycleService.SaveWindowState", ex);
        }
    }
}

