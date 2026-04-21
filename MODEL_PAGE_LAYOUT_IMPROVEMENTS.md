# Model Page Layout Improvements

**Date**: April 21, 2026  
**Status**: ✅ **COMPLETED** - Model names moved below thumbnails, articles section single-column

---

## Summary

Improved the visual layout of model pages:
1. **Model names** - Moved from overlaid on thumbnail to below the image
2. **Articles section** - Changed to single-column layout with max-width for better readability

---

## Changes Made

### 1. Models Index Page - Name Below Thumbnail

**Issue**: Model name was overlaid on the thumbnail image, obscuring the photo.

**Fix**: Moved name to the model-info section below the image.

#### File: `/src/TheArchive/Views/Models/Index.cshtml`

**Before**:
```razor
<div class="model-photo">
    <img src="..." />
    <div class="model-name-overlay">@item.Model.Name</div>  ← Overlaid on image
</div>
<div class="model-info">
    <div class="model-real-name">@item.Model.Measurements</div>
    ...
</div>
```

**After**:
```razor
<div class="model-photo">
    <img src="..." />  ← Clean image, no overlay
</div>
<div class="model-info">
    <div class="model-name">@item.Model.Name</div>  ← Name below image
    <div class="model-measurements">@item.Model.Measurements</div>
    ...
</div>
```

---

### 2. Model Detail Page - Name Below Portrait

**Issue**: Model name was overlaid on the portrait image.

**Fix**: Moved name below the portrait as a separate element.

#### File: `/src/TheArchive/Views/Models/Detail.cshtml`

**Before**:
```razor
<div class="model-portrait">
    <img src="..." />
    <div class="model-portrait-name">@Model.Model.Name</div>  ← Inside portrait
</div>
```

**After**:
```razor
<div class="model-portrait">
    <img src="..." />  ← Clean portrait
</div>
<div class="model-portrait-name">@Model.Model.Name</div>  ← Below portrait
```

---

### 3. CSS Updates

#### File: `/src/TheArchive/wwwroot/css/archive.css`

**Changes Made**:

1. **Removed gradient overlay from .model-photo**
   - Removed `display: flex`, `align-items`, `padding`
   - Removed `::before` pseudo-element with gradient
   - Image now displays cleanly without darkening

2. **Removed gradient overlay from .model-portrait**
   - Removed `display: flex`, `align-items`, `padding`
   - Removed `::before` pseudo-element with gradient
   - Added `margin-bottom: 1rem` for spacing

3. **Updated .model-name-overlay → .model-name**
   - Changed from overlay positioning to normal flow
   - Placed in model-info section below image
   - Changed color from white to paper color

4. **Updated .model-portrait-name**
   - Removed `position: relative` and `z-index`
   - Changed color from white to paper color
   - Added `text-align: center` for centered name
   - Now appears below portrait as separate element

5. **Renamed .model-real-name → .model-measurements**
   - More descriptive class name
   - Same styling maintained

6. **Added max-width to .model-stats**
   - `max-width: 800px` for better readability
   - Prevents articles section from being too wide

---

## Visual Changes

### Models Index Page

**Before**:
```
┌─────────────────────┐
│                     │
│     [Image]         │
│                     │
│  Name Overlaid ←────┼── Name obscures image
└─────────────────────┘
│ Measurements        │
│ 5 appearances       │
└─────────────────────┘
```

**After**:
```
┌─────────────────────┐
│                     │
│     [Image]         │ ← Clean, unobscured
│                     │
└─────────────────────┘
│ Name                │ ← Name below
│ Measurements        │
│ 5 appearances       │
└─────────────────────┘
```

### Model Detail Page

**Before**:
```
┌─────────────┬────────────────────────────────┐
│             │ Profile                        │
│   Portrait  │ ...                            │
│   with      │                                │
│   Name      │ Articles (full width) ─────────┤
│   Overlay   │ Long lines of text spanning... │
└─────────────┴────────────────────────────────┘
```

**After**:
```
┌─────────────┬────────────────────────────────┐
│             │ Profile                        │
│   Portrait  │ ...                            │
│   (clean)   │                                │
│             │ Articles (max 800px)           │
│  Name       │ Readable width text            │
└─────────────┴────────────────────────────────┘
```

---

## Benefits

✅ **Cleaner Thumbnails** - Full image visible without text overlay  
✅ **Better Hierarchy** - Name is clearly separate from image  
✅ **Improved Readability** - Articles section has optimal line length  
✅ **More Professional** - Modern card-based design pattern  
✅ **Consistent Layout** - Matches common UI conventions  

---

## CSS Changes Summary

### Removed

- `.model-photo::before` - Gradient overlay
- `.model-portrait::before` - Gradient overlay  
- `.model-name-overlay` - Overlay positioning
- `flex` and `padding` from photo containers

### Added

- `.model-name` - Name below thumbnail on index
- `margin-bottom` to `.model-portrait` - Spacing below portrait
- `max-width: 800px` to `.model-stats` - Constrain articles width
- `text-align: center` to `.model-portrait-name` - Center name

### Modified

- `.model-portrait-name` - Changed from overlay to standalone element
- Color schemes changed from white (overlay) to paper (below image)

---

## Files Modified

1. **`/src/TheArchive/Views/Models/Index.cshtml`**
   - Removed `model-name-overlay` from inside `model-photo`
   - Added `model-name` to `model-info` section
   - Renamed `model-real-name` to `model-measurements`

2. **`/src/TheArchive/Views/Models/Detail.cshtml`**
   - Moved `model-portrait-name` outside of `model-portrait`

3. **`/src/TheArchive/wwwroot/css/archive.css`**
   - Removed gradient overlays
   - Updated positioning and colors
   - Added max-width constraint

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
- [ ] Verify model thumbnails show full image without name overlay
- [ ] Verify model names appear below thumbnails
- [ ] Hover over model cards - verify hover effect works
- [ ] Navigate to a model detail page
- [ ] Verify portrait shows full image without name overlay
- [ ] Verify name appears centered below portrait
- [ ] Check articles section has comfortable reading width
- [ ] Verify layout looks good on different screen sizes

### Visual Checks

- [ ] Index page: Clean thumbnails, names clearly visible below
- [ ] Detail page: Portrait not obscured, name centered below
- [ ] Articles section: Not too wide, comfortable reading
- [ ] Initials fallback: Still displays when no image available
- [ ] Overall aesthetic: Professional and modern

---

## Design Rationale

### Why Remove Overlays?

1. **Image Visibility** - Model photos are the primary visual content
2. **Modern Pattern** - Most card UIs place text below images
3. **Accessibility** - Better contrast, easier to read
4. **Flexibility** - Works better with various image types

### Why Single Column for Articles?

1. **Readability** - Optimal line length is 60-80 characters
2. **Scanning** - Easier to scan a single column
3. **Focus** - Reduces visual noise
4. **Responsive** - Works better on all screen sizes

---

## Example Layouts

### Model Card (Index Page)

```
┌──────────────────────────┐
│                          │
│    [Model Photo]         │
│    Full, unobscured      │
│                          │
├──────────────────────────┤
│ Jane Doe                 │  ← Name
│ 34C-24-36               │  ← Measurements
│ 12 appearances          │  ← Count
└──────────────────────────┘
```

### Model Portrait (Detail Page)

```
┌─────────────────┐
│                 │
│   [Portrait]    │
│   Clean image   │
│                 │
└─────────────────┘
    Jane Doe          ← Centered name below
```

---

## Related Patterns

### Similar Layouts in App

- **Magazine cards** - Name below cover image
- **Issue cards** - Title below thumbnail
- **Article previews** - Text below image

This change brings model pages in line with the rest of the application's design language.

---

## Summary

Model thumbnails now display clean, unobscured images with names positioned below for better visibility and modern aesthetics. The articles section on the model detail page uses a single-column layout with optimal reading width for improved usability.

**Visual Improvements**:
- **Before**: Names overlaid on images, obscuring photos
- **After**: Clean images with names clearly displayed below

**Layout Improvements**:
- **Before**: Articles span full width (hard to read)
- **After**: Articles constrained to 800px max (comfortable reading)

---

**End of Enhancement**

