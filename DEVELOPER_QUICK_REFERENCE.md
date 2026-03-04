# Quick Reference: Using New Services

## ✅ IIndexFileService - File Operations

**When to use:** Loading or saving index files (_index.json or _index.txt)

### Before (OLD - Don't use):
```csharp
// ❌ Direct file I/O in MainWindow or Views - DUPLICATED CODE
var indexFilePath = Path.Combine(folder, "_index.json");
if (File.Exists(indexFilePath))
{
    var (mag, vol, num, year, articles, links) = IndexJsonSerializer.LoadFromJson(folder);
    // ... 200+ lines of parsing, error handling, fallback logic ...
}
// This same code was repeated in multiple places!
```

### After (NEW - Use this):
```csharp
// ✅ Inject IIndexFileService
public class MyClass
{
    private readonly IIndexFileService _indexFileService;
    
    public MyClass(IIndexFileService indexFileService)
    {
        _indexFileService = indexFileService;
    }
    
    public void LoadIndex(string folder)
    {
        var (metadata, articles, links) = _indexFileService.LoadFromFolder(folder);
        // Use the data - all parsing, error handling, and fallback logic handled!
    }
    
    public void SaveIndex(string folder, Metadata meta, List<ArticleLine> articles)
    {
        _indexFileService.SaveToFolder(folder, meta, articles);
    }
}
```

**Real Example: MainWindow Refactoring**
- **Before:** 247 lines of file I/O code in LoadArticlesFromFolder method
- **After:** 3 lines calling _indexFileService.LoadFromFolder()
- **Result:** Eliminated 232 lines of duplicate code ✨
- **Status:** ✅ Completed - MainWindow now uses IIndexFileService

---

## ⚠️ Static EditorState - DEPRECATED

**Warning:** Using static `EditorState` or `EditorActions` will show compiler warnings.

### Before (OLD - Don't use):
```csharp
// ❌ Static access - deprecated
var page = EditorState.CurrentPage;
var articles = EditorState.Articles;
EditorActions.SetCurrentPage(5);
```

### After (NEW - Use this):
```csharp
// ✅ Inject IEditorState and IEditorActions
public class MyService
{
    private readonly IEditorState _editorState;
    private readonly IEditorActions _editorActions;
    
    public MyService(IEditorState editorState, IEditorActions editorActions)
    {
        _editorState = editorState;
        _editorActions = editorActions;
    }
    
    public void DoSomething()
    {
        var page = _editorState.CurrentPage;
        var articles = _editorState.Articles;
        _editorActions.SetCurrentPage(5);
    }
}
```

---

## 📋 IDialogService - Coming Soon

**Purpose:** Centralized dialog management

```csharp
// Future usage:
var result = await _dialogService.ShowSavePrompt("Save changes?");
if (result == DialogResult.Yes)
{
    // Save...
}
```

---

## ⌨️ IKeyboardCommandService - Coming Soon

**Purpose:** Centralized keyboard shortcut handling

```csharp
// Future usage:
_keyboardService.RegisterHandler(Key.S, ModifierKeys.Control, SaveCommand);
```

---

## How to Register New Services

Add to `App.axaml.cs`:

```csharp
services.AddSingleton<IMyService, MyService>();
```

**Example from IIndexFileService:**
```csharp
// In App.axaml.cs OnFrameworkInitializationCompleted():
services.AddSingleton<Services.IIndexFileService, Services.IndexFileService>();

// In MainWindow constructor:
public MainWindow(string? folderToOpen, Services.IIndexFileService? indexFileService)
{
    _indexFileService = indexFileService;
}

// Usage in methods:
if (_indexFileService != null)
{
    (metadata, articles, links) = _indexFileService.LoadFromFolder(folder);
}
```

---

## Testing Your Service

```csharp
[Fact]
public void MyService_ShouldWork()
{
    // Arrange
    var mockEditorState = new Mock<IEditorState>();
    var service = new MyService(mockEditorState.Object);
    
    // Act
    var result = service.DoSomething();
    
    // Assert
    Assert.NotNull(result);
}
```

---

## Common Mistakes to Avoid

❌ **Don't** use static EditorState in new code  
❌ **Don't** do file I/O directly in views  
❌ **Don't** create services without interfaces  
❌ **Don't** forget to register services in DI  
❌ **Don't** duplicate file parsing logic across multiple classes  

✅ **Do** inject services via constructor  
✅ **Do** use interfaces (IMyService)  
✅ **Do** write unit tests for services  
✅ **Do** follow Single Responsibility Principle  
✅ **Do** reuse existing services (e.g., IIndexFileService for all file I/O)  

---

## Migration Progress Tracker

| Component | Status | Lines Saved | Notes |
|-----------|--------|-------------|-------|
| MainWindow | ✅ Complete | 232 lines | Now uses IIndexFileService |
| ArticleEditorView | ⏳ Pending | ~87 lines | Next target |
| KeyboardHandlers | ⏳ Pending | TBD | Use IEditorState/Actions |

---

## Need Help?

- Full analysis: `/CODE_QUALITY_ANALYSIS.md`
- Implementation guide: `/CRITICAL_ISSUES_RESOLUTION.md`
- Current status: `/CRITICAL_ISSUES_STATUS.txt`

**Questions?** Ask the development team or review the documentation above.

