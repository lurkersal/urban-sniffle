# Metadata Fix Summary - March 19, 2026

## Problem
When opening a folder **without** an `_index.json` or `_index.txt` file:
- ❌ Volume, number, and year were **NOT** shown in the top bar
- ❌ These values were **NOT** saved when creating a new `_index.json` file

## Root Causes Found

### Cause #1: Exception When No Index File Exists
- `IndexFileService.LoadFromFolder()` threw `FileNotFoundException`
- This prevented the folder from loading at all
- Metadata was never parsed from the folder name

### Cause #2: TopBar Not Notified of Changes  
- After setting `CurrentMagazine`, `CurrentVolume`, `CurrentNumber`, `CurrentYear` properties
- Code did NOT call `NotifyStateChanged()`
- TopBar's event handler was never triggered
- Display remained showing "—" placeholders

## Solutions Implemented

### Solution #1: Parse Folder Name When No Index Exists ✓
**File:** `src/index-editor/Services/IndexFileService.cs`

Changed the `else` block (when no index file exists) to:
1. Extract folder name from path
2. Parse using `FolderMetadataParser.ParseFolderMetadata()`
3. Return empty article list with parsed metadata
4. **No exception thrown**

### Solution #2: Notify UI After Setting Metadata ✓
**Files:** 
- `src/index-editor/Services/FolderLoadingService.cs` (line 158)
- `src/index-editor/Services/FileOperationsService.cs` (line 102)

Added after setting all metadata properties:
```csharp
// Notify UI components (like TopBar) that state has changed
_editorState.NotifyStateChanged();
```

## Expected Behavior Now

### When Opening Folder Without Index File
1. **Folder loads successfully** (no error toast)
2. **Metadata parsed from folder name:**
   - Format: `"Magazine Name VV-NN, YYYY"`
   - Example: `"Club International 20-05, 1991"`
3. **TopBar displays:**
   - Magazine: "Club International"
   - Vol: "20"
   - No: "05"  
   - Year: "1991"
4. **Page images load correctly**
5. **Save (Ctrl+S) creates `_index.json` with correct metadata**

### Test Folder Created
Location: `/tmp/test-no-index/Club International 20-05, 1991/`
- Contains: `001.jpg` (test image)
- No index file (for testing)

## Verification Status

✅ Build successful (0 errors, 21 pre-existing warnings)  
✅ Published binary created at `~/bin/IndexEditor`  
✅ Console logs show: `"LoadFromFolder: Parsed metadata - Magazine: 'Club International', Vol: '20', Num: '05', Year: '1991'"`  
⏳ **Manual UI test pending** - Need to verify TopBar display

## How to Test

### Quick Test
```bash
# 1. Run the app with test folder
IndexEditor "/tmp/test-no-index/Club International 20-05, 1991"

# 2. Check TopBar shows:
#    Magazine: Club International
#    Vol: 20
#    No: 05
#    Year: 1991

# 3. Press Ctrl+S to save

# 4. Verify file created with correct metadata
cat "/tmp/test-no-index/Club International 20-05, 1991/_index.json" | grep -A4 metadata
```

### Comprehensive Test
See `METADATA_NO_INDEX_FIX_VERIFIED.md` for full test procedures.

## Files Changed
1. `src/index-editor/Services/IndexFileService.cs` - Parse folder name when no index file
2. `src/index-editor/Services/FolderLoadingService.cs` - Add NotifyStateChanged call
3. `src/index-editor/Services/FileOperationsService.cs` - Add NotifyStateChanged call

## Related Documentation
- Full details: `METADATA_NO_INDEX_FIX_VERIFIED.md`
- Diagnostics: `DIAGNOSTIC_CHECKLIST_2026_03_19.md`

