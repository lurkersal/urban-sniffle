# Page Controller Article Display Fix

## Issue
When selecting a different article in the article list, the Page Controller would show inappropriate information. Specifically:
- User was viewing "Mandy" article (Model category, pages 44-51) with model name "Mandy CI-17-13" and photographer "Rupert Daines"
- User clicked on "Sacred Stiff" article (Fiction category, pages 52-54) with author "Less Russell"
- Page Controller showed "Mandy CI-17-13" alongside "Less Russell" for the Fiction article

### Example Data Issue
The JSON file contained incorrect data for the Fiction article:
```json
{
  "pages": [52, 53, 54],
  "category": "Fiction",
  "title": "Sacred Stiff",
  "modelNames": ["Mandy CI-17-13"],  // <-- WRONG! Fiction articles shouldn't have model names
  "ages": [null],
  "contributors": ["Less Russell"],
  "measurements": [""]
}
```

## Root Causes
1. **Display Logic Issue**: The `CreateArticleCard()` method in `PageControllerView.axaml.cs` was displaying `ModelName0` and `Age0` for ALL article categories, including Fiction, Review, Editorial, etc., which should not show model information.

2. **Data Entry Issue**: The editor was not properly clearing model-specific fields (ModelName, Age, Measurements) when switching between articles or when changing an article's category from Model/Cover/Group to Fiction/Review/Editorial.

## Solution
Modified the `CreateArticleCard()` method to only show model-specific information for appropriate categories:

**Categories that show Model Name and Age:**
- Model
- Cover
- Group
- Wives
- Interview

**Categories that do NOT show Model Name and Age:**
- Fiction
- Review
- Editorial
- Letters
- Humour
- Feature
- Index
- Cartoon

All categories continue to show the `Contributor0` field (as photographer, author, illustrator, etc., depending on context).

## Files Changed
- `/home/justin/repos/urban-sniffle/src/index-editor/Views/PageControllerView.axaml.cs`
  - Modified `CreateArticleCard()` method to add category-based field filtering (lines 273-296)

## Data Cleanup Required
The fix prevents the incorrect display, but the JSON data for "Sacred Stiff" (and potentially other Fiction/Review/Editorial articles) still contains incorrect `modelNames` data that should be cleaned up.

## Testing
To test this fix:
1. Launch IndexEditor
2. Load `/home/justin/Magazines/Club International/Club International 17-13, 1988`
3. Select the "Mandy" Model article - verify it shows "Mandy CI-17-13" and "Rupert Daines"
4. Select the "Sacred Stiff" Fiction article - verify it shows ONLY "Less Russell" (no model name)
5. Test with other categories (Review, Editorial, etc.) to ensure model info is not displayed
6. Test with Interview category to ensure model info IS displayed (since interviews can feature models)

## Date
March 4, 2026


