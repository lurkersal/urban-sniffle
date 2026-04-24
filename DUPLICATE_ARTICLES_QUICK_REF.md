# Duplicate Articles Fix - Quick Reference

## Problem
Articles with multiple models appeared multiple times on issue pages.

## Root Cause
SQL query was grouping by `ModelId` and `ModelName`, creating one row per model.

## Solution
Use correlated subqueries to aggregate model names and IDs in consistent order:

```sql
-- Aggregate model names alphabetically
(
    SELECT STRING_AGG(m2.Name, ' • ' ORDER BY m2.Name)
    FROM ContentModel cm2
    JOIN Model m2 ON cm2.ModelId = m2.ModelId
    WHERE cm2.ArticleId = a.ArticleId
) as ModelName,

-- Aggregate model IDs in same order as names
(
    SELECT STRING_AGG(cm2.ModelId::TEXT, ',' ORDER BY m2.Name)
    FROM ContentModel cm2
    JOIN Model m2 ON cm2.ModelId = m2.ModelId
    WHERE cm2.ArticleId = a.ArticleId
) as ModelIds
```

## View Template Update
Parse names and IDs to create individual clickable links:

```razor
@if (article.ModelName.Contains(" • ") && !string.IsNullOrEmpty(article.ModelIds))
{
    var modelNames = article.ModelName.Split(" • ", StringSplitOptions.RemoveEmptyEntries);
    var modelIds = article.ModelIds.Split(',', StringSplitOptions.RemoveEmptyEntries);
    
    for (int i = 0; i < modelNames.Length; i++)
    {
        if (i > 0) { <span> • </span> }
        
        if (i < modelIds.Length && int.TryParse(modelIds[i], out int modelId))
        {
            <a href="/models/@modelId">@modelNames[i]</a>  @* Each model is clickable *@
        }
    }
}
```

## Files Changed
- `Models/Article.cs` - Added ModelIds property
- `Services/ArchiveDatabase.cs` - GetArticlesByIssueAsync()
- `Views/Issues/Detail.cshtml` - Article byline display with individual links

## Result
- Single row per article
- Multiple model names shown as clickable links: [Model1](#) • [Model2](#) • [Model3](#)
- Alphabetically sorted
- Each model name links to their detail page

## Test
```bash
# Should return 1 (single article row)
curl -s http://localhost:5163/issues/59 | grep 'data-article-id="940"' | wc -l

# Should show two separate links
curl -s http://localhost:5163/issues/59 | grep -o 'href="/models/[0-9]*"' | grep -A1 -B1 940
```

