# Babepedia Check - Fixed Article Selection Path - March 19, 2026

## Problem Identified
The babepedia check was NOT being called when selecting articles because it was only added to `SelectArticleCommand.Execute()`, but articles can be selected through multiple code paths:

1. ✅ `SelectArticleCommand.Execute()` - Had the check
2. ❌ Direct `ViewModel.SelectedArticle = article` setter - Missing check
3. ❌ `ArticleList.OnArticleDoubleTapped()` - Calls setter directly, missing check

**Evidence from console output:**
```
09:08:10 info: IndexEditor[0] ==> SelectedArticle SETTER CALLED: incoming='Sandy'...
   at IndexEditor.Views.ArticleList.OnArticleDoubleTapped(Object sender, RoutedEventArgs e)
```

The stack trace showed selection was happening through `OnArticleDoubleTapped` → `ViewModel.SelectedArticle` setter, which **bypassed** the `SelectArticleCommand`.

## Solution Applied

### Moved babepedia check to central location
**From:** `SelectArticleCommand.Execute()` (only one path)  
**To:** `EditorStateViewModel.SelectedArticle` setter (all paths)

This ensures the babepedia check happens **regardless of HOW** the article is selected.

## Files Modified

### 1. EditorStateViewModel.cs ✅
**Added:**
- `CheckBabepediaAsync()` method (lines 607-659)
- Call to `CheckBabepediaAsync(_selectedArticle)` in SelectedArticle setter (line 160)

**Location in setter:**
```csharp
PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedArticle)));
// ...debug logging...

// Check babepedia.com for Model/Cover articles
if (_selectedArticle != null)
{
    CheckBabepediaAsync(_selectedArticle);
}

// Force the ArticleLine to notify all UI-bound properties...
```

### 2. SelectArticleCommand.cs ✅
**Removed:**
- Entire `CheckBabepediaAsync()` method (was ~60 lines)
- Call to `CheckBabepediaAsync(toSelect)`

**Added:**
- Comment explaining check moved to ViewModel

## How It Works Now

### All Article Selection Paths
```
User Action → Article Selection → ViewModel.SelectedArticle setter → CheckBabepediaAsync()
```

**Paths that now trigger check:**
1. Click on article in list
2. Double-click on article
3. Arrow up/down in article list
4. Ctrl+Up/Down navigation
5. SelectArticleCommand.Execute()
6. Any other code that sets ViewModel.SelectedArticle

### CheckBabepediaAsync Flow
```csharp
1. Log: "CheckBabepediaAsync: Called for article 'ArticleName' (Category: Cover)"
2. Check category: Only Model and Cover
3. Log: "CheckBabepediaAsync: Starting babepedia check"
4. HTTP request to babepedia.com
5. Log: "CheckBabepediaAsync: Result - exists=true/false, modelName='...', url='...'"
6. If exists:
   - Log: "BABEPEDIA: Model 'Name' found at URL"
   - Show dialog popup
```

## Expected Console Output (Next Test)

When you select a Model or Cover article, you should now see:
```
09:XX:XX info: IndexEditor[0] ==> SelectedArticle SETTER CALLED: incoming='Ginny', current='(null)'...
09:XX:XX info: IndexEditor[0] SelectedArticle set. current.Title='Ginny', Category='Cover'...
09:XX:XX info: IndexEditor[0] CheckBabepediaAsync: Called for article 'Ginny' (Category: Cover)
09:XX:XX info: IndexEditor[0] CheckBabepediaAsync: Starting babepedia check for Model/Cover article
09:XX:XX info: IndexEditor[0] BabepediaService: Checking URL: https://www.babepedia.com/babe/Brandy_Ledford
09:XX:XX info: IndexEditor[0] BabepediaService: Brandy Ledford -> Status: OK, Exists: true
09:XX:XX info: IndexEditor[0] CheckBabepediaAsync: Result - exists=true, modelName='Brandy Ledford', url='...'
09:XX:XX info: IndexEditor[0] BABEPEDIA: Model 'Brandy Ledford' found at https://www.babepedia.com/babe/Brandy_Ledford
```

Then the dialog should appear!

## Build Status
```
✅ Build succeeded
   0 errors
   22 warnings (all pre-existing)
```

## Test Instructions

1. **Rebuild and run:**
   ```bash
   cd ~/repos/urban-sniffle
   dotnet run --project src/index-editor/IndexEditor.csproj
   ```

2. **Select a Cover/Model article** (like "Ginny - Brandy Ledford")

3. **Watch console output** - You should see:
   - "CheckBabepediaAsync: Called for article..."
   - "CheckBabepediaAsync: Starting babepedia check..."
   - HTTP request logs
   - Result logs

4. **If model has babepedia page:**
   - Popup dialog appears
   - Shows model name and clickable URL

## Why This Fix Works

**Before:** Check only happened in one specific code path  
**After:** Check happens in the central SelectedArticle setter

**Benefit:** All selection methods (click, double-click, keyboard navigation, commands) now trigger the babepedia check automatically.

---

**Status:** ✅ Fix complete - Ready to test!

