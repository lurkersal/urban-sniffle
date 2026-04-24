# Article List Single Column Fix

**Date**: April 21, 2026  
**Status**: ✅ **COMPLETED** - Articles now display in single column

---

## Problem

The article list on the model detail page was displaying in **2 columns** using CSS `column-count: 2`, which made the layout cramped and harder to read.

---

## Root Cause

The `.article-list` CSS class was using CSS multi-column layout:

```css
.article-list {
    column-count: 2;      /* 2 columns */
    column-gap: 0.75rem;
}
```

This created a newspaper-style column layout where articles would flow from top to bottom in the left column, then continue in the right column.

---

## The Fix

Changed from CSS columns to flexbox for a true single-column layout:

### File: `/src/TheArchive/wwwroot/css/archive.css`

**Before**:
```css
.article-list {
    column-count: 2;      /* 2 columns */
    column-gap: 0.75rem;
}

.article-row {
    /* ... */
    break-inside: avoid;      /* Prevent breaking across columns */
    margin-bottom: 0.75rem;   /* Gap between items */
}

@media (max-width: 1024px) {
    .article-list {
        column-count: 1;      /* Single column on mobile */
    }
}
```

**After**:
```css
.article-list {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;             /* Consistent gap between items */
}

.article-row {
    /* ... */
    /* Removed break-inside and margin-bottom */
}

/* Removed media query - not needed with flexbox */
```

---

## Changes Made

### 1. Changed Layout Method

- **From**: CSS multi-column layout (`column-count: 2`)
- **To**: Flexbox single-column layout (`flex-direction: column`)

### 2. Removed Column-Specific CSS

- Removed `break-inside: avoid` from `.article-row` (only needed for columns)
- Removed `margin-bottom` from `.article-row` (using flexbox `gap` instead)

### 3. Removed Media Query

- Deleted the `@media (max-width: 1024px)` query that changed column count
- Not needed since flexbox is always single-column

---

## Visual Changes

### Before (2 Columns)

```
Articles Section:
┌────────────────────────────────────────────┐
│ 45 | Cover | Playboy   │ 80 | Model       │
│ 56 | Feature           │ 92 | Interview   │
│ 60 | Pictorial         │ 100| Cover       │
│                        │                   │
└────────────────────────────────────────────┘
     Column 1                  Column 2
```

### After (Single Column)

```
Articles Section:
┌────────────────────────────────────────────┐
│ 45 | Cover | Playboy Vol 46 No 3 (1999)   │
│ 56 | Feature | Penthouse Vol 30 No 6       │
│ 60 | Pictorial | Playboy Vol 46 No 8       │
│ 80 | Model | Club International Vol 15     │
│ 92 | Interview | Penthouse Vol 30 No 12    │
│ 100| Cover | Playboy Vol 47 No 1 (2000)    │
└────────────────────────────────────────────┘
         Single Column (full width)
```

---

## Benefits

✅ **Single Column** - Articles flow top to bottom in chronological order  
✅ **Full Width** - Each article row uses full available width  
✅ **Better Readability** - Longer rows show complete magazine info  
✅ **Consistent Gap** - Flexbox `gap` provides uniform spacing  
✅ **Simpler CSS** - No need for column-specific properties  
✅ **Responsive** - Works well on all screen sizes without media queries  

---

## Technical Details

### CSS Columns vs Flexbox

**CSS Columns** (old approach):
- Creates newspaper-style layout
- Items flow top-to-bottom, then left-to-right
- Requires `break-inside: avoid` to prevent splitting
- Needs media queries for responsive design

**Flexbox** (new approach):
- Creates single-column stack
- Items flow top-to-bottom only
- Natural single-column behavior
- Responsive by default

### Flexbox Gap

Using `gap: 0.75rem` in flexbox provides:
- Consistent spacing between articles
- No need for margins on individual items
- Cleaner, more maintainable CSS

---

## Example Article Row Layout

### Full Width Row

```
┌────────────────────────────────────────────────────────────┐
│ 45 │ [Cover]                                               │
│    │ Untitled                                              │
│    │ Playboy Vol 46 No 3 (1999)                           │
└────────────────────────────────────────────────────────────┘
 Pg#   Article Info (spans full width)
```

Each row now has more horizontal space to display:
- Page number
- Category badge
- Article title
- Magazine name + Volume + Number + Year

---

## Files Modified

1. **`/src/TheArchive/wwwroot/css/archive.css`**
   - Changed `.article-list` from columns to flexbox
   - Removed column-specific CSS from `.article-row`
   - Removed media query for responsive columns

---

## Build Status

```bash
cd /home/justin/repos/urban-sniffle/the-archive && dotnet build
# Build succeeded. 0 Error(s), 0 Warning(s)
```

---

## Testing

### Visual Checks

- [ ] Navigate to a model detail page
- [ ] Scroll to Articles section
- [ ] Verify articles display in single column (stacked vertically)
- [ ] Verify each row spans full width
- [ ] Verify consistent spacing between rows
- [ ] Check on different screen sizes
- [ ] Confirm chronological order is maintained

### Comparison

**Before**: Articles in 2 side-by-side columns, cramped layout  
**After**: Articles in single column, full-width rows, easier to scan

---

## Related CSS

### Article Row Grid

Each article row still uses CSS Grid internally:

```css
.article-row {
    display: grid;
    grid-template-columns: 32px 1fr auto;
    /* Page# | Content | Issue Info */
}
```

This creates the 3-column layout **within** each row:
1. Page number (32px fixed)
2. Article content (flexible)
3. Magazine info (auto-width)

---

## Summary

Converted the article list from a 2-column CSS multi-column layout to a single-column flexbox layout. Articles now stack vertically in chronological order, using full available width for better readability.

**Key Change**:
- **Before**: `column-count: 2` (2 columns, cramped)
- **After**: `flex-direction: column` (single column, spacious)

---

**End of Fix**

