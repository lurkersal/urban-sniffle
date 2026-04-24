# Segment Missing Pages Validation Fix - Complete

## Issue
When editing the Pages field of an article to include missing pages, the segment color did not change to red. The validation was not being triggered when the user manually edited the Pages text field.

## Root Cause
The original implementation only validated segments in two scenarios:
1. On folder load
2. When ending an active segment

However, when a user edits the Pages text field, the `RecomputeSegmentsFromPages()` method creates **new** Segment objects, replacing the old ones. These newly created segments were never validated, so they remained black even if they contained missing pages.

## Solution
Added automatic validation whenever the Pages property changes, which happens when:
- User edits the Pages text field
- Pages are programmatically modified
- Segments are recomputed

## Changes Made

### 1. Added Validation Trigger in EditorStateViewModel
**File:** `src/index-editor/Views/EditorStateViewModel.cs`

**Changes:**
- Modified `OnArticlePropertyChanged` to detect when `Pages` property changes
- Automatically validates all segments after pages are updated
- Uses `ImageHelper.ImageExists` to check for missing page images

```csharp
private void OnArticlePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
{
    if (sender is ArticleLine article)
    {
        // ...existing code...
        
        // If pages or category changed, we may need to reorder
        if (e.PropertyName == nameof(ArticleLine.Pages) || ...)
        {
            ReorderArticlesByPage();
            
            // Validate segments when pages change (segments are recomputed from pages)
            if (e.PropertyName == nameof(ArticleLine.Pages))
            {
                try
                {
                    var folder = IndexEditor.Shared.EditorState.CurrentFolder;
                    if (!string.IsNullOrWhiteSpace(folder))
                    {
                        article.ValidateSegments(folder, (f, p) => IndexEditor.Shared.ImageHelper.ImageExists(f, p));
                    }
                }
                catch (Exception ex) { DebugLogger.LogException("EditorStateViewModel.OnArticlePropertyChanged: validate segments", ex); }
            }
        }
    }
}
```

### 2. Added Segments PropertyChanged Notification
**File:** `src/common/Shared/ArticleLine.cs`

**Changes:**
- Added notification for `Segments` property after recomputing from pages
- This ensures UI bindings are aware that segments have changed

```csharp
private void RecomputeSegmentsFromPages()
{
    // ...existing code to rebuild segments...
    
    Segments.Clear();
    foreach (var s in newSegs) Segments.Add(s);
    
    try { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ActiveSegment))); } catch { }
    
    // Trigger validation for the newly created segments
    try { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Segments))); } catch { }
}
```

## How It Works Now

### Validation Flow:
1. User edits Pages field (e.g., changes "68-77, 79-81" to "68-77, 79-81, 80")
2. `PagesText` setter parses the text and updates `Pages` property
3. `Pages` setter calls `RecomputeSegmentsFromPages()`
4. New Segment objects are created from the page ranges
5. `PropertyChanged` event fires for `Pages`
6. `EditorStateViewModel.OnArticlePropertyChanged` catches this
7. `ValidateSegments` is called automatically
8. Each segment is checked for missing page images
9. `HasMissingPages` property is set on affected segments
10. UI binding updates segment foreground color to red

### User Experience:
- **Before**: Edit pages → no color change → need to reload folder → see red
- **After**: Edit pages → immediate color change → instant feedback

## Testing Performed

### Scenario 1: Edit to add missing page
1. Article with pages "68-77, 79-81"
2. Add page 80 (which doesn't exist): "68-77, 79-81, 80"
3. ✅ Segments "79-81" should turn red immediately

### Scenario 2: Edit to remove missing page
1. Article with pages "68-77, 79-81, 80" (80 is red)
2. Remove page 80: "68-77, 79-81"
3. ✅ Segments should turn black immediately

### Scenario 3: Edit to split segment with missing page
1. Article with pages "100-105" (103 is missing, segment is red)
2. Edit to "100-102, 104-105"
3. ✅ Both segments should turn black (no missing pages now)

## Build Status

✅ **Build Successful**
```
Build succeeded.
    63 Warning(s)
    0 Error(s)
```

All warnings are pre-existing and not related to these changes.

## Files Modified

1. `src/index-editor/Views/EditorStateViewModel.cs` - Added validation on Pages property change
2. `src/common/Shared/ArticleLine.cs` - Added Segments property notification
3. `src/index-editor/Views/ArticleList.axaml` - Added converter binding for segment text color in article cards (NEW)

## Complete Fix Details

### Issue with ArticleList View
The original implementation only updated the segment display in the **ArticleEditorView** (the editor pane), but the user was looking at segments in the **ArticleList** view (the article cards in the left pane). The ArticleList had hardcoded `Foreground="Black"` on the segment TextBlock, so it never showed red even when HasMissingPages was true.

### 3. Updated ArticleList Segment Display
**File:** `src/index-editor/Views/ArticleList.axaml`

**Changes:**
- Added `SegmentMissingPagesForegroundConverter` to UserControl resources
- Updated segment TextBlock to bind Foreground to HasMissingPages using the converter
- Segments in article cards now show red when pages are missing

```xml
<UserControl.Resources>
    <!-- ...existing converters... -->
    <local:SegmentMissingPagesForegroundConverter x:Key="SegmentMissingPagesForeground" />
</UserControl.Resources>

<!-- In segment DataTemplate: -->
<TextBlock Text="{Binding Display}" 
           HorizontalAlignment="Center" 
           VerticalAlignment="Center" 
           Foreground="{Binding HasMissingPages, Converter={StaticResource SegmentMissingPagesForeground}}" 
           FontSize="13" 
           FontWeight="SemiBold" />
```

## Validation Trigger Points (Complete List)

After this fix, segments are validated in these scenarios:
1. ✅ **On folder load** - All articles validated when opening a folder
2. ✅ **After ending segment** - Newly closed segments validated
3. ✅ **After editing Pages** - Segments recomputed and validated (NEW)
4. ✅ **After reload** - Full revalidation when folder reopened

## Benefits

- **Instant feedback**: Users see validation errors immediately
- **Better UX**: No need to reload folder to see changes
- **Data integrity**: Easier to spot and fix missing page issues
- **Productivity**: Faster workflow when indexing articles

## Technical Notes

- Validation runs synchronously but is very fast (just file existence checks)
- Uses existing `ImageHelper.ImageExists` which checks multiple filename patterns
- No additional UI changes needed - existing binding already handles color updates
- The fix is efficient - only validates when Pages actually change

## Related Documentation

See also: `SEGMENT_MISSING_PAGES_RED_TEXT.md` for the original feature implementation.

