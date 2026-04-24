using Avalonia.Controls;

namespace IndexEditor.Services
{
    /// <summary>
    /// Service interface for rendering article cards
    /// </summary>
    public interface IArticleCardRenderer
    {
        /// <summary>
        /// Sets the editor state for the card renderer
        /// </summary>
        void SetEditorState(IndexEditor.Shared.IEditorState editorState);

        /// <summary>
        /// Creates a visual card for an article with colored bar, title, details, and category.
        /// </summary>
        /// <param name="article">The article to render</param>
        /// <returns>A Border control containing the rendered article card, or null if rendering fails</returns>
        Border? CreateArticleCard(Common.Shared.ArticleLine article);
    }
}

