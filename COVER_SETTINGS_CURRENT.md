# Background Settings - Issue & Model Pages

**Updated**: April 22, 2026

---

## Pages with Blurred Backgrounds

✅ **Issue Detail Pages** - Uses cover image  
✅ **Model Detail Pages** - Uses model thumbnail  

---

## Quick Settings Reference

### Shared Background Class
```css
.issue-cover-bg {
    opacity: 0.45;           /* 45% - Bright and visible */
    filter: blur(8px);       /* 8px - Less blur, more detail */
    transform: scale(1.05);  /* Minimal scaling */
}

.issue-cover-bg::after {
    /* Gradient overlay: 20% → 50% → 80% dark */
    background: linear-gradient(to bottom, 
        rgba(15, 14, 13, 0.2) 0%,
        rgba(15, 14, 13, 0.5) 50%,
        rgba(15, 14, 13, 0.8) 100%);
}
```

### Page-Specific Content Gradients
```css
.issue-detail-page,
.model-detail-page {
    /* Content gradient: 0% → 60% → 90% dark */
    background: linear-gradient(to bottom, 
        transparent 0%,
        rgba(15, 14, 13, 0.6) 250px,
        rgba(15, 14, 13, 0.9) 400px);
}
```

---

## Result

✅ **Less blurred** - 8px blur (was 15px)  
✅ **Brighter** - 45% opacity (was 35%)  
✅ **More detail visible** from images  
✅ **Lighter overlays** throughout  
✅ **Text still readable**  
✅ **No thumbnail bleedthrough**  
✅ **Consistent across both page types**  

---

## Implementation

| Page Type | Background Image | Class Used |
|-----------|-----------------|------------|
| **Issue Detail** | Cover image | `.issue-cover-bg` |
| **Model Detail** | Model thumbnail | `.issue-cover-bg` |

Both pages share the same background styling for consistency.

---

## Adjustment History

1. **Initial**: 15% opacity, 20px blur - Too subtle, invisible
2. **First fix**: 35% opacity, 15px blur - Visible but soft
3. **Second fix**: 45% opacity, 8px blur - Bright and detailed
4. **Model pages**: Added same treatment using thumbnails

---

