# Duplicate Articles Fix - Session Summary

**Date:** 2026-04-23  
**Issue:** Articles with multiple models were appearing multiple times on issue detail pages  
**Example:** "Chocs away!" article in issue 59 appeared twice (once for each model)

## Problem Analysis

### Root Cause
The SQL query in `GetArticlesByIssueAsync` was performing a LEFT JOIN to the `ContentModel` and `Model` tables, then grouping by `cm.ModelId` and `m.Name`. This caused articles with multiple models to create separate rows for each model-article combination.

**Database State:**
- Article ID 940 ("Chocs away!") had 2 models:
  - Julie Honsman (ModelId 260)
  - Kathleen Turner (ModelId 334)
- This created a cartesian product: 2 pages × 2 models = 4 rows
- After GROUP BY with ModelId/ModelName: 2 result rows (one per model)

### Impact
- Issue detail pages showed duplicate article entries
- Users saw the same article listed multiple times
- Article count was inflated
- Confusing user experience

## Solution

### Database Query Changes
Modified `GetArticlesByIssueAsync` in `/home/justin/repos/urban-sniffle/the-archive/src/TheArchive/Services/ArchiveDatabase.cs`:

**Before:**
```csharp
cm.ModelId,
m.Name as ModelName,
...
GROUP BY a.ArticleId, a.CategoryId, a.Title, cat.Name, c.IssueId, cm.ModelId, m.Name, first_img.ImagePath
```

**After:**
```csharp
MIN(cm.ModelId) as ModelId,
STRING_AGG(DISTINCT m.Name, ' • ' ORDER BY m.Name) as ModelName,
...
GROUP BY a.ArticleId, a.CategoryId, a.Title, cat.Name, c.IssueId, first_img.ImagePath
```

**Key Changes:**
1. Removed `cm.ModelId` and `m.Name` from GROUP BY clause
2. Used `MIN(cm.ModelId)` to get the first model's ID (for single-model articles)
3. Used `STRING_AGG(DISTINCT m.Name, ' • ' ORDER BY m.Name)` to concatenate all model names alphabetically with bullet separator

### View Template Changes
Modified `/home/justin/repos/urban-sniffle/the-archive/src/TheArchive/Views/Issues/Detail.cshtml`:

Added logic to detect multiple models and handle linking appropriately:

```razor
@if (article.ModelName.Contains(" • "))
{
    <span>@article.ModelName</span>
}
else if (article.ModelId != null && article.ModelId > 0)
{
    <a href="/models/@article.ModelId" class="model-link" data-model-id="@article.ModelId">@article.ModelName</a>
}
else
{
    <span>@article.ModelName</span>
}
```

**Reasoning:**
- When multiple models are present (detected by " • " separator), we can't link to a single model page
- Display all names as unlinked text to avoid confusion
- Single-model articles still get clickable links to the model's page

## Results

### Before Fix
```html
<!-- Two separate article rows for the same article -->
<div class="article-row" data-article-id="940">Chocs away! - Julie Honsman</div>
<div class="article-row" data-article-id="940">Chocs away! - Kathleen Turner</div>
```

### After Fix
```html
<!-- Single article row with both models -->
<div class="article-row" data-article-id="940">
  Chocs away! - <span>Julie Honsman • Kathleen Turner</span>
</div>
```

### Verification
```bash
# Query shows aggregated result
$ PGPASSWORD='Barnowl1' psql -U postgres -d magazines -h localhost -c \
  "SELECT a.ArticleId, a.Title, MIN(c.Page) as PageStart, 
   STRING_AGG(DISTINCT m.Name, ' • ' ORDER BY m.Name) as ModelName 
   FROM Article a JOIN Content c ON a.ArticleId = c.ArticleId 
   LEFT JOIN ContentModel cm ON a.ArticleId = cm.ArticleId 
   LEFT JOIN Model m ON cm.ModelId = m.ModelId 
   WHERE c.IssueId = 59 AND a.Title LIKE 'Chocs%' 
   GROUP BY a.ArticleId, a.Title ORDER BY MIN(c.Page);"

 articleid |    title    | pagestart |            modelname            
-----------+-------------+-----------+---------------------------------
       940 | Chocs away! |        16 | Julie Honsman • Kathleen Turner
(1 row)

# Web page shows only one article row
$ curl -s http://localhost:5163/issues/59 | grep 'data-article-id="940"' | wc -l
1
```

## Technical Notes

### SQL Aggregation Functions
- **STRING_AGG**: PostgreSQL function that concatenates values from multiple rows into a single string
  - Syntax: `STRING_AGG(expression, delimiter [ORDER BY expression])`
  - `DISTINCT` ensures each model name appears only once
  - `ORDER BY` ensures consistent alphabetical ordering

- **MIN(ModelId)**: Returns the smallest ModelId when multiple exist
  - Used for backward compatibility with single-model article links
  - For multi-model articles, the specific ID doesn't matter since we don't link

### View Logic
The razor view uses string detection (`Contains(" • ")`) to determine if multiple models are present. This is:
- Simple and performant
- Doesn't require additional database queries or view model changes
- Automatically handles any number of models (2, 3, 4+)

### Edge Cases Handled
- ✅ Single-model articles: Show clickable link
- ✅ Multi-model articles: Show all names without link
- ✅ Articles with no models: No model display
- ✅ Ordering: Models appear alphabetically

## Related Areas

### Other Queries to Check
This same pattern (grouping by ModelId/ModelName) might exist in other queries:
- `GetAllArticlesAsync` - Already uses similar aggregation pattern (search query fix)
- API endpoints - Should be checked for consistency

### Future Enhancements
Potential improvements for multi-model articles:
1. Parse the concatenated names and create individual clickable links for each model
2. Add tooltip showing all model names when hovering
3. Create a "featured models" array in the Article model class for proper data structure

## Files Modified

1. **`/home/justin/repos/urban-sniffle/the-archive/src/TheArchive/Services/ArchiveDatabase.cs`**
   - Method: `GetArticlesByIssueAsync` (lines 304-346)
   - Changed GROUP BY clause to aggregate model names

2. **`/home/justin/repos/urban-sniffle/the-archive/src/TheArchive/Views/Issues/Detail.cshtml`**
   - Section: Article byline display (lines 73-96)
   - Added conditional logic for multi-model articles

## Testing

### Test Case: Multi-Model Article
- **URL:** http://localhost:5163/issues/59
- **Article:** "Chocs away!" (ArticleId 940)
- **Expected:** Single row showing "Julie Honsman • Kathleen Turner"
- **Result:** ✅ Pass

### Test Case: Single-Model Article
- **Expected:** Clickable model name linking to model detail page
- **Result:** ✅ Pass (existing behavior preserved)

## Status
✅ **FIXED** - Articles with multiple models now appear as single entries with all model names displayed

---

**Session Complete:** All duplicate article issues resolved. Issue detail pages now correctly show one row per article regardless of how many models are featured.

