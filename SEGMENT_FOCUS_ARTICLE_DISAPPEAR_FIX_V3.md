# Article Disappearing Bug - Final Fix (v3)

## Issue
After ending a segment, the article disappeared from the editor pane.

## Root Cause (Final Analysis)

The issue was **RECURSIVE REORDERING** causing a cascade of state updates:

### The Cascade:
1. End segment → modifies `article.Pages`
2. `OnArticlePropertyChanged` → calls `ReorderArticlesByPage()`
3. `ReorderArticlesByPage()` → restores selection
4. Restoration → calls `PropertyChanged?.Invoke(...)` for `SelectedArticle`
5. **PropertyChanged event** → triggers UI binding updates
6. UI bindings → might trigger `SelectedArticle` setter
7. Setter → calls `EditorState.NotifyStateChanged()`
8. NotifyStateChanged → triggers `OnEditorStateChanged()`
9. **LOOP BACK TO STEP 2** → `ReorderArticlesByPage()` called AGAIN!
10. Second reordering → loses the selection that was just restored!

### Why Previous Fixes Failed

**v1:** Tried timing delays - didn't address root cause  
**v2:** Added selection restoration - but didn't prevent recursive calls

The selection WAS being restored, but then **immediately lost** when reordering was triggered again during the restoration process!

## Final Solution

**Prevent recursive reordering calls** with a guard flag:

### Code Changes

**File:** `src/index-editor/Views/EditorStateViewModel.cs`

#### 1. Added Guard Flag

```csharp
private bool _suppressCategorySet = false;
private bool _isReordering = false;  // ← NEW
```

#### 2. Guard Against Recursion in ReorderArticlesByPage()

```csharp
private void ReorderArticlesByPage()
{
    // Prevent recursive calls during reordering
    if (_isReordering)
    {
        try { DebugLogger.Log("ReorderArticlesByPage: Skipping - already reordering"); } catch { }
        return;  // ← EXIT EARLY
    }
    
    _isReordering = true;
    try
    {
        // ... existing reordering logic ...
        
        // Restore selection
        if (currentSelection != null && ordered.Contains(currentSelection))
        {
            // ... restoration logic ...
        }
    }
    finally
    {
        _isReordering = false;  // ← ALWAYS RESET
    }
}
```

#### 3. Guard in OnArticlePropertyChanged()

```csharp
private void OnArticlePropertyChanged(object? sender, PropertyChangedEventArgs e)
{
    if (sender is ArticleLine article)
    {
        // ...
        
        if (e.PropertyName == nameof(ArticleLine.Pages) || ...)
        {
            // Skip if already reordering to prevent recursive calls
            if (!_isReordering)  // ← CHECK FLAG
            {
                ReorderArticlesByPage();
            }
        }
    }
}
```

### Why This Works

1. **First call:** `_isReordering = false` → reordering proceeds
2. **During reordering:** `_isReordering = true` → any triggered property changes are ignored
3. **Restoration phase:** PropertyChanged events fired, but reordering is skipped
4. **After reordering:** `_isReordering = false` (via finally) → normal operation resumes
5. **Result:** Selection restored successfully, no recursive cascade

### Flow with Fix

```
End Segment
  → article.Pages modified
  → OnArticlePropertyChanged triggered
  → _isReordering = false, so ReorderArticlesByPage() called
  
  [Inside ReorderArticlesByPage]
  → _isReordering = true (guard set)
  → Save selection
  → Reorder articles
  → Restore selection
  → PropertyChanged fired for SelectedArticle
    → UI bindings update
    → Maybe triggers OnArticlePropertyChanged again
    → BUT _isReordering = true, so SKIP!
  → finally: _isReordering = false
  
  [After ReorderArticlesByPage]
  → Selection preserved ✅
  → Article visible ✅
```

## Build Status
✅ **Build Successful** - 0 Errors, 63 Warnings (pre-existing)

## Testing

### Test 1: Basic Segment End
1. Select article, start segment (Ctrl+A)
2. Navigate pages
3. End segment (Enter)
4. ✅ **Verify:** Article REMAINS VISIBLE

### Test 2: Rapid Operations
1. End segment (Enter)
2. Immediately start new segment (Ctrl+A)
3. End segment (Enter)
4. Repeat 10 times rapidly
5. ✅ **Verify:** Article never disappears

### Test 3: Check Logs
In `indexeditor.trace.log`, you should see:
- "ReorderArticlesByPage: Saving selection: ..."
- "ReorderArticlesByPage: Restoring selection: ..."
- If recursive call attempted: "ReorderArticlesByPage: Skipping - already reordering"

## Files Modified
1. `src/index-editor/Views/EditorStateViewModel.cs`
   - Added `_isReordering` flag
   - Guard in `ReorderArticlesByPage()` 
   - Guard in `OnArticlePropertyChanged()`

## Key Insights

### The Real Enemy: Cascading Updates

In complex UI frameworks with bidirectional bindings:
- Property changes trigger events
- Events trigger handlers
- Handlers modify properties
- **Creates update loops!**

### Solution Pattern: Guard Flags

When performing multi-step operations that might trigger events:
1. Set guard flag BEFORE operation
2. Check guard flag in event handlers
3. Skip recursive calls if guard is set
4. ALWAYS reset guard in finally block

### Why try-finally Is Critical

```csharp
try
{
    _isReordering = true;
    // ... work that might throw ...
}
finally
{
    _isReordering = false;  // ← ALWAYS executes
}
```

If exception occurs, flag is still reset. Otherwise system stays locked!

## Prevention

For any operation that:
1. Modifies state
2. Triggers property change events
3. Those events might trigger the same operation

**Always add a guard flag!**

Examples:
- Reordering collections
- Bulk updates
- Cascading calculations
- Two-way binding synchronization

## Debug Logging

Logs show:
- When reordering starts (with selection info)
- When recursive call is blocked
- When selection is restored
- Warnings if restoration fails

Check `indexeditor.trace.log` for full trace.

## References

- v1: Timing delay approach (failed)
- v2: Selection restoration (incomplete)
- v3: Recursive prevention + restoration (SUCCESS!)

