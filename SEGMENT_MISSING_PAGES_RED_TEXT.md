# Segment Missing Pages Red Text Feature - Complete

## Summary
Implemented visual indication for article segments that have missing page images. Segment text now displays in **red** when any page in the segment range is missing (no image file exists on disk).

## Implementation Date
February 27, 2026

## Changes Made

### 1. Added HasMissingPages Property to Segment
**File:** `src/common/Shared/Segment.cs`

**Changes:**
- Added `HasMissingPages` boolean property to track if any pages in the segment range are missing
- Property is computed externally by checking if image files exist for all pages in the range
- Property raises PropertyChanged event when set, allowing UI to react

```csharp
public bool HasMissingPages
{
    get => _hasMissingPages;
    set
    {
        if (_hasMissingPages != value)
        {
            _hasMissingPages = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasMissingPages)));
        }
    }
}
```

### 2. Added ValidateSegments Method to ArticleLine
**File:** `src/common/Shared/ArticleLine.cs`

**Changes:**
- Added `ValidateSegments(string folder, Func<string, int, bool> imageExistsChecker)` method
- Method checks all closed segments in an article for missing pages
- Uses a provided callback function to check if each page image exists
- Sets `HasMissingPages` on each segment accordingly
- Active segments are not validated (since they're still being edited)

```csharp
public void ValidateSegments(string? folder, Func<string, int, bool>? imageExistsChecker = null)
{
    if (string.IsNullOrWhiteSpace(folder) || Segments == null)
        return;

    foreach (var segment in Segments)
    {
        if (segment.End.HasValue)
        {
            // Closed segment - check all pages in range
            bool hasMissing = false;
            for (int page = segment.Start; page <= segment.End.Value; page++)
            {
                bool exists = imageExistsChecker?.Invoke(folder, page) ?? false;
                if (!exists)
                {
                    hasMissing = true;
                    break;
                }
            }
            segment.HasMissingPages = hasMissing;
        }
        else
        {
            // Active segment - don't validate yet
            segment.HasMissingPages = false;
        }
    }
}
```

### 3. Created Foreground Color Converter
**File:** `src/index-editor/Views/SegmentMissingPagesForegroundConverter.cs` (NEW)

**Changes:**
- Created new converter that returns Red brush when `HasMissingPages` is true
- Returns Black brush when pages are not missing
- Used to dynamically set the foreground color of segment text

```csharp
public class SegmentMissingPagesForegroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool hasMissing && hasMissing)
        {
            return new SolidColorBrush(Colors.Red);
        }
        return new SolidColorBrush(Colors.Black);
    }
}
```

### 4. Updated ArticleEditorView XAML
**File:** `src/index-editor/Views/ArticleEditorView.axaml`

**Changes:**
- Added `SegmentMissingPagesForegroundConverter` to UserControl resources
- Updated segment TextBlock Foreground binding to use the converter
- Segments with missing pages now show red text

```xml
<UserControl.Resources>
    <!-- ...existing converters... -->
    <local:SegmentMissingPagesForegroundConverter x:Key="SegmentMissingPagesForeground" />
</UserControl.Resources>

<!-- In segment DataTemplate: -->
<TextBlock Text="{Binding Display}" 
           VerticalAlignment="Center" 
           HorizontalAlignment="Center" 
           Foreground="{Binding HasMissingPages, Converter={StaticResource SegmentMissingPagesForeground}}" 
           FontWeight="Normal" />
```

### 5. Added Validation on Article Load
**File:** `src/index-editor/MainWindow.axaml.cs` (LoadArticlesFromFolder)

**Changes:**
- Added segment validation after loading articles from folder
- Validates all segments for all articles to check for missing pages
- Uses `ImageExistsInFolder` helper method to check page image existence

```csharp
// Validate segments for missing pages
try
{
    foreach (var article in IndexEditor.Shared.EditorState.Articles)
    {
        article.ValidateSegments(folder, ImageExistsInFolder);
    }
}
catch (Exception ex) { DebugLogger.LogException("LoadArticlesFromFolder: validate segments", ex); }
```

### 6. Added Validation on Segment End
**File:** `src/index-editor/Views/PageControllerView.axaml.cs` (EndActiveSegment)

**Changes:**
- Added segment validation after ending an active segment
- Ensures newly closed segments are immediately validated
- Uses `ImageHelper.ImageExists` to check page existence

```csharp
// Validate segments for missing pages
if (art != null)
{
    try
    {
        var folder = EditorState.CurrentFolder;
        if (!string.IsNullOrWhiteSpace(folder))
        {
            art.ValidateSegments(folder, (f, p) => IndexEditor.Shared.ImageHelper.ImageExists(f, p));
        }
    }
    catch (Exception ex) { DebugLogger.LogException("EndActiveSegment: validate segments", ex); }
}
```

## Behavior

### Visual Indication
- **Normal segments**: Black text (e.g., "13-15")
- **Segments with missing pages**: Red text (e.g., "110-114" in red)

### When Validation Occurs
1. **On folder load**: All segments are validated when opening a folder
2. **After ending segment**: When an active segment is closed (End button clicked)
3. **Active segments**: Not validated until they are closed

### Detection Logic
- For each closed segment, checks if image files exist for all pages in the range
- Uses standard image filename patterns (e.g., 001.jpg, 1.jpg, page-1.jpg)
- If any page in the range is missing, the entire segment displays in red
- Missing pages are detected using `ImageHelper.ImageExists` which checks multiple filename patterns

## Files Modified/Created

### Modified:
1. `src/common/Shared/Segment.cs` - Added HasMissingPages property
2. `src/common/Shared/ArticleLine.cs` - Added ValidateSegments method
3. `src/index-editor/Views/ArticleEditorView.axaml` - Updated segment display with converter
4. `src/index-editor/MainWindow.axaml.cs` - Added validation on folder load
5. `src/index-editor/Views/PageControllerView.axaml.cs` - Added validation on segment end

### Created:
6. `src/index-editor/Views/SegmentMissingPagesForegroundConverter.cs` - New color converter

## Build Status

✅ **Build Successful**
```
Build succeeded.
    63 Warning(s)
    0 Error(s)
```

All warnings are pre-existing and not related to these changes.

## Testing Recommendations

1. **Test with complete segments:**
   - Open a folder with all page images present
   - Verify segments show in black text
   - End an active segment
   - Verify newly closed segment shows in black if all pages exist

2. **Test with missing pages:**
   - Open a folder where some pages in a segment range are missing
   - Verify affected segments show in red text
   - Create a new segment spanning pages where some don't exist
   - End the segment and verify it turns red

3. **Test segment validation:**
   - Delete an image file that's part of a segment
   - Reload the folder (close and reopen)
   - Verify the segment now shows in red

4. **Test active segments:**
   - Create an active segment (Ctrl+A)
   - Verify active segments don't show in red (not validated until closed)
   - End the segment
   - Verify validation occurs and color reflects missing pages status

## Use Cases

This feature helps users quickly identify problems with their magazine indexing:
- **Missing scans**: If pages are missing from a segment, it's immediately visible
- **File naming issues**: If image files don't follow expected naming patterns
- **Data quality**: Easy visual check to ensure all pages in an article are present
- **Before publishing**: Quick validation that all indexed pages have corresponding images

## Technical Notes

- The validation uses `ImageHelper.ImageExists` which checks multiple filename patterns
- Validation is performed synchronously during load/save operations
- The HasMissingPages property is not persisted - it's computed on demand
- Active (open) segments are not validated since they're still being edited
- The red color is hard-coded in the converter (not themeable)

## Future Enhancements

1. Add tooltip showing which specific pages are missing
2. Add context menu option to re-validate all segments
3. Make the error color configurable/themeable
4. Add a summary count of segments with missing pages
5. Add validation on segment reopening
6. Add option to automatically fix segments by removing missing pages
7. Show missing page indicators in the page controller as well

## Related Features

This feature complements:
- Segment display in article editor
- Page existence checking in page controller
- Image loading and validation
- Article list display

## Notes

- Validation only checks for image file existence, not validity
- The feature assumes standard image naming patterns
- Performance impact is minimal - validation only runs on load and segment close
- Red text stands out clearly against all category background colors

