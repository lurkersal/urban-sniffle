# Enhancement: Discovered Links Flag Index File as Modified

## Overview
When links are discovered during automatic scanning, the index file is now flagged as modified (`HasUnsavedChanges = true`). This ensures that discovered links are saved when the user saves the index file or closes the application.

## Rationale
Previously, when links were discovered:
- The status bar showed "Found X link(s) on Y page(s)"
- Links were displayed in the UI
- **BUT** the index file was not marked as modified
- User might close the application without being prompted to save
- Discovered links would be lost

This was problematic because:
1. **Data Loss**: Discovered links (which took time/CPU to generate) could be lost
2. **Inconsistent Behavior**: User expects discovered data to be saved
3. **Poor UX**: No indication that save is needed after discovery

## Solution
Mark the index file as modified when link discovery completes and links are found.

### Implementation
**File**: `/src/index-editor/MainWindow.axaml.cs`
**Method**: `OnLinkDiscoveryCompleted()`

When link discovery completes, check if any links were found. If so, set the `HasUnsavedChanges` flag:

```csharp
if (statusText != null)
{
    var linkCount = _discoveredLinks.Values.Sum(list => list.Count);
    statusText.Text = linkCount > 0 
        ? $"Found {linkCount} link(s) on {_discoveredLinks.Count} page(s)" 
        : "Ready";
    
    // Mark index as modified if links were discovered
    if (linkCount > 0)
    {
        IndexEditor.Shared.EditorState.HasUnsavedChanges = true;
        DebugLogger.Log($"Index file marked as modified due to {linkCount} discovered link(s)");
    }
}
```

## Behavior

### Scenario 1: Links Discovered During Auto-Scan
```
1. Open folder without existing links
2. Auto-discovery starts
3. Discovery finds 12 links
4. Status: "Found 12 link(s) on 8 page(s)"
5. ✅ HasUnsavedChanges = true
6. Try to close application
7. Prompt: "You have unsaved changes. Do you want to save before quitting?"
8. User saves → Links preserved in _index.json
```

### Scenario 2: Links Loaded from Existing JSON
```
1. Open folder with _index.json containing links
2. Status: "Loaded 12 link(s) from index file"
3. ✅ HasUnsavedChanges = false (loaded from disk, no changes)
4. User can close without prompt
```

### Scenario 3: Manual Re-scan Finds New Links
```
1. Open folder with 5 existing links
2. Click "Re-scan for Links"
3. Discovery finds 8 links (3 new)
4. Status: "Found 8 link(s) on 6 page(s)"
5. ✅ HasUnsavedChanges = true
6. User must save to keep new links
```

### Scenario 4: No Links Found
```
1. Open folder
2. Auto-discovery runs
3. No links found (pages have no magazine references)
4. Status: "Ready"
5. ✅ HasUnsavedChanges = false (no changes to save)
6. User can close without prompt
```

## User Experience

### Before Enhancement
```
User: Opens folder → Discovery finds 12 links → Closes app → No save prompt → Links lost
```

### After Enhancement
```
User: Opens folder → Discovery finds 12 links → Tries to close → Save prompt appears → Saves → Links preserved
```

## Technical Details

### When Flag is Set
- **Timing**: After link discovery completes (not during)
- **Condition**: Only if `linkCount > 0`
- **Thread**: On UI thread (via Dispatcher.UIThread.Post)

### When Flag is NOT Set
- Loading existing links from JSON (they're already saved)
- Re-scanning finds zero links
- Discovery cancelled before completion

### Integration with Existing Save Logic
The `HasUnsavedChanges` flag integrates with existing save prompts:
1. **File > Save** (Ctrl+S): Saves if flag is true
2. **File > Open**: Prompts to save if flag is true
3. **Application Close**: Prompts to save if flag is true

## Benefits

1. **Prevents Data Loss**: Discovered links are not lost
2. **User Awareness**: Save prompts indicate changes need saving
3. **Consistent Behavior**: Matches behavior for article edits
4. **Automatic**: No user action needed to trigger
5. **Smart**: Doesn't flag when no links found

## Edge Cases

### Case 1: Discovery During Unsaved Article Edits
```
User has unsaved article edits → HasUnsavedChanges already true
Discovery completes → HasUnsavedChanges remains true (idempotent)
User saves → Both article edits AND links saved
```

### Case 2: Discovery Cancelled
```
User starts discovery → Clicks "Open Folder" before completion
Discovery cancelled → OnLinkDiscoveryCompleted not called
HasUnsavedChanges NOT set → No prompt (correct, links not complete)
```

### Case 3: Re-scan Finds Same Links
```
Open folder with 12 links → Click "Re-scan"
Discovery finds same 12 links → HasUnsavedChanges set
(Slightly redundant, but harmless - user can save or discard)
```

### Case 4: Zero Links on First Scan
```
Folder has no magazine references → Discovery finds 0 links
HasUnsavedChanges NOT set → No prompt (correct, nothing to save)
```

## Testing

### Test 1: Auto-Discovery Flags File
1. Open folder without links
2. Wait for discovery to complete
3. **Verify**: Save button enabled
4. **Verify**: Close prompts to save
5. Save and close
6. Reopen folder
7. **Verify**: Links loaded from JSON

### Test 2: Loaded Links Don't Flag File
1. Open folder with existing _index.json
2. Links loaded: "Loaded X link(s)"
3. **Verify**: Save button disabled
4. **Verify**: Close does NOT prompt
5. Close without save
6. Reopen folder
7. **Verify**: Links still present

### Test 3: Re-scan Flags File
1. Open folder with existing links
2. Click "Re-scan for Links"
3. Wait for completion
4. **Verify**: Save button enabled
5. **Verify**: Close prompts to save

### Test 4: No Links Found
1. Open folder with no magazine references
2. Discovery completes: "Ready"
3. **Verify**: Save button disabled
4. **Verify**: Close does NOT prompt

## Logging

When links are discovered, log message appears:
```
[INFO] Link discovery completed. Found 8 pages with links.
[INFO] Index file marked as modified due to 12 discovered link(s)
```

This helps diagnose issues and confirms the flag is being set correctly.

## Files Modified

1. `/src/index-editor/MainWindow.axaml.cs`
   - Updated `OnLinkDiscoveryCompleted()` method
   - Added `HasUnsavedChanges = true` when links found
   - Added debug logging

## Build Status
✅ **Build Successful** - 0 Errors, 63 Warnings (pre-existing)

## Related Features

This enhancement works with:
- ✅ Link discovery (auto and manual)
- ✅ Save prompts on close
- ✅ Save prompts on open
- ✅ Ctrl+S save shortcut
- ✅ Save button in UI
- ✅ JSON index file format

## Future Considerations

### Potential Enhancement: Smart Detection
Instead of always flagging on re-scan, compare old and new links:
```csharp
// Check if links actually changed
bool linksChanged = !oldLinks.SequenceEqual(newLinks);
if (linksChanged) {
    HasUnsavedChanges = true;
}
```

This would avoid unnecessary save prompts when re-scan finds identical links.

### Potential Enhancement: Undo Discovered Links
Allow user to discard discovered links without saving:
```
Button: "Discard Discovered Links"
Action: Clear _discoveredLinks, set HasUnsavedChanges = false
```

## Summary

✅ **Enhancement Complete**: Discovered links now flag the index file as modified, preventing data loss and providing consistent save behavior. The implementation is simple, effective, and integrates seamlessly with existing save logic.

When discovery completes and finds links:
- Index file marked as modified
- User prompted to save on close/open
- Links preserved in _index.json
- No data loss

