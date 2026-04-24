# Article Detail Redirect Fix - No Intermediary Page

**Date:** 2026-04-23  
**Issue:** Article detail page showed as intermediary step, causing:
1. Visible metadata page between search and article viewer
2. Escape key didn't return to search results (stuck on metadata page)

## Problem

### User Experience Issue
1. Search for "dee"
2. Click article from search results
3. **See metadata page briefly** (shouldn't be visible)
4. Article viewer opens
5. Press Escape
6. **Stuck on metadata page** instead of returning to search results

### Root Cause
The `/articles/{id}` route was rendering a view (Detail.cshtml) that showed metadata first, then auto-opened the viewer via JavaScript. This created:
- An unwanted visual intermediary step
- Incorrect browser history (metadata page in history)
- Broken back navigation

## Solution

Changed `/articles/{id}` to **redirect directly to the issue page** with an `openArticle` query parameter, eliminating the intermediary step entirely.

### How It Works Now

1. User clicks article link: `/articles/832`
2. **Server redirects** to: `/issues/53?openArticle=832`
3. Issue page loads
4. JavaScript detects `openArticle` parameter
5. Auto-opens article viewer (100ms delay)
6. Cleans up URL (removes query parameter)
7. Browser history only contains: search page → issue page

**Result:** No intermediary page, and Escape returns to search results!

## Implementation

### Backend Changes

**File:** `/home/justin/repos/urban-sniffle/the-archive/src/TheArchive/Controllers/ArticlesController.cs`

**Before:**
```csharp
public async Task<IActionResult> Detail(int id)
{
    var article = await _db.GetArticleAsync(id);
    if (article == null) return NotFound();
    
    var issue = await _db.GetIssueAsync(article.IssueId);
    ViewBag.Article = article;
    ViewBag.Issue = issue;
    
    return View();  // Shows Detail.cshtml
}
```

**After:**
```csharp
public async Task<IActionResult> Detail(int id)
{
    var article = await _db.GetArticleAsync(id);
    if (article == null) return NotFound();
    
    // Redirect to issue page with article auto-open parameter
    return Redirect($"/issues/{article.IssueId}?openArticle={id}");
}
```

### Frontend Changes

**File:** `/home/justin/repos/urban-sniffle/the-archive/src/TheArchive/Views/Issues/Detail.cshtml`

Added auto-open logic at the start of the Scripts section:

```javascript
// Check for openArticle query parameter and auto-open the article
(function() {
    const urlParams = new URLSearchParams(window.location.search);
    const openArticleId = urlParams.get('openArticle');
    
    if (openArticleId) {
        // Find the article row with this ID
        const articleRow = document.querySelector(`[data-article-id="${openArticleId}"]`);
        
        if (articleRow && typeof openArticle === 'function') {
            const articleId = parseInt(articleRow.dataset.articleId);
            const articleTitle = articleRow.dataset.articleTitle;
            const articleCategory = articleRow.dataset.articleCategory;
            const issueId = parseInt(articleRow.dataset.issueId);
            
            // Small delay to ensure page is fully loaded
            setTimeout(function() {
                openArticle(articleId, articleTitle, articleCategory, issueId);
                
                // Clean up URL (remove query parameter) without reloading
                const cleanUrl = window.location.pathname;
                window.history.replaceState({}, document.title, cleanUrl);
            }, 100);
        }
    }
})();
```

## User Experience Flow

### Before Fix
```
Search Results
    ↓ (click article)
Article Metadata Page ← UNWANTED INTERMEDIARY
    ↓ (auto-open via JS)
Article Viewer
    ↓ (press Escape)
Article Metadata Page ← STUCK HERE
```

### After Fix
```
Search Results
    ↓ (click article)
Issue Page (redirect, no visual delay)
    ↓ (auto-open via JS, 100ms)
Article Viewer
    ↓ (press Escape)
Search Results ← RETURNS TO WHERE YOU STARTED
```

## Technical Details

### HTTP Redirect Flow
1. **Request:** `GET /articles/832`
2. **Response:** `HTTP 302 Found`
3. **Location:** `/issues/53?openArticle=832`
4. Browser automatically follows redirect

### Browser History
**Before:**
- History: [Search] → [Article Metadata] → [Issue Page]
- Escape from viewer: Goes to metadata page

**After:**
- History: [Search] → [Issue Page]
- Escape from viewer: Goes to search page

### URL Cleanup
After opening the article, the script removes the query parameter:
```javascript
window.history.replaceState({}, document.title, cleanUrl);
```

This changes:
- From: `/issues/53?openArticle=832`
- To: `/issues/53`

Benefits:
- Clean URL for sharing
- No confusion if page is refreshed
- Proper browser history

## Testing

### Manual Test Flow
1. Open http://localhost:5163
2. Search for "dee"
3. Click on any article result
4. **Verify:** Article viewer opens immediately (no metadata page flash)
5. Press Escape
6. **Verify:** Returns to search results page

### HTTP Redirect Test
```bash
# Test redirect is working
$ curl -v "http://localhost:5163/articles/832" 2>&1 | grep Location
< Location: /issues/53?openArticle=832
✓ Correct redirect

# Test issue page has auto-open script
$ curl -s "http://localhost:5163/issues/53?openArticle=832" | grep "openArticleId"
const openArticleId = urlParams.get('openArticle');
✓ Script present
```

### Edge Cases

| Scenario | Behavior |
|----------|----------|
| Article doesn't exist | 404 Not Found (no redirect) |
| Article exists | Redirects to issue page |
| Query parameter present | Auto-opens article |
| Query parameter missing | Normal issue page |
| JavaScript disabled | Issue page loads normally |
| Article not in issue | Script fails gracefully |

## Benefits

### User Benefits
1. **No Visual Glitch:** No metadata page flash
2. **Proper Navigation:** Escape returns to origin
3. **Faster:** Direct to content
4. **Clean URLs:** Query parameter removed after use

### Technical Benefits
1. **Simpler:** Server-side redirect vs client-side auto-open
2. **Proper History:** Browser history works correctly
3. **No Unused View:** Detail.cshtml no longer needed for search results
4. **Reuses Code:** Uses existing `openArticle()` function

### SEO Benefits
1. **Proper Redirects:** Search engines follow 302 correctly
2. **Canonical URLs:** Issue page is the canonical source
3. **No Duplicate Content:** No separate article detail pages

## Deprecated View

The file `/home/justin/repos/urban-sniffle/the-archive/src/TheArchive/Views/Articles/Detail.cshtml` is no longer used for search result clicks but could be:
- Kept for potential future use
- Removed to simplify codebase
- Repurposed for a different article detail view

**Recommendation:** Keep it but document that search results use redirect instead.

## Related Features

### Article Viewer
The spread viewer provides:
- Full article navigation
- Zoom controls
- Escape to close (returns to previous page)
- Arrow key navigation
- Metadata overlay

### Issue Page
The issue detail page shows:
- All articles in the issue
- Cover image background
- Category filtering
- Direct article opening

## Files Modified

1. **`/home/justin/repos/urban-sniffle/the-archive/src/TheArchive/Controllers/ArticlesController.cs`**
   - Changed `Detail()` to return redirect instead of view
   - Redirect to: `/issues/{issueId}?openArticle={articleId}`

2. **`/home/justin/repos/urban-sniffle/the-archive/src/TheArchive/Views/Issues/Detail.cshtml`**
   - Added auto-open script at start of Scripts section
   - Detects `openArticle` query parameter
   - Auto-opens article after 100ms
   - Cleans up URL using `history.replaceState()`

## Migration Notes

### For Existing Links
Any existing bookmarks or links to `/articles/{id}` will still work:
- They redirect to the issue page
- Article opens automatically
- URL gets cleaned up

### For Future Development
If you need a dedicated article detail page in the future:
- Use a different route: `/articles/{id}/details`
- Or add a `view=metadata` parameter
- The redirect approach is better for search results

## Status

✅ **FIXED** - Article links now redirect directly to issue page

**Verification:**
- No intermediary metadata page shown
- Article viewer opens automatically
- Escape returns to search results
- URL cleanup works
- Browser history correct
- Redirect is instant (302)

---

**Session Complete:** Article search results now provide seamless navigation without intermediary pages and proper back-button behavior.

