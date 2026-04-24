using System;
using System.Collections.Generic;
using System.Linq;
using Common.Shared;

namespace IndexEditor.Shared
{
    /// <summary>
    /// Static wrapper around IEditorActions for backward compatibility.
    /// NEW CODE SHOULD USE IEditorActions via dependency injection instead.
    /// This will be deprecated once all usages are migrated to DI.
    /// </summary>
    [Obsolete("Use IEditorActions via dependency injection instead. This static wrapper is for backward compatibility only.")]
    public static class EditorActions
    {
        // Singleton instance for backward compatibility
        private static IEditorActions? _instance;
        
        /// <summary>
        /// <summary>
        /// Sets the singleton instance. Called by DI container during initialization.
        /// For testing use only - not intended for application code.
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static void SetInstance(IEditorActions instance)
        {
            _instance = instance ?? throw new ArgumentNullException(nameof(instance));
        }
        
        private static IEditorActions Instance => _instance 
            ?? throw new InvalidOperationException("EditorActions not initialized. Call SetInstance() during app startup.");
        
        // Event raised when a new article is created, allowing UI layer to respond (e.g., select it)
        public static event Action<ArticleLine>? ArticleCreated
        {
            add => Instance.ArticleCreated += value;
            remove => Instance.ArticleCreated -= value;
        }
        
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
        public static void CancelActiveSegment() => Instance.CancelActiveSegment();

        /// <summary>
        /// Creates a new ACTIVE segment at the current page for the active article.
        /// If the page already belongs to the article, activates the existing segment instead.
        /// </summary>
        /// <returns>True if a segment was created or activated; false otherwise.</returns>
        public static bool AddSegmentAtCurrentPage() => Instance.AddSegmentAtCurrentPage();

        /// <summary>
        /// Requests focus for the ArticleEditor's Title TextBox.
        /// This increments the EditorState counter which ArticleEditor instances observe
        /// and will attempt to focus the TitleTextBox when they see the request.
        /// </summary>
        public static void FocusArticleTitle() => Instance.FocusArticleTitle();

        /// <summary>
        /// Creates a new closed single-page article at the current page.
        /// The article is automatically selected as the active article and includes
        /// one closed segment for the current page. Raises the ArticleCreated event.
        /// </summary>
        public static void CreateNewArticle() => Instance.CreateNewArticle();

        /// <summary>
        /// Ends the currently active segment by setting its End to the current page
        /// and updates the article's page collection to include all pages in the segment range.
        /// </summary>
        public static void EndActiveSegment() => Instance.EndActiveSegment();
    }
}
