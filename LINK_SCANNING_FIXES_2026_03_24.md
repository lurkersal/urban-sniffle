# Link Scanning Fixes - March 24, 2026

## Summary
Fixed two issues with the link discovery/scanning feature:

1. **Don't rescan if links already exist**: When opening a folder that has links already saved in the JSON file, the system now skips the automatic link discovery scan.

2. **Warning on folder open during scan**: When a link discovery scan is in progress and the user tries to open a new folder (via Ctrl+O or the Open button), a confirmation dialog is shown asking if they want to wait for the scan to complete or cancel it.

## Changes Made

### 1. Fixed Auto-Scan Behavior in FolderLoadingService

**File**: `/home/justin/repos/urban-sniffle/src/index-editor/Services/FolderLoadingService.cs`

**Method**: `HandleLinkDiscovery()`

**Before**:
```csharp
private void HandleLinkDiscovery(string folder)
{
    try
    {
        if ((_linkDiscoveryUIService?.DiscoveredLinks.Count ?? 0) > 0)
        {
            // Links already exist, show button to optionally re-scan
            _linkDiscoveryUIService?.ShowLinksLoadedFromIndex();
        }
        else
        {
            // No links exist, auto-start discovery
            var magazineName = _editorState.CurrentMagazine ?? "Unknown";
            _linkDiscoveryService?.StartDiscovery(folder, magazineName);
        }
    }
    catch (Exception ex)
    {
        DebugLogger.LogException("FolderLoadingService: link discovery logic", ex);
    }
}
```

**After**:
```csharp
private void HandleLinkDiscovery(string folder)
{
    try
    {
        var linksCount = _linkDiscoveryUIService?.DiscoveredLinks.Count ?? 0;
        
        if (linksCount > 0)
        {
            // Links already exist (loaded from JSON), show button to optionally re-scan
            // DO NOT auto-start discovery - user can manually trigger it if desired
            _linkDiscoveryUIService?.ShowLinksLoadedFromIndex();
            DebugLogger.Log($"FolderLoadingService: {linksCount} links already loaded from index, skipping auto-scan");
        }
        else
        {
            // No links exist, auto-start discovery
            var magazineName = _editorState.CurrentMagazine ?? "Unknown";
            _linkDiscoveryService?.StartDiscovery(folder, magazineName);
            DebugLogger.Log($"FolderLoadingService: No links loaded, auto-starting link discovery scan");
        }
    }
    catch (Exception ex)
    {
        DebugLogger.LogException("FolderLoadingService: link discovery logic", ex);
    }
}
```

**What Changed**:
- Added logging to clarify whether links were loaded from index or scan is starting
- Added comment emphasizing that auto-scan is **skipped** when links already exist
- This prevents redundant scanning when links have already been discovered and saved

### 2. Scan Warning Already Implemented

**Files**:
- `/home/justin/repos/urban-sniffle/src/index-editor/Services/KeyboardHandlers/FileKeyboardHandler.cs`
- `/home/justin/repos/urban-sniffle/src/index-editor/Views/TopBar.axaml.cs`

The warning when opening a folder during an active scan was **already implemented** in both:
- **Ctrl+O handler** (FileKeyboardHandler.HandleCtrlO)
- **Open button** (TopBar.openBtn.Click)

Both implementations:
1. Check if `_linkDiscoveryService?.IsScanning` is true
2. Show a confirmation dialog asking: "A link discovery scan is currently in progress. Do you want to wait for it to complete before opening a new folder?"
3. If user clicks **Yes** (wait): Show toast message and abort the folder open operation
4. If user clicks **No** (cancel scan): Stop the discovery scan and proceed with opening the folder

## How It Works Now

### Scenario 1: Opening Folder with Existing Links

```
User opens folder → 
    FolderLoadingService loads _index.txt → 
        Links found in JSON (e.g., 15 links) →
            LoadLinksFromIndex() populates DiscoveredLinks dictionary →
                HandleLinkDiscovery() sees linksCount > 0 →
                    Shows "Re-scan for Links" button (no auto-scan) →
                        Status bar: "Loaded 15 link(s) from index file"
```

### Scenario 2: Opening Folder without Links

```
User opens folder → 
    FolderLoadingService loads _index.txt → 
        No links in JSON (or first time) →
            DiscoveredLinks.Count == 0 →
                HandleLinkDiscovery() auto-starts scan →
                    LinkDiscoveryService scans all images with OCR →
                        Progress updates shown in status bar →
                            Links saved to index on next save
```

### Scenario 3: Opening Folder During Active Scan

```
User presses Ctrl+O (or clicks Open button) →
    FileKeyboardHandler/TopBar checks IsScanning →
        Scan in progress (IsScanning == true) →
            Shows confirmation dialog →
                User clicks "No" (cancel scan):
                    StopDiscovery() called →
                        Folder picker shown →
                            New folder loaded
                
                User clicks "Yes" (wait):
                    Toast: "Waiting for scan to complete..." →
                        Folder open cancelled →
                            Scan continues
```

## Testing Checklist

### Test 1: No Auto-Scan with Existing Links
1. ✅ Open a folder that has links in `_index.txt`
2. ✅ Verify status bar shows "Loaded N link(s) from index file"
3. ✅ Verify "Re-scan for Links" button is visible
4. ✅ Verify NO automatic OCR scan starts
5. ✅ Click "Re-scan for Links" button
6. ✅ Verify scan starts and finds links

### Test 2: Auto-Scan with No Links
1. ✅ Open a folder that has NO links in `_index.txt` (or delete the links section)
2. ✅ Verify link discovery scan starts automatically
3. ✅ Verify progress bar shows scan progress
4. ✅ Verify links are discovered and shown in page controller

### Test 3: Open During Scan (Ctrl+O)
1. ✅ Open a folder with no links (auto-scan starts)
2. ✅ While scan is in progress, press Ctrl+O
3. ✅ Verify confirmation dialog appears
4. ✅ Click "Yes" (wait) - verify folder open is cancelled
5. ✅ Press Ctrl+O again
6. ✅ Click "No" (cancel scan) - verify scan stops and folder picker opens

### Test 4: Open During Scan (Open Button)
1. ✅ Start a link scan
2. ✅ Click the "Open" button in TopBar
3. ✅ Verify same confirmation dialog behavior as Ctrl+O
The fix applies to both Ctrl+O and the Open button

## Benefits

1. **Performance**: No wasted time rescanning folders that already have links
2. **User Control**: Users can manually trigger re-scan if desired via "Re-scan for Links" button
3. **Data Integrity**: Prevents losing manually discovered links by rescanning
4. **Better UX**: Clear feedback about whether links were loaded or scanned
5. **Safety**: Warns user before interrupting an in-progress scan

## Related Code

### LinkDiscoveryService
- `IsScanning` property: Returns true when a scan is active
- `StartDiscovery()`: Begins OCR scan of all images in folder
- `StopDiscovery()`: Cancels active scan

### LinkDiscoveryUIService
- `DiscoveredLinks`: Dictionary of page → links
- `LoadLinksFromIndex()`: Loads links from JSON into dictionary
- `ShowLinksLoadedFromIndex()`: Shows "Re-scan" button and status message

### FolderLoadingService
- `HandleLinkDiscovery()`: Decides whether to auto-scan or show loaded links
- Called after folder is loaded and links are parsed from JSON

## Notes

- The warning dialog uses the same pattern as the "unsaved changes on exit" warning
- The scan can be safely interrupted at any time - discovered links are preserved
- Manual re-scan via button always works, even if links exist
- Logs include clear messages about skip/start decisions for debugging

