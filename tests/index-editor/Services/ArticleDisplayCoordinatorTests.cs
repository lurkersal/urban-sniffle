using Xunit;
using Moq;
using IndexEditor.Services;
using IndexEditor.Shared;
using Common.Shared;
using Avalonia.Controls;
using System.Collections.Generic;

namespace IndexEditor.Tests.Services
{
    public class ArticleDisplayCoordinatorTests
    {
        private readonly Mock<IEditorState> _mockEditorState;
        private readonly Mock<IArticleCardRenderer> _mockCardRenderer;
        private readonly ArticleDisplayCoordinator _coordinator;

        public ArticleDisplayCoordinatorTests()
        {
            _mockEditorState = new Mock<IEditorState>();
            _mockCardRenderer = new Mock<IArticleCardRenderer>();
            _coordinator = new ArticleDisplayCoordinator(_mockEditorState.Object, _mockCardRenderer.Object);
        }

        [Fact]
        public void Constructor_WithNullEditorState_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<System.ArgumentNullException>(() => 
                new ArticleDisplayCoordinator(null, _mockCardRenderer.Object));
        }

        [Fact]
        public void Constructor_WithNullCardRenderer_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<System.ArgumentNullException>(() => 
                new ArticleDisplayCoordinator(_mockEditorState.Object, null));
        }

        [Fact]
        public void UpdateArticleDisplay_WithNullContainer_DoesNotThrow()
        {
            // Act & Assert
            var exception = Record.Exception(() => 
                _coordinator.UpdateArticleDisplay(null, null));
            
            Assert.Null(exception);
        }

        [Fact]
        public void UpdateArticleDisplay_WithNoArticlesOnPage_ClearsContainer()
        {
            // Arrange
            var container = new StackPanel();
            container.Children.Add(new Border()); // Add a child to verify clearing
            
            _mockEditorState.Setup(s => s.CurrentPage).Returns(1);
            _mockEditorState.Setup(s => s.Articles).Returns(new List<ArticleLine>());

            // Act
            _coordinator.UpdateArticleDisplay(container, null);

            // Assert
            Assert.Empty(container.Children);
        }

        [Fact]
        public void UpdateArticleDisplay_WithArticlesOnPage_AddsCards()
        {
            // Arrange
            var container = new StackPanel();
            var article1 = new ArticleLine 
            { 
                Title = "Article 1", 
                Pages = new List<int> { 1, 2, 3 } 
            };
            var article2 = new ArticleLine 
            { 
                Title = "Article 2", 
                Pages = new List<int> { 1, 4, 5 } 
            };
            
            _mockEditorState.Setup(s => s.CurrentPage).Returns(1);
            _mockEditorState.Setup(s => s.Articles).Returns(new List<ArticleLine> { article1, article2 });
            
            _mockCardRenderer.Setup(r => r.CreateArticleCard(It.IsAny<ArticleLine>()))
                .Returns(new Border());

            // Act
            _coordinator.UpdateArticleDisplay(container, null);

            // Assert
            Assert.Equal(2, container.Children.Count);
            _mockCardRenderer.Verify(r => r.CreateArticleCard(article1), Times.Once);
            _mockCardRenderer.Verify(r => r.CreateArticleCard(article2), Times.Once);
        }

        [Fact]
        public void UpdateArticleDisplay_WithSelectedArticleOnPage_ShowsSelectedFirst()
        {
            // Arrange
            var container = new StackPanel();
            var article1 = new ArticleLine 
            { 
                Title = "Article 1", 
                Pages = new List<int> { 1, 2, 3 } 
            };
            var selectedArticle = new ArticleLine 
            { 
                Title = "Selected Article", 
                Pages = new List<int> { 1, 4, 5 } 
            };
            
            _mockEditorState.Setup(s => s.CurrentPage).Returns(1);
            _mockEditorState.Setup(s => s.Articles)
                .Returns(new List<ArticleLine> { article1, selectedArticle });
            
            var callOrder = new List<string>();
            _mockCardRenderer.Setup(r => r.CreateArticleCard(selectedArticle))
                .Callback(() => callOrder.Add("Selected"))
                .Returns(new Border());
            _mockCardRenderer.Setup(r => r.CreateArticleCard(article1))
                .Callback(() => callOrder.Add("Article1"))
                .Returns(new Border());

            // Act
            _coordinator.UpdateArticleDisplay(container, selectedArticle);

            // Assert
            Assert.Equal(2, callOrder.Count);
            Assert.Equal("Selected", callOrder[0]); // Selected article should be rendered first
            Assert.Equal("Article1", callOrder[1]);
        }

        [Fact]
        public void UpdateArticleDisplay_WithSelectedArticleNotOnPage_DoesNotShowSelected()
        {
            // Arrange
            var container = new StackPanel();
            var article1 = new ArticleLine 
            { 
                Title = "Article 1", 
                Pages = new List<int> { 1, 2, 3 } 
            };
            var selectedArticle = new ArticleLine 
            { 
                Title = "Selected Article", 
                Pages = new List<int> { 5, 6, 7 } // Not on page 1
            };
            
            _mockEditorState.Setup(s => s.CurrentPage).Returns(1);
            _mockEditorState.Setup(s => s.Articles)
                .Returns(new List<ArticleLine> { article1, selectedArticle });
            
            _mockCardRenderer.Setup(r => r.CreateArticleCard(article1))
                .Returns(new Border());

            // Act
            _coordinator.UpdateArticleDisplay(container, selectedArticle);

            // Assert
            Assert.Single(container.Children);
            _mockCardRenderer.Verify(r => r.CreateArticleCard(article1), Times.Once);
            _mockCardRenderer.Verify(r => r.CreateArticleCard(selectedArticle), Times.Never);
        }

        [Fact]
        public void UpdateArticleDisplay_WithNullCardFromRenderer_SkipsCard()
        {
            // Arrange
            var container = new StackPanel();
            var article1 = new ArticleLine 
            { 
                Title = "Article 1", 
                Pages = new List<int> { 1 } 
            };
            
            _mockEditorState.Setup(s => s.CurrentPage).Returns(1);
            _mockEditorState.Setup(s => s.Articles).Returns(new List<ArticleLine> { article1 });
            
            _mockCardRenderer.Setup(r => r.CreateArticleCard(article1))
                .Returns((Border)null); // Renderer returns null

            // Act
            _coordinator.UpdateArticleDisplay(container, null);

            // Assert
            Assert.Empty(container.Children); // Null cards should not be added
        }
    }
}

