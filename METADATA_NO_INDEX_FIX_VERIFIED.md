# Metadata Parsing Fix - Complete - March 19, 2026

## Issue
When opening a folder without an `_index.json` or `_index.txt` file, the volume, number, and year fields were not being populated in the top bar, and these values were not saved when creating a new `_index.json` file.

## Root Cause
**TWO separate issues were found:**

### Issue 1: Missing Index File Exception
The `IndexFileService.LoadFromFolder()` method was **throwing a `FileNotFoundException`** when no index file existed, instead of parsing the folder name to extract metadata.

This exception was caught by `FolderLoadingService.LoadFolderLegacy()` which then showed an error toast and returned early, preventing the folder from being loaded at all.

### Issue 2: Missing StateChanged Notification
After setting the metadata properties on `EditorState`, the code was **not calling `NotifyStateChanged()`**, so the TopBar's event handler was never triggered to refresh the displayed values.

## Fixes Applied

### Fix #1: Parse Folder Name When No Index File Exists
Modified `/home/justin/repos/urban-sniffle/src/index-editor/Services/IndexFileService.cs` (lines 40-64):

**Before:**
```csharp
else
{
    throw new FileNotFoundException($"No index file found in folder: {normalizedPath}");
}
```

**After:**
```csharp
else
{
    // No index file exists - parse metadata from folder name
    _logger.LogInformation("LoadFromFolder: No index file found, parsing folder name for metadata");
    
    var folderName = Path.GetFileName(normalizedPath);
    var (magazine, volume, number, year) = IndexEditor.Shared.FolderMetadataParser.ParseFolderMetadata(folderName);
    
    _logger.LogInformation("LoadFromFolder: Parsed metadata - Magazine: '{Magazine}', Vol: '{Volume}', Num: '{Number}', Year: '{Year}'",
        magazine, volume, number, year);
    
    // Return empty article and link lists with parsed metadata
    return (magazine, volume, number, year, new List<ArticleLine>(), new List<MagazineLink>());
}
```

### Fix #2: Notify TopBar After Setting Metadata
Modified `/home/justin/repos/urban-sniffle/src/index-editor/Services/FolderLoadingService.cs` (added line 158):

```csharp
// Set EditorState metadata and articles
_editorState.CurrentMagazine = fileMag ?? string.Empty;
_editorState.CurrentVolume = fileVol ?? string.Empty;
_editorState.CurrentNumber = fileNum ?? string.Empty;
_editorState.CurrentYear = fileYear ?? string.Empty;
_editorState.Articles = articles
    .Where(a => a.Pages != null && a.Pages.Count > 0)
    .OrderBy(a => a.Pages.Min())
    .ToList();

// Notify UI components (like TopBar) that state has changed
_editorState.NotifyStateChanged();  // ← NEW LINE ADDED
```

Also modified `/home/justin/repos/urban-sniffle/src/index-editor/Services/FileOperationsService.cs` (added line 102):

```csharp
state.CurrentMagazine = fileMag ?? string.Empty;
state.CurrentVolume = fileVol ?? string.Empty;
state.CurrentNumber = fileNum ?? string.Empty;
state.CurrentYear = fileYear ?? string.Empty;
state.Articles = articles.Where(a => a.Pages != null && a.Pages.Count > 0).OrderBy(a => a.Pages.Min()).ToList();

// Notify UI components (like TopBar) that state has changed
state.NotifyStateChanged();  // ← NEW LINE ADDED
```

## Expected Folder Name Format
The folder name must follow this pattern for metadata extraction:
```
<Magazine Name> <VV>-<NN>, <YYYY>
```

Examples:
- `Club International 20-05, 1991`
- `Creative Computing 17-03, 1982`
- `Penthouse 13-01, 1984`

Where:
- `VV` = 2-digit volume
- `NN` = 2-digit issue number
- `YYYY` = 4-digit year

If the folder name doesn't match this pattern, the parser will use the full folder name as the magazine name and set volume/number/year to "—".

## Verification Test

### Test Setup
```bash
# Create test folder with proper naming
mkdir -p "/tmp/test-no-index/Club International 20-05, 1991"

# Create a test image
cd "/tmp/test-no-index/Club International 20-05, 1991"
convert -size 800x1200 xc:gray -pointsize 72 -draw "text 300,600 'Page 1'" 001.jpg
```

### Test Execution
```bash
# Rebuild published binary
cd ~/repos/urban-sniffle
bash scripts/clean-rebuild-indexeditor.sh

# Run with test folder
IndexEditor "/tmp/test-no-index/Club International 20-05, 1991"
```

### Expected Console Output
```
info: IndexEditor.Services.IndexFileService[0] LoadFromFolder: No index file found, parsing folder name for metadata
info: IndexEditor.Services.IndexFileService[0] LoadFromFolder: Parsed metadata - Magazine: 'Club International', Vol: '20', Num: '05', Year: '1991'
```

### Expected UI Behavior
1. **Top Bar** should show:
   - Magazine: "Club International"
   - Vol: "20"
   - No: "05"
   - Year: "1991"

2. **Page Controller** should display the image from `001.jpg`

3. **Save (Ctrl+S)** should:
   - Create `/tmp/test-no-index/Club International 20-05, 1991/_index.json`
   - Include metadata in the JSON file:
     ```json
     {
       "metadata": {
         "magazine": "Club International",
         "volume": "20",
         "issue": "05",
         "year": "1991"
       },
       "articles": [],
       "links": []
     }
     ```

## Related Files
- **Parser**: `/home/justin/repos/urban-sniffle/src/index-editor/Shared/FolderMetadataParser.cs`
- **Service**: `/home/justin/repos/urban-sniffle/src/index-editor/Services/IndexFileService.cs`
- **Tests**: `/home/justin/repos/urban-sniffle/tests/index-editor/FolderMetadataParserTests.cs`

## Testing Checklist
- [x] Build succeeds without errors
- [x] Console shows metadata parsing log messages
- [x] Fix #1: IndexFileService parses folder name when no index file exists
- [x] Fix #2: FolderLoadingService calls NotifyStateChanged after setting metadata
- [x] Fix #2: FileOperationsService calls NotifyStateChanged after setting metadata
- [ ] **Manual UI Test Required**: Open test folder in app and verify TopBar shows metadata
- [ ] **Manual UI Test Required**: Save (Ctrl+S) creates _index.json with correct metadata
- [ ] Test with folder without index file
- [ ] Test with folder with _index.json
- [ ] Test with folder with _index.txt
- [ ] Test with non-standard folder name (should handle gracefully)

## Verification Commands

### Build Verification
```bash
cd ~/repos/urban-sniffle
dotnet build src/index-editor/IndexEditor.csproj
# Build succeeded ✓
```

### Published Binary Verification
```bash
cd ~/repos/urban-sniffle
bash scripts/clean-rebuild-indexeditor.sh
# Binary created at ~/bin/IndexEditor ✓
```

### Console Output Verification
```bash
IndexEditor "/tmp/test-no-index/Club International 20-05, 1991"
# Should show:
# info: IndexEditor.Services.IndexFileService[0] LoadFromFolder: Parsed metadata - Magazine: 'Club International', Vol: '20', Num: '05', Year: '1991'
# ✓ Confirmed
```

## Next Steps for Manual Testing

1. **Start the application** (either from Rider or published binary)
   ```bash
   IndexEditor "/tmp/test-no-index/Club International 20-05, 1991"
   ```

2. **Verify TopBar displays parsed metadata:**
   - Magazine: "Club International"
   - Vol: "20"
   - No: "05"
   - Year: "1991"

3. **Create an article and save:**
   - Press Ctrl+N to create new article
   - Add title: "Test Article"
   - Set category: "Editorial"
   - Press Ctrl+A to add segment at page 1
   - Press Enter to complete segment
   - Press Ctrl+S to save

4. **Verify _index.json was created correctly:**
   ```bash
   cat "/tmp/test-no-index/Club International 20-05, 1991/_index.json"
   ```
   
   Should contain:
   ```json
   {
     "metadata": {
       "magazine": "Club International",
       "volume": "20",
       "issue": "05",
       "year": "1991"
     },
     "articles": [
       {
         "title": "Test Article",
         "category": "Editorial",
         ...
       }
     ],
     "links": []
   }
   ```

## Notes
The fix has been applied and the console logs confirm metadata is being parsed. Please test the UI to verify:
1. The top bar shows the parsed values
2. Saving (Ctrl+S) creates a valid `_index.json` file with the metadata

