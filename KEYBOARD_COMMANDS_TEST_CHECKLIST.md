# Keyboard Commands Test Checklist

## Test Date: March 20, 2026
## Feature: Article Editor Keyboard Commands Fix

This checklist verifies that keyboard commands work correctly in the index-editor after the fix that allows normal text editing behavior in article editor fields.

---

## Prerequisites
1. Start index-editor: `dotnet run --project src/index-editor/IndexEditor.csproj`
2. Open a folder containing a magazine issue with articles
3. Select an article to populate the editor pane

---

## Test Group 1: Editor Focused - Normal Text Editing

**Setup**: Click on any text field in the article editor (Title, Model, Photographer, etc.)

### Test 1.1: Ctrl-A (Select All)
- [ ] **Action**: Press Ctrl-A in a text field with some text
- [ ] **Expected**: All text in the field is selected (NOT adding a segment)
- [ ] **Result**: ☐ Pass ☐ Fail
- [ ] **Notes**: _________________________________

### Test 1.2: Arrow Keys (Cursor Movement)
- [ ] **Action**: Press Left/Right arrow keys in a text field
- [ ] **Expected**: Cursor moves within the text field (NOT navigating pages)
- [ ] **Result**: ☐ Pass ☐ Fail
- [ ] **Notes**: _________________________________

### Test 1.3: Ctrl-C/V (Copy/Paste)
- [ ] **Action**: Select text, press Ctrl-C, then Ctrl-V
- [ ] **Expected**: Text is copied and pasted normally
- [ ] **Result**: ☐ Pass ☐ Fail
- [ ] **Notes**: _________________________________

### Test 1.4: Ctrl-X (Cut)
- [ ] **Action**: Select text, press Ctrl-X
- [ ] **Expected**: Text is cut to clipboard
- [ ] **Result**: ☐ Pass ☐ Fail
- [ ] **Notes**: _________________________________

### Test 1.5: Enter Key
- [ ] **Action**: Press Enter in a text field
- [ ] **Expected**: Normal form behavior (moves to next field or submits)
- [ ] **Result**: ☐ Pass ☐ Fail
- [ ] **Notes**: _________________________________

---

## Test Group 2: Editor Focused - Application Shortcuts Still Work

**Setup**: Text field in article editor has focus

### Test 2.1: Ctrl-S (Save)
- [ ] **Action**: Make a change to an article field, press Ctrl-S
- [ ] **Expected**: Index file is saved (check toast message or console)
- [ ] **Result**: ☐ Pass ☐ Fail
- [ ] **Notes**: _________________________________

### Test 2.2: Ctrl-O (Open Folder)
- [ ] **Action**: Press Ctrl-O while editor field has focus
- [ ] **Expected**: Folder picker dialog opens
- [ ] **Result**: ☐ Pass ☐ Fail
- [ ] **Notes**: _________________________________

### Test 2.3: ESC (Exit Editor Mode)
- [ ] **Action**: Press ESC while editing a field
- [ ] **Expected**: Focus moves away from editor field to article list
- [ ] **Result**: ☐ Pass ☐ Fail
- [ ] **Notes**: _________________________________

---

## Test Group 3: Editor NOT Focused - Application Shortcuts Work

**Setup**: Press ESC to exit editor mode, or click elsewhere (e.g., article list)

### Test 3.1: Ctrl-A (Add Segment)
- [ ] **Action**: Press Ctrl-A with no editor field focused
- [ ] **Expected**: New segment starts at current page
- [ ] **Result**: ☐ Pass ☐ Fail
- [ ] **Notes**: _________________________________

### Test 3.2: Left/Right Arrows (Page Navigation)
- [ ] **Action**: Press Left or Right arrow with no editor field focused
- [ ] **Expected**: Current page changes in page controller
- [ ] **Result**: ☐ Pass ☐ Fail
- [ ] **Notes**: _________________________________

### Test 3.3: Ctrl-N (New Article)
- [ ] **Action**: Press Ctrl-N with no editor field focused
- [ ] **Expected**: New article is created and title field gets focus
- [ ] **Result**: ☐ Pass ☐ Fail
- [ ] **Notes**: _________________________________

### Test 3.4: Ctrl-D (Delete Article)
- [ ] **Action**: Press Ctrl-D with an article selected
- [ ] **Expected**: Delete confirmation dialog appears
- [ ] **Result**: ☐ Pass ☐ Fail
- [ ] **Notes**: _________________________________

### Test 3.5: Ctrl-Up/Down (Navigate Articles)
- [ ] **Action**: Press Ctrl-Up or Ctrl-Down
- [ ] **Expected**: Selected article changes in article list
- [ ] **Result**: ☐ Pass ☐ Fail
- [ ] **Notes**: _________________________________

### Test 3.6: Ctrl-I (Index Overlay)
- [ ] **Action**: Press Ctrl-I
- [ ] **Expected**: JSON index file overlay opens
- [ ] **Result**: ☐ Pass ☐ Fail
- [ ] **Notes**: _________________________________

---

## Test Group 4: Edge Cases

### Test 4.1: Active Segment with Editor Focused
- [ ] **Action**: Start a segment (Ctrl-A), click in editor field, press Right arrow
- [ ] **Expected**: Cursor moves in text field (does NOT navigate to next page)
- [ ] **Result**: ☐ Pass ☐ Fail
- [ ] **Notes**: _________________________________

### Test 4.2: Multiple Fields
- [ ] **Action**: Tab between multiple editor fields, test Ctrl-A in each
- [ ] **Expected**: Ctrl-A selects all text in each field
- [ ] **Result**: ☐ Pass ☐ Fail
- [ ] **Notes**: _________________________________

### Test 4.3: Category Dropdown
- [ ] **Action**: Click category dropdown, use Up/Down arrows
- [ ] **Expected**: Arrows navigate dropdown items (NOT article list)
- [ ] **Result**: ☐ Pass ☐ Fail
- [ ] **Notes**: _________________________________

### Test 4.4: ESC Cancels Active Segment
- [ ] **Action**: Start segment (Ctrl-A), press ESC without focusing editor
- [ ] **Expected**: Segment is cancelled
- [ ] **Result**: ☐ Pass ☐ Fail
- [ ] **Notes**: _________________________________

### Test 4.5: ESC with Editor and Segment
- [ ] **Action**: Start segment, click editor field, press ESC
- [ ] **Expected**: Focus exits editor (segment remains active)
- [ ] **Result**: ☐ Pass ☐ Fail
- [ ] **Notes**: _________________________________

---

## Test Group 5: Regression Tests

### Test 5.1: Delete Key in Text Field
- [ ] **Action**: Select text in editor field, press Delete
- [ ] **Expected**: Text is deleted (NOT the article)
- [ ] **Result**: ☐ Pass ☐ Fail
- [ ] **Notes**: _________________________________

### Test 5.2: Ctrl-Enter (Not Implemented for Editor)
- [ ] **Action**: Press Ctrl-Enter in editor field
- [ ] **Expected**: Normal behavior (should not add special functionality)
- [ ] **Result**: ☐ Pass ☐ Fail
- [ ] **Notes**: _________________________________

### Test 5.3: F11 Fullscreen
- [ ] **Action**: Press F11 (editor NOT focused)
- [ ] **Expected**: Application toggles fullscreen mode
- [ ] **Result**: ☐ Pass ☐ Fail
- [ ] **Notes**: _________________________________

---

## Summary

**Total Tests**: 23  
**Tests Passed**: _____  
**Tests Failed**: _____  
**Pass Rate**: _____%  

## Issues Found
1. _________________________________________________________________
2. _________________________________________________________________
3. _________________________________________________________________

## Additional Notes
_____________________________________________________________________
_____________________________________________________________________
_____________________________________________________________________

---

## Sign-off
- [ ] All critical tests passed (Groups 1, 2, 3)
- [ ] No regressions detected (Group 5)
- [ ] Edge cases handled correctly (Group 4)
- [ ] Ready for production use

**Tester**: _________________  
**Date**: _________________  
**Signature**: _________________

