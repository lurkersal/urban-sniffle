# Bug Fix: Links from Previous Folder Persisting

## Issue Report
**Date**: March 2, 2026
**Reported By**: User
**Severity**: Medium

### Problem Description
When opening a new folder that contains an old `_index.txt` file (no JSON with links), the status bar incorrectly shows "Loaded X link(s) from index file" even though the current folder's index file contains no links. These links are actually from the previously opened folder.

### Root Cause
The `_discoveredLinks` dictionary was only being cleared inside the JSON loading block in `LoadArticlesFromFolder()`. When a folder had an old `_index.txt` file (not JSON), the clear operation was never executed, causing links from the previous folder to persist.

**Problematic Code Flow:**
```
1. Open Folder A with JSON containing links
   → _discoveredLinks.Clear() called (inside JSON block)
   → Links loaded from JSON
   
2. Open Folder B with old _index.txt (no links)
   → JSON block skipped (no JSON file)
   → _discoveredLinks.Clear() NEVER called
   → Old links from Folder A still in memory
   → Status shows "Loaded 4 link(s)" (incorrect)
```

### Code Location
**File**: `/src/index-editor/MainWindow.axaml.cs`
**Method**: `LoadArticlesFromFolder(string folder)`

**Before (Buggy Code):**
```csharp
var articles = new List<Common.Shared.ArticleLine>();
// ...

if (Common.Shared.IndexJsonSerializer.JsonExists(folder))
{
    // ...
    articles = jsonArticles;
    
    // Load links into discovered links dictionary
    _discoveredLinks.Clear();  // ❌ Only cleared if JSON exists!
    if (loadedLinks != null && loadedLinks.Count > 0)
    {
        // ...
    }
}
```

### Solution
Move `_discoveredLinks.Clear()` to the beginning of `LoadArticlesFromFolder()`, before any conditional loading logic. This ensures links are always cleared when loading a new folder, regardless of file format.

**After (Fixed Code):**
```csharp
// Clear discovered links from previous folder
_discoveredLinks.Clear();  // ✅ Always cleared for any new folder

var articles = new List<Common.Shared.ArticleLine>();
// ...

if (Common.Shared.IndexJsonSerializer.JsonExists(folder))
{
    // ...
    articles = jsonArticles;
    
    // Load links into discovered links dictionary (already cleared at method start)
    if (loadedLinks != null && loadedLinks.Count > 0)
    {
        // ...
    }
}
```

### Files Changed
1. `/src/index-editor/MainWindow.axaml.cs`
   - Added `_discoveredLinks.Clear()` at start of `LoadArticlesFromFolder()`
   - Removed redundant clear from inside JSON loading block
   - Updated comment to reflect change

### Testing
**Test Case 1: Open folder with JSON, then folder with TXT**
1. Open Folder A with `_index.json` containing 4 links
2. Verify status: "Loaded 4 link(s) from index file"
3. Open Folder B with `_index.txt` (no links)
4. **Expected**: Status shows "Ready" or starts discovery
5. **Before Fix**: Status showed "Loaded 4 link(s)" ❌
6. **After Fix**: Status correct ✅

**Test Case 2: Open folder with TXT, then folder with JSON**
1. Open Folder A with `_index.txt` (no links)
2. Verify auto-discovery starts
3. Open Folder B with `_index.json` containing 3 links
4. **Expected**: Status shows "Loaded 3 link(s) from index file"
5. **Before Fix**: Might show wrong count ❌
6. **After Fix**: Correct count ✅

**Test Case 3: Open multiple folders with JSON**
1. Open Folder A with 4 links
2. Open Folder B with 2 links
3. Open Folder C with 5 links
4. **Expected**: Each shows correct count
5. **Before & After**: Works correctly (clear was in JSON block) ✅

### Impact
- **Severity**: Medium (incorrect information, but doesn't crash)
- **User Experience**: Confusing - shows links that don't exist
- **Data Integrity**: Not affected (links not actually saved)
- **Frequency**: Every time user opens TXT folder after JSON folder

### Verification
After fix:
- ✅ Build succeeds with no errors
- ✅ Links always cleared when loading new folder
- ✅ Status bar shows accurate information
- ✅ No regression in existing functionality

### Related Issues
None - This was a new bug introduced with the link discovery feature.

### Prevention
Going forward, any state that's folder-specific should be cleared at the beginning of `LoadArticlesFromFolder()`, not conditionally inside loading blocks.

### Documentation Updated
1. `LINK_DEDUPLICATION_IMPLEMENTATION.md` - Added bug fix section
2. `LINK_PERSISTENCE_BUG_FIX.md` - This document

## Summary
✅ **Bug Fixed**: Links from previous folder no longer persist when opening a new folder with a TXT index file. The `_discoveredLinks` dictionary is now always cleared at the start of folder loading, ensuring accurate link counts and status messages.

## Status
**FULLY IMPLEMENTED**: The fix was applied on March 2, 2026. The `_discoveredLinks.Clear()` statement is now at line 895 of MainWindow.axaml.cs, immediately after parsing folder metadata and before loading any index files.


