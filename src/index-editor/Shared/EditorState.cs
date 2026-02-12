using System;
using System.Collections.Generic;
using Common.Shared;

namespace IndexEditor.Shared
{
    /// <summary>
    /// Implementation of IEditorState for application-wide editor state management.
    /// This class is injectable via DI for proper dependency inversion.
    /// </summary>
    public class EditorStateService : IEditorState
    {
        private int _articleEditorFocusRequest = 0;
        
        public int CurrentPage { get; set; } = 8;
        public List<ArticleLine> Articles { get; set; } = new();
        public ArticleLine? ActiveArticle { get; set; }
        public Segment? ActiveSegment { get; set; }
        public bool IsArticleEditorFocused { get; set; } = false;
        public string? CurrentMagazine { get; set; }
        public string? CurrentVolume { get; set; }
        public string? CurrentNumber { get; set; }
        public string? CurrentFolder { get; set; }
        public bool ShowImages { get; set; } = true;
        public int ArticleEditorFocusRequest => _articleEditorFocusRequest;
        
        public event Action? StateChanged;
        
        public void RequestArticleEditorFocus()
        {
            try 
            { 
                _articleEditorFocusRequest++; 
                NotifyStateChanged(); 
            } 
            catch (Exception ex) 
            { 
                DebugLogger.LogException("EditorStateService.RequestArticleEditorFocus", ex); 
            }
            Console.WriteLine("Focus requested. Current request count: " + _articleEditorFocusRequest);
        }
        
        public void NotifyStateChanged() => StateChanged?.Invoke();
    }

    /// <summary>
    /// Static wrapper around EditorStateService for backward compatibility.
    /// NEW CODE SHOULD USE IEditorState via dependency injection instead.
    /// This will be deprecated once all usages are migrated to DI.
    /// </summary>
    [Obsolete("Use IEditorState via dependency injection instead. This static wrapper is for backward compatibility only.")]
    public static class EditorState
    {
        // Singleton instance for backward compatibility
        // Will be set by DI container during app initialization
        private static IEditorState? _instance;
        
        /// <summary>
        /// Sets the singleton instance. Called by DI container during initialization.
        /// For testing use only - not intended for application code.
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static void SetInstance(IEditorState instance)
        {
            _instance = instance ?? throw new ArgumentNullException(nameof(instance));
        }
        
        private static IEditorState Instance => _instance 
            ?? throw new InvalidOperationException("EditorState not initialized. Call SetInstance() during app startup.");
        
        // Static properties delegating to singleton instance
        public static int CurrentPage 
        { 
            get => Instance.CurrentPage; 
            set => Instance.CurrentPage = value; 
        }
        
        public static List<ArticleLine> Articles 
        { 
            get => Instance.Articles; 
            set => Instance.Articles = value; 
        }
        
        public static ArticleLine? ActiveArticle 
        { 
            get => Instance.ActiveArticle; 
            set => Instance.ActiveArticle = value; 
        }
        
        public static Segment? ActiveSegment 
        { 
            get => Instance.ActiveSegment; 
            set => Instance.ActiveSegment = value; 
        }
        
        public static bool IsArticleEditorFocused 
        { 
            get => Instance.IsArticleEditorFocused; 
            set => Instance.IsArticleEditorFocused = value; 
        }
        
        public static string? CurrentMagazine 
        { 
            get => Instance.CurrentMagazine; 
            set => Instance.CurrentMagazine = value; 
        }
        
        public static string? CurrentVolume 
        { 
            get => Instance.CurrentVolume; 
            set => Instance.CurrentVolume = value; 
        }
        
        public static string? CurrentNumber 
        { 
            get => Instance.CurrentNumber; 
            set => Instance.CurrentNumber = value; 
        }
        
        public static string? CurrentFolder 
        { 
            get => Instance.CurrentFolder; 
            set => Instance.CurrentFolder = value; 
        }
        
        public static bool ShowImages 
        { 
            get => Instance.ShowImages; 
            set => Instance.ShowImages = value; 
        }
        
        public static int ArticleEditorFocusRequest => Instance.ArticleEditorFocusRequest;
        
        public static event Action? StateChanged
        {
            add => Instance.StateChanged += value;
            remove => Instance.StateChanged -= value;
        }
        
        public static void RequestArticleEditorFocus() => Instance.RequestArticleEditorFocus();
        
        public static void NotifyStateChanged() => Instance.NotifyStateChanged();
    }
}
