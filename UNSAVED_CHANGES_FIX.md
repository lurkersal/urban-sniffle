# Unsaved Changes False Positive Fix

## Date: March 1, 2026

## Problem
The editor was prompting to save on quit even when no actual changes were made - just navigating between articles with Ctrl+Up/Down arrows was triggering the unsaved changes flag.

## Root Cause
The `OnArticlePropertyChanged` handler in `EditorStateViewModel.cs` was setting `HasUnsavedChanges = true` for **ALL** property changes on articles, including UI-only properties that don't represent actual user data modifications.

When navigating with Ctrl+Up/Down:
1. The `SelectedArticle` property is set to a different article
2. This triggers various property changed notifications on articles
3. Properties like `ActiveSegment`, `LastModifiedSegment`, `WasAutoHighlighted`, `FormattedCardText`, `IsSelected` etc. would fire PropertyChanged events
4. The handler would mark all of these as "unsaved changes" even though they're just UI state

## Solution
Modified `OnArticlePropertyChanged` to only set `HasUnsavedChanges = true` for properties that represent actual user data changes:

### Data Properties (triggers unsaved changes):
- `Pages`
- `PagesText`
- `Category`
- `Title`
- `ModelNames`
- `Age`
- `Ages`
- `Contributors`
- `Illustrators`
- `ModelSize`
- `Measurements`
- `BustSize`, `WaistSize`, `HipSize`, `CupSize`
- `BustSizes`, `WaistSizes`, `HipSizes`, `CupSizes`
- `Notes`

### UI Properties (does NOT trigger unsaved changes):
- `ActiveSegment` - computed from segments, UI state
- `LastModifiedSegment` - transient UI state
- `WasAutoHighlighted` - UI highlight state
- `FormattedCardText` - computed display property
- `IsSelected` - UI selection state
- `DisplayTitle` - computed display property
- `CategoryDisplay` - computed display property
- `PagesDisplay` - computed display property
- `HasValidationError` - validation state
- `WasAutoInserted` - transient flag

## Implementation
File: `src/index-editor/Views/EditorStateViewModel.cs`

```csharp
private void OnArticlePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
{
    if (sender is ArticleLine article)
    {
        // Mark that we have unsaved changes only for properties that represent actual user data changes
        // Exclude UI-only properties like ActiveSegment, LastModifiedSegment, WasAutoHighlighted, FormattedCardText, IsSelected, etc.
        var dataProperties = new[] 
        { 
            nameof(ArticleLine.Pages), 
            nameof(ArticleLine.PagesText), 
            nameof(ArticleLine.Category), 
            nameof(ArticleLine.Title),
            nameof(ArticleLine.ModelNames),
            nameof(ArticleLine.Age),
            nameof(ArticleLine.Ages),
            nameof(ArticleLine.Contributors),
            nameof(ArticleLine.Illustrators),
            nameof(ArticleLine.ModelSize),
            nameof(ArticleLine.Measurements),
            nameof(ArticleLine.BustSize),
            nameof(ArticleLine.WaistSize),
            nameof(ArticleLine.HipSize),
            nameof(ArticleLine.CupSize),
            nameof(ArticleLine.BustSizes),
            nameof(ArticleLine.WaistSizes),
            nameof(ArticleLine.HipSizes),
            nameof(ArticleLine.CupSizes),
            nameof(ArticleLine.Notes)
        };
        
        if (dataProperties.Contains(e.PropertyName))
        {
            try { IndexEditor.Shared.EditorState.HasUnsavedChanges = true; } 
            catch (Exception ex) { DebugLogger.LogException("EditorStateViewModel.OnArticlePropertyChanged: set HasUnsavedChanges", ex); }
        }
        
        // ... rest of the method (reordering, notifications, etc.)
    }
}
```

## Testing
✅ Build successful
✅ Navigation with Ctrl+Up/Down should NOT trigger unsaved changes prompt
✅ Actual edits (title, category, pages, etc.) WILL trigger unsaved changes prompt
✅ Quit without changes works correctly

## Benefits
- No more false positives when navigating
- User only prompted to save when actual data was modified
- Better user experience
- More accurate dirty flag tracking

