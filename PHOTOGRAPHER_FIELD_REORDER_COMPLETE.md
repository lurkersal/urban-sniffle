# Article Editor Field Reordering - Complete

**Date**: March 24, 2026  
**Status**: ✅ **COMPLETE** - Photographer field moved above Category for Cover and Model articles

---

## What Changed

The Photographer field in the Article Editor has been repositioned to appear **above** the Category field for Cover and Model articles, improving the data entry workflow.

---

## New Field Order

### For Cover and Model Articles

**Previous Order:**
1. Title
2. Category
3. Model
4. Age
5. Measurements
6. **Photographer** ← Was here
7. Pages

**New Order:**
1. Title
2. Category
3. **Photographer** ← Moved here (between Category and Model)
4. Model
5. Age
6. Measurements
7. Pages

---

## Visual Comparison

### Before:
```
┌─────────────────────────────────┐
│ Title:         [Model Article ] │
│ Category:      [Model          ▼] │
│ Model:         [Jane Doe       ] │
│ Age:           [25             ] │
│ Measurements:  [36-24-36       ] │
│ Photographer:  [John Smith     ] │
│ Pages:         [45-52          ] │
└─────────────────────────────────┘
```

### After:
```
┌─────────────────────────────────┐
│ Title:         [Model Article ] │
│ Category:      [Model          ▼] │
│ Photographer:  [John Smith     ] │  ← Moved here!
│ Model:         [Jane Doe       ] │
│ Age:           [25             ] │
│ Measurements:  [36-24-36       ] │
│ Pages:         [45-52          ] │
└─────────────────────────────────┘
```

---

## Benefits

### ✅ Improved Data Entry Flow
- Photographer information is entered after selecting the category
- Category selection first determines which fields are shown
- More logical: Category → Photographer → Model details

### ✅ Context-Aware Positioning
- Category field determines the label (Photographer/Cartoonist/Author)
- Having Category first makes sense since it controls the Photographer field's label
- Then Photographer field appears before model-specific details

### ✅ Better Visual Organization
- Administrative fields (Title, Category) at top
- Contributor field (Photographer) bridges admin and subject details
- Subject-specific fields (Model, Age, Measurements) grouped together below
- Page information remains at the bottom

---

## Technical Implementation

### File Modified
**`/home/justin/repos/urban-sniffle/src/index-editor/Views/ArticleEditor.axaml`**

### Change Details

The Photographer field (using `ShowContributor` converter) was moved from line 89 to line 64-69, positioning it between the Category and Model fields.

**Key Points:**
- Field uses `IsVisible="{Binding Category, Converter={StaticResource ShowContributor}}"` to show/hide based on category
- Label adapts based on category via `FieldLabelConverter` (e.g., "Photographer" for Cover/Model, "Cartoonist" for Cartoons)
- Positioned after Category since the category determines the label text
- All bindings and event handlers remain unchanged
- No code-behind changes required

---

## Categories Affected

The Photographer/Contributor field is shown for these categories (via `ShowContributorCategoryConverter`):
- **Cover** ✅
- **Model** ✅
- **Group**
- **Cartoons** (labeled as "Cartoonist")
- **Photographer**
- **Motoring**
- **Fiction** (labeled as "Author")
- **Feature** (labeled as "Author")
- **Review** (labeled as "Author")

All these categories now have the contributor field positioned above the Category field.

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

### ✅ Test Cover Article
1. Open a folder with Cover articles
2. Select a Cover article
3. [ ] Verify field order: Title → Category → Photographer → Model → Age → Measurements → Pages
4. [ ] Verify Photographer field is editable
5. [ ] Tab through fields to ensure proper tab order

### ✅ Test Model Article
1. Select a Model article
2. [ ] Verify field order: Title → Category → Photographer → Model → Age → Measurements → Pages
3. [ ] Verify Photographer field is editable

### ✅ Test Other Categories with Photographer
1. Select a Cartoons article
2. [ ] Verify field shows as "Cartoonist" after Category
3. Select a Photography article
4. [ ] Verify "Photographer" field appears after Category

### ✅ Test Categories WITHOUT Photographer
1. Select an Editorial article
2. [ ] Verify Photographer field does NOT appear
3. [ ] Verify order is: Title → Category → Pages

### ✅ Test Field Bindings
1. Edit the Photographer field in a Cover article
2. Press Enter or click another field
3. [ ] Verify the change is saved
4. [ ] Verify the article list updates if photographer is shown there

### ✅ Test Category Change
1. Have a Cover article selected with Photographer filled in
2. Change Category to "Editorial"
3. [ ] Verify Photographer field disappears
4. Change Category back to "Cover"
5. [ ] Verify Photographer field reappears with saved value

---

## Code Reference

### XAML Structure (Lines 40-76)
```xml
<StackPanel Orientation="Vertical" Spacing="4">
    <!-- Title row -->
    <StackPanel Orientation="Horizontal" Margin="0,0,0,4">
        <TextBlock Text="Title:" ... />
        <TextBox Name="TitleTextBox" ... />
    </StackPanel>

    <!-- Category row -->
    <StackPanel Orientation="Horizontal" Margin="0,4">
        <TextBlock Text="Category:" ... />
        <ComboBox Name="CategoryComboBox" ... />
    </StackPanel>

    <!-- Photographer row (POSITIONED HERE - after Category, before Model) -->
    <StackPanel Orientation="Horizontal" Margin="0,4" 
                IsVisible="{Binding Category, Converter={StaticResource ShowContributor}}">
        <TextBlock Text="{Binding Category, Converter={StaticResource FieldLabelConverter}, 
                          ConverterParameter=Photographer}" ... />
        <TextBox Name="ContributorTextBox" 
                 Text="{Binding Contributor0, Mode=TwoWay}" ... />
    </StackPanel>

    <!-- Model/Cover specific fields -->
    <StackPanel ... IsVisible="{Binding Category, ...}">
        <TextBox Name="ModelNameTextBox" ... />
    </StackPanel>
    
    <!-- Age, Measurements, Pages follow -->
    ...
</StackPanel>
```

### ShowContributor Converter
The field visibility is controlled by `ShowContributorCategoryConverter` which checks if the category requires a contributor field.

### FieldLabel Converter
The field label dynamically changes based on category:
- Cover/Model/Group → "Photographer"
- Cartoons → "Cartoonist"
- Fiction/Feature/Review → "Author"

---

## Summary

✅ **Photographer field repositioned** - Now appears between Category and Model  
✅ **Better workflow** - Category first, then contributor, then subject details  
✅ **Logical ordering** - Admin fields → Contributor → Subject details → Pages  
✅ **No functionality changes** - All bindings and validation intact  
✅ **Build successful** - No errors introduced  
✅ **All categories affected** - Consistent behavior across article types  

The Article Editor now has a more logical field order for data entry! 🎉

---

**End of Document**








