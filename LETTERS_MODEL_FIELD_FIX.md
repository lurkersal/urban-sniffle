# Letters Category Model Field Fix

## Issue
Letters category articles did not show the Model field in the article editor.

## Fix Applied

**File:** `/home/justin/repos/urban-sniffle/src/index-editor/Views/ArticleCategoryDisplayConverter.cs`

Added "Letters" to display mode 2 (which shows Model field):

**Before:**
```csharp
if (category.Equals("Letters", StringComparison.OrdinalIgnoreCase))
    return 1;  // Category+Title only
if (category.Equals("Cover", StringComparison.OrdinalIgnoreCase) || 
    category.Equals("Model", StringComparison.OrdinalIgnoreCase) ||
    category.Equals("Wives", StringComparison.OrdinalIgnoreCase))
    return 2;  // Model/Cover fields
```

**After:**
```csharp
if (category.Equals("Cover", StringComparison.OrdinalIgnoreCase) || 
    category.Equals("Model", StringComparison.OrdinalIgnoreCase) ||
    category.Equals("Wives", StringComparison.OrdinalIgnoreCase) ||
    category.Equals("Letters", StringComparison.OrdinalIgnoreCase))
    return 2;  // Model/Cover fields
```

## Result - Letters Category Now Shows:

✅ Title  
✅ Category  
✅ **Model:** (model name field - bound to ModelName0)  
✅ Measurements: (if populated)  
✅ Pages  

**Does NOT show:**
❌ Age field (controlled by ShowAgeCategoryConverter - only Model/Cover show age)  
❌ Photographer/Contributor field

## Behavior Details

Letters category now has the same field display as Wives:
- Shows Model name field
- Shows Measurements field (if already populated, or can be added manually)
- Does NOT show Age field
- Does NOT show Photographer/Contributor field

This is appropriate for Letters articles that reference models in reader correspondence.

## Build Status
```
Build succeeded.
    0 Error(s)
```

## Testing
After rebuilding, run the application:
```bash
cd /home/justin/repos/urban-sniffle
./scripts/run-index-editor.sh
```

Edit a Letters category article to verify the Model field is now visible.

## Date Fixed
February 17, 2026

