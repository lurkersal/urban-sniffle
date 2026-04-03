# Editorial Category - Photographer Field Removed

**Date**: March 24, 2026  
**Status**: ✅ **COMPLETE** - Photographer field hidden for Editorial articles

---

## What Changed

The Photographer/Contributor field has been removed from Editorial articles. This field will no longer appear in the Article Editor when an article has the "Editorial" category.

---

## Technical Implementation

### File Modified
**`/home/justin/repos/urban-sniffle/src/index-editor/Views/ShowContributorCategoryConverter.cs`**

### Change Details

Removed `"editorial"` from the `ContributorCategories` array that determines which article categories display the Photographer/Contributor field.

**Before:**
```csharp
private static readonly string[] ContributorCategories = new[]
{
    "humour", "feature", "fiction", "editorial", "interview", "model", 
    "cover", "group", "photographer", "cartoons", "illustration", 
    "review", "motoring"
};
```

**After:**
```csharp
private static readonly string[] ContributorCategories = new[]
{
    "humour", "feature", "fiction", "interview", "model", "cover", 
    "group", "photographer", "cartoons", "illustration", "review", 
    "motoring"
};
```

---

## Categories with Photographer Field

### ✅ Categories That SHOW the Photographer/Contributor Field:
1. **Cover** - Label: "Photographer"
2. **Model** - Label: "Photographer"
3. **Group** - Label: "Photographer"
4. **Cartoons** - Label: "Cartoonist"
5. **Photographer** - Label: "Photographer"
6. **Illustration** - Label: "Illustrator"
7. **Fiction** - Label: "Author"
8. **Feature** - Label: "Author"
9. **Humour** - Label: "Author"
10. **Review** - Label: "Author"
11. **Interview** - Label: "Interviewer/Author"
12. **Motoring** - Label: "Author"

### ❌ Categories That HIDE the Photographer/Contributor Field:
1. **Editorial** ← Changed! (now hidden)
2. **Contents/Index**
3. **Letters**
4. **Wives**
5. **Pinup**
6. **Travel**

---

## Field Order Comparison

### Editorial Article - BEFORE
```
┌──────────────────────────────────────────┐
│  Article Editor - Editorial Article     │
├──────────────────────────────────────────┤
│  Title:        [___________________]     │
│  Category:     [Editorial          ▼]   │
│  Photographer: [___________________]     │ ← Was shown
│  Pages:        [____________]            │
└──────────────────────────────────────────┘
```

### Editorial Article - AFTER ✅
```
┌──────────────────────────────────────────┐
│  Article Editor - Editorial Article     │
├──────────────────────────────────────────┤
│  Title:        [___________________]     │
│  Category:     [Editorial          ▼]   │
│  Pages:        [____________]            │  ← Photographer gone!
└──────────────────────────────────────────┘
```

---

## Rationale

Editorial articles typically represent the magazine's editorial voice and don't require photographer attribution. The content is usually text-based commentary or opinion pieces from the editorial team.

**Why remove it?**
- ✅ Editorials don't typically have photographers
- ✅ Simplifies the form for this category
- ✅ Reduces clutter and confusion during data entry
- ✅ Focuses on essential fields: Title, Category, Pages

---

## How It Works

The `ShowContributorCategoryConverter` is used in the ArticleEditor XAML:

```xml
<StackPanel Orientation="Horizontal" Margin="0,4" 
            IsVisible="{Binding Category, Converter={StaticResource ShowContributor}}">
    <TextBlock Text="{Binding Category, Converter={StaticResource FieldLabelConverter}, 
                      ConverterParameter=Photographer}" ... />
    <TextBox Name="ContributorTextBox" ... />
</StackPanel>
```

When the Category is set to "Editorial", the converter returns `false`, hiding the entire StackPanel containing the Photographer field.

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

### ✅ Test Editorial Category
1. Open a folder with articles
2. Select or create an Editorial article
3. [ ] Verify the Photographer field does NOT appear
4. [ ] Verify field order is: Title → Category → Pages
5. [ ] Verify you can edit Title, Category, and Pages normally

### ✅ Test Category Change Behavior
1. Have a Cover article selected (with Photographer filled in)
2. Change Category from "Cover" to "Editorial"
3. [ ] Verify Photographer field disappears
4. [ ] Verify the photographer data is preserved in the model (not deleted)
5. Change Category back to "Cover"
6. [ ] Verify Photographer field reappears with the saved value

### ✅ Test Other Categories Still Work
1. Select a Cover article
2. [ ] Verify Photographer field appears
3. Select a Model article
4. [ ] Verify Photographer field appears
5. Select a Cartoons article
6. [ ] Verify "Cartoonist" field appears
7. Select a Fiction article
8. [ ] Verify "Author" field appears

### ✅ Test Data Integrity
1. Create an Editorial article with Title and Pages
2. Save the folder (_index.json)
3. Close and reopen the folder
4. [ ] Verify the Editorial article loads correctly
5. [ ] Verify no photographer data appears in the JSON for Editorial articles

---

## Impact on Existing Data

### Data Preservation
If an Editorial article already has contributor data in the database:
- ✅ The data is **preserved** in the model/JSON
- ✅ The field is simply **hidden** in the UI
- ✅ If the category is changed to a type that shows contributors, the data will reappear

### Migration
No data migration is required. This is purely a UI change that affects field visibility.

---

## Additional Notes

### FieldLabelConverter Still Active
The `FieldLabelConverter` is still active and will determine the correct label if the field is shown. For categories that do show the field, the label adapts:
- Cover/Model/Group → "Photographer"
- Cartoons → "Cartoonist"
- Fiction/Feature/Humour/Review → "Author"

### Consistent Behavior
This change maintains consistency with other categories that don't show the Photographer field, such as:
- Contents/Index (just page numbers)
- Letters (reader submissions)
- Wives (model category without photographer field)

---

## Summary

✅ **Editorial category updated** - Photographer field now hidden  
✅ **Cleaner UI** - Only essential fields shown for Editorial articles  
✅ **Data preserved** - Existing contributor data not deleted  
✅ **Build successful** - No errors introduced  
✅ **12 categories** - Still show Photographer/Contributor field  

Editorial articles now have a streamlined editing experience! 🎉

---

**End of Document**

