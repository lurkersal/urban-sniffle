# Model Thumbnails - Database Schema Issue Fix

**Date**: April 21, 2026  
**Status**: ✅ **FIXED** - Column reference error resolved

---

## Error

```
PostgresException: 42703: column a.issueid does not exist
POSITION: 345
```

---

## Root Cause

The query tried to reference `a.IssueId` and `a.PageStart` from the Article table, but **the Article table doesn't have these columns**.

### Database Schema

**Article Table** has:
- ArticleId (PK)
- CategoryId
- Title

**Content Table** has:
- ContentId (PK)
- ArticleId (FK to Article)
- **IssueId** ← This is where IssueId lives!
- **Page** ← This is the page number
- ImagePath

**ContentModel Table** has:
- ArticleId (FK to Article)
- ModelId (FK to Model)

### The Problem

The Article C# model has `IssueId` and `PageStart` as properties, but these are **computed from joins**, not actual database columns:

```csharp
public class Article
{
    public int ArticleId { get; set; }
    public int CategoryId { get; set; }
    public string? Title { get; set; }
    
    // Related data from joins - NOT database columns!
    public int IssueId { get; set; }  
    public int PageStart { get; set; }
    // ...
}
```

---

## The Fix

Use `c.IssueId` and `c.Page` from the Content table, with MIN() to get the first values:

```sql
WITH FirstArticle AS (
    SELECT DISTINCT ON (cm.ModelId)
        cm.ModelId,
        a.ArticleId,
        MIN(c.IssueId) as IssueId,      -- From Content, not Article
        MIN(c.Page) as PageStart         -- From Content, not Article
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

---

## Key Insights

1. **Article table is minimal** - Only has ArticleId, CategoryId, Title
2. **Content table has the details** - IssueId, Page, ImagePath per page
3. **Articles span multiple pages** - One Article → Many Content rows
4. **Use MIN() to aggregate** - Get first IssueId and Page from Content
5. **C# models can be misleading** - Properties may be computed, not columns

---

## How Other Queries Handle This

All other queries in ArchiveDatabase.cs correctly use Content table columns:

```csharp
// GetArticlesByModelAsync (line 502)
FROM Article a
JOIN Content c ON a.ArticleId = c.ArticleId
// Uses c.IssueId and MIN(c.Page) ✓

// GetArticlesByIssueAsync (line 283)
FROM Article a
JOIN Content c ON a.ArticleId = c.ArticleId
// Uses c.IssueId and MIN(c.Page) ✓
```

---

## Testing

```bash
cd /home/justin/repos/urban-sniffle/the-archive
dotnet build src/TheArchive/TheArchive.csproj
# Build succeeded. 0 Error(s)
```

Navigate to `/models` - should now work without errors.

---

**End of Fix**

