# Thumbnail Page Feature - Quick Reference

## User Guide

### How to Set a Thumbnail Page

1. **Open a magazine folder** in IndexEditor
2. **Navigate to the desired page** (e.g., page 47)
3. **Find the article card** in the right pane (PageControllerView)
4. **Check the "Thumbnail" checkbox** in the article card
5. **Save** (Ctrl+S)
6. **Import** to TheArchive
7. **Done!** The article will now use page 47 as its thumbnail

### How to Reset to Default (First Page)

**Option A:** Navigate to the first page and check the checkbox
**Option B:** Uncheck the checkbox on any page

Both set `ThumbnailPage` to NULL, which means "use the first page".

## Technical Quick Ref

### Database
```sql
-- Column: Article.ThumbnailPage (INT, nullable)
-- NULL = use first page
-- 47 = use page 47
```

### JSON
```json
{
  "title": "Beach Day",
  "category": "Model",
  "thumbnailPage": 47  // NULL if not set or first page
}
```

### UI Location
```
┌─────────────────────────────────────────┐
│ PageControllerView (Right Pane)         │
│ ┌─────────────────────────────────────┐ │
│ │ ▮ Beach Day  [☑ Thumbnail]  Model  │ │ ← HERE
│ └─────────────────────────────────────┘ │
│ [Page Image]                            │
│ [◀] [47] [▶]                            │
└─────────────────────────────────────────┘
```

### Checkbox Meaning

- **☑ Checked** = "This page (current) is the thumbnail"
- **☐ Unchecked** = "This page (current) is NOT the thumbnail"

### Code Locations

| Component | File | Purpose |
|-----------|------|---------|
| Checkbox rendering | `ArticleCardRenderer.cs` | Creates checkbox, determines checked state |
| Checkbox click handler | `ArticleCardRenderer.cs` | Updates `ThumbnailPage` property |
| Database column | `Article` table | Stores thumbnail page number |
| JSON field | `_index.json` | Persists thumbnail page |
| TheArchive query | `ArchiveDatabase.cs` | Uses `COALESCE(ThumbnailPage, MIN(Page))` |

## Status

✅ Fully implemented and working
✅ Build successful (0 errors)
✅ Backend complete (database, JSON, import, display)
✅ Frontend complete (UI, event handling, state management)
⏳ Ready for testing

## Documentation

- `THUMBNAIL_SINGLE_CHECKBOX_COMPLETE.md` - Full implementation details
- `THUMBNAIL_CHECKBOX_IMPLEMENTATION.md` - Technical implementation
- `ARTICLE_THUMBNAIL_PAGE_FEATURE.md` - Original specification

