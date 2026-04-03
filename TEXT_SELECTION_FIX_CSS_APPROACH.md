# Text Selection Fix - CSS Style Approach

**Date**: March 20, 2026  
**Status**: ✅ **READY FOR TESTING** - CSS-based solution

---

## The Real Solution

After multiple attempts at programmatically clearing text selection, I realized the fundamental issue: **We can't reliably control when Avalonia updates TextBox selection state programmatically**. 

The actual solution is simpler: **Use CSS styles to hide the selection visually when TextBoxes don't have focus**.

---

## What Changed

### 1. Added CSS Style to Hide Unfocused Selection

**File**: `/home/justin/repos/urban-sniffle/src/index-editor/Views/ArticleEditor.axaml`

Added this style block:

```xml
<UserControl.Styles>
    <!-- Style to hide selection when TextBox is not focused -->
    <Style Selector="TextBox:not(:focus)">
        <Setter Property="SelectionBrush" Value="Transparent" />
        <Setter Property="SelectionForegroundBrush" Value="{Binding $self.Foreground}" />
    </Style>
</UserControl.Styles>
```

**What this does**:
- When a TextBox is **not focused** (`:not(:focus)`), set `SelectionBrush` to transparent
- This makes any selection highlighting **invisible**
- The text remains selected internally, but you can't see it
- When the TextBox gets focus again, the normal selection brush returns

### 2. Added Diagnostic Logging

**File**: `/home/justin/repos/urban-sniffle/src/index-editor/Views/ArticleEditor.axaml.cs`

Updated `ClearAllTextSelections()` to log what's happening:

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
                    
                    DebugLogger.Log($"[DEBUG] ArticleEditor.ClearAllTextSelections: cleared TextBox '{tb.Name}', now SelectionStart={tb.SelectionStart}, SelectionEnd={tb.SelectionEnd}");
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

---

## Why This Approach Works

### The Problem with Programmatic Clearing

All previous attempts tried to clear the selection programmatically:
- Setting `SelectionStart = 0; SelectionEnd = 0;`
- Disabling/re-enabling controls
- Using multiple dispatcher priorities
- Focus manipulation

**The issue**: Avalonia's data binding and focus management system can restore the selection state after we clear it. We were fighting against the framework.

### The CSS Solution

By using CSS styles:
1. **No fighting the framework** - We're not changing the selection state, just hiding it visually
2. **Automatic** - Avalonia's built-in `:focus` pseudo-class handles the logic
3. **Reliable** - CSS styles are applied consistently by the rendering engine
4. **Clean** - The selection is only visible when the TextBox actually has focus

---

## How It Works

### When User is Editing
```
User clicks TextBox
  ↓
TextBox gets focus
  ↓
:focus pseudo-class matches
  ↓
Style does NOT apply (TextBox:not(:focus) doesn't match)
  ↓
Normal SelectionBrush used (blue highlight visible)
  ↓
User sees selection ✓
```

### When User Presses ESC
```
User presses ESC
  ↓
EndEdit() called
  ↓
Focus moves to KeyboardFocusHost
  ↓
TextBox loses focus
  ↓
:not(:focus) pseudo-class matches
  ↓
Style applies: SelectionBrush = Transparent
  ↓
Selection still exists internally, but is invisible
  ↓
User sees no blue highlight ✓
```

### When User Navigates Between Articles
```
User presses Up/Down
  ↓
Article changes
  ↓
TextBoxes may retain selection state internally
  ↓
BUT TextBoxes don't have focus
  ↓
:not(:focus) style applies
  ↓
Selection is invisible
  ↓
User sees clean fields ✓
```

---

## Build Status

✅ **Build Successful**  
✅ **0 Errors**  
✅ **22 Warnings** (all pre-existing)  
✅ **Diagnostic logging added**  
✅ **Ready for Testing**

---

## Testing Instructions

### Test 1: Press ESC After Editing
1. Click in the Pages field
2. Select some text (drag or Shift+Arrow)
3. Press ESC
4. **Expected**: Blue selection highlight disappears immediately
5. **What's happening**: Focus moves away, CSS style hides the selection

### Test 2: Navigate Between Articles
1. Edit the Model field
2. Press ESC
3. Press Up/Down to navigate
4. **Expected**: No blue highlight appears on any fields
5. **What's happening**: TextBoxes don't have focus, so selection is invisible

### Test 3: Re-Edit a Field
1. Press ESC (fields appear non-editable)
2. Click the Title field
3. **Expected**: Field becomes editable, text can be selected
4. **What's happening**: TextBox gets focus, normal selection brush applies

### Test 4: Check Debug Logs
Run the application with console output visible and:
1. Press ESC
2. Look for log messages like:
```
[DEBUG] ArticleEditor.ClearAllTextSelections: called
[DEBUG] ArticleEditor.ClearAllTextSelections: found 6 TextBox controls
[DEBUG] ArticleEditor.ClearAllTextSelections: clearing TextBox 'PagesTextBox', IsFocused=False, SelectionStart=0, SelectionEnd=3
```

This will confirm the method is being called and show the state of each TextBox.

---

## Key Advantages

### vs. Programmatic Clearing
- ❌ **Programmatic**: Timing issues, async problems, binding restoration
- ✅ **CSS**: Automatic, no timing issues, works with framework

### vs. Disabling Fields
- ❌ **Disabling**: Fields flicker, can't edit when needed
- ✅ **CSS**: Always editable, just selection hidden when not focused

### vs. IsReadOnly
- ❌ **IsReadOnly**: Changes behavior, affects functionality
- ✅ **CSS**: Visual-only change, no behavior impact

---

## CSS Selector Explanation

```xml
<Style Selector="TextBox:not(:focus)">
```

- `TextBox` - Applies to all TextBox controls
- `:not(:focus)` - Only when the TextBox does NOT have keyboard focus
- `SelectionBrush` - The brush used to paint selection highlighting
- `Transparent` - Make it invisible
- `SelectionForegroundBrush` - The text color when selected
- `{Binding $self.Foreground}` - Use the same color as normal (no contrast change)

---

## Why Previous Attempts Failed

### Attempt 1: Multi-Priority Dispatcher Posts
```csharp
Dispatcher.UIThread.Post(clearAction, DispatcherPriority.Render);
Dispatcher.UIThread.Post(clearAction, DispatcherPriority.Loaded);
Dispatcher.UIThread.Post(clearAction, DispatcherPriority.Background);
Task.Delay(50).ContinueWith(...);
```
**Problem**: Data binding could restore selection between or after our attempts

### Attempt 2: Disable/Enable Controls
```csharp
tb.IsEnabled = false;
tb.IsEnabled = true;
```
**Problem**: Too fast, Avalonia didn't process the state change

### Attempt 3: Focus Manipulation
```csharp
if (tb.IsFocused) tb.Focus(NavigationMethod.Unspecified);
```
**Problem**: Focus state is complex, we were fighting the focus manager

### Attempt 4: IsReadOnly
```csharp
tb.IsReadOnly = true;
```
**Problem**: Changes behavior, users can't edit when needed

---

## The CSS Solution is Best Because:

1. ✅ **Framework-native** - Uses Avalonia's built-in styling system
2. ✅ **Automatic** - No code execution needed, happens at render time
3. ✅ **Reliable** - Rendering engine handles it consistently
4. ✅ **No timing issues** - Applied synchronously during render
5. ✅ **Visual-only** - Doesn't affect functionality or behavior
6. ✅ **Maintainable** - Easy to understand, no complex logic

---

## Fallback Options

If the CSS approach doesn't fully work, we can enhance it:

### Option 1: Also Clear Selection Programmatically
Keep the CSS style AND clear the selection in code (belt and suspenders)

### Option 2: Adjust CSS Specificity
Make the style more specific if it's being overridden:
```xml
<Style Selector="Border TextBox:not(:focus)">
```

### Option 3: Use !important (Not recommended but possible)
Avalonia styles support priority levels if needed

---

## Conclusion

✅ **CSS-based solution implemented**  
✅ **No complex async logic**  
✅ **Framework-native approach**  
✅ **Diagnostic logging added**  
✅ **Ready for testing**

**This should finally fix the issue reliably.** The selection will be hidden visually whenever TextBoxes don't have focus, which is exactly what you wanted - making all fields "lose focus and selection so the appearance is that of a list of uneditable fields."

When you test, please watch for the diagnostic logs to confirm `ClearAllTextSelections()` is being called, and check if the selection is now invisible when you press ESC.

