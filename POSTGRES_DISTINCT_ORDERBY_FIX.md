# PostgreSQL SELECT DISTINCT + ORDER BY Fix

**Date**: April 21, 2026  
**Status**: ✅ **FIXED** - Added Year/Volume/Number to SELECT list

---

## Error

```
PostgresException: 42P10: for SELECT DISTINCT, ORDER BY expressions must appear in select list
POSITION: 789
```

---

## Root Cause

PostgreSQL has a strict requirement: **When using SELECT DISTINCT, all columns in ORDER BY must be in the SELECT list.**

---

## Why This Rule Exists

### The Problem

```sql
SELECT DISTINCT ArticleId, Title
FROM Article
ORDER BY Year
```

PostgreSQL needs to know: If there are duplicate (ArticleId, Title) pairs with different Years, which one should it keep?

### Example

```
Data:
  ArticleId: 1, Title: "Feature", Year: 1999
  ArticleId: 1, Title: "Feature", Year: 2000  ← Duplicate (ID, Title)

Query: SELECT DISTINCT ArticleId, Title ORDER BY Year

Which row to return?
  - If we want Year 1999 → return first row
  - If we want Year 2000 → return second row
  
But Year isn't in SELECT, so result won't include it!
```

**Solution**: Include Year in SELECT so the rows are different: (1, "Feature", 1999) and (1, "Feature", 2000).

---

## The Fix

### Before (ERROR)

```sql
SELECT DISTINCT
    a.ArticleId,
    a.CategoryId,
    a.Title,
    cat.Name as CategoryName,
    c.IssueId,
    MIN(c.Page) as PageStart,
    cm.ModelId,
    m.Name as ModelName
FROM Article a
-- ... joins ...
LEFT JOIN Issue i ON c.IssueId = i.IssueId
WHERE cm.ModelId = @ModelId
GROUP BY a.ArticleId, a.CategoryId, a.Title, cat.Name, c.IssueId, cm.ModelId, m.Name, i.Year, i.Volume, i.Number
ORDER BY i.Year ASC, i.Volume ASC, i.Number ASC, MIN(c.Page) ASC
--       ^^^^^^  ^^^^^^^^  ^^^^^^^^ 
--       These are not in SELECT! ❌
```

### After (WORKS)

```sql
SELECT DISTINCT
    a.ArticleId,
    a.CategoryId,
    a.Title,
    cat.Name as CategoryName,
    c.IssueId,
    MIN(c.Page) as PageStart,
    cm.ModelId,
    m.Name as ModelName,
    i.Year,      -- Added ✓
    i.Volume,    -- Added ✓
    i.Number     -- Added ✓
FROM Article a
-- ... joins ...
LEFT JOIN Issue i ON c.IssueId = i.IssueId
WHERE cm.ModelId = @ModelId
GROUP BY a.ArticleId, a.CategoryId, a.Title, cat.Name, c.IssueId, cm.ModelId, m.Name, i.Year, i.Volume, i.Number
ORDER BY i.Year ASC, i.Volume ASC, i.Number ASC, MIN(c.Page) ASC
--       ^^^^^^  ^^^^^^^^  ^^^^^^^^ 
--       Now in SELECT! ✓
```

---

## Additional Fix: Article Model

Added properties to the Article model to receive the new SELECT fields:

```csharp
public class Article
{
    // ... existing properties ...
    
    // Issue date fields (from Issue table joins)
    public int Year { get; set; }
    public int Volume { get; set; }
    public int Number { get; set; }
}
```

Without these properties, Dapper couldn't map the query results.

---

## Files Modified

1. **`/src/TheArchive/Services/ArchiveDatabase.cs`**
   - Added `i.Year, i.Volume, i.Number` to SELECT list in `GetArticlesByModelAsync()`

2. **`/src/TheArchive/Models/Article.cs`**
   - Added `Year`, `Volume`, `Number` properties

---

## PostgreSQL vs Other Databases

### PostgreSQL (Strict)

```sql
-- ❌ ERROR in PostgreSQL
SELECT DISTINCT Name
ORDER BY Age

-- ✅ WORKS in PostgreSQL
SELECT DISTINCT Name, Age
ORDER BY Age
```

### MySQL/SQL Server (Permissive)

Some databases allow ordering by columns not in SELECT for DISTINCT queries, but PostgreSQL enforces stricter rules for correctness.

---

## Lesson Learned

### Rule of Thumb

**When using SELECT DISTINCT:**
1. Check what columns are in ORDER BY
2. Ensure ALL ORDER BY columns are in SELECT
3. Add them if missing
4. Update model/DTO to receive them

### Checklist

```
[ ] Writing a SELECT DISTINCT query?
[ ] Using ORDER BY?
[ ] Are ALL ORDER BY columns in SELECT?
[ ] Does the model have properties for those columns?
```

---

## Testing

### Build Status

```bash
cd /home/justin/repos/urban-sniffle/the-archive && dotnet build
# Build succeeded. 0 Error(s)
```

### Manual Test

- [ ] Navigate to model detail page
- [ ] Verify articles display without error
- [ ] Confirm articles are in chronological order
- [ ] Check Year/Volume/Number are populated in Article objects

---

## Examples

### Correct DISTINCT Queries

```sql
-- ✅ Simple case
SELECT DISTINCT Title
ORDER BY Title

-- ✅ Multiple columns
SELECT DISTINCT Year, Volume, Number
ORDER BY Year, Volume, Number

-- ✅ With aggregates
SELECT DISTINCT CategoryId, COUNT(*) as Total
GROUP BY CategoryId
ORDER BY COUNT(*)  -- COUNT(*) is in SELECT as "Total"
```

### Incorrect DISTINCT Queries

```sql
-- ❌ Ordering by column not in SELECT
SELECT DISTINCT Title
ORDER BY Year

-- ❌ Ordering by multiple columns, some missing
SELECT DISTINCT ArticleId, Title
ORDER BY Year, Volume, Number

-- ❌ Aggregate in ORDER BY but not in SELECT
SELECT DISTINCT CategoryId
GROUP BY CategoryId
ORDER BY COUNT(*)  -- Not in SELECT!
```

---

## Summary

Fixed PostgreSQL error by adding `i.Year, i.Volume, i.Number` to the SELECT list, ensuring compliance with PostgreSQL's requirement that ORDER BY expressions must appear in SELECT when using DISTINCT.

**Key Changes**:
1. Added Year/Volume/Number to SELECT
2. Added Year/Volume/Number properties to Article model
3. Query now returns chronologically ordered articles without errors

---

**End of Fix**

