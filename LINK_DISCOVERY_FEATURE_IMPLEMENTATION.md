# Link Discovery Feature Implementation

## Overview

Successfully integrated OCR-based link discovery functionality from the find-links project into the index-editor. The editor now automatically scans page images in the background to find references to other magazine issues and displays link indicators in the UI.

## Features Implemented

### 1. Background Link Discovery Service
- **File**: `/src/index-editor/Services/LinkDiscoveryService.cs`
- Runs OCR (Tesseract) on page images in background
- Finds magazine issue references using regex patterns (Vol X No Y format)
- Reports progress via events
- Cancellable/restartable

### 2. Status Bar with Progress Indicator
- **File**: `/src/index-editor/MainWindow.axaml`
- Added status bar at bottom of window with:
  - Status text showing current activity
  - Progress bar showing scan progress
  - Link count when complete

### 3. Link Icon in Page Controller
- **File**: `/src/index-editor/Views/PageControllerView.axaml`
- Shows 🔗 icon when current page has discovered links
- Displays count of links found on page
- Positioned in bottom-right of navigation area
- **Hover tooltip** shows detailed list of all discovered links:
  - Magazine name
  - Volume and issue numbers
  - Formatted as: "• Magazine Vol.X No.Y"

### 4. Link Storage and Persistence
- Links are stored in `_index.json` file
- Loaded when opening an issue
- Saved automatically with index
- Format:
```json
{
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

## Technical Implementation

### MainWindow Changes
- Added `LinkDiscoveryService` instance
- Added `_discoveredLinks` dictionary (page → links)
- Event handlers for progress, discovery, and completion
- Starts discovery automatically when folder is loaded
- Loads existing links from JSON file
- Saves links when saving index

### ImageHelper Enhancement
- Added `GetAllImageFiles()` method
- Returns list of (pageNumber, imagePath) tuples
- Scans folder for .jpg, .png, .jpeg files
- Parses numeric filenames to page numbers

### PageControllerView Enhancement
- Added `UpdateDiscoveredLinks()` method
- Added `UpdateLinkIndicator()` private method
- Updates link icon visibility based on current page
- Shows count of links on current page

### IndexSaver Update
- Added optional `links` parameter
- Passes links to `IndexJsonSerializer.SaveToJson()`
- Backward compatible (links default to null)

## User Experience

### When Opening a Folder:
1. Index loads normally
2. Any existing links are loaded from JSON
3. Status bar shows "Discovering links... X%"
4. Progress bar animates during scan
5. When complete: "Found X link(s) on Y page(s)"

### When Viewing a Page with Links:
- 🔗 icon appears in page controller bottom-right
- Shows count: "1 link" or "X links"
- **Hover over icon** to see detailed tooltip overlay:
  - Shows all discovered links for that page
  - Format: "• Magazine Vol.X No.Y"
  - Example: "• Club International Vol.16 No.12"

### When Saving:
- Links are automatically included in saved JSON
- No user action required

## Dependencies

### Required Software:
- **Tesseract OCR** must be installed on system
  - Ubuntu/Debian: `sudo apt-get install tesseract-ocr`
  - Fedora: `sudo dnf install tesseract`
  - macOS: `brew install tesseract`

### Pattern Recognition:
The OCR looks for patterns like:
- "Vol 12 No 3"
- "Vol. 12 No. 3"
- "Volume 12 Number 3"
- Various spacing/punctuation variations

## Performance Considerations

- Link discovery runs in background thread
- Does not block UI
- Can be cancelled if user closes folder
- OCR is CPU-intensive but runs at low priority
- Progress updates sent to UI thread safely

## Future Enhancements

Potential improvements:
1. **Manual Link Editing**: UI to add/edit/remove links
2. **Link Navigation**: Click link icon to see/navigate to referenced issue
3. **Link Validation**: Verify referenced issues exist in database
4. **Confidence Scores**: Show OCR confidence for each link
5. **Skip Already Scanned**: Don't re-scan pages that already have links
6. **Batch Processing**: Queue multiple folders for link discovery
7. **Link Types**: Categorize links (reference, continuation, advertisement)

## Files Modified

1. `/src/common/Shared/IndexFileModels.cs` - Added MagazineLink class
2. `/src/common/Shared/IndexJsonSerializer.cs` - Added links parameter
3. `/src/index-editor/Services/LinkDiscoveryService.cs` - NEW
4. `/src/index-editor/Shared/ImageHelper.cs` - Added GetAllImageFiles
5. `/src/index-editor/Shared/IndexSaver.cs` - Added links parameter
6. `/src/index-editor/MainWindow.axaml` - Added status bar
7. `/src/index-editor/MainWindow.axaml.cs` - Integration logic
8. `/src/index-editor/Views/PageControllerView.axaml` - Added link indicator
9. `/src/index-editor/Views/PageControllerView.axaml.cs` - Link display logic

## Testing Recommendations

1. **Basic Functionality**:
   - Open folder with magazine images
   - Verify status bar shows progress
   - Check link count when complete

2. **Link Detection**:
   - Use test images with visible "Vol X No Y" text
   - Verify links are discovered
   - Check link icon appears on correct pages

3. **Persistence**:
   - Discover links, save, close
   - Reopen folder
   - Verify links still present

4. **Performance**:
   - Test with large folder (100+ pages)
   - Verify UI remains responsive
   - Check memory usage

5. **Edge Cases**:
   - Folder with no text (should complete quickly)
   - Folder with many links per page
   - Missing Tesseract installation

## Known Limitations

1. **Tesseract Required**: Feature won't work without Tesseract installed
2. **English Only**: OCR tuned for English text
3. **No Error Recovery**: If OCR fails, silently continues
4. **No Manual Editing**: Can't add/edit links via UI yet
5. **Simple Pattern**: Only detects standard "Vol/No" format

## Documentation

See also:
- `MAGAZINE_LINKS_DOCUMENTATION.md` - JSON format and API usage
- `MAGAZINE_LINKS_EXAMPLE.json` - Example JSON with links
- `MAGAZINE_LINKS_IMPLEMENTATION_SUMMARY.md` - Original links feature summary

