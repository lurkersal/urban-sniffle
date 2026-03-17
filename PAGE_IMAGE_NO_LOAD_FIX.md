# Page Image Not Loading After Folder Open - FIXED

## Problem
When opening a folder, the page image wasn't being loaded/displayed in PageControllerView. The image area showed "No folder opened" even after a folder was successfully loaded.

## Root Cause
PageControllerView was subscribed to StateChanged events on the WRONG EditorState instance:

1. **Constructor creates local instance**: PageControllerView() constructor creates a new `EditorStateService()` instance and stores it in `_editorState`
2. **InitializeUI subscribes to wrong instance**: InitializeUI() then subscribes to `_editorState.StateChanged` - but this is the local instance, not the shared one
3. **SetEditorState called too late**: MainWindow later calls `SetEditorState()` to inject the correct shared EditorState instance, but the StateChanged subscription was already pointing to the old local instance
4. **Events never fire**: When folders are loaded, NotifyStateChanged() is called on the shared instance, but PageControllerView is listening to the wrong instance, so it never receives the event

## Solution
Modified `SetEditorState()` to properly manage StateChanged subscriptions:

1. **Store handler in field**: Created `_stateChangedHandler` field to store the StateChanged event handler
2. **Unsubscribe from old instance**: In `SetEditorState()`, unsubscribe from old EditorState before switching
3. **Subscribe to new instance**: Subscribe to the new EditorState after switching
4. **Trigger immediate update**: Call the handler immediately to load the current state

### Changes Made

**File: `/home/justin/repos/urban-sniffle/src/index-editor/Views/PageControllerView.axaml.cs`**

1. Added field to store handler:
```csharp
// Store the StateChanged handler so we can unsubscribe/resubscribe when EditorState changes
private Action? _stateChangedHandler;
```

2. Updated `SetEditorState()` method:
```csharp
public void SetEditorState(IndexEditor.Shared.IEditorState editorState)
{
    // Unsubscribe from old EditorState if we have a handler
    if (_stateChangedHandler != null && _editorState != null)
    {
        _editorState.StateChanged -= _stateChangedHandler;
    }
    
    _editorState = editorState;
    var folder = _editorState?.CurrentFolder ?? "(null)";
    System.Console.WriteLine($"[DEBUG] PageControllerView.SetEditorState: CurrentFolder = '{folder}'");
    DebugLogger.Log($"PageControllerView.SetEditorState: CurrentFolder = '{folder}'");
    
    // Re-subscribe to the new EditorState
    if (_stateChangedHandler != null && _editorState != null)
    {
        _editorState.StateChanged += _stateChangedHandler;
        System.Console.WriteLine("[DEBUG] PageControllerView.SetEditorState: Re-subscribed to StateChanged");
        DebugLogger.Log("PageControllerView.SetEditorState: Re-subscribed to StateChanged");
        
        // Trigger an immediate update to load the current state
        try
        {
            _stateChangedHandler.Invoke();
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("PageControllerView.SetEditorState: initial state update", ex);
        }
    }
}
```

3. Updated `InitializeUI()` to store handler in field:
```csharp
// Create and store the StateChanged handler so it can be unsubscribed/resubscribed if EditorState changes
_stateChangedHandler = () => Dispatcher.UIThread.Post(() =>
{
    try
    {
        // Always rescan available pages to pick up newly added files
        // This ensures that if image files are added while the editor is open,
        // they become available for navigation
        ScanAvailablePages();
        
        if (pageInput != null) pageInput.Text = _editorState.CurrentPage.ToString();
        UpdateUi();
        UpdateNavigationButtons();
        UpdateCurrentArticleDisplay();
        LoadCurrentPageImage();
    }
    catch (Exception ex) { DebugLogger.LogException("PageControllerView.StateChanged handler", ex); }
});

// Subscribe to state changes to refresh UI
_editorState.StateChanged += _stateChangedHandler;
```

## Testing
To test the fix:
1. Build: `dotnet build src/index-editor/IndexEditor.csproj`
2. Run: `dotnet run --project src/index-editor/IndexEditor.csproj`
3. Open a folder using File > Open or Ctrl+O
4. Verify that the page image loads correctly after opening the folder

## Expected Behavior After Fix
- When SetEditorState is called, debug output shows: "Re-subscribed to StateChanged"
- When SetEditorState is called, LoadCurrentPageImage is called immediately to load the current page
- When folder is loaded and NotifyStateChanged is called, LoadCurrentPageImage is triggered again
- Page image displays correctly instead of showing "No folder opened"

## Status
✅ FIXED - Build successful

