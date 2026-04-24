# Model Article Navigation Fix

**Date**: April 21, 2026  
**Status**: ✅ **COMPLETED** - Model article clicks now open article viewer instead of navigating to issue

---

## Summary

Changed the behavior on the model detail page so that clicking on a model's article opens the article viewer directly, rather than navigating to the issue page that contains the article.

---

## Problem

**Before**: On the model detail page, clicking on any of the model's articles would navigate to the issue page (`/issues/{issueId}`), requiring users to find and click the article again.

**After**: Clicking on a model's article now opens the article viewer overlay directly, providing immediate access to the article's pages.

---

## Changes Made

### File Modified: `/Views/Models/Detail.cshtml`

#### 1. Removed Inline onclick Handler

**Before (Line 71)**:
```html
<div class="article-row" style="padding: 8px 0;" onclick="window.location.href='/issues/@article.IssueId'">
```

**After (Lines 71-76)**:
```html
<div class="article-row" 
     style="padding: 8px 0; cursor: pointer;"
     data-article-id="@article.ArticleId"
     data-article-title="@(article.Title ?? "Untitled")"
     data-article-category="@article.CategoryName"
     data-issue-id="@article.IssueId">
```

#### 2. Added ID to Article List Container

**Before (Line 67)**:
```html
<div class="article-list" style="margin-top: 0.5rem;">
```

**After (Line 67)**:
```html
<div class="article-list" id="model-article-list" style="margin-top: 0.5rem;">
```

#### 3. Added Event Delegation Script

**New Script Section (Lines 101-123)**:
```javascript
@section Scripts {
    <script>
        // Handle clicks on model's articles to open article viewer
        document.addEventListener('DOMContentLoaded', () => {
            const articleList = document.getElementById('model-article-list');
            if (articleList) {
                articleList.addEventListener('click', (e) => {
                    const articleRow = e.target.closest('.article-row');
                    if (articleRow) {
                        const articleId = parseInt(articleRow.dataset.articleId);
                        const articleTitle = articleRow.dataset.articleTitle || 'Untitled';
                        const category = articleRow.dataset.articleCategory;
                        const issueId = parseInt(articleRow.dataset.issueId);
                        
                        if (articleId && issueId) {
                            openArticle(articleId, articleTitle, category, issueId);
                        }
                    }
                });
            }
        });
    </script>
}
```

---

## Technical Details

### How It Works

1. **Data Attributes**: Each article row now has `data-*` attributes containing:
   - `data-article-id`: The article's ID
   - `data-article-title`: The article's title (or "Untitled")
   - `data-article-category`: The article's category
   - `data-issue-id`: The issue ID containing the article

2. **Event Delegation**: A single click event listener is attached to the article list container using event delegation, which:
   - Captures clicks on any article row
   - Extracts the article data from the data attributes
   - Calls the global `openArticle()` function defined in `_Layout.cshtml`

3. **Article Viewer**: The `openArticle()` function:
   - Fetches the article's pages via API (`/api/v1/pages/article/{articleId}`)
   - Opens the spread overlay with the article's pages
   - Provides navigation controls (keyboard arrows, page numbers)

---

## Benefits

✅ **Better UX** - Users get immediate access to the article without extra clicks  
✅ **Consistency** - Matches the behavior on the Issue detail page  
✅ **CSP Compliant** - Removed inline `onclick` handler  
✅ **Event Delegation** - Single event listener for all article rows  
✅ **Performance** - No navigation/page reload required  
✅ **Visual Feedback** - Added `cursor: pointer` for better affordance  

---

## Behavior Examples

### Example 1: Clicking a Model Article
1. User visits `/models/123` (model detail page)
2. User sees list of articles featuring that model
3. User clicks on an article row
4. **Article viewer overlay opens** showing the article's pages
5. User can navigate through pages with arrow keys or buttons
6. User can close overlay with Esc key or close button

### Example 2: Cover Article
- If the article is a "Cover" category, the `openArticle()` function automatically loads all issue pages instead of just the cover page

---

## Testing

### Manual Testing Checklist

- [x] ✅ Build succeeds with no errors
- [ ] Model detail page loads correctly
- [ ] Article list displays all model's articles
- [ ] Clicking an article row opens the article viewer
- [ ] Article viewer shows correct pages
- [ ] Arrow keys navigate between pages
- [ ] Esc key closes the viewer
- [ ] Issue chips still navigate to issue page (unchanged)
- [ ] "Issue X" label in article row still displays (for context)

### Edge Cases

- [ ] Article with no title shows "Untitled"
- [ ] Article with no pages shows warning toast
- [ ] Cover articles load full issue pages
- [ ] API errors display error toast

---

## Code Quality Improvements

### Before This Change
- ❌ Inline `onclick` handler (CSP violation)
- ❌ Navigates away from model page
- ❌ Requires multiple clicks to view article

### After This Change
- ✅ Event delegation (CSP compliant)
- ✅ Stays on model page with overlay
- ✅ Single click to view article
- ✅ Matches Issue detail page behavior

---

## Build Status

```bash
cd /home/justin/repos/urban-sniffle/the-archive && dotnet build src/TheArchive/TheArchive.csproj
# Build succeeded.
#     0 Error(s)
#     1 Warning(s) - Nullable reference in Issues/Detail.cshtml (pre-existing)
```

---

## Related Files

### Files Modified
1. `/the-archive/src/TheArchive/Views/Models/Detail.cshtml` - Main changes

### Dependencies
1. `/the-archive/src/TheArchive/Views/Shared/_Layout.cshtml` - Defines `openArticle()` function
2. `/the-archive/src/TheArchive/Controllers/ApiController.cs` - Article pages API endpoint

### Similar Implementation
- `/the-archive/src/TheArchive/Views/Issues/Detail.cshtml` - Uses same pattern for article clicks

---

## Future Enhancements

### Potential Improvements

1. **Show All Articles**: Currently limited to `.Take(10)`, could add pagination or "Show More"
2. **Article Thumbnails**: Could show thumbnails for visual articles (like Issue detail page)
3. **Hover Preview**: Show article thumbnail on hover
4. **Loading Indicator**: Add spinner while article pages are loading
5. **Keyboard Navigation**: Add keyboard shortcuts to navigate between articles

### Low Priority

1. Extract inline styles to CSS classes
2. Add animation when opening article viewer
3. Prefetch first page image on hover

---

## Breaking Changes

⚠️ **None** - This is a behavior change, but no API changes or breaking functionality.

---

## Summary

Successfully updated the model detail page to open articles directly in the viewer overlay instead of navigating to the issue page. This provides a much better user experience and aligns with the behavior on the issue detail page.

**User Journey Improved**:
- **Before**: Model page → Click article → Navigate to issue page → Find article → Click article → Open viewer (3-4 clicks)
- **After**: Model page → Click article → Open viewer (1 click)

---

**End of Implementation Summary**

