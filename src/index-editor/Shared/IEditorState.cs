using System;
using System.Collections.Generic;
using Common.Shared;

namespace IndexEditor.Shared
{
    /// <summary>
    /// Interface for application-wide editor state management.
    /// Enables dependency injection and testability.
    /// </summary>
    public interface IEditorState
    {
        /// <summary>
        /// The current page number being viewed.
        /// </summary>
        int CurrentPage { get; set; }
        
        /// <summary>
        /// Collection of all articles in the current magazine issue.
        /// </summary>
        List<ArticleLine> Articles { get; set; }
        
        /// <summary>
        /// The currently active/selected article.
        /// </summary>
        ArticleLine? ActiveArticle { get; set; }
        
        /// <summary>
        /// The currently active (open/being edited) segment.
        /// </summary>
        Segment? ActiveSegment { get; set; }
        
        /// <summary>
        /// Whether the Article Editor control currently has keyboard focus.
        /// When true, global arrow-key handlers should avoid changing the current page.
        /// </summary>
        bool IsArticleEditorFocused { get; set; }
        
        /// <summary>
        /// Magazine name extracted from _index.txt or folder name.
        /// </summary>
        string? CurrentMagazine { get; set; }
        
        /// <summary>
        /// Volume number extracted from _index.txt or folder name.
        /// </summary>
        string? CurrentVolume { get; set; }
        
        /// <summary>
        /// Issue number extracted from _index.txt or folder name.
        /// </summary>
        string? CurrentNumber { get; set; }
        
        /// <summary>
        /// Folder path opened by the app (used to locate page image files).
        /// </summary>
        string? CurrentFolder { get; set; }
        
        /// <summary>
        /// Flag to disable loading/showing images (can be set via --no-images command-line argument).
        /// </summary>
        bool ShowImages { get; set; }
        
        /// <summary>
        /// Counter used to request focus in the ArticleEditor.
        /// ArticleEditor instances monitor this counter and attempt to focus the Title textbox when it increases.
        /// </summary>
        int ArticleEditorFocusRequest { get; }
        
        /// <summary>
        /// Event raised when any editor state property changes.
        /// </summary>
        event Action? StateChanged;
        
        /// <summary>
        /// Requests focus for the ArticleEditor's Title textbox by incrementing the focus request counter.
        /// </summary>
        void RequestArticleEditorFocus();
        
        /// <summary>
        /// Notifies all subscribers that the editor state has changed.
        /// </summary>
        void NotifyStateChanged();
    }
}

