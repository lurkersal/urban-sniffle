# Article Editor Field Labels Fix

## Issue
The article editor pane was showing incorrect field labels for Humour and Wives categories:
- **Humour** articles were showing "Photographer:" instead of "Author:"
- **Wives** articles were not showing the model name field at all

## Root Cause
The UI converters that control field label display and field visibility in the article editor were not configured for these categories:

1. `FieldLabelConverter.cs` - Controls what label text is shown for the contributor field
2. `ShowContributorCategoryConverter.cs` - Controls whether the contributor field is visible
3. `ArticleCategoryDisplayConverter.cs` - Controls which fields are shown based on category

## Fix Applied

### 1. Updated FieldLabelConverter.cs
**File:** `/home/justin/repos/urban-sniffle/src/index-editor/Views/FieldLabelConverter.cs`

Added "humour" and "humor" to show "Author:" label:
```csharp
if (category == "motoring" || category == "feature" || category == "fiction" || 
    category == "review" || category == "humour" || category == "humor") 
    return "Author:";
```

**Note:** Initially added "wives" case to return "Model:" but this was removed because the contributor field is now hidden for Wives category (see change #2).

### 2. Updated ShowContributorCategoryConverter.cs
**File:** `/home/justin/repos/urban-sniffle/src/index-editor/Views/ShowContributorCategoryConverter.cs`

Initially added "wives" to show the contributor field, but this was **REMOVED** because Wives should only show the Model name field (ModelName0), not the contributor field. 

Also **REMOVED** "editorial" from the contributor categories list because Editorial articles should not show the photographer/contributor field at all.

The final version does NOT include "wives" or "editorial" in the contributor categories list.

### 3. Updated ArticleCategoryDisplayConverter.cs
**File:** `/home/justin/repos/urban-sniffle/src/index-editor/Views/ArticleCategoryDisplayConverter.cs`

Added "Wives" to return display mode 2 (shows Model name field):
```csharp
if (category.Equals("Cover", StringComparison.OrdinalIgnoreCase) || 
    category.Equals("Model", StringComparison.OrdinalIgnoreCase) ||
    category.Equals("Wives", StringComparison.OrdinalIgnoreCase))
    return 2;
```

### 4. Created ShowAgeCategoryConverter.cs
**File:** `/home/justin/repos/urban-sniffle/src/index-editor/Views/ShowAgeCategoryConverter.cs`

New converter to control Age field visibility - shows for Model/Cover but NOT for Wives:
```csharp
public class ShowAgeCategoryConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var cat = (value as string)?.Trim().ToLowerInvariant() ?? string.Empty;
        return cat == "model" || cat == "cover";
    }
}
```

### 5. Updated ArticleEditor.axaml
**File:** `/home/justin/repos/urban-sniffle/src/index-editor/Views/ArticleEditor.axaml`

- Added ShowAgeCategoryConverter to resources
- Changed Age field to use ShowAge converter instead of CategoryModeMatches
- This prevents the Age field from showing for Wives category

## Result

### Humour Category Articles
The article editor pane now shows:
- Title
- Category
- **Author:** (instead of Photographer:)
- Pages

### Wives Category Articles
The article editor pane now shows:
- Title
- Category
- **Model:** (model name field - bound to ModelName0)
- Measurements: (if applicable)
- Pages

**Note:** The Age field and duplicate Model/contributor field have been removed for Wives category.

### Editorial Category Articles
The article editor pane now shows:
- Title
- Category
- Pages

**Note:** The photographer/contributor field does not show for Editorial articles.

## Testing
After rebuilding:
```bash
cd /home/justin/repos/urban-sniffle
dotnet build src/index-editor/IndexEditor.csproj
```

Run the application and edit articles with Humour or Wives categories to verify the field labels are correct.

## Related Changes
- Card text display was previously fixed in `ArticleLine.cs` to show correct text in the article cards
- This fix addresses the editor pane field labels specifically

## Date Fixed
February 17, 2025

