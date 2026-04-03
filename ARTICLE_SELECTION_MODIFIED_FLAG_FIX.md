# Article Selection Modified Flag Fix

## Issue
When changing the selected article in the article list (by clicking on a different article), the file was being marked as modified (`HasUnsavedChanges = true`), even though no actual data had changed. This caused the application to prompt "Do you want to save changes?" when closing, even when the user had only browsed through different articles without making any edits.

## Root Cause
The issue was in `EditorStateViewModel.cs`. When an article is selected:

1. The `SelectedArticle` setter is called
2. It calls `RefreshUIBindings()` on the selected article to force UI bindings to update
3. `RefreshUIBindings()` fires `PropertyChanged` events for several properties:
   - `Title`
   - `Category`
   - `PagesText`
   - `ModelName0`
   - `Age0`
   - `Measurements0`
   - `Contributor0`
   - `Segments`

4. These `PropertyChanged` events trigger `OnArticlePropertyChanged` in the ViewModel
5. `OnArticlePropertyChanged` checks if the property is in the `dataProperties` list
6. Since `Title`, `Category`, and `PagesText` ARE in the `dataProperties` list, it sets `HasUnsavedChanges = true`

This was a false positive - the property changes were just UI refresh notifications, not actual user edits.

Additionally, when selecting an article, the `IsSelected` property is set on all articles to update which one has the selection border. While `IsSelected` is NOT in the `dataProperties` list, it still triggers `PropertyChanged` events unnecessarily.

## Solution
Added a `_suppressHasUnsavedChanges` flag to `EditorStateViewModel` that temporarily prevents setting `HasUnsavedChanges = true` during UI-only operations:

1. **Added the flag**: `private bool _suppressHasUnsavedChanges = false;`

2. **Wrapped `IsSelected` updates**: When updating `IsSelected` flags on all articles during selection, set the flag:
   ```csharp
   _suppressHasUnsavedChanges = true;
   try { /* set IsSelected on all articles */ }
   finally { _suppressHasUnsavedChanges = false; }
   ```

3. **Wrapped `RefreshUIBindings()` call**: When calling `RefreshUIBindings()` to update UI, set the flag:
   ```csharp
   _suppressHasUnsavedChanges = true;
   try { _selectedArticle?.RefreshUIBindings(); }
   finally { _suppressHasUnsavedChanges = false; }
   ```

4. **Check flag in `OnArticlePropertyChanged`**: Only set `HasUnsavedChanges` if not suppressed:
   ```csharp
   if (dataProperties.Contains(e.PropertyName))
   {
       if (!_suppressHasUnsavedChanges)
       {
           IndexEditor.Shared.EditorState.HasUnsavedChanges = true;
       }
   }
   ```

## Files Changed
- `/home/justin/repos/urban-sniffle/src/index-editor/Views/EditorStateViewModel.cs`
  - Added `_suppressHasUnsavedChanges` flag (line 189)
  - Wrapped `IsSelected` updates with suppress flag (lines 131-141)
  - Wrapped `RefreshUIBindings()` call with suppress flag (lines 159-168)
  - Check suppress flag before setting `HasUnsavedChanges` (lines 444-449)

## Testing
To verify this fix:

1. Launch IndexEditor and load a magazine folder
2. Click on different articles in the article list
3. Don't make any edits - just browse through articles
4. Close the application (Ctrl+Q)
5. **Expected**: Application closes immediately without prompting to save
6. **Previous behavior**: Application would ask "Do you want to save changes?" even though nothing changed

To verify actual edits still work:
1. Launch IndexEditor and load a magazine folder
2. Select an article
3. Edit a field (e.g., change the title)
4. Close the application (Ctrl+Q)
5. **Expected**: Application prompts "Do you want to save changes?"

## Related Issues
This fix complements the previous fix in `UNSAVED_CHANGES_FIX.md` which filtered out UI-only properties from triggering the modified flag. That fix addressed properties like `ActiveSegment`, `LastModifiedSegment`, `WasAutoHighlighted`, and `IsSelected`. This fix addresses the scenario where legitimate data properties fire change events during UI refresh operations.

## Date
March 4, 2026

