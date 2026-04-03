# Issues Page Cover Images Fix

## Summary
Fixed TheArchive to display cover images from the database on the /issues page (http://localhost:5163/issues). The cover images are identified as page articles with the "Cover" category.

## Changes Made

### 1. Database Service Updates (`the-archive/src/TheArchive/Services/ArchiveDatabase.cs`)
Updated all Issue query methods to fetch cover images from the database:

- **GetFilteredIssuesAsync** (lines 121-177): Added LEFT JOIN to fetch cover image from articles with 'Cover' category
- **GetAllIssuesAsync** (lines 93-116): Added cover image query
- **GetIssuesByMagazineAsync** (lines 182-205): Added cover image query
- **GetIssueAsync** (lines 210-231): Added cover image query

All methods now include this pattern:
```sql
LEFT JOIN (
    SELECT DISTINCT ON (mc.IssueId) mc.IssueId, mc.ImagePath
    FROM Content mc
    JOIN Article a ON mc.ArticleId = a.ArticleId
    JOIN Category cat ON a.CategoryId = cat.CategoryId
    WHERE cat.Name = 'Cover'
    ORDER BY mc.IssueId, mc.Page
) cover ON i.IssueId = cover.IssueId
```

### 2. View Update (`the-archive/src/TheArchive/Views/Issues/Index.cshtml`)
Modified the issue card rendering to display cover images as background-image:

**Before:**
```html
<div class="issue-thumb @magazineClass">
```

**After:**
```html
@{
    var backgroundStyle = "";
    if (!string.IsNullOrEmpty(issue.CoverImagePath))
    {
        backgroundStyle = $"background-image: url('/image?path={Uri.EscapeDataString(issue.CoverImagePath)}'); background-size: cover; background-position: center;";
    }
}
<div class="issue-thumb @magazineClass" style="@backgroundStyle">
```

### 3. Image Controller Created (`the-archive/src/TheArchive/Controllers/ImageController.cs`)
Created a new ImageController to serve image files via `/image?path=...` endpoint:

- Handles both absolute and relative image paths
- Uses MAGAZINE_IMAGE_ROOT environment variable for relative paths
- Supports common image formats (JPEG, PNG, GIF, BMP)
- Returns appropriate content-type headers

## Database Schema
The cover images are identified using the following criteria:
- Articles with Category.Name = 'Cover'
- The first page (lowest page number) of the cover article
- One cover per issue (DISTINCT ON IssueId)

## Testing
Verified on http://localhost:5163/issues:
- All 6 issues display their cover images
- Images are served correctly through the /image endpoint
- Cover images have correct file paths from the database

## Date
April 3, 2026

