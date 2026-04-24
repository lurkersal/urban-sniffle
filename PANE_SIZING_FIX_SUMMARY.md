# Persistent Pane Sizing Fix - Quick Summary

## Problem
✗ Pane sizes (Article List, Editor, Page Controller) were NOT persisting between sessions

## Root Cause
`SaveWindowState()` was only saving window size, NOT column widths

## Fix Applied
**File**: `WindowLifecycleService.cs`

### Before
```csharp
WindowStateStore.SetWindowState(width, height, isMax);
// Column widths NOT saved! ❌
```

### After
```csharp
// NEW: Capture column widths from MainGrid
double col0Width = 0, col1Width = 0, col2Width = 0;
var mainGrid = _window.FindControl<Grid>("MainGrid");
if (mainGrid != null && mainGrid.ColumnDefinitions.Count >= 3)
{
    col0Width = mainGrid.ColumnDefinitions[0].Width.Value;
    col1Width = mainGrid.ColumnDefinitions[1].Width.Value;
    col2Width = mainGrid.ColumnDefinitions[2].Width.Value;
    DebugLogger.Log($"Saving column widths: {col0Width:F2}, {col1Width:F2}, {col2Width:F2}");
}

// FIXED: Now saves column widths ✅
WindowStateStore.SetWindowState(width, height, isMax, col0Width, col1Width, col2Width);
```

## Testing
1. Resize panes with GridSplitters
2. Close application
3. Reopen application
4. ✅ Panes should be same size as before

## Build Status
✅ 0 errors, 22 warnings (all pre-existing)

## Storage
- Linux: `~/.local/share/urban-sniffle/index-editor-windowstate.json`
- Windows: `%APPDATA%\urban-sniffle\index-editor-windowstate.json`
- macOS: `~/Library/Application Support/urban-sniffle/index-editor-windowstate.json`

## Debug Log Messages
```
# On Save:
WindowLifecycleService: Saving column widths: 0.80, 1.50, 0.70

# On Restore:
WindowLifecycleService: Restored column widths: 0.80, 1.50, 0.70

# On First Run (no saved state):
WindowLifecycleService: Column widths not saved (col0=0, col1=0, col2=0)
```

