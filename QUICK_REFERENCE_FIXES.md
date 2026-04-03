# Quick Reference - Recent Bug Fixes

## Summary
Two critical bugs were fixed on March 18, 2026:

1. **Page image not loading after opening a folder**
2. **Missing metadata when opening folder without index file**

---

## Fix #1: Page Image Not Loading
**Status**: ✅ Fixed  
**File**: `src/index-editor/Views/PageControllerView.axaml.cs`

### What was wrong?
PageControllerView was listening to StateChanged events on the wrong EditorState instance.

### How to verify the fix works:
1. Run the app
2. Open a folder with images
3. Image should display immediately
4. Check console for: `"Re-subscribed to StateChanged"`

---

## Fix #2: Missing Metadata Without Index File
**Status**: ✅ Fixed  
**File**: `src/index-editor/Services/IndexFileService.cs`

### What was wrong?
Opening a folder without `_index.json` threw an error and didn't populate magazine/volume/number/year fields.

### How to verify the fix works:
1. Create a test folder: `"Creative Computing 17-03, 1982"` (no index file)
2. Add some JPG files to it
3. Open the folder in the app
4. Check metadata fields are populated:
   - Magazine: "Creative Computing"
   - Volume: "17"
   - Number: "03"
   - Year: "1982"
5. Create an article and save (Ctrl+S)
6. Verify `_index.json` contains the metadata

---

## Folder Naming Convention
For automatic metadata parsing, use this format:
```
Magazine Name VV-NN, YYYY
```

Examples:
- ✅ `"Creative Computing 17-03, 1982"`
- ✅ `"Mayfair 10-05, 1975"`
- ❌ `"Magazine Vol17 No3"` (won't parse, but won't error)
- ❌ `"Random Folder"` (won't parse, uses folder name as magazine)

---

## Build & Test

### Build:
```bash
dotnet build src/index-editor/IndexEditor.csproj
```

### Run:
```bash
dotnet run --project src/index-editor/IndexEditor.csproj
```

### Test FolderMetadataParser:
```bash
dotnet test tests/index-editor/IndexEditor.Tests.csproj --filter "FolderMetadataParserTests"
```

---

## Documentation Files
- `PAGE_IMAGE_NO_LOAD_FIX.md` - Details about image loading fix
- `MISSING_METADATA_NO_INDEX_FILE_FIX.md` - Details about metadata parsing fix
- `TEST_SCENARIO_NO_INDEX_FILE.md` - Test scenarios for metadata fix
- `SESSION_SUMMARY_2026_03_18.md` - Complete session summary

---

## Common Issues

### "No folder opened" still showing after fix?
- Make sure you rebuilt after pulling the latest code
- Check console for "Re-subscribed to StateChanged" message
- If not present, SetEditorState may not be getting called

### Metadata fields showing "—" after opening folder?
- Check folder name format matches: `"Name VV-NN, YYYY"`
- You can manually edit the fields and save
- Next time you open that folder, it will load from `_index.json`

### Still getting "Failed to load index file" error?
- Make sure you pulled the latest IndexFileService.cs changes
- Rebuild the project
- Check that FolderMetadataParser.cs exists in the project

