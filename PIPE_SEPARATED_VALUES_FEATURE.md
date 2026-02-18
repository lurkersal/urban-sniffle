# Pipe-Separated Values Feature

## Summary
The index-editor now supports pipe-separated (`|`) values for model names, ages, and measurements when there are multiple models in a single article.

## Changes Made

### 1. ArticleLine Model Updates (`src/common/Shared/ArticleLine.cs`)

#### ModelName0 Property
- **Before**: Supported only a single model name
- **After**: Supports pipe-separated model names (e.g., "Sarah|Jennifer|Amanda")
- On get: Returns all model names joined by `|`
- On set: Splits input by `|` and populates the ModelNames list

#### Age0 Property
- **Before**: Supported only a single age as `int?`
- **After**: Now returns a `string` with pipe-separated ages (e.g., "23|25|27")
- On get: Returns all ages joined by `|`
- On set: Splits input by `|`, parses each as integer, and populates the Ages list

#### Measurements0 Property
- **Before**: Supported only a single measurement string
- **After**: Supports pipe-separated measurements (e.g., "36B-28-38|34C-24-34|35D-26-36")
- On get: Returns all measurements joined by `|`
- On set: Splits input by `|`, normalizes each measurement, and populates the Measurements list

### 2. Validation Updates

#### ArticleLine.Validate() Method
- Added "group" to categories that support measurements
- Updated validation to check each pipe-separated measurement individually
- Error messages now indicate which measurement (by index) has an error
- Multiple measurement errors are joined with `;` separator

### 3. Group Category Support

The "Group" category is now treated the same as "Model" and "Cover" categories:

#### Updated Converters:
- `ShowMeasurementsConverter`: Shows measurement fields for Group category
- `ArticleCategoryDisplayConverter`: Returns mode 2 (Model/Cover fields) for Group
- `CoverModelCategoryConverter`: Returns true for Group category
- `ShowPhotographerCategoryConverter`: Shows photographer field for Group
- `ShowContributorCategoryConverter`: Shows contributor field for Group

### 4. UI Binding Changes

#### ArticleEditorView.axaml
- Changed Age field binding from `SelectedArticle.Ages[0]` to `SelectedArticle.Age0`
- Changed Model field binding from `SelectedArticle.ModelNames[0]` to `SelectedArticle.ModelName0`
- These changes allow the fields to accept pipe-separated values

When you select an article with category "Group", "Model", or "Cover", the editor pane now shows:
- **Model** field (supports pipe-separated names: `Sarah|Jennifer|Amanda`)
- **Age** field (supports pipe-separated ages: `23|25|27`)
- **Measurements** field (supports pipe-separated measurements: `36B-28-38|34C-24-34`)
- **Photographer** field
- **Pages** field

### 5. Display Updates

#### GetFormattedCardText() Method
- Updated to show pipe-separated values with ` | ` separator for better readability
- Group category now displays the same fields as Model/Cover:
  - Model names (pipe-separated)
  - Ages (pipe-separated)
  - Photographer
  - Measurements (pipe-separated)

## Usage Examples

### Single Model Article
```
Model: Sarah
Age: 23
Measurements: 36B-28-38
```

### Multiple Models Article (Group)
```
Model: Sarah|Jennifer|Amanda
Age: 23|25|27
Measurements: 36B-28-38|34C-24-34|35D-26-36
```

### Partial Information
You can also have different numbers of each field:
```
Model: Sarah|Jennifer
Age: 23|25
Measurements: 36B-28-38
```

## Validation

Each measurement in a pipe-separated list is validated individually. If any measurement is invalid, the error message will indicate which one:

```
Measurement 1: Invalid bust format (expected number optionally followed by cup letter); Measurement 3: Hip seems implausibly small relative to waist
```

## Notes

- Whitespace around the `|` separator is automatically trimmed
- Empty values between separators are filtered out
- The parser in `IndexFileParser` already supported pipe-separated values in the index file format
- This update brings the UI in line with the existing file format support

