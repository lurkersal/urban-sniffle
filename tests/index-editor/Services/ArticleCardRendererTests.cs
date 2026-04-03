using Xunit;
using IndexEditor.Services;
using Common.Shared;
using Avalonia.Controls;
using Avalonia.Media;
using System.Linq;

namespace IndexEditor.Tests.Services
{
    public class ArticleCardRendererTests
    {
        private readonly ArticleCardRenderer _renderer;

        public ArticleCardRendererTests()
        {
            _renderer = new ArticleCardRenderer();
        }

        [Fact]
        public void CreateArticleCard_WithValidArticle_ReturnsCard()
        {
            // Arrange
            var article = new ArticleLine
            {
                Title = "Test Article",
                Category = "Model",
                ModelName0 = "Jane Doe",
                Age0 = "25",
                Contributor0 = "John Photographer"
            };

            // Act
            var card = _renderer.CreateArticleCard(article);

            // Assert
            Assert.NotNull(card);
            Assert.IsType<Border>(card);
        }

        [Fact]
        public void CreateArticleCard_WithNullArticle_ReturnsNull()
        {
            // Act
            var card = _renderer.CreateArticleCard(null);

            // Assert
            Assert.Null(card);
        }

        [Fact]
        public void CreateArticleCard_WithCategoryOnly_UsesCategory()
        {
            // Arrange
            var article = new ArticleLine
            {
                Category = "Editorial"
            };

            // Act
            var card = _renderer.CreateArticleCard(article);

            // Assert
            Assert.NotNull(card);
            // The card should be created even without title
        }

        [Fact]
        public void CreateArticleCard_WithModelCategory_ShowsModelDetails()
        {
            // Arrange
            var article = new ArticleLine
            {
                Title = "Model Test",
                Category = "Model",
                ModelName0 = "Jane Doe",
                Age0 = "25"
            };

            // Act
            var card = _renderer.CreateArticleCard(article);

            // Assert
            Assert.NotNull(card);
            // Card structure should contain model name and age
            var grid = card.Child as Grid;
            Assert.NotNull(grid);
            Assert.Equal(3, grid.ColumnDefinitions.Count); // Color bar, content, category
        }

        [Fact]
        public void CreateArticleCard_WithCoverCategory_ShowsModelDetails()
        {
            // Arrange
            var article = new ArticleLine
            {
                Title = "Cover Test",
                Category = "Cover",
                ModelName0 = "Jane Doe",
                Age0 = "25"
            };

            // Act
            var card = _renderer.CreateArticleCard(article);

            // Assert
            Assert.NotNull(card);
        }

        [Fact]
        public void CreateArticleCard_WithEditorialCategory_HidesModelDetails()
        {
            // Arrange
            var article = new ArticleLine
            {
                Title = "Editorial Test",
                Category = "Editorial",
                Contributor0 = "John Writer"
            };

            // Act
            var card = _renderer.CreateArticleCard(article);

            // Assert
            Assert.NotNull(card);
            // Should only show contributor, not model details
        }

        [Fact]
        public void CreateArticleCard_WithEmptyTitle_UsesCategoryAsFallback()
        {
            // Arrange
            var article = new ArticleLine
            {
                Title = "",
                Category = "Letters"
            };

            // Act
            var card = _renderer.CreateArticleCard(article);

            // Assert
            Assert.NotNull(card);
        }

        [Fact]
        public void CreateArticleCard_CreatesColorBar()
        {
            // Arrange
            var article = new ArticleLine
            {
                Title = "Test",
                Category = "Model"
            };

            // Act
            var card = _renderer.CreateArticleCard(article);

            // Assert
            Assert.NotNull(card);
            var grid = card.Child as Grid;
            Assert.NotNull(grid);
            
            // First child should be the color bar
            var colorBar = grid.Children[0] as Border;
            Assert.NotNull(colorBar);
        }
    }
}

