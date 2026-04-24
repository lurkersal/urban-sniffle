# Confirm Dialog - Infinite Loop Fix (Complete)

## Issue

When trying to quit the application with unsaved changes:
1. "Unsaved Changes" dialog appears
2. User presses "No" (or N/Escape) to quit without saving
3. Dialog closes and **immediately reopens**
4. This repeats infinitely - cannot quit without killing the process!

## Root Causes

There were **TWO separate issues** causing the infinite loop:

### Issue 1: Global HotKey on Cancel Button

The ConfirmDialog had `HotKey="Escape"` on the Cancel button which was firing even after the dialog closed.

### Issue 2: HasUnsavedChanges Flag Not Cleared

**The main issue:** In `MainWindow.OnWindowClosing()`, when the user chose "No" (don't save):
1. Dialog returns `false`
2. Code calls `this.Close()` 
3. **But `HasUnsavedChanges` is still `true`!**
4. `OnWindowClosing` fires again
5. Sees unsaved changes, shows dialog again
6. **Infinite loop!**

```csharp
// OLD CODE - CAUSED INFINITE LOOP
if (result)
{
    // Save...
}
// HasUnsavedChanges still true here! ❌
this.Close();  // Triggers OnWindowClosing again → infinite loop!
```

## The Complete Fix

### 1. Removed Global HotKeys

**File:** `src/index-editor/Views/ConfirmDialog.axaml`

**Before:**
```xml
<Button x:Name="CancelButton" Content="_Cancel" HotKey="Escape" />
<Button x:Name="OkButton" Content="_Save" HotKey="Enter" IsDefault="True" />
```

**After:**
```xml
<Button x:Name="CancelButton" Content="_No" />
<Button x:Name="OkButton" Content="_Yes" IsDefault="True" />
```

Changes:
- ❌ Removed `HotKey="Escape"` (was causing infinite loop)
- ❌ Removed `HotKey="Enter"` (could conflict with other handlers)
- ✅ Changed "Cancel" → "No" (clearer for Yes/No question)
- ✅ Changed "Save" → "Yes" (clearer for Yes/No question)

### 2. Added Window Title

```xml
<Window Title="Unsaved Changes" ...>
```

Gives the dialog a proper title instead of blank.

### 3. Enhanced Keyboard Handling

**File:** `src/index-editor/Views/ConfirmDialog.axaml.cs`

**Before:**
```csharp
if (e.Key == Key.Y) Close(true);
else if (e.Key == Key.N) Close(false);
```

**After:**
```csharp
if (e.Key == Key.Y || e.Key == Key.Enter) Close(true);
else if (e.Key == Key.N || e.Key == Key.Escape) Close(false);
```

Now supports:
- **Yes/Save:** Y or Enter
- **No/Cancel:** N or Escape

But these keys are handled by the Window's KeyDown event, not global HotKeys, so they only work when the dialog is active!

### 4. Clear HasUnsavedChanges Flag Before Closing

**File:** `src/index-editor/MainWindow.axaml.cs`

**This was the critical fix!**

**Before:**
```csharp
var result = await ConfirmDialog.ShowDialog(this, "...");

if (result)
{
    // User wants to save
    SaveIndex(folder);
}

// HasUnsavedChanges still true! ❌
this.Close();  // Triggers OnWindowClosing again → infinite loop!
```

**After:**
```csharp
var result = await ConfirmDialog.ShowDialog(this, "...");

if (result)
{
    // User wants to save
    SaveIndex(folder);
}

// Clear the flag so we can quit without being prompted again ✅
IndexEditor.Shared.EditorState.HasUnsavedChanges = false;

this.Close();  // Now this won't trigger OnWindowClosing's prompt again!
```

The key insight: **Whether the user saves or not, they've made their choice. Clear the flag and let them quit!**

## Why This Fix Works

### Fix 1: Context-Aware Keyboard Handling

**Global HotKeys (BAD):**
```xml
<Button HotKey="Escape" />
```
- Fires even when control is not visible
- Fires even when parent window is closing
- Can create infinite loops

**Window KeyDown (GOOD):**
```csharp
this.KeyDown += OnKeyDown;
```
- Only fires when window is active
- Stops firing when window closes
- No infinite loops!

### Fix 2: Clear State Before Re-triggering Events

**The Pattern:**
```csharp
if (someFlag) {
    e.Cancel = true;
    // Do something...
    someFlag = false;  // ✅ Clear the flag first!
    DoActionThatTriggersEventAgain();
}
```

**In this case:**
1. User makes choice in dialog (save or don't save)
2. **Clear `HasUnsavedChanges = false`** ← This is critical!
3. Call `this.Close()`
4. `OnWindowClosing` fires again, but sees `HasUnsavedChanges = false`
5. No dialog, window closes cleanly!

## Build Status
✅ **Build Successful** - 0 Errors, 63 Warnings (pre-existing)

## Testing

### Test 1: Quit Without Saving
1. Make an edit (e.g., add a segment)
2. Press **Alt+F4** or close window
3. Dialog appears: "You have unsaved changes. Do you want to save before quitting?"
4. Press **N** or **Escape** or click **No**
5. ✅ **Verify:** Dialog closes, application quits

### Test 2: Save and Quit
1. Make an edit
2. Try to quit
3. Dialog appears
4. Press **Y** or **Enter** or click **Yes**
5. ✅ **Verify:** Index saved, application quits

### Test 3: No Infinite Loop
1. Make an edit
2. Try to quit
3. Dialog appears
4. Press **Escape** repeatedly
5. ✅ **Verify:** Dialog closes after first press, no reopening, app quits

### Test 4: Keyboard Shortcuts Work
1. Make an edit
2. Try to quit
3. Dialog appears
4. Test all keyboard shortcuts:
   - **Y** → Yes/Save
   - **N** → No/Don't Save
   - **Enter** → Yes/Save
   - **Escape** → No/Don't Save
5. ✅ **Verify:** All shortcuts work correctly

## Related Issues Fixed

This is the **same root cause** as the "Article Disappearing" bug:
- Delete Article dialog had `HotKey="Enter"` 
- Was globally capturing Enter presses meant for ending segments
- Fixed in `ARTICLE_DISAPPEAR_REAL_FIX_FINAL.md`

### Pattern Identified

**All Avalonia dialogs with global HotKeys have issues:**
1. **Delete Article dialog:** Enter captured globally, deleted articles unintentionally
2. **Confirm/Save dialog:** Escape captured globally, created infinite loop

**Solution:** Never use `HotKey` attribute on buttons in dialogs. Use Window KeyDown events instead.

## Files Modified

1. **`src/index-editor/Views/ConfirmDialog.axaml`**
   - Removed `HotKey="Escape"` from Cancel/No button
   - Removed `HotKey="Enter"` from OK/Yes button
   - Changed button labels: "Cancel" → "No", "Save" → "Yes"
   - Added window title: "Unsaved Changes"

2. **`src/index-editor/Views/ConfirmDialog.axaml.cs`**
   - Enhanced KeyDown handler to support Enter/Escape in addition to Y/N
   - Context-aware handling (only when window is active)

3. **`src/index-editor/MainWindow.axaml.cs`** ← **Critical fix!**
   - Added `HasUnsavedChanges = false` before calling `this.Close()`
   - Prevents infinite loop when user chooses not to save

## Prevention

### Audit Complete

Searched for all remaining global HotKeys:
```bash
grep -r 'HotKey="' src/index-editor/**/*.axaml
```

**Result:** No results! ✅

All global HotKeys have been removed from the application.

### Best Practice for Future Dialogs

**DO NOT:**
```xml
<Button HotKey="Enter" />  <!-- Global, always active, causes bugs! -->
```

**DO:**
```csharp
// In dialog constructor:
this.KeyDown += (s, e) => {
    if (e.Key == Key.Enter) {
        // Handle Enter only when dialog is active
        OkButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        e.Handled = true;
    }
};
```

## Summary

### The Problem
- Confirm dialog had global `HotKey="Escape"` 
- **`HasUnsavedChanges` flag was not cleared before closing**
- Created infinite loop when trying to quit without saving
- Application became unusable, required kill

### The Solution
- Removed global HotKeys
- Added context-aware Window KeyDown handling
- **Clear `HasUnsavedChanges = false` before calling `this.Close()`**
- Changed button labels to Yes/No (clearer)
- Added proper window title

### The Result
✅ Can now quit without saving  
✅ No infinite loop  
✅ All keyboard shortcuts still work  
✅ Better UX with Yes/No buttons  
✅ Proper dialog title  

🎉 **Bug fixed!**

