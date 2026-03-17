using Xunit;
using Moq;
using IndexEditor.Services;
using IndexEditor.Shared;
using Common.Shared;
using System.Collections.Generic;
namespace IndexEditor.Tests.Services
{
    /// <summary>
    /// Tests for ArticleFocusManager service.
    /// Note: Some methods require Avalonia platform initialization (Window creation)
    /// which is not practical in unit tests. Those are tested via integration tests.
    /// </summary>
    public class ArticleFocusManagerTests
    {
        private readonly Mock<IEditorState> _mockEditorState;
        private readonly ArticleFocusManager _focusManager;
        public ArticleFocusManagerTests()
        {
            _mockEditorState = new Mock<IEditorState>();
            _focusManager = new ArticleFocusManager(_mockEditorState.Object);
        }
        [Fact]
        public void Constructor_WithNullEditorState_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<System.ArgumentNullException>(() => new ArticleFocusManager(null));
        }
        [Fact]
        public void SetupFocusForNewArticle_WithNullArticle_DoesNotThrow()
        {
            // Act & Assert - passing null visualRoot avoids Window creation
            var exception = Record.Exception(() => 
                _focusManager.SetupFocusForNewArticle(null, null, null));
            Assert.Null(exception);
        }
        [Fact]
        public void SetupFocusForNewArticle_WithNullVisualRoot_DoesNotThrow()
        {
            // Arrange
            var article = new ArticleLine { Title = "Test" };
            // Act & Assert
            var exception = Record.Exception(() => 
                _focusManager.SetupFocusForNewArticle(article, null, null));
            Assert.Null(exception);
        }
        [Fact]
        public void SetupFocusForNewArticle_WithNonVisualRoot_DoesNotThrow()
        {
            // Arrange
            var article = new ArticleLine { Title = "Test" };
            var notAVisual = new object();
            // Act & Assert
            var exception = Record.Exception(() => 
                _focusManager.SetupFocusForNewArticle(article, notAVisual, null));
            Assert.Null(exception);
        }
        [Fact]
        public void SetupFocusForNewArticle_WithValidArticleButNullVisual_DoesNotCallFocus()
        {
            // Arrange
            var article = new ArticleLine { Title = "Test", Pages = new List<int> { 1 } };
            _mockEditorState.Setup(s => s.RequestArticleEditorFocus());
            // Act
            _focusManager.SetupFocusForNewArticle(article, null, null);
            // Assert - should NOT call focus request if visualRoot is null
            _mockEditorState.Verify(s => s.RequestArticleEditorFocus(), Times.Never);
        }
        // Note: Tests that require actual Window creation are skipped in unit tests
        // and should be covered by integration/UI tests instead.
    }
}
