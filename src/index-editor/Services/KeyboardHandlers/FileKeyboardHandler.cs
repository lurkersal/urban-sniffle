using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using IndexEditor.Shared;
using IndexEditor.Views;

namespace IndexEditor.Services.KeyboardHandlers;

/// <summary>
/// Handles keyboard shortcuts related to file operations.
/// - Ctrl+O: Open folder
/// - Ctrl+S: Save index file
/// - Ctrl+I: Toggle index file overlay
/// - F11: Toggle fullscreen
/// </summary>
public class FileKeyboardHandler : IKeyboardShortcutHandler
{
    private readonly Window _window;
    private readonly Action<string>? _loadArticlesFromFolder;
    private readonly OverlayManager? _overlayManager;
    private readonly IEditorState _editorState;
    private readonly LinkDiscoveryService? _linkDiscoveryService;

    public FileKeyboardHandler(
        Window window, 
        IEditorState editorState, 
        Action<string>? loadArticlesFromFolder = null, 
        OverlayManager? overlayManager = null,
        LinkDiscoveryService? linkDiscoveryService = null)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
        _editorState = editorState ?? throw new ArgumentNullException(nameof(editorState));
        _loadArticlesFromFolder = loadArticlesFromFolder;
        _overlayManager = overlayManager;
        _linkDiscoveryService = linkDiscoveryService;
    }

    public int Priority => 80; // High priority for file operations

    public bool TryHandle(KeyEventArgs e)
    {
        // Ctrl+O: Open folder
        if (e.Key == Key.O && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            HandleCtrlO(e);
            return true;
        }

        // Ctrl+S: Save index file
        if (e.Key == Key.S && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            HandleCtrlS(e);
            return true;
        }

        // Ctrl+I: Toggle index file overlay
        if (e.Key == Key.I && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            HandleCtrlI(e);
            return true;
        }

        // Ctrl+B: Check babepedia for selected article
        if (e.Key == Key.B && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            HandleCtrlB(e);
            return true;
        }

        // F11: Toggle fullscreen
        if (e.Key == Key.F11)
        {
            HandleF11(e);
            return true;
        }

        return false;
    }

    private void HandleCtrlO(KeyEventArgs e)
    {
        try
        {
            var active = _editorState.ActiveSegment;
            if (active != null && active.IsActive)
            {
                ToastService.Show("End or cancel the active segment before opening a new folder");
                e.Handled = true;
                return;
            }

            // Check if link discovery scan is in progress
            if (_linkDiscoveryService?.IsScanning ?? false)
            {
                // Dispatch async dialog on UI thread
                Dispatcher.UIThread.Post(async () =>
                {
                    try
                    {
                        bool shouldWait = await ConfirmDialog.ShowDialog(
                            _window,
                            "A link discovery scan is currently in progress. Do you want to wait for it to complete before opening a new folder?");
                        
                        if (!shouldWait)
                        {
                            // User chose to cancel scan and proceed
                            _linkDiscoveryService?.StopDiscovery();
                            ToastService.Show("Link discovery scan cancelled.");
                            
                            // Now proceed with opening folder
                            await OpenFolderAsync();
                        }
                        else
                        {
                            ToastService.Show("Waiting for scan to complete. Try opening again when scan finishes.");
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogException("FileKeyboardHandler: scan check dialog", ex);
                    }
                });
                e.Handled = true;
                return;
            }

            // No scan in progress, proceed with opening
            Dispatcher.UIThread.Post(async () => await OpenFolderAsync());
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("FileKeyboardHandler: Ctrl+O handler", ex);
        }
        
        e.Handled = true;
    }

    private async System.Threading.Tasks.Task OpenFolderAsync()
    {
        try
        {
            var start = _editorState.CurrentFolder;
            string? path = null;
            try
            {
                path = await FolderPicker.PickFolderAsync(_window, start);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("FileKeyboardHandler: PickFolderAsync", ex);
            }

            if (!string.IsNullOrWhiteSpace(path))
            {
                _loadArticlesFromFolder?.Invoke(path);
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("FileKeyboardHandler: OpenFolderAsync", ex);
        }
    }

    private void HandleCtrlS(KeyEventArgs e)
    {
        try
        {
            // Check if index overlay is visible - if so, save from there
            var overlay = _window.FindControl<Border>("IndexFileOverlay");
            if (overlay != null && overlay.IsVisible)
            {
                var textBox = _window.FindControl<TextBox>("IndexFileTextBox");
                if (textBox != null)
                {
                    try
                    {
                        var folder = _editorState.CurrentFolder;
                        if (string.IsNullOrWhiteSpace(folder))
                        {
                            ToastService.Show("No folder open; cannot save index");
                            e.Handled = true;
                            return;
                        }

                        var indexPath = Path.Combine(folder, "_index.txt");
                        File.WriteAllText(indexPath, textBox.Text ?? string.Empty);
                        ToastService.Show("Index saved");
                        
                        // Close overlay and reload
                        overlay.IsVisible = false;
                        _loadArticlesFromFolder?.Invoke(folder);
                    }
                    catch (Exception ex)
                    {
                        ToastService.Show("Failed to save index");
                        DebugLogger.LogException("FileKeyboardHandler: Ctrl+S save from overlay", ex);
                    }
                }
                e.Handled = true;
                return;
            }

            // Normal save (overlay not visible)
            var active = _editorState.ActiveSegment;
            if (active != null && active.IsActive)
            {
                ToastService.Show("End or cancel the active segment before saving");
                e.Handled = true;
                return;
            }

            // Save the index file directly
            var saveFolder = _editorState.CurrentFolder;
            if (string.IsNullOrWhiteSpace(saveFolder))
            {
                ToastService.Show("No folder open; cannot save index");
                e.Handled = true;
                return;
            }

            try
            {
                var links = MainWindow.Instance?.GetDiscoveredLinks();
                IndexSaver.SaveIndex(saveFolder, links);
                ToastService.Show("Index saved");
            }
            catch (Exception saveEx)
            {
                ToastService.Show("Failed to save index");
                DebugLogger.LogException("FileKeyboardHandler: Save index", saveEx);
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("FileKeyboardHandler: Ctrl+S handler", ex);
        }
        
        e.Handled = true;
    }

    private void HandleCtrlI(KeyEventArgs e)
    {
        try
        {
            var folder = _editorState.CurrentFolder;
            if (string.IsNullOrWhiteSpace(folder))
            {
                ToastService.Show("No folder open");
                e.Handled = true;
                return;
            }

            // Use OverlayManager if available, otherwise fall back to direct control access
            if (_overlayManager != null)
            {
                _overlayManager.ToggleIndexOverlay(folder);
            }
            else
            {
                // Fallback to direct control access
                var overlay = _window.FindControl<Border>("IndexFileOverlay");
                if (overlay == null)
                {
                    e.Handled = true;
                    return;
                }

                if (!overlay.IsVisible)
                {
                    var indexPath = Path.Combine(folder, "_index.txt");
                    if (!File.Exists(indexPath))
                    {
                        ToastService.Show("_index.txt not found in current folder");
                        e.Handled = true;
                        return;
                    }

                    var textBox = _window.FindControl<TextBox>("IndexFileTextBox");
                    if (textBox != null)
                    {
                        try
                        {
                            textBox.Text = File.ReadAllText(indexPath);
                        }
                        catch (Exception ex)
                        {
                            DebugLogger.LogException("FileKeyboardHandler: Read _index.txt", ex);
                            textBox.Text = "(error reading file)";
                        }
                    }

                    overlay.IsVisible = true;
                }
                else
                {
                    overlay.IsVisible = false;
                }
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("FileKeyboardHandler: Ctrl+I handler", ex);
        }
        
        e.Handled = true;
    }

    private void HandleF11(KeyEventArgs e)
    {
        _window.WindowState = _window.WindowState == Avalonia.Controls.WindowState.FullScreen 
            ? Avalonia.Controls.WindowState.Normal 
            : Avalonia.Controls.WindowState.FullScreen;
        e.Handled = true;
    }

    private void HandleCtrlB(KeyEventArgs e)
    {
        try
        {
            // Get the EditorStateViewModel from the window's DataContext
            var vm = _window.DataContext as EditorStateViewModel;
            if (vm != null)
            {
                DebugLogger.Log("FileKeyboardHandler: Ctrl+B - Checking babepedia for selected article");
                vm.CheckBabepediaForSelectedArticle();
            }
            else
            {
                DebugLogger.Log("FileKeyboardHandler: Ctrl+B - No ViewModel found");
                ToastService.Show("Cannot check babepedia - no article selected");
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("FileKeyboardHandler: Ctrl+B handler", ex);
        }
        
        e.Handled = true;
    }
}

