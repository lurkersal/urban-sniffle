# Missing Metadata When Opening Folder Without Index File - FIXED

## Problem
When opening a folder without an `_index.json` file, the magazine volume, number, and year fields were not being set correctly. When the index file was saved, these fields were missing from the saved file.

## Root Cause
`IndexFileService.LoadFromFolder()` was throwing a `FileNotFoundException` when no index file (`_index.json` or `_index.txt`) was found in the folder:

```csharp
else
{
    throw new FileNotFoundException($"No index file found in folder: {normalizedPath}");
}
```

This exception was caught by the folder loading services, which would show an error toast and return early, preventing the metadata fields from being initialized. The folder would appear to load, but with empty/missing metadata fields.

## Solution
Modified `IndexFileService.LoadFromFolder()` to parse metadata from the folder name instead of throwing an exception when no index file exists.

The folder name parser (`FolderMetadataParser.ParseFolderMetadata`) extracts metadata from folder names in the format:
- **Format**: `"MagazineName 17-03, 1982"` 
  - Magazine name (any text)
  - 2-digit volume - 2-digit number
  - 4-digit year

If the folder name doesn't match this format, it uses the folder name as the magazine name and placeholders ("—") for volume, number, and year.

### Changes Made

**File: `/home/justin/repos/urban-sniffle/src/index-editor/Services/IndexFileService.cs`**

Modified the `LoadFromFolder()` method to handle missing index files:

```csharp
else
{
    // No index file found - parse metadata from folder name and return empty data
    _logger.LogInformation("LoadFromFolder: No index file found, parsing metadata from folder name");
    
    var folderName = Path.GetFileName(normalizedPath);
    var (magazine, volume, number, year) = IndexEditor.Shared.FolderMetadataParser.ParseFolderMetadata(folderName);
    
    _logger.LogInformation("LoadFromFolder: Parsed from folder name - Magazine: '{Magazine}', Vol: '{Volume}', Num: '{Number}', Year: '{Year}'",
        magazine, volume, number, year);
    
    return (magazine, volume, number, year, new List<ArticleLine>(), new List<MagazineLink>());
}
```

## Behavior After Fix

### When Opening a Folder Without Index File:
1. ✅ No error/exception is thrown
2. ✅ Folder name is parsed to extract metadata
3. ✅ Magazine, volume, number, and year are populated from the folder name
4. ✅ Empty article list is returned (user can create articles)
5. ✅ When saved, the index file includes the parsed metadata

### Example Folder Name Parsing:
- **Folder**: `"Creative Computing 17-03, 1982"`
  - Magazine: `"Creative Computing"`
  - Volume: `"17"`
  - Number: `"03"`
  - Year: `"1982"`

- **Folder**: `"Random Folder Name"` (doesn't match pattern)
  - Magazine: `"Random Folder Name"`
  - Volume: `"—"`
  - Number: `"—"`
  - Year: `"—"`

### User Can Edit Metadata:
After opening a folder without an index file, users can:
1. Edit the metadata fields in the UI if the folder name parsing didn't work correctly
2. Create articles
3. Save to create a new `_index.json` file with the metadata

## Testing
To test the fix:
1. Build: `dotnet build src/index-editor/IndexEditor.csproj`
2. Run: `dotnet run --project src/index-editor/IndexEditor.csproj`
3. Open a folder that does NOT contain `_index.json` or `_index.txt`
4. Verify:
   - No error is shown
   - Metadata fields are populated from folder name
   - You can create articles
   - When you save (Ctrl+S), the `_index.json` file includes the metadata

## Related Code
- **Folder Name Parser**: `/home/justin/repos/urban-sniffle/src/index-editor/Shared/FolderMetadataParser.cs`
- **Index File Service**: `/home/justin/repos/urban-sniffle/src/index-editor/Services/IndexFileService.cs`
- **File Operations Service**: `/home/justin/repos/urban-sniffle/src/index-editor/Services/FileOperationsService.cs`
- **Folder Loading Service**: `/home/justin/repos/urban-sniffle/src/index-editor/Services/FolderLoadingService.cs`

## Status
✅ FIXED - Build successful

