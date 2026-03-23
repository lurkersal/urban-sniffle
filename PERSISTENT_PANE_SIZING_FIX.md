# Persistent Pane Sizing Fix - March 24, 2026

## Problem
The application has 3 resizable panes (Article List, Article Editor, Page Controller) separated by GridSplitters. Users can resize these panes by dragging the splitters, but **the pane sizes were not persisting** between application sessions.

## Root Cause
The `WindowLifecycleService.SaveWindowState()` method was only saving window width/height and maximized state, but **NOT the column widths** of the MainGrid.

### Before Fix
```csharp
private void SaveWindowState()
{
    var isMax = _window.WindowState == Avalonia.Controls.WindowState.Maximized;
    var width = _window.Width;
    var height = _window.Height;
    
    // ❌ Column widths NOT captured!
    WindowStateStore.SetWindowState(width, height, isMax);
}
```

Even though:
- `WindowState` class had `Column0Width`, `Column1Width`, `Column2Width` properties
- `RestoreWindowState()` method could restore column widths
- `WindowStateStore.SetWindowState()` accepted column width parameters

The `SaveWindowState()` method was calling it **without the column width parameters**, so they defaulted to 0 and were never saved.

## Solution

### File Modified
**`/home/justin/repos/urban-sniffle/src/index-editor/Services/WindowLifecycleService.cs`**

### Change 1: Save Column Widths (Line ~277)
```csharp
private void SaveWindowState()
{
    try
    {
        var isMax = _window.WindowState == Avalonia.Controls.WindowState.Maximized;
        var width = _window.Width;
        var height = _window.Height;

        if (double.IsNaN(width) || double.IsInfinity(width) || width <= 0)
            width = 1024;
        if (double.IsNaN(height) || double.IsInfinity(height) || height <= 0)
            height = 768;

        // ✅ NEW: Capture column widths from MainGrid
        double col0Width = 0, col1Width = 0, col2Width = 0;
        try
        {
            var mainGrid = _window.FindControl<Grid>("MainGrid");
            if (mainGrid != null && mainGrid.ColumnDefinitions.Count >= 3)
            {
                var col0 = mainGrid.ColumnDefinitions[0];
                var col1 = mainGrid.ColumnDefinitions[1];
                var col2 = mainGrid.ColumnDefinitions[2];

                // Only save if they use Star sizing (resizable columns)
                if (col0.Width.IsStar && col1.Width.IsStar && col2.Width.IsStar)
                {
                    col0Width = col0.Width.Value;
                    col1Width = col1.Width.Value;
                    col2Width = col2.Width.Value;
                    DebugLogger.Log($"Saving column widths: {col0Width:F2}, {col1Width:F2}, {col2Width:F2}");
                }
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("SaveWindowState: capture column widths", ex);
        }

        // ✅ CHANGED: Now passes column widths
        WindowStateStore.SetWindowState(width, height, isMax, col0Width, col1Width, col2Width);
    }
    catch (Exception ex)
    {
        DebugLogger.LogException("WindowLifecycleService.SaveWindowState", ex);
    }
}
```

### Change 2: Add Logging to Restore (Line ~138)
Added debug logging to help verify when column widths are restored or why they might not be:

```csharp
// Restore column widths if they were saved
if (st.Column0Width > 0 && st.Column1Width > 0 && st.Column2Width > 0)
{
    try
    {
        var mainGrid = _window.FindControl<Grid>("MainGrid");
        if (mainGrid != null && mainGrid.ColumnDefinitions.Count >= 3)
        {
            mainGrid.ColumnDefinitions[0] = new ColumnDefinition(st.Column0Width, GridUnitType.Star);
            mainGrid.ColumnDefinitions[1] = new ColumnDefinition(st.Column1Width, GridUnitType.Star);
            mainGrid.ColumnDefinitions[2] = new ColumnDefinition(st.Column2Width, GridUnitType.Star);
            // ✅ NEW: Log restoration
            DebugLogger.Log($"Restored column widths: {st.Column0Width:F2}, {st.Column1Width:F2}, {st.Column2Width:F2}");
        }
        else
        {
            // ✅ NEW: Log if MainGrid not found
            DebugLogger.Log("MainGrid not found or has insufficient columns");
        }
    }
    catch (Exception ex)
    {
        DebugLogger.LogException("RestoreWindowState: restore column widths", ex);
    }
}
else
{
    // ✅ NEW: Log when column widths weren't saved
    DebugLogger.Log($"Column widths not saved (col0={st.Column0Width}, col1={st.Column1Width}, col2={st.Column2Width})");
}
```

## How It Works

### MainGrid Structure (MainWindow.axaml)
```xml
<Grid x:Name="MainGrid" RowDefinitions="Auto,*,Auto" ColumnDefinitions="*,*,*">
    <!-- Column 0: Article List -->
    <Border Grid.Row="1" Grid.Column="0">
        <views:ArticleList />
    </Border>
    
    <!-- GridSplitter between columns 0 and 1 -->
    <GridSplitter Grid.Row="1" Grid.Column="1" Width="6" HorizontalAlignment="Left" />
    
    <!-- Column 1: Article Editor -->
    <Border Grid.Row="1" Grid.Column="1">
        <views:ArticleEditor />
    </Border>
    
    <!-- GridSplitter between columns 1 and 2 -->
    <GridSplitter Grid.Row="1" Grid.Column="2" Width="6" HorizontalAlignment="Left" />
    
    <!-- Column 2: Page Controller -->
    <Border Grid.Row="1" Grid.Column="2">
        <views:PageControllerView />
    </Border>
</Grid>
```

### Column Width Values (Star Sizing)
- Initial: `*,*,*` = 1.0, 1.0, 1.0 (equal widths)
- After user resizes: e.g., 0.8, 1.5, 0.7 (proportional)
- These Star values determine the **relative** widths of the columns

### Save Flow
```
User closes window →
    WindowLifecycleService.OnWindowClosingAsync() →
        SaveWindowState() →
            Read MainGrid.ColumnDefinitions[0..2].Width.Value →
                WindowStateStore.SetWindowState(w, h, max, col0, col1, col2) →
                    Serialize to JSON →
                        Save to: ~/.local/share/urban-sniffle/index-editor-windowstate.json
```

### Restore Flow
```
User opens window →
    WindowLifecycleService.OnWindowOpened() →
        RestoreWindowState() →
            WindowStateStore.GetWindowState() →
                Deserialize from JSON →
                    If column widths > 0:
                        mainGrid.ColumnDefinitions[0] = new ColumnDefinition(col0, Star)
                        mainGrid.ColumnDefinitions[1] = new ColumnDefinition(col1, Star)
                        mainGrid.ColumnDefinitions[2] = new ColumnDefinition(col2, Star)
```

## Storage Location

### Windows
```
C:\Users\<username>\AppData\Roaming\urban-sniffle\index-editor-windowstate.json
```

### Linux
```
~/.local/share/urban-sniffle/index-editor-windowstate.json
```

### macOS
```
~/Library/Application Support/urban-sniffle/index-editor-windowstate.json
```

## Sample JSON File

### Before Fix (No Column Widths)
```json
{
  "Width": 1280,
  "Height": 720,
  "IsMaximized": false,
  "Column0Width": 0,
  "Column1Width": 0,
  "Column2Width": 0
}
```

### After Fix (With Column Widths)
```json
{
  "Width": 1280,
  "Height": 720,
  "IsMaximized": false,
  "Column0Width": 0.8,
  "Column1Width": 1.5,
  "Column2Width": 0.7
}
```

## Testing

### Test 1: Save Pane Sizes
1. ✅ Run the application
2. ✅ Resize the panes by dragging the GridSplitters
3. ✅ Close the application
4. ✅ Check console output: Should see "Saving column widths: X.XX, Y.YY, Z.ZZ"
5. ✅ Check JSON file: Column widths should be non-zero

### Test 2: Restore Pane Sizes
1. ✅ Open the application
2. ✅ Check console output: Should see "Restored column widths: X.XX, Y.YY, Z.ZZ"
3. ✅ Verify panes are the same size as when you closed the app
4. ✅ Resize again and restart to verify it works repeatedly

### Test 3: First Run (No Saved State)
1. ✅ Delete the window state JSON file
2. ✅ Open the application
3. ✅ Check console output: Should see "Column widths not saved (col0=0, col1=0, col2=0)"
4. ✅ Verify panes use default equal sizing (1.0, 1.0, 1.0)

## Debug Logging

### On Save
```
WindowLifecycleService: Saving column widths: 0.80, 1.50, 0.70
```

### On Restore (Success)
```
WindowLifecycleService: Restored column widths: 0.80, 1.50, 0.70
```

### On Restore (No Saved Widths)
```
WindowLifecycleService: Column widths not saved (col0=0, col1=0, col2=0)
```

### On Restore (MainGrid Not Found)
```
WindowLifecycleService: MainGrid not found or has insufficient columns
```

## Build Status
✅ **Build Successful** (0 errors, 22 warnings - all pre-existing)

## Impact

### User Experience
- ✅ Pane sizes now persist between sessions
- ✅ Users can set up their preferred layout once
- ✅ No need to resize panes every time the app opens

### Performance
- ✅ Minimal overhead (just reading 3 double values)
- ✅ No noticeable impact on startup or shutdown time

### Compatibility
- ✅ Backward compatible with existing window state files (missing column widths default to 0, which means use default sizing)
- ✅ Works with maximized windows
- ✅ Handles edge cases (NaN, Infinity, negative values)

## Related Code

### WindowState Class
```csharp
public class WindowState
{
    public double Width { get; set; }
    public double Height { get; set; }
    public bool IsMaximized { get; set; }
    public double Column0Width { get; set; }  // ← Already existed
    public double Column1Width { get; set; }  // ← Already existed
    public double Column2Width { get; set; }  // ← Already existed
}
```

### WindowStateStore
```csharp
public static void SetWindowState(
    double width, 
    double height, 
    bool isMaximized, 
    double col0Width = 0,   // ← Optional parameters already existed
    double col1Width = 0, 
    double col2Width = 0)
{
    // Serializes to JSON and saves
}
```

## Future Enhancements

Potential improvements:
1. Add row height persistence for horizontal splitters
2. Add toolbar/status bar size persistence
3. Add min/max width constraints for panes
4. Add preset layouts (default, compact, wide, etc.)
5. Add "Reset Layout" button to restore defaults

