# Link Discovery Auto-Save and Manual Start Enhancement

## Overview
Enhanced the link discovery feature to:
1. Save discovered links when saving the index file
2. Load existing links from the index file
3. Show a "Scan for Links" button instead of auto-starting discovery when links already exist
4. Allow manual re-scanning of links

## Changes Made

### 1. MainWindow - Static Instance
**File**: `/src/index-editor/MainWindow.axaml.cs`

Added static instance reference to allow accessing discovered links from anywhere:
```csharp
public static MainWindow? Instance { get; private set; }
```

Set in constructor:
```csharp
Instance = this;
```

### 2. Updated All SaveIndex Calls
Updated all places that call `IndexSaver.SaveIndex()` to include discovered links:

#### Files Modified:
1. **TopBar.axaml.cs** - 3 occurrences
2. **MainWindowViewModel.cs** - 1 occurrence
3. **FileKeyboardHandler.cs** - 1 occurrence
4. **MainWindow.axaml.cs** - 1 occurrence (already done)

**Pattern:**
```csharp
var links = MainWindow.Instance?.GetDiscoveredLinks();
IndexSaver.SaveIndex(folder, links);
```

### 3. Status Bar - Added Start Discovery Button
**File**: `/src/index-editor/MainWindow.axaml`

Added button in status bar:
```xml
<Button x:Name="StartLinkDiscoveryBtn" Grid.Column="2" 
        Content="Scan for Links" Margin="10,0" 
        IsVisible="False" Padding="8,4" />
```

### 4. Conditional Link Discovery Logic
**File**: `/src/index-editor/MainWindow.axaml.cs`

#### When Loading Issue:
- If links exist in JSON file → Show "Re-scan for Links" button
- If no links exist → Auto-start discovery

```csharp
if (_discoveredLinks.Count > 0)
{
    // Links already exist, show button to optionally re-scan
    startBtn.IsVisible = true;
    startBtn.Content = "Re-scan for Links";
    statusText.Text = $"Loaded {linkCount} link(s) from index file";
}
else
{
    // No links exist, auto-start discovery
    _linkDiscoveryService?.StartDiscovery(folder, magazineName);
}
```

#### Button Click Handler:
When user clicks "Scan for Links" or "Re-scan for Links":
1. Hide the button
2. Clear existing links
3. Update PageControllerView
4. Start fresh discovery

```csharp
startBtn.Click += (s, e) =>
{
    startBtn.IsVisible = false;
    
    // Clear existing links when user manually starts/re-scans
    _discoveredLinks.Clear();
    var pcView = this.FindControl<Views.PageControllerView>("PageControllerControl");
    pcView?.UpdateDiscoveredLinks(_discoveredLinks);
    
    _linkDiscoveryService?.StartDiscovery(folder, magazineName);
};
```

## User Experience

### Scenario 1: Opening Issue WITHOUT Existing Links
1. User opens folder (Ctrl+O)
2. Index loads
3. Status bar shows: "Discovering links... X%"
4. Progress bar animates
5. When complete: "Found X link(s) on Y page(s)"
6. Links saved automatically when saving index

### Scenario 2: Opening Issue WITH Existing Links
1. User opens folder (Ctrl+O)
2. Index loads with existing links
3. Status bar shows: "Loaded X link(s) from index file"
4. **"Re-scan for Links" button appears** in status bar
5. Link icons appear on pages with links
6. No automatic scanning

### Scenario 3: Re-scanning Links
1. User clicks "Re-scan for Links" button
2. Button disappears
3. Existing links cleared
4. Status bar shows: "Discovering links... X%"
5. Fresh scan begins
6. New links discovered and displayed

### Scenario 4: Saving Index
1. User saves index (Ctrl+S or Save button)
2. Discovered links automatically included in JSON
3. Next time issue is opened, links are loaded

## Technical Details

### Link Persistence Format
Links saved in `_index.json`:
```json
{
  "metadata": { ... },
  "articles": [ ... ],
  "links": [
    {
      "page": 10,
      "magazine": "Club International",
      "volume": "16",
      "issue": "12"
    }
  ]
}
```

### Button States
- **Hidden**: During discovery or when no folder open
- **"Scan for Links"**: When no links loaded (first time)
- **"Re-scan for Links"**: When links already exist

### Status Text States
- "Ready": Default state
- "Discovering links... X%": During scan
- "Found X link(s) on Y page(s)": After auto-discovery
- "Loaded X link(s) from index file": After loading existing links

## Benefits

1. **Faster Loading**: Don't re-scan if links already exist
2. **Bandwidth Savings**: OCR is CPU-intensive, skip if not needed
3. **User Control**: Manual re-scan option if desired
4. **Data Persistence**: Links survive save/load cycles
5. **Clear Feedback**: User knows if links are loaded vs. discovered

## Files Modified

1. `/src/index-editor/MainWindow.axaml` - Added button
2. `/src/index-editor/MainWindow.axaml.cs` - Logic changes
3. `/src/index-editor/Views/TopBar.axaml.cs` - Save with links (3 places)
4. `/src/index-editor/Views/MainWindowViewModel.cs` - Save with links
5. `/src/index-editor/Services/KeyboardHandlers/FileKeyboardHandler.cs` - Save with links

## Build Status
✅ **Build Successful**
- 0 Errors
- 63 Warnings (all pre-existing)
- No new issues introduced

## Testing Recommendations

1. **First Time Open**:
   - Open folder without links
   - Verify auto-discovery starts
   - Verify progress shown
   - Verify links saved

2. **Subsequent Open**:
   - Close and reopen same folder
   - Verify "Loaded X links" message
   - Verify "Re-scan for Links" button shown
   - Verify links displayed without scanning

3. **Manual Re-scan**:
   - Click "Re-scan for Links"
   - Verify button disappears
   - Verify progress shows
   - Verify new scan completes

4. **Save/Load Cycle**:
   - Discover links
   - Save index
   - Close editor
   - Reopen folder
   - Verify links loaded

5. **Empty Links**:
   - Open folder with no link references
   - Verify scan completes quickly
   - Verify empty links saved
   - Verify no "Re-scan" button on reload

## Known Behaviors

1. **Auto-scan only on first open**: Links only auto-discovered when not in JSON
2. **Re-scan clears old links**: Manual re-scan starts fresh
3. **Links saved on any save**: Ctrl+S, Save button, or close dialog
4. **Button shows after load**: Button appears after loading, not during
5. **No partial updates**: Discovery completes before saving

## Future Enhancements

Potential improvements:
1. **Background re-scan**: Option to re-scan without clearing existing
2. **Link expiry**: Mark links as "old" after X days
3. **Selective re-scan**: Re-scan only specific pages
4. **Link diff view**: Show what changed in re-scan
5. **Auto re-scan**: Option to always re-scan on open
6. **Link statistics**: Show discovery accuracy/confidence

