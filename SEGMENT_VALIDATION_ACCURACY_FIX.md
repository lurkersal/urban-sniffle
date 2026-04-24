# Segment Validation Fixes - False Positives and Text Editing Issues

## Problems Identified
1. **False positive red segments**: Segments showing red even when all pages exist
2. **Backspace deleting entire text**: When editing the Pages field, pressing backspace sometimes deleted the entire text instead of single character

## Root Causes

### Problem 1: False Positive Red Segments
The `ImageExistsInFolder` method in MainWindow.axaml.cs only checked for ONE specific filename pattern:
```csharp
var filePath = System.IO.Path.Combine(folder, $"{pageNumber:D3}.jpg");
return System.IO.File.Exists(filePath);
```

This only looked for `001.jpg`, `002.jpg`, etc.

However, images can exist with many different naming patterns:
- `1.jpg`, `2.jpg` (no zero-padding)
- `01.jpg`, `02.jpg` (2-digit padding)
- `001.jpg`, `002.jpg` (3-digit padding)
- `page-1.jpg`, `p1.jpg` (with prefixes)
- `.png`, `.tif`, etc. (different extensions)

The `ImageHelper.ImageExists` method checks ALL these patterns, but `ImageExistsInFolder` was only checking one. This created inconsistency:
- `ImageHelper.ImageExists` (used in some places) → found the image → segment valid
- `ImageExistsInFolder` (used on folder load) → didn't find the image → segment marked as missing → RED TEXT

### Problem 2: Backspace Deleting Entire Text
The `PagesText` property setter was calling `Validate()` immediately:
```csharp
public string PagesText
{
    get => string.Join("|", PagesToSegments(Pages));
    set
    {
        var parsed = ParsePageText(value, out bool hasError);
        Pages = parsed;
        HasPageNumberError = hasError;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PagesText)));
        Validate();  // ← PROBLEM: Called during every keystroke!
    }
}
```

When `Validate()` is called, it triggers property change notifications that can interfere with the TextBox's internal state during editing. This caused:
- Cursor position to be lost
- Selection to be reset
- Sometimes entire text to be replaced on backspace

This is a common WPF/Avalonia issue when validation runs during text input.

## Solutions

### Fix 1: Use ImageHelper.ImageExists Everywhere
**File:** `src/index-editor/MainWindow.axaml.cs`

Changed `ImageExistsInFolder` to use `ImageHelper.ImageExists`:
```csharp
private bool ImageExistsInFolder(string folder, int pageNumber)
{
    // Use ImageHelper.ImageExists to check all possible filename patterns
    return IndexEditor.Shared.ImageHelper.ImageExists(folder, pageNumber);
}
```

Now validation uses the same logic everywhere and checks all possible filename patterns.

### Fix 2: Defer Validation Until Focus Lost
**File:** `src/common/Shared/ArticleLine.cs`

Removed `Validate()` call from `PagesText` setter:
```csharp
public string PagesText
{
    get => string.Join("|", PagesToSegments(Pages));
    set
    {
        var parsed = ParsePageText(value, out bool hasError);
        Pages = parsed;
        HasPageNumberError = hasError;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PagesText)));
        // Don't call Validate() here - let it be triggered when the field loses focus
        // Calling Validate() during editing interferes with text input (e.g., backspace)
    }
}
```

**File:** `src/index-editor/Views/ArticleEditor.axaml`

Added LostFocus event to Pages TextBox:
```xml
<TextBox Name="PagesTextBox" 
         Text="{Binding PagesText, Mode=TwoWay}" 
         Width="240" 
         BorderBrush="{Binding HasPagesError, Converter={StaticResource FieldErrorToBrushConverter}}" 
         LostFocus="OnPagesTextBoxLostFocus" />
```

**File:** `src/index-editor/Views/ArticleEditor.axaml.cs`

Added event handler to validate when user finishes editing:
```csharp
private void OnPagesTextBoxLostFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
{
    try
    {
        // Get the article from the DataContext and trigger validation
        if (this.DataContext is Common.Shared.ArticleLine article)
        {
            article.Validate();
            
            // Also validate segments for missing pages
            try
            {
                var folder = IndexEditor.Shared.EditorState.CurrentFolder;
                if (!string.IsNullOrWhiteSpace(folder))
                {
                    article.ValidateSegments(folder, (f, p) => IndexEditor.Shared.ImageHelper.ImageExists(f, p));
                }
            }
            catch (Exception ex) { DebugLogger.LogException("OnPagesTextBoxLostFocus: validate segments", ex); }
        }
    }
    catch (Exception ex) { DebugLogger.LogException("OnPagesTextBoxLostFocus: outer", ex); }
}
```

Now validation only runs when:
1. User tabs out of the field
2. User clicks elsewhere
3. Field loses focus for any reason

This is standard UI practice - don't validate during typing, validate after editing is complete.

## Build Status
✅ **Build Successful**
```
Build succeeded.
    0 Error(s)
```

## Testing Instructions

### Test 1: False Positives Fixed
1. Open a folder where images use various naming patterns (e.g., "1.jpg", "2.jpg" not "001.jpg")
2. Articles with these pages should NOT show red segments anymore
3. Only segments with truly missing pages should be red

### Test 2: Backspace Works Correctly
1. Open any article
2. Click in the Pages field
3. Type some text: "1|2|3"
4. Position cursor in the middle and press backspace
5. ✅ Should delete ONE character at a time (not entire text)
6. Try typing, deleting, editing normally
7. ✅ All text editing should work naturally

### Test 3: Validation Still Works
1. Edit the Pages field to add a missing page
2. Tab out or click elsewhere
3. ✅ Validation should run (segments turn red/black appropriately)
4. Border should show validation errors if format is wrong

### Test 4: Segment Color Accuracy
1. Find a segment that was showing red before but shouldn't have been
2. Reload folder (Ctrl+O, reopen same folder)
3. ✅ Segment should now be black (if all pages exist)
4. Edit to add a truly missing page
5. Tab out
6. ✅ Segment should turn red (accurately detecting the missing page)

## Files Modified
1. `src/index-editor/MainWindow.axaml.cs` - Fixed ImageExistsInFolder to use ImageHelper
2. `src/common/Shared/ArticleLine.cs` - Removed Validate() from PagesText setter
3. `src/index-editor/Views/ArticleEditor.axaml` - Added LostFocus event to Pages TextBox
4. `src/index-editor/Views/ArticleEditor.axaml.cs` - Added OnPagesTextBoxLostFocus handler

## Impact

### Validation Accuracy
- ✅ **Before**: False positives due to checking only one filename pattern
- ✅ **After**: Accurate validation checking all filename patterns

### Text Editing Experience
- ✅ **Before**: Backspace could delete entire text, cursor jumps, weird behavior
- ✅ **After**: Natural text editing, backspace works correctly

### Performance
- ✅ **Before**: Validation ran on every keystroke
- ✅ **After**: Validation only runs when needed (on focus lost)

## Technical Notes

### Why Validation on Every Keystroke is Bad
1. **Performance**: Unnecessary work on every character
2. **UX**: Interferes with text input state
3. **State Management**: PropertyChanged events during editing cause issues
4. **Cursor Position**: Can cause cursor to jump or reset
5. **Selection**: Can cause text selection to be lost

### Standard UI Pattern
The standard pattern is:
1. **During typing**: No validation, just accept input
2. **On focus lost**: Validate and show errors
3. **On submit**: Final validation before save

This is what all modern UIs do (web forms, desktop apps, mobile apps).

### ImageHelper.ImageExists Patterns
The method checks these patterns in order:
1. `{page}.jpg` (e.g., "1.jpg")
2. `{page}.png`
3. `{page:D2}.jpg` (e.g., "01.jpg")
4. `{page:D2}.png`
5. `{page:D3}.jpg` (e.g., "001.jpg")
6. `{page:D3}.png`
7. `page-{page}.jpg` (e.g., "page-1.jpg")
8. `p{page}.jpg` (e.g., "p1.jpg")

Plus .tif, .tiff, .bmp extensions.

## Related Issues Fixed
- False positive red segments for pages that exist
- Backspace deleting entire field text
- Validation running on every keystroke
- Inconsistent filename pattern checking

## Related Documentation
- `SEGMENT_MISSING_PAGES_RED_TEXT.md` - Original feature
- `SEGMENT_VALIDATION_FIX.md` - Validation timing fix
- `ARTICLELIST_RED_TEXT_FIX.md` - ArticleList view fix

