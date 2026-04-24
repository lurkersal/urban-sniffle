# Group Category Age Field Fix

## Problem
Group articles in the article editor pane were missing the Age field.

## Root Cause
The Age field in `ArticleEditorView.axaml` was using `CategoryModeMatches` converter with parameter '2', which shows the field for ALL mode 2 categories (Model, Cover, Group, Wives, Letters). 

However, there was already a `ShowAgeCategoryConverter` that specifically determines which categories should show the Age field, but it only included Model and Cover - not Group. This converter existed but wasn't being used in the AXAML.

## Solution

### 1. Updated ShowAgeCategoryConverter.cs
Added "Group" to the list of categories that should show the Age field:

```csharp
// Only Model, Cover, and Group show age field
return cat == "model" || cat == "cover" || cat == "group";
```

### 2. Updated ArticleEditorView.axaml

**Added the converter to resources:**
```xml
<local:ShowAgeCategoryConverter x:Key="ShowAge" />
```

**Changed the Age field to use this converter:**
```xml
<!-- BEFORE -->
<StackPanel Orientation="Horizontal" IsVisible="{Binding SelectedArticle.Category, Converter={StaticResource CategoryModeMatches}, ConverterParameter='2'}">

<!-- AFTER -->
<StackPanel Orientation="Horizontal" IsVisible="{Binding SelectedArticle.Category, Converter={StaticResource ShowAge}}">
```

## Result

Now the Age field visibility is controlled by `ShowAgeCategoryConverter`, which:
- ✅ Shows Age for: Model, Cover, Group
- ✅ Hides Age for: Wives, Letters (even though they are mode 2 categories)

This ensures Group articles have all the same fields as Model and Cover articles.

## Testing

Created `ShowAgeCategoryConverterTests.cs` with test cases verifying:
- Age field shows for Model, Cover, and Group (case-insensitive)
- Age field does NOT show for Wives, Letters, or other categories

## Build Status
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

## Files Modified
1. `/home/justin/repos/urban-sniffle/src/index-editor/Views/ShowAgeCategoryConverter.cs`
2. `/home/justin/repos/urban-sniffle/src/index-editor/Views/ArticleEditorView.axaml`

## Files Created
1. `/home/justin/repos/urban-sniffle/src/index-editor/Tests/ShowAgeCategoryConverterTests.cs`

