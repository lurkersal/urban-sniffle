# Search Functionality - Quick Reference

## What the Search Bar Searches

The global search bar (top of every page) searches across:

1. **Article Titles** - Text in article names
   - Example: "Chocs" → finds "Chocs away!"

2. **Model Names** - Models featured in articles
   - Example: "Julie" → finds articles with Julie Honsman, Julie, etc.

3. **Contributors** - Photographers, authors, illustrators
   - Example: "Dag" → finds articles by Dag Ohrlund

## How to Use

1. Click the search box (or press `/`)
2. Type at least 2 characters
3. Results appear after 300ms
4. Click any result to navigate

## Search Results

Results show in two sections:

### Models Section
- Model name
- Number of appearances
- Links to model detail page (`/models/{id}`)

### Articles Section  
- Article title (or "Untitled")
- Magazine, volume, issue, and year info (e.g., "Club International Vol.17 #11 (1988)")
- Links to article detail page (`/articles/{id}`)

## Examples

| Search Term | Finds |
|-------------|-------|
| "Chocs" | Articles with "Chocs" in title |
| "Julie" | Articles featuring models named Julie |
| "Dag" | Articles photographed by Dag Ohrlund |
| "linz" | Models named Linz AND articles titled/featuring "linz" |

### Example Search Result
```
Models
├─ Julie Honsman (12 appearances) → /models/260

Articles
├─ Chocs away! - Club International Vol.17 #11 (1988) → /articles/940
├─ Julie - Mayfair Vol.17 #3 (1982) → /articles/2074
└─ Fort Sex USA - Club International Vol.17 #10 (1988) → /articles/1014
```

## Technical Details

### API Endpoints
```bash
# Search articles (title, model, contributor)
GET /api/v1/articles?q={query}&per_page=5

# Search models (name only)
GET /api/v1/models?q={query}&per_page=5
```

### Database Query
```sql
-- Article search includes:
WHERE (
    LOWER(a.Title) LIKE '%{query}%'       -- Article titles
    OR LOWER(m.Name) LIKE '%{query}%'     -- Model names
    OR LOWER(contrib.Name) LIKE '%{query}%' -- Contributors
)
```

## Notes

- Search is **case-insensitive**
- Requires **minimum 2 characters**
- **Maximum 5 results** per section in dropdown
- Search triggers after **300ms delay** (debounced typing)
- Click outside search box to close results

## File Location

Implementation: `/home/justin/repos/urban-sniffle/the-archive/src/TheArchive/Services/ArchiveDatabase.cs`
- Method: `GetAllArticlesAsync()`

