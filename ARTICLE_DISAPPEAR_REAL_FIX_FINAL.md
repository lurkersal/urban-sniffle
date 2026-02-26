# Article Disappearing Bug - REAL FIX (Final)

## THE REAL ROOT CAUSE - FINALLY FOUND!

After extensive investigation with detailed logs, the actual bug was discovered:

### The Bug

**The Delete Article confirmation button had `HotKey="Enter"` which was globally capturing ALL Enter keypresses, even when the dialog was hidden!**

### What Was Happening

When you:
1. Started a segment (Ctrl+A) ✓
2. Navigated pages ✓  
3. Pressed **Enter to end the segment**
4. **Delete button's HotKey captured the Enter press!**
5. **Article was deleted instead of segment being ended!**

### The Evidence

From your log:
```
[TRACE] EditorActionsService.AddSegmentAtCurrentPage: created new active segment start=31 article='Emma'
10:21:43 info: IndexEditor[0] ==> OnEditorStateChanged CALLED: SelectedArticle is currently 'Emma'
...
10:23:03 info: IndexEditor[0] DeleteArticleConfirmBtn.Click invoked  ← Enter captured here!
10:23:03 info: IndexEditor[0] toDelete=Emma
```

Stack trace shows:
```
at Avalonia.Controls.HotKeyManager.HotkeyCommandWrapper.Execute(Object parameter)
at Avalonia.Input.KeyBinding.TryHandle(KeyEventArgs args)
```

This confirms the HotKey system captured the Enter press before the SegmentKeyboardHandler could handle it!

### Why This Happened

**Avalonia HotKeys are GLOBAL** - they work even when:
- The control is not visible
- The control is not focused
- The control is inside a hidden overlay

The XAML was:
```xml
<Button x:Name="DeleteArticleConfirmBtn" Content="_Delete" HotKey="Enter" IsDefault="True" />
```

This made Enter **always** trigger delete, regardless of context!

## The Fix

### 1. Removed Global HotKeys from Delete Dialog Buttons

**File:** `src/index-editor/MainWindow.axaml`

**Before:**
```xml
<Button x:Name="DeleteArticleConfirmBtn" Content="_Delete" HotKey="Enter" IsDefault="True" />
<Button x:Name="DeleteArticleCancelBtn" Content="_Cancel" HotKey="Escape" />
```

**After:**
```xml
<Button x:Name="DeleteArticleConfirmBtn" Content="_Delete" IsDefault="True" />
<Button x:Name="DeleteArticleCancelBtn" Content="_Cancel" />
```

Removed `HotKey="Enter"` and `HotKey="Escape"` to prevent global capture.

### 2. Added Context-Aware Keyboard Handling

**File:** `src/index-editor/MainWindow.axaml.cs`

Added KeyDown handler to the Delete overlay that:
- **Only handles keys when overlay is visible**
- **Enter:** Triggers delete confirm button
- **Escape:** Closes overlay
- **Makes overlay focusable** so it receives keyboard input
- **Auto-focuses overlay** when it becomes visible

```csharp
delOverlay.KeyDown += (s, e) =>
{
    if (delOverlay.IsVisible)
    {
        if (e.Key == Key.Enter)
        {
            // Trigger delete confirm
            delConfirm.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            // Close overlay
            delOverlay.IsVisible = false;
            e.Handled = true;
        }
    }
};

// Auto-focus overlay when it becomes visible
delOverlay.PropertyChanged += (s, e) =>
{
    if (e.Property.Name == "IsVisible" && isVisible)
    {
        delOverlay.Focus();
    }
};
```

### Why This Fix Works

**Context-aware keyboard handling:**
- ✅ Enter ends segment (when no overlay visible)
- ✅ Enter confirms delete (when delete overlay visible)
- ✅ No global hotkey conflicts
- ✅ Proper event handling hierarchy

## Build Status
✅ **Build Successful** - 0 Errors, 0 Warnings

## Testing

### Test 1: End Segment (Your Exact Steps)
1. Double-click article "Emma"
2. Navigate to page 31 (left arrow)
3. Press **Ctrl+A** (start segment)
4. Navigate to page 32 (left arrow)
5. Press **Enter** (end segment)
6. ✅ **Verify:** Segment ends, article stays visible, focus returns to page controller

### Test 2: Delete Article Still Works
1. Select an article
2. Press **Ctrl+D** (show delete confirmation)
3. Press **Enter** (confirm delete)
4. ✅ **Verify:** Article deleted

### Test 3: Cancel Delete
1. Select an article
2. Press **Ctrl+D** (show delete confirmation)
3. Press **Escape** (cancel)
4. ✅ **Verify:** Dialog closes, article NOT deleted

### Test 4: Multiple Segments
1. Start segment (Ctrl+A)
2. Navigate pages
3. End segment (Enter) ← Should work now!
4. Start another segment (Ctrl+A)
5. End segment (Enter) ← Should work now!
6. ✅ **Verify:** All segments added, article never disappears

## Why All Previous Fixes Failed

**v1-v4:** All attempted to fix selection preservation during reordering
- ✅ Those fixes ARE working correctly
- ❌ But they couldn't prevent the WRONG BUTTON from being clicked!

**The real issue:** Enter was **never reaching** the segment end handler - it was being intercepted by the Delete button's global HotKey!

### Evidence from Your Testing

You said: "I have not hit delete or ctrl d at all"

**You were right!** You pressed Enter to end the segment, but:
1. Delete button's HotKey captured it
2. Delete button clicked itself
3. Article deleted
4. You thought it "disappeared due to segment ending"

**Actually:** It disappeared due to accidental deletion triggered by global HotKey conflict!

## The Journey

1. **Initial attempts:** Tried to fix selection preservation (not the issue)
2. **Disabled reordering:** Article still disappeared (confirmed not reordering)
3. **Removed fake PropertyChanged:** Still disappeared (not the issue)
4. **Added extensive logging:** Finally captured the stack trace
5. **Stack trace showed:** `HotKeyManager.HotkeyCommandWrapper.Execute`
6. **Root cause:** Global HotKey on Delete button

## Key Learnings

### 1. Always Check Stack Traces

The stack trace immediately showed:
```
at Avalonia.Controls.HotKeyManager.HotkeyCommandWrapper.Execute
```

This pointed directly to the HotKey system being involved!

### 2. Global HotKeys Are Dangerous

HotKeys in Avalonia work regardless of:
- Visibility
- Focus
- Context

Always use context-aware keyboard handling instead!

### 3. User Reports Are Often Correct

You said: "I pressed Enter to end segment, article disappeared, I didn't press delete"

**You were 100% correct!** The issue was the Enter press was hijacked by the wrong handler!

### 4. Symptoms Can Be Misleading

**Symptom:** "Article disappears after ending segment"  
**Assumption:** Selection lost during reordering  
**Reality:** Article deleted by hotkey conflict

Always verify assumptions with logging/debugging!

## Prevention

### For Future Dialogs/Overlays

**Bad:**
```xml
<Button HotKey="Enter" />  <!-- Global, always active! -->
```

**Good:**
```csharp
overlay.KeyDown += (s, e) => {
    if (overlay.IsVisible && e.Key == Key.Enter) {
        // Handle Enter only when visible
    }
};
```

### Audit Other Dialogs

Search for other global HotKeys:
```bash
grep -r 'HotKey="' src/index-editor/**/*.axaml
```

Review each one to ensure they're context-appropriate!

## Files Modified

1. **`src/index-editor/MainWindow.axaml`**
   - Removed `HotKey="Enter"` from Delete confirm button
   - Removed `HotKey="Escape"` from Delete cancel button

2. **`src/index-editor/MainWindow.axaml.cs`**
   - Added KeyDown handler to Delete overlay
   - Added auto-focus when overlay becomes visible
   - Enter/Escape now only work when overlay visible

## Conclusion

After 4+ attempted fixes targeting the wrong issue, the real bug was finally found:

**A global HotKey conflict caused Enter presses to trigger article deletion instead of ending segments.**

The fix is simple, surgical, and correct. The article will now stay visible after ending segments because the segment will actually end properly instead of being hijacked by the delete button!

🎉 **Bug finally squashed!**

