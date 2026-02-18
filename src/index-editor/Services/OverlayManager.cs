using System;
using System.IO;
using Avalonia.Controls;
using IndexEditor.Shared;

namespace IndexEditor.Services;

/// <summary>
/// Service for managing UI overlays (index file editor, help, delete confirmation).
/// Centralizes overlay show/hide logic and state management.
/// </summary>
public class OverlayManager
{
    private readonly Window _window;

    public OverlayManager(Window window)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
    }

    /// <summary>
    /// Shows the index file editor overlay with the content of _index.txt.
    /// </summary>
    /// <param name="folder">The folder containing _index.txt</param>
    public void ShowIndexOverlay(string? folder)
    {
        try
        {
            var overlay = _window.FindControl<Border>("IndexOverlay");
            var textBox = _window.FindControl<TextBox>("IndexOverlayTextBox");
            var errBorder = _window.FindControl<Border>("IndexOverlayErrorBorder");
            var errLine = _window.FindControl<TextBlock>("IndexOverlayErrorLine");

            if (overlay == null || textBox == null)
            {
                return;
            }

            // Clear any error visuals when opening
            if (errBorder != null) errBorder.IsVisible = false;
            if (errLine != null) errLine.Text = string.Empty;

            // Load _index.txt content
            if (string.IsNullOrWhiteSpace(folder))
            {
                textBox.Text = "No folder open.";
            }
            else
            {
                var indexPath = Path.Combine(folder, "_index.txt");
                textBox.Text = File.Exists(indexPath)
                    ? File.ReadAllText(indexPath)
                    : $"_index.txt not found in folder: {folder}";
            }

            overlay.IsVisible = true;
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("OverlayManager.ShowIndexOverlay", ex);
        }
    }

    /// <summary>
    /// Closes the index file editor overlay.
    /// </summary>
    public void CloseIndexOverlay()
    {
        try
        {
            var overlay = _window.FindControl<Border>("IndexOverlay");
            var errBorder = _window.FindControl<Border>("IndexOverlayErrorBorder");
            var errLine = _window.FindControl<TextBlock>("IndexOverlayErrorLine");

            // Clear error visuals when closing
            if (errBorder != null) errBorder.IsVisible = false;
            if (errLine != null) errLine.Text = string.Empty;

            if (overlay != null)
            {
                overlay.IsVisible = false;
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("OverlayManager.CloseIndexOverlay", ex);
        }
    }

    /// <summary>
    /// Shows an error in the index overlay for a specific line.
    /// </summary>
    /// <param name="errorLine">The line that caused the error</param>
    /// <param name="fullText">The full text of the index file</param>
    public void ShowIndexOverlayError(string errorLine, string fullText)
    {
        try
        {
            var overlay = _window.FindControl<Border>("IndexOverlay");
            var textBox = _window.FindControl<TextBox>("IndexOverlayTextBox");
            var errBorder = _window.FindControl<Border>("IndexOverlayErrorBorder");
            var errLine = _window.FindControl<TextBlock>("IndexOverlayErrorLine");

            if (overlay == null || textBox == null)
            {
                return;
            }

            textBox.Text = fullText;
            overlay.IsVisible = true;

            // Display the errored line
            if (errBorder != null && errLine != null)
            {
                errLine.Text = errorLine.Trim();
                errBorder.IsVisible = true;
            }

            // Select the line in the textbox
            try
            {
                var pos = fullText.IndexOf(errorLine, StringComparison.Ordinal);
                if (pos >= 0)
                {
                    textBox.SelectionStart = pos;
                    textBox.SelectionEnd = pos + errorLine.Length;
                    textBox.CaretIndex = pos;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("OverlayManager.ShowIndexOverlayError: select line", ex);
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("OverlayManager.ShowIndexOverlayError", ex);
        }
    }

    /// <summary>
    /// Toggles the index file editor overlay visibility.
    /// </summary>
    /// <param name="folder">The folder containing _index.txt</param>
    public void ToggleIndexOverlay(string? folder)
    {
        try
        {
            var overlay = _window.FindControl<Border>("IndexOverlay");
            if (overlay == null)
            {
                return;
            }

            if (overlay.IsVisible)
            {
                CloseIndexOverlay();
            }
            else
            {
                ShowIndexOverlay(folder);
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("OverlayManager.ToggleIndexOverlay", ex);
        }
    }

    /// <summary>
    /// Shows the help overlay.
    /// </summary>
    public void ShowHelpOverlay()
    {
        try
        {
            var overlay = _window.FindControl<Border>("HelpOverlay");
            if (overlay != null)
            {
                overlay.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("OverlayManager.ShowHelpOverlay", ex);
        }
    }

    /// <summary>
    /// Closes the help overlay.
    /// </summary>
    public void CloseHelpOverlay()
    {
        try
        {
            var overlay = _window.FindControl<Border>("HelpOverlay");
            if (overlay != null)
            {
                overlay.IsVisible = false;
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("OverlayManager.CloseHelpOverlay", ex);
        }
    }

    /// <summary>
    /// Toggles the help overlay visibility.
    /// </summary>
    public void ToggleHelpOverlay()
    {
        try
        {
            var overlay = _window.FindControl<Border>("HelpOverlay");
            if (overlay != null)
            {
                overlay.IsVisible = !overlay.IsVisible;
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("OverlayManager.ToggleHelpOverlay", ex);
        }
    }

    /// <summary>
    /// Shows the delete confirmation overlay for an article.
    /// </summary>
    /// <param name="articleTitle">The title of the article to delete</param>
    public void ShowDeleteConfirmation(string articleTitle)
    {
        try
        {
            var overlay = _window.FindControl<Border>("DeleteArticleConfirmOverlay");
            var titleLabel = _window.FindControl<TextBlock>("DeleteArticleTitleLabel");

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
            DebugLogger.LogException("OverlayManager.ShowDeleteConfirmation", ex);
        }
    }

    /// <summary>
    /// Closes the delete confirmation overlay.
    /// </summary>
    public void CloseDeleteConfirmation()
    {
        try
        {
            var overlay = _window.FindControl<Border>("DeleteArticleConfirmOverlay");
            if (overlay != null)
            {
                overlay.IsVisible = false;
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("OverlayManager.CloseDeleteConfirmation", ex);
        }
    }

    /// <summary>
    /// Checks if the index overlay is currently visible.
    /// </summary>
    public bool IsIndexOverlayVisible()
    {
        try
        {
            var overlay = _window.FindControl<Border>("IndexOverlay");
            return overlay?.IsVisible ?? false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if the help overlay is currently visible.
    /// </summary>
    public bool IsHelpOverlayVisible()
    {
        try
        {
            var overlay = _window.FindControl<Border>("HelpOverlay");
            return overlay?.IsVisible ?? false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if the delete confirmation overlay is currently visible.
    /// </summary>
    public bool IsDeleteConfirmationVisible()
    {
        try
        {
            var overlay = _window.FindControl<Border>("DeleteArticleConfirmOverlay");
            return overlay?.IsVisible ?? false;
        }
        catch
        {
            return false;
        }
    }
}

