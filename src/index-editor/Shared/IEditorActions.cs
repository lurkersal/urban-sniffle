using System;
using System.Collections.Generic;
using System.Linq;
using Common.Shared;

namespace IndexEditor.Shared
{
    /// <summary>
    /// Injectable service for article and segment editing operations.
    /// Implements business logic for the index editor.
    /// </summary>
    public interface IEditorActions
    {
        /// <summary>
        /// Event raised when a new article is created, allowing UI layer to respond (e.g., select it).
        /// </summary>
        event Action<ArticleLine>? ArticleCreated;
        
        /// <summary>
        /// Cancels the currently active segment if one exists.
        /// If the segment was newly created, it is removed from the article.
        /// Otherwise, its End value is restored to the original or set to Start.
        /// </summary>
        void CancelActiveSegment();
        
        /// <summary>
        /// Creates a new ACTIVE segment at the current page for the active article.
        /// If the page already belongs to the article, activates the existing segment instead.
        /// </summary>
        /// <returns>True if a segment was created or activated; false otherwise.</returns>
        bool AddSegmentAtCurrentPage();
        
        /// <summary>
        /// Requests focus for the ArticleEditor's Title TextBox.
        /// This increments the EditorState counter which ArticleEditor instances observe
        /// and will attempt to focus the TitleTextBox when they see the request.
        /// </summary>
        void FocusArticleTitle();
        
        /// <summary>
        /// Creates a new closed single-page article at the current page.
        /// The article is automatically selected as the active article and includes
        /// one closed segment for the current page. Raises the ArticleCreated event.
        /// </summary>
        void CreateNewArticle();
        
        /// <summary>
        /// Ends the currently active segment by setting its End to the current page
        /// and updates the article's page collection to include all pages in the segment range.
        /// </summary>
        void EndActiveSegment();
    }
    
    /// <summary>
    /// Implementation of IEditorActions with dependency injection.
    /// </summary>
    public class EditorActionsService : IEditorActions
    {
        private readonly IEditorState _state;
        
        public EditorActionsService(IEditorState state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }
        
        public event Action<ArticleLine>? ArticleCreated;
        
        /// <summary>
        /// Helper to write trace logs without throwing if console is unavailable.
        /// </summary>
        private static void TraceLog(string message)
        {
            try { Console.WriteLine(message); }
            catch { /* Console output not available */ }
        }
        
        public void CancelActiveSegment()
        {
            try
            {
                var segment = _state.ActiveSegment;
                if (segment == null || !segment.IsActive) return;
                
                var article = _state.ActiveArticle;
                if (segment.WasNew)
                {
                    try { article?.Segments?.Remove(segment); }
                    catch (Exception ex) { DebugLogger.LogException("EditorActionsService.CancelActiveSegment: remove segment", ex); }
                }
                else if (segment.OriginalEnd.HasValue)
                {
                    segment.End = segment.OriginalEnd;
                    segment.OriginalEnd = null;
                }
                else
                {
                    segment.End = segment.Start;
                }
                
                segment.WasNew = false;
                try { segment.CurrentPreviewEnd = null; }
                catch (Exception ex) { DebugLogger.LogException("EditorActionsService.CancelActiveSegment: clear preview", ex); }
                
                _state.ActiveSegment = null;
                _state.NotifyStateChanged();
            }
            catch (Exception ex) { DebugLogger.LogException("EditorActionsService.CancelActiveSegment: outer", ex); }
        }

        public bool AddSegmentAtCurrentPage()
        {
            try
            {
                var article = _state.ActiveArticle;
                if (!ValidateCanAddSegment(article))
                    return false;

                if (article == null) return false;

                var page = _state.CurrentPage;

                if (PageBelongsToArticle(article, page))
                    return ActivateExistingSegment(article, page);

                return CreateNewActiveSegment(article, page);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("EditorActionsService.AddSegmentAtCurrentPage: outer", ex);
                return false;
            }
        }

        private bool ValidateCanAddSegment(ArticleLine? article)
        {
            if (article == null)
            {
                ToastService.Show("No active article selected");
                return false;
            }

            if (_state.ActiveSegment != null && _state.ActiveSegment.IsActive)
            {
                ToastService.Show("Finish or cancel the active segment first");
                TraceLog("[TRACE] EditorActionsService.AddSegmentAtCurrentPage: blocked - active segment exists");
                return false;
            }

            return true;
        }

        private static bool PageBelongsToArticle(ArticleLine article, int page)
        {
            return article.Pages != null && article.Pages.Contains(page);
        }

        private bool ActivateExistingSegment(ArticleLine article, int page)
        {
            try
            {
                var existingSegment = FindSegmentContainingPage(article, page);

                if (existingSegment != null)
                {
                    OpenSegmentForEditing(existingSegment);
                    _state.ActiveSegment = existingSegment;
                    
                    try { existingSegment.CurrentPreviewEnd = _state.CurrentPage; }
                    catch (Exception ex) { DebugLogger.LogException("EditorActionsService.ActivateExistingSegment: set preview", ex); }
                    
                    TraceLog($"[TRACE] EditorActionsService.AddSegmentAtCurrentPage: activated existing segment start={existingSegment.Start} article='{article.Title ?? "<untitled>"}'");
                    _state.NotifyStateChanged();
                    return true;
                }

                ToastService.Show($"Page {page} already in article");
                return false;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("EditorActionsService.ActivateExistingSegment: outer", ex);
                ToastService.Show($"Page {page} already in article");
                return false;
            }
        }

        private static Segment? FindSegmentContainingPage(ArticleLine article, int page)
        {
            return article.Segments?.FirstOrDefault(s =>
            {
                try
                {
                    var sEnd = s.End ?? s.Start;
                    return page >= s.Start && page <= sEnd;
                }
                catch { return false; }
            });
        }

        private static void OpenSegmentForEditing(Segment segment)
        {
            if (segment.End.HasValue)
            {
                segment.OriginalEnd = segment.End;
                segment.WasNew = false;
                segment.End = null;
            }
        }

        private bool CreateNewActiveSegment(ArticleLine article, int page)
        {
            var segment = new Segment(page)
            {
                WasNew = true,
                End = null
            };

            article.Segments.Add(segment);
            AddPageToArticle(article, page);
            
            _state.ActiveSegment = segment;
            
            try { segment.CurrentPreviewEnd = _state.CurrentPage; }
            catch (Exception ex) { DebugLogger.LogException("EditorActionsService.CreateNewActiveSegment: set preview", ex); }
            
            TraceLog($"[TRACE] EditorActionsService.AddSegmentAtCurrentPage: created new active segment start={segment.Start} article='{article.Title ?? "<untitled>"}'");
            _state.NotifyStateChanged();
            return true;
        }

        private static void AddPageToArticle(ArticleLine article, int page)
        {
            if (article.Pages == null)
            {
                article.Pages = new List<int> { page };
            }
            else if (!article.Pages.Contains(page))
            {
                article.Pages.Add(page);
                article.Pages.Sort();
            }
        }

        public void FocusArticleTitle()
        {
            try
            {
                _state.RequestArticleEditorFocus();
                TraceLog("[TRACE] EditorActionsService.FocusArticleTitle: requested focus");
            }
            catch (Exception ex) { DebugLogger.LogException("EditorActionsService.FocusArticleTitle", ex); }
        }

        public void CreateNewArticle()
        {
            try
            {
                if (_state.ActiveSegment != null && _state.ActiveSegment.IsActive)
                {
                    ToastService.Show("End or cancel the active segment before adding a new article");
                    return;
                }
                
                var article = new ArticleLine();
                article.Pages = new List<int> { _state.CurrentPage };
                
                if (_state.Articles == null)
                    _state.Articles = new List<ArticleLine> { article };
                else
                {
                    int insertIndex = _state.Articles.FindIndex(a => a.Pages != null && a.Pages.Count > 0 && a.Pages.Min() > article.Pages.Min());
                    if (insertIndex == -1)
                        _state.Articles.Add(article);
                    else
                        _state.Articles.Insert(insertIndex, article);
                }
                
                _state.ActiveArticle = article;
                
                // Add a closed segment for the current page if not already present
                bool alreadyHas = false;
                try
                {
                    if (article.Segments != null)
                    {
                        foreach (var segment in article.Segments)
                        {
                            try
                            {
                                var start = segment.Start;
                                var end = segment.End ?? segment.Start;
                                if (_state.CurrentPage >= start && _state.CurrentPage <= end)
                                {
                                    alreadyHas = true;
                                    break;
                                }
                            }
                            catch (Exception ex) { DebugLogger.LogException("EditorActionsService.CreateNewArticle: checking existing segments", ex); }
                        }
                    }
                }
                catch (Exception ex) { DebugLogger.LogException("EditorActionsService.CreateNewArticle: outer segment check", ex); }
                
                if (!alreadyHas)
                {
                    var segment = new Segment(_state.CurrentPage)
                    {
                        End = _state.CurrentPage,
                        WasNew = false
                    };
                    article.Segments?.Add(segment);
                }
                
                _state.NotifyStateChanged();
                
                // Raise event to notify UI layer
                try
                {
                    ArticleCreated?.Invoke(article);
                }
                catch (Exception ex) { DebugLogger.LogException("EditorActionsService.CreateNewArticle: invoke ArticleCreated event", ex); }
            }
            catch (Exception ex) { DebugLogger.LogException("EditorActionsService.CreateNewArticle: outer", ex); }
        }

        public void EndActiveSegment()
        {
            try
            {
                var segment = _state.ActiveSegment;
                if (segment == null || !segment.IsActive) return;
                
                var start = segment.Start;
                var end = _state.CurrentPage;
                if (end < start) (start, end) = (end, start);
                
                var article = _state.ActiveArticle;
                if (article != null)
                {
                    var newPages = new List<int>(article.Pages ?? new List<int>());
                    for (int p = start; p <= end; p++)
                    {
                        if (!newPages.Contains(p))
                            newPages.Add(p);
                    }
                    newPages.Sort();
                    article.Pages = newPages;
                }
                
                segment.End = _state.CurrentPage;
                segment.CurrentPreviewEnd = null;
                _state.ActiveSegment = null;
                _state.NotifyStateChanged();
            }
            catch (Exception ex) { DebugLogger.LogException("EditorActionsService.EndActiveSegment", ex); }
        }
    }
}

