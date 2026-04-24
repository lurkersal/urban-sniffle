# Article Selection Crash Fix

## Issue
The application was crashing when clicking on an article in the list.

## Root Cause
The `ArticleEditor.axaml` file was referencing a converter resource `ShowAge` that was not defined in the UserControl.Resources section:

```xml
<StackPanel Orientation="Horizontal" Margin="0,4" IsVisible="{Binding Category, Converter={StaticResource ShowAge}}">
```

This XAML referenced `{StaticResource ShowAge}` but the converter was missing from the resources, causing a runtime exception when trying to render the article editor.

## Fix Applied

**File:** `/home/justin/repos/urban-sniffle/src/index-editor/Views/ArticleEditor.axaml`

Added the missing `ShowAgeCategoryConverter` to the UserControl.Resources section:

```xml
<UserControl.Resources>
    <!-- ...existing converters... -->
    <local:ShowAgeCategoryConverter x:Key="ShowAge" />
    <!-- ...rest of converters... -->
</UserControl.Resources>
```

## Explanation
During the previous fix for Wives category field display, we:
1. Created the `ShowAgeCategoryConverter.cs` file
2. Updated the XAML to use `{StaticResource ShowAge}` for the Age field visibility
3. BUT the resource registration got missed/reverted

The XAML was trying to use a converter that didn't exist in the resources, causing an exception when clicking any article that would trigger the article editor to render.

## Files Changed
- `/home/justin/repos/urban-sniffle/src/index-editor/Views/ArticleEditor.axaml` - Added ShowAge resource

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

Click on any article in the list - the article editor should now display without crashing.

## Date Fixed
February 17, 2025

