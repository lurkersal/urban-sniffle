using System;
using Avalonia.Controls;
using IndexEditor.Services;
using Moq;
using Xunit;

namespace IndexEditor.Tests.Services;

/// <summary>
/// Unit tests for OverlayManager service.
/// Tests overlay visibility management, state queries, and error handling.
/// </summary>
public class OverlayManagerTests
{
    private Mock<Window> CreateMockWindow()
    {
        var mockWindow = new Mock<Window>();
        return mockWindow;
    }

    private Mock<Border> CreateMockBorder(bool isVisible = false)
    {
        var mockBorder = new Mock<Border>();
        mockBorder.SetupProperty(b => b.IsVisible, isVisible);
        return mockBorder;
    }

    private Mock<TextBox> CreateMockTextBox()
    {
        var mockTextBox = new Mock<TextBox>();
        mockTextBox.SetupProperty(t => t.Text, string.Empty);
        mockTextBox.SetupProperty(t => t.SelectionStart, 0);
        mockTextBox.SetupProperty(t => t.SelectionEnd, 0);
        mockTextBox.SetupProperty(t => t.CaretIndex, 0);
        return mockTextBox;
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
        Assert.Throws<ArgumentNullException>(() => new OverlayManager(null!));
    }

    [Fact]
    public void ShowIndexOverlay_WithValidFolder_SetsOverlayVisible()
    {
        // Arrange
        var mockWindow = CreateMockWindow();
        var mockOverlay = CreateMockBorder(false);
        var mockTextBox = CreateMockTextBox();

        mockWindow.Setup(w => w.FindControl<Border>("IndexOverlay")).Returns(mockOverlay.Object);
        mockWindow.Setup(w => w.FindControl<TextBox>("IndexOverlayTextBox")).Returns(mockTextBox.Object);

        var manager = new OverlayManager(mockWindow.Object);

        // Act
        manager.ShowIndexOverlay("/test/folder");

        // Assert
        Assert.True(mockOverlay.Object.IsVisible);
    }

    [Fact]
    public void CloseIndexOverlay_WhenCalled_SetsOverlayHidden()
    {
        // Arrange
        var mockWindow = CreateMockWindow();
        var mockOverlay = CreateMockBorder(true);

        mockWindow.Setup(w => w.FindControl<Border>("IndexOverlay")).Returns(mockOverlay.Object);

        var manager = new OverlayManager(mockWindow.Object);

        // Act
        manager.CloseIndexOverlay();

        // Assert
        Assert.False(mockOverlay.Object.IsVisible);
    }

    [Fact]
    public void ToggleIndexOverlay_WhenHidden_ShowsOverlay()
    {
        // Arrange
        var mockWindow = CreateMockWindow();
        var mockOverlay = CreateMockBorder(false);
        var mockTextBox = CreateMockTextBox();

        mockWindow.Setup(w => w.FindControl<Border>("IndexOverlay")).Returns(mockOverlay.Object);
        mockWindow.Setup(w => w.FindControl<TextBox>("IndexOverlayTextBox")).Returns(mockTextBox.Object);

        var manager = new OverlayManager(mockWindow.Object);

        // Act
        manager.ToggleIndexOverlay("/test/folder");

        // Assert
        Assert.True(mockOverlay.Object.IsVisible);
    }

    [Fact]
    public void ToggleIndexOverlay_WhenVisible_HidesOverlay()
    {
        // Arrange
        var mockWindow = CreateMockWindow();
        var mockOverlay = CreateMockBorder(true);

        mockWindow.Setup(w => w.FindControl<Border>("IndexOverlay")).Returns(mockOverlay.Object);

        var manager = new OverlayManager(mockWindow.Object);

        // Act
        manager.ToggleIndexOverlay("/test/folder");

        // Assert
        Assert.False(mockOverlay.Object.IsVisible);
    }

    [Fact]
    public void ShowHelpOverlay_WhenCalled_SetsHelpOverlayVisible()
    {
        // Arrange
        var mockWindow = CreateMockWindow();
        var mockOverlay = CreateMockBorder(false);

        mockWindow.Setup(w => w.FindControl<Border>("HelpOverlay")).Returns(mockOverlay.Object);

        var manager = new OverlayManager(mockWindow.Object);

        // Act
        manager.ShowHelpOverlay();

        // Assert
        Assert.True(mockOverlay.Object.IsVisible);
    }

    [Fact]
    public void CloseHelpOverlay_WhenCalled_SetsHelpOverlayHidden()
    {
        // Arrange
        var mockWindow = CreateMockWindow();
        var mockOverlay = CreateMockBorder(true);

        mockWindow.Setup(w => w.FindControl<Border>("HelpOverlay")).Returns(mockOverlay.Object);

        var manager = new OverlayManager(mockWindow.Object);

        // Act
        manager.CloseHelpOverlay();

        // Assert
        Assert.False(mockOverlay.Object.IsVisible);
    }

    [Fact]
    public void ToggleHelpOverlay_TogglesVisibility()
    {
        // Arrange
        var mockWindow = CreateMockWindow();
        var mockOverlay = CreateMockBorder(false);

        mockWindow.Setup(w => w.FindControl<Border>("HelpOverlay")).Returns(mockOverlay.Object);

        var manager = new OverlayManager(mockWindow.Object);

        // Act & Assert - First toggle (show)
        manager.ToggleHelpOverlay();
        Assert.True(mockOverlay.Object.IsVisible);

        // Act & Assert - Second toggle (hide)
        manager.ToggleHelpOverlay();
        Assert.False(mockOverlay.Object.IsVisible);
    }

    [Fact]
    public void ShowDeleteConfirmation_WithArticleTitle_SetsOverlayVisibleAndTitle()
    {
        // Arrange
        var mockWindow = CreateMockWindow();
        var mockOverlay = CreateMockBorder(false);
        var mockTitleLabel = CreateMockTextBlock();

        mockWindow.Setup(w => w.FindControl<Border>("DeleteArticleConfirmOverlay")).Returns(mockOverlay.Object);
        mockWindow.Setup(w => w.FindControl<TextBlock>("DeleteArticleTitleLabel")).Returns(mockTitleLabel.Object);

        var manager = new OverlayManager(mockWindow.Object);

        // Act
        manager.ShowDeleteConfirmation("Test Article");

        // Assert
        Assert.True(mockOverlay.Object.IsVisible);
        Assert.Equal("Test Article", mockTitleLabel.Object.Text);
    }

    [Fact]
    public void CloseDeleteConfirmation_WhenCalled_SetsOverlayHidden()
    {
        // Arrange
        var mockWindow = CreateMockWindow();
        var mockOverlay = CreateMockBorder(true);

        mockWindow.Setup(w => w.FindControl<Border>("DeleteArticleConfirmOverlay")).Returns(mockOverlay.Object);

        var manager = new OverlayManager(mockWindow.Object);

        // Act
        manager.CloseDeleteConfirmation();

        // Assert
        Assert.False(mockOverlay.Object.IsVisible);
    }

    [Fact]
    public void IsIndexOverlayVisible_WhenVisible_ReturnsTrue()
    {
        // Arrange
        var mockWindow = CreateMockWindow();
        var mockOverlay = CreateMockBorder(true);

        mockWindow.Setup(w => w.FindControl<Border>("IndexOverlay")).Returns(mockOverlay.Object);

        var manager = new OverlayManager(mockWindow.Object);

        // Act
        var result = manager.IsIndexOverlayVisible();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsIndexOverlayVisible_WhenHidden_ReturnsFalse()
    {
        // Arrange
        var mockWindow = CreateMockWindow();
        var mockOverlay = CreateMockBorder(false);

        mockWindow.Setup(w => w.FindControl<Border>("IndexOverlay")).Returns(mockOverlay.Object);

        var manager = new OverlayManager(mockWindow.Object);

        // Act
        var result = manager.IsIndexOverlayVisible();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsHelpOverlayVisible_WhenVisible_ReturnsTrue()
    {
        // Arrange
        var mockWindow = CreateMockWindow();
        var mockOverlay = CreateMockBorder(true);

        mockWindow.Setup(w => w.FindControl<Border>("HelpOverlay")).Returns(mockOverlay.Object);

        var manager = new OverlayManager(mockWindow.Object);

        // Act
        var result = manager.IsHelpOverlayVisible();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsDeleteConfirmationVisible_WhenHidden_ReturnsFalse()
    {
        // Arrange
        var mockWindow = CreateMockWindow();
        var mockOverlay = CreateMockBorder(false);

        mockWindow.Setup(w => w.FindControl<Border>("DeleteArticleConfirmOverlay")).Returns(mockOverlay.Object);

        var manager = new OverlayManager(mockWindow.Object);

        // Act
        var result = manager.IsDeleteConfirmationVisible();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShowIndexOverlay_WithNullFolder_DisplaysNoFolderMessage()
    {
        // Arrange
        var mockWindow = CreateMockWindow();
        var mockOverlay = CreateMockBorder(false);
        var mockTextBox = CreateMockTextBox();

        mockWindow.Setup(w => w.FindControl<Border>("IndexOverlay")).Returns(mockOverlay.Object);
        mockWindow.Setup(w => w.FindControl<TextBox>("IndexOverlayTextBox")).Returns(mockTextBox.Object);

        var manager = new OverlayManager(mockWindow.Object);

        // Act
        manager.ShowIndexOverlay(null);

        // Assert
        Assert.Equal("No folder open.", mockTextBox.Object.Text);
        Assert.True(mockOverlay.Object.IsVisible);
    }

    [Fact]
    public void ShowIndexOverlay_ClearsErrorVisuals()
    {
        // Arrange
        var mockWindow = CreateMockWindow();
        var mockOverlay = CreateMockBorder(false);
        var mockTextBox = CreateMockTextBox();
        var mockErrorBorder = CreateMockBorder(true);
        var mockErrorLine = CreateMockTextBlock();
        mockErrorLine.Object.Text = "Error text";

        mockWindow.Setup(w => w.FindControl<Border>("IndexOverlay")).Returns(mockOverlay.Object);
        mockWindow.Setup(w => w.FindControl<TextBox>("IndexOverlayTextBox")).Returns(mockTextBox.Object);
        mockWindow.Setup(w => w.FindControl<Border>("IndexOverlayErrorBorder")).Returns(mockErrorBorder.Object);
        mockWindow.Setup(w => w.FindControl<TextBlock>("IndexOverlayErrorLine")).Returns(mockErrorLine.Object);

        var manager = new OverlayManager(mockWindow.Object);

        // Act
        manager.ShowIndexOverlay("/test");

        // Assert
        Assert.False(mockErrorBorder.Object.IsVisible);
        Assert.Equal(string.Empty, mockErrorLine.Object.Text);
    }

    [Fact]
    public void CloseIndexOverlay_ClearsErrorVisuals()
    {
        // Arrange
        var mockWindow = CreateMockWindow();
        var mockOverlay = CreateMockBorder(true);
        var mockErrorBorder = CreateMockBorder(true);
        var mockErrorLine = CreateMockTextBlock();
        mockErrorLine.Object.Text = "Error text";

        mockWindow.Setup(w => w.FindControl<Border>("IndexOverlay")).Returns(mockOverlay.Object);
        mockWindow.Setup(w => w.FindControl<Border>("IndexOverlayErrorBorder")).Returns(mockErrorBorder.Object);
        mockWindow.Setup(w => w.FindControl<TextBlock>("IndexOverlayErrorLine")).Returns(mockErrorLine.Object);

        var manager = new OverlayManager(mockWindow.Object);

        // Act
        manager.CloseIndexOverlay();

        // Assert
        Assert.False(mockErrorBorder.Object.IsVisible);
        Assert.Equal(string.Empty, mockErrorLine.Object.Text);
    }

    [Fact]
    public void ShowIndexOverlayError_DisplaysErrorAndShowsOverlay()
    {
        // Arrange
        var mockWindow = CreateMockWindow();
        var mockOverlay = CreateMockBorder(false);
        var mockTextBox = CreateMockTextBox();
        var mockErrorBorder = CreateMockBorder(false);
        var mockErrorLine = CreateMockTextBlock();

        mockWindow.Setup(w => w.FindControl<Border>("IndexOverlay")).Returns(mockOverlay.Object);
        mockWindow.Setup(w => w.FindControl<TextBox>("IndexOverlayTextBox")).Returns(mockTextBox.Object);
        mockWindow.Setup(w => w.FindControl<Border>("IndexOverlayErrorBorder")).Returns(mockErrorBorder.Object);
        mockWindow.Setup(w => w.FindControl<TextBlock>("IndexOverlayErrorLine")).Returns(mockErrorLine.Object);

        var manager = new OverlayManager(mockWindow.Object);
        var errorLine = "  Bad line  ";
        var fullText = "Good line\nBad line\nAnother line";

        // Act
        manager.ShowIndexOverlayError(errorLine, fullText);

        // Assert
        Assert.True(mockOverlay.Object.IsVisible);
        Assert.True(mockErrorBorder.Object.IsVisible);
        Assert.Equal("Bad line", mockErrorLine.Object.Text); // Trimmed
        Assert.Equal(fullText, mockTextBox.Object.Text);
    }

    [Fact]
    public void ShowIndexOverlayError_SelectsErrorLineInTextBox()
    {
        // Arrange
        var mockWindow = CreateMockWindow();
        var mockOverlay = CreateMockBorder(false);
        var mockTextBox = CreateMockTextBox();
        var mockErrorBorder = CreateMockBorder(false);
        var mockErrorLine = CreateMockTextBlock();

        mockWindow.Setup(w => w.FindControl<Border>("IndexOverlay")).Returns(mockOverlay.Object);
        mockWindow.Setup(w => w.FindControl<TextBox>("IndexOverlayTextBox")).Returns(mockTextBox.Object);
        mockWindow.Setup(w => w.FindControl<Border>("IndexOverlayErrorBorder")).Returns(mockErrorBorder.Object);
        mockWindow.Setup(w => w.FindControl<TextBlock>("IndexOverlayErrorLine")).Returns(mockErrorLine.Object);

        var manager = new OverlayManager(mockWindow.Object);
        var errorLine = "Bad line";
        var fullText = "Good line\nBad line\nAnother line";

        // Act
        manager.ShowIndexOverlayError(errorLine, fullText);

        // Assert
        var expectedStart = fullText.IndexOf(errorLine);
        Assert.Equal(expectedStart, mockTextBox.Object.SelectionStart);
        Assert.Equal(expectedStart + errorLine.Length, mockTextBox.Object.SelectionEnd);
        Assert.Equal(expectedStart, mockTextBox.Object.CaretIndex);
    }
}

