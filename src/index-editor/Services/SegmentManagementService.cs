using IndexEditor.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IndexEditor.Services
{
    /// <summary>
    /// Service responsible for managing active segments in articles.
    /// Handles segment creation, completion, and page assignment.
    /// </summary>
    public class SegmentManagementService : ISegmentManagementService
    {
        private readonly IEditorState _editorState;

        public SegmentManagementService(IEditorState editorState)
        {
            _editorState = editorState ?? throw new ArgumentNullException(nameof(editorState));
        }

        /// <summary>
        /// Ends the currently active segment by setting its end page and updating the article's pages.
        /// </summary>
        /// <param name="dataContext">The DataContext (typically EditorStateViewModel) for syncing view model</param>
        public void EndActiveSegment(object? dataContext)
        {
            try
            {
                DebugLogger.Log("SegmentManagementService.EndActiveSegment: invoked");
                Console.WriteLine("[DEBUG] SegmentManagementService.EndActiveSegment: invoked");
                
                if (_editorState.ActiveSegment == null || !_editorState.ActiveSegment.IsActive)
                {
                    DebugLogger.Log("EndActiveSegment: no active segment to end");
                    return;
                }

                var start = _editorState.ActiveSegment.Start;
                var end = _editorState.CurrentPage;
                
                // Swap if end is before start
                if (end < start) 
                {
                    (start, end) = (end, start);
                }
                
                DebugLogger.Log($"EndActiveSegment: start={start} end={end}");
                Console.WriteLine($"[DEBUG] EndActiveSegment: start={start} end={end}");

                var article = _editorState.ActiveArticle;
                if (article != null)
                {
                    UpdateArticlePages(article, start, end);
                    SyncViewModelArticle(article, dataContext);
                    ValidateArticleSegments(article);
                }

                CloseActiveSegment();
                
                _editorState.NotifyStateChanged();
                DebugLogger.Log("EndActiveSegment: completed and cleared ActiveSegment");
                Console.WriteLine("[DEBUG] EndActiveSegment: completed and cleared ActiveSegment");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("SegmentManagementService.EndActiveSegment", ex);
            }
        }

        /// <summary>
        /// Adds a new active segment at the current page.
        /// Delegates to EditorActions for consistency.
        /// </summary>
        /// <returns>True if a new active segment was created</returns>
        public bool AddSegmentAtCurrentPage()
        {
            try
            {
                return EditorActions.AddSegmentAtCurrentPage();
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("SegmentManagementService.AddSegmentAtCurrentPage", ex);
                return false;
            }
        }

        /// <summary>
        /// Updates the article's page list to include all pages in the segment range
        /// </summary>
        private void UpdateArticlePages(Common.Shared.ArticleLine article, int start, int end)
        {
            try
            {
                var newPages = new List<int>(article.Pages ?? new List<int>());
                
                // Add all pages in the range
                for (int page = start; page <= end; page++)
                {
                    if (!newPages.Contains(page))
                    {
                        newPages.Add(page);
                    }
                }
                
                newPages.Sort();
                article.Pages = newPages;
                
                DebugLogger.Log($"EndActiveSegment: article updated with {newPages.Count} pages (first={newPages.FirstOrDefault()})");
                Console.WriteLine($"[DEBUG] EndActiveSegment: article updated with {newPages.Count} pages (first={newPages.FirstOrDefault()})");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("SegmentManagementService.UpdateArticlePages", ex);
            }
        }

        /// <summary>
        /// Syncs the article pages with the view model
        /// </summary>
        private void SyncViewModelArticle(Common.Shared.ArticleLine article, object? dataContext)
        {
            try
            {
                var vm = dataContext as Views.EditorStateViewModel;
                if (vm == null) return;

                var newPages = article.Pages;
                if (newPages == null) return;

                var vmMatch = vm.Articles.FirstOrDefault(a => ReferenceEquals(a, article))
                          ?? vm.Articles.FirstOrDefault(a => 
                              a.Pages != null && 
                              a.Pages.SequenceEqual(newPages) && 
                              (a.Title ?? string.Empty) == (article.Title ?? string.Empty));
                
                if (vmMatch != null && !ReferenceEquals(vmMatch, article))
                {
                    vmMatch.Pages = new List<int>(newPages);
                    DebugLogger.Log("EndActiveSegment: VM article pages updated");
                    Console.WriteLine("[DEBUG] EndActiveSegment: VM article pages updated");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("SegmentManagementService.SyncViewModelArticle", ex);
            }
        }

        /// <summary>
        /// Validates segments for missing pages
        /// </summary>
        private void ValidateArticleSegments(Common.Shared.ArticleLine article)
        {
            try
            {
                var folder = _editorState.CurrentFolder;
                if (!string.IsNullOrWhiteSpace(folder))
                {
                    article.ValidateSegments(folder, (f, p) => ImageHelper.ImageExists(f, p));
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("SegmentManagementService.ValidateArticleSegments", ex);
            }
        }

        /// <summary>
        /// Closes and clears the active segment
        /// </summary>
        private void CloseActiveSegment()
        {
            try
            {
                if (_editorState.ActiveSegment != null)
                {
                    _editorState.ActiveSegment.End = _editorState.CurrentPage;
                    _editorState.ActiveSegment.CurrentPreviewEnd = null;
                    DebugLogger.Log($"EndActiveSegment: set ActiveSegment.End={_editorState.CurrentPage}");
                    Console.WriteLine($"[DEBUG] EndActiveSegment: set ActiveSegment.End={_editorState.CurrentPage}");
                }
                
                _editorState.ActiveSegment = null;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("SegmentManagementService.CloseActiveSegment", ex);
            }
        }
    }
}

