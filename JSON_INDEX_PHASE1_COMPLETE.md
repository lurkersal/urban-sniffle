# JSON Index Format - Phase 1 Implementation Complete ✅

## Date: February 28, 2026

## Status: COMPLETE
Phase 1 implementation is finalized and tested. All 119 existing tests pass.

## Overview
Phase 1 of the JSON index file migration is complete. The system now:
- **Reads** both `_index.json` (preferred) and `_index.txt` (fallback)
- **Writes** only `_index.json` format
- Maintains full backward compatibility with existing `_index.txt` files

## Files Created/Modified

### New Files
1. **src/common/Shared/IndexFileModels.cs**
   - Defines JSON schema models: `IndexFileJson`, `IndexMetadata`, `ArticleJson`, `SegmentJson`
   - Uses System.Text.Json with camelCase naming

2. **src/common/Shared/IndexJsonSerializer.cs**
   - `SaveToJson()` - Converts ArticleLine objects to JSON and saves with atomic write + backup
   - `LoadFromJson()` - Reads JSON and converts to ArticleLine objects
   - `JsonExists()` - Checks if _index.json exists in folder
   - Handles conversion between ArticleLine (internal) and JSON models

### Modified Files
1. **src/index-editor/Shared/IndexSaver.cs**
   - Now uses IndexJsonSerializer.SaveToJson() instead of CSV format
   - Saves as `_index.json` with backup to `_index.json~`

2. **src/index-editor/MainWindow.axaml.cs**
   - `LoadArticlesFromFolder()` updated to check for JSON first, fall back to CSV
   - Logs which format is being loaded
   - Overlay save button detects format based on content (JSON vs CSV)

3. **src/index-editor/Services/OverlayManager.cs**
   - `ShowIndexOverlay()` prefers `_index.json` over `_index.txt`
   - Displays whichever format exists in the Ctrl+I overlay

## JSON Format Structure

```json
{
  "metadata": {
    "magazine": "Club International",
    "volume": "17",
    "number": "01",
    "year": "1988"
  },
  "articles": [
    {
      "pages": [25, 26, 27, 28],
      "category": "Model",
      "title": "Emma",
      "modelNames": ["Emma"],
      "ages": [23],
      "contributors": ["Wavelength"],
      "measurements": ["36C-24-36"]
    }
  ]
}
```

### Important: Pages vs Segments

**Key Design Decision**: The JSON format stores ONLY `pages`, not `segments`.

- **`pages`**: The canonical source of truth - a flat list of all page numbers in the article
- **`segments`**: NOT stored in JSON - these are automatically computed from pages when loading

**Why?**
- Segments are derived data (computed from pages via `RecomputeSegmentsFromPages()`)
- Storing both creates ambiguity and potential for inconsistency
- Pages are simpler and more flexible
- When ArticleLine.Pages is set, segments are automatically recomputed

**Example**:
- JSON stores: `"pages": [25, 26, 27, 30, 31]`
- ArticleLine computes: Two segments: `25-27` and `30-31`
- UI displays these segments as lozenge bars

## Key Features

### Atomic Writes with Backup
- Writes to `.tmp` file first
- Creates backup (`~` suffix) before replacing
- Uses `File.Replace()` for atomic operation

### Backward Compatibility
- Existing `_index.txt` files still work
- System automatically upgrades to JSON on next save
- Both formats supported during transition

### Error Handling
- JSON parse failures fall back to CSV gracefully
- User sees toast notification if JSON fails to load
- Overlay editor works with both formats

## Testing
- All 119 existing tests pass
- No breaking changes to existing functionality
- CSV parsing still works correctly

## Next Steps (Future Phases)

### Phase 2 (Optional)
- Update magazine-parser to output JSON format
- Add JSON validation/linting in editor overlay
- Pretty-print JSON with syntax highlighting

### Phase 3 (Optional)
- Deprecate CSV format completely
- Add migration tool to batch-convert all `_index.txt` to JSON
- Remove CSV reading code

## Notes
- The JSON format uses camelCase property names (JSON convention)
- Internal C# models use PascalCase (C# convention)
- **Segments are NOT stored in JSON** - they are computed from pages automatically
- Null/empty collections are omitted from JSON (cleaner output)
- Pages list is the single source of truth for article page membership

