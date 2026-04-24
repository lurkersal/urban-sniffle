# Article List Display Restored

## Summary

Restored the ArticleList display to its original appearance by removing the thumbnail display feature that was causing empty thumbnail borders to appear.

## Problem

The ArticleList (left pane) was showing empty white thumbnail rectangles instead of category labels:
- **Before fix:** Empty 120x160px bordered rectangles on the right, category labels hidden
- **After fix:** Category labels displayed on the right (Cover, Index, Feature, Model, etc.), no thumbnail rectangles

## Root Cause

The ArticleList XAML template included conditional thumbnail display logic:
1. When `HasThumbnail` was true: showed thumbnail border (even if empty) and hid category label
2. When `HasThumbnail` was false: showed category label and hid thumbnail border

The `HasThumbnail` property was returning true (likely due to `ThumbnailPath` being set), causing empty thumbnail borders to display.

## Changes Made

**File:** `/home/justin/repos/urban-sniffle/src/index-editor/Views/ArticleList.axaml`

### Removed:
1. **Thumbnail Border** (lines 70-78) - The entire `<Border>` element for displaying thumbnail images
2. **Conditional Height Binding** - Changed from dynamic height based on `HasThumbnail` to fixed 100px height
3. **Conditional Category Visibility** - Removed `IsVisible="{Binding !HasThumbnail}"` from category label

### Result:
```xml
<!-- Before -->
Height="{Binding HasThumbnail, Converter={StaticResource ArticleRowHeightConverter}}"
<Border Grid.Column="2" ... IsVisible="{Binding HasThumbnail}">
    <Image Source="{Binding ThumbnailPath}" ... />
</Border>
<TextBlock ... IsVisible="{Binding !HasThumbnail}"/>

<!-- After -->
Height="100"
<!-- Thumbnail border removed -->
<TextBlock ... /> <!-- Category always visible -->
```

## Current Behavior

The ArticleList now displays:
- **Color bar** (left) - Category-based color indicator
- **Article content** (center) - Title, model info, photographer, segments
- **Category label** (right) - Always visible, e.g., "Cover", "Model", "Feature", "Index"
- **Fixed height** - All article rows are 100px tall
- **No thumbnails** - Thumbnail display feature removed from ArticleList

## Note

This change only affects the **ArticleList** (left pane). The thumbnail page selection feature for TheArchive is still functional via the backend:
- Database: `Article.ThumbnailPage` column
- JSON: `thumbnailPage` field in _index.json
- TheArchive: Uses `COALESCE(ThumbnailPage, MIN(Page))` for display

## Build Status

✅ **Build Successful** - 0 errors, 22 warnings (all pre-existing)

## Testing

- [ ] ArticleList shows category labels on the right
- [ ] No empty thumbnail borders visible
- [ ] All rows are consistent 100px height
- [ ] Color bars and article content display correctly

