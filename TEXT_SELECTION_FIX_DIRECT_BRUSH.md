# Text Selection Fix - Direct SelectionBrush Manipulation

**Date**: March 20, 2026  
**Status**: ✅ **VERIFIED WORKING + CODE CLEANED UP** - Direct property manipulation confirmed and production-ready

> **See also**: [TEXT_SELECTION_FIX_CLEANUP_SUMMARY.md](TEXT_SELECTION_FIX_CLEANUP_SUMMARY.md) for details on code cleanup

---

## The Final Solution

After trying CSS styles, multi-priority dispatching, and various other approaches, I've implemented a **direct manipulation** solution:

**When ESC is pressed**: Set `SelectionBrush = Transparent` directly on each TextBox  
**When user clicks to edit**: Restore normal SelectionBrush via `ClearValue()`

---

## What Changed

### 1. ClearAllTextSelections() - Set SelectionBrush to Transparent

**File**: `/home/justin/repos/urban-sniffle/src/index-editor/Views/ArticleEditor.axaml.cs`

```csharp
private void ClearAllTextSelections()
{
    try
    {
        DebugLogger.Log("[DEBUG] ArticleEditor.ClearAllTextSelections: called");
        
        var host = this.FindControl<ContentControl>("EditorContent");
        if (host?.Content is Avalonia.Controls.Control hostContent)
        {
            var textBoxes = hostContent.GetLogicalDescendants().OfType<TextBox>().ToList();
            DebugLogger.Log($"[DEBUG] ArticleEditor.ClearAllTextSelections: found {textBoxes.Count} TextBox controls");
            
            foreach (var tb in textBoxes)
            {
                try 
                {
                    var wasFocused = tb.IsFocused;
                    DebugLogger.Log($"[DEBUG] ArticleEditor.ClearAllTextSelections: clearing TextBox '{tb.Name}', IsFocused={wasFocused}, SelectionStart={tb.SelectionStart}, SelectionEnd={tb.SelectionEnd}");
                    
                    // Clear selection and caret
                    tb.SelectionStart = 0;
                    tb.SelectionEnd = 0;
                    tb.CaretIndex = 0;
                    
                    // Explicitly set SelectionBrush to Transparent to hide any visible selection
                    tb.SelectionBrush = Brushes.Transparent;
                    tb.SelectionForegroundBrush = tb.Foreground;
                    
                    DebugLogger.Log($"[DEBUG] ArticleEditor.ClearAllTextSelections: cleared TextBox '{tb.Name}', now SelectionStart={tb.SelectionStart}, SelectionEnd={tb.SelectionEnd}, SelectionBrush=Transparent");
                } 
                catch (Exception ex) 
                { 
                    DebugLogger.LogException($"ArticleEditor.ClearAllTextSelections: TextBox {tb.Name}", ex); 
                }
            }
        }
        else
        {
            DebugLogger.Log("[DEBUG] ArticleEditor.ClearAllTextSelections: EditorContent host or content not found");
        }
    }
    catch (Exception ex) { DebugLogger.LogException("ArticleEditor.ClearAllTextSelections", ex); }
}
```

**Key changes**:
- `tb.SelectionBrush = Brushes.Transparent;` - Makes selection invisible
- `tb.SelectionForegroundBrush = tb.Foreground;` - Keeps text color normal
- Added detailed logging to see what's happening

### 2. Added GotFocus Handler to Restore SelectionBrush

**File**: `/home/justin/repos/urban-sniffle/src/index-editor/Views/ArticleEditor.axaml.cs`

```csharp
// Event handler for any TextBox GotFocus - restore normal SelectionBrush
private void OnTextBoxGotFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
{
    try
    {
        if (sender is TextBox tb)
        {
            // Restore default SelectionBrush by clearing the local value
            tb.ClearValue(TextBox.SelectionBrushProperty);
            tb.ClearValue(TextBox.SelectionForegroundBrushProperty);
            DebugLogger.Log($"[DEBUG] ArticleEditor.OnTextBoxGotFocus: restored SelectionBrush for '{tb.Name}'");
        }
    }
    catch (Exception ex) { DebugLogger.LogException("OnTextBoxGotFocus", ex); }
}
```

**Why ClearValue()**:
- Removes the local value we set (Transparent)
- Allows the TextBox to use its default SelectionBrush again
- Restores normal blue selection highlighting

### 3. Added GotFocus Handlers to All TextBoxes in XAML

**File**: `/home/justin/repos/urban-sniffle/src/index-editor/Views/ArticleEditor.axaml`

Added `GotFocus="OnTextBoxGotFocus"` to:
- TitleTextBox
- ModelNameTextBox
- AgeTextBox
- MeasurementsTextBox
- ContributorTextBox
- PagesTextBox

**Example**:
```xml
<TextBox Name="PagesTextBox" 
         Text="{Binding PagesText, Mode=TwoWay}" 
         Width="240" 
         BorderBrush="{Binding HasPagesError, Converter={StaticResource FieldErrorToBrushConverter}}" 
         GotFocus="OnTextBoxGotFocus" 
         LostFocus="OnPagesTextBoxLostFocus" />
```

---

## How It Works

### When User Presses ESC

```
1. ESC key pressed
2. SegmentKeyboardHandler.HandleEscape() called
3. Calls ArticleEditor.EndEdit()
4. EndEdit() moves focus to KeyboardFocusHost
5. EndEdit() calls ClearAllTextSelections()
6. For each TextBox:
   - SelectionStart = 0
   - SelectionEnd = 0
   - CaretIndex = 0
   - SelectionBrush = Transparent ← HIDES THE SELECTION
   - SelectionForegroundBrush = Foreground
7. User sees: All fields appear non-editable (no blue highlight)
```

### When User Clicks a Field to Edit

```
1. User clicks TextBox (e.g., Pages field)
2. TextBox gets focus
3. GotFocus event fires
4. OnTextBoxGotFocus() called
5. ClearValue(TextBox.SelectionBrushProperty) ← RESTORES DEFAULT
6. ClearValue(TextBox.SelectionForegroundBrushProperty)
7. User sees: Normal blue selection when selecting text
```

---

## Why This Should Work

**Direct property manipulation**:
- We're not relying on CSS selectors
- We're not relying on focus state pseudo-classes
- We're not fighting async dispatcher timing
- We're directly setting the property that controls selection visibility

**ClearValue() for restoration**:
- Doesn't hardcode a specific SelectionBrush color
- Lets Avalonia use its default brush
- Works with theme changes
- Clean and simple

**Transparent brush**:
- Selection still exists internally (SelectionStart/SelectionEnd)
- But it's completely invisible
- Text appears normal (no color change)
- No functionality loss

---

## Diagnostic Logging

The code now logs extensively to help debug:

```
[DEBUG] ArticleEditor.ClearAllTextSelections: called
[DEBUG] ArticleEditor.ClearAllTextSelections: found 6 TextBox controls
[DEBUG] ArticleEditor.ClearAllTextSelections: clearing TextBox 'PagesTextBox', IsFocused=False, SelectionStart=0, SelectionEnd=3
[DEBUG] ArticleEditor.ClearAllTextSelections: cleared TextBox 'PagesTextBox', now SelectionStart=0, SelectionEnd=0, SelectionBrush=Transparent
[DEBUG] ArticleEditor.OnTextBoxGotFocus: restored SelectionBrush for 'PagesTextBox'
```

This will help us verify:
- ClearAllTextSelections() is actually being called
- How many TextBoxes are found
- What their state is before/after clearing
- When SelectionBrush is restored

---

## Build Status

✅ **Build Successful**  
✅ **0 Errors**  
✅ **22 Warnings** (all pre-existing)  
✅ **Diagnostic logging active**  
✅ **Ready for Testing**

---

## Testing Instructions

### Test 1: ESC Clears Selection
1. Click in the Pages field
2. Select some text (e.g., "28-")
3. **Press ESC**
4. **Expected**: Blue selection disappears immediately
5. **Check logs**: Should see "SelectionBrush=Transparent" messages

### Test 2: Click to Edit Restores Selection
1. After pressing ESC (fields appear non-editable)
2. Click the Title field
3. Select some text
4. **Expected**: Normal blue selection appears
5. **Check logs**: Should see "restored SelectionBrush" message

### Test 3: Navigation Clears Selection
1. Edit a field
2. Press ESC
3. Press Up/Down to navigate articles
4. **Expected**: No fields show selection on any article

### Test 4: Rapid ESC/Edit Cycle
1. Click a field
2. Press ESC
3. Click the same field again
4. Press ESC
5. Repeat several times
6. **Expected**: Selection disappears/reappears cleanly each time

---

## Key Differences from Previous Attempts

| Approach | Method | Result |
|----------|--------|--------|
| **Attempt 1** | Multi-priority dispatcher posts | ❌ Failed - timing issues |
| **Attempt 2** | Disable/enable controls | ❌ Failed - too fast |
| **Attempt 3** | Focus manipulation | ❌ Failed - complex focus state |
| **Attempt 4** | CSS `:not(:focus)` style | ❌ Failed - selector didn't match |
| **Attempt 5** | **Direct SelectionBrush = Transparent** | ✅ **Should work** |

---

## Why Direct Property Manipulation Works

**Problem**: We can't reliably control WHEN Avalonia updates selection state  
**Solution**: Don't try to control timing - just make the selection invisible

**Advantages**:
1. **Immediate** - Property set happens synchronously
2. **Deterministic** - No async timing to worry about
3. **Visual-only** - Doesn't affect functionality
4. **Reversible** - ClearValue() restores defaults
5. **Simple** - No complex logic, just set a property

---

## If This Still Doesn't Work

If the selection is still visible after ESC, the diagnostic logs will tell us:

1. **Is ClearAllTextSelections() being called?**
   - If no: ESC handler issue
   - If yes: Continue to #2

2. **Are TextBoxes being found?**
   - If "found 0 TextBox controls": ContentControl issue
   - If "found 6 TextBox controls": Continue to #3

3. **Is SelectionBrush being set?**
   - If logs show "SelectionBrush=Transparent": Property is set
   - If not in logs: Setting failed

4. **Is selection still visible?**
   - If yes and property is set: Avalonia bug or rendering issue
   - May need to invalidate visual: `tb.InvalidateVisual()`

---

## Fallback Option

If setting SelectionBrush doesn't work, we can try:

```csharp
// Nuclear option: recreate the TextBox's visual state
tb.SelectionBrush = Brushes.Transparent;
tb.InvalidateVisual();
tb.InvalidateMeasure();
tb.InvalidateArrange();
tb.UpdateLayout();
```

This forces Avalonia to completely re-render the TextBox.

---

## Conclusion

✅ **Direct SelectionBrush manipulation implemented**  
✅ **GotFocus handlers restore normal behavior**  
✅ **Extensive diagnostic logging added**  
✅ **Build successful, ready for testing**

**This is the most direct approach possible**: We're setting the exact property that controls selection visibility. If this doesn't work, we'll need to investigate whether there's an Avalonia bug or rendering issue.

Please test and check the diagnostic logs in the console output. The logs will tell us exactly what's happening at each step.

---

## ✅ VERIFICATION - March 20, 2026

**Status**: **CONFIRMED WORKING** ✅

### Test Results

All functionality verified through user testing:

1. ✅ **ESC clears selection** - Confirmed working
2. ✅ **Click to edit restores selection** - Confirmed working  
3. ✅ **Navigation clears selection** - Confirmed working across articles
4. ✅ **Rapid ESC/Edit cycles** - No issues observed

### Log Confirmation

From production logs:
```
12:48:40 [DEBUG] ArticleEditor.ClearAllTextSelections: found 7 TextBox controls via visual tree search
12:48:40 [DEBUG] ArticleEditor.ClearAllTextSelections: cleared TextBox 'TitleTextBox', now SelectionStart=0, SelectionEnd=0, forced visual refresh
12:48:45 [DEBUG] ArticleEditor.OnTextBoxGotFocus: restored SelectionBrush for 'TitleTextBox'
```

**The direct property manipulation approach successfully solves the text selection visibility issue.**

---

## ✅ CODE CLEANUP - March 20, 2026

**Status**: **COMPLETE** ✅

All debug logging has been cleaned up and simplified. The code is now production-ready with:
- ✅ Removed 35+ verbose debug log statements
- ✅ Simplified ClearAllTextSelections() from 90+ lines to ~35 lines
- ✅ Added proper XML documentation comments
- ✅ Removed unnecessary code (SelectAll technique, excessive invalidation calls)
- ✅ Build verified: 0 errors, 22 warnings (all pre-existing)

**See**: [TEXT_SELECTION_FIX_CLEANUP_SUMMARY.md](TEXT_SELECTION_FIX_CLEANUP_SUMMARY.md) for complete cleanup details.

---

**End of Document**
