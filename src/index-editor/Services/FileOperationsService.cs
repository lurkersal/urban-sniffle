using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common.Shared;
using IndexEditor.Shared;

namespace IndexEditor.Services;

/// <summary>
/// Implementation of high-level file operations service.
/// Coordinates between IIndexFileService, EditorState, and ImageHelper.
/// </summary>
public class FileOperationsService : IFileOperationsService
{
    private readonly IIndexFileService _indexFileService;
    private readonly IEditorState? _editorState;

    public FileOperationsService(IIndexFileService indexFileService, IEditorState? editorState = null)
    {
        _indexFileService = indexFileService ?? throw new ArgumentNullException(nameof(indexFileService));
        _editorState = editorState;
    }

    /// <summary>
    /// Loads articles and metadata from a folder and updates application state.
    /// </summary>
    public Dictionary<int, List<MagazineLink>> LoadFolder(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            throw new ArgumentException("Folder path cannot be null or empty", nameof(folderPath));
        }

        DebugLogger.Log($"FileOperationsService.LoadFolder: Input folder: '{folderPath}'");

        // Normalize to absolute path to avoid issues with relative paths like './'
        try
        {
            var originalFolder = folderPath;
            folderPath = Path.GetFullPath(folderPath);
            DebugLogger.Log($"FileOperationsService.LoadFolder: Normalized '{originalFolder}' to '{folderPath}'");
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("FileOperationsService.LoadFolder: GetFullPath", ex);
        }

        // Load index data using IIndexFileService
        string fileMag, fileVol, fileNum, fileYear;
        List<ArticleLine> articles;
        List<MagazineLink>? loadedLinks;

        try
        {
            (fileMag, fileVol, fileNum, fileYear, articles, loadedLinks) = _indexFileService.LoadFromFolder(folderPath);
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("FileOperationsService.LoadFolder: IIndexFileService.LoadFromFolder failed", ex);
            ToastService.Show("Failed to load index file: " + ex.Message);
            throw;
        }

        // Process loaded links into UI-friendly dictionary
        var discoveredLinks = new Dictionary<int, List<MagazineLink>>();
        
        if (loadedLinks != null && loadedLinks.Count > 0)
        {
            foreach (var link in loadedLinks)
            {
                if (!discoveredLinks.ContainsKey(link.Page))
                {
                    discoveredLinks[link.Page] = new List<MagazineLink>();
                }

                // Check if this exact link already exists for this page (deduplicate)
                bool isDuplicate = discoveredLinks[link.Page].Any(l =>
                    l.Magazine == link.Magazine &&
                    l.Volume == link.Volume &&
                    l.Issue == link.Issue);

                if (!isDuplicate)
                {
                    discoveredLinks[link.Page].Add(link);
                }
            }

            var totalLinks = discoveredLinks.Values.Sum(list => list.Count);
            DebugLogger.Log($"FileOperationsService: Loaded {totalLinks} unique links from JSON (deduplicated from {loadedLinks.Count})");
        }

        // Update EditorState if available
        var state = _editorState ?? new EditorStateService();
        
        state.CurrentMagazine = fileMag ?? string.Empty;
        state.CurrentVolume = fileVol ?? string.Empty;
        state.CurrentNumber = fileNum ?? string.Empty;
        state.CurrentYear = fileYear ?? string.Empty;
        state.Articles = articles.Where(a => a.Pages != null && a.Pages.Count > 0).OrderBy(a => a.Pages.Min()).ToList();

        // Notify UI components (like TopBar) that state has changed
        state.NotifyStateChanged();

        // Validate segments for missing pages
        try
        {
            foreach (var article in state.Articles)
            {
                article.ValidateSegments(folderPath, (folder, page) => ImageExists(folder, page));
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("FileOperationsService.LoadFolder: validate segments", ex);
        }

        DebugLogger.Log($"FileOperationsService.LoadFolder: Setting CurrentFolder to '{folderPath}'");
        state.CurrentFolder = folderPath;

        // Choose first existing image page (prefer 1)
        try
        {
            int? firstImage = FindFirstImagePage(folderPath, 1, 2000);
            if (!firstImage.HasValue)
            {
                var allPages = state.Articles?.SelectMany(a => a.Pages ?? new List<int>()).Distinct().OrderBy(p => p).ToList();
                if (allPages != null && allPages.Count > 0)
                {
                    foreach (var p in allPages)
                    {
                        try
                        {
                            if (ImageExists(folderPath, p))
                            {
                                firstImage = p;
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            DebugLogger.LogException("FileOperationsService.LoadFolder: ImageExists check", ex);
                        }
                    }
                }
            }
            state.CurrentPage = firstImage ?? 1;
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("FileOperationsService.LoadFolder: choose first image", ex);
            state.CurrentPage = 1;
        }

        state.NotifyStateChanged();

        // Clear unsaved changes flag since we just loaded from disk
        state.HasUnsavedChanges = false;

        // Persist the folder as the most-recently opened so future runs can default to it
        try
        {
            RecentFolderStore.SetLastOpenedFolder(folderPath);
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("FileOperationsService.LoadFolder: persist recent folder", ex);
        }

        return discoveredLinks;
    }

    /// <summary>
    /// Saves the current articles and metadata to a folder.
    /// </summary>
    public void SaveFolder(string folderPath, List<ArticleLine> articles, Dictionary<int, List<MagazineLink>> discoveredLinks)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            throw new ArgumentException("Folder path cannot be null or empty", nameof(folderPath));
        }

        var state = _editorState ?? new EditorStateService();

        // Flatten discovered links
        var flatLinks = discoveredLinks.Values.SelectMany(l => l).ToList();

        try
        {
            _indexFileService.SaveToFolder(
                folderPath,
                state.CurrentMagazine ?? string.Empty,
                state.CurrentVolume ?? string.Empty,
                state.CurrentNumber ?? string.Empty,
                state.CurrentYear ?? string.Empty,
                articles,
                flatLinks
            );

            DebugLogger.Log($"FileOperationsService.SaveFolder: Successfully saved index to '{folderPath}'");
            state.HasUnsavedChanges = false;
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("FileOperationsService.SaveFolder failed", ex);
            ToastService.Show("Failed to save index file: " + ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Validates if an image exists in a folder for a given page number.
    /// </summary>
    public bool ImageExists(string folderPath, int pageNumber)
    {
        try
        {
            return ImageHelper.ImageExists(folderPath, pageNumber);
        }
        catch (Exception ex)
        {
            DebugLogger.LogException($"FileOperationsService.ImageExists: folder='{folderPath}', page={pageNumber}", ex);
            return false;
        }
    }

    /// <summary>
    /// Finds the first existing image page in a folder within a range.
    /// </summary>
    public int? FindFirstImagePage(string folderPath, int startPage, int endPage)
    {
        try
        {
            return ImageHelper.FindFirstImageInFolder(folderPath, startPage, endPage);
        }
        catch (Exception ex)
        {
            DebugLogger.LogException($"FileOperationsService.FindFirstImagePage: folder='{folderPath}', range={startPage}-{endPage}", ex);
            return null;
        }
    }
}

