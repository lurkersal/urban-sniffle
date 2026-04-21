# Model Detail Page - Chronological Order & Display Improvements

**Date**: April 21, 2026  
**Status**: ✅ **COMPLETED** - Issues and articles now chronological, better formatting

---

## Summary

Fixed two issues on the model detail page:
1. **Appearances section** - Now ordered chronologically (earliest to latest)
2. **Articles section** - Now shows magazine name, volume, number, and year instead of IssueId

---

## Changes Made

### 1. Appearances Section - Chronological Order

**Issue**: Issues were ordered DESC (newest first), which is counter-intuitive for viewing a model's career progression.

**Fix**: Changed ordering to ASC (oldest first) for true chronological order.

#### File: `/src/TheArchive/Services/ArchiveDatabase.cs`

**Method**: `GetIssuesByModelAsync(int modelId)`

**Before**:
```sql
ORDER BY i.Year DESC, i.Volume DESC, i.Number DESC
```

**After**:
```sql
ORDER BY i.Year ASC, i.Volume ASC, i.Number ASC
```

**Result**: Issues now appear from earliest to latest, showing career timeline.

---

### 2. Articles Section - Better Display Format

**Issue**: Articles showed "Issue @IssueId" which is a meaningless database ID.

**Fix**: Display magazine name, volume, number, and year for human-readable context.

#### Files Modified

**1. Article Model** - Added MagazineName property
```csharp
// /src/TheArchive/Models/Article.cs
public string MagazineName { get; set; } = string.Empty;
```

**2. Database Query** - Added Magazine join and MagazineName to SELECT
```sql
-- /src/TheArchive/Services/ArchiveDatabase.cs - GetArticlesByModelAsync()

SELECT DISTINCT
    a.ArticleId,
    ...,
    i.Year,
    i.Volume,
    i.Number,
    mag.Name as MagazineName  -- Added
FROM Article a
...
LEFT JOIN Magazine mag ON i.MagazineId = mag.MagazineId  -- Added join
WHERE cm.ModelId = @ModelId
GROUP BY ..., mag.Name  -- Added to GROUP BY
ORDER BY i.Year ASC, i.Volume ASC, i.Number ASC, MIN(c.Page) ASC
```

**3. View** - Updated display format
```razor
<!-- /src/TheArchive/Views/Models/Detail.cshtml -->

<!-- Before -->
<div>Issue @article.IssueId</div>

<!-- After -->
<div>@article.MagazineName Vol @article.Volume No @article.Number (@article.Year)</div>
```

**Example Display**:
- Before: `Issue 12345`
- After: `Playboy Vol 46 No 3 (1999)`

---

## User Experience Improvements

### Before

**Appearances Section**:
```
2001 issues
2000 issues  
1999 issues  ← Career start at the bottom
```

**Articles Section**:
```
Page 45 | Model | Issue 12345
Page 80 | Cover | Issue 11234
```

### After

**Appearances Section**:
```
1999 issues  ← Career start at the top
2000 issues
2001 issues
```

**Articles Section**:
```
Page 45 | Model | Playboy Vol 46 No 3 (1999)
Page 80 | Cover | Penthouse Vol 30 No 12 (2000)
```

---

## Benefits

✅ **Chronological Timeline** - See career progression from start to finish  
✅ **Human-Readable** - Magazine names instead of database IDs  
✅ **Complete Context** - Volume, number, and year provide full issue identification  
✅ **Consistent** - Matches chronological ordering throughout the app  
✅ **Professional** - Looks more polished and informative  

---

## Technical Details

### Issue Ordering

**Appearances (Issues)**:
- Ordered by: Year ASC → Volume ASC → Number ASC
- Shows earliest appearance first
- Chronological career timeline

**Articles**:
- Ordered by: Year ASC → Volume ASC → Number ASC → Page ASC
- Shows earliest article first
- Same chronological progression

### Display Format

**Magazine Information Pattern**:
```
{MagazineName} Vol {Volume} No {Number} ({Year})
```

**Examples**:
- `Playboy Vol 46 No 3 (1999)`
- `Penthouse Vol 30 No 12 (2000)`
- `Club International Vol 15 No 8 (2001)`

---

## Example Views

### Model Detail Page - Appearances

```
Appearances (3 issues)
┌─────────────────────────────┐
│ Playboy                     │
│ March 1999                  │
└─────────────────────────────┘
┌─────────────────────────────┐
│ Penthouse                   │
│ December 2000               │
└─────────────────────────────┘
┌─────────────────────────────┐
│ Playboy                     │
│ August 2001                 │
└─────────────────────────────┘
```

### Model Detail Page - Articles

```
Articles (3)
┌────────────────────────────────────────────────────────┐
│ 45 | [Cover]                                           │
│      Untitled                                          │
│      Playboy Vol 46 No 3 (1999)                       │
├────────────────────────────────────────────────────────┤
│ 80 | [Model]                                           │
│      Victoria's Secret                                 │
│      Penthouse Vol 30 No 12 (2000)                    │
├────────────────────────────────────────────────────────┤
│ 56 | [Pictorial]                                       │
│      Summer Beauty                                     │
│      Playboy Vol 48 No 8 (2001)                       │
└────────────────────────────────────────────────────────┘
```

---

## Files Modified

1. **`/src/TheArchive/Services/ArchiveDatabase.cs`**
   - `GetIssuesByModelAsync()` - Changed order from DESC to ASC
   - `GetArticlesByModelAsync()` - Added Magazine join and MagazineName

2. **`/src/TheArchive/Models/Article.cs`**
   - Added `MagazineName` property

3. **`/src/TheArchive/Views/Models/Detail.cshtml`**
   - Changed article display from "Issue {id}" to magazine info format

---

## Build Status

```bash
cd /home/justin/repos/urban-sniffle/the-archive && dotnet build
# Build succeeded. 0 Error(s)
```

---

## Testing

### Manual Testing Checklist

- [ ] Navigate to a model detail page
- [ ] Verify "Appearances" section shows earliest issues first
- [ ] Verify "Articles" section shows earliest articles first
- [ ] Check article display shows magazine name, volume, number, year
- [ ] Confirm format is readable: "Playboy Vol 46 No 3 (1999)"
- [ ] Verify clicking article opens article viewer

### Edge Cases

- [ ] Model with single appearance → displays correctly
- [ ] Model with many appearances → chronological order maintained
- [ ] Articles from same year → ordered by volume, then number
- [ ] Articles from same issue → ordered by page

---

## Related Changes

### Consistency Across App

- **Magazine issues page** - Ordered by date DESC (newest first) - for browsing latest
- **Model appearances** - Ordered by date ASC (oldest first) - for career timeline
- **Model articles** - Ordered by date ASC (oldest first) - for career timeline

Different pages have different ordering based on use case:
- **Browsing** = Newest first (DESC)
- **Timeline/History** = Oldest first (ASC)

---

## Summary

The model detail page now provides a proper chronological timeline of the model's career, with appearances and articles shown from earliest to latest. The display format has been improved to show meaningful magazine information instead of database IDs.

**Before**:
- Issues: 2001 → 2000 → 1999 (reverse chronological)
- Articles: "Issue 12345" (meaningless)

**After**:
- Issues: 1999 → 2000 → 2001 (chronological timeline)
- Articles: "Playboy Vol 46 No 3 (1999)" (informative)

---

**End of Enhancement**

