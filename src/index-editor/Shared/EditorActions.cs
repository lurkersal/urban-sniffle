using System;
using System.Collections.Generic;
using System.Linq;
using Common.Shared;

namespace IndexEditor.Shared
{
    public static class EditorActions
    {
        // Event raised when a new article is created, allowing UI layer to respond (e.g., select it)
        public static event Action<ArticleLine>? ArticleCreated;
        
        /// <summary>
        /// Helper to write trace logs without throwing if console is unavailable.
        /// </summary>
        private static void TraceLog(string message)
        {
            try { Console.WriteLine(message); }
            catch { /* Console output not available */ }
        }
        
        /// <summary>
        /// Cancels the currently active segment if one exists.
        /// If the segment was newly created, it is removed from the article.
        /// Otherwise, its End value is restored to the original or set to Start.
        /// </summary>
        public static void CancelActiveSegment()
        {
            try
            {
                var segment = EditorState.ActiveSegment;
                if (segment == null || !segment.IsActive) return;
                
                var article = EditorState.ActiveArticle;
                if (segment.WasNew)
                {
                    try { article?.Segments?.Remove(segment); }
                    catch (Exception ex) { DebugLogger.LogException("EditorActions.CancelActiveSegment: remove segment", ex); }
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
                catch (Exception ex) { DebugLogger.LogException("EditorActions.CancelActiveSegment: clear preview", ex); }
                
                EditorState.ActiveSegment = null;
                EditorState.NotifyStateChanged();
            }
            catch (Exception ex) { DebugLogger.LogException("EditorActions.CancelActiveSegment: outer", ex); }
        }

        /// <summary>
        /// Creates a new ACTIVE segment at the current page for the active article.
        /// If the page already belongs to the article, activates the existing segment instead.
        /// </summary>
        /// <returns>True if a segment was created or activated; false otherwise.</returns>
        public static bool AddSegmentAtCurrentPage()
        {
            try
            {
                var article = EditorState.ActiveArticle;
                if (!ValidateCanAddSegment(article))
                    return false;

                // At this point, article is guaranteed to be non-null by ValidateCanAddSegment
                if (article == null) return false; // Should never happen, but satisfies nullable analysis

                var page = EditorState.CurrentPage;

                // If the page already belongs to this article, activate existing segment
                if (PageBelongsToArticle(article, page))
                    return ActivateExistingSegment(article, page);

                // Page is not in article -> create a new active segment
                return CreateNewActiveSegment(article, page);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("EditorActions.AddSegmentAtCurrentPage: outer", ex);
                return false;
            }
        }

        /// <summary>
        /// Validates that a segment can be added (article exists and no other active segment).
        /// </summary>
        private static bool ValidateCanAddSegment(ArticleLine? article)
        {
            if (article == null)
            {
                ToastService.Show("No active article selected");
                return false;
            }

            if (EditorState.ActiveSegment != null && EditorState.ActiveSegment.IsActive)
            {
                ToastService.Show("Finish or cancel the active segment first");
                TraceLog("[TRACE] EditorActions.AddSegmentAtCurrentPage: blocked - active segment exists");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if a page already belongs to an article.
        /// </summary>
        private static bool PageBelongsToArticle(ArticleLine article, int page)
        {
            return article.Pages != null && article.Pages.Contains(page);
        }

        /// <summary>
        /// Activates an existing segment that contains the specified page.
        /// </summary>
        private static bool ActivateExistingSegment(ArticleLine article, int page)
        {
            try
            {
                var existingSegment = FindSegmentContainingPage(article, page);

                if (existingSegment != null)
                {
                    OpenSegmentForEditing(existingSegment);
                    EditorState.ActiveSegment = existingSegment;
                    
                    try { existingSegment.CurrentPreviewEnd = EditorState.CurrentPage; }
                    catch (Exception ex) { DebugLogger.LogException("EditorActions.ActivateExistingSegment: set preview", ex); }
                    
                    TraceLog($"[TRACE] EditorActions.AddSegmentAtCurrentPage: activated existing segment start={existingSegment.Start} article='{article.Title ?? "<untitled>"}'");
                    EditorState.NotifyStateChanged();
                    return true;
                }

                // No existing segment found that contains the page
                ToastService.Show($"Page {page} already in article");
                return false;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("EditorActions.ActivateExistingSegment: outer", ex);
                ToastService.Show($"Page {page} already in article");
                return false;
            }
        }

        /// <summary>
        /// Finds the segment that contains the specified page number.
        /// </summary>
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

        /// <summary>
        /// Opens a closed segment for editing by clearing its End value.
        /// </summary>
        private static void OpenSegmentForEditing(Segment segment)
        {
            if (segment.End.HasValue)
            {
                segment.OriginalEnd = segment.End;
                segment.WasNew = false;
                segment.End = null; // make active
            }
        }

        /// <summary>
        /// Creates a new active segment at the specified page.
        /// </summary>
        private static bool CreateNewActiveSegment(ArticleLine article, int page)
        {
            var segment = new Segment(page)
            {
                WasNew = true,
                End = null
            };

            article.Segments.Add(segment);
            AddPageToArticle(article, page);
            
            EditorState.ActiveSegment = segment;
            
            try { segment.CurrentPreviewEnd = EditorState.CurrentPage; }
            catch (Exception ex) { DebugLogger.LogException("EditorActions.CreateNewActiveSegment: set preview", ex); }
            
            TraceLog($"[TRACE] EditorActions.AddSegmentAtCurrentPage: created new active segment start={segment.Start} article='{article.Title ?? "<untitled>"}'");
            EditorState.NotifyStateChanged();
            return true;
        }

        /// <summary>
        /// Adds a page to an article's page collection, maintaining sorted order.
        /// </summary>
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

        /// <summary>
        /// Requests focus for the ArticleEditor's Title TextBox.
        /// This increments the EditorState counter which ArticleEditor instances observe
        /// and will attempt to focus the TitleTextBox when they see the request.
        /// </summary>
        public static void FocusArticleTitle()
        {
            try
            {
                EditorState.RequestArticleEditorFocus();
                TraceLog("[TRACE] EditorActions.FocusArticleTitle: requested focus");
            }
            catch (Exception ex) { DebugLogger.LogException("EditorActions.FocusArticleTitle", ex); }
        }

        /// <summary>
        /// Creates a new closed single-page article at the current page.
        /// The article is automatically selected as the active article and includes
        /// one closed segment for the current page. Raises the ArticleCreated event.
        /// </summary>
        public static void CreateNewArticle()
        {
            try
            {
                if (EditorState.ActiveSegment != null && EditorState.ActiveSegment.IsActive)
                {
                    ToastService.Show("End or cancel the active segment before adding a new article");
                    return;
                }
                var article = new ArticleLine();
                article.Pages = new List<int> { EditorState.CurrentPage };
                if (EditorState.Articles == null)
                    EditorState.Articles = new List<ArticleLine> { article };
                else
                {
                    int insertIndex = EditorState.Articles.FindIndex(a => a.Pages != null && a.Pages.Count > 0 && a.Pages.Min() > article.Pages.Min());
                    if (insertIndex == -1)
                        EditorState.Articles.Add(article);
                    else
                        EditorState.Articles.Insert(insertIndex, article);
                }
                EditorState.ActiveArticle = article;
                
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
                                if (EditorState.CurrentPage >= start && EditorState.CurrentPage <= end)
                                {
                                    alreadyHas = true;
                                    break;
                                }
                            }
                            catch (Exception ex) { DebugLogger.LogException("EditorActions.CreateNewArticle: checking existing segments", ex); }
                        }
                    }
                }
                catch (Exception ex) { DebugLogger.LogException("EditorActions.CreateNewArticle: outer segment check", ex); }
                
                if (!alreadyHas)
                {
                    var segment = new Segment(EditorState.CurrentPage)
                    {
                        End = EditorState.CurrentPage,
                        WasNew = false
                    };
                    article.Segments?.Add(segment);
                }
                
                EditorState.NotifyStateChanged();
                
                // Raise event to notify UI layer (e.g., to select the article in the ViewModel)
                try
                {
                    ArticleCreated?.Invoke(article);
                }
                catch (Exception ex) { DebugLogger.LogException("EditorActions.CreateNewArticle: invoke ArticleCreated event", ex); }
            }
            catch (Exception ex) { DebugLogger.LogException("EditorActions.CreateNewArticle: outer", ex); }
        }

        /// <summary>
        /// Ends the currently active segment by setting its End to the current page
        /// and updates the article's page collection to include all pages in the segment range.
        /// </summary>
        public static void EndActiveSegment()
        {
            try
            {
                var segment = EditorState.ActiveSegment;
                if (segment == null || !segment.IsActive) return;
                
                var start = segment.Start;
                var end = EditorState.CurrentPage;
                if (end < start) (start, end) = (end, start);
                
                var article = EditorState.ActiveArticle;
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
                
                segment.End = EditorState.CurrentPage;
                segment.CurrentPreviewEnd = null;
                EditorState.ActiveSegment = null;
                EditorState.NotifyStateChanged();
            }
            catch (Exception ex) { DebugLogger.LogException("EditorActions.EndActiveSegment", ex); }
        }
    }
}
