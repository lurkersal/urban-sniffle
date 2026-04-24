# Babepedia Manual Check - Removed from Auto Selection - March 19, 2026

## Change Summary

The babepedia lookup functionality has been **removed from automatic article selection** and converted to a **manual trigger** using the **Ctrl+B** keyboard shortcut.

## What Changed

### Before (Automatic)
- Every time you selected a Model or Cover article
- Automatic HTTP request to babepedia.com
- Popup appeared if model page existed
- Could be annoying for frequent article navigation

### After (Manual)
- Select any article
- Press **Ctrl+B** when you want to check babepedia
- Popup appears only when you explicitly request it
- No automatic lookups during article selection

## How to Use

### Keyboard Shortcut: Ctrl+B

1. **Select an article** in the article list (any category)
2. **Press Ctrl+B**
3. If the article is Model or Cover category:
   - HTTP request sent to babepedia.com
   - If model page exists → Popup appears
   - If no page → Silent (logged to console)
4. If the article is not Model/Cover:
   - Message: "Skipping - not a Model or Cover article"

### Example Usage
```
1. Click on "Ginny" (Cover article, model: Brandy Ledford)
2. Press Ctrl+B
3. Console output:
   CheckBabepediaAsync: Called for article 'Ginny' (Category: Cover)
   CheckBabepediaAsync: Starting babepedia check
   BabepediaService: Checking URL: https://www.babepedia.com/babe/Brandy_Ledford
   BabepediaService: Final URL: .../babe/Brandy_Ledford
   BabepediaService: Brandy Ledford -> Exists: true
   BABEPEDIA: Model 'Brandy Ledford' found at ...
4. Popup dialog appears ✅
```

## Files Modified

### 1. EditorStateViewModel.cs ✅
**Removed:**
- Automatic call to `CheckBabepediaAsync()` in `SelectedArticle` setter

**Added:**
- `CheckBabepediaForSelectedArticle()` public method
- Made `CheckBabepediaAsync()` public (was private)

**Changes:**
```csharp
// OLD (in SelectedArticle setter):
if (_selectedArticle != null)
{
    CheckBabepediaAsync(_selectedArticle);  // ❌ Removed
}

// NEW (public method for manual trigger):
public void CheckBabepediaForSelectedArticle()
{
    if (_selectedArticle != null)
    {
        CheckBabepediaAsync(_selectedArticle);
    }
    else
    {
        ToastService.Show("No article selected");
    }
}

public async void CheckBabepediaAsync(ArticleLine article) // Made public
{
    // ...existing implementation...
}
```

### 2. FileKeyboardHandler.cs ✅
**Added:**
- Ctrl+B keyboard shortcut handling
- `HandleCtrlB()` method

**Changes:**
```csharp
// In TryHandle():
if (e.Key == Key.B && e.KeyModifiers.HasFlag(KeyModifiers.Control))
{
    HandleCtrlB(e);
    return true;
}

// New method:
private void HandleCtrlB(KeyEventArgs e)
{
    var vm = _window.DataContext as EditorStateViewModel;
    if (vm != null)
    {
        vm.CheckBabepediaForSelectedArticle();
    }
    // ...error handling...
}
```

## Benefits

### 1. Performance
- No automatic HTTP requests on every article selection
- Faster article navigation
- Reduced network traffic

### 2. User Control
- You decide when to check babepedia
- No unwanted popups during workflow
- Check multiple articles selectively

### 3. Same Functionality
- All babepedia checking code still exists
- Same redirect detection logic
- Same popup dialog
- Just triggered manually instead of automatically

## Keyboard Shortcuts Summary

Updated list of file-related shortcuts:
- **Ctrl+O** - Open folder
- **Ctrl+S** - Save index file
- **Ctrl+I** - Toggle index file overlay
- **Ctrl+B** - Check babepedia for selected article ← **NEW**
- **F11** - Toggle fullscreen

## Console Output Examples

### Successful Check (Model Exists)
```
FileKeyboardHandler: Ctrl+B - Checking babepedia for selected article
CheckBabepediaAsync: Called for article 'Ginny' (Category: Cover)
CheckBabepediaAsync: Starting babepedia check for Model/Cover article
BabepediaService: Checking URL: https://www.babepedia.com/babe/Brandy_Ledford
BabepediaService: Requested: .../babe/Brandy_Ledford
BabepediaService: Final URL: .../babe/Brandy_Ledford
BabepediaService: Brandy Ledford -> Status: OK, Redirected to search: false, Exists: true
BABEPEDIA: Model 'Brandy Ledford' found at ...
→ Popup appears
```

### No Model Page (Redirected to Search)
```
FileKeyboardHandler: Ctrl+B - Checking babepedia for selected article
CheckBabepediaAsync: Called for article 'Test' (Category: Model)
CheckBabepediaAsync: Starting babepedia check for Model/Cover article
BabepediaService: Checking URL: https://www.babepedia.com/babe/Tanya
BabepediaService: Requested: .../babe/Tanya
BabepediaService: Final URL: .../search/Tanya
BabepediaService: Tanya -> Status: OK, Redirected to search: true, Exists: false
→ No popup (silent)
```

### Non-Model Article
```
FileKeyboardHandler: Ctrl+B - Checking babepedia for selected article
CheckBabepediaAsync: Called for article 'Tease!' (Category: Editorial)
CheckBabepediaAsync: Skipping - not a Model or Cover article
→ No popup (silent)
```

### No Article Selected
```
FileKeyboardHandler: Ctrl+B - Checking babepedia for selected article
CheckBabepediaForSelectedArticle: No article selected
→ Toast: "No article selected"
```

## Build Status
```
✅ Build succeeded
   0 errors
   22 warnings (all pre-existing)
```

## Testing Instructions

### Test Manual Trigger
```bash
cd ~/repos/urban-sniffle
dotnet run --project src/index-editor/IndexEditor.csproj
```

1. **Open a folder** with Model/Cover articles
2. **Click on "Ginny"** (or any Model/Cover article)
3. **Press Ctrl+B**
4. **Expected:** Popup appears if model has babepedia page

### Test Article Navigation
1. **Click through several articles** (Ginny → Index → Tease!)
2. **Expected:** No automatic popups ✅
3. **Press Ctrl+B on each**
4. **Expected:** Only Model/Cover articles trigger check

### Test Non-Model Article
1. **Select "Index" article** (category: Index)
2. **Press Ctrl+B**
3. **Expected:** Console shows "Skipping - not a Model or Cover article"
4. **Expected:** No popup

## Migration Notes

### Old Behavior
- Automatic check on every article selection
- Could be disruptive during navigation

### New Behavior
- Manual check only when requested
- User has full control over when to check

### No Data Loss
- All babepedia checking functionality preserved
- Same redirect detection
- Same popup dialog
- Just different trigger mechanism

---

**Status:** ✅ Feature converted to manual trigger (Ctrl+B)

**Result:** Babepedia lookup is now on-demand instead of automatic, giving you full control over when to check model pages.

