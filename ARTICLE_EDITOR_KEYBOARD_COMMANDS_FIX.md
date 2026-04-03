# Article Editor Keyboard Commands Fix

## Summary
Fixed keyboard command handling in the article editor to allow normal text editing behavior. Now only Ctrl-S (save) and Ctrl-O (open) are intercepted when the editor is focused, while all other keyboard commands work normally for text editing. Users must press ESC to exit editor mode before other application shortcuts take effect.

## Changes Made

### 1. SegmentKeyboardHandler.cs
**Location**: `/home/justin/repos/urban-sniffle/src/index-editor/Services/KeyboardHandlers/SegmentKeyboardHandler.cs`

**Change**: Moved the editor focus check to the beginning of `TryHandle()` method (after ESC handling) so that when editor is focused, only ESC is handled and all other keys pass through.

**Before**:
- Ctrl+A was always intercepted (even when editor focused) to add a segment
- Ctrl+Enter was always intercepted (even when editor focused)
- Editor focus was only checked for plain Enter key

**After**:
- ESC is always handled (to exit editor mode or cancel segment)
- When editor is focused, all other shortcuts (including Ctrl+A, Ctrl+Enter) return false, allowing normal text editing behavior
- Ctrl+A now works as "Select All" in text boxes
- Ctrl+C/V work for copy/paste
- Ctrl+Enter only works when editor is NOT focused

### 2. NavigationKeyboardHandler.cs
**Location**: `/home/justin/repos/urban-sniffle/src/index-editor/Services/KeyboardHandlers/NavigationKeyboardHandler.cs`

**Change**: Removed redundant editor focus check from `HandleRightArrow()` method.

**Before**:
- Had special logic to allow right arrow navigation even when editor focused if there's an active segment

**After**:
- Consistent behavior: when editor is focused, arrow keys always work for text editing (cursor movement)
- No special case for active segment - user must press ESC first to navigate pages

### 3. MainWindow.axaml.cs
**Location**: `/home/justin/repos/urban-sniffle/src/index-editor/MainWindow.axaml.cs`

**Change**: Fixed EditorState fallback in `HandleDeleteKey()` method.

**Before**:
- Tried to use `EditorState.Instance` which is private

**After**:
- Creates new `EditorStateService()` instance as fallback

## Behavior Summary

### When Article Editor is Focused
**Intercepted shortcuts (always work)**:
- **ESC**: Exit editor mode, cancel segment, or move focus to article list
- **Ctrl-S**: Save index file
- **Ctrl-O**: Open folder

**Normal text editing shortcuts (now work correctly)**:
- **Ctrl-A**: Select all text in current field
- **Ctrl-C**: Copy selected text
- **Ctrl-V**: Paste text
- **Ctrl-X**: Cut selected text
- **Left/Right Arrow**: Move cursor within text field
- **Up/Down Arrow**: Navigate in dropdown menus or move cursor
- **Enter**: Submit form or move to next field (normal behavior)
- **Ctrl-Enter**: Works as normal in text fields

### When Article Editor is NOT Focused
All application shortcuts work normally:
- **Ctrl-A**: Add segment at current page
- **Ctrl-N**: Create new article
- **Ctrl-D**: Delete selected article
- **Ctrl-I**: Toggle index file overlay
- **Ctrl-B**: Check babepedia for model
- **Ctrl-Up/Down**: Navigate articles
- **Left/Right Arrow**: Navigate pages
- **Enter**: End active segment or focus editor
- **ESC**: Cancel active segment
- **F11**: Toggle fullscreen
- **Delete**: Delete selected article

## User Workflow
1. Click on a field in the article editor to start editing
2. All keyboard shortcuts work normally for text editing (Ctrl-A selects all, arrows move cursor, etc.)
3. Only Ctrl-S (save) and Ctrl-O (open) are intercepted by the application
4. Press **ESC** to exit editor mode and return focus to the main window
5. Now all application shortcuts (Ctrl-A for segment, arrows for navigation, etc.) work again

## Testing Checklist
- [ ] Text fields allow Ctrl-A for "Select All"
- [ ] Arrow keys move cursor in text fields when editor is focused
- [ ] Ctrl-S saves index file even when editor is focused
- [ ] Ctrl-O opens folder dialog even when editor is focused
- [ ] Ctrl-A adds segment only when editor is NOT focused
- [ ] ESC exits editor mode and returns focus to article list
- [ ] ESC cancels active segment when present
- [ ] Normal copy/paste (Ctrl-C/V) works in text fields
- [ ] Enter key submits form normally when editor is focused
- [ ] Left/Right arrows navigate pages only when editor is NOT focused
- [ ] Up/Down arrows navigate article list only when editor is NOT focused

## Implementation Notes
- The `IsArticleEditorFocused` flag is set by `ArticleEditor.axaml.cs` on GotFocus/LostFocus events
- All keyboard handlers check this flag early in their `TryHandle()` method
- FileKeyboardHandler explicitly allows Ctrl-S and Ctrl-O even when editor is focused
- Priority order: Segment (100) > Article (90) > File (80) > Navigation (50)

## Date
March 20, 2026

