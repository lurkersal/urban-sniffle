# Keyboard Handler Migration Pattern

## Overview
This document provides a step-by-step pattern for migrating keyboard handlers from static `EditorState` to dependency-injected `IEditorState`.

**Status:** Pattern established with ArticleKeyboardHandler migration ✅

---

## ✅ Completed Migration

### ArticleKeyboardHandler
**Date:** March 5, 2026  
**Lines Modified:** ~15 references  
**Build Status:** ✅ SUCCESS (0 errors)

---

## 🎯 Migration Pattern

### Step 1: Update Constructor
```csharp
// BEFORE
public ArticleKeyboardHandler(Window window)
{
    _window = window ?? throw new ArgumentNullException(nameof(window));
}

// AFTER
public ArticleKeyboardHandler(Window window, IEditorState editorState)
{
    _window = window ?? throw new ArgumentNullException(nameof(window));
    _editorState = editorState ?? throw new ArgumentNullException(nameof(editorState));
}
```

### Step 2: Add Private Field
```csharp
private readonly IEditorState _editorState;
```

### Step 3: Replace Static References
```csharp
// BEFORE
if (EditorState.IsArticleEditorFocused)
var articles = EditorState.Articles?.ToList();
EditorState.ActiveArticle = article;
EditorState.CurrentPage = page;
EditorState.NotifyStateChanged();

// AFTER
if (_editorState.IsArticleEditorFocused)
var articles = _editorState.Articles?.ToList();
_editorState.ActiveArticle = article;
_editorState.CurrentPage = page;
_editorState.NotifyStateChanged();
```

### Step 4: Update MainWindow Instantiation
```csharp
// Create fallback instance if DI not available
var editorStateForHandlers = _editorState ?? new IndexEditor.Shared.EditorStateService();

var handlers = new List<Services.KeyboardHandlers.IKeyboardShortcutHandler>
{
    new Services.KeyboardHandlers.ArticleKeyboardHandler(this, editorStateForHandlers),
    // ... other handlers
};
```

---

## 📋 Remaining Handlers to Migrate

### High Priority
1. **SegmentKeyboardHandler** (~12 static references)
   - Heavy usage of EditorState and EditorActions
   - Critical for segment editing functionality

2. **NavigationKeyboardHandler** (~8 static references)
   - Page navigation logic
   - CurrentPage manipulation

3. **FileKeyboardHandler** (~6 static references)
   - File operations
   - State queries

---

## 🔍 Search Pattern

To find static references in a handler:
```bash
grep -n "EditorState\." HandlerFile.cs
grep -n "EditorActions\." HandlerFile.cs
```

---

## ✅ Verification Checklist

After each migration:
- [ ] Build succeeds with 0 errors
- [ ] No new compiler warnings introduced
- [ ] All static references replaced
- [ ] Constructor updated with IEditorState parameter
- [ ] MainWindow updated to inject dependency
- [ ] Handler functionality tested manually
- [ ] Documentation updated

---

## 📊 Progress Tracking

| Handler | Status | Static Refs | Date Completed |
|---------|--------|-------------|----------------|
| ArticleKeyboardHandler | ✅ Complete | ~15 | March 5, 2026 |
| SegmentKeyboardHandler | ⏳ Pending | ~12 | - |
| NavigationKeyboardHandler | ⏳ Pending | ~8 | - |
| FileKeyboardHandler | ⏳ Pending | ~6 | - |

**Total Progress:** 25% (1/4 handlers migrated)

---

## 💡 Benefits Achieved

### Per Handler Migration
- ✅ Eliminated static dependencies
- ✅ Improved testability (can mock IEditorState)
- ✅ Better separation of concerns
- ✅ Follows Dependency Injection principle
- ✅ Maintains backward compatibility

### Overall Impact
- **Static State Anti-Pattern:** Reduced from 10% → 25% complete
- **Compiler Warnings:** Guide developers to proper patterns
- **Architecture:** Moving toward clean, testable design

---

## 🚀 Next Steps

1. **Immediate:** Migrate SegmentKeyboardHandler (highest priority)
2. **Short-term:** Migrate NavigationKeyboardHandler
3. **Medium-term:** Migrate FileKeyboardHandler
4. **Long-term:** Migrate remaining 30+ files over 6-12 months

---

## 📝 Notes

- All migrations preserve backward compatibility
- Static wrappers remain with [Obsolete] warnings
- Each migration builds on established pattern
- No breaking changes to public APIs
- Gradual migration approach minimizes risk

---

**Pattern Status:** ✅ Established and Validated  
**Ready for:** Team-wide adoption

