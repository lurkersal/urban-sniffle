# Article Editor Field Bindings Fix

## Date: March 3, 2026

## Problem
The model name field (and potentially other fields) in the Article Editor was not persisting changes correctly when navigating between articles. Users would edit a field, switch to another article, and upon returning, the old value would still be present.

## Root Cause
The issue was that while all fields had TwoWay bindings in XAML, not all fields had explicit LostFocus handlers to ensure the bindings were properly synchronized when the user finished editing and moved focus away from the field.

## Solution Applied

### 1. Added Name Attributes to All Text Fields
All TextBox controls now have explicit Name attributes for easier identification:
- `TitleTextBox` 
- `ModelNameTextBox`
- `AgeTextBox`
- `MeasurementsTextBox` (already had this)
- `ContributorTextBox`
- `PagesTextBox` (already had this)

### 2. Added LostFocus Event Handlers
Added `LostFocus` event handlers to all text field bindings in the AXAML:
- **Title**: `LostFocus="OnTitleTextBoxLostFocus"`
- **ModelName0**: `LostFocus="OnModelNameTextBoxLostFocus"`
- **Age0**: `LostFocus="OnAgeTextBoxLostFocus"`
- **Measurements0**: Already had `LostFocus="OnMeasurementsTextBoxLostFocus"`
- **Contributor0**: `LostFocus="OnContributorTextBoxLostFocus"`
- **PagesText**: Already had `LostFocus="OnPagesTextBoxLostFocus"`

### 3. Implemented LostFocus Handlers in Code-Behind
Added corresponding handler methods in `ArticleEditor.axaml.cs`:

```csharp
private void OnTitleTextBoxLostFocus(object? sender, RoutedEventArgs e)
{
    // Manually sync the text value to ensure it's saved
    if (sender is TextBox tb && tb.DataContext is ArticleLine article)
    {
        var text = tb.Text ?? string.Empty;
        if (article.Title != text)
        {
            article.Title = text;
        }
    }
}

private void OnModelNameTextBoxLostFocus(object? sender, RoutedEventArgs e)
{
    // Similar implementation for ModelName0
}

private void OnAgeTextBoxLostFocus(object? sender, RoutedEventArgs e)
{
    // Similar implementation for Age0
}

private void OnContributorTextBoxLostFocus(object? sender, RoutedEventArgs e)
{
    // Similar implementation for Contributor0
}
```

## How It Works

1. **User edits a field**: The TwoWay binding updates the underlying ArticleLine property
2. **User moves focus away**: The LostFocus event fires
3. **Handler ensures persistence**: The LostFocus handler explicitly sets the property value from the TextBox.Text, ensuring the binding is synchronized
4. **Property change notifications fire**: The ArticleLine property setter raises PropertyChanged events
5. **UI updates across all views**: Other panes (like the article list) see the updated values

## Fields Covered

All editable text fields in the Article Editor are now protected:
- ✅ **Title** - required for all categories
- ✅ **Category** - ComboBox (already working correctly)
- ✅ **ModelName0** - visible for Model, Cover, Group, Wives, Interview
- ✅ **Age0** - visible for Model, Cover, Group
- ✅ **Measurements0** - visible for Model, Cover, Group (with validation)
- ✅ **Contributor0** - visible based on category (Photographer/Author/Contributor)
- ✅ **PagesText** - required for all categories (with validation)

## Testing Checklist

To verify the fix:
1. ✅ Build succeeds without errors
2. ⏳ Edit ModelName field, switch articles, return - value should persist
3. ⏳ Edit Age field, switch articles, return - value should persist
4. ⏳ Edit Title field, switch articles, return - value should persist
5. ⏳ Edit Contributor field, switch articles, return - value should persist
6. ⏳ Edit Measurements field with invalid value - validation should trigger on LostFocus
7. ⏳ Edit Pages field with invalid value - validation should trigger on LostFocus
8. ⏳ All fields should update the article list cards when changed

## Build Status

✅ **Build Successful** - 0 errors, 63 warnings (all pre-existing warnings about obsolete EditorState usage)

## Next Steps

Please test the application to confirm:
1. All fields persist their changes correctly
2. Validation triggers appropriately on LostFocus
3. Article list updates reflect changes made in the editor
4. No regressions in existing functionality

If any fields still have issues, please report with specific steps to reproduce.

