# Index-Editor Code Quality Analysis
**Date:** March 4, 2026

## Executive Summary

The index-editor project shows **mixed adherence** to coding standards and best practices. While there are positive architectural elements (DI infrastructure, service separation), several significant issues exist that impact maintainability, testability, and code quality.

**Overall Grade: C+ (Fair - Needs Improvement)**

---

## 1. God Objects / Large Classes ⚠️

### Critical Issues:

#### **MainWindow.axaml.cs** - 1,375 lines ❌
- **Problem:** This is a clear "God Object" - handles too many responsibilities
- **Responsibilities identified:**
  - Window lifecycle management
  - File I/O operations (loading/saving index files)
  - Keyboard event routing
  - Article parsing
  - Link discovery coordination
  - Overlay management
  - Fullscreen image handling
  - Dialog management
  
**Recommendation:** Extract these concerns into separate services:
- `IndexFileLoader` service
- `IndexFileSaver` service  
- `DialogService`
- Move parsing logic to `IndexFileParser` (already exists)
- Keep only window-specific UI logic in MainWindow

#### **PageControllerView.axaml.cs** - 1,289 lines ⚠️
- **Problem:** Handles too many page-related operations
- Mixing view logic with business logic (page scanning, image loading)
- **Recommendation:** Extract to `PageNavigationService`, `ImageLoadingService`

#### **OverlayManager.cs** - 705 lines ⚠️
- Manages multiple overlay types (index, JSON, delete, segment)
- **Recommendation:** Split into separate overlay managers per type

---

## 2. Code Duplication 🔴

### High-Priority Duplications:

#### **LoadArticlesFromFolder** - Duplicated logic
**Locations:**
1. `MainWindow.axaml.cs:864-1095` (232 lines)
2. `ArticleEditorView.axaml.cs:135-221` (87 lines)

**Duplicated operations:**
- File path resolution
- Index file detection (JSON vs TXT)
- Article parsing
- Error handling for parse failures
- ViewModel synchronization

**Impact:** Changes must be made in multiple places, increasing bug risk

**Recommendation:** 
```csharp
// Create a centralized service
public interface IIndexFileService
{
    (Metadata, List<ArticleLine>, List<MagazineLink>) LoadFromFolder(string folder);
    void SaveToFolder(string folder, Metadata metadata, List<ArticleLine> articles);
}
```

#### **ParseArticleLine** - Multiple implementations
**Locations:**
1. `MainWindow.axaml.cs` - private method
2. `IndexFileParser.cs` - public static
3. Both parse the same CSV format

**Recommendation:** Remove MainWindow version, use only IndexFileParser

---

## 3. Static State / Service Locator Anti-Pattern ⚠️

### The Good News:
✅ Proper DI infrastructure exists with interfaces:
- `IEditorState` / `EditorStateService`
- `IEditorActions` / `EditorActionsService`
- `IToastService` / `DefaultToastService`

### The Problem:
❌ **Extensive use of static wrappers** for backward compatibility:

```csharp
[Obsolete("Use IEditorState via dependency injection instead")]
public static class EditorState
{
    private static IEditorState? _instance;
    // ... 90+ lines of static property wrappers
}
```

**Current state:** Most code still uses static access patterns:
- `EditorState.CurrentPage` (static wrapper)
- `EditorState.Articles` (static wrapper)
- Makes testing difficult
- Tight coupling between components

**Files using static EditorState:** 30+ files

**Recommendation:**
- Create migration plan to inject `IEditorState` instead
- Set deprecation timeline (6 months)
- Add compiler warnings for static usage
- Update all new code to use DI

---

## 4. Separation of Concerns 📊

### Violations Found:

#### **View Classes Doing Business Logic:**

**ArticleEditorView.axaml.cs:**
```csharp
private void LoadArticlesFromIndexFile()  // ❌ View loading data
{
    var lines = System.IO.File.ReadAllLines(indexFilePath);  // ❌ Direct file I/O
    var parsed = IndexEditor.Shared.IndexFileParser.ParseArticleLine(raw);  // OK
}
```

**PageControllerView.axaml.cs:**
```csharp
private void ScanAvailablePages()  // ❌ View scanning filesystem
{
    var files = Directory.GetFiles(folder);  // ❌ Direct file I/O
}
```

**Recommendation:** Views should only handle UI logic. Move file operations to services.

---

## 5. Method Complexity 📏

### Long Methods Identified:

1. **MainWindow.OnMainWindowKeyDown** - 500+ lines ❌
   - Single method handling all keyboard shortcuts
   - Deep nesting (4-5 levels)
   - Multiple responsibilities
   - **Recommendation:** Use Command pattern or Strategy pattern

2. **MainWindow.LoadArticlesFromFolder** - 232 lines ❌
   - Too many responsibilities
   - **Recommendation:** Extract to service with smaller methods

3. **PageControllerView.OnImageTapped** - Complex nested logic ⚠️
   - **Recommendation:** Extract helper methods

---

## 6. SOLID Principles Assessment

### Single Responsibility Principle (SRP): ❌ **Violated**
- MainWindow has 10+ responsibilities
- Views handle business logic
- Mixed concerns throughout

### Open/Closed Principle (OCP): ⚠️ **Partially Met**
- Good: Interface-based services allow extension
- Bad: Many concrete implementations hardcoded

### Liskov Substitution Principle (LSP): ✅ **Generally Good**
- Interfaces properly implemented
- No obvious violations

### Interface Segregation Principle (ISP): ✅ **Good**
- Focused interfaces: `IEditorState`, `IEditorActions`, etc.
- No "fat interfaces" detected

### Dependency Inversion Principle (DIP): ⚠️ **Partially Met**
- Good: Proper interfaces defined
- Bad: Static usage prevents full DI benefits
- Views depend on concrete implementations

---

## 7. Code Smells Detected 👃

### High Priority:

1. **Feature Envy** - MainWindow accessing EditorState properties extensively
2. **Shotgun Surgery** - Changing article loading requires edits in 3+ files
3. **Long Parameter Lists** - Some methods have 5+ parameters
4. **Primitive Obsession** - Using strings for categories, volumes, etc.
5. **Dead Code** - Comments like "// legacy AddSegmentBtn removed"
6. **Magic Numbers** - Hardcoded values (e.g., `FindFirstImageInFolder(folder, 1, 2000)`)

### Medium Priority:

7. **Inappropriate Intimacy** - Views directly manipulating EditorState
8. **Message Chains** - `this.DataContext as VM` patterns throughout
9. **Speculative Generality** - Obsolete attributes on still-used code
10. **Comments Explaining Code** - "// Helper: refresh..." suggests unclear method names

---

## 8. Testing Concerns 🧪

### Positive:
✅ Test projects exist
✅ TestDIHelper for test setup
✅ Integration tests present

### Concerns:
❌ Static state makes unit testing difficult
❌ Views with business logic are hard to test
❌ God objects require extensive mocking

---

## 9. Naming Conventions ✅

**Generally Good:**
- Clear, descriptive names
- Consistent C# conventions
- Good interface naming (`IEditorState`)

**Minor Issues:**
- Some abbreviations: `vm`, `pc`, `lb` in local variables
- Inconsistent between `OnXxxYyy` vs `Handle Xxx`

---

## 10. Exception Handling ⚠️

### Pattern Found:
```csharp
try { /* operation */ }
catch (Exception ex) { DebugLogger.LogException("context", ex); }
```

**Issues:**
- Catching all exceptions (`Exception`) too broadly
- Silent failures in many cases
- No retry logic for I/O operations

**Recommendation:**
- Catch specific exceptions
- Let critical errors propagate
- Add retry logic for file operations

---

## Priority Action Items

### 🔴 Critical (Do First):

1. **Break up MainWindow** - Extract 4-5 services
2. **Eliminate LoadArticlesFromFolder duplication** - Create `IIndexFileService`
3. **Remove static EditorState usage** - Migrate to DI (start with new code)

### 🟡 High Priority:

4. **Extract business logic from Views** - Move to ViewModels or Services
5. **Reduce OnMainWindowKeyDown complexity** - Use Command pattern
6. **Add method-level tests** - Especially for parsing logic

### 🟢 Medium Priority:

7. **Extract PageControllerView services** - ImageLoading, PageNavigation
8. **Standardize exception handling** - Specific catch blocks
9. **Remove dead code** - Clean up obsolete comments
10. **Add XML documentation** - Especially for public APIs

---

## Estimated Refactoring Effort

| Task | Effort | Impact |
|------|--------|--------|
| Break up MainWindow | 2-3 days | High |
| Centralize file loading | 1 day | High |
| Migrate from static state | 3-4 days | High |
| Extract view logic | 2 days | Medium |
| Reduce method complexity | 1-2 days | Medium |
| **Total** | **9-12 days** | |

---

## Conclusion

The index-editor has **good architectural foundations** with DI interfaces and service separation, but suffers from **incomplete migration** from legacy patterns. The main issues are:

1. **God objects** (MainWindow, PageControllerView)
2. **Code duplication** (loading logic)
3. **Mixed concerns** (views doing business logic)
4. **Static state overuse** (backward compatibility wrapper)

**These are fixable** with systematic refactoring. The codebase is not in crisis, but **technical debt is accumulating**. Addressing the critical items would significantly improve maintainability.

**Recommendation:** Allocate 1-2 weeks for focused refactoring before adding major new features.

