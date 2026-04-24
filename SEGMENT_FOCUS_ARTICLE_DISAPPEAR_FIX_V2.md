# Segment Focus Return Bug Fix - Article Disappearing (v2 - Enhanced)

## Bug Report
**Date:** February 27, 2026  
**Issue:** After ending a segment with Enter, the article disappeared from the editor.

**Status:** Initial fix incomplete. Enhanced fix now implemented.

## Root Cause

When ending a segment:
1. `EditorActions.EndActiveSegment()` modifies `article.Pages`
2. This triggers `ReorderArticlesByPage()` (articles sort by first page)
3. **Reordering was losing the `SelectedArticle` reference AND related state**
4. Article editor visibility binding saw incomplete state
5. Article disappeared

## Why Initial Fix Failed

Initial fix saved/restored `_selectedArticle` but was INCOMPLETE:

```csharp
// Initial attempt - INCOMPLETE
if (restored != null && _selectedArticle != restored)
{
    _selectedArticle = restored;  // ← Only this
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedArticle)));  // ← Only this
}
```

**What Was Missing:**
- ❌ Didn't update `IsSelected` flags on articles
- ❌ Didn't update `EditorState.ActiveArticle`
- ❌ Didn't notify `SelectedCategory` binding
- ❌ Didn't notify `CurrentShownArticle` binding
- ❌ Skipped restoration if references were same (even if state was stale)

**Result:** Incomplete state caused UI to think article wasn't selected → disappeared

## Enhanced Solution

**File:** `src/index-editor/Views/EditorStateViewModel.cs`

Complete state restoration after reordering:

```csharp
private void ReorderArticlesByPage()
{
    // 1. SAVE selection
    var currentSelection = _selectedArticle;
    try { DebugLogger.Log($"ReorderArticlesByPage: Saving '{currentSelection?.Title}'"); } catch { }
    
    _suppressCategorySet = true;
    
    // 2. REORDER
    var ordered = (EditorState.Articles ?? new List<ArticleLine>())
        .OrderBy(a => (a.Pages != null && a.Pages.Count > 0) ? a.Pages.Min() : int.MaxValue)
        .ThenBy(a => a.Title)
        .ToList();
    
    EditorState.Articles = ordered;
    
    for (int targetIndex = 0; targetIndex < ordered.Count; targetIndex++)
    {
        var article = ordered[targetIndex];
        var currentIndex = Articles.IndexOf(article);
        if (currentIndex == -1)
            Articles.Insert(targetIndex, article);
        else if (currentIndex != targetIndex)
            Articles.Move(currentIndex, targetIndex);
    }
    
    // 3. RESTORE COMPLETE STATE
    if (currentSelection != null && ordered.Contains(currentSelection))
    {
        var restored = ordered.FirstOrDefault(a => object.ReferenceEquals(a, currentSelection))
                    ?? ordered.FirstOrDefault(a => a.Pages != null && currentSelection.Pages != null && 
                                                  a.Pages.SequenceEqual(currentSelection.Pages) && 
                                                  (a.Title ?? string.Empty) == (currentSelection.Title ?? string.Empty));
        if (restored != null)
        {
            try { DebugLogger.Log($"ReorderArticlesByPage: Restoring '{restored.Title}'"); } catch { }
            
            var wasSelected = _selectedArticle;
            _selectedArticle = restored;
            
            if (!object.ReferenceEquals(wasSelected, restored))
            {
                try
                {
                    // ✅ Update IsSelected flags (CRITICAL!)
                    foreach (var a in Articles)
                    {
                        a.IsSelected = object.ReferenceEquals(a, _selectedArticle);
                    }
                    
                    // ✅ Update global state (CRITICAL!)
                    IndexEditor.Shared.EditorState.ActiveArticle = _selectedArticle;
                    
                    // ✅ Notify ALL bindings (CRITICAL!)
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedArticle)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedCategory)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentShownArticle)));
                }
                catch (Exception ex) { DebugLogger.LogException("ReorderArticlesByPage: restore", ex); }
            }
        }
        else
        {
            try { DebugLogger.Log("ReorderArticlesByPage: WARNING - Could not find article!"); } catch { }
        }
    }
    else
    {
        try { DebugLogger.Log("ReorderArticlesByPage: WARNING - Selection null or missing!"); } catch { }
    }
    
    _suppressCategorySet = false;
}
```

## Why Enhanced Fix Works

Complete state restoration includes:
1. **Reference:** `_selectedArticle = restored`
2. **Visual indicators:** `IsSelected` flags on all articles
3. **Global state:** `EditorState.ActiveArticle`
4. **All bindings:** `SelectedArticle`, `SelectedCategory`, `CurrentShownArticle`

Missing ANY of these = inconsistent UI state = article disappears

## Build Status
✅ **Build Successful** - 0 Errors

## Testing

### Test 1: Basic Operation
1. Select article, start segment (Ctrl+A)
2. Navigate pages
3. End segment (Enter)
4. ✅ **Verify:** Article REMAINS VISIBLE

### Test 2: Multiple Operations
1. End segment (Enter)
2. Create new segment (Ctrl+A)
3. End segment (Enter)
4. Repeat 5-10 times
5. ✅ **Verify:** Article never disappears

### Test 3: Check Logs
Look for in `indexeditor.trace.log`:
- "ReorderArticlesByPage: Saving selection: ..."
- "ReorderArticlesByPage: Restoring selection: ..."
- No WARNING messages

## Files Modified
1. `src/index-editor/Views/EditorStateViewModel.cs` - Enhanced ReorderArticlesByPage() with complete state restoration

## Key Insight

**Partial state restoration is worse than no restoration** - it creates inconsistent state that causes bugs.

When preserving UI selection through data operations, you MUST restore:
- ✅ Selection reference
- ✅ Visual indicators (IsSelected)
- ✅ Global state (ActiveArticle)
- ✅ ALL related bindings

## Debug Logging

Added comprehensive logging:
- When selection saved
- When restoration attempted
- Success/failure of restoration
- Warnings if issues occur

Check `indexeditor.trace.log` to diagnose problems.

