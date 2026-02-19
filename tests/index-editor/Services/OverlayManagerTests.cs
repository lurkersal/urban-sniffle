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
    private Mock<IControlFinder> CreateMockControlFinder()
    {
        return new Mock<IControlFinder>();
    }

    private Border CreateBorder(bool isVisible = false)
    {
        var border = new Border { IsVisible = isVisible };
        return border;
    }

    // Using real TextBox since properties are settable
    private TextBox CreateTextBox()
    {
        return new TextBox();
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
        Assert.Throws<ArgumentNullException>(() => new OverlayManager((IControlFinder)null!));
    }

    [Fact]
    public void ShowIndexOverlay_WithValidFolder_SetsOverlayVisible()
    {
        // Arrange
        var mockControlFinder = CreateMockControlFinder();
        var mockOverlay = CreateBorder(false);
        var mockTextBox = CreateTextBox();

        mockControlFinder.Setup(cf => cf.FindControl<Border>("IndexOverlay")).Returns(mockOverlay);
        mockControlFinder.Setup(cf => cf.FindControl<TextBox>("IndexOverlayTextBox")).Returns(mockTextBox);

        var manager = new OverlayManager(mockControlFinder.Object);

        // Act
        manager.ShowIndexOverlay("/test/folder");

        // Assert
        Assert.True(mockOverlay.IsVisible);
    }

    [Fact]
    public void CloseIndexOverlay_WhenCalled_SetsOverlayHidden()
    {
        // Arrange
        var mockControlFinder = CreateMockControlFinder();
        var mockOverlay = CreateBorder(true);

        mockControlFinder.Setup(cf => cf.FindControl<Border>("IndexOverlay")).Returns(mockOverlay);

        var manager = new OverlayManager(mockControlFinder.Object);

        // Act
        manager.CloseIndexOverlay();

        // Assert
        Assert.False(mockOverlay.IsVisible);
    }

    [Fact]
    public void ToggleIndexOverlay_WhenHidden_ShowsOverlay()
    {
        // Arrange
        var mockControlFinder = CreateMockControlFinder();
        var mockOverlay = CreateBorder(false);
        var mockTextBox = CreateTextBox();

        mockControlFinder.Setup(cf => cf.FindControl<Border>("IndexOverlay")).Returns(mockOverlay);
        mockControlFinder.Setup(cf => cf.FindControl<TextBox>("IndexOverlayTextBox")).Returns(mockTextBox);

        var manager = new OverlayManager(mockControlFinder.Object);

        // Act
        manager.ToggleIndexOverlay("/test/folder");

        // Assert
        Assert.True(mockOverlay.IsVisible);
    }

    [Fact]
    public void ToggleIndexOverlay_WhenVisible_HidesOverlay()
    {
        // Arrange
        var mockControlFinder = CreateMockControlFinder();
        var mockOverlay = CreateBorder(true);

        mockControlFinder.Setup(cf => cf.FindControl<Border>("IndexOverlay")).Returns(mockOverlay);

        var manager = new OverlayManager(mockControlFinder.Object);

        // Act
        manager.ToggleIndexOverlay("/test/folder");

        // Assert
        Assert.False(mockOverlay.IsVisible);
    }

    [Fact]
    public void ShowHelpOverlay_WhenCalled_SetsHelpOverlayVisible()
    {
        // Arrange
        var mockControlFinder = CreateMockControlFinder();
        var mockOverlay = CreateBorder(false);

        mockControlFinder.Setup(cf => cf.FindControl<Border>("HelpOverlay")).Returns(mockOverlay);

        var manager = new OverlayManager(mockControlFinder.Object);

        // Act
        manager.ShowHelpOverlay();

        // Assert
        Assert.True(mockOverlay.IsVisible);
    }

    [Fact]
    public void CloseHelpOverlay_WhenCalled_SetsHelpOverlayHidden()
    {
        // Arrange
        var mockControlFinder = CreateMockControlFinder();
        var mockOverlay = CreateBorder(true);

        mockControlFinder.Setup(cf => cf.FindControl<Border>("HelpOverlay")).Returns(mockOverlay);

        var manager = new OverlayManager(mockControlFinder.Object);

        // Act
        manager.CloseHelpOverlay();

        // Assert
        Assert.False(mockOverlay.IsVisible);
    }

    [Fact]
    public void ToggleHelpOverlay_TogglesVisibility()
    {
        // Arrange
        var mockControlFinder = CreateMockControlFinder();
        var mockOverlay = CreateBorder(false);

        mockControlFinder.Setup(cf => cf.FindControl<Border>("HelpOverlay")).Returns(mockOverlay);

        var manager = new OverlayManager(mockControlFinder.Object);

        // Act & Assert - First toggle (show)
        manager.ToggleHelpOverlay();
        Assert.True(mockOverlay.IsVisible);

        // Act & Assert - Second toggle (hide)
        manager.ToggleHelpOverlay();
        Assert.False(mockOverlay.IsVisible);
    }

    [Fact]
    public void ShowDeleteConfirmation_WithArticleTitle_SetsOverlayVisibleAndTitle()
    {
        // Arrange
        var mockControlFinder = CreateMockControlFinder();
        var mockOverlay = CreateBorder(false);
        var mockTitleLabel = CreateTextBlock();

        mockControlFinder.Setup(cf => cf.FindControl<Border>("DeleteArticleConfirmOverlay")).Returns(mockOverlay);
        mockControlFinder.Setup(cf => cf.FindControl<TextBlock>("DeleteArticleTitleLabel")).Returns(mockTitleLabel);

        var manager = new OverlayManager(mockControlFinder.Object);

        // Act
        manager.ShowDeleteConfirmation("Test Article");

        // Assert
        Assert.True(mockOverlay.IsVisible);
        Assert.Equal("Test Article", mockTitleLabel.Text);
    }

    [Fact]
    public void CloseDeleteConfirmation_WhenCalled_SetsOverlayHidden()
    {
        // Arrange
        var mockControlFinder = CreateMockControlFinder();
        var mockOverlay = CreateBorder(true);

        mockControlFinder.Setup(cf => cf.FindControl<Border>("DeleteArticleConfirmOverlay")).Returns(mockOverlay);

        var manager = new OverlayManager(mockControlFinder.Object);

        // Act
        manager.CloseDeleteConfirmation();

        // Assert
        Assert.False(mockOverlay.IsVisible);
    }

    [Fact]
    public void IsIndexOverlayVisible_WhenVisible_ReturnsTrue()
    {
        // Arrange
        var mockControlFinder = CreateMockControlFinder();
        var mockOverlay = CreateBorder(true);

        mockControlFinder.Setup(cf => cf.FindControl<Border>("IndexOverlay")).Returns(mockOverlay);

        var manager = new OverlayManager(mockControlFinder.Object);

        // Act
        var result = manager.IsIndexOverlayVisible();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsIndexOverlayVisible_WhenHidden_ReturnsFalse()
    {
        // Arrange
        var mockControlFinder = CreateMockControlFinder();
        var mockOverlay = CreateBorder(false);

        mockControlFinder.Setup(cf => cf.FindControl<Border>("IndexOverlay")).Returns(mockOverlay);

        var manager = new OverlayManager(mockControlFinder.Object);

        // Act
        var result = manager.IsIndexOverlayVisible();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsHelpOverlayVisible_WhenVisible_ReturnsTrue()
    {
        // Arrange
        var mockControlFinder = CreateMockControlFinder();
        var mockOverlay = CreateBorder(true);

        mockControlFinder.Setup(cf => cf.FindControl<Border>("HelpOverlay")).Returns(mockOverlay);

        var manager = new OverlayManager(mockControlFinder.Object);

        // Act
        var result = manager.IsHelpOverlayVisible();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsDeleteConfirmationVisible_WhenHidden_ReturnsFalse()
    {
        // Arrange
        var mockControlFinder = CreateMockControlFinder();
        var mockOverlay = CreateBorder(false);

        mockControlFinder.Setup(cf => cf.FindControl<Border>("DeleteArticleConfirmOverlay")).Returns(mockOverlay);

        var manager = new OverlayManager(mockControlFinder.Object);

        // Act
        var result = manager.IsDeleteConfirmationVisible();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShowIndexOverlay_WithNullFolder_DisplaysNoFolderMessage()
    {
        // Arrange
        var mockControlFinder = CreateMockControlFinder();
        var mockOverlay = CreateBorder(false);
        var mockTextBox = CreateTextBox();

        mockControlFinder.Setup(cf => cf.FindControl<Border>("IndexOverlay")).Returns(mockOverlay);
        mockControlFinder.Setup(cf => cf.FindControl<TextBox>("IndexOverlayTextBox")).Returns(mockTextBox);

        var manager = new OverlayManager(mockControlFinder.Object);

        // Act
        manager.ShowIndexOverlay(null);

        // Assert
        Assert.Equal("No folder open.", mockTextBox.Text);
        Assert.True(mockOverlay.IsVisible);
    }

    [Fact]
    public void ShowIndexOverlay_ClearsErrorVisuals()
    {
        // Arrange
        var mockControlFinder = CreateMockControlFinder();
        var mockOverlay = CreateBorder(false);
        var mockTextBox = CreateTextBox();
        var mockErrorBorder = CreateBorder(true);
        var mockErrorLine = CreateTextBlock();
        mockErrorLine.Text = "Error text";

        mockControlFinder.Setup(cf => cf.FindControl<Border>("IndexOverlay")).Returns(mockOverlay);
        mockControlFinder.Setup(cf => cf.FindControl<TextBox>("IndexOverlayTextBox")).Returns(mockTextBox);
        mockControlFinder.Setup(cf => cf.FindControl<Border>("IndexOverlayErrorBorder")).Returns(mockErrorBorder);
        mockControlFinder.Setup(cf => cf.FindControl<TextBlock>("IndexOverlayErrorLine")).Returns(mockErrorLine);

        var manager = new OverlayManager(mockControlFinder.Object);

        // Act
        manager.ShowIndexOverlay("/test");

        // Assert
        Assert.False(mockErrorBorder.IsVisible);
        Assert.Equal(string.Empty, mockErrorLine.Text);
    }

    [Fact]
    public void CloseIndexOverlay_ClearsErrorVisuals()
    {
        // Arrange
        var mockControlFinder = CreateMockControlFinder();
        var mockOverlay = CreateBorder(true);
        var mockErrorBorder = CreateBorder(true);
        var mockErrorLine = CreateTextBlock();
        mockErrorLine.Text = "Error text";

        mockControlFinder.Setup(cf => cf.FindControl<Border>("IndexOverlay")).Returns(mockOverlay);
        mockControlFinder.Setup(cf => cf.FindControl<Border>("IndexOverlayErrorBorder")).Returns(mockErrorBorder);
        mockControlFinder.Setup(cf => cf.FindControl<TextBlock>("IndexOverlayErrorLine")).Returns(mockErrorLine);

        var manager = new OverlayManager(mockControlFinder.Object);

        // Act
        manager.CloseIndexOverlay();

        // Assert
        Assert.False(mockErrorBorder.IsVisible);
        Assert.Equal(string.Empty, mockErrorLine.Text);
    }

    [Fact]
    public void ShowIndexOverlayError_DisplaysErrorAndShowsOverlay()
    {
        // Arrange
        var mockControlFinder = CreateMockControlFinder();
        var mockOverlay = CreateBorder(false);
        var mockTextBox = CreateTextBox();
        var mockErrorBorder = CreateBorder(false);
        var mockErrorLine = CreateTextBlock();

        mockControlFinder.Setup(cf => cf.FindControl<Border>("IndexOverlay")).Returns(mockOverlay);
        mockControlFinder.Setup(cf => cf.FindControl<TextBox>("IndexOverlayTextBox")).Returns(mockTextBox);
        mockControlFinder.Setup(cf => cf.FindControl<Border>("IndexOverlayErrorBorder")).Returns(mockErrorBorder);
        mockControlFinder.Setup(cf => cf.FindControl<TextBlock>("IndexOverlayErrorLine")).Returns(mockErrorLine);

        var manager = new OverlayManager(mockControlFinder.Object);
        var errorLine = "  Bad line  ";
        var fullText = "Good line\nBad line\nAnother line";

        // Act
        manager.ShowIndexOverlayError(errorLine, fullText);

        // Assert
        Assert.True(mockOverlay.IsVisible);
        Assert.True(mockErrorBorder.IsVisible);
        Assert.Equal("Bad line", mockErrorLine.Text); // Trimmed
        Assert.Equal(fullText, mockTextBox.Text);
    }

    [Fact(Skip = "Requires Avalonia TextBox in visual tree for selection properties to work correctly")]
    public void ShowIndexOverlayError_SelectsErrorLineInTextBox()
    {
        // Arrange
        var mockControlFinder = CreateMockControlFinder();
        var mockOverlay = CreateBorder(false);
        var mockTextBox = CreateTextBox();
        var mockErrorBorder = CreateBorder(false);
        var mockErrorLine = CreateTextBlock();

        mockControlFinder.Setup(cf => cf.FindControl<Border>("IndexOverlay")).Returns(mockOverlay);
        mockControlFinder.Setup(cf => cf.FindControl<TextBox>("IndexOverlayTextBox")).Returns(mockTextBox);
        mockControlFinder.Setup(cf => cf.FindControl<Border>("IndexOverlayErrorBorder")).Returns(mockErrorBorder);
        mockControlFinder.Setup(cf => cf.FindControl<TextBlock>("IndexOverlayErrorLine")).Returns(mockErrorLine);

        var manager = new OverlayManager(mockControlFinder.Object);
        var errorLine = "Bad line";
        var fullText = "Good line\nBad line\nAnother line";

        // Act
        manager.ShowIndexOverlayError(errorLine, fullText);

        // Assert
        var expectedStart = fullText.IndexOf(errorLine);
        Assert.Equal(expectedStart, mockTextBox.SelectionStart);
        Assert.Equal(expectedStart + errorLine.Length, mockTextBox.SelectionEnd);
        Assert.Equal(expectedStart, mockTextBox.CaretIndex);
    }
}

