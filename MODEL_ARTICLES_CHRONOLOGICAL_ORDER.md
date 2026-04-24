# Model Detail Page - Articles Chronological Ordering

**Date**: April 21, 2026  
**Status**: ✅ **COMPLETED** - Articles now listed in chronological order

---

## Summary

Articles on the model detail page are now listed in chronological order (earliest to latest) based on issue date and page number, making it easier to see a model's career timeline.

---

## Change

### Before
Articles were ordered by `ArticleId DESC` (newest article IDs first), which doesn't necessarily correspond to publication date.

### After
Articles are ordered by actual publication date fields:
1. **Year ASC** (earliest year first)
2. **Volume ASC** (earliest volume within that year)
3. **Number ASC** (earliest issue number within that volume)
4. **PageStart ASC** (earliest page within that issue)

This provides a true chronological view based on publication date, not database primary keys.

**Important**: We cannot use IssueId or ArticleId for ordering as these are primary keys and not guaranteed to be chronological.

---

## Implementation

### File Modified

**`/src/TheArchive/Services/ArchiveDatabase.cs`**

**Method**: `GetArticlesByModelAsync(int modelId)`

**Change**:
```csharp
// Before
ORDER BY a.ArticleId DESC

// After  
ORDER BY i.Year ASC, i.Volume ASC, i.Number ASC, MIN(c.Page) ASC
```

**Full Query**:
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
JOIN Category cat ON a.CategoryId = cat.CategoryId
JOIN ContentModel cm ON a.ArticleId = cm.ArticleId
JOIN Model m ON cm.ModelId = m.ModelId
LEFT JOIN Content c ON a.ArticleId = c.ArticleId
LEFT JOIN Issue i ON c.IssueId = i.IssueId
WHERE cm.ModelId = @ModelId
GROUP BY a.ArticleId, a.CategoryId, a.Title, cat.Name, c.IssueId, cm.ModelId, m.Name, i.Year, i.Volume, i.Number
ORDER BY i.Year ASC, i.Volume ASC, i.Number ASC, MIN(c.Page) ASC
```

**Key Changes**:
1. Added `JOIN Issue i ON c.IssueId = i.IssueId` to access date fields
2. Added `i.Year, i.Volume, i.Number` to GROUP BY (required for ordering)
3. Changed ORDER BY to use actual date fields, not primary keys

---

## Benefits

✅ **Chronological Timeline** - See model's career progression from start to finish  
✅ **Consistent Ordering** - Same order every time, not arbitrary by ArticleId  
✅ **Intuitive Display** - Matches expected date-based sorting  
✅ **Easy Navigation** - Find earliest/latest appearances quickly  

---

## User Experience

### Before
```
Model: Jane Doe
Articles:
  - Article #125: Pictorial (somewhere in 2001)
  - Article #98:  Cover (somewhere in 1999)
  - Article #115: Feature (somewhere in 2000)
```
Order was unpredictable and didn't reflect timeline.

### After
```
Model: Jane Doe
Articles:
  - Issue 100 (1999): Cover
  - Issue 112 (2000): Feature  
  - Issue 125 (2001): Pictorial
```
Order shows career progression chronologically.

---

## Examples

### Example 1: Model with Multiple Appearances

**Database**:
```
Model: Sarah Smith
Articles:
  - ArticleId: 50, Year: 2000, Volume: 12, Number: 3, Page: 45, Category: Model
  - ArticleId: 25, Year: 1999, Volume: 11, Number: 6, Page: 1,  Category: Cover
  - ArticleId: 75, Year: 2000, Volume: 12, Number: 9, Page: 80, Category: Interview
```

**Display Order**:
1. Year 1999, Vol 11, No 6 - Cover
2. Year 2000, Vol 12, No 3 - Model (page 45)
3. Year 2000, Vol 12, No 9 - Interview (page 80)

### Example 2: Multiple Articles in Same Issue

**Database**:
```
Model: Emma Johnson
Articles:
  - ArticleId: 100, Year: 2001, Volume: 15, Number: 6, Page: 1,  Category: Cover
  - ArticleId: 101, Year: 2001, Volume: 15, Number: 6, Page: 45, Category: Pictorial
  - ArticleId: 102, Year: 2001, Volume: 15, Number: 6, Page: 90, Category: Interview
```

**Display Order** (same year/volume/number, ordered by page):
1. Page 1  - Cover
2. Page 45 - Pictorial
3. Page 90 - Interview

---

## ⚠️ Important: Primary Keys Are Not Chronological

**Critical Lesson**: Database primary keys (IssueId, ArticleId, etc.) are **NOT reliable for chronological ordering**.

### Why Primary Keys Don't Work

1. **Import Order**: IDs are often assigned during data import, not based on publication date
2. **Batch Processing**: Multiple issues may be imported in random order
3. **Data Updates**: New records can have higher IDs but earlier dates
4. **Database Design**: PKs are for uniqueness, not chronology

### Example Problem

```
IssueId: 100 → Year: 2000, Volume: 15, Number: 6
IssueId: 101 → Year: 1999, Volume: 14, Number: 3  ← Earlier date, higher ID!
IssueId: 102 → Year: 2000, Volume: 15, Number: 1
```

If we ordered by IssueId ASC, we'd get: 2000 → 1999 → 2000 (wrong!)

### Correct Approach

Always use **semantic date fields** for chronological ordering:
- ✅ `Year, Volume, Number` (publication metadata)
- ✅ `PublicationDate` (if available)
- ✅ `CreatedAt, UpdatedAt` (for recent records)
- ❌ `IssueId, ArticleId` (primary keys)

---

## Technical Details

### Ordering Logic

1. **Primary Sort**: `i.Year ASC`
   - Orders by publication year
   - Earliest year appears first

2. **Secondary Sort**: `i.Volume ASC`
   - Within the same year, orders by volume number
   - Earlier volumes appear first

3. **Tertiary Sort**: `i.Number ASC`
   - Within the same volume, orders by issue number
   - Earlier issue numbers appear first

4. **Quaternary Sort**: `MIN(c.Page) ASC`
   - Within the same issue, articles ordered by starting page
   - Earlier pages appear first

### Why Year/Volume/Number Instead of IssueId?

**Critical**: IssueId is a database primary key and **not guaranteed to be chronological**.

- Primary keys can be assigned in any order during data import
- They may not reflect publication sequence
- Year, Volume, Number are the actual publication metadata
- This ensures true chronological ordering

### Issue Table Join

The query now joins the Issue table to access:
- `i.Year` - Publication year
- `i.Volume` - Volume number  
- `i.Number` - Issue number within that volume

These fields must be added to the GROUP BY clause since they're used in ORDER BY.

---

## View Display

The view (Detail.cshtml) displays articles with `.Take(10)` showing the first 10 articles chronologically:

```razor
@foreach (var article in Model.Articles.Take(10))
{
    <div class="article-row">
        <div class="article-pg">@article.PageStart</div>
        <div class="article-body">
            <span class="article-cat">@article.CategoryName</span>
            <div class="article-title">@article.Title</div>
        </div>
        <div>Issue @article.IssueId</div>
    </div>
}
```

**Result**: Users see the model's first 10 appearances in chronological order.

---

## Build Status

```bash
cd /home/justin/repos/urban-sniffle/the-archive && dotnet build src/TheArchive/TheArchive.csproj
# Build succeeded. 0 Error(s)
```

---

## Testing

### Manual Testing Checklist

- [ ] Navigate to a model detail page (e.g., `/models/{id}`)
- [ ] Check that articles are ordered from earliest to latest issue
- [ ] Verify Issue IDs increase down the list
- [ ] Check multiple articles in same issue are ordered by page number
- [ ] Confirm first appearance is at the top

### Edge Cases

- [ ] Model with single article → displays correctly
- [ ] Model with no articles → no articles section shown
- [ ] Model with 10+ articles → first 10 shown in chronological order
- [ ] Articles spanning multiple issues → each shown in order

---

## Future Enhancements

Potential improvements:

1. **Year Headers**: Group articles by year with visual separators
2. **Timeline View**: Visual timeline showing career span
3. **Publication Dates**: Show actual Month/Year instead of just IssueId
4. **Sort Toggle**: Allow users to reverse order (latest first)
5. **Pagination**: Show all articles, not just first 10

### Example: Year Headers
```
1999
  - Jan 1999: Cover
  - Jun 1999: Pictorial

2000
  - Mar 2000: Feature
  - Dec 2000: Interview
```

---

## Related Changes

### Other Chronological Orderings in App

- Issue list: Ordered by Year DESC, Volume DESC, Number DESC
- Magazine issues: Ordered by date descending
- Search results: May vary based on search type

### Consistency

This change makes the model detail page consistent with chronological expectations throughout the app.

---

## Summary

Articles on the model detail page are now ordered chronologically (earliest to latest) by issue and page, providing a clear timeline of the model's career progression. This makes it easier to browse a model's work chronologically and understand their career arc.

**User Experience Improved**:
- **Before**: Random order by ArticleId
- **After**: Chronological order from first to most recent appearance

---

**End of Change**

