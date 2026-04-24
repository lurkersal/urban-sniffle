# IndexEditor - How to Set Article Thumbnail Page

**Feature:** Custom Thumbnail Page Selection  
**Location:** Page Navigation Pane (right side)  
**Version:** Updated 2026-04-24

## Quick Start

1. **Open an issue** in IndexEditor
2. **Select an article** from the article list
3. **Look for the "Thumbnail Page" panel** in the Page Navigation pane (right side)
4. **Click the checkbox** next to the page you want to use as the thumbnail
5. **Save the index file** (Ctrl+S)

## Step-by-Step Guide

### Step 1: Select an Article

In the main window, click on any article in the article list on the left side. The Article Editor panel will appear in the center showing the article's details.

### Step 2: Locate the Thumbnail Page Panel

The **"Thumbnail Page"** panel appears in the **Page Navigation pane** on the right side of the window:

**Position:** Between the page image and the page navigation controls at the bottom

**UI Layout:**
```
┌─────────────────────────────────────────────┐
│ Page Navigation Pane (Right Side)          │
├─────────────────────────────────────────────┤
│                                             │
│ [Page Image Display]                       │
│                                             │
├─────────────────────────────────────────────┤
│ Thumbnail Page:                             │
│ Select which page to use as the thumbnail   │
│ for this article:                           │
│                                             │
│ ☑ Page 45  ☐ Page 46  ☐ Page 47  ☐ Page 48│  ← Click to select
│                                             │
├─────────────────────────────────────────────┤
│         [◀]  [8]  [▶]                       │  ← Page controls
└─────────────────────────────────────────────┘
```

### Step 3: Select the Thumbnail Page

**To Set a Custom Thumbnail:**
1. Look at all the checkboxes shown (one for each page in the article)
2. Click the checkbox next to the page you want to use as the thumbnail
3. The previously selected checkbox will automatically uncheck
4. The change is saved immediately to the article

**Default Behavior:**
- The **first page** checkbox is checked by default
- Clicking the first page checkbox sets the thumbnail to "default" (uses first page)

### Step 4: Save the Index File

Press **Ctrl+S** (or use File → Save) to save your changes to `_index.json`.

## Field Behavior

### Checkbox Selection
- **One checkbox** is always selected (radio button behavior)
- **First page checkbox** = Uses default behavior (no thumbnailPage in JSON)
- **Any other page checkbox** = Sets that page as the thumbnail
- **Automatic unchecking** = Previous selection is cleared when you select a new page

### Visual Indicators
- **Panel visibility:** Only shown when an article is selected
- **Panel hidden:** When no article is selected or article has no pages
- **Checkboxes:** One for each page in the selected article's page list

### Auto-Update
The thumbnail page is updated immediately when you:
- Click any checkbox
- The article model is updated in real-time
- Save (Ctrl+S) writes the changes to `_index.json`

## Examples

### Example 1: Multi-Page Article

**Scenario:** Article has 4 pages, but you want page 3 as the thumbnail

**Article:**
- Title: "Beach Beauties"
- Pages: `10, 11, 12, 13`
- Current thumbnail: Page 10 (first page - checkbox selected by default)

**Action:**
1. Select the article
2. Look at the "Thumbnail Page" panel in the Page Navigation pane
3. Click the checkbox next to "Page 12"
4. Save (Ctrl+S)

**Result:** TheArchive will now display page 12 as the thumbnail instead of page 10

### Example 2: Cover Article

**Scenario:** Cover article starts on page 1 (full cover), but you want the inner spread as thumbnail

**Article:**
- Title: "Cover Feature - July Edition"
- Pages: `1, 2, 3, 4`
- Current thumbnail: Page 1 (magazine cover - first checkbox selected)

**Action:**
1. Select the article
2. In the "Thumbnail Page" panel, click the checkbox next to "Page 3"
3. Save (Ctrl+S)

**Result:** Thumbnail shows page 3 (the feature content) instead of the magazine cover

### Example 3: Reset to Default

**Scenario:** You previously set a custom thumbnail but want to go back to using the first page

**Article:**
- Title: "Summer Fun"
- Pages: `20, 21, 22`
- Thumbnail Page: `21` (Page 21 checkbox is selected)

**Action:**
1. Select the article
2. In the "Thumbnail Page" panel, click the checkbox next to "Page 20" (first page)
3. Save (Ctrl+S)

**Result:** Thumbnail reverts to page 20 (first page, default behavior)

## Visual Indicators

### Panel Appearance

**When Article Selected:**
```
┌─────────────────────────────────────────────┐
│ Thumbnail Page:                             │
│ Select which page to use as the thumbnail   │
│ for this article:                           │
│                                             │
│ ☑ Page 45  ☐ Page 46  ☐ Page 47  ☐ Page 48│
└─────────────────────────────────────────────┘
```

**When No Article Selected:**
- Panel is hidden completely

### Checkbox States
- **☑ Checked:** This page is the thumbnail (or first page for default)
- **☐ Unchecked:** This page is not the thumbnail
- **Only one** checkbox can be checked at a time

## Technical Details

### What Happens Behind the Scenes

1. **User Input:** You enter a page number
2. **Validation:** Field checks if input is a valid integer
3. **Update Model:** Sets `ArticleLine.ThumbnailPage` property
4. **Save to JSON:** Saves as `"thumbnailPage": 47` in `_index.json`
5. **Import:** Parser reads value during import
6. **Database:** Stores in `Article.ThumbnailPage` column
7. **Display:** TheArchive uses this page for thumbnails

### Code Reference

**UI Panel Definition** (`PageControllerView.axaml` lines 38-51):
```xml
<!-- Row 1: Thumbnail Page Selection Panel (shows when article is selected) -->
<Border Grid.Row="1" Background="#F8F8F8" BorderBrush="#CCCCCC" BorderThickness="0,1,0,1" Padding="12" Name="ThumbnailPanel" IsVisible="False">
    <StackPanel Spacing="8">
        <TextBlock Text="Thumbnail Page:" FontWeight="Bold" FontSize="14" />
        <TextBlock Name="ThumbnailInstructionText" Text="Select which page to use as the thumbnail for this article:" FontSize="12" Foreground="#666" TextWrapping="Wrap" />
        <ScrollViewer HorizontalScrollBarVisibility="Auto" VerticalScrollBarVisibility="Disabled" MaxHeight="100">
            <StackPanel Name="ThumbnailPagesContainer" Orientation="Horizontal" Spacing="4">
                <!-- Checkboxes for each page will be dynamically added here -->
            </StackPanel>
        </ScrollViewer>
    </StackPanel>
</Border>
```

**Panel Update Logic** (`PageControllerView.axaml.cs` lines 138-184):
```csharp
private void UpdateThumbnailPanel()
{
    // Get selected article
    var vm = this.DataContext as EditorStateViewModel;
    var selectedArticle = vm?.SelectedArticle;

    // Hide panel if no article selected
    if (selectedArticle == null || selectedArticle.Pages == null || selectedArticle.Pages.Count == 0)
    {
        thumbnailPanel.IsVisible = false;
        return;
    }

    // Show panel and create checkboxes for each page
    thumbnailPanel.IsVisible = true;
    thumbnailPagesContainer.Children.Clear();

    foreach (var page in selectedArticle.Pages)
    {
        var checkBox = new CheckBox
        {
            Content = $"Page {page}",
            IsChecked = (currentThumbnailPage == page) || (currentThumbnailPage == null && page == selectedArticle.Pages[0]),
            Tag = page
        };
        checkBox.Click += OnThumbnailPageCheckboxClicked;
        thumbnailPagesContainer.Children.Add(checkBox);
    }
}
```

**Checkbox Click Handler** (`PageControllerView.axaml.cs` lines 186-221):
```csharp
private void OnThumbnailPageCheckboxClicked(object? sender, RoutedEventArgs e)
{
    if (sender is not CheckBox clickedCheckbox) return;
    
    var selectedArticle = vm?.SelectedArticle;
    if (selectedArticle == null) return;

    int clickedPage = (int)(clickedCheckbox.Tag ?? 0);

    // Uncheck all other checkboxes
    foreach (var child in thumbnailPagesContainer.Children)
    {
        if (child is CheckBox cb && cb != clickedCheckbox)
        {
            cb.IsChecked = false;
        }
    }

    // Ensure clicked checkbox is checked
    clickedCheckbox.IsChecked = true;

    // Update the article's thumbnail page
    if (clickedPage == selectedArticle.Pages[0])
    {
        selectedArticle.ThumbnailPage = null; // Default to first page
    }
    else
    {
        selectedArticle.ThumbnailPage = clickedPage;
    }
}
```

## Common Questions

### Q: What happens if I select a different article?
**A:** The thumbnail panel updates to show the pages of the newly selected article. Each article maintains its own thumbnail page selection.

### Q: Can I select a page that doesn't exist in the article?
**A:** No. The panel only shows checkboxes for pages that are actually in the article's page list.

### Q: What if the article has many pages?
**A:** The checkboxes are displayed in a horizontal scrollable area with a maximum height of 100 pixels. You can scroll horizontally to see all pages.

### Q: What if I set the thumbnail page but the article doesn't have images on that page?
**A:** TheArchive will attempt to find the image for that page. If no image exists, the thumbnail will be empty/missing.

### Q: Do I need to re-import the issue after changing thumbnail pages?
**A:** Yes! Changes made in IndexEditor only affect the `_index.json` file. You must re-import the issue for changes to appear in TheArchive.

### Q: Can I set different thumbnail pages for the same article in different issues?
**A:** If an article appears in multiple issues (linked/reprinted), each issue's `_index.json` can have a different `thumbnailPage` value.

### Q: Does this affect the article viewer?
**A:** No. This only affects the thumbnail shown in the article list. When you open the article, it still shows all pages in order.

## Best Practices

### When to Use Custom Thumbnails

**Good Use Cases:**
- ✅ Cover articles where the cover isn't representative
- ✅ Multi-page articles where the best visual is on a later page
- ✅ Articles starting with title pages or text-heavy intro pages
- ✅ Articles where a specific page shows the main subject better

**Probably Don't Need:**
- ❌ Single-page articles (only one option anyway)
- ❌ Articles where the first page is already the best visual
- ❌ Text-only articles (no visual difference)

### Workflow Tips

1. **Visual Selection:** You can see all the page numbers at once - just click the one you want
2. **Immediate Feedback:** The checkbox updates instantly when clicked
3. **Save Immediately:** Press Ctrl+S to avoid losing changes
4. **Re-import:** Import the issue to see your changes in TheArchive

### Validation Checklist

Before saving, verify:
- ✅ The correct page checkbox is selected
- ✅ The page has an image (not a blank page)
- ✅ The image on that page is appropriate for a thumbnail
- ✅ You've saved the _index.json file

## Troubleshooting

### Problem: Panel doesn't appear
**Solution:** Make sure you have selected an article from the article list. The panel only appears when an article is selected.

### Problem: No checkboxes shown
**Solution:** The article may not have any pages set. Check the "Pages" field in the Article Editor to ensure pages are defined.

### Problem: Changes don't appear in TheArchive
**Solution:** 
1. Verify the checkbox is selected
2. Save the _index.json file (Ctrl+S)
3. Re-import the issue in TheArchive
4. Refresh the issue detail page

### Problem: Can't find the thumbnail panel
**Solution:** Make sure you're looking in the **Page Navigation pane** on the right side of the window, between the page image and the navigation controls.

### Problem: Multiple checkboxes are checked
**Solution:** This shouldn't happen. If it does, click the checkbox you want - all others will automatically uncheck.

## See Also

- **ARTICLE_THUMBNAIL_PAGE_FEATURE.md** - Technical implementation details
- **ARTICLE_THUMBNAIL_DISPLAY_COMPLETE.md** - Display logic documentation
- **_index.json format** - JSON schema including thumbnailPage field

---

**Last Updated:** 2026-04-24  
**Feature Status:** ✅ Fully Implemented and Working









