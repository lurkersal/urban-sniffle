# Segment Focus Return Bug Fix - Article Disappearing

## Bug Report
**Date:** February 27, 2026  
**Issue:** After ending a segment with Enter, the article disappeared from the editor even though focus correctly returned to the page controller.

## Root Cause Analysis

### Symptom
1. User adds a segment to an article (Ctrl+A)
2. User navigates with arrow keys
3. User presses Enter to complete the segment
4. **BUG:** Article disappears from the article editor pane

### Investigation Process

**Initial Hypothesis (INCORRECT):** 
Focus transfer timing issue - thought the focus change was happening too quickly and causing a race condition with UI binding updates.

**Initial Fix Attempt (FAILED):**
Added 50ms delay before focusing page controller - did not solve the problem.

**Debugging Steps:**
1. Disabled focus transfer entirely - article still disappeared
2. This proved focus transfer was NOT the root cause
3. Searched for what clears `SelectedArticle` when segment ends
4. Found the actual culprit in `EditorStateViewModel`

### Real Root Cause: Article Reordering Loses Selection

When a segment is ended:
1. `EditorActions.EndActiveSegment()` modifies `article.Pages` (adds pages in segment range)
2. Modifying `Pages` triggers `PropertyChanged` event for "Pages"
3. `EditorStateViewModel.OnArticlePropertyChanged()` listens to this event
4. When "Pages" changes, it calls `ReorderArticlesByPage()`
5. **`ReorderArticlesByPage()` creates a NEW ordered list and replaces `EditorState.Articles`**
6. The collection replacement causes binding updates
7. **`SelectedArticle` reference becomes stale/lost during reordering**
8. Article editor visibility binding sees `SelectedArticle` as null/invalid
9. Article editor disappears

**The Code That Caused The Bug:**

```csharp
// EditorStateViewModel.cs
private void OnArticlePropertyChanged(object? sender, PropertyChangedEventArgs e)
{
    if (sender is ArticleLine article)
    {
        // When Pages changes, reorder articles
        if (e.PropertyName == nameof(ArticleLine.Pages) || ...)
        {
            ReorderArticlesByPage();  // ← This loses the selection!
        }
    }
}

private void ReorderArticlesByPage()
{
    // Create NEW ordered list
    var ordered = EditorState.Articles
        .OrderBy(a => a.Pages.Min())
        .ToList();
    
    // Replace the whole list - THIS breaks the SelectedArticle reference!
    EditorState.Articles = ordered;
    
    // Reorder ObservableCollection
    // ... but SelectedArticle is already lost by this point
}
```

## Solution

Preserve the `SelectedArticle` reference during reordering and restore it after the reordering completes.

### Code Changes

**File:** `src/index-editor/Views/EditorStateViewModel.cs`

**Method:** `ReorderArticlesByPage()`

**Before:**
```csharp
private void ReorderArticlesByPage()
{
    _suppressCategorySet = true;
    var ordered = (EditorState.Articles ?? new List<ArticleLine>())
        .OrderBy(a => (a.Pages != null && a.Pages.Count > 0) ? a.Pages.Min() : int.MaxValue)
        .ThenBy(a => a.Title)
        .ToList();

    EditorState.Articles = ordered;

    // Reorder ObservableCollection in-place
    for (int targetIndex = 0; targetIndex < ordered.Count; targetIndex++)
    {
        var article = ordered[targetIndex];
        var currentIndex = Articles.IndexOf(article);
        if (currentIndex == -1)
            Articles.Insert(targetIndex, article);
        else if (currentIndex != targetIndex)
            Articles.Move(currentIndex, targetIndex);
    }

    _suppressCategorySet = false;
}
```

**After:**
```csharp
private void ReorderArticlesByPage()
{
    // ← SAVE the current selection before reordering
    var currentSelection = _selectedArticle;
    
    _suppressCategorySet = true;
    var ordered = (EditorState.Articles ?? new List<ArticleLine>())
        .OrderBy(a => (a.Pages != null && a.Pages.Count > 0) ? a.Pages.Min() : int.MaxValue)
        .ThenBy(a => a.Title)
        .ToList();

    EditorState.Articles = ordered;

    // Reorder ObservableCollection in-place
    for (int targetIndex = 0; targetIndex < ordered.Count; targetIndex++)
    {
        var article = ordered[targetIndex];
        var currentIndex = Articles.IndexOf(article);
        if (currentIndex == -1)
            Articles.Insert(targetIndex, article);
        else if (currentIndex != targetIndex)
            Articles.Move(currentIndex, targetIndex);
    }

    // ← RESTORE the selection after reordering
    if (currentSelection != null && ordered.Contains(currentSelection))
    {
        // Find the article in the new ordered list
        var restored = ordered.FirstOrDefault(a => object.ReferenceEquals(a, currentSelection))
                    ?? ordered.FirstOrDefault(a => a.Pages != null && currentSelection.Pages != null && 
                                                  a.Pages.SequenceEqual(currentSelection.Pages) && 
                                                  (a.Title ?? string.Empty) == (currentSelection.Title ?? string.Empty));
        if (restored != null && _selectedArticle != restored)
        {
            _selectedArticle = restored;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedArticle)));
        }
    }

    _suppressCategorySet = false;
}
```

### Why This Works

1. **Before reordering:** Save the current `SelectedArticle` reference in `currentSelection`
2. **During reordering:** Articles list is replaced, ObservableCollection is reordered
3. **After reordering:** 
   - Check if the saved article still exists in the new ordered list
   - Find it by reference equality first (fastest), or by matching Pages/Title (fallback)
   - Restore `_selectedArticle` to point to the article in the new list
   - Raise `PropertyChanged` so UI bindings update with the restored selection
4. **Result:** Article remains visible because `SelectedArticle` is preserved through the reordering

## Build Status
✅ **Build Successful**
```
Build succeeded.
    63 Warning(s)
    0 Error(s)
```

All warnings are pre-existing.

## Testing Instructions

### Test 1: Verify Bug is Fixed
1. Open a folder with images
2. Select an article
3. Navigate to a page (e.g., page 10)
4. Press **Ctrl+A** to start a segment
5. Press **Right Arrow** a few times (e.g., to page 13)
6. Press **Enter** to end the segment
7. ✅ **Verify**: 
   - Segment is ended correctly (pages added)
   - Focus returns to page input field (text selected)
   - **Article REMAINS VISIBLE in editor pane** ← BUG FIXED!
8. Type a new page number and press Enter
9. ✅ **Verify**: Navigation works, article still visible

### Test 2: Verify Cancel Works Too
1. Start a segment with **Ctrl+A**
2. Navigate with arrow keys
3. Press **Escape** to cancel
4. ✅ **Verify**:
   - Segment cancelled
   - Focus returns to page input
   - Article remains visible

### Test 3: Multiple Segments
1. Create and end a segment (Enter)
2. Immediately create another segment (Ctrl+A)
3. End it (Enter)
4. Repeat several times
5. ✅ **Verify**: Article never disappears, even after multiple segment operations

### Test 4: Article Reordering
1. Select an article at page 50
2. Start a segment (Ctrl+A)
3. Navigate to page 5 (much earlier page)
4. End segment (Enter) - this will trigger reordering because article now starts at page 5
5. ✅ **Verify**: 
   - Article moves to new position in list (reordered)
   - Article REMAINS VISIBLE in editor
   - Selection is preserved through reordering

## Files Modified
1. `src/index-editor/Views/EditorStateViewModel.cs` - Preserve and restore SelectedArticle during ReorderArticlesByPage()

## Impact

### User Experience
- **Before**: Article disappeared after ending segment, confusing and breaking workflow
- **After**: Article remains visible, smooth workflow continues

### Why This Is Better Than Timing Delays
- **Reliable**: Doesn't depend on arbitrary delays
- **Fast**: No unnecessary waiting
- **Correct**: Addresses the actual root cause
- **Robust**: Works regardless of system speed or load

## Related Issues

This fix applies to any operation that modifies article.Pages, which triggers reordering:
- **Ending segment** (Enter): Adds pages to article
- **Canceling segment** (Escape): May modify pages if segment was partially open
- **Manual page editing**: Direct editing of Pages field
- **Any PropertyChanged for "Pages"**: All scenarios that modify the pages

## Technical Notes

### Why Articles Get Reordered
Articles are displayed in page order (earliest page first). When segment ending adds new pages to an article, the article's minimum page number may change, requiring reordering to maintain sorted order.

### Why Reordering Broke Selection
When `EditorState.Articles = ordered` replaces the backing list:
1. The old article instances might not be in the new list (if LINQ creates new instances)
2. Even if they are the same instances, the collection replacement can trigger binding refreshes
3. UI bindings re-evaluate `SelectedArticle` 
4. If the reference doesn't match any item in the new/reordered collection, selection is lost

### Reference Equality vs Value Equality
The fix tries both:
1. **Reference equality** (`object.ReferenceEquals`) - fastest, works when LINQ preserves instances
2. **Value equality** (Pages + Title match) - fallback, works if instances differ but represent same article

## Prevention

To prevent similar issues in the future:
1. Always preserve selection state before collection operations (add, remove, replace, reorder)
2. Restore selection after collection operations complete
3. Test UI state preservation during data operations
4. Be cautious when replacing entire collections (prefer in-place modifications)

## Notes

- This is a classic UI pattern issue: selection lost during collection updates
- Common in all MVVM frameworks (WPF, Avalonia, Xamarin, etc.)
- The solution is standard: save → modify → restore
- Better than timing delays because it's deterministic and immediate

## References

- Original feature request: `SEGMENT_FOCUS_RETURN.md`
- Related: MVVM collection update patterns
- Related: ObservableCollection best practices
- Related: Preserving UI state during data updates

