# New Article Selection Fix

## Issue
When "New Article" was run (via Ctrl+N or the command), the new article was created but not automatically selected in the article list or shown in the article editor.

## Root Cause
The application has two different code paths for creating new articles:

1. **`PageControllerView.CreateNewArticle()`** - Has comprehensive selection logic that:
   - Sets `EditorState.ActiveArticle`
   - Sets `vm.SelectedArticle`
   - Updates the ListBox selection
   - Focuses the editor

2. **`EditorActions.CreateNewArticle()`** - Only sets `EditorState.ActiveArticle` but doesn't update the ViewModel's `SelectedArticle` property

The problem was that `MainWindowViewModel.NewArticle()` calls `EditorActions.CreateNewArticle()`, which didn't update the ViewModel selection.

## Solution
Modified `EditorActions.CreateNewArticle()` to also select the new article in the ViewModel after creating it.

### Changes Made

**File: `/home/justin/repos/urban-sniffle/src/index-editor/Shared/EditorActions.cs`**

Added code after `EditorState.NotifyStateChanged()` to:
1. Find the MainWindow via `Avalonia.Application.Current`
2. Get the `EditorStateViewModel` from the MainWindow's DataContext
3. Find the newly created article in the VM's Articles collection
4. Execute the SelectArticleCommand and set `vm.SelectedArticle`
5. Log the selection for debugging

The selection logic runs on the UI thread with `DispatcherPriority.Background` to ensure the VM's Articles collection is updated first.

## Code Added

```csharp
// Select the new article in the ViewModel so it appears in the article list and editor
try
{
    // Find the MainWindow and get the EditorStateViewModel
    var app = Avalonia.Application.Current;
    if (app?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
    {
        var mainWindow = desktop.MainWindow;
        if (mainWindow != null)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    var vm = mainWindow.DataContext as IndexEditor.Views.EditorStateViewModel;
                    if (vm != null)
                    {
                        // Find the article in the VM's collection (it should match by reference or pages)
                        var inList = vm.Articles.FirstOrDefault(a => object.ReferenceEquals(a, article))
                                  ?? vm.Articles.FirstOrDefault(a => a.Pages != null && article.Pages != null && a.Pages.SequenceEqual(article.Pages));
                        var toSelect = inList ?? article;
                        
                        // Select the article
                        if (vm.SelectArticleCommand != null && vm.SelectArticleCommand.CanExecute(toSelect))
                            vm.SelectArticleCommand.Execute(toSelect);
                        vm.SelectedArticle = toSelect;
                        
                        DebugLogger.Log($"EditorActions.CreateNewArticle: Selected article in ViewModel");
                    }
                }
                catch (Exception ex) { DebugLogger.LogException("EditorActions.CreateNewArticle: select article in VM", ex); }
            }, Avalonia.Threading.DispatcherPriority.Background);
        }
    }
}
catch (Exception ex) { DebugLogger.LogException("EditorActions.CreateNewArticle: find and select article", ex); }
```

## Build Status
✅ **Build successful** (0 errors, 0 warnings)

## Testing

To verify the fix:

1. **Run the application**:
   ```bash
   cd /home/justin/repos/urban-sniffle
   ./scripts/run-index-editor.sh [FOLDER]
   ```

2. **Test creating new article**:
   - Press `Ctrl+N` to create a new article
   - The new article should be immediately selected in the article list
   - The article editor should show the new article
   - You should be able to edit the title, category, etc.

3. **Check debug logs**:
   - Look for: `EditorActions.CreateNewArticle: Selected article in ViewModel`
   - This confirms the selection logic ran successfully

## Expected Behavior

After pressing Ctrl+N:
1. A new article is created at the current page
2. The article is inserted in the correct position in the list (sorted by page number)
3. The article is **automatically selected** in the article list
4. The article editor displays the newly selected article
5. You can immediately start editing the article

## Notes

- The fix maintains consistency between both code paths (`PageControllerView.CreateNewArticle()` and `EditorActions.CreateNewArticle()`)
- Uses the same selection logic pattern as the PageControllerView implementation
- Includes comprehensive error handling and logging
- Runs on the UI thread to ensure proper ViewModel updates

