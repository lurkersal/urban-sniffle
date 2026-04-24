# Search Results Bug Fix - Article Search

## Problem

When searching for "linz" in The Archive application, the results showed:

**MODELS** (correctly matched):
- Linzi (1 appearances)
- Linzi Drew (88 appearances)

**ARTICLES** (incorrectly showing unrelated articles):
- Susannah (Page 193)
- Gabrielle (Page 172)
- Jacqui in the Jacuzzi (Page 148)
- Channel Sex (Page 139)
- Aslan's Angels (Page 137)

The article results had nothing to do with the search term "linz".

## Root Cause

The search functionality in `_Layout.cshtml` was calling:
```javascript
fetch(`/api/v1/articles?q=${encodeURIComponent(query)}&per_page=5`)
```

However, the API endpoint in `ArticlesController.cs` was **ignoring the `q` parameter** completely. The `GetAllArticlesAsync` method only supported filtering by category, not by search query, so it was just returning the most recent 5 articles regardless of the search term.

## Solution

### 1. Updated API Controller
**File**: `/home/justin/repos/urban-sniffle/the-archive/src/TheArchive/Controllers/Api/ArticlesController.cs`

Added support for the `q` query parameter:
```csharp
[HttpGet]
public async Task<IActionResult> GetAll(
    [FromQuery] string? category = null, 
    [FromQuery] string? q = null,  // NEW: Added search query parameter
    [FromQuery] int page = 1, 
    [FromQuery] int per_page = 50)
{
    var articles = await _db.GetAllArticlesAsync(category, q, page, per_page);
    return Ok(articles);
}
```

### 2. Updated Database Service
**File**: `/home/justin/repos/urban-sniffle/the-archive/src/TheArchive/Services/ArchiveDatabase.cs`

Enhanced `GetAllArticlesAsync` to search articles by title and model names:

- Added `searchQuery` parameter
- Added LEFT JOINs to include ContentModel and Model tables
- Added WHERE clause to filter by article title or model name (case-insensitive, partial match):
  ```csharp
  if (!string.IsNullOrWhiteSpace(searchQuery))
  {
      whereClauses.Add("(LOWER(a.Title) LIKE LOWER(@SearchQuery) OR LOWER(m.Name) LIKE LOWER(@SearchQuery))");
      parameters.Add("SearchQuery", $"%{searchQuery}%");
  }
  ```

### 3. Fixed Existing Controller Call
**File**: `/home/justin/repos/urban-sniffle/the-archive/src/TheArchive/Controllers/ArticlesController.cs`

Updated the non-API ArticlesController to pass the new parameter:
```csharp
var articles = await _db.GetAllArticlesAsync(category, searchQuery: null, page, perPage: 100);
```

## Result

Now when searching for "linz":
- **MODELS**: Will show Linzi and Linzi Drew (as before)
- **ARTICLES**: Will only show articles that either:
  - Have "linz" in the article title, OR
  - Feature a model with "linz" in their name (e.g., articles featuring Linzi or Linzi Drew)

The unrelated articles (Susannah, Gabrielle, etc.) will no longer appear in the search results unless they actually match the search term.

## Date
Fixed: April 23, 2026

