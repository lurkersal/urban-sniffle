# Link Discovery Scan Protection - March 19, 2026

## Feature Request
When the user attempts to exit the app or open a new folder while a link discovery scan is in progress, warn the user and let them choose to:
1. Wait for the scan to complete
2. Cancel the scan and proceed

## Implementation Summary

### Changes Made

#### 1. Added `IsScanning` Property to LinkDiscoveryService ✅
**File:** `src/index-editor/Services/LinkDiscoveryService.cs`

Added a public property to check if a scan is currently running:
```csharp
public bool IsScanning => _cts != null && !_cts.IsCancellationRequested;
```

This checks if:
- A CancellationTokenSource exists (scan started)
- Cancellation hasn't been requested (scan still running)

#### 2. Updated WindowLifecycleService for Exit Protection ✅
**File:** `src/index-editor/Services/WindowLifecycleService.cs`

**Changes:**
- Added `LinkDiscoveryService` as constructor dependency
- Added `PromptForScanInProgressAsync()` helper method
- Modified `OnWindowClosingAsync()` to check for ongoing scan

**Behavior:**
When user tries to close the window:
1. Check if `LinkDiscoveryService.IsScanning` is true
2. If yes, show dialog: "A link discovery scan is currently in progress. Do you want to wait for it to complete before exiting?"
3. If user clicks **Yes/Save**: Don't close window, show toast "Waiting for scan to complete"
4. If user clicks **No/Cancel**: Cancel the scan, proceed with exit

#### 3. Updated TopBar Open Folder Handler ✅
**File:** `src/index-editor/Views/TopBar.axaml.cs`

**Changes:**
- Added scan-in-progress check before opening folder picker
- Uses reflection to access `MainWindow._linkDiscoveryService`

**Behavior:**
When user clicks "Open Folder" button:
1. Check if scan is in progress
2. If yes, show dialog asking if user wants to wait
3. If user chooses to wait: Show toast, don't open folder
4. If user chooses to proceed: Cancel scan, continue with folder open

#### 4. Updated FileKeyboardHandler for Ctrl+O Protection ✅
**File:** `src/index-editor/Services/KeyboardHandlers/FileKeyboardHandler.cs`

**Changes:**
- Added `LinkDiscoveryService` as constructor dependency
- Added scan check in `HandleCtrlO()` method
- Extracted `OpenFolderAsync()` helper method to avoid code duplication

**Behavior:**
When user presses Ctrl+O:
1. Check if scan is in progress
2. If yes, show dialog asking if user wants to wait
3. If user chooses to wait: Show toast, don't open folder
4. If user chooses to proceed: Cancel scan, continue with folder open

#### 5. Updated MainWindow Dependency Injection ✅
**File:** `src/index-editor/MainWindow.axaml.cs`

**Changes:**
- Pass `_linkDiscoveryService` to `WindowLifecycleService` constructor (2 locations)
- Pass `_linkDiscoveryService` to `FileKeyboardHandler` constructor

## User Experience Flow

### Scenario 1: User Tries to Exit During Scan
```
1. User starts IndexEditor and opens a folder
2. Link discovery scan starts automatically (progress bar visible)
3. User clicks window close button (X) or presses Alt+F4
4. Dialog appears:
   "A link discovery scan is currently in progress. 
    Do you want to wait for it to complete before exiting?"
   [Yes/Save] [No/Cancel]
5a. User clicks Yes → Window stays open, toast shows "Waiting for scan to complete..."
5b. User clicks No → Scan cancelled, toast shows "Link discovery scan cancelled.", window closes
```

### Scenario 2: User Tries to Open New Folder During Scan
```
1. User has folder open with scan in progress
2. User clicks "Open Folder" button OR presses Ctrl+O
3. Dialog appears:
   "A link discovery scan is currently in progress. 
    Do you want to wait for it to complete before opening a new folder?"
   [Yes/Save] [No/Cancel]
4a. User clicks Yes → Folder picker doesn't open, toast shows "Waiting for scan to complete..."
4b. User clicks No → Scan cancelled, folder picker opens normally
```

## Technical Details

### IsScanning Property
- **Thread-safe**: Reads volatile `_cts` field
- **Null-safe**: Returns false if `_cts` is null
- **Accurate**: Checks `IsCancellationRequested` to avoid false positives

### Dialog Behavior
- Uses existing `ConfirmDialog.ShowDialog()` for consistency
- Shows on UI thread (proper async/await handling)
- Yes/Save button = Wait for scan
- No/Cancel button = Cancel scan and proceed

### Cancellation Handling
- Calls `LinkDiscoveryService.StopDiscovery()` when user chooses to proceed
- Cleans up resources properly (disposes CancellationTokenSource)
- Shows confirmation toast after cancellation

## Files Modified
1. ✅ `src/index-editor/Services/LinkDiscoveryService.cs`
2. ✅ `src/index-editor/Services/WindowLifecycleService.cs`
3. ✅ `src/index-editor/Views/TopBar.axaml.cs`
4. ✅ `src/index-editor/Services/KeyboardHandlers/FileKeyboardHandler.cs`
5. ✅ `src/index-editor/MainWindow.axaml.cs`

## Build Status
```
✅ Build succeeded (0 errors, 22 pre-existing warnings)
```

## Testing Checklist
- [ ] **Manual Test**: Start scan, try to close window → Dialog appears
- [ ] **Manual Test**: Start scan, click Yes in dialog → Window stays open
- [ ] **Manual Test**: Start scan, click No in dialog → Scan cancelled, window closes
- [ ] **Manual Test**: Start scan, press Ctrl+O → Dialog appears
- [ ] **Manual Test**: Start scan, click Open Folder button → Dialog appears
- [ ] **Manual Test**: Start scan, click No in open dialog → Scan cancelled, folder picker opens
- [ ] **Manual Test**: Scan completes → Can close/open folder normally without dialog

## Edge Cases Handled
1. ✅ **Scan already complete**: No dialog shown, normal behavior
2. ✅ **No scan ever started**: No dialog shown, normal behavior  
3. ✅ **Scan cancelled externally**: IsScanning returns false, no dialog
4. ✅ **Multiple open attempts**: Each shows dialog, properly cancels/continues
5. ✅ **Service not available**: Null checks prevent crashes

## Notes
- The dialog message could be customized per context (exit vs open folder)
- Consider adding a "Don't ask again" checkbox in future enhancement
- Scan progress is preserved in discovered links even if cancelled
- User can always manually restart scan using "Start Link Discovery" button

