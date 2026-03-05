using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IndexEditor.Shared;

namespace IndexEditor.Services
{
    /// <summary>
    /// Service responsible for managing page navigation, scanning available pages, and finding nearest pages.
    /// Extracted from PageControllerView to improve separation of concerns.
    /// </summary>
    public interface IPageNavigationService
    {
        /// <summary>
        /// Gets the list of available page numbers in the current folder.
        /// </summary>
        IReadOnlyList<int> AvailablePages { get; }

        /// <summary>
        /// Scans the specified folder for image files and builds a list of available page numbers.
        /// </summary>
        void ScanAvailablePages(string folder);

        /// <summary>
        /// Finds the index of the current page in the available pages list.
        /// </summary>
        int? FindPageIndex(int pageNumber);

        /// <summary>
        /// Gets the page number at the specified index in the available pages list.
        /// </summary>
        int? GetPageAtIndex(int index);

        /// <summary>
        /// Determines if a previous page exists.
        /// </summary>
        bool HasPreviousPage(int currentPage);

        /// <summary>
        /// Determines if a next page exists.
        /// </summary>
        bool HasNextPage(int currentPage);

        /// <summary>
        /// Gets the previous page number, or null if none exists.
        /// </summary>
        int? GetPreviousPage(int currentPage);

        /// <summary>
        /// Gets the next page number, or null if none exists.
        /// </summary>
        int? GetNextPage(int currentPage);

        /// <summary>
        /// Finds the nearest existing page by searching in one direction.
        /// </summary>
        int? FindNearestExistingPage(string folder, int startPage, int direction);

        /// <summary>
        /// Finds the nearest existing page by searching both directions from the start page.
        /// </summary>
        int? FindNearestExistingPageBothDirections(string folder, int startPage);
    }

    public class PageNavigationService : IPageNavigationService
    {
        private readonly List<int> _availablePages = new List<int>();
        private string? _lastScannedFolder;

        private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".tif", ".tiff", ".bmp" };

        public IReadOnlyList<int> AvailablePages => _availablePages.AsReadOnly();

        public void ScanAvailablePages(string folder)
        {
            _availablePages.Clear();

            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            {
                _lastScannedFolder = null;
                return;
            }

            try
            {
                var files = Directory.GetFiles(folder)
                    .Where(f => ImageExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                    .ToList();

                foreach (var file in files)
                {
                    var nameWithoutExt = Path.GetFileNameWithoutExtension(file);
                    // Try to parse the filename as an integer (handles "42", "003", etc.)
                    if (int.TryParse(nameWithoutExt, out var pageNum) && pageNum > 0)
                    {
                        if (!_availablePages.Contains(pageNum))
                        {
                            _availablePages.Add(pageNum);
                        }
                    }
                }

                _availablePages.Sort();
                _lastScannedFolder = folder;

                DebugLogger.Log($"PageNavigationService: Scanned {folder}, found {_availablePages.Count} pages");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("PageNavigationService.ScanAvailablePages", ex);
            }
        }

        public int? FindPageIndex(int pageNumber)
        {
            var idx = _availablePages.IndexOf(pageNumber);
            return idx >= 0 ? idx : null;
        }

        public int? GetPageAtIndex(int index)
        {
            if (index >= 0 && index < _availablePages.Count)
            {
                return _availablePages[index];
            }
            return null;
        }

        public bool HasPreviousPage(int currentPage)
        {
            var idx = FindPageIndex(currentPage);
            return idx.HasValue && idx.Value > 0;
        }

        public bool HasNextPage(int currentPage)
        {
            var idx = FindPageIndex(currentPage);
            return idx.HasValue && idx.Value < _availablePages.Count - 1;
        }

        public int? GetPreviousPage(int currentPage)
        {
            var idx = FindPageIndex(currentPage);
            if (idx.HasValue && idx.Value > 0)
            {
                return _availablePages[idx.Value - 1];
            }
            return null;
        }

        public int? GetNextPage(int currentPage)
        {
            var idx = FindPageIndex(currentPage);
            if (idx.HasValue && idx.Value < _availablePages.Count - 1)
            {
                return _availablePages[idx.Value + 1];
            }
            return null;
        }

        public int? FindNearestExistingPage(string folder, int startPage, int direction)
        {
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            {
                return null;
            }

            // If we haven't scanned this folder yet, or it's changed, rescan
            if (_lastScannedFolder != folder || _availablePages.Count == 0)
            {
                ScanAvailablePages(folder);
            }

            // Search in the specified direction
            for (int offset = 1; offset <= 50; offset++)
            {
                int candidatePage = startPage + (direction * offset);
                if (candidatePage <= 0) break;

                if (_availablePages.Contains(candidatePage))
                {
                    return candidatePage;
                }
            }

            return null;
        }

        public int? FindNearestExistingPageBothDirections(string folder, int startPage)
        {
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            {
                return null;
            }

            // If we haven't scanned this folder yet, or it's changed, rescan
            if (_lastScannedFolder != folder || _availablePages.Count == 0)
            {
                ScanAvailablePages(folder);
            }

            // Check if the start page itself exists
            if (_availablePages.Contains(startPage))
            {
                return startPage;
            }

            // Interleave search: try +1, -1, +2, -2, +3, -3, etc.
            for (int offset = 1; offset <= 50; offset++)
            {
                int candidateForward = startPage + offset;
                int candidateBackward = startPage - offset;

                if (_availablePages.Contains(candidateForward))
                {
                    return candidateForward;
                }
                if (candidateBackward > 0 && _availablePages.Contains(candidateBackward))
                {
                    return candidateBackward;
                }
            }

            return null;
        }
    }
}

