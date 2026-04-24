# Interview Article Fields Fix - Complete

## Summary
Updated Interview article category to show "Author" instead of "Photographer" and added Model field support in the article editor.

## Implementation Date
February 27, 2026

## Changes Made

### 1. FieldLabelConverter.cs - Label Display
**File:** `src/index-editor/Views/FieldLabelConverter.cs`

**Changes:**
- Added "interview" to the list of categories that show "Author:" label instead of "Photographer:"
- The contributor field label now displays as "Author:" for Interview articles (along with Motoring, Feature, Fiction, Review, and Humour)

```csharp
case "photographer":
    if (category == "cartoons") return "Cartoonist:";
    if (category == "motoring" || category == "feature" || category == "fiction" || 
        category == "review" || category == "humour" || category == "humor" || 
        category == "interview") return "Author:";
    return "Photographer:";
```

### 2. ArticleCategoryDisplayConverter.cs - Field Visibility
**File:** `src/index-editor/Views/ArticleCategoryDisplayConverter.cs`

**Changes:**
- Added "interview" to return display mode 2
- Display mode 2 shows Model, Age, Measurements, and Contributor fields
- Interview articles now show the same field set as Model, Cover, Group, Wives, and Letters categories

```csharp
if (category.Equals("Cover", StringComparison.OrdinalIgnoreCase) || 
    category.Equals("Model", StringComparison.OrdinalIgnoreCase) ||
    category.Equals("Group", StringComparison.OrdinalIgnoreCase) ||
    category.Equals("Wives", StringComparison.OrdinalIgnoreCase) ||
    category.Equals("Letters", StringComparison.OrdinalIgnoreCase) ||
    category.Equals("Interview", StringComparison.OrdinalIgnoreCase))
    return 2;
```

### 3. ArticleLine.cs - Article List Display
**File:** `src/common/Shared/ArticleLine.cs`

**Changes:**
- Added Interview category formatting in `GetFormattedCardText()` method
- Interview articles in the article list now display: Category, Pages, Title, Model, and Author

```csharp
// Interview: show author and model
if (cat == "interview")
    return $"{categoryText}\n{pagesText}\nTitle: {Title}\nModel: {string.Join(" | ", ModelNames)}\nAuthor: {string.Join(", ", Contributors)}";
```

## Behavior

### Article Editor Card (Right Pane)
When an Interview article is selected or created:
- **Title** field: Editable text box
- **Category** field: Dropdown showing "Interview"
- **Model** field: Editable text box (supports pipe-separated values: Model1 | Model2)
- **Age** field: Editable text box (optional, supports pipe-separated values)
- **Measurements** field: Editable text box (optional, format: bust(+cup)-waist-hip)
- **Author** field: Editable text box (label shows "Author:" not "Photographer:")
- **Pages** field: Editable text box (comma-separated page numbers or ranges)

### Article List Card (Left Pane)
Interview articles display in the article list with:
```
Category: Interview
Pages: [page numbers]
Title: [article title]
Model: [model name(s)]
Author: [author name(s)]
```

## Field Mapping
- **Author field** → Stored in `Contributors` list (field 6 in _index.txt)
- **Model field** → Stored in `ModelNames` list (field 4 in _index.txt)
- **Age field** → Stored in `Ages` list (field 5 in _index.txt)
- **Measurements field** → Stored in `Measurements` list (field 7 in _index.txt)

## Files Modified

1. `src/index-editor/Views/FieldLabelConverter.cs`
2. `src/index-editor/Views/ArticleCategoryDisplayConverter.cs`
3. `src/common/Shared/ArticleLine.cs`

## Build Status

✅ **Build Successful**
```
Build succeeded.
    69 Warning(s)
    0 Error(s)
```

All warnings are pre-existing and not related to these changes.

## Testing Recommendations

1. Open index-editor and create a new article (Ctrl+N)
2. Set the category to "Interview"
3. Verify the editor shows:
   - Title field
   - Model field
   - Age field (optional)
   - Measurements field (optional)
   - Author field (NOT Photographer)
   - Pages field
4. Fill in the fields and save
5. Verify the article displays correctly in the article list with Author and Model
6. Test pipe-separated values for Model field (e.g., "John Doe | Jane Smith")
7. Verify existing Interview articles load correctly if any exist in your data

## Related Categories

Interview articles now share the same field configuration as:
- Model
- Cover
- Group
- Wives
- Letters

But with "Author:" label instead of "Photographer:" label.

## Notes

- The Model field supports pipe-separated values (|) for multiple models
- The Age field is optional and also supports pipe-separated values
- The Measurements field is optional with format validation (bust-waist-hip or bust+cup-waist-hip)
- The Author field uses the unified Contributors list internally
- All fields are saved in the canonical 7-field format in _index.txt

