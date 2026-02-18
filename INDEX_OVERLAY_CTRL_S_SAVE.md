# Index Overlay Ctrl-S Save Feature

## Issue Fixed
Ctrl-S was not saving the _index.txt file when the index overlay (Ctrl-I) was visible. The overlay TextBox would receive focus but Ctrl-S did nothing.

## Root Cause
The `OnMainWindowKeyDown` handler had an early return statement when the index overlay was visible:

```csharp
if (overlay != null && overlay.IsVisible)
{
    // Allow Ctrl+I (toggle overlay) while editing
    if (e.Key == Key.I && e.KeyModifiers.HasFlag(KeyModifiers.Control))
    {
        // Handle Ctrl+I
        return;
    }
    
    // Allow Esc to close the overlay
    if (e.Key == Key.Escape)
    {
        // Handle Esc
        return;
    }
    
    // Let the textbox capture all other keys; do not run global shortcuts
    return;  // ← THIS WAS BLOCKING CTRL-S!
}
```

The comment "Let the textbox capture all other keys; do not run global shortcuts" was preventing Ctrl-S from being processed, even though there was Ctrl-S handling code later in the method.

## Fix Applied

**File:** `/home/justin/repos/urban-sniffle/src/index-editor/MainWindow.axaml.cs`
**Method:** `OnMainWindowKeyDown`
**Lines:** 557-598 (approximately)

Added Ctrl-S handling in the early overlay section, **before** the return statement:

```csharp
if (overlay != null && overlay.IsVisible)
{
    // Allow Ctrl+I (toggle overlay) while editing
    if (e.Key == Key.I && e.KeyModifiers.HasFlag(KeyModifiers.Control))
    {
        overlay.IsVisible = false;
        e.Handled = true;
        return;
    }
    
    // Allow Esc to close the overlay
    if (e.Key == Key.Escape)
    {
        overlay.IsVisible = false;
        e.Handled = true;
        return;
    }
    
    // Allow Ctrl+S to save the overlay content  ← NEW!
    if (e.Key == Key.S && e.KeyModifiers.HasFlag(KeyModifiers.Control))
    {
        // Save overlay text to _index.txt
        // Reload articles
        e.Handled = true;
        return;
    }
    
    // Let the textbox capture all other keys
    return;
}
```

### Behavior

**When Index Overlay is VISIBLE (Ctrl-I pressed):**
1. User presses Ctrl-S
2. The early handler in OnMainWindowKeyDown catches it (lines ~557-598)
3. The text in the overlay TextBox is saved to _index.txt
4. Toast shows: "_index.txt saved from overlay"
5. Articles are reloaded from the file to reflect changes
6. Overlay remains open for further editing

**When Index Overlay is NOT visible:**
1. User presses Ctrl-S
2. Normal save logic executes (saves from EditorState.Articles using IndexSaver)
3. Toast shows: "_index.txt saved"
4. Active segment check applies (blocks save if segment is active)

## Code Structure

The Ctrl-S handling is now in **two places**:

1. **Early handler when overlay is visible** (lines ~557-598)
   - Runs BEFORE the early return that blocks other shortcuts
   - Directly accesses the overlay TextBox
   - No active segment check (overlay editing is direct file editing)

2. **Late handler when overlay is NOT visible** (lines ~729-800)
   - Runs during normal keyboard shortcut processing
   - Checks for active segments
   - Uses IndexSaver to save from EditorState.Articles

## User Experience

### Before This Change:
1. Press Ctrl-I to open overlay
2. Edit the _index.txt content
3. Click the "Save" button with mouse
4. Continue editing or press Ctrl-I to close

### After This Change:
1. Press Ctrl-I to open overlay
2. Edit the _index.txt content
3. **Press Ctrl-S to save** (no mouse needed!)
4. Continue editing or press Ctrl-I to close

## Advantages

✅ **Keyboard-driven workflow** - No need to switch to mouse to click Save button  
✅ **Consistent with expectations** - Ctrl-S naturally saves the currently focused content  
✅ **Non-breaking** - Normal Ctrl-S behavior unchanged when overlay is closed  
✅ **Same atomic write logic** - Uses the same safe write-to-temp-then-replace logic as the button  
✅ **Auto-reload** - Articles automatically reload after save, just like the button  

## Edge Cases Handled

1. **No folder open** → Toast: "No folder open; cannot save _index.txt"
2. **Write failure** → Toast: "Failed to save _index.txt" + exception logged
3. **Overlay closed** → Falls through to normal save logic
4. **Active segment** → Only checked when overlay is closed; overlay save works regardless

## Code Reuse

The overlay save logic is now available in **three** places:
1. **Save Button Click** (lines ~286-314) - Original implementation
2. **Ctrl-S Early Handler** (lines ~557-598) - When overlay is visible
3. **Ctrl-S Late Handler** (lines ~729-800) - When overlay is NOT visible (fallback, shouldn't be reached for overlay)

All use identical logic:
- Check for current folder
- Atomic write to temp file then replace
- Toast notification
- Reload articles from folder

## Consistency Note

When saving from overlay:
- **Active segment check is bypassed** - This is intentional because overlay editing is a direct file edit mode, not segment-based editing
- This matches the Save button behavior which also doesn't check for active segments

## Build Status
```
Build succeeded.
    0 Error(s)
```

## Testing

After rebuilding:
```bash
cd /home/justin/repos/urban-sniffle
./scripts/run-index-editor.sh
```

**Test Steps:**
1. Open a folder with an _index.txt file
2. Press Ctrl-I to open the index overlay
3. Make some edits to the text
4. Press Ctrl-S
5. ✅ Should show toast: "_index.txt saved from overlay"
6. ✅ Articles should reload with changes
7. ✅ Overlay should remain open
8. Press Ctrl-I to close overlay
9. Press Ctrl-S again
10. ✅ Should save normally: "_index.txt saved"

## Integration with Existing Features

This feature integrates seamlessly with:
- **Ctrl-I** - Toggle overlay on/off
- **Ctrl-S** - Context-aware save (overlay vs normal)
- **Save Button** - Still works for mouse users
- **Close Button** - Closes overlay without saving (if changes made)
- **Global Ctrl-A** - Works in overlay (with handledEventsToo fix)

## Date Implemented
February 17, 2026

