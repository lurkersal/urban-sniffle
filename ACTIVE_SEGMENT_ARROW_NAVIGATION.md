# Active Segment Arrow Key Navigation Fix

## Summary
When an active segment exists, the left and right arrow keys now change the current page even when editable fields in the article editor have keyboard focus. This allows seamless page navigation while editing segment details.

## Problem
Previously, when editing article fields (title, category, etc.), the left and right arrow keys would only work within the focused text field and wouldn't change the current page. This made it difficult to navigate pages while building up a segment, requiring users to click away from the editor to navigate.

## Solution
Modified the arrow key handlers in `MainWindow.axaml.cs` to check if an active segment exists. If a segment is active and being edited, the arrow keys will change pages even if the article editor has focus.

## Changes Made

### File: `src/index-editor/MainWindow.axaml.cs`

**Modified:** Left arrow key handler (around line 1248)
- Added check for active segment existence
- Arrow key now changes page if: `hasActiveSegment == true` OR `IsArticleEditorFocused == false`
- Previously would only change page if: `IsArticleEditorFocused == false`

**Modified:** Right arrow key handler (around line 1268)
- Added same check for active segment existence
- Arrow key now changes page if: `hasActiveSegment == true` OR `IsArticleEditorFocused == false`
- Previously would only change page if: `IsArticleEditorFocused == false`

## Implementation Details

The logic checks two conditions:
```csharp
var hasActiveSegment = IndexEditor.Shared.EditorState.ActiveSegment != null 
                    && IndexEditor.Shared.EditorState.ActiveSegment.IsActive;
if (IndexEditor.Shared.EditorState.IsArticleEditorFocused && !hasActiveSegment) return;
```

This means:
- **Without active segment:** Arrow keys are blocked when editor has focus (normal text editing behavior)
- **With active segment:** Arrow keys always navigate pages, even from editor fields

## User Experience

**Before:**
1. User presses Ctrl+A to start a segment
2. User starts typing article title or other fields
3. User tries to press → to advance to next page
4. **Nothing happens** - arrow key only moves cursor in text field
5. User must click away from editor to navigate pages

**After:**
1. User presses Ctrl+A to start a segment
2. User starts typing article title or other fields
3. User presses → to advance to next page
4. **Page advances** while keeping editor focused
5. User can seamlessly type and navigate without losing focus

## Testing

Build Status: ✅ Success (no errors)
- Compiled successfully with no new warnings or errors
- Only pre-existing deprecation warnings remain

## Notes

- This change only affects behavior when an active segment exists
- Normal text editing behavior (without active segment) is unchanged
- The change makes multi-page segment creation much more fluid
- Users can now keep their hands on the keyboard during the entire segment creation workflow

