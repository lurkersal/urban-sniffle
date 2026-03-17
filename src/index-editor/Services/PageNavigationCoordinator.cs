using IndexEditor.Shared;
using System;
using System.Linq;

namespace IndexEditor.Services
{
    /// <summary>
    /// Service responsible for coordinating page navigation with UI controls.
    /// Handles navigation between pages with folder scanning and button state management.
    /// </summary>
    public class PageNavigationCoordinator : IPageNavigationCoordinator
    {
        private readonly IEditorState _editorState;
        private readonly IPageNavigationService _pageNavigationService;

        public PageNavigationCoordinator(
            IEditorState editorState, 
            IPageNavigationService pageNavigationService)
        {
            _editorState = editorState ?? throw new ArgumentNullException(nameof(editorState));
            _pageNavigationService = pageNavigationService ?? throw new ArgumentNullException(nameof(pageNavigationService));
        }

        /// <summary>
        /// Moves to the previous available page
        /// </summary>
        /// <returns>The new page number, or null if unable to move</returns>
        public int? MoveToPreviousPage()
        {
            try
            {
                var folder = _editorState.CurrentFolder;
                if (!string.IsNullOrWhiteSpace(folder) && _editorState.ShowImages)
                {
                    return NavigateToPreviousPageWithScanning(folder);
                }
                else
                {
                    // No folder or images disabled: just decrement
                    return Math.Max(1, _editorState.CurrentPage - 1);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("PageNavigationCoordinator.MoveToPreviousPage", ex);
                return null;
            }
        }

        /// <summary>
        /// Moves to the next available page
        /// </summary>
        /// <returns>The new page number, or null if unable to move</returns>
        public int? MoveToNextPage()
        {
            try
            {
                var folder = _editorState.CurrentFolder;
                if (!string.IsNullOrWhiteSpace(folder) && _editorState.ShowImages)
                {
                    return NavigateToNextPageWithScanning(folder);
                }
                else
                {
                    // No folder or images disabled: just increment
                    return _editorState.CurrentPage + 1;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("PageNavigationCoordinator.MoveToNextPage", ex);
                return null;
            }
        }

        /// <summary>
        /// Navigates to the previous page with folder scanning
        /// </summary>
        private int? NavigateToPreviousPageWithScanning(string folder)
        {
            // Ensure pages are scanned
            _pageNavigationService.ScanAvailablePages(folder);

            var currentPage = _editorState.CurrentPage;
            var prevPage = _pageNavigationService.GetPreviousPage(currentPage);

            if (prevPage.HasValue)
            {
                return prevPage.Value;
            }

            // Try to find a page before current page if current page is not in the list
            var availablePages = _pageNavigationService.AvailablePages;
            var closestPrev = availablePages
                .Where(p => p < currentPage)
                .OrderByDescending(p => p)
                .FirstOrDefault();

            return closestPrev > 0 ? closestPrev : (int?)null;
        }

        /// <summary>
        /// Navigates to the next page with folder scanning
        /// </summary>
        private int? NavigateToNextPageWithScanning(string folder)
        {
            // Ensure pages are scanned
            _pageNavigationService.ScanAvailablePages(folder);

            var currentPage = _editorState.CurrentPage;
            var nextPage = _pageNavigationService.GetNextPage(currentPage);

            if (nextPage.HasValue)
            {
                return nextPage.Value;
            }

            // Try to find a page after current page if current page is not in the list
            var availablePages = _pageNavigationService.AvailablePages;
            var closestNext = availablePages
                .Where(p => p > currentPage)
                .OrderBy(p => p)
                .FirstOrDefault();

            return closestNext > 0 ? closestNext : (int?)null;
        }
    }
}

