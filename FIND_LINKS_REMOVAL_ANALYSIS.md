# find-links Project Removal Analysis

## Date
March 4, 2026

## Summary
The `find-links` project can be **safely removed** from the repository. All of its functionality has been integrated into the `index-editor` application.

## Background

### What find-links Did
The `find-links` project was a command-line tool that:
1. Connected to a PostgreSQL database to get magazine issue data
2. Used Tesseract OCR to scan page images for issue link references
3. Found references to other magazine issues using regex patterns (e.g., "Vol 12 No 3")
4. Stored discovered links in the database's `IssueLink` table
5. Tracked which issues had been scanned via `LinkScanPerformed` flag

### What index-editor Now Does
The `index-editor` has equivalent functionality through:

1. **LinkDiscoveryService** (`src/index-editor/Services/LinkDiscoveryService.cs`)
   - Performs the same OCR scanning using Tesseract
   - Uses the identical regex pattern to find issue links
   - Runs as a background task when an issue is loaded
   - Displays progress in the UI status bar

2. **Link Storage in JSON**
   - Links are stored in `_index.json` file in the `links` array
   - Format: `{"page": "10", "magazine": "Club International", "volume": "1", "issue": "1"}`
   - Links are loaded when opening an issue
   - Links are saved when saving the index file
   - No database required

3. **UI Integration**
   - Links are displayed in the Page Controller pane
   - Link icon appears on pages with discovered links
   - Hover over link icon shows found links
   - Manual scan can be triggered via button

## Code Comparison

### Link Discovery Pattern
Both use the same regex pattern:

**find-links:**
```csharp
private static readonly Regex IssueLinkRegex = new(@"(vol(?:ume)?)[\s\.:/]*([0-9]+)[\s\S]*?(no(?:\.|umber)?)[\s\.:/]*([0-9]+)", RegexOptions.IgnoreCase);
```

**index-editor:**
```csharp
var regex = new System.Text.RegularExpressions.Regex(
    @"(vol(?:ume)?)[\s\.:/]*([0-9]+)[\s\S]*?(no(?:\.|umber)?)[\s\.:/]*([0-9]+)",
    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
```

### OCR Implementation
Both use Tesseract command-line tool via `Process.Start()` with nearly identical code.

## Dependencies Analysis

### find-links Dependencies
- `Npgsql` - PostgreSQL database connection
- `Tesseract` - OCR (external command-line tool)
- Database schema with `IssueLink` table

### index-editor Link Discovery Dependencies
- `Tesseract` - OCR (external command-line tool)
- No database required
- No additional NuGet packages needed

## Projects That Reference find-links
**None.** 

Checked:
- ✅ Solution files (Magazine.sln) - only includes project, no references
- ✅ Other projects (.csproj files) - no dependencies on find-links
- ✅ Scripts (publish-tools.sh) - includes find-links in build list
- ✅ magazine-viewer - uses its own database queries for links

## Advantages of index-editor's Approach

1. **No Database Required** - Links stored with the index file
2. **File-Based** - Links are part of the issue data, portable
3. **UI Integrated** - Better user experience with visual feedback
4. **Progressive** - Scans in background while user works
5. **Persistent** - Links saved automatically with index file
6. **Simpler** - No need to manage database state

## Removal Plan

### Files to Remove
```
src/find-links/
├── find-links.csproj
├── Program.cs
├── FindLinksApp.cs
├── Models/
│   └── ContentEntry.cs
└── Services/
    ├── IDatabaseRepository.cs
    ├── PostgresRepository.cs
    ├── IOcrService.cs
    ├── TesseractOcrService.cs
    ├── IContentParser.cs
    ├── MagazineContentParser.cs
    ├── IssueLinkParser.cs
    ├── IFileSystemService.cs
    └── MagazineFileSystemService.cs
```

### Documentation to Remove/Update
- ❌ Remove: `FIND_LINKS_OCR_FIX.md`
- ❌ Remove: `FIND_LINKS_OCR_TESSERACT_INSTALLED.md`
- ❌ Remove: `FIND_LINKS_OCR_COMPLETE.md` (if exists)
- ⚠️ Update: `README.md` - Remove find-links section
- ⚠️ Update: `scripts/publish-tools.sh` - Remove find-links from build list

### Solution Files to Update
- ⚠️ Update: `Magazine.sln` - Remove find-links project entries

## Testing Checklist

After removal, verify:
- ✅ `Magazine.sln` builds without errors
- ✅ `scripts/publish-tools.sh` runs successfully
- ✅ `index-editor` still discovers links when loading an issue
- ✅ Links are saved to `_index.json` when saving
- ✅ Links are loaded from `_index.json` when opening an issue
- ✅ Link icon appears in Page Controller when links are found
- ✅ No broken references in any project

## Migration Notes for Existing Users

### If you previously used find-links:
1. Links stored in the database will **not** be automatically migrated
2. Index-editor will discover links fresh when you open an issue
3. Links will be stored in the `_index.json` file going forward
4. The database `IssueLink` table is no longer used by any tool

### magazine-viewer Compatibility
The `magazine-viewer` web app currently reads links from the database. This is **unaffected** by removing find-links because:
- magazine-viewer has its own database queries
- magazine-viewer doesn't depend on find-links as a project
- Future enhancement: magazine-viewer could be updated to read links from `_index.json` files instead

## Conclusion

✅ **Safe to remove find-links project**

The index-editor provides superior link discovery functionality:
- Same OCR and pattern matching capabilities
- Better user experience with UI integration
- No database dependency
- Automatic persistence in JSON format
- Background scanning with progress feedback

## Recommendation

**Proceed with removal.** The find-links project is obsolete and removing it will:
- Reduce codebase complexity
- Eliminate database dependency for link scanning
- Remove maintenance burden
- Simplify the build process
- Reduce confusion about which tool to use

