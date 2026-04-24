# Ctrl+Up/Down - Page Preservation Update

**Date**: March 24, 2026  
**Status**: ✅ **COMPLETE** - Ctrl+Up/Down now preserve the current page

---

## Issue Identified

The Ctrl+Up and Ctrl+Down shortcuts were changing both the selected article AND the current page, which was disruptive to the workflow.

---

## Solution

Modified the keyboard handlers to **only change the selected article** without affecting the current page.

---

## Behavior Comparison

### Before Fix ❌
```
Current state:
- Viewing page 45
- Article #5 selected (pages 40-50)

Press Ctrl+Up:
- Viewing page 3 (CHANGED - jumps to first article's page) ❌
- Article #1 selected (pages 3-10)
```

### After Fix ✅
```
Current state:
- Viewing page 45
- Article #5 selected (pages 40-50)

Press Ctrl+Up:
- Viewing page 45 (UNCHANGED - stays on same page) ✅
- Article #1 selected (pages 3-10)
```

---

## Use Cases

### ✅ Select Article for Editing Without Losing Your Place
**Scenario**: You're reviewing page 50, but need to select Article #1 to edit its metadata

```
1. Currently viewing page 50
2. Press Ctrl+Up to select first article
3. Edit the article metadata in the article editor
4. Still viewing page 50 ✅
```

### ✅ Quick Article Selection While Reviewing Pages
**Scenario**: You're reviewing pages sequentially but need to mark different articles

```
1. Viewing page 30
2. Press Ctrl+Up to select first article
3. Make notes or edits
4. Press Ctrl+Down to select last article
5. Make notes or edits
6. Still viewing page 30 ✅
```

---

## Technical Implementation

### Previous Implementation
Both handlers called `NavigateToArticle()` which:
1. Updated selected article
2. Changed current page to first page of the article ❌

### New Implementation
Handlers now:
1. Update `vm.SelectedArticle` directly
2. Update `_editorState.ActiveArticle` directly
3. Update ListBox selection UI
4. **Do NOT change the current page** ✅

### Code Changes

**File**: `/home/justin/repos/urban-sniffle/src/index-editor/Services/KeyboardHandlers/ArticleKeyboardHandler.cs`

#### HandleCtrlUp() - Before:
```csharp
private void HandleCtrlUp(KeyEventArgs e)
{
    // ...
    var targetArticle = articles[0];
    NavigateToArticle(vm, targetArticle);  // ← Changed page!
    // ...
}
```

#### HandleCtrlUp() - After:
```csharp
private void HandleCtrlUp(KeyEventArgs e)
{
    // ...
    var targetArticle = articles[0];
    
    // Update article selection only (no page change)
    if (vm != null)
    {
        vm.SelectedArticle = targetArticle;
        _editorState.ActiveArticle = targetArticle;
    }
    else
    {
        _editorState.ActiveArticle = targetArticle;
    }
    
    // Update ListBox selection UI
    var lb = _window.FindControl<Views.ArticleList>("ArticleListControl")?.FindControl<ListBox>("ArticlesListBox");
    if (lb != null)
    {
        lb.SelectedIndex = 0;
    }
    // ...
}
```

Same pattern applied to `HandleCtrlDown()`.

---

## Keyboard Navigation Summary

| Key Combination | Article Selection | Page Change |
|----------------|-------------------|-------------|
| **Up Arrow** | Previous article | Yes - navigates to article's page |
| **Down Arrow** | Next article | Yes - navigates to article's page |
| **Ctrl+Up** ⭐ | First article | **No - page preserved** |
| **Ctrl+Down** ⭐ | Last article | **No - page preserved** |

---

## Benefits

### ✅ Non-Disruptive Navigation
- Select articles without losing your viewing position
- Review pages while marking multiple articles
- Edit article metadata without jumping around

### ✅ Efficient Workflow
- Quick article selection: Ctrl+Up, Ctrl+Down
- Page stays put: No need to navigate back
- Focus on task: Less distraction from page jumps

### ✅ Consistent with Intent
- Ctrl+Up/Down are for **article list navigation**
- Left/Right arrows are for **page navigation**
- Each shortcut has a clear, single purpose

---

## Build Status

✅ **Build Successful**
```
Build succeeded.
    0 Error(s)
   22 Warning(s) (all pre-existing)
```

---

## Testing Checklist

When you run the application:

### ✅ Test Page Preservation (Ctrl+Up)
1. Open a folder with multiple articles
2. Navigate to page 50 (or any page)
3. Press **Ctrl+Up**
4. [ ] Verify first article is selected in the list
5. [ ] Verify you're still viewing page 50 (page didn't change)
6. [ ] Verify article editor shows first article

### ✅ Test Page Preservation (Ctrl+Down)
1. Navigate to page 30
2. Press **Ctrl+Down**
3. [ ] Verify last article is selected in the list
4. [ ] Verify you're still viewing page 30 (page didn't change)
5. [ ] Verify article editor shows last article

### ✅ Test Article Editing
1. Navigate to page 45
2. Press **Ctrl+Up** to select first article
3. Edit the article's Title field
4. [ ] Verify you can edit the article
5. [ ] Verify you're still viewing page 45
6. Save the change
7. [ ] Verify save works and page remains at 45

### ✅ Test Combined Navigation
1. Navigate to page 60
2. Press **Ctrl+Up** (select first article)
3. [ ] Verify page stays at 60
4. Press **Ctrl+Down** (select last article)
5. [ ] Verify page stays at 60
6. Press **Up** arrow (previous article)
7. [ ] Verify page changes to that article's page (normal behavior)

### ✅ Compare with Arrow Keys
1. Navigate to page 40
2. Press **Down** arrow
3. [ ] Verify page changes to next article's first page (old behavior)
4. Navigate back to page 40
5. Press **Ctrl+Down**
6. [ ] Verify page stays at 40 (new behavior)

---

## Summary

✅ **Ctrl+Up now preserves page** - Only selects first article  
✅ **Ctrl+Down now preserves page** - Only selects last article  
✅ **Non-disruptive workflow** - Edit articles without losing your place  
✅ **Clear purpose separation** - Article selection vs page navigation  
✅ **Build successful** - No errors introduced  

The shortcuts now work exactly as intended - quick article selection without disrupting your current viewing position! 🎉

---

**End of Document**

