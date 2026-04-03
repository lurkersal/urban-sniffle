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
Modified the issue card rendering to display cover images as background-image and moved volume/date info below the image:

**Before:**
```html
<div class="issue-thumb @magazineClass">
    <div class="issue-label">
        <div class="issue-vol">@issue.VolumeDisplay</div>
        <div class="issue-date">@issue.DateLabel</div>
    </div>
</div>
<div class="issue-info">
    <div class="issue-num">@issue.MagazineName</div>
    <div class="issue-art-count">@issue.ArticleCount articles</div>
</div>
```

**After:**
```html
<div class="issue-thumb @magazineClass" style="@backgroundStyle">
</div>
<div class="issue-info">
    <div class="issue-num">@issue.MagazineName</div>
    <div class="issue-vol">@issue.VolumeDisplay</div>
    <div class="issue-date">@issue.DateLabel</div>
    <div class="issue-art-count">@issue.ArticleCount articles</div>
</div>
```

The cover image is now displayed without any overlay text, and all metadata (magazine name, volume, date, and article count) is shown in the info section below the image.

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
- Issues are displayed in volume/number order (ascending), grouped by magazine name
- Scrolling is smooth with lazy loading - images load only when needed
- Loading spinners appear while images are being fetched
- Images fade in smoothly when loaded

## Date
April 3, 2026

