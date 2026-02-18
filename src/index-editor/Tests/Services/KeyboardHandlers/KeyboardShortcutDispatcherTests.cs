using System;
using System.Collections.Generic;
using Avalonia.Input;
using IndexEditor.Services.KeyboardHandlers;
using Moq;
using Xunit;

namespace IndexEditor.Tests.Services.KeyboardHandlers;

/// <summary>
/// Unit tests for KeyboardShortcutDispatcher.
/// Tests handler priority ordering, event dispatching, and error handling.
/// </summary>
public class KeyboardShortcutDispatcherTests
{
    private class TestKeyboardHandler : IKeyboardShortcutHandler
    {
        public int Priority { get; set; }
        public bool ShouldHandle { get; set; }
        public bool WasCalled { get; private set; }
        public KeyEventArgs? LastEvent { get; private set; }

        public bool TryHandle(KeyEventArgs e)
        {
            WasCalled = true;
            LastEvent = e;
            return ShouldHandle;
        }

        public void Reset()
        {
            WasCalled = false;
            LastEvent = null;
        }
    }

    private class TestKeyEventArgs : KeyEventArgs
    {
        public TestKeyEventArgs(Key key)
        {
            Key = key;
            Handled = false;
        }

        public new Key Key { get; }
        public new bool Handled { get; set; }
    }

    private TestKeyEventArgs CreateKeyEvent(Key key = Key.A)
    {
        return new TestKeyEventArgs(key);
    }

    [Fact]
    public void Constructor_WithHandlers_CreatesDispatcher()
    {
        // Arrange
        var handler1 = new TestKeyboardHandler { Priority = 100 };
        var handler2 = new TestKeyboardHandler { Priority = 50 };
        var handlers = new List<IKeyboardShortcutHandler> { handler1, handler2 };

        // Act
        var dispatcher = new KeyboardShortcutDispatcher(handlers);

        // Assert
        Assert.NotNull(dispatcher);
    }

    [Fact]
    public void Constructor_WithEmptyList_CreatesDispatcher()
    {
        // Arrange
        var handlers = new List<IKeyboardShortcutHandler>();

        // Act
        var dispatcher = new KeyboardShortcutDispatcher(handlers);

        // Assert
        Assert.NotNull(dispatcher);
    }

    [Fact]
    public void Dispatch_InvokesPriorityOrderHighestFirst()
    {
        // Arrange
        var handler1 = new TestKeyboardHandler { Priority = 50, ShouldHandle = false };
        var handler2 = new TestKeyboardHandler { Priority = 100, ShouldHandle = false };
        var handler3 = new TestKeyboardHandler { Priority = 75, ShouldHandle = false };
        
        var handlers = new List<IKeyboardShortcutHandler> { handler1, handler2, handler3 };
        var dispatcher = new KeyboardShortcutDispatcher(handlers);
        var keyEvent = CreateKeyEvent();

        // Act
        dispatcher.Dispatch(keyEvent);

        // Assert - All should be called in priority order (100, 75, 50)
        Assert.True(handler2.WasCalled); // Priority 100
        Assert.True(handler3.WasCalled); // Priority 75
        Assert.True(handler1.WasCalled); // Priority 50
    }

    [Fact]
    public void Dispatch_WhenHandlerHandlesEvent_StopsDispatching()
    {
        // Arrange
        var handler1 = new TestKeyboardHandler { Priority = 100, ShouldHandle = false };
        var handler2 = new TestKeyboardHandler { Priority = 50, ShouldHandle = true }; // Handles it
        var handler3 = new TestKeyboardHandler { Priority = 25, ShouldHandle = false };
        
        var handlers = new List<IKeyboardShortcutHandler> { handler1, handler2, handler3 };
        var dispatcher = new KeyboardShortcutDispatcher(handlers);
        var keyEvent = CreateKeyEvent();

        // Act
        var result = dispatcher.Dispatch(keyEvent);

        // Assert
        Assert.True(result);
        Assert.True(handler1.WasCalled); // Called first (priority 100)
        Assert.True(handler2.WasCalled); // Called second, handled it (priority 50)
        Assert.False(handler3.WasCalled); // Should NOT be called (priority 25)
    }

    [Fact]
    public void Dispatch_WhenNoHandlerHandles_ReturnsFalse()
    {
        // Arrange
        var handler1 = new TestKeyboardHandler { Priority = 100, ShouldHandle = false };
        var handler2 = new TestKeyboardHandler { Priority = 50, ShouldHandle = false };
        
        var handlers = new List<IKeyboardShortcutHandler> { handler1, handler2 };
        var dispatcher = new KeyboardShortcutDispatcher(handlers);
        var keyEvent = CreateKeyEvent();

        // Act
        var result = dispatcher.Dispatch(keyEvent);

        // Assert
        Assert.False(result);
        Assert.True(handler1.WasCalled);
        Assert.True(handler2.WasCalled);
    }

    [Fact]
    public void Dispatch_WhenHandlerHandles_ReturnsTrue()
    {
        // Arrange
        var handler = new TestKeyboardHandler { Priority = 100, ShouldHandle = true };
        var handlers = new List<IKeyboardShortcutHandler> { handler };
        var dispatcher = new KeyboardShortcutDispatcher(handlers);
        var keyEvent = CreateKeyEvent();

        // Act
        var result = dispatcher.Dispatch(keyEvent);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Dispatch_PassesEventToHandlers()
    {
        // Arrange
        var handler = new TestKeyboardHandler { Priority = 100, ShouldHandle = false };
        var handlers = new List<IKeyboardShortcutHandler> { handler };
        var dispatcher = new KeyboardShortcutDispatcher(handlers);
        var keyEvent = CreateKeyEvent(Key.B);

        // Act
        dispatcher.Dispatch(keyEvent);

        // Assert
        Assert.NotNull(handler.LastEvent);
        Assert.Equal(Key.B, handler.LastEvent.Key);
    }

    [Fact]
    public void Dispatch_WithHandlerException_ContinuesToNextHandler()
    {
        // Arrange
        var mockHandler = new Mock<IKeyboardShortcutHandler>();
        mockHandler.Setup(h => h.Priority).Returns(100);
        mockHandler.Setup(h => h.TryHandle(It.IsAny<KeyEventArgs>()))
            .Throws(new Exception("Test exception"));

        var handler2 = new TestKeyboardHandler { Priority = 50, ShouldHandle = true };
        
        var handlers = new List<IKeyboardShortcutHandler> { mockHandler.Object, handler2 };
        var dispatcher = new KeyboardShortcutDispatcher(handlers);
        var keyEvent = CreateKeyEvent();

        // Act
        var result = dispatcher.Dispatch(keyEvent);

        // Assert - Despite exception, should continue and handler2 should be called
        Assert.True(result);
        Assert.True(handler2.WasCalled);
    }

    [Fact]
    public void Dispatch_WithMultipleHandlersSamePriority_InvokesAllBeforeMovingOn()
    {
        // Arrange
        var handler1 = new TestKeyboardHandler { Priority = 100, ShouldHandle = false };
        var handler2 = new TestKeyboardHandler { Priority = 100, ShouldHandle = false };
        var handler3 = new TestKeyboardHandler { Priority = 50, ShouldHandle = false };
        
        var handlers = new List<IKeyboardShortcutHandler> { handler1, handler2, handler3 };
        var dispatcher = new KeyboardShortcutDispatcher(handlers);
        var keyEvent = CreateKeyEvent();

        // Act
        dispatcher.Dispatch(keyEvent);

        // Assert - All should be invoked
        Assert.True(handler1.WasCalled);
        Assert.True(handler2.WasCalled);
        Assert.True(handler3.WasCalled);
    }

    [Fact]
    public void Dispatch_WithNoHandlers_ReturnsFalse()
    {
        // Arrange
        var handlers = new List<IKeyboardShortcutHandler>();
        var dispatcher = new KeyboardShortcutDispatcher(handlers);
        var keyEvent = CreateKeyEvent();

        // Act
        var result = dispatcher.Dispatch(keyEvent);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Dispatch_RespectsDescendingPriorityOrder()
    {
        // Arrange
        var callOrder = new List<int>();
        
        var mockHandler1 = new Mock<IKeyboardShortcutHandler>();
        mockHandler1.Setup(h => h.Priority).Returns(10);
        mockHandler1.Setup(h => h.TryHandle(It.IsAny<KeyEventArgs>()))
            .Callback(() => callOrder.Add(10))
            .Returns(false);

        var mockHandler2 = new Mock<IKeyboardShortcutHandler>();
        mockHandler2.Setup(h => h.Priority).Returns(100);
        mockHandler2.Setup(h => h.TryHandle(It.IsAny<KeyEventArgs>()))
            .Callback(() => callOrder.Add(100))
            .Returns(false);

        var mockHandler3 = new Mock<IKeyboardShortcutHandler>();
        mockHandler3.Setup(h => h.Priority).Returns(50);
        mockHandler3.Setup(h => h.TryHandle(It.IsAny<KeyEventArgs>()))
            .Callback(() => callOrder.Add(50))
            .Returns(false);
        
        var handlers = new List<IKeyboardShortcutHandler> 
        { 
            mockHandler1.Object, 
            mockHandler2.Object, 
            mockHandler3.Object 
        };
        var dispatcher = new KeyboardShortcutDispatcher(handlers);
        var keyEvent = CreateKeyEvent();

        // Act
        dispatcher.Dispatch(keyEvent);

        // Assert - Should be called in descending priority order: 100, 50, 10
        Assert.Equal(3, callOrder.Count);
        Assert.Equal(100, callOrder[0]);
        Assert.Equal(50, callOrder[1]);
        Assert.Equal(10, callOrder[2]);
    }
}




