# Model Thumbnails - Category Preference Enhancement

**Date**: April 21, 2026  
**Status**: ✅ **COMPLETED** - Thumbnails now prefer model articles over cover articles

---

## Enhancement

Model thumbnails now intelligently select the best image to represent each model by preferring model/pictorial articles over cover articles.

---

## Behavior

### Priority Order

When selecting a thumbnail for a model, the system now uses this priority:

1. **First Priority**: Model, Pictorial, Pinup, Feature articles (category priority 0)
2. **Second Priority**: Cover articles (category priority 1)
3. **Last Priority**: Any other category (category priority 2)

Within each priority level, articles are ordered by:
- IssueId (earliest first)
- Page (lowest page number first)

### Why This Matters

**Before**: A model's thumbnail might be their cover appearance even if they had full pictorial spreads.

**After**: Thumbnails show the model from their pictorial/feature articles, which typically provide better, more representative images than cover shots.

**Example**:
- Model has Cover in Issue #1 (Jan 1999)
- Model has Pictorial in Issue #5 (May 1999)
- **Before**: Thumbnail from Issue #1 (Cover)
- **After**: Thumbnail from Issue #5 (Pictorial)

---

## Implementation

### SQL Query Logic

```sql
WITH FirstArticle AS (
    SELECT DISTINCT ON (cm.ModelId)
        cm.ModelId,
        a.ArticleId,
        MIN(c.IssueId) as IssueId,
        MIN(c.Page) as PageStart
    FROM ContentModel cm
    JOIN Article a ON cm.ArticleId = a.ArticleId
    JOIN Category cat ON a.CategoryId = cat.CategoryId  -- Added category join
    JOIN Content c ON a.ArticleId = c.ArticleId
    GROUP BY cm.ModelId, a.ArticleId, cat.Name
    ORDER BY cm.ModelId, 
             CASE 
                 WHEN LOWER(cat.Name) IN ('model', 'pictorial', 'pinup', 'feature') THEN 0
                 WHEN LOWER(cat.Name) = 'cover' THEN 1
                 ELSE 2
             END,
             MIN(c.IssueId), 
             MIN(c.Page)
)
```

### Key Changes

1. **Added Category Join**: `JOIN Category cat ON a.CategoryId = cat.CategoryId`
2. **Added to GROUP BY**: `cat.Name` (required for category-based sorting)
3. **Category Priority in ORDER BY**: CASE expression assigns priority values
4. **Fallback Logic**: Covers only used when no model/pictorial articles exist

---

## Category Mapping

### Priority 0 (Preferred)
- `model` - Model pictorials
- `pictorial` - General pictorials
- `pinup` - Pin-up style shoots
- `feature` - Feature articles (may include model photos)

### Priority 1 (Fallback)
- `cover` - Cover appearances

### Priority 2 (Last Resort)
- Any other category (interviews, editorials, etc.)

---

## Examples

### Example 1: Model with Multiple Article Types

**Database**:
```
Model: Jane Doe (ModelId: 123)
Articles:
  - ArticleId: 1, Category: Cover, IssueId: 100, Page: 1
  - ArticleId: 2, Category: Model, IssueId: 105, Page: 45
  - ArticleId: 3, Category: Interview, IssueId: 110, Page: 80
```

**Selection Process**:
1. Filter by ModelId = 123
2. Sort by:
   - Category: Cover (1), Model (0), Interview (2)
   - IssueId: 100, 105, 110
3. **Selected**: ArticleId 2 (Model category, priority 0)
4. **Thumbnail**: First page of ArticleId 2

### Example 2: Model with Only Cover

**Database**:
```
Model: Sarah Smith (ModelId: 456)
Articles:
  - ArticleId: 10, Category: Cover, IssueId: 200, Page: 1
  - ArticleId: 11, Category: Cover, IssueId: 205, Page: 1
```

**Selection Process**:
1. Filter by ModelId = 456
2. No model/pictorial articles found
3. Falls back to cover articles
4. **Selected**: ArticleId 10 (earliest cover)
5. **Thumbnail**: Cover from IssueId 200

### Example 3: Later Model Article Chosen Over Earlier Cover

**Database**:
```
Model: Emma Johnson (ModelId: 789)
Articles:
  - ArticleId: 20, Category: Cover, IssueId: 300, Page: 1 (Jan 2000)
  - ArticleId: 21, Category: Model, IssueId: 312, Page: 56 (Jan 2001)
```

**Selection Process**:
1. Filter by ModelId = 789
2. Model category (priority 0) beats Cover (priority 1)
3. **Selected**: ArticleId 21 even though it's a year later
4. **Thumbnail**: Model pictorial from 2001, not cover from 2000

---

## Benefits

✅ **Better Representation** - Thumbnails show models in their pictorial spreads  
✅ **More Visual Interest** - Pictorials typically have better composition than covers  
✅ **Logical Fallback** - Still uses covers if no model articles exist  
✅ **Consistent Ordering** - Same logic applies to all models  
✅ **Database Efficient** - Single query with smart ordering  

---

## Testing Scenarios

### Test Cases

1. **Model with only pictorials** → Uses first pictorial
2. **Model with only covers** → Uses first cover
3. **Model with covers + pictorials** → Uses first pictorial (even if cover is earlier)
4. **Model with multiple pictorials** → Uses earliest pictorial
5. **Model with no images** → Shows initials (graceful fallback)

### Manual Testing

- [ ] Navigate to `/models` page
- [ ] Check models that appear on covers - verify thumbnail is from pictorial, not cover
- [ ] Check models with only cover appearances - verify cover is used
- [ ] Verify thumbnails are representative of the model's work

---

## Code Changes

### File Modified

**`/src/TheArchive/Services/ArchiveDatabase.cs`**
- Updated `GetModelThumbnailsAsync()` method
- Added Category table join
- Added category priority in ORDER BY clause
- Updated method summary comment

### Build Status

```bash
cd /home/justin/repos/urban-sniffle/the-archive && dotnet build src/TheArchive/TheArchive.csproj
# Build succeeded. 0 Error(s)
```

---

## Performance Impact

**Minimal**: 
- Added one join (Category table)
- Added one field to GROUP BY
- CASE expression in ORDER BY is very fast
- No additional database round-trips

The query remains efficient with DISTINCT ON and proper indexing.

---

## Future Enhancements

Potential improvements:
1. Make category priority configurable
2. Consider image quality/resolution in selection
3. Allow manual thumbnail override per model
4. Prefer color images over B&W
5. Consider article page count (longer articles might have better photos)

---

## Summary

Model thumbnails now intelligently prefer pictorial/model articles over covers, providing more representative and visually interesting images while maintaining graceful fallback to covers when needed.

**User Experience**: Models are now represented by their best work (pictorials) rather than just their cover appearances.

---

**End of Enhancement**

