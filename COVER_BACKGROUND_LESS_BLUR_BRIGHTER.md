# Cover Background Adjustment - Less Blur, Brighter

**Date**: April 22, 2026  
**Status**: ✅ **UPDATED**

---

## Changes Made

Adjusted the cover background to be **less blurred** and **brighter** as requested.

### CSS Property Changes

| Property | Previous | New | Change |
|----------|----------|-----|--------|
| **Blur** | 15px | 8px | -47% (much sharper) |
| **Opacity** | 35% | 45% | +29% (brighter) |
| **Scale** | 1.1 | 1.05 | Less scaling (sharper edges) |
| **Overlay Top** | 30% dark | 20% dark | 33% lighter |
| **Overlay Mid** | 60% dark | 50% dark | 17% lighter |
| **Overlay Bottom** | 85% dark | 80% dark | 6% lighter |
| **Content Mid** | 70% dark | 60% dark | 14% lighter |
| **Content Bottom** | 95% dark | 90% dark | 5% lighter |

---

## Visual Impact

### Blur Reduction (15px → 8px)
- **More detail visible** from the cover image
- **Sharper appearance** while still atmospheric
- **Better recognition** of cover artwork
- **Less "frosted glass" effect**

### Brightness Increase
- **45% opacity** (was 35%) - cover is more prominent
- **Lighter overlays** at all levels - less darkening
- **20% dark at top** (was 30%) - much brighter header
- **Overall effect**: Cover image pops more

---

## Before vs After

### Before (35% opacity, 15px blur)
```
Background: Subtle, heavily blurred
Header: Dark (30% overlay + 35% opacity)
Effect: Atmospheric but very soft
```

### After (45% opacity, 8px blur)
```
Background: Vibrant, moderately blurred
Header: Bright (20% overlay + 45% opacity)
Effect: Clear cover with artistic blur
```

---

## Readability Check

✅ **Magazine Title**: Still perfectly readable (white on blurred background)  
✅ **Subtitles**: Clear and legible  
✅ **Filter Chips**: Good contrast  
✅ **Article List**: Clean backgrounds, no interference  
✅ **Article Thumbnails**: No bleedthrough  

---

## Technical Details

### Why 8px Blur?

- **15px** - Too blurred, lost too much detail
- **8px** - Sweet spot: atmospheric but recognizable
- **5px-** - Would be too sharp, compete with content

### Why 45% Opacity?

- **35%** - A bit too subtle
- **45%** - More prominent without overwhelming
- **50%+** - Would start to dominate

### Why Lighter Overlays?

Combined with higher opacity, needed to reduce darkening:
- Top: 20% dark (was 30%) - lets more background through
- Mid: 50% dark (was 60%) - maintains balance
- Bottom: 80% dark (was 85%) - still protects thumbnails

---

## Layer Calculation

### Top (Header Area)
```
Cover opacity: 45%
Overlay darkness: 20%
Content gradient: 0%
= Effective brightness: ~36% visible
```

### Middle (Transition)
```
Cover opacity: 45%
Overlay darkness: 50%
Content gradient: 60%
= Effective brightness: ~9% visible
```

### Bottom (Article List)
```
Cover opacity: 45%
Overlay darkness: 80%
Content gradient: 90%
= Effective brightness: ~1% visible
```

Result: Background clearly visible at top, fades smoothly to protect content.

---

## Files Modified

**File**: `/home/justin/repos/urban-sniffle/the-archive/src/TheArchive/wwwroot/css/archive.css`

**Lines Modified**: 38-74

---

## Build Status

```bash
cd /home/justin/repos/urban-sniffle/the-archive/src/TheArchive
dotnet build

# Result:
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

---

## User Experience

### What You'll Notice

1. **More Detail** - Can see more of the cover artwork
2. **Brighter** - Cover image more prominent
3. **Less "Foggy"** - Cleaner, sharper appearance
4. **Better Colors** - Cover colors show through more
5. **Still Atmospheric** - Maintains the artistic effect
6. **No Interference** - Article content still clean

### Perfect For

- ✅ Recognizing cover models
- ✅ Seeing cover text/headlines
- ✅ Appreciating cover photography
- ✅ Creating immersive browsing experience
- ✅ Maintaining professional appearance

---

## Customization Options

If you want to adjust further:

### More Blur
```css
filter: blur(12px);  /* Softer */
```

### Less Blur
```css
filter: blur(5px);   /* Sharper */
```

### Brighter
```css
opacity: 0.5;        /* 50% */
```

### Darker
```css
opacity: 0.4;        /* 40% */
```

---

## Summary

Successfully adjusted the cover background to be less blurred (15px → 8px) and brighter (35% → 45% opacity, lighter overlays). The cover image is now more recognizable and vibrant while maintaining perfect text readability and protecting article thumbnails from interference. The effect creates a beautiful, immersive magazine browsing experience with clear visual hierarchy.

**Blur**: ✅ Reduced from 15px to 8px  
**Brightness**: ✅ Increased from 35% to 45%  
**Overlays**: ✅ Lightened at all levels  
**Readability**: ✅ Still perfect  
**Build**: ✅ Success  

---

**End of Adjustment Documentation**

