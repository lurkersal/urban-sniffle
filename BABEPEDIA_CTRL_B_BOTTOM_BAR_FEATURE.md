# Ctrl+B Babepedia Check - Bottom Bar Clickable Link Feature

**Date**: March 20, 2026  
**Status**: ✅ **IMPLEMENTED** - Ctrl+B now checks Babepedia and displays clickable link in bottom status bar

---

## Feature Overview

When the user presses **Ctrl+B**, the application will:
1. Check if the selected article has a Babepedia page for its model
2. Display the result in the **bottom status bar**
3. Show a **clickable URL link** if the page exists (opens in default browser)
4. Show "not found" message if the link doesn't exist

---

## User Experience

### When Link Exists ✅
- **Status text**: `✓ Babepedia: [ModelName]`
- **Clickable link**: Blue underlined URL that opens in default browser when clicked
- **Example**: `✓ Babepedia: Anna Nicole Smith` [https://www.babepedia.com/babe/Anna_Nicole_Smith]
  - The URL is clickable and styled with blue text and underline
  - Cursor changes to hand pointer on hover
  - Click to open in browser

### When Link Doesn't Exist ❌
- **Status text**: `✗ Babepedia: [ModelName] not found`
- **No link displayed**

---

## Implementation Details

### Files Modified

1. **ArticleKeyboardHandler.cs** - `/home/justin/repos/urban-sniffle/src/index-editor/Services/KeyboardHandlers/ArticleKeyboardHandler.cs`
   - Added `HandleCtrlB()` method to handle Ctrl+B keyboard shortcut
   - Updated `UpdateStatusBar()` to accept URL parameter and show/hide clickable link
   - Added `OnStatusLinkClicked()` event handler to open URL in browser
   - Integrated with existing `BabepediaService.CheckModelPageAsync()`

2. **MainWindow.axaml** - `/home/justin/repos/urban-sniffle/src/index-editor/MainWindow.axaml`
   - Added `StatusLink` TextBlock with clickable styling (blue, underlined, hand cursor)
   - Positioned next to status text in the status bar
   - Added Ctrl+B documentation to the help overlay (F1)

---

## How It Works

### User Flow

1. User selects an article in the article list (Model or Cover category)
2. User presses **Ctrl+B**
3. Application shows "Checking Babepedia for [ModelName]..." in status bar
4. Application performs async HTTP check to babepedia.com
5. Status bar updates with result:
   - ✅ **If found**: Status text + clickable blue link that opens in browser
   - ❌ **If not found**: Status text only (no link)

### Edge Cases Handled

- **No article selected**: Status bar shows "No article selected"
- **Non-Model/Cover article**: Status bar shows "Article '[Title]' is not a Model or Cover article"
- **No model name**: Status bar shows "No model name found for '[Title]'"
- **Network error**: Status bar shows "Error checking Babepedia for [ModelName]"

---

## Code Structure

### HandleCtrlB Method

```csharp
private void HandleCtrlB(KeyEventArgs e)
{
    // 1. Get selected article
    var article = vm?.SelectedArticle ?? _editorState.ActiveArticle;
    
    // 2. Validate article (must be Model or Cover with a model name)
    if (!valid) { UpdateStatusBar(message, null); return; }
    
    // 3. Show "Checking..." message
    UpdateStatusBar($"Checking Babepedia for {modelName}...", null);
    
    // 4. Perform async check
    Task.Run(async () => {
        var (exists, url) = await BabepediaService.CheckModelPageAsync(modelName);
        
        // 5. Update status bar on UI thread with result
        Dispatcher.UIThread.Post(() => {
            if (exists)
                UpdateStatusBar($"✓ Babepedia: {modelName}", url); // With clickable link
            else
                UpdateStatusBar($"✗ Babepedia: {modelName} not found", null); // No link
        });
    });
}
```

### UpdateStatusBar Method

```csharp
private void UpdateStatusBar(string message, string? clickableUrl)
{
    // Update status text
    statusText.Text = message;
    
    // Show/hide clickable link
    if (!string.IsNullOrWhiteSpace(clickableUrl))
    {
        statusLink.Text = clickableUrl;
        statusLink.IsVisible = true;
        statusLink.Tag = clickableUrl; // Store for click handler
        statusLink.PointerPressed += OnStatusLinkClicked; // Wire up click
    }
    else
    {
        statusLink.IsVisible = false;
        statusLink.PointerPressed -= OnStatusLinkClicked; // Remove handler
    }
}
```

### OnStatusLinkClicked Event Handler

```csharp
private void OnStatusLinkClicked(object? sender, PointerPressedEventArgs e)
{
    if (sender is TextBlock tb && tb.Tag is string url)
    {
        // Open URL in default browser
        var psi = new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        };
        Process.Start(psi);
    }
}
```

---

## Status Bar UI Structure

The status bar in `MainWindow.axaml` now has two text elements:

```xml
<Border Grid.Row="2" Grid.ColumnSpan="3" Background="#F5F5F5" ...>
    <Grid ColumnDefinitions="Auto,Auto,*,Auto,Auto">
        <!-- Status message (e.g., "✓ Babepedia: Anna Nicole Smith") -->
        <TextBlock x:Name="StatusText" Grid.Column="0" ... />
        
        <!-- Clickable link (e.g., "https://www.babepedia.com/babe/Anna_Nicole_Smith") -->
        <TextBlock x:Name="StatusLink" Grid.Column="1" 
                   Foreground="Blue" TextDecorations="Underline" Cursor="Hand"
                   IsVisible="False" Margin="10,0,0,0" />
        
        <!-- Other status bar elements... -->
    </Grid>
</Border>
```

**Styling**:
- `Foreground="Blue"` - Blue text color
- `TextDecorations="Underline"` - Underlined text
- `Cursor="Hand"` - Hand cursor on hover
- `IsVisible="False"` - Hidden by default, shown only when link exists

---

## User-Visible Changes

### Help Overlay (F1)

Added to keyboard shortcuts list:
```
Ctrl+B: Check Babepedia for selected article's model
```

### Status Bar Display Examples

**Link Found**:
```
✓ Babepedia: Anna Nicole Smith  [https://www.babepedia.com/babe/Anna_Nicole_Smith] ← Clickable
```

**Link Not Found**:
```
✗ Babepedia: Jane Doe not found
```

**Checking**:
```
Checking Babepedia for Anna Nicole Smith...
```

---

## Testing Scenarios

### ✅ Happy Path - Link Found
1. Select a Model article with a valid model name (e.g., "Anna Nicole Smith")
2. Press Ctrl+B
3. Verify status bar shows "Checking..." then shows the status message
4. Verify blue underlined link appears next to the message
5. Hover over link - cursor should change to hand
6. Click the link - browser should open to Babepedia page

### ✅ Model Not Found
1. Select a Model article with an unknown model name
2. Press Ctrl+B
3. Verify status bar shows "not found" message
4. Verify no link is displayed

### ✅ Non-Model Article
1. Select an Editorial or other non-Model/Cover article
2. Press Ctrl+B
3. Verify status bar shows appropriate message
4. Verify no link is displayed

### ✅ No Selection
1. Deselect all articles (if possible)
2. Press Ctrl+B
3. Verify status bar shows "No article selected"
4. Verify no link is displayed

### ✅ Link Click
1. After finding a valid link, click it
2. Verify default browser opens to the Babepedia page
3. Verify browser navigation works correctly

---

## Cross-Platform URL Opening

The implementation uses `Process.Start()` with `UseShellExecute = true` which works across platforms:
- **Windows**: Opens URL in default browser
- **Linux**: Uses xdg-open to open URL
- **macOS**: Uses open command to open URL

---

## Build Status

✅ **Build successful**: 0 errors, 22 warnings (all pre-existing)

```bash
dotnet build src/index-editor/IndexEditor.csproj
# Build succeeded.
#     0 Error(s)
#    22 Warning(s)
```

---

## Future Enhancements (Optional)

Possible improvements for future versions:

1. **Copy to clipboard**: Right-click context menu to copy URL
2. **Persistent display**: Keep last check result until next check or article change
3. **Icon indicator**: Add visual icon (✓ or ✗) before the status message
4. **History**: Keep track of recent checks for quick reference
5. **Auto-check**: Optionally check automatically when Model article is selected
6. **Tooltip**: Show full URL in tooltip on hover
7. **Status bar section**: Dedicate a section of status bar for Babepedia results

---

## Related Files

- `/home/justin/repos/urban-sniffle/src/index-editor/Services/BabepediaService.cs` - HTTP checking service
- `/home/justin/repos/urban-sniffle/src/index-editor/Services/KeyboardHandlers/ArticleKeyboardHandler.cs` - Keyboard handler
- `/home/justin/repos/urban-sniffle/src/index-editor/MainWindow.axaml` - UI definition with status bar
- `/home/justin/repos/urban-sniffle/src/index-editor/Views/EditorStateViewModel.cs` - Has existing `CheckBabepediaAsync()` (shows dialog)

---

## Summary

✅ **Feature complete** - Ctrl+B now checks Babepedia and displays **clickable link** in the bottom status bar  
✅ **Clickable URL** - Blue underlined link opens in default browser when clicked  
✅ **Non-blocking** - Async HTTP check doesn't freeze UI  
✅ **Clear feedback** - Status messages for all scenarios  
✅ **Cross-platform** - URL opening works on Windows, Linux, and macOS  
✅ **Well integrated** - Reuses existing BabepediaService  
✅ **Documented** - Added to help overlay (F1)  

The feature is ready to use! Press **Ctrl+B** on any Model/Cover article to check if the model has a Babepedia page. If found, the URL will appear as a clickable blue link in the bottom status bar that opens in your default browser.

---

**End of Document**

### Files Modified

1. **ArticleKeyboardHandler.cs** - `/home/justin/repos/urban-sniffle/src/index-editor/Services/KeyboardHandlers/ArticleKeyboardHandler.cs`
   - Added `HandleCtrlB()` method to handle Ctrl+B keyboard shortcut
   - Added `UpdateStatusBar()` helper method to update the bottom status bar
   - Integrated with existing `BabepediaService.CheckModelPageAsync()`

2. **MainWindow.axaml** - `/home/justin/repos/urban-sniffle/src/index-editor/MainWindow.axaml`
   - Added Ctrl+B documentation to the help overlay (F1)

---

## How It Works

### User Flow

1. User selects an article in the article list (Model or Cover category)
2. User presses **Ctrl+B**
3. Application shows "Checking Babepedia for [ModelName]..." in status bar
4. Application performs async HTTP check to babepedia.com
5. Status bar updates with result:
   - ✅ **If found**: `✓ Babepedia: [ModelName] - https://www.babepedia.com/babe/[ModelName]`
   - ❌ **If not found**: `✗ Babepedia: [ModelName] not found`

### Edge Cases Handled

- **No article selected**: Status bar shows "No article selected"
- **Non-Model/Cover article**: Status bar shows "Article '[Title]' is not a Model or Cover article"
- **No model name**: Status bar shows "No model name found for '[Title]'"
- **Network error**: Status bar shows "Error checking Babepedia for [ModelName]"

---

## Code Structure

### HandleCtrlB Method

```csharp
private void HandleCtrlB(KeyEventArgs e)
{
    // 1. Get selected article
    var article = vm?.SelectedArticle ?? _editorState.ActiveArticle;
    
    // 2. Validate article (must be Model or Cover with a model name)
    if (!valid) { UpdateStatusBar(message); return; }
    
    // 3. Show "Checking..." message
    UpdateStatusBar($"Checking Babepedia for {modelName}...");
    
    // 4. Perform async check
    Task.Run(async () => {
        var (exists, url) = await BabepediaService.CheckModelPageAsync(modelName);
        
        // 5. Update status bar on UI thread with result
        Dispatcher.UIThread.Post(() => {
            if (exists)
                UpdateStatusBar($"✓ Babepedia: {modelName} - {url}");
            else
                UpdateStatusBar($"✗ Babepedia: {modelName} not found");
        });
    });
}
```

### Integration with BabepediaService

The existing `BabepediaService.CheckModelPageAsync()` is reused:
- Performs HTTP GET request to `https://www.babepedia.com/babe/{modelName}`
- Detects redirects to `/search/` (indicates model not found)
- Returns `(bool exists, string url)` tuple

---

## Status Bar UI Element

The bottom status bar is defined in `MainWindow.axaml`:

```xml
<!-- Status Bar -->
<Border Grid.Row="2" Grid.ColumnSpan="3" Background="#F5F5F5" ...>
    <Grid ColumnDefinitions="Auto,*,Auto,Auto">
        <TextBlock x:Name="StatusText" Grid.Column="0" Text="Ready" ... />
        <ProgressBar x:Name="LinkDiscoveryProgress" ... />
        <Button x:Name="StartLinkDiscoveryBtn" ... />
        <TextBlock x:Name="LinkDiscoveryStatus" ... />
    </Grid>
</Border>
```

The `StatusText` TextBlock (left side) is used to display Babepedia check results.

---

## User-Visible Changes

### Help Overlay (F1)

Added to keyboard shortcuts list:
```
Ctrl+B: Check Babepedia for selected article's model
```

### Status Bar Messages

Examples:
- `Checking Babepedia for Anna Nicole Smith...`
- `✓ Babepedia: Anna Nicole Smith - https://www.babepedia.com/babe/Anna_Nicole_Smith`
- `✗ Babepedia: Jane Doe not found`
- `No article selected`
- `Article 'Editorial' is not a Model or Cover article`

---

## Testing Scenarios

### ✅ Happy Path
1. Select a Model article with a valid model name
2. Press Ctrl+B
3. Verify status bar shows "Checking..." then shows the URL

### ✅ Model Not Found
1. Select a Model article with an unknown model name
2. Press Ctrl+B
3. Verify status bar shows "not found" message

### ✅ Non-Model Article
1. Select an Editorial or other non-Model/Cover article
2. Press Ctrl+B
3. Verify status bar shows appropriate message

### ✅ No Selection
1. Deselect all articles (if possible)
2. Press Ctrl+B
3. Verify status bar shows "No article selected"

---

## Async Behavior

The Babepedia check is **non-blocking**:
- HTTP request runs on background thread via `Task.Run()`
- Status bar updates happen on UI thread via `Dispatcher.UIThread.Post()`
- User can continue working while check is in progress
- Timeout is 5 seconds (configured in `BabepediaService`)

---

## Build Status

✅ **Build successful**: 0 errors, 22 warnings (all pre-existing)

```bash
dotnet build src/index-editor/IndexEditor.csproj
# Build succeeded.
#     0 Error(s)
#    22 Warning(s)
```

---

## Future Enhancements (Optional)

Possible improvements for future versions:

1. **Clickable link**: Make the status bar URL clickable to open in browser
2. **Copy to clipboard**: Add context menu to copy URL
3. **Persistent display**: Keep last check result until next check
4. **Icon indicator**: Add visual icon (✓ or ✗) for better visibility
5. **History**: Keep track of recent checks for quick reference
6. **Auto-check**: Optionally check automatically when article is selected

---

## Related Files

- `/home/justin/repos/urban-sniffle/src/index-editor/Services/BabepediaService.cs` - HTTP checking service
- `/home/justin/repos/urban-sniffle/src/index-editor/Services/KeyboardHandlers/ArticleKeyboardHandler.cs` - Keyboard handler
- `/home/justin/repos/urban-sniffle/src/index-editor/MainWindow.axaml` - UI definition with status bar
- `/home/justin/repos/urban-sniffle/src/index-editor/Views/EditorStateViewModel.cs` - Has existing `CheckBabepediaAsync()` (shows dialog)

---

## Summary

✅ **Feature complete** - Ctrl+B now checks Babepedia and displays results in the bottom status bar  
✅ **Non-blocking** - Async HTTP check doesn't freeze UI  
✅ **Clear feedback** - Status messages for all scenarios  
✅ **Well integrated** - Reuses existing BabepediaService  
✅ **Documented** - Added to help overlay (F1)  

The feature is ready to use! Press **Ctrl+B** on any Model/Cover article to check if the model has a Babepedia page, and the result will appear in the bottom status bar with the URL if found.

---

**End of Document**


