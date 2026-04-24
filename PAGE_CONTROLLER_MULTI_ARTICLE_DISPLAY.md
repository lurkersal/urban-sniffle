# Page Controller Multi-Article Display Enhancement

## Summary
Enhanced the Page Controller to display multiple article cards when a page belongs to more than one article, and reorganized the layout to place navigation controls at the bottom.

## Changes Made

### Layout Restructure

**New Order (Top to Bottom):**
1. **Article Cards** - Stacked at the top (one or more)
2. **Page Image** - Middle section
3. **Page Navigation Controls** - Bottom (arrows and page number)

**Previous Order:**
1. Article Info (single card)
2. Page Navigation Controls
3. Page Image

### File: `src/index-editor/Views/PageControllerView.axaml`

**Major Changes:**
- Replaced single `CurrentArticleInfo` Border with `ArticleCardsContainer` StackPanel
- Moved page navigation controls (PrevPageBtn, PageInput, NextPageBtn) to bottom
- Simplified structure - article cards are now dynamically created in code

**Layout Structure:**
```xml
<StackPanel>
    <StackPanel Name="ArticleCardsContainer">
        <!-- Dynamic article cards added here -->
    </StackPanel>
    
    <Border Name="PageImageBorder">
        <!-- Page image display -->
    </Border>
    
    <StackPanel> <!-- Navigation controls -->
        <Button Name="PrevPageBtn" />
        <TextBox Name="PageInput" />
        <Button Name="NextPageBtn" />
    </StackPanel>
</StackPanel>
```

### File: `src/index-editor/Views/PageControllerView.axaml.cs`

**Rewritten Method: `UpdateCurrentArticleDisplay()`**
- Changed from finding single article to finding **all articles** containing the current page
- Clears and rebuilds article cards dynamically
- Creates one card per article
- Shows nothing if page belongs to no articles

**New Method: `CreateArticleCard(ArticleLine article)`**
- Programmatically creates a visual card for an article
- Builds Border → Grid → (ColorBar | Content | Category)
- Uses same visual design as before:
  - 16px colored vertical bar (category-specific at 60% opacity)
  - Bold title (16px)
  - Details text (model, age, contributor at 12px)
  - Category label (13px bold)
  - Light gray background (#F8F8F8)

**Card Structure:**
```
┌────────────────────────────────────────┐
│ ╔══╗ Title                    Category │
│ ║██║ Details (model • age • photo)     │
│ ╚══╝                                   │
└────────────────────────────────────────┘
```

## Features

### Multiple Article Support

**When a page belongs to 1 article:**
- Shows single article card at top
- Behaves as before

**When a page belongs to 2+ articles:**
- Stacks multiple article cards vertically
- Each card has 8px spacing between them
- All cards use the same visual design
- Each shows its own category color

**When a page belongs to 0 articles:**
- No cards shown
- Page image and navigation still visible

### Visual Consistency

Each article card displays:
- **Colored bar**: Category-specific color at 60% opacity
- **Title**: Article title (or category if no title)
- **Details**: Model name, age, contributor (bullet-separated)
- **Category**: Category name in bold

### Use Cases

This enhancement helps with:
1. **Index/Contents pages** - Often belong to multiple logical sections
2. **Cover pages** - May be referenced in multiple articles
3. **Multi-part features** - Pages that span article boundaries
4. **Editorial layouts** - Shared pages between sections

## Layout Benefits

### Bottom Navigation
- **More natural reading flow** - View articles → see image → navigate
- **Less scrolling needed** - Navigation always visible at bottom
- **Image prominence** - Central focus of the pane
- **Touch-friendly** - Navigation at bottom easier to reach

### Stacked Articles
- **Clear context** - Immediately see all articles for current page
- **No ambiguity** - Users know when pages are shared
- **Better workflow** - Can see all relevant article info at once

## Visual Example

```
┌─────────────────────────────────────────────────────┐
│ Page Controller                                     │
├─────────────────────────────────────────────────────┤
│                                                     │
│ ╔══╗ Cover Article                      Cover      │
│ ║██║ January 2026 Issue                            │
│ ╚══╝                                               │
│                                                     │
│ ╔══╗ Index                               Index     │
│ ║██║ Magazine Contents                             │
│ ╚══╝                                               │
│                                                     │
│ ┌─────────────────────────────────────────────┐   │
│ │                                             │   │
│ │                                             │   │
│ │              [Page Image]                   │   │
│ │                                             │   │
│ │                                             │   │
│ └─────────────────────────────────────────────┘   │
│                                                     │
│              ◀    [  42  ]    ▶                    │
│                                                     │
└─────────────────────────────────────────────────────┘
```

## Build Status

✅ **Build Successful** - 0 errors, only pre-existing warnings

## Testing Recommendations

1. **Single Article Page**
   - Navigate to page in one article
   - Verify single card displays correctly
   - Check navigation works

2. **Multiple Article Page**
   - Find/create page in 2+ articles
   - Verify multiple cards stack properly
   - Check each card shows correct info

3. **Unassigned Page**
   - Navigate to page not in any article
   - Verify no cards shown
   - Check image and navigation still work

4. **Navigation Position**
   - Verify controls are at bottom
   - Test arrow buttons work
   - Test page number entry works

5. **Color Accuracy**
   - Check each card's color bar matches its category
   - Verify opacity is correct (60%)

## Code Quality

- **Dynamic UI generation** - Cards created on-demand
- **Memory efficient** - Old cards cleared before creating new ones
- **Exception handling** - All UI operations wrapped in try-catch
- **Consistent styling** - Uses same color converter as Article List
- **Maintainable** - Card creation logic in separate method

## Future Enhancements

Potential improvements:
- Click card to select that article in Article List
- Highlight active/selected article card
- Show page position within each article (e.g., "Page 2 of 5")
- Context menu on cards (Edit, Remove page, etc.)
- Drag-and-drop between cards to move pages
- Collapse/expand cards when many articles exist

