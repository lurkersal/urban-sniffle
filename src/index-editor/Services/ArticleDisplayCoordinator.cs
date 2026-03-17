using Avalonia.Controls;
using IndexEditor.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IndexEditor.Services
{
    /// <summary>
    /// Service responsible for coordinating article display in the page controller.
    /// Handles which articles to display and in what order.
    /// </summary>
    public class ArticleDisplayCoordinator : IArticleDisplayCoordinator
    {
        private readonly IEditorState _editorState;
        private readonly IArticleCardRenderer _cardRenderer;

        public ArticleDisplayCoordinator(IEditorState editorState, IArticleCardRenderer cardRenderer)
        {
            _editorState = editorState ?? throw new ArgumentNullException(nameof(editorState));
            _cardRenderer = cardRenderer ?? throw new ArgumentNullException(nameof(cardRenderer));
        }

        /// <summary>
        /// Updates the article cards container with articles for the current page.
        /// Shows article cards for all articles that contain the current page (stacked if multiple).
        /// Prioritizes showing the currently selected article if it exists.
        /// </summary>
        /// <param name="articleCardsContainer">The container to populate with article cards</param>
        /// <param name="selectedArticle">The currently selected article (if any)</param>
        public void UpdateArticleDisplay(StackPanel articleCardsContainer, Common.Shared.ArticleLine? selectedArticle)
        {
            if (articleCardsContainer == null)
            {
                return;
            }

            try
            {
                // Clear existing article cards
                articleCardsContainer.Children.Clear();

                // Find all articles that contain the current page
                var currentPage = _editorState.CurrentPage;
                var articlesOnPage = GetArticlesOnPage(currentPage);

                // Render articles based on selection
                if (ShouldShowSelectedArticleFirst(selectedArticle, currentPage))
                {
                    RenderSelectedArticleFirst(articleCardsContainer, selectedArticle!, articlesOnPage);
                }
                else if (articlesOnPage.Count > 0)
                {
                    RenderAllArticlesOnPage(articleCardsContainer, articlesOnPage);
                }
                // If no articles found on the current page, leave container empty
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ArticleDisplayCoordinator.UpdateArticleDisplay", ex);
            }
        }

        /// <summary>
        /// Gets all articles that contain the specified page
        /// </summary>
        private List<Common.Shared.ArticleLine> GetArticlesOnPage(int page)
        {
            return _editorState.Articles
                .Where(a => a.Pages != null && a.Pages.Contains(page))
                .ToList();
        }

        /// <summary>
        /// Determines if the selected article should be shown first
        /// </summary>
        private bool ShouldShowSelectedArticleFirst(Common.Shared.ArticleLine? selectedArticle, int currentPage)
        {
            return selectedArticle != null 
                && selectedArticle.Pages != null 
                && selectedArticle.Pages.Contains(currentPage);
        }

        /// <summary>
        /// Renders the selected article first, followed by other articles on the page
        /// </summary>
        private void RenderSelectedArticleFirst(
            StackPanel container, 
            Common.Shared.ArticleLine selectedArticle, 
            List<Common.Shared.ArticleLine> articlesOnPage)
        {
            // Show selected article first
            var card = _cardRenderer.CreateArticleCard(selectedArticle);
            if (card != null)
            {
                container.Children.Add(card);
            }

            // Show other articles on this page (excluding the selected one)
            foreach (var article in articlesOnPage.Where(a => !ReferenceEquals(a, selectedArticle)))
            {
                card = _cardRenderer.CreateArticleCard(article);
                if (card != null)
                {
                    container.Children.Add(card);
                }
            }
        }

        /// <summary>
        /// Renders all articles on the current page
        /// </summary>
        private void RenderAllArticlesOnPage(StackPanel container, List<Common.Shared.ArticleLine> articlesOnPage)
        {
            foreach (var article in articlesOnPage)
            {
                var card = _cardRenderer.CreateArticleCard(article);
                if (card != null)
                {
                    container.Children.Add(card);
                }
            }
        }
    }
}

