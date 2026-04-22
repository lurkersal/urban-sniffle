# Search Enhancement - Contributors Support

**Date:** 2026-04-23  
**Enhancement:** Added contributor/photographer search to the global search bar

## What Changed

The search bar now searches across three data types:
1. **Article Titles** - Any text in the article title
2. **Model Names** - Models featured in articles
3. **Contributors** - Photographers, authors, illustrators, etc.

### Before
Search only covered:
- Article titles
- Model names

### After
Search now includes:
- Article titles
- Model names
- **Contributors (photographers, authors, etc.)** ← NEW

## Implementation

### Database Query Update

**File:** `/home/justin/repos/urban-sniffle/the-archive/src/TheArchive/Services/ArchiveDatabase.cs`

**Method:** `GetAllArticlesAsync()`

**Changes:**
1. Added JOIN to ContentContributor and Contributor tables
2. Extended WHERE clause to include contributor name matching

**Before:**
```sql
FROM Article a
JOIN Category cat ON a.CategoryId = cat.CategoryId
JOIN Content c ON a.ArticleId = c.ArticleId
LEFT JOIN ContentModel cm ON a.ArticleId = cm.ArticleId
LEFT JOIN Model m ON cm.ModelId = m.ModelId

WHERE (LOWER(a.Title) LIKE LOWER(@SearchQuery) 
    OR LOWER(m.Name) LIKE LOWER(@SearchQuery))
```

**After:**
```sql
FROM Article a
JOIN Category cat ON a.CategoryId = cat.CategoryId
JOIN Content c ON a.ArticleId = c.ArticleId
LEFT JOIN ContentModel cm ON a.ArticleId = cm.ArticleId
LEFT JOIN Model m ON cm.ModelId = m.ModelId
LEFT JOIN ContentContributor cc ON c.ContentId = cc.ContentId
LEFT JOIN Contributor contrib ON cc.ContributorId = contrib.ContributorId

WHERE (LOWER(a.Title) LIKE LOWER(@SearchQuery) 
    OR LOWER(m.Name) LIKE LOWER(@SearchQuery) 
    OR LOWER(contrib.Name) LIKE LOWER(@SearchQuery))
```

## Examples

### Search by Article Title
```bash
$ curl "http://localhost:5163/api/v1/articles?q=Chocs&per_page=3"
# Returns: "Chocs away!" article
```

### Search by Model Name
```bash
$ curl "http://localhost:5163/api/v1/articles?q=Julie&per_page=3"
# Returns: All articles featuring models with "Julie" in their name
# - "Julie" (Model: Suzanne)
# - "Chocs away!" (Model: Julie Honsman)
# - etc.
```

### Search by Contributor/Photographer (NEW)
```bash
$ curl "http://localhost:5163/api/v1/articles?q=Dag&per_page=5"
# Returns: All articles photographed/contributed by "Dag Ohrlund"
# - "Big Nudes!"
# - "Fort Sex USA"
# - "Chocs away!"
```

## User Experience

### Global Search Bar Behavior
1. User types in search box (minimum 2 characters)
2. 300ms debounce delay
3. Parallel API calls to:
   - `/api/v1/articles?q=...` - Searches articles by title, model, contributor
   - `/api/v1/models?q=...` - Searches models by name
4. Results displayed in dropdown:
   - **Models** section (if any matches)
   - **Articles** section (if any matches)
   - "No results found" (if no matches)

### Search Results Display
```
┌─────────────────────────────────────┐
│ Models                              │
│ ├─ Julie Honsman (12 appearances)   │
│ └─ Julie (8 appearances)            │
│                                     │
│ Articles                            │
│ ├─ Chocs away! - Page 16            │
│ ├─ Big Nudes! - Page 42             │
│ └─ Fort Sex USA - Page 8            │
└─────────────────────────────────────┘
```

## Database Schema Reference

### Tables Involved
- **Article** - Article metadata (title, category)
- **Content** - Content pages (page numbers, images)
- **ContentContributor** - Links content to contributors
- **Contributor** - Photographers, authors, illustrators
- **ContentModel** - Links articles to models
- **Model** - Model information

### Relationships
```
Article (1) ─── (N) Content
                      │
                      ├─ (N) ContentContributor ─── (1) Contributor
                      └─ (N) ContentModel ─────────── (1) Model
```

## Performance Considerations

### Query Performance
- **Additional JOINs:** 2 LEFT JOINs added (ContentContributor, Contributor)
- **Indexes:** Existing indexes on foreign keys handle this efficiently
- **Search Pattern:** Uses `LIKE` with wildcards - acceptable for typical search volumes
- **Limit:** Results limited to 5 articles in search dropdown

### Optimization Opportunities
1. **Full-Text Search:** For larger datasets, consider PostgreSQL's full-text search
2. **Materialized View:** Pre-aggregate search data if performance degrades
3. **Caching:** Cache search results for common queries
4. **Elastic Search:** For very large datasets, consider dedicated search engine

## Testing

### Manual Tests Performed
```bash
# Test 1: Article title search
$ curl -s "http://localhost:5163/api/v1/articles?q=Chocs" | jq '.[].title'
"Chocs away!"  ✅

# Test 2: Model name search
$ curl -s "http://localhost:5163/api/v1/articles?q=Julie" | jq '.[].title'
"Julie"
"Julie"
"Julie"  ✅

# Test 3: Contributor search (NEW)
$ curl -s "http://localhost:5163/api/v1/articles?q=Dag" | jq '.[].title'
"Big Nudes!"
"Fort Sex USA"
"Chocs away!"  ✅

# Verify database has contributor data
$ psql -c "SELECT a.Title, contrib.Name FROM Article a 
          JOIN Content c ON a.ArticleId = c.ArticleId 
          JOIN ContentContributor cc ON c.ContentId = cc.ContentId 
          JOIN Contributor contrib ON cc.ContributorId = contrib.ContributorId 
          WHERE contrib.Name LIKE '%Dag%' LIMIT 5;"
    title     |    name     
--------------+-------------
Chocs away!  | Dag Ohrlund
Fort Sex USA | Dag Ohrlund
Big Nudes!   | Dag Ohrlund  ✅
```

### Edge Cases Handled
| Scenario | Behavior |
|----------|----------|
| Search query < 2 chars | No search performed (client-side) |
| No results | "No results found" message |
| Contributor with no articles | Not shown |
| Article with no contributors | Still searchable by title/model |
| Special characters in name | Properly escaped in SQL |
| Case sensitivity | Case-insensitive search (LOWER()) |

## UI/Frontend

### Search Input Location
**File:** `/home/justin/repos/urban-sniffle/the-archive/src/TheArchive/Views/Shared/_Layout.cshtml`

The search JavaScript already handles the enhanced results - no frontend changes needed:
```javascript
async function handleSearch(query) {
    const [articlesRes, modelsRes] = await Promise.all([
        fetch(`/api/v1/articles?q=${encodeURIComponent(query)}&per_page=5`),
        fetch(`/api/v1/models?q=${encodeURIComponent(query)}&per_page=5`)
    ]);
    // Displays results automatically
}
```

## Related Enhancements

### Future Improvements
1. **Search Highlights:** Highlight matching text in results
2. **Search History:** Remember recent searches
3. **Advanced Filters:** Filter by category, date range, etc.
4. **Fuzzy Matching:** Handle typos (e.g., "Dag" matches "Dagg")
5. **Autocomplete:** Suggest as user types
6. **Search Analytics:** Track popular searches

### Contributor Types
Currently searches all contributor types:
- Photographers
- Authors
- Illustrators
- Artists
- Any other credited contributors

Could be enhanced to filter by contributor role if needed.

## Files Modified

1. **`/home/justin/repos/urban-sniffle/the-archive/src/TheArchive/Services/ArchiveDatabase.cs`**
   - Method: `GetAllArticlesAsync()` (lines 366-393)
   - Added LEFT JOIN to ContentContributor and Contributor tables
   - Extended WHERE clause to include `contrib.Name` in search

## Documentation

### Quick Reference
```sql
-- Search articles by title, model, or contributor
SELECT ... FROM Article a
LEFT JOIN ContentModel cm ON a.ArticleId = cm.ArticleId
LEFT JOIN Model m ON cm.ModelId = m.ModelId
LEFT JOIN ContentContributor cc ON c.ContentId = cc.ContentId
LEFT JOIN Contributor contrib ON cc.ContributorId = contrib.ContributorId
WHERE (
    LOWER(a.Title) LIKE '%query%' OR      -- Article title
    LOWER(m.Name) LIKE '%query%' OR       -- Model name
    LOWER(contrib.Name) LIKE '%query%'    -- Contributor name
)
```

### API Endpoint
```
GET /api/v1/articles?q={searchQuery}&per_page={limit}

Parameters:
  - q: Search query (searches title, model, contributor)
  - per_page: Results limit (default: 50, search uses 5)

Returns: Array of Article objects
```

## Status

✅ **COMPLETE** - Search now includes contributor/photographer names

**Search Coverage:**
- ✅ Article titles
- ✅ Model names  
- ✅ Contributors (photographers, authors, etc.)

**Verification:**
- Search for "Dag" returns articles by Dag Ohrlund
- Search for "Julie" returns articles featuring Julie
- Search for "Chocs" returns "Chocs away!" article
- All three search types work independently and together

---

**Session Complete:** Global search bar now searches article titles, model names, AND contributors.

