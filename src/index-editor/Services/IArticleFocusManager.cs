using Avalonia;

namespace IndexEditor.Services
{
    /// <summary>
    /// Service interface for managing focus when creating new articles
    /// </summary>
    public interface IArticleFocusManager
    {
        /// <summary>
        /// Manages the focus flow after creating a new article
        /// </summary>
        /// <param name="article">The newly created article</param>
        /// <param name="visualRoot">The visual root (typically MainWindow)</param>
        /// <param name="dataContext">The DataContext (typically EditorStateViewModel)</param>
        void SetupFocusForNewArticle(
            Common.Shared.ArticleLine article,
            object? visualRoot,
            object? dataContext);
    }
}


