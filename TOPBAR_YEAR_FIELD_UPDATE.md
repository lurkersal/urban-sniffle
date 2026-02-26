# Index-Editor TopBar and Year Field Update - Complete

## Summary
Removed the "Top Bar" label from the TopBar UI and added Year field alongside Volume and Number. The year is now extracted from folder names and _index.txt files, stored in EditorState, displayed in the TopBar, and saved when writing index files.

## Changes Made

### 1. EditorState Updates

#### `src/index-editor/Shared/EditorState.cs`
- Added `CurrentYear` property to `EditorStateService` class
- Added `CurrentYear` static wrapper property to backward-compatible `EditorState` class

#### `src/index-editor/Shared/IEditorState.cs`
- Added `CurrentYear` property to interface with XML documentation

### 2. FolderMetadataParser Updates

#### `src/index-editor/Shared/FolderMetadataParser.cs`
- Changed return type from `(string mag, string vol, string num)` to `(string mag, string vol, string num, string year)`
- Now extracts year from folder names matching pattern: `"MagazineName 17-03, 1982"`
- Returns "—" placeholder if year cannot be extracted

### 3. MainWindow Updates

#### `src/index-editor/MainWindow.axaml.cs`
- Updated folder metadata parsing to handle 4-tuple with year
- Added `fileYear` variable to track year during index file loading
- Added parsing for `# Year:` comment header lines in _index.txt
- Added parsing for year as 4th field in CSV metadata line
- Stores parsed year in `EditorState.CurrentYear`

**Supported Index File Formats:**

1. **Comment headers** (legacy format):
   ```
   # Magazine: Mayfair
   # Volume: 18
   # Number: 07
   # Year: 1982
   ```

2. **CSV metadata line** (current format):
   ```
   Mayfair,18,07,1982
   ```

### 4. IndexSaver Updates

#### `src/index-editor/Shared/IndexSaver.cs`
- Updated comment to reflect 4-field format: `Magazine,Volume,Number,Year`
- Changed metadata line generation to include year as 4th field
- Checks all 4 metadata fields (magazine, volume, number, year) before writing metadata line

**Output Format:**
```
Mayfair,18,07,1982
1-8,Cover,,,,,
9-14,Editorial,The Editor's Column,,,John Smith,
15-25,Model,Sarah Connor,23,,Mike Photo,36C-24-36
```

### 5. TopBar UI Updates

#### `src/index-editor/Views/TopBar.axaml`
- **Removed** `<TextBlock Text="Top Bar" .../>` label
- **Added** `<TextBlock Name="YearText" Text="Year: —" .../>`  alongside other metadata fields
- Layout now shows: `Magazine: X | Vol: X | No: X | Year: X`

#### `src/index-editor/Views/TopBar.axaml.cs`
- Added `yearText` TextBlock reference in constructor
- Updated `RefreshMetadataDisplay()` to get and display `CurrentYear`
- Year defaults to "—" if not available

## TopBar Display

**Before:**
```
┌────────────────────────────────────────────────────────────┐
│ Top Bar      Magazine: Mayfair  Vol: 18  No: 07           │
│                                          [Buttons]          │
└────────────────────────────────────────────────────────────┘
```

**After:**
```
┌────────────────────────────────────────────────────────────┐
│    Magazine: Mayfair  Vol: 18  No: 07  Year: 1982         │
│                                          [Buttons]          │
└────────────────────────────────────────────────────────────┘
```

## Folder Name Format

The parser extracts year from folder names matching this strict pattern:
```
MagazineName 17-03, 1982
```

- Magazine name (anything)
- Space
- 2-digit volume
- Hyphen
- 2-digit number
- Comma + optional space
- 4-digit year

**Examples:**
- ✅ `Mayfair 18-07, 1982` → Magazine: Mayfair, Vol: 18, No: 07, Year: 1982
- ✅ `Club International 05-03, 1975` → Magazine: Club International, Vol: 05, No: 03, Year: 1975
- ❌ `Mayfair 18-07` → No year extracted (returns "—")
- ❌ `1982` → Not a valid format (returns "—")

## Index File Format Evolution

### Version 1 (Old - Comment Headers)
```
# Magazine: Mayfair
# Volume: 18
# Number: 07
1-8,Cover,,,,,
9-14,Editorial,The Editor's Column,,,John Smith,
```

### Version 2 (New - CSV Metadata + Year)
```
Mayfair,18,07,1982
1-8,Cover,,,,,
9-14,Editorial,The Editor's Column,,,John Smith,
```

**Backward Compatibility:**
- Index-editor can **read** both formats
- Index-editor always **writes** Version 2 format
- If year is missing from old files, it defaults to "—"

## Testing

### Manual Testing Steps

1. **Test folder with year in name:**
   ```bash
   mkdir "Mayfair 18-07, 1982"
   cd "Mayfair 18-07, 1982"
   # Open in index-editor
   ```
   - TopBar should show: `Year: 1982`

2. **Test index file with CSV metadata:**
   ```
   Mayfair,18,07,1982
   1-8,Cover,,,,,
   ```
   - TopBar should show: `Year: 1982`

3. **Test index file with comment headers:**
   ```
   # Magazine: Mayfair
   # Volume: 18
   # Number: 07
   # Year: 1982
   1-8,Cover,,,,,
   ```
   - TopBar should show: `Year: 1982`

4. **Test save functionality:**
   - Open folder with year
   - Add/edit articles
   - Save (Ctrl+S)
   - Verify _index.txt contains: `Mayfair,18,07,1982` as first line

5. **Test missing year:**
   - Folder without year in name
   - TopBar should show: `Year: —`

## Build Status

✅ **Build Successful**
- 0 Errors
- 63 Warnings (pre-existing, unrelated to this change)
- All functionality preserved

```bash
cd /home/justin/repos/urban-sniffle
dotnet build src/index-editor/IndexEditor.csproj
# Build succeeded. 0 Error(s)
```

## Files Modified

1. `src/index-editor/Shared/EditorState.cs` - Added CurrentYear property
2. `src/index-editor/Shared/IEditorState.cs` - Added CurrentYear to interface
3. `src/index-editor/Shared/FolderMetadataParser.cs` - Extract and return year
4. `src/index-editor/MainWindow.axaml.cs` - Parse year from folder and file
5. `src/index-editor/Shared/IndexSaver.cs` - Save year in metadata line
6. `src/index-editor/Views/TopBar.axaml` - Remove label, add Year display
7. `src/index-editor/Views/TopBar.axaml.cs` - Display year in UI

## Benefits

1. **Complete Metadata**: Year is now visible alongside other issue information
2. **Cleaner UI**: Removed redundant "Top Bar" label for more space
3. **Data Integrity**: Year is preserved when loading and saving index files
4. **Backward Compatible**: Can read old format files without year
5. **Forward Compatible**: Always writes year in new files

## Date Completed
February 26, 2026

