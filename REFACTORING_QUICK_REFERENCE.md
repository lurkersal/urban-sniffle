# Refactoring Quick Reference

## 📊 Current Status (March 5, 2026)

### Completed ✅
- [x] DialogService extracted from MainWindow
- [x] MainWindow reduced: 1,405 → 1,253 lines (-152)
- [x] Build verified: ✅ Passing
- [x] Verified IndexFileService already eliminates duplication

### In Progress 🟡
- [ ] MainWindow reduction (goal: <800 lines)
- [ ] Overlay management extraction

### Pending ⏳
- [ ] PageControllerView refactoring (1,289 lines → <800)
- [ ] Static EditorState migration (30+ files)
- [ ] Keyboard handler DI migration

---

## 🎯 Next 3 Actions (Priority Order)

1. **Split OverlayManager** (2-3 hours)
   - File: `/src/index-editor/Services/OverlayManager.cs` (748 lines)
   - Create: `IOverlayService`, `IndexOverlayService`, `JsonViewerService`
   - Impact: -300 lines coupling, better testability

2. **Extract Link Discovery** (1-2 hours)
   - Create: `LinkDiscoveryService`
   - Impact: -100 lines from MainWindow

3. **Refactor PageControllerView** (1-2 days)
   - File: `/src/index-editor/Views/PageControllerView.axaml.cs` (1,289 lines)
   - Create: `PageNavigationService`, `ImageLoadingService`
   - Impact: -400 lines from view

---

## 📁 Key Files

### New Services Created:
- `/src/index-editor/Services/IDialogService.cs`
- `/src/index-editor/Services/DialogService.cs`

### Existing Services (Working):
- `/src/index-editor/Services/IIndexFileService.cs`
- `/src/index-editor/Services/IndexFileService.cs` ✅ Eliminates duplication

### God Objects (Need Work):
- `/src/index-editor/MainWindow.axaml.cs` (1,253 lines) - Goal: <800
- `/src/index-editor/Views/PageControllerView.axaml.cs` (1,289 lines) - Goal: <800
- `/src/index-editor/Services/OverlayManager.cs` (748 lines) - Goal: Split into 4-5 services

---

## 🔧 Build & Test

### Quick Build:
```bash
cd /home/justin/repos/urban-sniffle
dotnet build src/index-editor/IndexEditor.csproj
```

### Full Rebuild:
```bash
bash scripts/clean-rebuild-indexeditor.sh
```

### Run Tests:
```bash
dotnet test tests/index-editor/
```

### Current Status:
- ✅ Build: Passing
- ✅ Tests: Passing  
- ⚠️ Warnings: ~40 (obsolete attributes - expected)

---

## 📈 Progress Metrics

| Target | Before | Now | Goal | Progress |
|--------|--------|-----|------|----------|
| MainWindow | 1,405 | 1,253 | 800 | 25% |
| Services | 0 | 2 | 10 | 20% |
| Total Refactor | 0% | 15% | 100% | 15% |

---

## 💡 Pattern Examples

### ✅ Good: Using Dependency Injection
```csharp
public class MyService
{
    private readonly IIndexFileService _fileService;
    
    public MyService(IIndexFileService fileService)
    {
        _fileService = fileService;
    }
}
```

### ❌ Bad: Using Static State
```csharp
var articles = EditorState.Articles; // Obsolete!
```

### ✅ Better: Inject IEditorState
```csharp
private readonly IEditorState _editorState;
public MyService(IEditorState editorState)
{
    _editorState = editorState;
}
```

---

## 📚 Documentation

### Created:
- `CODE_QUALITY_ANALYSIS.md` - Comprehensive analysis (March 4)
- `REFACTORING_PROGRESS_REPORT.md` - Detailed progress
- `REFACTORING_SESSION_SUMMARY.md` - Session summary
- `REFACTORING_QUICK_REFERENCE.md` - This file

### Original Analysis:
- Lines: 308
- Critical Issues: 3
- High Priority Issues: 2
- Estimated Effort: 9-12 days total

---

## 🎯 Success Criteria

### Phase 1 (Complete): ✅
- [x] MainWindow reduced by 150+ lines
- [x] DialogService extracted
- [x] No compilation errors
- [x] Tests passing

### Phase 2 (Next): 🎯
- [ ] MainWindow < 1,000 lines
- [ ] 3+ services extracted
- [ ] Overlay management split

### Phase 3 (Future): ⏳
- [ ] PageControllerView < 800 lines
- [ ] Views free of business logic
- [ ] All file I/O through services

### Phase 4 (Final): 🏁
- [ ] Zero static EditorState usage
- [ ] Full DI throughout
- [ ] 100% testable

---

## ⚡ Quick Commands

### Line count check:
```bash
wc -l src/index-editor/MainWindow.axaml.cs
wc -l src/index-editor/Views/PageControllerView.axaml.cs
wc -l src/index-editor/Services/OverlayManager.cs
```

### Find static usage:
```bash
grep -r "EditorState\." src/index-editor/ --include="*.cs" | wc -l
```

### Check compilation:
```bash
dotnet build --no-restore 2>&1 | grep -E "(error|warning)"
```

---

## 🔗 Related Files

- `src/index-editor/App.axaml.cs` - DI registration (line 52)
- `src/index-editor/Shared/EditorState.cs` - Static wrapper (159 lines)
- `src/index-editor/Shared/IEditorState.cs` - Interface
- `tests/index-editor/TestDIHelper.cs` - Test infrastructure

---

*Last Updated: March 5, 2026, 08:41 AM*  
*Status: ✅ Build Passing | Tests Passing*

