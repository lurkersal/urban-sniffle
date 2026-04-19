# Article-Specific Page Viewer Implementation

**Date**: April 20, 2026  
**Status**: ✅ **IMPLEMENTED** - Page viewer now shows only the clicked article's pages

---

## Feature Overview

The page viewer has been updated to display only the pages belonging to the clicked article, rather than showing a generic spread starting from that page.

### Key Features

✅ **Article-specific pages** - Only shows pages from the clicked article  
✅ **Cover article special handling** - Cover articles show ALL issue pages for full magazine browsing  
✅ **Non-consecutive page support** - Handles articles with pages 12, 15, 18, etc.  
✅ **Smart spread detection** - Shows double spreads when pages are consecutive  
✅ **Single page display** - Shows one page when not consecutive  
✅ **Article-aware navigation** - Prev/Next navigate through article pages only  

---

## Implementation Details

### 1. Database Layer

**File**: `src/TheArchive/Services/ArchiveDatabase.cs`

New method to get pages for a specific article:

```csharp
public async Task<List<(int Page, string? ImagePath)>> GetArticlePageImagesAsync(int articleId)
{
    using var conn = GetConnection();
    const string sql = @"
        SELECT Page, ImagePath
        FROM Content
        WHERE ArticleId = @ArticleId
        ORDER BY Page";
    
    var results = await conn.QueryAsync<(int Page, string? ImagePath)>(sql, new { ArticleId = articleId });
    return results.ToList();
}
```

### 2. API Endpoint

**File**: `src/TheArchive/Controllers/Api/PagesController.cs`

New endpoint to fetch article pages:

```csharp
[HttpGet("article/{articleId}")]
public async Task<IActionResult> GetArticlePages(int articleId)
{
    var pages = await _db.GetArticlePageImagesAsync(articleId);
    
    return Ok(new
    {
        articleId,
        pages = pages.Select(p => new { page = p.Page, imagePath = p.ImagePath }).ToList()
    });
}
```

**Endpoint**: `GET /api/v1/pages/article/{articleId}`

**Response Example**:
```json
{
  "articleId": 456,
  "pages": [
    { "page": 12, "imagePath": "path/to/page12.jpg" },
    { "page": 13, "imagePath": "path/to/page13.jpg" },
    { "page": 18, "imagePath": "path/to/page18.jpg" }
  ]
}
```

### 3. Click Handler Update

**File**: `src/TheArchive/Views/Issues/Detail.cshtml`

Updated article row click to call `openArticle` instead of `openSpread`:

```html
<div onclick="openArticle(@article.ArticleId, '@(article.Title?.Replace("'", "\\'") ?? "Untitled")')">
```

### 4. JavaScript Implementation

**File**: `src/TheArchive/Views/Shared/_Layout.cshtml`

New functions to handle article viewing:

#### `openArticle(articleId, articleTitle, category, issueId)`
- Checks if article is a Cover article
- **If Cover**: Calls `openIssuePages(issueId)` to load all issue pages
- **Otherwise**: Fetches pages for the specific article
- Stores pages in `currentArticlePages` array
- Displays first page(s)
- Opens viewer overlay

#### `openIssuePages(issueId)`
- Loads ALL pages for the issue via issue pages API
- Converts page dictionary to array format
- Sorts pages by page number
- Displays using same spread logic as regular articles

#### `displayArticlePages()`
- Gets current page and checks if next page is consecutive
- Shows double spread if pages are consecutive (e.g., 12-13)
- Shows single page if not consecutive (e.g., 12, then 18)
- Updates page info display
- Updates Prev/Next button states

#### `navigateArticlePages(direction)`
- Navigates through article's pages
- Skips 2 pages if viewing a spread
- Skips 1 page if viewing single page
- Handles previous navigation intelligently

#### `shiftSpread(delta)`
- Updated to check if viewing article pages
- Uses `navigateArticlePages()` for articles
- Falls back to old spread navigation otherwise

---

## User Experience

### Special Case: Cover Articles

**When clicking a Cover article**:
- Loads **ALL pages of the entire issue** (not just the cover page)
- Allows browsing the complete magazine from cover to cover
- Uses same spread logic (even-odd pairs)

**Example**: Cover article on page 1
1. Click Cover article
2. Viewer loads pages 1, 2, 3, 4... (all pages in the issue)
3. Shows page **1** (single - odd)
4. Next → Shows pages **2 & 3** (spread - even-odd)
5. Next → Shows pages **4 & 5** (spread)
... continues through entire issue

### Standard Articles

All other article categories show only their specific pages:

### Example: Article with Pages 12, 13, 18, 20

**User clicks article**:
1. Viewer opens showing pages **12 & 13** (consecutive, shown as spread)
2. User clicks "Next" →
3. Viewer shows page **18** (single page, next page 20 is not consecutive)
4. User clicks "Next" →
5. Viewer shows page **20** (single page, last page)
6. "Next" button is disabled

### Example: Article with Pages 5, 8, 12

**User clicks article**:
1. Viewer shows page **5** (single page)
2. User clicks "Next" →
3. Viewer shows page **8** (single page)
4. User clicks "Next" →
5. Viewer shows page **12** (single page)

### Example: Article with Pages 24, 25, 26, 27

**User clicks article**:
1. Viewer shows pages **24 & 25** (spread)
2. User clicks "Next" →
3. Viewer shows pages **26 & 27** (spread)

### Example: Article with Pages 9, 10, 11, 12

**User clicks article** (note: no page 8):
1. Viewer shows page **9** (single - odd page with no preceding even)
2. User clicks "Next" →
3. Viewer shows pages **10 & 11** (spread - even-odd pair)
4. User clicks "Next" →
5. Viewer shows page **12** (single - even page with no following odd)

---

## Navigation Behavior

### Forward Navigation
- If viewing spread (consecutive pages): Advance 2 pages
- If viewing single page: Advance 1 page
- Button disabled when at last page

### Backward Navigation
- Smart detection of whether to go back 1 or 2 pages
- Checks if previous page is part of a spread
- Button disabled when at first page

### Keyboard Navigation
- **Right Arrow**: Next page(s)
- **Left Arrow**: Previous page(s)
- **ESC**: Close viewer

---

## Technical Details

### Spread Detection Logic

```javascript
const currentPageData = currentArticlePages[currentPageIndex];
const nextPageData = currentArticlePages[currentPageIndex + 1];

// Check if we can show a double spread:
// 1. Pages must be consecutive (n, n+1)
// 2. First page must be even, second must be odd
const canShowSpread = nextPageData && 
                      (nextPageData.page === currentPageData.page + 1) &&
                      (currentPageData.page % 2 === 0) && 
                      (nextPageData.page % 2 === 1);

// Pages are ALWAYS shown in numerical order (never swapped)
```

**Spread Ordering Rules**:
- **Only show spreads for even-odd pairs**: 8-9, 10-11, 12-13, etc.
- **Always in numerical order**: Pages displayed left to right as they appear numerically
- **Never swap pages**: Page 8 is always shown before page 9
- **Odd-even pairs shown separately**: 9-10 shown as two single pages, not as a spread

**Examples**:
- Pages 8-9 → **Spread**: 8 left (even), 9 right (odd) ✅
- Pages 10-11 → **Spread**: 10 left (even), 11 right (odd) ✅
- Pages 9, 10, 11 (no page 8) → Page **9 single**, then **10-11 spread** ✅
- Pages 13, 14, 15 (no page 12) → Page **13 single**, then **14-15 spread** ✅

### Page Info Display

- **Double spread (even-odd pairs)**: "Page **8** & **9**" (in numerical order)
- **Single page**: "Page **9**" or "Page **10**"

**Visual Layout for Spreads**:
```
┌──────────────────────────────────────┐
│  Page 8 & 9           [Prev] [Next]  │
│                                       │
│  ┌────────────┐  ┌────────────┐     │
│  │            │  │            │     │
│  │  Page 8    │  │  Page 9    │     │
│  │  (even)    │  │  (odd)     │     │
│  │            │  │            │     │
│  └────────────┘  └────────────┘     │
│     LEFT            RIGHT            │
└──────────────────────────────────────┘
```

**Visual Layout for Single Pages** (odd-even pairs like 9-10):
```
Page 9 (single):
┌──────────────┐
│              │
│   Page 9     │
│   (odd)      │
│              │
└──────────────┘

Then Next → Page 10 (single):
┌──────────────┐
│              │
│   Page 10    │
│   (even)     │
│              │
└──────────────┘
```

### State Management

```javascript
let currentArticlePages = null;  // Array of {page, imagePath} objects
let currentPageIndex = 0;        // Current position in array
```

---

## Build Status

✅ **Build successful**: 0 errors, 3 warnings (all pre-existing)

```bash
cd /home/justin/repos/urban-sniffle/the-archive && dotnet build src/TheArchive/TheArchive.csproj
# Build succeeded.
#     0 Error(s)
#     3 Warning(s)
```

---

## Testing Checklist

### Basic Functionality
- [ ] Click article with consecutive pages (e.g., 12-13) - should show spread
- [ ] Click article with non-consecutive pages (e.g., 12, 18) - should show single pages
- [ ] Click article with mix - should show spreads and singles appropriately

### Navigation
- [ ] Next button advances correctly (2 pages for spread, 1 for single)
- [ ] Previous button goes back correctly
- [ ] Arrow keys work for navigation
- [ ] Buttons disable at start/end of article

### Edge Cases
- [ ] Article with only 1 page - shows single page, Next disabled
- [ ] Article with 2 consecutive pages - shows as spread
- [ ] Article with 2 non-consecutive pages - shows as separate singles
- [ ] Article with pages throughout issue (e.g., 5, 20, 45, 80)

---

## Related Files

- `/home/justin/repos/urban-sniffle/the-archive/src/TheArchive/Services/ArchiveDatabase.cs` - Database service
- `/home/justin/repos/urban-sniffle/the-archive/src/TheArchive/Controllers/Api/PagesController.cs` - API endpoint
- `/home/justin/repos/urban-sniffle/the-archive/src/TheArchive/Views/Shared/_Layout.cshtml` - JavaScript viewer logic
- `/home/justin/repos/urban-sniffle/the-archive/src/TheArchive/Views/Issues/Detail.cshtml` - Article list with click handlers

---

## Summary

✅ **Feature complete** - Page viewer now shows only the clicked article's pages  
✅ **Non-consecutive support** - Handles articles with gaps in page numbers  
✅ **Smart spread detection** - Shows double spreads when pages are consecutive  
✅ **Intelligent navigation** - Navigates through article pages appropriately  
✅ **Build verified** - No compilation errors  

Users can now click on any article to see only its pages, regardless of whether they're consecutive or scattered throughout the issue!

---

**End of Document**

