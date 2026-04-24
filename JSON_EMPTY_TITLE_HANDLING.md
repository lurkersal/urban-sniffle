# Empty/Null Title Handling Enhancement

**Date**: March 4, 2026  
**Feature**: JSON tree viewer now shows article category even when title is missing

## Problem

When an article in the JSON index file had an empty, null, or missing `title` field, the collapsed article would show the generic `{ ... N properties }` text instead of showing the category.

This made it difficult to identify what type of article it was without expanding the node.

## Solution

Modified the article preview logic to:
1. **Check category first**: If category exists and is non-empty, treat it as an article
2. **Handle missing title gracefully**: Show "(no title)" instead of falling back to generic format

## Examples

### Before
```json
{
  "category": "Index",
  "pages": [3]
}
```
Displayed as: `▶ [1]: { ... 2 properties }`

### After
```json
{
  "category": "Index",
  "pages": [3]
}
```
Displayed as: `▶ [1]: { Index - (no title) }`

## All Scenarios

| Title Value | Display Format |
|-------------|----------------|
| "Normal Title" | `{ Cover - Normal Title }` |
| "" (empty string) | `{ Editorial - (no title) }` |
| null | `{ Model - (no title) }` |
| (property missing) | `{ Index - (no title) }` |
| No category | `{ ... N properties }` |

## Code Changes

**File**: `src/index-editor/Services/JsonTreeViewer.cs`

### Key Changes

1. **Removed dual requirement**: Previously required **both** category and title to be non-empty
2. **Added fallback text**: Uses "(no title)" when title is missing/empty
3. **Prioritizes category**: Shows category-based preview whenever possible

```csharp
// OLD (required both)
if (categoryNode != null && titleNode != null && 
    !string.IsNullOrWhiteSpace(categoryNode.Value) && 
    !string.IsNullOrWhiteSpace(titleNode.Value))

// NEW (category only, title optional)
if (categoryNode != null && !string.IsNullOrWhiteSpace(categoryNode.Value))
{
    var title = titleNode != null && !string.IsNullOrWhiteSpace(titleNode.Value)
        ? titleNode.Value
        : "(no title)";
```

## Testing

Created test JSON file at `/tmp/test-empty-title.json` with various scenarios:

```json
{
  "articles": [
    {
      "category": "Cover",
      "title": "Normal Article",
      "pages": [1]
    },
    {
      "category": "Editorial",
      "title": "",
      "pages": [2]
    },
    {
      "category": "Index",
      "pages": [3]
    },
    {
      "category": "Model",
      "title": null,
      "pages": [4]
    }
  ]
}
```

**Expected Results:**
- Article 0: `{ Cover - Normal Article }`
- Article 1: `{ Editorial - (no title) }` (empty string)
- Article 2: `{ Index - (no title) }` (missing property)
- Article 3: `{ Model - (no title) }` (null value)

## Benefits

✅ **More informative**: Always shows article category when available  
✅ **Better UX**: User can identify article type without expanding  
✅ **Handles edge cases**: Gracefully handles missing/null/empty titles  
✅ **Consistent**: Uses same preview format for all articles  

## Files Modified

- ✅ `src/index-editor/Services/JsonTreeViewer.cs` - Updated article preview logic
- ✅ `JSON_ARTICLE_PREVIEW_ENHANCEMENT.md` - Updated documentation
- ✅ Binary rebuilt and published to `~/bin/IndexEditor`

## Build Status

✅ **Build successful**: No compilation errors  
✅ **Binary size**: 76M (full build, no trimming)  
✅ **Ready to test**: Run `IndexEditor` and press Ctrl+I  

## How to Test

1. Run `IndexEditor`
2. Open a magazine folder (or use test JSON: `/tmp/test-empty-title.json`)
3. Press **Ctrl+I** to open JSON overlay
4. Navigate to the "articles" array
5. Verify collapsed articles show:
   - `{ Category - Title }` for normal articles
   - `{ Category - (no title) }` for articles without titles

---

**Enhancement Complete** ✓

Articles with missing titles now display meaningful previews!

