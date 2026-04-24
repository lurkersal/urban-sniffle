# Primary Keys Not Chronological - Critical Fix

**Date**: April 21, 2026  
**Status**: ✅ **FIXED** - Changed from IssueId ordering to Year/Volume/Number

---

## Problem

Initial implementation incorrectly ordered articles by `IssueId` (a primary key), assuming it was chronological. **This assumption was wrong.**

---

## Why Primary Keys Don't Work

### Database Primary Keys Are NOT Chronological

**IssueId, ArticleId, ContentId** are all primary keys designed for:
- Uniqueness
- Foreign key relationships
- Database indexing

They are **NOT** designed for chronological ordering.

### Real-World Example

During data import, issues might be processed in any order:

```
Import batch 1: Issues from 2000 → IssueIds 1-50
Import batch 2: Issues from 1999 → IssueIds 51-100  ← Earlier dates!
Import batch 3: Issues from 2001 → IssueIds 101-150
```

Result: IssueId 55 might be from 1999, even though IssueId 25 is from 2000.

**Ordering by IssueId would show**: 2000 issues → 1999 issues → 2001 issues ❌

---

## The Fix

### Before (WRONG)
```sql
ORDER BY c.IssueId ASC, MIN(c.Page) ASC
```

### After (CORRECT)
```sql
SELECT DISTINCT
    a.ArticleId,
    ...,
    i.Year,      -- Added to SELECT (required for ORDER BY)
    i.Volume,    -- Added to SELECT (required for ORDER BY)
    i.Number     -- Added to SELECT (required for ORDER BY)
FROM ...
LEFT JOIN Issue i ON c.IssueId = i.IssueId
...
GROUP BY ..., i.Year, i.Volume, i.Number
ORDER BY i.Year ASC, i.Volume ASC, i.Number ASC, MIN(c.Page) ASC
```

**PostgreSQL Requirement**: When using `SELECT DISTINCT`, all columns in `ORDER BY` must appear in the `SELECT` list.

---

## Implementation

### Changes Made

1. **Added Issue Table Join**
   ```sql
   LEFT JOIN Issue i ON c.IssueId = i.IssueId
   ```

2. **Added Date Fields to SELECT** (required for PostgreSQL DISTINCT + ORDER BY)
   ```sql
   SELECT DISTINCT
       a.ArticleId,
       ...,
       i.Year,
       i.Volume,
       i.Number
   ```

3. **Added Date Fields to GROUP BY**
   ```sql
   GROUP BY a.ArticleId, ..., i.Year, i.Volume, i.Number
   ```

4. **Changed ORDER BY to Use Date Fields**
   ```sql
   ORDER BY i.Year ASC, i.Volume ASC, i.Number ASC, MIN(c.Page) ASC
   ```

5. **Added Properties to Article Model**
   ```csharp
   public int Year { get; set; }
   public int Volume { get; set; }
   public int Number { get; set; }
   ```

### File Modified
- `/src/TheArchive/Services/ArchiveDatabase.cs` - Method: `GetArticlesByModelAsync(int modelId)`
- `/src/TheArchive/Models/Article.cs` - Added Year, Volume, Number properties

---

## PostgreSQL DISTINCT Requirement

### Error Encountered

```
PostgresException: 42P10: for SELECT DISTINCT, ORDER BY expressions must appear in select list
```

### PostgreSQL Rule

When using `SELECT DISTINCT`, **all columns in the ORDER BY clause must also be in the SELECT list**.

**Why?**: DISTINCT eliminates duplicate rows based on selected columns. If you order by columns not in SELECT, PostgreSQL can't determine which duplicate to keep.

### Example

```sql
-- ❌ WRONG - Year not in SELECT
SELECT DISTINCT ArticleId, Title
ORDER BY Year

-- ✅ CORRECT - Year in SELECT
SELECT DISTINCT ArticleId, Title, Year
ORDER BY Year
```

### Our Fix

**Before** (caused error):
```sql
SELECT DISTINCT a.ArticleId, a.Title, ...
ORDER BY i.Year, i.Volume, i.Number  -- Not in SELECT!
```

**After** (works):
```sql
SELECT DISTINCT a.ArticleId, a.Title, ..., i.Year, i.Volume, i.Number
ORDER BY i.Year, i.Volume, i.Number  -- Now in SELECT ✓
```

---

## Why This Matters

### Incorrect Result (by IssueId)
```
Model Articles:
1. IssueId: 205 (actually 1999)
2. IssueId: 206 (actually 2001)  
3. IssueId: 207 (actually 2000)
```
Completely random ordering!

### Correct Result (by Year/Volume/Number)
```
Model Articles:
1. 1999, Vol 11, No 6
2. 2000, Vol 12, No 3
3. 2001, Vol 13, No 1
```
True chronological order!

---

## Lesson Learned

### Never Assume Primary Keys Are Chronological

**Rule of Thumb**:
- Primary keys = Unique identifiers
- Chronological ordering = Date/timestamp fields

**Always ask**:
1. Is this field a primary key?
2. If yes, is there a date field I should use instead?
3. Can records be inserted out of chronological order?

### Common Pitfalls

❌ **Wrong Assumptions**:
```sql
ORDER BY UserId          -- Users can be imported in any order
ORDER BY ProductId       -- Products added whenever
ORDER BY IssueId         -- Issues imported in batches
ORDER BY OrderId         -- Orders can be backdated
```

✅ **Correct Approach**:
```sql
ORDER BY CreatedAt       -- Timestamp field
ORDER BY PublicationDate -- Semantic date field
ORDER BY Year, Month     -- Composite date
ORDER BY TransactionDate -- Business date
```

---

## Other Potential Issues in Codebase

### Places to Review

Search for any queries using primary key ordering:

```bash
# Find potential issues
grep -r "ORDER BY.*Id" src/
```

### Candidates to Check

1. **Magazine Ordering** - MagazineId vs Name
2. **Issue Ordering** - IssueId vs Year/Volume/Number ✅ (already correct)
3. **Article Ordering** - ArticleId vs IssueId + Page ✅ (now fixed)
4. **Model Ordering** - ModelId vs Name ✅ (ordered by name)
5. **Content Ordering** - ContentId vs Page ✅ (ordered by page)

---

## Best Practices

### For Chronological Ordering

1. **Use Semantic Date Fields**
   - Publication date
   - Created timestamp
   - Year/Month/Day fields

2. **Never Use Primary Keys**
   - They guarantee uniqueness, not order
   - Import order != chronological order

3. **Document Assumptions**
   - Comment why a field is used for ordering
   - Note if ordering is chronological vs alphabetical vs custom

4. **Test with Real Data**
   - Import data out of order
   - Verify ordering still works

---

## Example: Correct Date Ordering

### Magazine Issues (from ArchiveDatabase.cs)

```sql
-- ✅ CORRECT - Uses actual date fields
SELECT i.*
FROM Issue i
JOIN Magazine m ON i.MagazineId = m.MagazineId
WHERE m.MagazineId = @MagazineId
ORDER BY i.Year DESC, i.Volume DESC, i.Number DESC
```

### Model Articles (now fixed)

```sql
-- ✅ CORRECT - Uses date fields from Issue table
SELECT a.*
FROM Article a
JOIN Issue i ON c.IssueId = i.IssueId
ORDER BY i.Year ASC, i.Volume ASC, i.Number ASC, MIN(c.Page) ASC
```

---

## Summary

**Changed from**:
```sql
ORDER BY c.IssueId ASC  -- Primary key (wrong!)
```

**Changed to**:
```sql
ORDER BY i.Year ASC, i.Volume ASC, i.Number ASC  -- Semantic dates (correct!)
```

This ensures articles are truly ordered chronologically by publication date, not by arbitrary database IDs assigned during import.

---

## Build Status

```bash
cd /home/justin/repos/urban-sniffle/the-archive && dotnet build
# Build succeeded. 0 Error(s)
```

---

**End of Critical Fix**

