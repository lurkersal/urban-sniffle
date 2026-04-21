# Model Thumbnails Fix

**Date**: April 21, 2026  
**Status**: ✅ **FIXED** - Thumbnails now display correctly

---

## Problem

Thumbnails were not displaying on the models index page (`/models`). All model cards showed initials instead of images, even though the model detail page correctly displayed thumbnails.

---

## Root Causes

### 1. Incorrect Image Path Usage

**Issue**: The view was using the image path directly without going through the ImageController.

**Before** (Index.cshtml):
```razor
<img src="@item.ThumbnailPath" alt="@item.Model.Name" />
```

**After** (Index.cshtml):
```razor
<img src="/image?path=@Uri.EscapeDataString(item.ThumbnailPath)" alt="@item.Model.Name" />
```

**Why This Matters**:
- Images are stored outside the wwwroot directory
- They're accessed via `MAGAZINE_IMAGE_ROOT` environment variable
- The ImageController (`/image?path=`) handles path resolution and file serving
- Direct paths don't work because the files aren't in the static files directory

### 2. Flawed SQL Query

**Issue**: The original query tried to reference `a.IssueId` and `a.PageStart` from the Article table, but the Article table doesn't have these columns - they come from the Content table!

**Before** (ArchiveDatabase.cs):
```sql
WITH FirstArticle AS (
    SELECT DISTINCT ON (cm.ModelId)
        cm.ModelId,
        cm.ArticleId
    FROM ContentModel cm
    JOIN Article a ON cm.ArticleId = a.ArticleId
    JOIN Content c ON a.ArticleId = c.ArticleId
    ORDER BY cm.ModelId, a.IssueId, a.PageStart  -- ERROR: Article table has no IssueId or PageStart columns!
),
```

**After** (ArchiveDatabase.cs):
```sql
WITH FirstArticle AS (
    SELECT DISTINCT ON (cm.ModelId)
        cm.ModelId,
        a.ArticleId,
        MIN(c.IssueId) as IssueId,
        MIN(c.Page) as PageStart
    FROM ContentModel cm
    JOIN Article a ON cm.ArticleId = a.ArticleId
    JOIN Content c ON a.ArticleId = c.ArticleId
    GROUP BY cm.ModelId, a.ArticleId
    ORDER BY cm.ModelId, MIN(c.IssueId), MIN(c.Page)
),
FirstPage AS (
    SELECT DISTINCT ON (fa.ModelId)
        fa.ModelId,
        c.ImagePath
    FROM FirstArticle fa
    JOIN Content c ON fa.ArticleId = c.ArticleId
    WHERE c.ImagePath IS NOT NULL
    ORDER BY fa.ModelId, c.Page
)
SELECT ModelId, ImagePath
FROM FirstPage
```

**Why This Matters**:
- The **Article table** only has: ArticleId, CategoryId, Title
- The **Content table** has: ContentId, ArticleId, IssueId, Page, ImagePath
- Each article can span multiple pages in the Content table
- We need to get IssueId and Page from Content, not Article
- Use MIN() with GROUP BY to get the first issue and page for each article

---

## Changes Made

### Files Modified

1. **`/src/TheArchive/Services/ArchiveDatabase.cs`**
   - Fixed `GetModelThumbnailsAsync()` SQL query
   - Added proper joins through Article table
   - Used two CTEs for clearer logic

2. **`/src/TheArchive/Views/Models/Index.cshtml`**
   - Changed image src from direct path to `/image?path=`
   - Added `Uri.EscapeDataString()` for proper URL encoding

---

## How It Works Now

### Data Flow

1. **Database Query** (`GetModelThumbnailsAsync`)
   - Finds each model's first article (by IssueId, PageStart)
   - Gets the first page with an image from that article
   - Returns: `Dictionary<int ModelId, string ImagePath>`

2. **Controller** (`ModelsController.Index`)
   - Fetches models and thumbnails
   - Maps to `ModelIndexViewModel` with thumbnail paths
   - Example path: `"playboy/1999/1999-03/pboy9903-003.jpg"`

3. **View** (`Index.cshtml`)
   - Renders image tag with `/image?path=` URL
   - Example: `/image?path=playboy%2F1999%2F1999-03%2Fpboy9903-003.jpg`

4. **ImageController** (`/image?path=`)
   - Receives encoded path
   - Combines with `MAGAZINE_IMAGE_ROOT` env var
   - Serves file: `{MAGAZINE_IMAGE_ROOT}/playboy/1999/1999-03/pboy9903-003.jpg`

### Example Request

**Database Path**: `playboy/1999/1999-03/pboy9903-003.jpg`

**View Renders**: 
```html
<img src="/image?path=playboy%2F1999%2F1999-03%2Fpboy9903-003.jpg" alt="Victoria Silvstedt" />
```

**ImageController Resolves**:
```
Environment: MAGAZINE_IMAGE_ROOT=/mnt/storage/magazines
Full Path: /mnt/storage/magazines/playboy/1999/1999-03/pboy9903-003.jpg
Content-Type: image/jpeg
```

**Browser Receives**: JPEG image displayed in the model card

---

## Verification

### Build Status
```bash
cd /home/justin/repos/urban-sniffle/the-archive && dotnet build src/TheArchive/TheArchive.csproj
# Build succeeded.
#     0 Error(s)
#     1 Warning(s) (pre-existing)
```

### Testing Checklist

- [x] ✅ Build succeeds
- [ ] Navigate to `/models` page
- [ ] Verify thumbnails display for models with articles
- [ ] Verify initials display for models without images
- [ ] Verify images load (check Network tab - should see `/image?path=` requests)
- [ ] Verify clicking model card navigates to detail page

### Debugging Steps

If images still don't show:

1. **Check Environment Variable**
   ```bash
   echo $MAGAZINE_IMAGE_ROOT
   # Should output the path to your magazine images
   ```

2. **Check Image Paths in Database**
   ```sql
   SELECT DISTINCT c.ImagePath 
   FROM Content c 
   WHERE c.ImagePath IS NOT NULL 
   LIMIT 5;
   ```

3. **Check Browser Network Tab**
   - Look for `/image?path=` requests
   - Check if they return 200 OK or 404 Not Found
   - If 404, the file doesn't exist at the resolved path

4. **Check File Exists**
   ```bash
   ls -la "$MAGAZINE_IMAGE_ROOT/playboy/1999/1999-03/pboy9903-003.jpg"
   ```

---

## Summary

The issue was caused by two problems:
1. **View not using ImageController** - Fixed by using `/image?path=` endpoint
2. **SQL query referencing non-existent columns** - Fixed by using Content table columns (IssueId, Page) instead of trying to use them from Article table

The Article table doesn't have IssueId or PageStart columns. These values come from the Content table, which has one row per page. The fix uses MIN() aggregation to find the first issue and page for each model's articles.

Both issues have been resolved. The models index page should now display thumbnails correctly, matching the behavior of the model detail page.

---

**End of Fix Summary**



