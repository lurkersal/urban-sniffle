# Segment Red Text - ArticleList View Fix - COMPLETE

## Problem
The segment validation feature was working, but the red text was not appearing in the **ArticleList** view (the article cards in the left pane). The user reported that after editing the Pages field to include a missing page (80), the segment text in the article card did not turn red.

## Root Cause
The segment red text feature was only implemented for the **ArticleEditorView** (the editor pane on the right), but NOT for the **ArticleList** view (the article cards on the left). 

The ArticleList.axaml had this hardcoded TextBlock:
```xml
<TextBlock Text="{Binding Display}" 
           Foreground="Black"  <!-- HARDCODED BLACK -->
           ... />
```

Even though `HasMissingPages` was being set correctly and validation was working, the ArticleList view never used it because the foreground color was hardcoded to Black.

## Solution
Added the same converter binding to ArticleList that was already working in ArticleEditorView.

## Changes Made

### Updated ArticleList.axaml
**File:** `src/index-editor/Views/ArticleList.axaml`

**Changes:**
1. Added `SegmentMissingPagesForegroundConverter` to UserControl resources
2. Changed segment TextBlock Foreground from hardcoded "Black" to binding with converter

**Before:**
```xml
<TextBlock Text="{Binding Display}" 
           HorizontalAlignment="Center" 
           VerticalAlignment="Center" 
           Foreground="Black"  <!-- HARDCODED -->
           FontSize="13" 
           FontWeight="SemiBold" />
```

**After:**
```xml
<TextBlock Text="{Binding Display}" 
           HorizontalAlignment="Center" 
           VerticalAlignment="Center" 
           Foreground="{Binding HasMissingPages, Converter={StaticResource SegmentMissingPagesForeground}}"
           FontSize="13" 
           FontWeight="SemiBold" />
```

## Build Status
✅ **Build Successful**
```
Build succeeded.
    63 Warning(s)
    0 Error(s)
```

All warnings are pre-existing.

## Testing Instructions

### Test Now:
1. Open your folder with the "Women's Writes" article
2. The article should show segments "68-77" and "79-81" in the article card
3. Edit the Pages field to add page 80: change "68-77|79-81" to "68-77|79-81|80"
4. Press Enter or Tab to commit the change
5. ✅ The segment "79-81" should **immediately turn RED** in the article card
6. The segment should also be red in the ArticleEditorView (if you look there)

### Additional Test:
1. Reload the folder (close and reopen, or Ctrl+O)
2. ✅ The segment "79-81" should still be RED on the article card
3. Edit to remove page 80: change back to "68-77|79-81"
4. ✅ The segment should turn BLACK again

## Where Segments Display Red Now

After this fix, segments with missing pages show in red in BOTH locations:
1. ✅ **ArticleList view** - Article cards in the left pane (FIXED)
2. ✅ **ArticleEditorView** - Segment lozenges in the editor pane (already working)

## Files Modified (This Fix)
- `src/index-editor/Views/ArticleList.axaml`

## Files Modified (Complete Feature)
1. `src/common/Shared/Segment.cs` - Added HasMissingPages property
2. `src/common/Shared/ArticleLine.cs` - Added ValidateSegments method and Segments notification
3. `src/index-editor/Views/SegmentMissingPagesForegroundConverter.cs` - Created converter (NEW)
4. `src/index-editor/Views/ArticleEditorView.axaml` - Added converter binding (editor pane)
5. `src/index-editor/Views/ArticleList.axaml` - Added converter binding (article cards) **← THIS FIX**
6. `src/index-editor/Views/EditorStateViewModel.cs` - Added automatic validation
7. `src/index-editor/MainWindow.axaml.cs` - Added validation on folder load
8. `src/index-editor/Views/PageControllerView.axaml.cs` - Added validation on segment end

## Summary

The feature was working correctly behind the scenes - validation was happening and `HasMissingPages` was being set. The problem was purely a UI issue: the ArticleList view wasn't using the data. 

This was a simple one-line fix (plus adding the resource reference) that makes the red text visible where the user was actually looking.

## Related Documentation
- `SEGMENT_MISSING_PAGES_RED_TEXT.md` - Original feature implementation
- `SEGMENT_VALIDATION_FIX.md` - Validation timing fix for Pages editing

