# Model Page Refinements

**Date**: April 21, 2026  
**Status**: ✅ **COMPLETED** - Removed measurements from index, full-width articles section

---

## Summary

Two refinements to model pages:
1. **Models Index Page** - Removed measurements display, keeping only name and appearance count
2. **Model Detail Page** - Articles section now uses full available width

---

## Changes Made

### 1. Models Index Page - Removed Measurements

**Issue**: Measurements displayed under model name cluttered the card design.

**Fix**: Removed measurements, showing only name and appearance count.

#### File: `/src/TheArchive/Views/Models/Index.cshtml`

**Before**:
```razor
<div class="model-info">
    <div class="model-name">Jane Doe</div>
    <div class="model-measurements">34C-24-36</div>  ← Removed
    <div class="model-appear">12 appearances</div>
</div>
```

**After**:
```razor
<div class="model-info">
    <div class="model-name">Jane Doe</div>
    <div class="model-appear">12 appearances</div>  ← Cleaner
</div>
```

**Result**: Cleaner, simpler card design focusing on identity and count.

---

### 2. Model Detail Page - Full-Width Articles Section

**Issue**: Articles section was constrained to 800px max-width, leaving unused space.

**Fix**: Changed max-width to 100% so articles use full available width.

#### File: `/src/TheArchive/wwwroot/css/archive.css`

**Before**:
```css
.model-stats {
    display: flex;
    flex-direction: column;
    gap: 1.5rem;
    max-width: 800px;  /* Limited width */
}
```

**After**:
```css
.model-stats {
    display: flex;
    flex-direction: column;
    gap: 1.5rem;
    max-width: 100%;  /* Full width */
}
```

**Result**: Articles section uses full width of the right column in the grid layout, with longer rows for better space utilization.

---

## Visual Changes

### Models Index Page

**Before**:
```
┌──────────────────────────┐
│    [Model Photo]         │
├──────────────────────────┤
│ Jane Doe                 │
│ 34C-24-36               │ ← Removed
│ 12 appearances          │
└──────────────────────────┘
```

**After**:
```
┌──────────────────────────┐
│    [Model Photo]         │
├──────────────────────────┤
│ Jane Doe                 │
│ 12 appearances          │ ← Simpler
└──────────────────────────┘
```

### Model Detail Page - Articles Section

**Before**:
```
┌─────────────┬──────────────────────────────────────────┐
│             │                                          │
│  Portrait   │  Profile                                 │
│             │                                          │
│    Name     │  Articles (max 800px)                    │
│             │  ┌──────────────────┐                    │
│             │  │ Article rows     │  ← Constrained     │
│             │  └──────────────────┘  ← Unused space   │
└─────────────┴──────────────────────────────────────────┘
```

**After**:
```
┌─────────────┬──────────────────────────────────────────┐
│             │                                          │
│  Portrait   │  Profile                                 │
│             │                                          │
│    Name     │  Articles (full width)                   │
│             │  ┌────────────────────────────────────┐  │
│             │  │ Longer article rows               │  │
│             │  └────────────────────────────────────┘  │
└─────────────┴──────────────────────────────────────────┘
```

---

## Benefits

✅ **Cleaner Index Cards** - Removed clutter, focusing on essentials  
✅ **Better Space Usage** - Articles use full available width  
✅ **Simpler Design** - Less information density on index cards  
✅ **More Data Visible** - Longer rows show more of magazine name/details  

---

## Design Rationale

### Why Remove Measurements from Index?

1. **Card Simplicity** - Index cards should show minimal info for browsing
2. **Privacy/Discretion** - Measurements may not need to be prominently displayed
3. **Focus on Identity** - Name and appearance count are the key identifiers
4. **Detail Page** - Full measurements still available on detail page

### Why Full Width for Articles?

1. **Space Utilization** - Use available screen real estate
2. **Information Density** - Show more of magazine name, volume, number, year
3. **Grid Layout** - Right column already has defined width, articles should fill it
4. **Consistency** - Matches other full-width content sections

---

## Example Layouts

### Model Card (Index Page)

**Before**:
```
┌────────────────┐
│   [Photo]      │
├────────────────┤
│ Jane Doe       │
│ 34C-24-36     │
│ 12 appearances │
└────────────────┘
```

**After**:
```
┌────────────────┐
│   [Photo]      │
├────────────────┤
│ Jane Doe       │
│ 12 appearances │
└────────────────┘
```

### Article Row (Detail Page)

**Before (constrained)**:
```
45 | [Cover] Untitled                           
     Playboy Vol 46...
```

**After (full width)**:
```
45 | [Cover] Untitled                                                  
     Playboy Vol 46 No 3 (1999)
```

---

## Files Modified

1. **`/src/TheArchive/Views/Models/Index.cshtml`**
   - Removed measurements conditional display block

2. **`/src/TheArchive/wwwroot/css/archive.css`**
   - Changed `.model-stats` max-width from 800px to 100%

---

## Build Status

```bash
cd /home/justin/repos/urban-sniffle/the-archive && dotnet build
# Build succeeded. 0 Error(s)
```

---

## Testing

### Manual Testing Checklist

- [ ] Navigate to `/models` page
- [ ] Verify no measurements shown under model names
- [ ] Verify cards look clean with just name and count
- [ ] Navigate to a model detail page
- [ ] Verify articles section uses full width of right column
- [ ] Verify article rows are longer and show full info
- [ ] Check layout on different screen sizes

---

## Note on Measurements

Measurements are still:
- ✅ Stored in database
- ✅ Shown on model detail page (Profile section)
- ❌ Hidden from index page cards (for cleaner design)

This allows users to find measurements when viewing a specific model while keeping the browsing experience clean.

---

## Summary

The models index page now shows cleaner, simpler cards without measurements, focusing on model identity and appearance count. The model detail page's articles section now uses the full available width for better space utilization and information display.

**Index Page**:
- **Before**: Name + Measurements + Count
- **After**: Name + Count (cleaner)

**Detail Page Articles**:
- **Before**: Constrained to 800px
- **After**: Full width (better space usage)

---

**End of Refinement**

