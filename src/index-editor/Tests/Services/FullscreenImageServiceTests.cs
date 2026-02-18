using System;
using Avalonia.Controls;
using Avalonia.Media;
using IndexEditor.Services;
using Moq;
using Xunit;

namespace IndexEditor.Tests.Services;

/// <summary>
/// Unit tests for FullscreenImageService.
/// Tests fullscreen image display, window state management, and visibility tracking.
/// </summary>
public class FullscreenImageServiceTests
{
    private Mock<Window> CreateMockWindow()
    {
        var mockWindow = new Mock<Window>();
        mockWindow.SetupProperty(w => w.WindowState, Avalonia.Controls.WindowState.Normal);
        return mockWindow;
    }

    private Mock<Border> CreateMockBorder(bool isVisible = false)
    {
        var mockBorder = new Mock<Border>();
        mockBorder.SetupProperty(b => b.IsVisible, isVisible);
        return mockBorder;
    }

    private Mock<Image> CreateMockImage()
    {
        var mockImage = new Mock<Image>();
        mockImage.SetupProperty(i => i.Source);
        return mockImage;
    }

    private Mock<TextBlock> CreateMockTextBlock()
    {
        var mockTextBlock = new Mock<TextBlock>();
        mockTextBlock.SetupProperty(t => t.Text, string.Empty);
        return mockTextBlock;
    }

    [Fact]
    public void Constructor_WithNullWindow_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new FullscreenImageService(null!));
    }

    [Fact]
    public void ShowFullscreen_WithValidImage_SetsOverlayVisible()
    {
        // Arrange
        var mockWindow = CreateMockWindow();
        var mockOverlay = CreateMockBorder(false);
        var mockImage = CreateMockImage();
        var mockImageSource = new Mock<IImage>();

        mockWindow.Setup(w => w.FindControl<Border>("FullscreenImageOverlay")).Returns(mockOverlay.Object);
        mockWindow.Setup(w => w.FindControl<Image>("FullscreenImage")).Returns(mockImage.Object);

        var service = new FullscreenImageService(mockWindow.Object);

        // Act
        service.ShowFullscreen(mockImageSource.Object, 42);

        // Assert
        Assert.True(mockOverlay.Object.IsVisible);
    }

    [Fact]
    public void ShowFullscreen_WithValidImage_SetsImageSource()
    {
        // Arrange
        var mockWindow = CreateMockWindow();
        var mockOverlay = CreateMockBorder(false);
        var mockImage = CreateMockImage();
        var mockImageSource = new Mock<IImage>();

        mockWindow.Setup(w => w.FindControl<Border>("FullscreenImageOverlay")).Returns(mockOverlay.Object);
        mockWindow.Setup(w => w.FindControl<Image>("FullscreenImage")).Returns(mockImage.Object);

        var service = new FullscreenImageService(mockWindow.Object);

        // Act
        service.ShowFullscreen(mockImageSource.Object, 42);

        // Assert
        Assert.Equal(mockImageSource.Object, mockImage.Object.Source);
    }

    [Fact]
    public void ShowFullscreen_WithPageNumber_SetsPageText()
    {
        // Arrange
        var mockWindow = CreateMockWindow();
        var mockOverlay = CreateMockBorder(false);
        var mockImage = CreateMockImage();
        var mockPageText = CreateMockTextBlock();
        var mockImageSource = new Mock<IImage>();

        mockWindow.Setup(w => w.FindControl<Border>("FullscreenImageOverlay")).Returns(mockOverlay.Object);
        mockWindow.Setup(w => w.FindControl<Image>("FullscreenImage")).Returns(mockImage.Object);
        mockWindow.Setup(w => w.FindControl<TextBlock>("FullscreenPageNumber")).Returns(mockPageText.Object);

        var service = new FullscreenImageService(mockWindow.Object);

        // Act
        service.ShowFullscreen(mockImageSource.Object, 42);

        // Assert
        Assert.Equal("Page 42", mockPageText.Object.Text);
    }

    [Fact]
    public void ShowFullscreen_SetsWindowToFullscreen()
    {
        // Arrange
        var mockWindow = CreateMockWindow();
        var mockOverlay = CreateMockBorder(false);
        var mockImage = CreateMockImage();
        var mockImageSource = new Mock<IImage>();

        mockWindow.Setup(w => w.FindControl<Border>("FullscreenImageOverlay")).Returns(mockOverlay.Object);
        mockWindow.Setup(w => w.FindControl<Image>("FullscreenImage")).Returns(mockImage.Object);

        var service = new FullscreenImageService(mockWindow.Object);

        // Act
        service.ShowFullscreen(mockImageSource.Object, 1);

        // Assert
        Assert.Equal(Avalonia.Controls.WindowState.FullScreen, mockWindow.Object.WindowState);
    }

    [Fact]
    public void ShowFullscreen_WithMissingOverlay_DoesNotThrow()
    {
        // Arrange
        var mockWindow = CreateMockWindow();
        mockWindow.Setup(w => w.FindControl<Border>("FullscreenImageOverlay")).Returns((Border?)null);

        var service = new FullscreenImageService(mockWindow.Object);
        var mockImageSource = new Mock<IImage>();

        // Act & Assert - Should not throw
        var exception = Record.Exception(() => service.ShowFullscreen(mockImageSource.Object, 1));
        Assert.Null(exception);
    }

    [Fact]
    public void ShowFullscreen_WithMissingImage_DoesNotThrow()
    {
        // Arrange
        var mockWindow = CreateMockWindow();
        var mockOverlay = CreateMockBorder(false);
        mockWindow.Setup(w => w.FindControl<Border>("FullscreenImageOverlay")).Returns(mockOverlay.Object);
        mockWindow.Setup(w => w.FindControl<Image>("FullscreenImage")).Returns((Image?)null);

        var service = new FullscreenImageService(mockWindow.Object);
        var mockImageSource = new Mock<IImage>();

        // Act & Assert - Should not throw
        var exception = Record.Exception(() => service.ShowFullscreen(mockImageSource.Object, 1));
        Assert.Null(exception);
    }

    [Fact]
    public void CloseFullscreen_HidesOverlay()
    {
        // Arrange
        var mockWindow = CreateMockWindow();
        var mockOverlay = CreateMockBorder(true);

        mockWindow.Setup(w => w.FindControl<Border>("FullscreenImageOverlay")).Returns(mockOverlay.Object);

        var service = new FullscreenImageService(mockWindow.Object);

        // Act
        service.CloseFullscreen();

        // Assert
        Assert.False(mockOverlay.Object.IsVisible);
    }

    [Fact]
    public void CloseFullscreen_WhenFullscreen_RestoresNormalWindowState()
    {
        // Arrange
        var mockWindow = CreateMockWindow();
        mockWindow.Object.WindowState = Avalonia.Controls.WindowState.FullScreen;
        var mockOverlay = CreateMockBorder(true);

        mockWindow.Setup(w => w.FindControl<Border>("FullscreenImageOverlay")).Returns(mockOverlay.Object);

        var service = new FullscreenImageService(mockWindow.Object);

        // Act
        service.CloseFullscreen();

        // Assert
        Assert.Equal(Avalonia.Controls.WindowState.Normal, mockWindow.Object.WindowState);
    }

    [Fact]
    public void CloseFullscreen_WhenNotFullscreen_DoesNotChangeWindowState()
    {
        // Arrange
        var mockWindow = CreateMockWindow();
        mockWindow.Object.WindowState = Avalonia.Controls.WindowState.Maximized;
        var mockOverlay = CreateMockBorder(true);

        mockWindow.Setup(w => w.FindControl<Border>("FullscreenImageOverlay")).Returns(mockOverlay.Object);

        var service = new FullscreenImageService(mockWindow.Object);

        // Act
        service.CloseFullscreen();

        // Assert
        Assert.Equal(Avalonia.Controls.WindowState.Maximized, mockWindow.Object.WindowState);
    }

    [Fact]
    public void CloseFullscreen_WithMissingOverlay_DoesNotThrow()
    {
        // Arrange
        var mockWindow = CreateMockWindow();
        mockWindow.Setup(w => w.FindControl<Border>("FullscreenImageOverlay")).Returns((Border?)null);

        var service = new FullscreenImageService(mockWindow.Object);

        // Act & Assert - Should not throw
        var exception = Record.Exception(() => service.CloseFullscreen());
        Assert.Null(exception);
    }

    [Fact]
    public void IsFullscreenVisible_WhenVisible_ReturnsTrue()
    {
        // Arrange
        var mockWindow = CreateMockWindow();
        var mockOverlay = CreateMockBorder(true);

        mockWindow.Setup(w => w.FindControl<Border>("FullscreenImageOverlay")).Returns(mockOverlay.Object);

        var service = new FullscreenImageService(mockWindow.Object);

        // Act
        var result = service.IsFullscreenVisible();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsFullscreenVisible_WhenHidden_ReturnsFalse()
    {
        // Arrange
        var mockWindow = CreateMockWindow();
        var mockOverlay = CreateMockBorder(false);

        mockWindow.Setup(w => w.FindControl<Border>("FullscreenImageOverlay")).Returns(mockOverlay.Object);

        var service = new FullscreenImageService(mockWindow.Object);

        // Act
        var result = service.IsFullscreenVisible();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsFullscreenVisible_WithMissingOverlay_ReturnsFalse()
    {
        // Arrange
        var mockWindow = CreateMockWindow();
        mockWindow.Setup(w => w.FindControl<Border>("FullscreenImageOverlay")).Returns((Border?)null);

        var service = new FullscreenImageService(mockWindow.Object);

        // Act
        var result = service.IsFullscreenVisible();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShowFullscreen_ThenCloseFullscreen_TogglesCorrectly()
    {
        // Arrange
        var mockWindow = CreateMockWindow();
        var mockOverlay = CreateMockBorder(false);
        var mockImage = CreateMockImage();
        var mockImageSource = new Mock<IImage>();

        mockWindow.Setup(w => w.FindControl<Border>("FullscreenImageOverlay")).Returns(mockOverlay.Object);
        mockWindow.Setup(w => w.FindControl<Image>("FullscreenImage")).Returns(mockImage.Object);

        var service = new FullscreenImageService(mockWindow.Object);

        // Act & Assert - Show
        service.ShowFullscreen(mockImageSource.Object, 1);
        Assert.True(service.IsFullscreenVisible());
        Assert.Equal(Avalonia.Controls.WindowState.FullScreen, mockWindow.Object.WindowState);

        // Act & Assert - Close
        service.CloseFullscreen();
        Assert.False(service.IsFullscreenVisible());
        Assert.Equal(Avalonia.Controls.WindowState.Normal, mockWindow.Object.WindowState);
    }
}

