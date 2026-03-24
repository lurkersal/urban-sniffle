using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Threading;
using IndexEditor.Shared;
using IndexEditor.Views;

namespace IndexEditor.Services;

/// <summary>
/// Service responsible for managing the UI aspects of link discovery.
/// Handles progress updates, discovered link notifications, and UI element updates.
/// Extracted from MainWindow.axaml.cs to improve separation of concerns.
/// </summary>
public interface ILinkDiscoveryUIService
{
    /// <summary>
    /// Gets the discovered links dictionary (page number -> list of links).
    /// </summary>
    Dictionary<int, List<Common.Shared.MagazineLink>> DiscoveredLinks { get; }

    /// <summary>
    /// Clears all discovered links.
    /// </summary>
    void ClearDiscoveredLinks();

    /// <summary>
    /// Loads links from the index file into the discovered links dictionary.
    /// </summary>
    void LoadLinksFromIndex(List<Common.Shared.MagazineLink>? loadedLinks);

    /// <summary>
    /// Handles link discovery progress update.
    /// </summary>
    void OnLinkDiscoveryProgress(object? sender, LinkDiscoveryProgressEventArgs e);

    /// <summary>
    /// Handles a newly discovered link.
    /// </summary>
    void OnLinkDiscovered(object? sender, LinkDiscoveredEventArgs e);

    /// <summary>
    /// Handles completion of link discovery.
    /// </summary>
    void OnLinkDiscoveryCompleted(object? sender, EventArgs e);

    /// <summary>
    /// Updates the UI to show that links were loaded from the index file.
    /// </summary>
    void ShowLinksLoadedFromIndex();

    /// <summary>
    /// Updates the status text to display current link count.
    /// </summary>
    void UpdateStatusText();
}

public class LinkDiscoveryUIService : ILinkDiscoveryUIService
{
    private readonly Window _window;
    private readonly IEditorState _editorState;
    private readonly Dictionary<int, List<Common.Shared.MagazineLink>> _discoveredLinks = new();

    public LinkDiscoveryUIService(Window window, IEditorState editorState)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
        _editorState = editorState ?? throw new ArgumentNullException(nameof(editorState));
    }

    public Dictionary<int, List<Common.Shared.MagazineLink>> DiscoveredLinks => _discoveredLinks;

    public void ClearDiscoveredLinks()
    {
        _discoveredLinks.Clear();
    }

    public void LoadLinksFromIndex(List<Common.Shared.MagazineLink>? loadedLinks)
    {
        if (loadedLinks == null || loadedLinks.Count == 0)
        {
            return;
        }

        foreach (var link in loadedLinks)
        {
            if (!_discoveredLinks.ContainsKey(link.Page))
            {
                _discoveredLinks[link.Page] = new List<Common.Shared.MagazineLink>();
            }

            // Check if this exact link already exists for this page (deduplicate)
            bool isDuplicate = _discoveredLinks[link.Page].Any(l =>
                l.Magazine == link.Magazine &&
                l.Volume == link.Volume &&
                l.Issue == link.Issue);

            if (!isDuplicate)
            {
                _discoveredLinks[link.Page].Add(link);
            }
        }

        var totalLinks = _discoveredLinks.Values.Sum(list => list.Count);
        DebugLogger.Log($"Loaded {totalLinks} unique links from JSON (deduplicated from {loadedLinks.Count})");
    }

    public void OnLinkDiscoveryProgress(object? sender, LinkDiscoveryProgressEventArgs e)
    {
        try
        {
            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    var progress = _window.FindControl<ProgressBar>("LinkDiscoveryProgress");
                    var status = _window.FindControl<TextBlock>("LinkDiscoveryStatus");

                    if (progress != null && status != null)
                    {
                        progress.IsVisible = true;
                        status.IsVisible = true;
                        progress.Value = e.PercentComplete;
                        status.Text = $"Scanning for links: {e.ProcessedPages}/{e.TotalPages} pages";
                    }
                }
                catch (Exception ex) { DebugLogger.LogException("OnLinkDiscoveryProgress UI update", ex); }
            });
        }
        catch (Exception ex) { DebugLogger.LogException("OnLinkDiscoveryProgress", ex); }
    }

    public void OnLinkDiscovered(object? sender, LinkDiscoveredEventArgs e)
    {
        try
        {
            // Add to discovered links dictionary
            if (!_discoveredLinks.ContainsKey(e.Page))
            {
                _discoveredLinks[e.Page] = new List<Common.Shared.MagazineLink>();
            }

            // Check if this exact link already exists for this page (deduplicate)
            bool isDuplicate = _discoveredLinks[e.Page].Any(l =>
                l.Magazine == e.Magazine &&
                l.Volume == e.Volume &&
                l.Issue == e.Issue);

            if (!isDuplicate)
            {
                _discoveredLinks[e.Page].Add(new Common.Shared.MagazineLink
                {
                    Page = e.Page,
                    Magazine = e.Magazine,
                    Volume = e.Volume,
                    Issue = e.Issue
                });

                DebugLogger.Log($"Link discovered on page {e.Page}: {e.Magazine} Vol.{e.Volume} No.{e.Issue}");

                // Update PageControllerView with current links
                UpdatePageControllerLinks();
            }
            else
            {
                DebugLogger.Log($"Duplicate link skipped on page {e.Page}: {e.Magazine} Vol.{e.Volume} No.{e.Issue}");
            }
        }
        catch (Exception ex) { DebugLogger.LogException("OnLinkDiscovered", ex); }
    }

    public void OnLinkDiscoveryCompleted(object? sender, EventArgs e)
    {
        try
        {
            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    var progress = _window.FindControl<ProgressBar>("LinkDiscoveryProgress");
                    var status = _window.FindControl<TextBlock>("LinkDiscoveryStatus");
                    var linkCountText = _window.FindControl<TextBlock>("LinkCountText");

                    if (progress != null && status != null)
                    {
                        progress.IsVisible = false;
                        status.IsVisible = false;
                    }

                    if (linkCountText != null)
                    {
                        var linkCount = _discoveredLinks.Values.Sum(list => list.Count);
                        linkCountText.Text = linkCount > 0 ? $"{linkCount} link{(linkCount != 1 ? "s" : "")} found" : "";

                        // Mark index as modified if links were discovered
                        if (linkCount > 0)
                        {
                            _editorState.HasUnsavedChanges = true;
                            DebugLogger.Log($"Index file marked as modified due to {linkCount} discovered link(s)");
                        }
                    }

                    DebugLogger.Log($"Link discovery completed. Found {_discoveredLinks.Count} pages with links.");
                }
                catch (Exception ex) { DebugLogger.LogException("OnLinkDiscoveryCompleted UI update", ex); }
            });
        }
        catch (Exception ex) { DebugLogger.LogException("OnLinkDiscoveryCompleted", ex); }
    }

    public void ShowLinksLoadedFromIndex()
    {
        try
        {
            if (_discoveredLinks.Count > 0)
            {
                // Links already exist, show button to optionally re-scan
                var startBtn = _window.FindControl<Button>("StartLinkDiscoveryBtn");
                var linkCountText = _window.FindControl<TextBlock>("LinkCountText");

                if (startBtn != null)
                {
                    startBtn.IsVisible = true;
                    startBtn.Content = "Re-scan for Links";
                }

                if (linkCountText != null)
                {
                    var linkCount = _discoveredLinks.Values.Sum(l => l.Count);
                    linkCountText.Text = $"{linkCount} link{(linkCount != 1 ? "s" : "")} found";
                }
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("ShowLinksLoadedFromIndex", ex);
        }
    }

    private void UpdatePageControllerLinks()
    {
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                var pcView = _window.FindControl<PageControllerView>("PageControllerControl");
                pcView?.UpdateDiscoveredLinks(_discoveredLinks);
            }
            catch (Exception ex) { DebugLogger.LogException("UpdatePageControllerLinks", ex); }
        });
    }

    public void UpdateStatusText()
    {
        try
        {
            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    var linkCountText = _window.FindControl<TextBlock>("LinkCountText");
                    if (linkCountText != null)
                    {
                        var linkCount = _discoveredLinks.Values.Sum(list => list.Count);
                        linkCountText.Text = linkCount > 0 ? $"{linkCount} link{(linkCount != 1 ? "s" : "")} found" : "";
                    }
                }
                catch (Exception ex) { DebugLogger.LogException("UpdateStatusText", ex); }
            });
        }
        catch (Exception ex) { DebugLogger.LogException("UpdateStatusText outer", ex); }
    }
}

