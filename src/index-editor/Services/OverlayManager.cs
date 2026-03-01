using System;
using System.IO;
using System.Text.Json;
using Avalonia.Controls;
using IndexEditor.Shared;
using Common.Shared;

namespace IndexEditor.Services;

/// <summary>
/// Service for managing UI overlays (index file editor, help, delete confirmation).
/// Centralizes overlay show/hide logic and state management.
/// </summary>
public class OverlayManager
{
    private readonly IControlFinder _controlFinder;

    public OverlayManager(Window window)
        : this(new WindowControlFinder(window))
    {
    }

    public OverlayManager(IControlFinder controlFinder)
    {
        _controlFinder = controlFinder ?? throw new ArgumentNullException(nameof(controlFinder));
    }

    /// <summary>
    /// Shows the index file editor overlay with the content of _index.json or _index.txt.
    /// Phase 1: Prefers JSON format if it exists.
    /// </summary>
    /// <param name="folder">The folder containing the index file</param>
    public void ShowIndexOverlay(string? folder)
    {
        try
        {
            var overlay = _controlFinder.FindControl<Border>("IndexOverlay");
            var textBox = _controlFinder.FindControl<TextBox>("IndexOverlayTextBox");
            var errBorder = _controlFinder.FindControl<Border>("IndexOverlayErrorBorder");
            var errLine = _controlFinder.FindControl<TextBlock>("IndexOverlayErrorLine");

            if (overlay == null || textBox == null)
            {
                return;
            }

            // Clear any error visuals when opening
            if (errBorder != null) errBorder.IsVisible = false;
            if (errLine != null) errLine.Text = string.Empty;

            // Load index file content - prefer JSON
            if (string.IsNullOrWhiteSpace(folder))
            {
                textBox.Text = "No folder open.";
            }
            else
            {
                var jsonPath = Path.Combine(folder, "_index.json");
                var txtPath = Path.Combine(folder, "_index.txt");
                
                if (File.Exists(jsonPath))
                {
                    textBox.Text = File.ReadAllText(jsonPath);
                }
                else if (File.Exists(txtPath))
                {
                    textBox.Text = File.ReadAllText(txtPath);
                }
                else
                {
                    textBox.Text = $"No index file found in folder: {folder}";
                }
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
            var overlay = _controlFinder.FindControl<Border>("IndexOverlay");
            var errBorder = _controlFinder.FindControl<Border>("IndexOverlayErrorBorder");
            var errLine = _controlFinder.FindControl<TextBlock>("IndexOverlayErrorLine");

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
            var overlay = _controlFinder.FindControl<Border>("IndexOverlay");
            var textBox = _controlFinder.FindControl<TextBox>("IndexOverlayTextBox");
            var errBorder = _controlFinder.FindControl<Border>("IndexOverlayErrorBorder");
            var errLine = _controlFinder.FindControl<TextBlock>("IndexOverlayErrorLine");

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
            var overlay = _controlFinder.FindControl<Border>("IndexOverlay");
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
            var overlay = _controlFinder.FindControl<Border>("HelpOverlay");
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
            var overlay = _controlFinder.FindControl<Border>("HelpOverlay");
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
            var overlay = _controlFinder.FindControl<Border>("HelpOverlay");
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
            var overlay = _controlFinder.FindControl<Border>("DeleteArticleConfirmOverlay");
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
            var overlay = _controlFinder.FindControl<Border>("IndexOverlay");
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
            var overlay = _controlFinder.FindControl<Border>("HelpOverlay");
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
            var overlay = _controlFinder.FindControl<Border>("DeleteArticleConfirmOverlay");
            return overlay?.IsVisible ?? false;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Validates JSON content. Returns (isValid, errorMessage).
    /// Phase 2 - Task 2: JSON validation
    /// </summary>
    public (bool isValid, string? errorMessage) ValidateJsonContent(string content)
    {
        try
        {
            // Check if content looks like JSON
            var trimmed = content.Trim();
            if (!trimmed.StartsWith("{"))
            {
                // Not JSON, assume it's CSV - no validation needed
                return (true, null);
            }
            
            // Try to parse as JSON using shared JsonOptions
            var jsonData = JsonSerializer.Deserialize<IndexFileJson>(content, IndexJsonSerializer.JsonOptions);
            
            if (jsonData == null)
            {
                return (false, "JSON deserialization returned null");
            }
            
            // Validate structure
            if (jsonData.Metadata == null)
            {
                return (false, "Missing 'metadata' section");
            }
            
            if (string.IsNullOrWhiteSpace(jsonData.Metadata.Magazine))
            {
                return (false, "Missing magazine name in metadata");
            }
            
            if (jsonData.Articles == null || jsonData.Articles.Count == 0)
            {
                return (false, "Missing or empty 'articles' array");
            }
            
            // Validate each article
            for (int i = 0; i < jsonData.Articles.Count; i++)
            {
                var article = jsonData.Articles[i];
                
                if (string.IsNullOrWhiteSpace(article.Category))
                {
                    return (false, $"Article {i + 1}: Missing category");
                }
                
                if (article.Pages == null || article.Pages.Count == 0)
                {
                    return (false, $"Article {i + 1} ({article.Title ?? "untitled"}): Missing pages");
                }
            }
            
            return (true, null);
        }
        catch (JsonException ex)
        {
            return (false, $"JSON parse error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return (false, $"Validation error: {ex.Message}");
        }
    }
}

