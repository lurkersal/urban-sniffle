# New Article Title Field Auto-Focus

## Summary
When creating a new article with Ctrl-N, the title field in the article editor pane now automatically receives focus, allowing the user to start typing immediately.

## Changes Made

### File: `src/index-editor/MainWindow.axaml.cs`

**Modified Method:** `OnArticleCreated(Common.Shared.ArticleLine article)`

Added a call to `FocusArticleTitle()` after the new article is selected in the ViewModel. This ensures that when a new article is created:
1. The article is created and added to the list
2. The article is selected in the UI
3. **The title field automatically receives focus**

## Implementation Details

The focus request is made by calling:
```csharp
IndexEditor.Shared.EditorActions.FocusArticleTitle();
```

This leverages the existing focus infrastructure:
- `EditorActions.FocusArticleTitle()` - Requests focus through the editor state
- `EditorState.RequestArticleEditorFocus()` - Increments a counter that ArticleEditor instances observe
- `ArticleEditor.FocusTitle()` - Performs multiple async attempts to focus the TitleTextBox, handling timing/templating race conditions

## User Experience

**Before:** After pressing Ctrl-N, the user had to manually click on the title field to start typing.

**After:** When Ctrl-N is pressed, the new article is created and the title field automatically receives focus, allowing immediate typing.

## Testing

Build Status: ✅ Success (no errors)
- Compiled successfully with no new warnings or errors
- Only pre-existing deprecation warnings remain

## Notes

- The focus mechanism uses multiple retry attempts (up to 15 attempts with 60ms delays) to handle Avalonia's DataTemplate rendering timing issues
- The focus request is posted to the UI thread with `DispatcherPriority.Background` to ensure the article is fully rendered before focusing
- A visual flash effect is applied to the title field to help users see where focus landed (debugging aid)

