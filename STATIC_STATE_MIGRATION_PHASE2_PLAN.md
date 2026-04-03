# Static State Migration - Phase 2 Plan
**Date:** March 5, 2026  
**Session:** Evening Extended Refactoring (Continued)  
**Status:** 📋 Planning Next Phase

---

## 🎯 Current Status

### Phase 1 Complete ✅
- ✅ All 4 Keyboard Handlers migrated (41 static refs eliminated)
- ✅ Zero build warnings
- ✅ FileOperationsService created and in use

### Remaining Static References: 255
Distribution by file:

| File | Count | Priority | Estimated Time |
|------|-------|----------|----------------|
| PageControllerView.axaml.cs | 63 | HIGH | 3-4 hours |
| MainWindow.axaml.cs | 54 | HIGH | 2-3 hours |
| DemoRunner.cs | 31 | LOW | 1-2 hours |
| ArticleList.axaml.cs | 21 | HIGH | 2-3 hours |
| EditorStateViewModel.cs | 18 | HIGH | 2-3 hours |
| ArticleEditorView.axaml.cs | 18 | HIGH | 2-3 hours |
| TopBar.axaml.cs | 16 | MEDIUM | 1-2 hours |
| ArticleEditor.axaml.cs | 10 | MEDIUM | 1-2 hours |
| MainWindowViewModel.cs | 7 | MEDIUM | 1 hour |
| IndexSaver.cs | 6 | MEDIUM | 1 hour |
| SelectArticleCommand.cs | 4 | MEDIUM | 30 min |
| DialogService.cs | 4 | LOW | 30 min |
| App.axaml.cs | 2 | LOW | 15 min |
| ActiveSegmentToTextConverter.cs | 1 | LOW | 15 min |
| **TOTAL** | **255** | | **17-24 hours** |

---

## 📋 Migration Strategy

### Phase 2A: UI Components (HIGH PRIORITY)
**Target:** PageControllerView, ArticleList, ArticleEditorView  
**Total Refs:** 102  
**Estimated Time:** 7-10 hours

These are core UI components that directly interact with state. Migrating them will:
- Improve testability significantly
- Enable proper unit testing
- Reduce coupling
- Establish pattern for other UI components

### Phase 2B: View Models (HIGH PRIORITY)
**Target:** EditorStateViewModel, MainWindowViewModel  
**Total Refs:** 25  
**Estimated Time:** 3-4 hours

View models should not use static state. This migration will:
- Align with MVVM pattern
- Enable easier testing
- Improve data flow clarity

### Phase 2C: Supporting UI (MEDIUM PRIORITY)
**Target:** TopBar, ArticleEditor, SelectArticleCommand  
**Total Refs:** 30  
**Estimated Time:** 2-3 hours

Secondary UI components that are less complex.

### Phase 2D: Services (MEDIUM PRIORITY)
**Target:** IndexSaver, DialogService  
**Total Refs:** 10  
**Estimated Time:** 1-2 hours

Services should use DI, not static state.

### Phase 2E: Infrastructure (LOW PRIORITY)
**Target:** App.axaml.cs, DemoRunner, Converters  
**Total Refs:** 34  
**Estimated Time:** 2-3 hours

Less critical but should be cleaned up eventually.

---

## 🚀 Immediate Next Steps

### Step 1: Migrate PageControllerView (63 refs)
**Why First:**
- Highest count of static references
- Core component for page navigation
- Once fixed, will serve as example for other views

**Approach:**
1. Add `IEditorState` parameter to constructor
2. Store as private field `_editorState`
3. Replace all `EditorState.` with `_editorState.`
4. Update instantiation in MainWindow
5. Test thoroughly
6. Build and verify

**Files to modify:**
- `src/index-editor/Views/PageControllerView.axaml.cs`
- `src/index-editor/MainWindow.axaml.cs` (instantiation)

### Step 2: Migrate ArticleList (21 refs)
**Why Second:**
- Important UI component
- Moderate complexity
- Builds on PageControllerView pattern

**Approach:** Same as PageControllerView

### Step 3: Migrate ArticleEditorView (18 refs)
**Why Third:**
- Core editing component
- Similar pattern to above

---

## 📊 Success Metrics

### Before Phase 2
- Static refs: 255
- Files with static refs: 14
- Build warnings: 0 ✅

### Target After Phase 2A
- Static refs: ~153 (-102)
- Files with static refs: 11 (-3)
- Build warnings: 0
- Test coverage: >50% for migrated components

### Target After Complete Phase 2
- Static refs: 0 (-255)
- Files with static refs: 0 (-14)
- Build warnings: 0
- Test coverage: >70%

---

## ⚠️ Risks & Mitigation

### Risk 1: Breaking Changes
**Mitigation:** 
- Test after each file migration
- Maintain fallback patterns initially
- Use comprehensive manual testing

### Risk 2: Time Overrun
**Mitigation:**
- Focus on high-priority files first
- Can pause between phases
- Each component is independently testable

### Risk 3: Cascading Issues
**Mitigation:**
- Migrate one file at a time
- Build after each change
- Use git to track changes

---

## 🎓 Migration Pattern (Established)

```csharp
// 1. Add private field
private readonly IEditorState _editorState;

// 2. Update constructor
public MyComponent(IEditorState editorState)
{
    _editorState = editorState ?? throw new ArgumentNullException(nameof(editorState));
    // ... existing code ...
}

// 3. Replace static calls
// BEFORE: IndexEditor.Shared.EditorState.CurrentPage
// AFTER:  _editorState.CurrentPage

// BEFORE: EditorState.Articles
// AFTER:  _editorState.Articles
```

---

## 📅 Estimated Timeline

Assuming 2-3 hour sessions, 2-3 times per week:

| Phase | Duration | Target Completion |
|-------|----------|-------------------|
| Phase 2A | 7-10 hours | March 19, 2026 |
| Phase 2B | 3-4 hours | March 26, 2026 |
| Phase 2C | 2-3 hours | April 2, 2026 |
| Phase 2D | 1-2 hours | April 5, 2026 |
| Phase 2E | 2-3 hours | April 9, 2026 |
| **TOTAL** | **15-22 hours** | **~April 9, 2026** |

---

## ✅ Phase 2A - Step 1: PageControllerView Migration

### Pre-Migration Checklist
- [ ] Build is clean (0 warnings, 0 errors)
- [ ] Current code is committed to git
- [ ] Read PageControllerView.axaml.cs to understand structure
- [ ] Identify all `EditorState.` references (63 total)
- [ ] Plan constructor changes

### Migration Steps
1. [ ] Add `IEditorState` parameter to constructor
2. [ ] Add private field `_editorState`
3. [ ] Replace all 63 static references
4. [ ] Update MainWindow instantiation
5. [ ] Build and fix any errors
6. [ ] Manual test: Load folder, navigate pages
7. [ ] Verify page controller works correctly
8. [ ] Commit changes

### Post-Migration Verification
- [ ] Build succeeds (0 errors)
- [ ] No new warnings introduced
- [ ] PageControllerView works correctly
- [ ] Search for remaining refs: `grep "EditorState\." PageControllerView.axaml.cs` returns 0

---

## 📝 Notes

### Key Observations
1. PageControllerView has highest concentration of static refs
2. MainWindow still has 54 refs despite cleanup claims
3. DemoRunner can be lower priority (testing/demo tool)
4. Most refs are in UI layer - good candidate for DI

### Dependencies
- PageControllerView is instantiated in MainWindow
- ArticleList is instantiated in MainWindow
- All UI components can be migrated independently
- Services (IndexSaver, DialogService) should follow after UI

---

**Status:** 📋 PLAN READY - Ready to start Phase 2A Step 1

**Next Action:** Migrate PageControllerView.axaml.cs (63 static refs)

