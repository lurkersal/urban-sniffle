# Article Layout: Conditional Columns

**Date**: April 21, 2026  
**Status**: ✅ **COMPLETED** - Issue pages 2 columns, model pages single column

---

## Summary

Implemented conditional column layout for articles:
- **Issue Detail Pages**: 2 columns (newspaper-style layout)
- **Model Detail Pages**: Single column (full-width rows)

---

## Problem

All pages were using single column after previous change. User wants:
- Issue pages to show articles in 2 columns for compact viewing
- Model pages to show articles in single column for chronological reading

---

## Solution

Use CSS specificity to apply different layouts based on parent container:

### File: `/src/TheArchive/wwwroot/css/archive.css`

```css
/* Default: 2 columns for issue pages */
.article-list {
    column-count: 2;
    column-gap: 0.75rem;
}

/* Override: Single column for model detail pages */
.model-detail .article-list {
    column-count: 1;
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
}

/* Article rows: support both layouts */
.article-row {
    /* ... */
    break-inside: avoid;      /* For column layout */
    margin-bottom: 0.75rem;   /* For column layout */
}

/* Remove margin for flexbox layout */
.model-detail .article-row {
    margin-bottom: 0;  /* Gap handled by flex parent */
}

/* Responsive: single column on mobile for all */
@media (max-width: 1024px) {
    .article-list {
        column-count: 1;
    }
}
```

---

## How It Works

### CSS Specificity

1. **Default Rule** (`.article-list`)
   - Applies to all article lists
   - Uses 2-column layout

2. **Specific Rule** (`.model-detail .article-list`)
   - Higher specificity (two classes)
   - Overrides default for model pages only
   - Uses single-column flexbox

3. **Media Query** (`@media`)
   - Applies to all screens < 1024px
   - Forces single column on mobile

---

## Visual Result

### Issue Detail Page (2 Columns)

```
Articles:
┌──────────────────────────────────────────────┐
│ 45 | Cover       │ 80 | Model              │
│ 56 | Feature     │ 92 | Interview          │
│ 60 | Pictorial   │ 100| Cover              │
└──────────────────────────────────────────────┘
    Column 1            Column 2
```

### Model Detail Page (Single Column)

```
Articles:
┌──────────────────────────────────────────────┐
│ 45 | Cover | Playboy Vol 46 No 3 (1999)     │
│ 56 | Feature | Penthouse Vol 30 No 6        │
│ 60 | Pictorial | Playboy Vol 46 No 8        │
│ 80 | Model | Club International Vol 15      │
└──────────────────────────────────────────────┘
         Single Column (chronological)
```

---

## Benefits

✅ **Issue Pages**: Compact 2-column layout shows more articles at once  
✅ **Model Pages**: Single column shows chronological timeline clearly  
✅ **Responsive**: Both collapse to single column on mobile  
✅ **CSS Only**: No JavaScript or view changes required  
✅ **Maintainable**: Clear separation using parent class selector  

---

## Technical Details

### Parent Containers

**Issue Detail Page**:
```html
<div class="main">
    <div class="article-list">
        <!-- 2 columns by default -->
    </div>
</div>
```

**Model Detail Page**:
```html
<div class="main">
    <div class="model-detail">
        <div class="article-list">
            <!-- Single column via .model-detail override -->
        </div>
    </div>
</div>
```

### CSS Cascade

1. Base rule applies 2 columns
2. Specific rule (`.model-detail .article-list`) overrides for model pages
3. Media query overrides all for mobile

---

## Responsive Behavior

### Desktop (> 1024px)

- **Issue pages**: 2 columns
- **Model pages**: 1 column

### Mobile (≤ 1024px)

- **All pages**: 1 column

---

## Files Modified

1. **`/src/TheArchive/wwwroot/css/archive.css`**
   - Default: 2 columns for `.article-list`
   - Override: 1 column for `.model-detail .article-list`
   - Added: Media query for responsive layout
   - Added: Margin override for flexbox layout

---

## Build Status

```bash
cd /home/justin/repos/urban-sniffle/the-archive && dotnet build
# Build succeeded. 0 Error(s), 0 Warning(s)
```

---

## Testing

### Issue Detail Page

- [ ] Navigate to an issue page (e.g., `/issues/123`)
- [ ] Scroll to articles section
- [ ] Verify articles display in 2 columns side-by-side
- [ ] Resize window to < 1024px
- [ ] Verify columns collapse to single column

### Model Detail Page

- [ ] Navigate to a model page (e.g., `/models/123`)
- [ ] Scroll to articles section
- [ ] Verify articles display in single column
- [ ] Verify chronological order is clear
- [ ] Verify full-width rows

---

## CSS Selectors Used

### Default (Issue Pages)

```css
.article-list { }  /* Specificity: 0,1,0 */
```

### Override (Model Pages)

```css
.model-detail .article-list { }  /* Specificity: 0,2,0 (higher!) */
```

The higher specificity ensures the model page rule overrides the default.

---

## Summary

Successfully implemented conditional column layouts using CSS specificity:

- **Issue detail pages**: 2-column layout for compact article display
- **Model detail pages**: Single-column layout for chronological timeline viewing
- **Responsive**: Both layouts adapt to mobile screens

**Key Technique**: Using parent class selector (`.model-detail`) to override default layout without modifying HTML or requiring JavaScript.

---

**End of Implementation**

