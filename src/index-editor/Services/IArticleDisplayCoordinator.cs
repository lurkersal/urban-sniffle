using Avalonia.Controls;

namespace IndexEditor.Services
{
    /// <summary>
    /// Service interface for coordinating article display in the page controller
    /// </summary>
    public interface IArticleDisplayCoordinator
    {
        /// <summary>
        /// Updates the article cards container with articles for the current page.
        /// Shows article cards for all articles that contain the current page (stacked if multiple).
        /// Prioritizes showing the currently selected article if it exists.
        /// </summary>
        /// <param name="articleCardsContainer">The container to populate with article cards</param>
        /// <param name="selectedArticle">The currently selected article (if any)</param>
        void UpdateArticleDisplay(StackPanel articleCardsContainer, Common.Shared.ArticleLine? selectedArticle);
    }
}

