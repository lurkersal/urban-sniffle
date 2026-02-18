# Page Controller Non-Existent Pages Fix ✅
## Problem
The PageController was allowing users to navigate to page numbers that don't exist on disk:
- Users could type any page number in the page input box
- The application would accept it even if no image file existed for that page
- This caused confusion and made it hard to work with the actual magazine content
## Solution
Modified the `Page` property setter in `PageControllerView` to enforce that pages must exist on disk when images are enabled.
### Behavior Changes
**Before:**
- User types page 999 → Shows page 999 even if no image exists
- No feedback to the user
- "Page not found" message shown but page number still set
**After:**
- User types page 999 (doesn't exist) → Automatically jumps to nearest existing page
- Shows toast notification: "Page 999 not found, showing nearest page 42"
- If no pages exist at all → Stays on current page, shows: "Page 999 not found"
## Implementation Details
### Updated Logic
```csharp
public int Page
{
    get => EditorState.CurrentPage;
    set
    {
        var desired = value;
        if (IndexEditor.Shared.EditorState.ShowImages)
        {
            var folder = EditorState.CurrentFolder;
            if (!string.IsNullOrWhiteSpace(folder))
            {
                // Check if the desired page exists
                var candidate = IndexEditor.Shared.ImageHelper.FindImagePath(folder, desired);
                if (candidate == null)
                {
                    // Page doesn't exist, find the nearest existing page
                    var nearest = IndexEditor.Shared.ImageHelper.FindNearestExistingPageBothDirections(folder, desired);
                    if (nearest.HasValue)
                    {
                        desired = nearest.Value;
                        // Show notification to user
                        ToastService.Show($"Page {value} not found, showing nearest page {nearest.Value}");
                    }
                    else
                    {
                        // No pages exist in the folder at all
                        ToastService.Show($"Page {value} not found");
                        return; // Keep current page
                    }
                }
            }
        }
        EditorState.CurrentPage = desired;
        // ...rest of the code
    }
}
```
### Key Features
1. **Validation**: Checks if page exists using `ImageHelper.FindImagePath()`
2. **Smart Navigation**: If page doesn't exist, finds nearest existing page in both directions
3. **User Feedback**: Shows toast notifications explaining what happened
4. **Graceful Degradation**: If no pages exist, stays on current page instead of crashing
### Edge Cases Handled
- ✅ Page exists → Navigate to it (no change in behavior)
- ✅ Page doesn't exist → Navigate to nearest existing page + show notification
- ✅ No pages exist in folder → Stay on current page + show "not found" message
- ✅ Images disabled (`--no-images`) → Allow any page number (existing behavior)
- ✅ No folder open → Allow any page number (existing behavior)
## Files Modified
**File:** `/src/index-editor/Views/PageControllerView.axaml.cs`
**Changes:**
1. Enhanced `Page` property setter to validate page existence
2. Added nearest page lookup when desired page doesn't exist
3. Added toast notifications for user feedback
4. Added early return if no valid pages found
## Testing
### Test Scenarios
1. **Navigate to existing page**
   - Type "42" → Goes to page 42 ✅
2. **Navigate to non-existent page**
   - Type "999" → Shows "Page 999 not found, showing nearest page 150" ✅
   - Jumps to page 150
3. **Navigate when no pages exist**
   - Type "10" in empty folder → Shows "Page 10 not found" ✅
   - Stays on current page
4. **Prev/Next buttons** (already working correctly)
   - Previous button → Finds nearest existing page backward ✅
   - Next button → Finds nearest existing page forward ✅
5. **Images disabled mode**
   - Type any page → Accepts it (doesn't enforce existence) ✅
## Impact
### Benefits
- ✅ **User Experience**: Clear feedback when trying to access non-existent pages
- ✅ **Data Integrity**: Can't accidentally work on non-existent pages
- ✅ **Efficiency**: Automatically jumps to nearest valid page
- ✅ **Consistency**: Same behavior as Prev/Next buttons (they already check for existence)
### No Breaking Changes
- Images disabled mode still allows free navigation
- No folder open still allows free navigation
- Existing keyboard shortcuts (Left/Right arrows) still work
- Prev/Next buttons unchanged
## Build Status
✅ **Build succeeded: 0 Errors, 72 Warnings (all pre-existing style warnings)**
## Related Code
- **ImageHelper.FindImagePath()**: Checks if page image exists on disk
- **ImageHelper.FindNearestExistingPageBothDirections()**: Finds closest existing page
- **ToastService.Show()**: User notifications
- **EditorState.CurrentPage**: Current page state
---
**Fix Date:** February 19, 2026  
**Issue:** PageController showing non-existent pages  
**Status:** ✅ RESOLVED  
**Build:** ✅ SUCCESS
