# The Archive - Homepage Statistics Cards Fix

**Date:** March 30, 2026  
**Issues Fixed:**
1. ✅ Statistics cards were not clickable
2. ✅ Cards now navigate to the correct pages
3. ✅ Disabled state for pages that don't exist yet

---

## Problems Fixed

### 1. Non-Functional Statistics Cards

**Before:** The statistics dashboard cards had hover effects but were not clickable - they were just `<div>` elements.

**After:** Cards are now `<a>` links that navigate to their respective pages:
- **Magazines** → `/` (homepage)
- **Issues** → `/issues`
- **Models** → `/models`
- **Articles** → Disabled (page coming soon)
- **Photographers** → Disabled (page coming soon)

### 2. Missing Routes

**Issue:** Not all stat card categories have corresponding pages yet.

**Solution:** 
- Made working routes clickable
- Disabled Articles and Photographers cards with tooltips indicating "coming soon"
- Visual indicator (opacity 0.5, no hover effect) for disabled cards

---

## Files Modified

### 1. `/the-archive/src/TheArchive/Views/Home/Index.cshtml`

**Changes:**
```html
<!-- ❌ BEFORE: Non-clickable div -->
<div class="stat-card">
    <div class="stat-value">@ViewBag.Stats.TotalMagazines</div>
    <div class="stat-label">Magazines</div>
</div>

<!-- ✅ AFTER: Clickable link -->
<a href="/" class="stat-card stat-card-link">
    <div class="stat-value">@ViewBag.Stats.TotalMagazines</div>
    <div class="stat-label">Magazines</div>
</a>

<!-- ✅ AFTER: Disabled for non-existent pages -->
<div class="stat-card stat-card-disabled" title="Articles browse page coming soon">
    <div class="stat-value">@ViewBag.Stats.TotalArticles</div>
    <div class="stat-label">Articles</div>
</div>
```

### 2. `/the-archive/src/TheArchive/wwwroot/css/archive.css`

**Added CSS classes:**

```css
.stat-card {
    /* Base styles */
    display: block;
    text-decoration: none;
    color: inherit;
}

.stat-card-link {
    cursor: pointer;
}

.stat-card-link:hover {
    background: rgba(245, 240, 232, 0.06);
    border-color: rgba(245, 240, 232, 0.2);
    transform: translateY(-2px);
}

.stat-card-disabled {
    opacity: 0.5;
    cursor: not-allowed;
}

.stat-card-disabled:hover {
    /* Prevent hover effects on disabled cards */
    background: rgba(245, 240, 232, 0.04);
    border-color: rgba(245, 240, 232, 0.1);
    transform: none;
}
```

---

## Current Page Routes

| Stat Card | Link | Status | Controller |
|-----------|------|--------|------------|
| **Magazines** | `/` | ✅ Active | HomeController.Index |
| **Issues** | `/issues` | ✅ Active | IssuesController.Index |
| **Articles** | - | ⚠️ Coming Soon | Not implemented |
| **Models** | `/models` | ✅ Active | ModelsController.Index |
| **Photographers** | - | ⚠️ Coming Soon | Not implemented |

---

## Statistics Calculations

The statistics are pulled from the database via `ArchiveStatistics` service:

```csharp
stats.TotalMagazines = await connection.QuerySingleAsync<int>(
    "SELECT COUNT(*) FROM magazine");
    
stats.TotalIssues = await connection.QuerySingleAsync<int>(
    "SELECT COUNT(*) FROM issue");
    
stats.TotalArticles = await connection.QuerySingleAsync<int>(
    "SELECT COUNT(*) FROM content");
    
stats.TotalModels = await connection.QuerySingleAsync<int>(
    "SELECT COUNT(DISTINCT model_id) FROM content_model WHERE model_id IS NOT NULL");
    
stats.TotalPhotographers = await connection.QuerySingleAsync<int>(
    "SELECT COUNT(DISTINCT photographer) FROM content WHERE photographer IS NOT NULL");
```

**Note:** If statistics appear incorrect, it may be due to:
1. Database data quality (NULL values, duplicates)
2. Photographer field might need to link to a `photographer` table instead of counting strings
3. Content table might have test/invalid data

---

## User Experience Improvements

### Visual Feedback
1. **Clickable cards:**
   - Cursor changes to pointer
   - Hover effect (lift up, brighten)
   - Clear indication they're interactive

2. **Disabled cards:**
   - Lower opacity (50%)
   - No hover effects
   - Tooltip on hover explaining status
   - Cannot be clicked

### Navigation Flow
```
Homepage (Magazines)
    ↓
    ├─→ Magazines Card → Stays on homepage (/)
    ├─→ Issues Card → All Issues (/issues)
    ├─→ Models Card → All Models (/models)
    ├─→ Articles Card → [Disabled - Coming Soon]
    └─→ Photographers Card → [Disabled - Coming Soon]
```

---

## Testing Checklist

To verify the fixes:

1. **Visit homepage** (`http://localhost:5163`)
   - [ ] Statistics cards are visible with correct counts
   
2. **Click Magazines card**
   - [ ] Stays on homepage or refreshes
   - [ ] Page works normally
   
3. **Click Issues card**
   - [ ] Navigates to `/issues`
   - [ ] Shows all issues across all magazines
   
4. **Click Models card**
   - [ ] Navigates to `/models`
   - [ ] Shows all models
   
5. **Hover over Articles card**
   - [ ] No hover effect (disabled state)
   - [ ] Tooltip shows "Articles browse page coming soon"
   - [ ] Cannot click
   
6. **Hover over Photographers card**
   - [ ] No hover effect (disabled state)
   - [ ] Tooltip shows "Photographers browse page coming soon"
   - [ ] Cannot click

---

## Future Enhancements

To complete the statistics dashboard:

### 1. Articles Browse Page
Create `ArticlesController` with:
```csharp
// GET /articles - Browse all articles
public async Task<IActionResult> Index(
    int? yearFrom, 
    int? yearTo,
    string? category,
    string? magazine)
{
    var articles = await _db.GetFilteredArticlesAsync(...);
    return View(articles);
}
```

### 2. Photographers Page
Create `PhotographersController` with:
```csharp
// GET /photographers - All photographers
public async Task<IActionResult> Index()
{
    var photographers = await _db.GetPhotographersAsync();
    return View(photographers);
}

// GET /photographers/{id} - Photographer detail
public async Task<IActionResult> Detail(int id)
{
    var photographer = await _db.GetPhotographerAsync(id);
    var articles = await _db.GetArticlesByPhotographerAsync(id);
    return View();
}
```

### 3. Enhanced Statistics
Consider adding:
- Decade breakdown (1970s, 1980s, 1990s)
- Category distribution chart
- Most active photographers
- Popular models timeline

---

## Build Status

✅ **Build:** Successful  
⚠️ **Warnings:** 3 nullable reference warnings (pre-existing, unrelated)  
✅ **Runtime:** Ready to test

---

## Known Issues

### Statistics Count Accuracy

If the numbers seem incorrect, investigate:

1. **TotalModels** - Uses `content_model` table
   - Check for NULL model_id values
   - Verify distinct count is accurate

2. **TotalPhotographers** - Uses `photographer` string field
   - May need normalization (trim, lowercase)
   - Consider creating a `photographer` table with IDs
   - Current query: `COUNT(DISTINCT photographer)` may count variations

3. **TotalArticles** - Uses `content` table
   - Verify what counts as an "article" vs other content types
   - May need filtering by category

### Recommended Investigation

Run these SQL queries to verify counts:

```sql
-- Check magazines
SELECT COUNT(*) FROM magazine;

-- Check issues
SELECT COUNT(*) FROM issue;

-- Check articles
SELECT COUNT(*) FROM content;

-- Check distinct models
SELECT COUNT(DISTINCT model_id) FROM content_model WHERE model_id IS NOT NULL;

-- Check photographer variations
SELECT photographer, COUNT(*) 
FROM content 
WHERE photographer IS NOT NULL 
GROUP BY photographer 
ORDER BY COUNT(*) DESC;
```

---

**Report Generated:** March 30, 2026  
**Status:** ✅ Ready for Testing  
**Next Steps:** Test on live site, verify statistics accuracy

