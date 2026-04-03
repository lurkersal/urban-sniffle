using System;
using System.IO;
using System.Text.Json;
using Avalonia.Controls;
using IndexEditor.Shared;
using Common.Shared;

namespace IndexEditor.Services;

/// <summary>
/// Service for managing the index file editor overlay.
/// Handles display, editing, validation, and saving of index files.
/// </summary>
public class IndexOverlayService : IIndexOverlayService
{
    private readonly IControlFinder _controlFinder;
    private JsonTreeViewer? _jsonTreeViewer;

    public IndexOverlayService(IControlFinder controlFinder)
    {
        _controlFinder = controlFinder ?? throw new ArgumentNullException(nameof(controlFinder));
    }

    /// <summary>
    /// Shows the index file editor overlay with the content of _index.json or _index.txt.
    /// Prefers JSON format if it exists.
    /// </summary>
    public void ShowIndexOverlay(string? folder)
    {
        try
        {
            var overlay = _controlFinder.FindControl<Border>("IndexOverlay");
            var textBlock = _controlFinder.FindControl<TextBlock>("IndexOverlayTextBlock");
            var errBorder = _controlFinder.FindControl<Border>("IndexOverlayErrorBorder");
            var errLine = _controlFinder.FindControl<TextBlock>("IndexOverlayErrorLine");

            if (overlay == null || textBlock == null)
            {
                return;
            }
            
            // Find the ScrollViewer within the overlay
            ScrollViewer? scrollViewer = FindScrollViewer(overlay);
            
            // Clear any error visuals when opening
            if (errBorder != null) errBorder.IsVisible = false;
            if (errLine != null) errLine.Text = string.Empty;

            // Load index file content - prefer JSON
            if (string.IsNullOrWhiteSpace(folder))
            {
                ShowContent(scrollViewer, textBlock, "No folder open.");
            }
            else
            {
                LoadIndexFile(folder, scrollViewer, textBlock);
            }

            // Always set overlay visible even if ScrollViewer not found (for test scenarios)
            overlay.IsVisible = true;
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("IndexOverlayService.ShowIndexOverlay", ex);
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
            DebugLogger.LogException("IndexOverlayService.CloseIndexOverlay", ex);
        }
    }

    /// <summary>
    /// Toggles the index file editor overlay visibility.
    /// </summary>
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
            DebugLogger.LogException("IndexOverlayService.ToggleIndexOverlay", ex);
        }
    }

    /// <summary>
    /// Shows an error in the index overlay for a specific line.
    /// </summary>
    public void ShowIndexOverlayError(string errorLine, string fullText)
    {
        try
        {
            var overlay = _controlFinder.FindControl<Border>("IndexOverlay");
            var textBlock = _controlFinder.FindControl<TextBlock>("IndexOverlayTextBlock");
            var errBorder = _controlFinder.FindControl<Border>("IndexOverlayErrorBorder");
            var errLine = _controlFinder.FindControl<TextBlock>("IndexOverlayErrorLine");

            if (overlay == null || textBlock == null)
            {
                return;
            }

            // Display with syntax highlighting if it looks like JSON
            if (fullText.TrimStart().StartsWith("{") || fullText.TrimStart().StartsWith("["))
            {
                JsonSyntaxHighlighter.ApplyHighlighting(textBlock, fullText);
            }
            else
            {
                textBlock.Text = fullText;
            }
            
            overlay.IsVisible = true;

            // Display the errored line
            if (errBorder != null && errLine != null)
            {
                errLine.Text = errorLine.Trim();
                errBorder.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("IndexOverlayService.ShowIndexOverlayError", ex);
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
    /// Expand all JSON nodes in the tree viewer.
    /// </summary>
    public void ExpandAllJsonNodes()
    {
        try
        {
            _jsonTreeViewer?.ExpandAll();
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("IndexOverlayService.ExpandAllJsonNodes", ex);
        }
    }

    /// <summary>
    /// Collapse all JSON nodes in the tree viewer.
    /// </summary>
    public void CollapseAllJsonNodes()
    {
        try
        {
            _jsonTreeViewer?.CollapseAll();
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("IndexOverlayService.CollapseAllJsonNodes", ex);
        }
    }

    /// <summary>
    /// Toggle between View Mode (collapsible tree) and Edit Mode (editable TextBox).
    /// </summary>
    public void ToggleEditMode(string? folder)
    {
        try
        {
            var overlay = _controlFinder.FindControl<Border>("IndexOverlay");
            if (overlay == null) return;
            
            ScrollViewer? scrollViewer = FindScrollViewer(overlay);
            if (scrollViewer == null) return;
            
            var expandBtn = _controlFinder.FindControl<Button>("IndexOverlayExpandAllBtn");
            var collapseBtn = _controlFinder.FindControl<Button>("IndexOverlayCollapseAllBtn");
            var saveBtn = _controlFinder.FindControl<Button>("IndexOverlaySaveBtn");
            var toggleBtn = _controlFinder.FindControl<Button>("IndexOverlayToggleModeBtn");
            
            // Check current mode by looking at current content
            bool isCurrentlyInViewMode = scrollViewer.Content is JsonTreeViewer;
            
            if (isCurrentlyInViewMode)
            {
                SwitchToEditMode(scrollViewer, folder, expandBtn, collapseBtn, saveBtn, toggleBtn);
            }
            else
            {
                SwitchToViewMode(scrollViewer, expandBtn, collapseBtn, saveBtn, toggleBtn);
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("IndexOverlayService.ToggleEditMode", ex);
        }
    }

    /// <summary>
    /// Saves the index file content from the overlay back to disk.
    /// Works in Edit Mode (from TextBox).
    /// </summary>
    public void SaveIndexFromOverlay(string folder)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(folder))
            {
                ToastService.Show("No folder open; cannot save");
                return;
            }

            // In Edit Mode, we have a TextBox
            var textBox = _controlFinder.FindControl<TextBox>("IndexOverlayTextBox");
            if (textBox == null)
            {
                ToastService.Show("Switch to Edit Mode to save changes");
                return;
            }

            var content = textBox.Text ?? string.Empty;
            
            // Determine if this is JSON or text based on the content
            var isJson = content.TrimStart().StartsWith("{") || content.TrimStart().StartsWith("[");
            var filePath = Path.Combine(folder, isJson ? "_index.json" : "_index.txt");
            
            // Write to temp file first, then replace
            var tempPath = filePath + ".tmp";
            File.WriteAllText(tempPath, content);
            
            if (File.Exists(filePath))
            {
                File.Replace(tempPath, filePath, null);
            }
            else
            {
                File.Move(tempPath, filePath);
            }
            
            ToastService.Show($"{Path.GetFileName(filePath)} saved");
            DebugLogger.Log($"IndexOverlayService: Saved index file to {filePath}");
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("IndexOverlayService.SaveIndexFromOverlay", ex);
            ToastService.Show("Failed to save index file");
        }
    }

    /// <summary>
    /// Validates JSON content. Returns (isValid, errorMessage).
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

    #region Private Helper Methods

    private ScrollViewer? FindScrollViewer(Border overlay)
    {
        if (overlay is Border border && border.Child is Grid grid)
        {
            foreach (var child in grid.Children)
            {
                if (child is Border innerBorder && innerBorder.Child is Grid innerGrid)
                {
                    foreach (var innerChild in innerGrid.Children)
                    {
                        if (innerChild is ScrollViewer sv)
                        {
                            return sv;
                        }
                    }
                }
            }
        }
        return null;
    }

    private void LoadIndexFile(string folder, ScrollViewer? scrollViewer, TextBlock? textBlock)
    {
        var jsonPath = Path.Combine(folder, "_index.json");
        var txtPath = Path.Combine(folder, "_index.txt");
        
        if (File.Exists(jsonPath))
        {
            var rawContent = File.ReadAllText(jsonPath);
            DebugLogger.Log($"IndexOverlayService: Loading JSON index, original length={rawContent.Length}");
            
            // Use JsonTreeViewer for collapsible display
            ShowContent(scrollViewer, textBlock, rawContent, isJson: true);
        }
        else if (File.Exists(txtPath))
        {
            // For text files, just display as plain text
            ShowContent(scrollViewer, textBlock, File.ReadAllText(txtPath), isJson: false);
        }
        else
        {
            var msg = $"No index file found in folder: {folder}";
            ShowContent(scrollViewer, textBlock, msg, isJson: false);
        }
    }

    private void ShowContent(ScrollViewer? scrollViewer, TextBlock? textBlock, string content, bool isJson = false)
    {
        if (isJson)
        {
            ShowJsonTree(scrollViewer, textBlock, content);
        }
        else
        {
            ShowPlainText(scrollViewer, textBlock, content);
        }
    }

    private void ShowJsonTree(ScrollViewer? scrollViewer, TextBlock? textBlock, string jsonText)
    {
        try
        {
            // Hide the simple TextBlock
            if (textBlock != null)
            {
                textBlock.IsVisible = false;
            }
            
            if (scrollViewer == null)
            {
                // Fallback for test scenarios: just set text on TextBlock
                if (textBlock != null)
                {
                    textBlock.Text = jsonText;
                    textBlock.IsVisible = true;
                }
                return;
            }
            
            // Start in View Mode with JsonTreeViewer for colors and collapsing
            _jsonTreeViewer = new JsonTreeViewer();
            _jsonTreeViewer.LoadJson(jsonText);
            scrollViewer.Content = _jsonTreeViewer;
            
            // Show expand/collapse buttons, hide save button (view mode)
            var expandBtn = _controlFinder.FindControl<Button>("IndexOverlayExpandAllBtn");
            var collapseBtn = _controlFinder.FindControl<Button>("IndexOverlayCollapseAllBtn");
            var saveBtn = _controlFinder.FindControl<Button>("IndexOverlaySaveBtn");
            var toggleBtn = _controlFinder.FindControl<Button>("IndexOverlayToggleModeBtn");
            
            if (expandBtn != null) expandBtn.IsVisible = true;
            if (collapseBtn != null) collapseBtn.IsVisible = true;
            if (saveBtn != null) saveBtn.IsVisible = false;
            if (toggleBtn != null) toggleBtn.Content = "Switch to Edit Mode";
            
            DebugLogger.Log($"IndexOverlayService: Loaded JSON into JsonTreeViewer (View Mode)");
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("IndexOverlayService.ShowJsonTree", ex);
            // Fallback to plain text on error
            ShowPlainText(scrollViewer, textBlock, jsonText);
        }
    }
    
    private void ShowPlainText(ScrollViewer? scrollViewer, TextBlock? textBlock, string text)
    {
        if (textBlock == null) return;
        
        textBlock.Text = text;
        textBlock.IsVisible = true;
        
        if (scrollViewer != null)
        {
            scrollViewer.Content = textBlock;
        }
        
        // Clear tree viewer reference
        _jsonTreeViewer = null;
    }

    private void SwitchToEditMode(ScrollViewer? scrollViewer, string? folder, 
        Button? expandBtn, Button? collapseBtn, Button? saveBtn, Button? toggleBtn)
    {
        if (scrollViewer == null) return;
        
        DebugLogger.Log("IndexOverlayService: Switching to Edit Mode");
        
        // Get the current JSON content from the tree viewer
        string jsonContent = string.Empty;
        if (string.IsNullOrWhiteSpace(folder))
        {
            jsonContent = "{}";
        }
        else
        {
            var jsonPath = Path.Combine(folder, "_index.json");
            if (File.Exists(jsonPath))
            {
                jsonContent = File.ReadAllText(jsonPath);
            }
        }
        
        // Create editable TextBox
        var jsonTextBox = new TextBox
        {
            Name = "IndexOverlayTextBox",
            FontFamily = new Avalonia.Media.FontFamily("'Ubuntu Mono', 'DejaVu Sans Mono', 'Courier New', monospace"),
            FontSize = 14,
            AcceptsReturn = true,
            TextWrapping = Avalonia.Media.TextWrapping.NoWrap,
            IsReadOnly = false,
            Text = PrettyPrintJson(jsonContent)
        };
        
        scrollViewer.Content = jsonTextBox;
        _jsonTreeViewer = null;
        
        // Update button visibility
        if (expandBtn != null) expandBtn.IsVisible = false;
        if (collapseBtn != null) collapseBtn.IsVisible = false;
        if (saveBtn != null) saveBtn.IsVisible = true;
        if (toggleBtn != null) toggleBtn.Content = "Switch to View Mode";
    }

    private void SwitchToViewMode(ScrollViewer? scrollViewer, 
        Button? expandBtn, Button? collapseBtn, Button? saveBtn, Button? toggleBtn)
    {
        if (scrollViewer == null) return;
        
        DebugLogger.Log("IndexOverlayService: Switching to View Mode");
        
        // Get the current content from the TextBox
        string jsonContent = string.Empty;
        if (scrollViewer.Content is TextBox textBox)
        {
            jsonContent = textBox.Text ?? string.Empty;
        }
        
        // Create JsonTreeViewer
        _jsonTreeViewer = new JsonTreeViewer();
        _jsonTreeViewer.LoadJson(jsonContent);
        scrollViewer.Content = _jsonTreeViewer;
        
        // Update button visibility
        if (expandBtn != null) expandBtn.IsVisible = true;
        if (collapseBtn != null) collapseBtn.IsVisible = true;
        if (saveBtn != null) saveBtn.IsVisible = false;
        if (toggleBtn != null) toggleBtn.Content = "Switch to Edit Mode";
    }

    private string PrettyPrintJson(string jsonText)
    {
        try
        {
            DebugLogger.Log($"PrettyPrintJson: Input length={jsonText.Length}, first 100 chars: {jsonText.Substring(0, Math.Min(100, jsonText.Length))}");
            
            // Parse and format using System.Text.Json without reflection
            using var doc = JsonDocument.Parse(jsonText);
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions
            {
                Indented = true
            }))
            {
                doc.RootElement.WriteTo(writer);
            }
            
            var result = System.Text.Encoding.UTF8.GetString(stream.ToArray());
            DebugLogger.Log($"PrettyPrintJson: Output length={result.Length}, first 100 chars: {result.Substring(0, Math.Min(100, result.Length))}");
            return result;
        }
        catch (Exception ex)
        {
            // If pretty-printing fails, return original
            DebugLogger.LogException("IndexOverlayService.PrettyPrintJson", ex);
            return jsonText;
        }
    }

    #endregion
}

