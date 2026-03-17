using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using IndexEditor.Shared;
using IndexEditor.Views;
using Common.Shared;

namespace IndexEditor.Services;

/// <summary>
/// Service responsible for loading magazine folders and initializing the editor state.
/// Extracted from MainWindow.axaml.cs to improve separation of concerns.
/// </summary>
public interface IFolderLoadingService
{
    /// <summary>
    /// Loads articles and metadata from the specified folder.
    /// </summary>
    /// <param name="folder">The folder path to load from</param>
    void LoadFolder(string folder);
}

public class FolderLoadingService : IFolderLoadingService
{
    private readonly Window _window;
    private readonly IEditorState _editorState;
    private readonly IFileOperationsService? _fileOperationsService;
    private readonly IIndexFileService? _indexFileService;
    private readonly LinkDiscoveryUIService? _linkDiscoveryUIService;
    private readonly LinkDiscoveryService? _linkDiscoveryService;
    private readonly Func<string, int, bool>? _imageExistsChecker;

    public FolderLoadingService(
        Window window,
        IEditorState editorState,
        IFileOperationsService? fileOperationsService = null,
        IIndexFileService? indexFileService = null,
        LinkDiscoveryUIService? linkDiscoveryUIService = null,
        LinkDiscoveryService? linkDiscoveryService = null,
        Func<string, int, bool>? imageExistsChecker = null)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
        _editorState = editorState ?? throw new ArgumentNullException(nameof(editorState));
        _fileOperationsService = fileOperationsService;
        _indexFileService = indexFileService;
        _linkDiscoveryUIService = linkDiscoveryUIService;
        _linkDiscoveryService = linkDiscoveryService;
        _imageExistsChecker = imageExistsChecker ?? ImageHelper.ImageExists;
    }

    public void LoadFolder(string folder)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(folder)) return;

            // Try using FileOperationsService first (preferred path)
            if (_fileOperationsService != null)
            {
                try
                {
                    LoadFolderUsingFileOperationsService(folder);
                    return;
                }
                catch (Exception ex)
                {
                    DebugLogger.LogException("FolderLoadingService: FileOperationsService.LoadFolder failed", ex);
                    // Fall through to legacy implementation
                }
            }

            // Legacy fallback implementation
            LoadFolderLegacy(folder);
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("FolderLoadingService.LoadFolder failed", ex);
            Console.WriteLine("[ERROR] FolderLoadingService.LoadFolder failed: " + ex);
        }
    }

    private void LoadFolderUsingFileOperationsService(string folder)
    {
        // Clear discovered links from previous folder
        _linkDiscoveryUIService?.ClearDiscoveredLinks();

        // Load folder using service
        var discoveredLinks = _fileOperationsService!.LoadFolder(folder);

        // Load discovered links into UI service
        _linkDiscoveryUIService?.LoadLinksFromIndex(discoveredLinks.Values.SelectMany(l => l).ToList());

        // Update ViewModel
        UpdateViewModel();

        // Update PageController with loaded links
        UpdatePageControllerLinks();

        // Handle link discovery
        HandleLinkDiscovery(folder);
    }

    private void LoadFolderLegacy(string folder)
    {
        DebugLogger.Log($"FolderLoadingService: Input folder: '{folder}'");

        // Normalize to absolute path to avoid issues with relative paths like './'
        try
        {
            var originalFolder = folder;
            folder = System.IO.Path.GetFullPath(folder);
            DebugLogger.Log($"FolderLoadingService: Normalized '{originalFolder}' to '{folder}'");
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("FolderLoadingService: GetFullPath", ex);
        }

        // Clear discovered links from previous folder
        _linkDiscoveryUIService?.ClearDiscoveredLinks();

        // Use IIndexFileService to load index data
        if (_indexFileService == null)
        {
            DebugLogger.Log("FolderLoadingService: ERROR - IIndexFileService is null, cannot load folder");
            ToastService.Show("Service initialization error - cannot load folder");
            return;
        }

        string fileMag, fileVol, fileNum, fileYear;
        List<ArticleLine> articles;
        List<MagazineLink>? loadedLinks;

        try
        {
            (fileMag, fileVol, fileNum, fileYear, articles, loadedLinks) = _indexFileService.LoadFromFolder(folder);
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("FolderLoadingService: IIndexFileService.LoadFromFolder failed", ex);
            ToastService.Show("Failed to load index file: " + ex.Message);
            return;
        }

        // Process loaded links into UI service
        _linkDiscoveryUIService?.LoadLinksFromIndex(loadedLinks);

        // Set EditorState metadata and articles
        _editorState.CurrentMagazine = fileMag ?? string.Empty;
        _editorState.CurrentVolume = fileVol ?? string.Empty;
        _editorState.CurrentNumber = fileNum ?? string.Empty;
        _editorState.CurrentYear = fileYear ?? string.Empty;
        _editorState.Articles = articles
            .Where(a => a.Pages != null && a.Pages.Count > 0)
            .OrderBy(a => a.Pages.Min())
            .ToList();

        // Validate segments for missing pages
        try
        {
            foreach (var article in _editorState.Articles)
            {
                article.ValidateSegments(folder, _imageExistsChecker);
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("FolderLoadingService: validate segments", ex);
        }

        // Update ViewModel
        UpdateViewModel();

        DebugLogger.Log($"FolderLoadingService: Setting CurrentFolder to '{folder}'");
        _editorState.CurrentFolder = folder;

        // Choose first existing image page (prefer 1)
        SetInitialPage(folder);

        _editorState.NotifyStateChanged();

        // Clear unsaved changes flag since we just loaded from disk
        _editorState.HasUnsavedChanges = false;

        // Persist the folder as the most-recently opened so future runs can default to it
        try
        {
            RecentFolderStore.SetLastOpenedFolder(folder);
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("FolderLoadingService: persist recent folder", ex);
        }

        // Update PageController with loaded links
        UpdatePageControllerLinks();

        // Handle link discovery
        HandleLinkDiscovery(folder);
    }

    private void UpdateViewModel()
    {
        try
        {
            var vm = _window.DataContext as EditorStateViewModel;
            if (vm != null)
            {
                vm.Articles.Clear();
                foreach (var a in _editorState.Articles)
                {
                    vm.Articles.Add(a);
                }

                // If nothing is selected yet, select the first article so the editor shows content
                if (vm.SelectedArticle == null && vm.Articles.Count > 0)
                {
                    try
                    {
                        vm.SelectedArticle = vm.Articles[0];
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogException("FolderLoadingService: set SelectedArticle", ex);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("FolderLoadingService: update VM", ex);
        }
    }

    private void UpdatePageControllerLinks()
    {
        try
        {
            var pcView = _window.FindControl<PageControllerView>("PageControllerControl");
            var links = _linkDiscoveryUIService?.DiscoveredLinks ?? new Dictionary<int, List<MagazineLink>>();
            pcView?.UpdateDiscoveredLinks(links);
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("FolderLoadingService: update PageController with loaded links", ex);
        }
    }

    private void HandleLinkDiscovery(string folder)
    {
        try
        {
            if ((_linkDiscoveryUIService?.DiscoveredLinks.Count ?? 0) > 0)
            {
                // Links already exist, show button to optionally re-scan
                _linkDiscoveryUIService?.ShowLinksLoadedFromIndex();
            }
            else
            {
                // No links exist, auto-start discovery
                var magazineName = _editorState.CurrentMagazine ?? "Unknown";
                _linkDiscoveryService?.StartDiscovery(folder, magazineName);
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("FolderLoadingService: link discovery logic", ex);
        }
    }

    private void SetInitialPage(string folder)
    {
        try
        {
            int? firstImage = ImageHelper.FindFirstImageInFolder(folder, 1, 2000);
            if (!firstImage.HasValue)
            {
                var allPages = _editorState.Articles?
                    .SelectMany(a => a.Pages ?? new List<int>())
                    .Distinct()
                    .OrderBy(p => p)
                    .ToList();

                if (allPages != null && allPages.Count > 0)
                {
                    foreach (var p in allPages)
                    {
                        try
                        {
                            if (ImageHelper.ImageExists(folder, p))
                            {
                                firstImage = p;
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            DebugLogger.LogException("FolderLoadingService: ImageExists check", ex);
                        }
                    }
                }
            }
            _editorState.CurrentPage = firstImage ?? 1;
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("FolderLoadingService: choose first image", ex);
            _editorState.CurrentPage = 1;
        }
    }
}

