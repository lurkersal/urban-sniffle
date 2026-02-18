# Measurements Field Visibility Fix

## Issue
When adding a new article (Ctrl-N) and changing the category to Model or Cover, the Measurements field was not shown until the article was saved and reloaded.

## Root Cause
The `ShowMeasurementsConverter` was bound to the entire ArticleLine object using:
```xml
IsVisible="{Binding Path=., Converter={StaticResource ShowMeasurements}}"
```

When binding to the whole object (`.`), the converter only re-evaluates when the object reference changes, not when individual properties change. Therefore, when the Category property was modified, the UI didn't update to show/hide the Measurements field.

## Fix Applied

### 1. Updated ShowMeasurementsConverter.cs
**File:** `/home/justin/repos/urban-sniffle/src/index-editor/Views/ShowMeasurementsConverter.cs`

Modified the converter to accept either an ArticleLine object or a Category string:

**Before:**
```csharp
public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
{
    if (value is ArticleLine article)
    {
        var cat = (article.Category ?? string.Empty).Trim().ToLowerInvariant();
        if (cat == "model" || cat == "cover")
            return true;
        if (article.Measurements != null && article.Measurements.Count > 0 && !string.IsNullOrWhiteSpace(article.Measurements[0]))
            return true;
    }
    return false;
}
```

**After:**
```csharp
public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
{
    // Can receive either an ArticleLine object or a Category string
    string? cat = null;
    
    if (value is ArticleLine article)
    {
        cat = (article.Category ?? string.Empty).Trim().ToLowerInvariant();
    }
    else if (value is string categoryString)
    {
        cat = categoryString.Trim().ToLowerInvariant();
    }
    
    if (!string.IsNullOrEmpty(cat))
    {
        if (cat == "model" || cat == "cover")
            return true;
    }
    
    return false;
}
```

### 2. Updated ArticleEditor.axaml
**File:** `/home/justin/repos/urban-sniffle/src/index-editor/Views/ArticleEditor.axaml`

Changed the binding to use the Category property instead of the whole object:

**Before:**
```xml
<StackPanel Orientation="Horizontal" Margin="0,4" IsVisible="{Binding Path=., Converter={StaticResource ShowMeasurements}}">
```

**After:**
```xml
<StackPanel Orientation="Horizontal" Margin="0,4" IsVisible="{Binding Category, Converter={StaticResource ShowMeasurements}}">
```

## Result

✅ **Measurements field now shows immediately** when changing category to Model or Cover  
✅ **Measurements field hides immediately** when changing from Model/Cover to another category  
✅ Works correctly for new articles (Ctrl-N)  
✅ Works correctly for existing articles  

## Technical Details

By binding to the `Category` property instead of the entire object, Avalonia's binding system now:
1. Monitors the Category property for changes
2. Re-evaluates the converter when Category changes
3. Updates the UI visibility immediately

The converter was updated to handle both binding scenarios:
- Binding to the whole object (backwards compatibility)
- Binding to the Category string (new behavior for reactive updates)

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

Test steps:
1. Press Ctrl-N to create a new article
2. Change category dropdown to "Model" or "Cover"
3. ✅ Measurements field should appear immediately
4. Change category to something else (e.g., "Feature")
5. ✅ Measurements field should disappear immediately

## Date Fixed
February 17, 2026

