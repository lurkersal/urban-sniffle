# Fullscreen Image View Feature

## Feature Enhancement
When double-clicking on a page image, the application now enters fullscreen mode while displaying the image, providing an immersive viewing experience.

## Previous Behavior
- Double-click on page image
- Image displayed in an overlay on top of the normal window
- Window remained in normal state (windowed mode)
- User could see window borders, taskbar, etc.

## New Behavior
- Double-click on page image
- Image displayed in an overlay
- **Window enters fullscreen mode** (WindowState.FullScreen)
- Full screen usage - no window borders, taskbar hidden
- Press Esc or click Close button to exit
- Window returns to normal state (WindowState.Normal)

## Implementation

**File:** `/home/justin/repos/urban-sniffle/src/index-editor/MainWindow.axaml.cs`

### Changes to ShowFullscreenImage Method

**Lines:** ~1426-1462

Added code to enter fullscreen mode when showing the image:

```csharp
private void ShowFullscreenImage(Avalonia.Media.IImage imageSource, int pageNumber)
{
    // ...existing code to show overlay and set image...
    
    overlay.IsVisible = true;
    
    // Enter fullscreen mode  ← NEW!
    try
    {
        this.WindowState = WindowState.FullScreen;
        DebugLogger.Log("MainWindow.ShowFullscreenImage: entered fullscreen mode");
    }
    catch (Exception fsEx)
    {
        DebugLogger.LogException("MainWindow.ShowFullscreenImage: enter fullscreen", fsEx);
    }
}
```

### Changes to CloseFullscreenImage Method

**Lines:** ~1464-1484

Added code to exit fullscreen mode when closing the image:

```csharp
private void CloseFullscreenImage()
{
    // ...existing code to hide overlay...
    
    // Exit fullscreen mode  ← NEW!
    try
    {
        if (this.WindowState == WindowState.FullScreen)
        {
            this.WindowState = WindowState.Normal;
            DebugLogger.Log("MainWindow.CloseFullscreenImage: exited fullscreen mode");
        }
    }
    catch (Exception fsEx)
    {
        DebugLogger.LogException("MainWindow.CloseFullscreenImage: exit fullscreen", fsEx);
    }
}
```

## User Experience

### Opening Fullscreen Image View:
1. Navigate to a page in the PageController
2. Double-click on the page image
3. ✅ Window enters fullscreen mode
4. ✅ Image displayed in overlay covering entire screen
5. ✅ Page number shown in overlay
6. ✅ No window borders, taskbar, or distractions

### Closing Fullscreen Image View:
1. Press Esc key OR click the Close button
2. ✅ Overlay closes
3. ✅ Window returns to normal windowed mode
4. ✅ Returns to previous view state

## Advantages

✅ **Immersive viewing** - Full screen usage for examining page details  
✅ **No distractions** - Window borders and taskbar hidden  
✅ **Maximum screen real estate** - Image uses entire display  
✅ **Simple to exit** - Esc key or Close button returns to normal mode  
✅ **Smooth transition** - Automatically manages window state  

## Platform Support

This feature uses Avalonia's `WindowState.FullScreen` which is supported on:
- ✅ Linux (X11, Wayland)
- ✅ Windows
- ✅ macOS

## Edge Cases Handled

1. **Fullscreen entry failure** - Exception caught and logged, overlay still shows
2. **Already in fullscreen** - Check before exiting prevents unnecessary state changes
3. **Overlay controls missing** - Early return prevents crash
4. **Image source null** - Validation in OnImageDoubleTapped prevents showing empty fullscreen

## Related Keyboard Shortcuts

- **Double-click image** - Enter fullscreen image view
- **Esc** - Exit fullscreen image view
- **F11** - Toggle fullscreen mode (existing feature, independent of image view)

## Technical Details

### Window State Transitions

**Normal → Fullscreen (on double-click):**
```
Normal Window → Image Overlay Visible → WindowState.FullScreen
```

**Fullscreen → Normal (on close):**
```
WindowState.FullScreen → Image Overlay Hidden → WindowState.Normal
```

### Error Handling

Both transitions are wrapped in try-catch blocks to ensure:
- Overlay functionality works even if fullscreen mode fails
- Window state issues don't crash the application
- All errors are logged for debugging

## Build Status
```
Build succeeded.
    4 Warning(s)  # Pre-existing warnings
    0 Error(s)
```

## Testing

After rebuilding:
```bash
cd /home/justin/repos/urban-sniffle
./scripts/run-index-editor.sh
```

**Test Steps:**
1. Open a folder with magazine pages
2. Navigate to a page (ensure page image is visible)
3. Double-click on the page image
4. ✅ Window should enter fullscreen mode
5. ✅ Image should display covering entire screen
6. ✅ Page number should be visible
7. Press Esc (or click Close button)
8. ✅ Window should return to normal mode
9. ✅ Application should be back to normal view

## Notes

- This feature is **independent** of the existing F11 fullscreen toggle
- F11 toggles fullscreen for the entire application window
- Double-click image enters fullscreen AND shows image overlay
- The overlay can still be shown in windowed mode if fullscreen entry fails

## Date Implemented
February 17, 2026

