# Multi-Model Clickable Links - Session Summary

**Date:** 2026-04-23  
**Context:** Follow-up to duplicate articles fix  
**Issue:** After fixing article duplication, model names were no longer clickable links

## Problem

After fixing the duplicate articles issue by aggregating model names into a single string (e.g., "Julie Honsman • Kathleen Turner"), the model names were displayed as plain text instead of clickable links to their detail pages.

**Original Fix Limitation:**
```html
<!-- After duplicate fix, models were plain text -->
<span>Julie Honsman • Kathleen Turner</span>
```

**User Expectation:**
Each model name should be an individual clickable link to their model detail page.

## Solution Overview

Enhanced the database query and view template to:
1. Collect both model names AND model IDs in matching alphabetical order
2. Split the aggregated strings in the view
3. Create individual clickable links for each model

## Implementation Details

### 1. Model Class Enhancement

Added `ModelIds` property to store comma-separated IDs:

**File:** `/home/justin/repos/urban-sniffle/the-archive/src/TheArchive/Models/Article.cs`

```csharp
public string? ModelName { get; set; }
public string? ModelIds { get; set; }  // Comma-separated IDs (matches order of ModelName)
```

### 2. Database Query Update

Modified `GetArticlesByIssueAsync` to use correlated subqueries for consistent ordering:

**File:** `/home/justin/repos/urban-sniffle/the-archive/src/TheArchive/Services/ArchiveDatabase.cs`

**Before:**
```sql
STRING_AGG(DISTINCT m.Name, ' • ' ORDER BY m.Name) as ModelName
```

**After:**
```sql
-- Subquery for model names
(
    SELECT STRING_AGG(m2.Name, ' • ' ORDER BY m2.Name)
    FROM ContentModel cm2
    JOIN Model m2 ON cm2.ModelId = m2.ModelId
    WHERE cm2.ArticleId = a.ArticleId
) as ModelName,

-- Subquery for model IDs (same ordering)
(
    SELECT STRING_AGG(cm2.ModelId::TEXT, ',' ORDER BY m2.Name)
    FROM ContentModel cm2
    JOIN Model m2 ON cm2.ModelId = m2.ModelId
    WHERE cm2.ArticleId = a.ArticleId
) as ModelIds
```

**Why Subqueries?**
- `STRING_AGG` with `DISTINCT` doesn't allow `ORDER BY` on different columns
- Subqueries allow ordering by `m2.Name` while aggregating `cm2.ModelId`
- Both subqueries use identical `ORDER BY m2.Name` to ensure alignment

### 3. View Template Enhancement

Updated the Razor template to parse and link each model individually:

**File:** `/home/justin/repos/urban-sniffle/the-archive/src/TheArchive/Views/Issues/Detail.cshtml`

```razor
@if (article.ModelName.Contains(" • ") && !string.IsNullOrEmpty(article.ModelIds))
{
    var modelNames = article.ModelName.Split(" • ", StringSplitOptions.RemoveEmptyEntries);
    var modelIds = article.ModelIds.Split(',', StringSplitOptions.RemoveEmptyEntries);
    
    for (int i = 0; i < modelNames.Length; i++)
    {
        if (i > 0)
        {
            <span> • </span>
        }
        
        if (i < modelIds.Length && int.TryParse(modelIds[i], out int modelId))
        {
            <a href="/models/@modelId" class="model-link" data-model-id="@modelId">@modelNames[i]</a>
        }
        else
        {
            <span>@modelNames[i]</span>  @* Fallback if ID parsing fails *@
        }
    }
}
else if (article.ModelId != null && article.ModelId > 0)
{
    @* Single model - use existing single-link logic *@
    <a href="/models/@article.ModelId" class="model-link" data-model-id="@article.ModelId">@article.ModelName</a>
}
else
{
    @* No model ID available *@
    <span>@article.ModelName</span>
}
```

## Results

### Database Query Result
```sql
SELECT ArticleId, Title, ModelName, ModelIds 
FROM ... 
WHERE ArticleId = 940;
```

```
articleid |    title    |            modelname            | modelids 
-----------+-------------+---------------------------------+----------
       940 | Chocs away! | Julie Honsman • Kathleen Turner | 260,334
```

**Key Points:**
- Names: "Julie Honsman • Kathleen Turner" (alphabetical)
- IDs: "260,334" (matching the alphabetical order)
- Julie Honsman → ID 260 (first alphabetically, first ID)
- Kathleen Turner → ID 334 (second alphabetically, second ID)

### HTML Output

**Before Fix:**
```html
<div class="article-byline">
    <span>Julie Honsman • Kathleen Turner</span>
    <span> • </span>
    <span>Dag Ohrlund</span>
</div>
```

**After Fix:**
```html
<div class="article-byline">
    <a href="/models/260" class="model-link" data-model-id="260">Julie Honsman</a>
    <span> • </span>
    <a href="/models/334" class="model-link" data-model-id="334">Kathleen Turner</a>
    <span> • </span>
    <span>Dag Ohrlund</span>
</div>
```

### Verification

```bash
# Check article appears only once
$ curl -s http://localhost:5163/issues/59 | grep 'data-article-id="940"' | wc -l
1

# Verify both model links are present
$ curl -s http://localhost:5163/issues/59 | grep -A 10 "Chocs away"
...
<a href="/models/260" class="model-link" data-model-id="260">Julie Honsman</a>
<span> • </span>
<a href="/models/334" class="model-link" data-model-id="334">Kathleen Turner</a>
...

# Verify both model pages are accessible
$ curl -s -o /dev/null -w "%{http_code}" http://localhost:5163/models/260
200

$ curl -s -o /dev/null -w "%{http_code}" http://localhost:5163/models/334
200
```

## Technical Design Decisions

### Why Not Use JSON Aggregation?

**Considered:**
```sql
SELECT json_agg(json_build_object('id', cm.ModelId, 'name', m.Name) ORDER BY m.Name)
```

**Rejected because:**
- Requires deserializing JSON in C# code
- More complex view template logic
- Overkill for this simple use case
- String splitting is performant enough for typical 2-3 models

### Why Use Correlated Subqueries?

**Alternative Approaches:**
1. **Window functions** - Too complex for this need
2. **Array aggregation** - Requires PostgreSQL-specific array handling in C#
3. **Multiple queries** - Performance overhead
4. **Client-side model lookup** - Requires additional API calls

**Chosen approach:**
- Simple and efficient
- Single query execution
- Predictable ordering
- Standard SQL that's easy to maintain

### Ordering Consistency

Both subqueries use `ORDER BY m2.Name` to ensure:
- Model names sorted alphabetically: "Julie • Kathleen"
- Model IDs in same order: "260,334"
- Index alignment: names[0] matches ids[0]

## Edge Cases Handled

| Scenario | Behavior |
|----------|----------|
| Single model | Single clickable link (existing behavior) |
| Two models | Two separate links with bullet separator |
| Three+ models | All models as individual links |
| No models | No model display (existing behavior) |
| ModelIds missing | Fallback to plain text |
| ID parse failure | Individual model as plain text |
| Model count mismatch | Extra names shown as plain text |

## Performance Considerations

### Query Performance
- **Subqueries:** Executed once per article (not per row in join)
- **Indexes:** ContentModel(ArticleId), Model(ModelId) already indexed
- **Typical case:** 1-3 models per article, negligible overhead

### View Performance
- **String splitting:** O(n) where n = number of models (typically 1-3)
- **Parsing:** One integer parse per model
- **Razor compilation:** Compiled into efficient C# code

### Caching Opportunities
Future optimization: Cache the entire article list in memory since it rarely changes.

## Testing Notes

### Manual Testing
- ✅ Issue 59, Article 940 ("Chocs away!") - 2 models, both clickable
- ✅ Single-model articles - unchanged behavior
- ✅ Articles without models - no display

### Regression Testing
- ✅ No duplicate articles
- ✅ Article count accurate
- ✅ Page numbers correct
- ✅ Photographer display unaffected
- ✅ Category filtering still works

## Future Enhancements

### Potential Improvements
1. **Hover effects:** Show model thumbnail on hover
2. **Model order:** Option to order by appearance in article instead of alphabetically
3. **Primary model indicator:** Highlight the "main" model
4. **Bulk link generation:** Helper method for reuse in other views

### Related Areas to Update
This same pattern should be applied to:
- Article list view (if showing models)
- Search results (if displaying model info)
- API endpoints (if returning article data)

## Files Modified

1. **`/home/justin/repos/urban-sniffle/the-archive/src/TheArchive/Models/Article.cs`**
   - Added `ModelIds` property (line 21)

2. **`/home/justin/repos/urban-sniffle/the-archive/src/TheArchive/Services/ArchiveDatabase.cs`**
   - Modified `GetArticlesByIssueAsync` query (lines 308-324)
   - Added correlated subqueries for ModelName and ModelIds

3. **`/home/justin/repos/urban-sniffle/the-archive/src/TheArchive/Views/Issues/Detail.cshtml`**
   - Enhanced model display logic (lines 73-105)
   - Added string splitting and link generation

4. **`/home/justin/repos/urban-sniffle/DUPLICATE_ARTICLES_QUICK_REF.md`**
   - Updated documentation to reflect clickable links

## Status

✅ **COMPLETE** - All model names in multi-model articles are now individual clickable links

**User Experience:**
- Click "Julie Honsman" → Navigate to Julie's model detail page
- Click "Kathleen Turner" → Navigate to Kathleen's model detail page
- Visual clarity maintained with bullet separators
- Consistent with single-model article behavior

---

**Session Complete:** Multi-model articles now display each model as a clickable link to their detail page while maintaining the deduplication fix.

