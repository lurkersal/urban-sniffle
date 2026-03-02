# Save Message Corrections

## Issue
**Date**: March 2, 2026  
**Reporter**: User  
**Issue**: "when saving, the text shown is incorrect"

## Problem Description
The toast messages shown when saving the index file incorrectly referenced "_index.txt" even though the application now saves to "_index.json" format (as implemented in JSON Phase 1).

### Incorrect Messages
- "cannot save _index.txt"
- "_index.txt saved"
- "Failed to save _index.txt"

### Why This Was Wrong
Since the JSON implementation (Phase 1), the `IndexSaver.SaveIndex()` method uses `IndexJsonSerializer.SaveToJson()` which saves to `_index.json`, not `_index.txt`. The messages were outdated and confusing to users.

## Solution
Updated all save-related toast messages to use generic "index" wording instead of the specific filename.

### New Messages
- "cannot save index" → Generic, correct
- "Index saved" → Generic, correct
- "Failed to save index" → Generic, correct

## Files Modified

### 1. TopBar.axaml.cs
**File**: `/src/index-editor/Views/TopBar.axaml.cs`

**Changes**: 2 occurrences fixed
- Save button click handler (lines 58, 68, 72, 73)
- Save before opening folder (lines 113, 118)

**Before**:
```csharp
ToastService.Show("No folder opened; cannot save _index.txt");
ToastService.Show("_index.txt saved");
ToastService.Show("Failed to save _index.txt");
```

**After**:
```csharp
ToastService.Show("No folder opened; cannot save index");
ToastService.Show("Index saved");
ToastService.Show("Failed to save index");
```

### 2. FileKeyboardHandler.cs
**File**: `/src/index-editor/Services/KeyboardHandlers/FileKeyboardHandler.cs`

**Changes**: 4 occurrences fixed
- Overlay save (Ctrl+S in index overlay) - lines 130, 137, 145
- Normal save (Ctrl+S) - lines 166, 175, 179

**Before**:
```csharp
ToastService.Show("No folder open; cannot save _index.txt");
ToastService.Show("_index.txt saved");
ToastService.Show("Failed to save _index.txt");
```

**After**:
```csharp
ToastService.Show("No folder open; cannot save index");
ToastService.Show("Index saved");
ToastService.Show("Failed to save index");
```

### 3. MainWindowViewModel.cs
**File**: `/src/index-editor/Views/MainWindowViewModel.cs`

**Changes**: 4 occurrences fixed
- SaveIndex method - lines 74, 82, 88
- SaveIndexFromOverlay method - lines 119, 123

**Before**:
```csharp
ToastService.Show("No folder open; cannot save _index.txt");
ToastService.Show("_index.txt saved");
ToastService.Show("Failed to save _index.txt");
```

**After**:
```csharp
ToastService.Show("No folder open; cannot save index");
ToastService.Show("Index saved");
ToastService.Show("Failed to save index");
```

## Build Status
✅ **Build Successful**
- 0 Errors
- 63 Warnings (all pre-existing)
- No new issues introduced

## Testing Verification

### Test Scenarios
1. **Save Button (Ctrl+S)**
   - Click Save button or press Ctrl+S
   - ✅ Should show: "Index saved"
   - ❌ Previously showed: "_index.txt saved"

2. **Save Before Open**
   - Have unsaved changes
   - Open a different folder
   - Choose "Yes" to save
   - ✅ Should show: "Index saved"
   - ❌ Previously showed: "_index.txt saved"

3. **Save From Overlay**
   - Press Ctrl+I to open index overlay
   - Edit the index
   - Press Ctrl+S to save
   - ✅ Should show: "Index saved"
   - ❌ Previously showed: "_index.txt saved"

4. **Save Failure**
   - Trigger a save error (e.g., read-only folder)
   - ✅ Should show: "Failed to save index"
   - ❌ Previously showed: "Failed to save _index.txt"

## Impact

### User Experience
- **Before**: Confusing - message says "_index.txt" but file saved is "_index.json"
- **After**: Clear - generic message works for any file format

### Future-Proof
The generic messages will remain correct even if:
- File format changes again
- Multiple formats are supported simultaneously
- File naming conventions change

## Related Context

### JSON Implementation
The JSON format was implemented in phases:
- **Phase 1**: Save to `_index.json` only (IndexSaver always uses JSON)
- **Phase 2**: Read both formats (JSON preferred, fallback to TXT)
- The messages weren't updated to reflect this change

### Remaining _index.txt Reference
Note: `MainWindow.axaml.cs` line 356 has:
```csharp
ToastService.Show(isJson ? "_index.json saved" : "_index.txt saved");
```

This is in the overlay save logic and correctly distinguishes between formats based on what was actually saved. This message is correct and was not changed.

## Summary
✅ **COMPLETE**: All incorrect save messages have been updated from "_index.txt" to generic "index" wording. Messages now correctly reflect the actual file format being saved (_index.json) and are future-proof for any format changes.

**Files Changed**: 3  
**Messages Updated**: 10  
**Build Status**: Success  
**User Experience**: Improved

