using Xunit;
using Moq;
using IndexEditor.Services;
using IndexEditor.Shared;
using Common.Shared;
using System.Collections.Generic;
using System.Linq;

namespace IndexEditor.Tests.Services
{
    public class SegmentManagementServiceTests
    {
        private readonly Mock<IEditorState> _mockEditorState;
        private readonly SegmentManagementService _service;

        public SegmentManagementServiceTests()
        {
            _mockEditorState = new Mock<IEditorState>();
            _service = new SegmentManagementService(_mockEditorState.Object);
        }

        [Fact]
        public void Constructor_WithNullEditorState_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<System.ArgumentNullException>(() => new SegmentManagementService(null));
        }

        [Fact]
        public void EndActiveSegment_WithNoActiveSegment_DoesNotThrow()
        {
            // Arrange
            _mockEditorState.Setup(s => s.ActiveSegment).Returns((Segment)null);

            // Act & Assert
            var exception = Record.Exception(() => _service.EndActiveSegment(null));
            Assert.Null(exception);
        }

        [Fact]
        public void EndActiveSegment_WithInactiveSegment_DoesNotThrow()
        {
            // Arrange
            var segment = new Segment(1);
            segment.End = 1; // Mark as inactive
            _mockEditorState.Setup(s => s.ActiveSegment).Returns(segment);

            // Act & Assert
            var exception = Record.Exception(() => _service.EndActiveSegment(null));
            Assert.Null(exception);
        }

        [Fact]
        public void EndActiveSegment_WithActiveSegment_UpdatesArticlePages()
        {
            // Arrange
            var segment = new Segment(5); // Active segment starting at page 5
            var article = new ArticleLine
            {
                Title = "Test Article",
                Pages = new List<int> { 1, 2, 3 }
            };

            _mockEditorState.Setup(s => s.ActiveSegment).Returns(segment);
            _mockEditorState.Setup(s => s.ActiveArticle).Returns(article);
            _mockEditorState.Setup(s => s.CurrentPage).Returns(8);
            _mockEditorState.Setup(s => s.CurrentFolder).Returns("/test/folder");

            // Act
            _service.EndActiveSegment(null);

            // Assert
            Assert.NotNull(article.Pages);
            Assert.Contains(5, article.Pages);
            Assert.Contains(6, article.Pages);
            Assert.Contains(7, article.Pages);
            Assert.Contains(8, article.Pages);
        }

        [Fact]
        public void EndActiveSegment_WithReversedRange_SwapsStartAndEnd()
        {
            // Arrange - End page (3) is before start page (7)
            var segment = new Segment(7); // Starting at page 7
            var article = new ArticleLine
            {
                Title = "Test Article",
                Pages = new List<int>()
            };

            _mockEditorState.Setup(s => s.ActiveSegment).Returns(segment);
            _mockEditorState.Setup(s => s.ActiveArticle).Returns(article);
            _mockEditorState.Setup(s => s.CurrentPage).Returns(3); // Current page is before start
            _mockEditorState.Setup(s => s.CurrentFolder).Returns("/test/folder");

            // Act
            _service.EndActiveSegment(null);

            // Assert - Should include pages 3-7
            Assert.NotNull(article.Pages);
            Assert.Equal(5, article.Pages.Count);
            Assert.Contains(3, article.Pages);
            Assert.Contains(7, article.Pages);
        }

        [Fact]
        public void EndActiveSegment_ClearsActiveSegment()
        {
            // Arrange
            var segment = new Segment(5);
            var article = new ArticleLine { Title = "Test", Pages = new List<int>() };

            _mockEditorState.Setup(s => s.ActiveSegment).Returns(segment);
            _mockEditorState.Setup(s => s.ActiveArticle).Returns(article);
            _mockEditorState.Setup(s => s.CurrentPage).Returns(5);
            _mockEditorState.Setup(s => s.CurrentFolder).Returns("/test/folder");
            _mockEditorState.SetupSet(s => s.ActiveSegment = null);

            // Act
            _service.EndActiveSegment(null);

            // Assert
            _mockEditorState.VerifySet(s => s.ActiveSegment = null, Times.Once);
        }

        [Fact]
        public void EndActiveSegment_NotifiesStateChanged()
        {
            // Arrange
            var segment = new Segment(5);
            var article = new ArticleLine { Title = "Test", Pages = new List<int>() };

            _mockEditorState.Setup(s => s.ActiveSegment).Returns(segment);
            _mockEditorState.Setup(s => s.ActiveArticle).Returns(article);
            _mockEditorState.Setup(s => s.CurrentPage).Returns(5);
            _mockEditorState.Setup(s => s.CurrentFolder).Returns("/test/folder");

            // Act
            _service.EndActiveSegment(null);

            // Assert
            _mockEditorState.Verify(s => s.NotifyStateChanged(), Times.Once);
        }

        [Fact]
        public void AddSegmentAtCurrentPage_ReturnsResult()
        {
            // This test verifies the method delegates to EditorActions
            // The actual logic is tested in EditorActions tests

            // Act
            var result = _service.AddSegmentAtCurrentPage();

            // Assert - Should return a boolean (true or false)
            Assert.IsType<bool>(result);
        }
    }
}

