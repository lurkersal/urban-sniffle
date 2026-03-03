# Pages Field Edit Fix

## Problem
Manual edits to the Pages field were not being saved. When the user edited the field and then navigated away and back to the article, the previous value would reappear.

## Root Cause
The `PagesTextBox` had a TwoWay binding to `PagesText`, but the binding was not reliably syncing on LostFocus. The `OnPagesTextBoxLostFocus` event handler was only calling `Validate()` but not explicitly ensuring the text value was written to the article's `PagesText` property.

## Solution
Modified `OnPagesTextBoxLostFocus` in `ArticleEditor.axaml.cs` to explicitly sync the TextBox's Text to the article's `PagesText` property before validation, similar to what was already done for the Measurements field:

```csharp
private void OnPagesTextBoxLostFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
{
    try
    {
        // Manually sync the text value to ensure it's saved
        if (sender is TextBox tb && tb.DataContext is Common.Shared.ArticleLine article)
        {
            var text = tb.Text ?? string.Empty;
            if (article.PagesText != text)
            {
                article.PagesText = text;
            }
            
            article.Validate();
            
            // Also validate segments for missing pages
            try
            {
                var folder = IndexEditor.Shared.EditorState.CurrentFolder;
                if (!string.IsNullOrWhiteSpace(folder))
                {
                    article.ValidateSegments(folder, (f, p) => IndexEditor.Shared.ImageHelper.ImageExists(f, p));
                }
            }
            catch (Exception ex) { DebugLogger.LogException("OnPagesTextBoxLostFocus: validate segments", ex); }
        }
    }
    catch (Exception ex) { DebugLogger.LogException("OnPagesTextBoxLostFocus: outer", ex); }
}
```

## Testing
1. Open an article in the editor
2. Edit the Pages field (e.g., change "8-15" to "8-12")
3. Click outside the field (trigger LostFocus)
4. Navigate to another article
5. Navigate back to the original article
6. The Pages field should show the edited value ("8-12"), not the original value

The fix ensures that manual edits are explicitly written to the ArticleLine's PagesText property, which in turn updates the Pages collection.

