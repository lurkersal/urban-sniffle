# Link Discovery Scan Dialog Fix - March 19, 2026

## Issue
When quitting the application, a "continue scan" dialog appeared even though the discovery scan had already finished. The dialog would ask:

> "A link discovery scan is currently in progress. Do you want to wait for it to complete before exiting?"

This was confusing and incorrect when the scan had already completed.

## Root Cause
The `LinkDiscoveryService.IsScanning` property checks if `_cts` (CancellationTokenSource) is not null:

```csharp
public bool IsScanning => _cts != null && !_cts.IsCancellationRequested;
```

However, when `DiscoverAsync()` completed naturally (successfully scanned all pages), it never cleaned up the `_cts` field. The `_cts` was only being set to null in the `StopDiscovery()` method, which is called when:
- User explicitly cancels the scan
- User opens a new folder during scan
- User quits during scan

But when the scan completed normally, `_cts` remained non-null, causing `IsScanning` to incorrectly return `true`.

## Solution
Modified `LinkDiscoveryService.DiscoverAsync()` to add a `finally` block that cleans up the `_cts` field after the scan completes (whether successfully or via cancellation):

```csharp
private async Task DiscoverAsync(string folder, string magazineName, CancellationToken ct)
{
    try
    {
        // ... existing scan logic ...
        
        DiscoveryCompleted?.Invoke(this, EventArgs.Empty);
    }
    finally
    {
        // Clean up the cancellation token source so IsScanning returns false
        _cts?.Dispose();
        _cts = null;
    }
}
```

This ensures that:
1. After a successful scan completion, `_cts` is set to null
2. After a cancelled scan, `_cts` is also set to null
3. The `IsScanning` property correctly returns `false` after completion
4. The "scan in progress" dialog won't appear incorrectly when quitting

## Files Modified
- `/home/justin/repos/urban-sniffle/src/index-editor/Services/LinkDiscoveryService.cs`

## Build Status
✅ **Build successful**
- 0 Errors
- 22 Warnings (pre-existing, none related to this change)

## Testing Instructions
1. Open a folder in the index editor
2. Wait for the link discovery scan to complete (progress bar disappears)
3. Try to quit the application
4. **Expected**: No "scan in progress" dialog appears, just the normal "unsaved changes" dialog (if applicable)
5. **Previous behavior**: "scan in progress" dialog incorrectly appeared

## Related Code
The fix prevents incorrect behavior in:
- `WindowLifecycleService.OnWindowClosingAsync()` - checks `IsScanning` before prompting to quit
- `FileKeyboardHandler.HandleCtrlO()` - checks `IsScanning` before opening new folder
- `TopBar` open folder button - checks `IsScanning` via reflection

All these locations rely on `IsScanning` being accurate, which now works correctly.

