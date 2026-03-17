using Xunit;
using Moq;
using IndexEditor.Services;
using IndexEditor.Shared;
using System.Collections.Generic;

namespace IndexEditor.Tests.Services
{
    public class PageNavigationCoordinatorTests
    {
        private readonly Mock<IEditorState> _mockEditorState;
        private readonly Mock<IPageNavigationService> _mockPageNavigationService;
        private readonly PageNavigationCoordinator _coordinator;

        public PageNavigationCoordinatorTests()
        {
            _mockEditorState = new Mock<IEditorState>();
            _mockPageNavigationService = new Mock<IPageNavigationService>();
            _coordinator = new PageNavigationCoordinator(
                _mockEditorState.Object,
                _mockPageNavigationService.Object);
        }

        [Fact]
        public void Constructor_WithNullEditorState_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<System.ArgumentNullException>(() =>
                new PageNavigationCoordinator(null, _mockPageNavigationService.Object));
        }

        [Fact]
        public void Constructor_WithNullPageNavigationService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<System.ArgumentNullException>(() =>
                new PageNavigationCoordinator(_mockEditorState.Object, null));
        }

        [Fact]
        public void MoveToPreviousPage_WithNoFolder_DecrementsPage()
        {
            // Arrange
            _mockEditorState.Setup(s => s.CurrentFolder).Returns((string)null);
            _mockEditorState.Setup(s => s.CurrentPage).Returns(5);

            // Act
            var result = _coordinator.MoveToPreviousPage();

            // Assert
            Assert.Equal(4, result);
        }

        [Fact]
        public void MoveToPreviousPage_WithImagesDisabled_DecrementsPage()
        {
            // Arrange
            _mockEditorState.Setup(s => s.CurrentFolder).Returns("/test/folder");
            _mockEditorState.Setup(s => s.ShowImages).Returns(false);
            _mockEditorState.Setup(s => s.CurrentPage).Returns(10);

            // Act
            var result = _coordinator.MoveToPreviousPage();

            // Assert
            Assert.Equal(9, result);
        }

        [Fact]
        public void MoveToPreviousPage_AtPageOne_ReturnsOne()
        {
            // Arrange
            _mockEditorState.Setup(s => s.CurrentFolder).Returns((string)null);
            _mockEditorState.Setup(s => s.CurrentPage).Returns(1);

            // Act
            var result = _coordinator.MoveToPreviousPage();

            // Assert
            Assert.Equal(1, result); // Should not go below 1
        }

        [Fact]
        public void MoveToPreviousPage_WithScanning_UsesPreviousAvailablePage()
        {
            // Arrange
            _mockEditorState.Setup(s => s.CurrentFolder).Returns("/test/folder");
            _mockEditorState.Setup(s => s.ShowImages).Returns(true);
            _mockEditorState.Setup(s => s.CurrentPage).Returns(10);

            _mockPageNavigationService.Setup(s => s.ScanAvailablePages("/test/folder"));
            _mockPageNavigationService.Setup(s => s.GetPreviousPage(10)).Returns(8);

            // Act
            var result = _coordinator.MoveToPreviousPage();

            // Assert
            Assert.Equal(8, result);
            _mockPageNavigationService.Verify(s => s.ScanAvailablePages("/test/folder"), Times.Once);
        }

        [Fact]
        public void MoveToPreviousPage_WithNoPreviousPage_FindsClosestPreviousPage()
        {
            // Arrange
            _mockEditorState.Setup(s => s.CurrentFolder).Returns("/test/folder");
            _mockEditorState.Setup(s => s.ShowImages).Returns(true);
            _mockEditorState.Setup(s => s.CurrentPage).Returns(10);

            _mockPageNavigationService.Setup(s => s.GetPreviousPage(10)).Returns((int?)null);
            _mockPageNavigationService.Setup(s => s.AvailablePages)
                .Returns(new List<int> { 1, 3, 5, 7, 15, 20 });

            // Act
            var result = _coordinator.MoveToPreviousPage();

            // Assert
            Assert.Equal(7, result); // Closest page before 10
        }

        [Fact]
        public void MoveToNextPage_WithNoFolder_IncrementsPage()
        {
            // Arrange
            _mockEditorState.Setup(s => s.CurrentFolder).Returns((string)null);
            _mockEditorState.Setup(s => s.CurrentPage).Returns(5);

            // Act
            var result = _coordinator.MoveToNextPage();

            // Assert
            Assert.Equal(6, result);
        }

        [Fact]
        public void MoveToNextPage_WithImagesDisabled_IncrementsPage()
        {
            // Arrange
            _mockEditorState.Setup(s => s.CurrentFolder).Returns("/test/folder");
            _mockEditorState.Setup(s => s.ShowImages).Returns(false);
            _mockEditorState.Setup(s => s.CurrentPage).Returns(10);

            // Act
            var result = _coordinator.MoveToNextPage();

            // Assert
            Assert.Equal(11, result);
        }

        [Fact]
        public void MoveToNextPage_WithScanning_UsesNextAvailablePage()
        {
            // Arrange
            _mockEditorState.Setup(s => s.CurrentFolder).Returns("/test/folder");
            _mockEditorState.Setup(s => s.ShowImages).Returns(true);
            _mockEditorState.Setup(s => s.CurrentPage).Returns(5);

            _mockPageNavigationService.Setup(s => s.ScanAvailablePages("/test/folder"));
            _mockPageNavigationService.Setup(s => s.GetNextPage(5)).Returns(7);

            // Act
            var result = _coordinator.MoveToNextPage();

            // Assert
            Assert.Equal(7, result);
            _mockPageNavigationService.Verify(s => s.ScanAvailablePages("/test/folder"), Times.Once);
        }

        [Fact]
        public void MoveToNextPage_WithNoNextPage_FindsClosestNextPage()
        {
            // Arrange
            _mockEditorState.Setup(s => s.CurrentFolder).Returns("/test/folder");
            _mockEditorState.Setup(s => s.ShowImages).Returns(true);
            _mockEditorState.Setup(s => s.CurrentPage).Returns(10);

            _mockPageNavigationService.Setup(s => s.GetNextPage(10)).Returns((int?)null);
            _mockPageNavigationService.Setup(s => s.AvailablePages)
                .Returns(new List<int> { 1, 3, 5, 7, 15, 20 });

            // Act
            var result = _coordinator.MoveToNextPage();

            // Assert
            Assert.Equal(15, result); // Closest page after 10
        }

        [Fact]
        public void MoveToNextPage_WithNoClosestNext_ReturnsNull()
        {
            // Arrange
            _mockEditorState.Setup(s => s.CurrentFolder).Returns("/test/folder");
            _mockEditorState.Setup(s => s.ShowImages).Returns(true);
            _mockEditorState.Setup(s => s.CurrentPage).Returns(100);

            _mockPageNavigationService.Setup(s => s.GetNextPage(100)).Returns((int?)null);
            _mockPageNavigationService.Setup(s => s.AvailablePages)
                .Returns(new List<int> { 1, 3, 5, 7 }); // All pages before current

            // Act
            var result = _coordinator.MoveToNextPage();

            // Assert
            Assert.Null(result); // No pages available after current page
        }
    }
}

