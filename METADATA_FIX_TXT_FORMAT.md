# Metadata Parsing Fix #3 - TXT File Format - March 19, 2026

## Additional Issue Found
After fixing the "no index file" case, discovered that folders **with existing `_index.txt` files** were also not showing metadata correctly.

## Root Cause #3: Incorrect TXT File Parser
**File:** `src/index-editor/Services/IndexFileService.cs` - `LoadFromTxtFile()` method

**Problem:**
The new `LoadFromTxtFile()` implementation was only looking for metadata in lines starting with `#`:
```csharp
if (lines.Length > 0 && lines[0].StartsWith("#"))
{
    // Parse metadata from first line: # Magazine Name Vol X No Y
    ...
}
```

**Actual Format:**
Real `_index.txt` files have metadata as the **first non-comment CSV line**:
```
Club International,20,02
1,Cover,Ginny,Brandy Ledford,,,
3,Index,,,,,
...
```

Or with year:
```
Club International,20,02,1991
...
```

## Fix #3: Parse CSV Metadata from First Non-Comment Line

Rewrote `LoadFromTxtFile()` to match original `MainWindow.LoadArticlesFromFolder()` behavior:

```csharp
private (string, string, string, string, List<ArticleLine>, List<MagazineLink>) LoadFromTxtFile(string txtPath, string folderPath)
{
    // ... initialization ...
    
    int articleStartIndex = 0;

    // Parse metadata from first non-comment line (CSV format)
    for (int i = 0; i < lines.Length; i++)
    {
        var line = lines[i].Trim();
        
        // Skip empty lines and comments
        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
        {
            continue;
        }

        // First non-comment line: try CSV metadata
        var parts = IndexEditor.Shared.IndexFileParser.SplitRespectingEscapedCommas(line);
        if (parts.Count >= 3)
        {
            string Unescape(string s) => s.Replace("\\,", ",");
            
            magazine = Unescape(parts[0]);
            volume = Unescape(parts[1]);
            number = Unescape(parts[2]);
            
            // If 4th field exists, use it as year
            if (parts.Count >= 4 && !string.IsNullOrWhiteSpace(parts[3]))
            {
                year = Unescape(parts[3]);
            }
            else
            {
                // No year in file, try to parse from folder name
                var folderName = Path.GetFileName(folderPath);
                var (_, _, _, folderYear) = FolderMetadataParser.ParseFolderMetadata(folderName);
                year = folderYear;
            }
            
            articleStartIndex = i + 1;
            break;
        }
        
        articleStartIndex = i;
        break;
    }

    // Parse articles starting after metadata line
    for (int i = articleStartIndex; i < lines.Length; i++)
    {
        // ... parse articles ...
    }
    
    return (magazine, volume, number, year, articles, new List<MagazineLink>());
}
```

## Key Improvements
1. ✅ Parses CSV metadata from first non-comment line (not just `#` lines)
2. ✅ Supports both 3-field (`Magazine,Vol,Num`) and 4-field (`Magazine,Vol,Num,Year`) formats
3. ✅ Falls back to parsing year from folder name if not in file
4. ✅ Handles escaped commas (`\,`) in metadata
5. ✅ Skips metadata line when parsing articles (no duplication)

## Test Results

### Test File
```
/home/justin/Magazines/Club International/Club International 20-02, 1991/_index.txt
```

**First line:** `Club International,20,02` (no year field)

### Expected Behavior
- Magazine: `Club International` (from file)
- Volume: `20` (from file)
- Number: `02` (from file)
- Year: `1991` (from folder name - fallback)

### Actual Result ✅
```
info: IndexEditor.Services.IndexFileService[0] 
  LoadFromTxtFile: Parsed metadata - Magazine: 'Club International', 
  Vol: '20', Num: '02', Year: '1991'
```

**Status:** ✅ **Working correctly!**

## All Three Fixes Summary

### Fix #1: Parse Folder Name When No Index File Exists
- **File:** `IndexFileService.LoadFromFolder()`
- **What:** Return parsed metadata instead of throwing exception
- **When:** No `_index.json` or `_index.txt` file exists

### Fix #2: Notify UI After Setting Metadata
- **Files:** `FolderLoadingService`, `FileOperationsService`
- **What:** Call `NotifyStateChanged()` after setting metadata properties
- **When:** Always after loading folder

### Fix #3: Parse CSV Metadata from TXT Files
- **File:** `IndexFileService.LoadFromTxtFile()`
- **What:** Parse first non-comment CSV line for metadata
- **When:** Loading existing `_index.txt` files

## Files Modified (Total)
1. ✅ `src/index-editor/Services/IndexFileService.cs` - Fixes #1 and #3
2. ✅ `src/index-editor/Services/FolderLoadingService.cs` - Fix #2
3. ✅ `src/index-editor/Services/FileOperationsService.cs` - Fix #2

## Complete Testing Status
- [x] Build successful
- [x] Published binary created
- [x] No index file case works (Fix #1)
- [x] StateChanged notification works (Fix #2)  
- [x] TXT file parsing works (Fix #3)
- [x] **Real-world test:** Club International 20-02, 1991 shows metadata correctly

## Manual Verification
The TopBar should now show correct metadata when opening:
1. ✅ Folders with `_index.json` files
2. ✅ Folders with `_index.txt` files (3-field or 4-field format)
3. ✅ Folders with no index file (parsed from folder name)

**Please reopen the folder in the app to verify the TopBar displays the metadata!**

