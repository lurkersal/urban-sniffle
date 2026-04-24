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
    private Mock<IWindowStateManager> CreateMockWindowStateManager()
    {
        var mock = new Mock<IWindowStateManager>();
        mock.SetupProperty(w => w.WindowState, Avalonia.Controls.WindowState.Normal);
        return mock;
    }

    private Mock<IControlFinder> CreateMockControlFinder()
    {
        return new Mock<IControlFinder>();
    }

    private Border CreateBorder(bool isVisible = false)
    {
        var border = new Border { IsVisible = isVisible };
        return border;
    }

    private Image CreateImage()
    {
        var image = new Image();
        return image;
    }

    private TextBlock CreateTextBlock()
    {
        var textBlock = new TextBlock { Text = string.Empty };
        return textBlock;
    }

    [Fact]
    public void Constructor_WithNullWindow_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new FullscreenImageService(null!, CreateMockControlFinder().Object));
    }

    [Fact]
    public void ShowFullscreen_WithValidImage_SetsOverlayVisible()
    {
        // Arrange
        var mockWindowState = CreateMockWindowStateManager();
        var mockControlFinder = CreateMockControlFinder();
        var mockOverlay = CreateBorder(false);
        var mockImage = CreateImage();
        var mockImageSource = new Mock<IImage>();

        mockControlFinder.Setup(cf => cf.FindControl<Border>("FullscreenImageOverlay")).Returns(mockOverlay);
        mockControlFinder.Setup(cf => cf.FindControl<Image>("FullscreenImage")).Returns(mockImage);

        var service = new FullscreenImageService(mockWindowState.Object, mockControlFinder.Object);

        // Act
        service.ShowFullscreen(mockImageSource.Object, 42);

        // Assert
        Assert.True(mockOverlay.IsVisible);
    }

    [Fact]
    public void ShowFullscreen_WithValidImage_SetsImageSource()
    {
        // Arrange
        var mockWindowState = CreateMockWindowStateManager();
        var mockControlFinder = CreateMockControlFinder();
        var mockOverlay = CreateBorder(false);
        var mockImage = CreateImage();
        var mockImageSource = new Mock<IImage>();

        mockControlFinder.Setup(cf => cf.FindControl<Border>("FullscreenImageOverlay")).Returns(mockOverlay);
        mockControlFinder.Setup(cf => cf.FindControl<Image>("FullscreenImage")).Returns(mockImage);

        var service = new FullscreenImageService(mockWindowState.Object, mockControlFinder.Object);

        // Act
        service.ShowFullscreen(mockImageSource.Object, 42);

        // Assert
        Assert.Equal(mockImageSource.Object, mockImage.Source);
    }

    [Fact]
    public void ShowFullscreen_WithPageNumber_SetsPageText()
    {
        // Arrange
        var mockWindowState = CreateMockWindowStateManager();
        var mockControlFinder = CreateMockControlFinder();
        var mockOverlay = CreateBorder(false);
        var mockImage = CreateImage();
        var mockPageText = CreateTextBlock();
        var mockImageSource = new Mock<IImage>();

        mockControlFinder.Setup(cf => cf.FindControl<Border>("FullscreenImageOverlay")).Returns(mockOverlay);
        mockControlFinder.Setup(cf => cf.FindControl<Image>("FullscreenImage")).Returns(mockImage);
        mockControlFinder.Setup(cf => cf.FindControl<TextBlock>("FullscreenPageNumber")).Returns(mockPageText);

        var service = new FullscreenImageService(mockWindowState.Object, mockControlFinder.Object);

        // Act
        service.ShowFullscreen(mockImageSource.Object, 42);

        // Assert
        Assert.Equal("Page 42", mockPageText.Text);
    }

    [Fact]
    public void ShowFullscreen_SetsWindowToFullscreen()
    {
        // Arrange
        var mockWindowState = CreateMockWindowStateManager();
        var mockControlFinder = CreateMockControlFinder();
        var mockOverlay = CreateBorder(false);
        var mockImage = CreateImage();
        var mockImageSource = new Mock<IImage>();

        mockControlFinder.Setup(cf => cf.FindControl<Border>("FullscreenImageOverlay")).Returns(mockOverlay);
        mockControlFinder.Setup(cf => cf.FindControl<Image>("FullscreenImage")).Returns(mockImage);

        var service = new FullscreenImageService(mockWindowState.Object, mockControlFinder.Object);

        // Act
        service.ShowFullscreen(mockImageSource.Object, 1);

        // Assert
        Assert.Equal(Avalonia.Controls.WindowState.FullScreen, mockWindowState.Object.WindowState);
    }

    [Fact]
    public void ShowFullscreen_WithMissingOverlay_DoesNotThrow()
    {
        // Arrange
        var mockWindowState = CreateMockWindowStateManager();
        var mockControlFinder = CreateMockControlFinder();
        mockControlFinder.Setup(cf => cf.FindControl<Border>("FullscreenImageOverlay")).Returns((Border?)null);

        var service = new FullscreenImageService(mockWindowState.Object, mockControlFinder.Object);
        var mockImageSource = new Mock<IImage>();

        // Act & Assert - Should not throw
        var exception = Record.Exception(() => service.ShowFullscreen(mockImageSource.Object, 1));
        Assert.Null(exception);
    }

    [Fact]
    public void ShowFullscreen_WithMissingImage_DoesNotThrow()
    {
        // Arrange
        var mockWindowState = CreateMockWindowStateManager();
        var mockControlFinder = CreateMockControlFinder();
        var mockOverlay = CreateBorder(false);
        mockControlFinder.Setup(cf => cf.FindControl<Border>("FullscreenImageOverlay")).Returns(mockOverlay);
        mockControlFinder.Setup(cf => cf.FindControl<Image>("FullscreenImage")).Returns((Image?)null);

        var service = new FullscreenImageService(mockWindowState.Object, mockControlFinder.Object);
        var mockImageSource = new Mock<IImage>();

        // Act & Assert - Should not throw
        var exception = Record.Exception(() => service.ShowFullscreen(mockImageSource.Object, 1));
        Assert.Null(exception);
    }

    [Fact]
    public void CloseFullscreen_HidesOverlay()
    {
        // Arrange
        var mockWindowState = CreateMockWindowStateManager();
        var mockControlFinder = CreateMockControlFinder();
        var mockOverlay = CreateBorder(true);

        mockControlFinder.Setup(cf => cf.FindControl<Border>("FullscreenImageOverlay")).Returns(mockOverlay);

        var service = new FullscreenImageService(mockWindowState.Object, mockControlFinder.Object);

        // Act
        service.CloseFullscreen();

        // Assert
        Assert.False(mockOverlay.IsVisible);
    }

    [Fact]
    public void CloseFullscreen_WhenFullscreen_RestoresNormalWindowState()
    {
        // Arrange
        var mockWindowState = CreateMockWindowStateManager();
        var mockControlFinder = CreateMockControlFinder();
        mockWindowState.Object.WindowState = Avalonia.Controls.WindowState.FullScreen;
        var mockOverlay = CreateBorder(true);

        mockControlFinder.Setup(cf => cf.FindControl<Border>("FullscreenImageOverlay")).Returns(mockOverlay);

        var service = new FullscreenImageService(mockWindowState.Object, mockControlFinder.Object);

        // Act
        service.CloseFullscreen();

        // Assert
        Assert.Equal(Avalonia.Controls.WindowState.Normal, mockWindowState.Object.WindowState);
    }

    [Fact]
    public void CloseFullscreen_WhenNotFullscreen_DoesNotChangeWindowState()
    {
        // Arrange
        var mockWindowState = CreateMockWindowStateManager();
        var mockControlFinder = CreateMockControlFinder();
        mockWindowState.Object.WindowState = Avalonia.Controls.WindowState.Maximized;
        var mockOverlay = CreateBorder(true);

        mockControlFinder.Setup(cf => cf.FindControl<Border>("FullscreenImageOverlay")).Returns(mockOverlay);

        var service = new FullscreenImageService(mockWindowState.Object, mockControlFinder.Object);

        // Act
        service.CloseFullscreen();

        // Assert
        Assert.Equal(Avalonia.Controls.WindowState.Maximized, mockWindowState.Object.WindowState);
    }

    [Fact]
    public void CloseFullscreen_WithMissingOverlay_DoesNotThrow()
    {
        // Arrange
        var mockWindowState = CreateMockWindowStateManager();
        var mockControlFinder = CreateMockControlFinder();
        mockControlFinder.Setup(cf => cf.FindControl<Border>("FullscreenImageOverlay")).Returns((Border?)null);

        var service = new FullscreenImageService(mockWindowState.Object, mockControlFinder.Object);

        // Act & Assert - Should not throw
        var exception = Record.Exception(() => service.CloseFullscreen());
        Assert.Null(exception);
    }

    [Fact]
    public void IsFullscreenVisible_WhenVisible_ReturnsTrue()
    {
        // Arrange
        var mockWindowState = CreateMockWindowStateManager();
        var mockControlFinder = CreateMockControlFinder();
        var mockOverlay = CreateBorder(true);

        mockControlFinder.Setup(cf => cf.FindControl<Border>("FullscreenImageOverlay")).Returns(mockOverlay);

        var service = new FullscreenImageService(mockWindowState.Object, mockControlFinder.Object);

        // Act
        var result = service.IsFullscreenVisible();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsFullscreenVisible_WhenHidden_ReturnsFalse()
    {
        // Arrange
        var mockWindowState = CreateMockWindowStateManager();
        var mockControlFinder = CreateMockControlFinder();
        var mockOverlay = CreateBorder(false);

        mockControlFinder.Setup(cf => cf.FindControl<Border>("FullscreenImageOverlay")).Returns(mockOverlay);

        var service = new FullscreenImageService(mockWindowState.Object, mockControlFinder.Object);

        // Act
        var result = service.IsFullscreenVisible();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsFullscreenVisible_WithMissingOverlay_ReturnsFalse()
    {
        // Arrange
        var mockWindowState = CreateMockWindowStateManager();
        var mockControlFinder = CreateMockControlFinder();
        mockControlFinder.Setup(cf => cf.FindControl<Border>("FullscreenImageOverlay")).Returns((Border?)null);

        var service = new FullscreenImageService(mockWindowState.Object, mockControlFinder.Object);

        // Act
        var result = service.IsFullscreenVisible();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShowFullscreen_ThenCloseFullscreen_TogglesCorrectly()
    {
        // Arrange
        var mockWindowState = CreateMockWindowStateManager();
        var mockControlFinder = CreateMockControlFinder();
        var mockOverlay = CreateBorder(false);
        var mockImage = CreateImage();
        var mockImageSource = new Mock<IImage>();

        mockControlFinder.Setup(cf => cf.FindControl<Border>("FullscreenImageOverlay")).Returns(mockOverlay);
        mockControlFinder.Setup(cf => cf.FindControl<Image>("FullscreenImage")).Returns(mockImage);

        var service = new FullscreenImageService(mockWindowState.Object, mockControlFinder.Object);

        // Act & Assert - Show
        service.ShowFullscreen(mockImageSource.Object, 1);
        Assert.True(service.IsFullscreenVisible());
        Assert.Equal(Avalonia.Controls.WindowState.FullScreen, mockWindowState.Object.WindowState);

        // Act & Assert - Close
        service.CloseFullscreen();
        Assert.False(service.IsFullscreenVisible());
        Assert.Equal(Avalonia.Controls.WindowState.Normal, mockWindowState.Object.WindowState);
    }
}

