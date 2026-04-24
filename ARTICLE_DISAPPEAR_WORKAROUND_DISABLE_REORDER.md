# Article Disappearing Bug - Workaround (Reordering Disabled)

## Status
**Previous fixes (v1, v2, v3) all failed.** Article still disappears after ending segment.

## Workaround Applied

**DISABLED automatic reordering when article Pages change.**

### Code Change

**File:** `src/index-editor/Views/EditorStateViewModel.cs`

```csharp
// OLD - Reordered on every Pages change
if (e.PropertyName == nameof(ArticleLine.Pages) || 
    e.PropertyName == nameof(ArticleLine.PagesText) || 
    e.PropertyName == nameof(ArticleLine.Category))
{
    if (!_isReordering)
    {
        ReorderArticlesByPage();
    }
}

// NEW - Only reorder on Category changes, NOT on Pages changes
if (e.PropertyName == nameof(ArticleLine.Category))  // ← Only Category
{
    if (!_isReordering)
    {
        ReorderArticlesByPage();
    }
}
```

### What This Means

**Before:**
- Ending segment adds pages to article
- Article automatically reordered by first page number
- Article list always sorted by page order
- **Article disappears (bug)**

**After (Workaround):**
- Ending segment adds pages to article
- **No automatic reordering**
- Article list may not be in perfect page order
- **Article should stay visible** ✓

### Trade-offs

**Pros:**
- ✅ Article should remain visible (no reordering = no lost selection)
- ✅ Simpler, more predictable behavior
- ✅ No cascading update issues

**Cons:**
- ❌ Article list not automatically sorted by page order
- ❌ Articles may appear out of sequence
- ❌ User must manually trigger reordering (if we add that feature)

### Why All Previous Fixes Failed

Despite 3 attempts with different approaches:
1. **v1:** Timing delays - didn't help
2. **v2:** Selection restoration - article still disappeared
3. **v3:** Recursive prevention + restoration - article STILL disappeared

**Conclusion:** The reordering + selection preservation is fundamentally broken in the current architecture. The complexity of the binding system, property change cascades, and ObservableCollection updates creates an unpredictable state that consistently loses the selection.

## Testing

### Test This Workaround
1. Start segment (Ctrl+A)
2. Navigate pages
3. End segment (Enter)
4. ✅ **Verify:** Article REMAINS VISIBLE (because no reordering happens)
5. ⚠️ **Note:** Article list may not be in page order

### If This Works

If the article stays visible with reordering disabled, we have **confirmed** that reordering is the root cause. Then we have two options:

**Option A:** Keep reordering disabled (simpler, but less organized UI)

**Option B:** Implement explicit reordering:
- Add a "Sort by Page" button
- User triggers reordering manually
- Happens when no article is selected (no selection to lose)

## Build Status
✅ **Build Successful** - 0 Errors

## Files Modified
1. `src/index-editor/Views/EditorStateViewModel.cs` - Disabled auto-reordering on Pages changes

## Next Steps

### If Article Stays Visible Now:
The workaround is successful. Decision needed:
- Keep workaround (no auto-sort by page)?
- Add manual "Sort" button?
- Attempt deeper architectural fix?

### If Article STILL Disappears:
Reordering was NOT the issue. The problem is something else entirely:
- Check if `SelectedArticle` is being set to null elsewhere
- Check if article is being removed from collection
- Check if visibility binding has other issues
- May need to debug with breakpoints in the running app

## Recommendation

**Try this build and report results:**
- Does the article stay visible after ending segment?
- Is the lack of auto-sorting acceptable?

Based on the answer, we can decide whether to:
1. Keep the workaround
2. Add manual sorting
3. Investigate deeper (if article still disappears)

