# Article Thumbnails Feature

## Summary
Added thumbnail images to the article list on issue detail pages (e.g., http://localhost:5163/issues/1). Each article row now displays a small thumbnail of the first image from that article, positioned to the right of the article title without increasing the row height. The entire article row is clickable to open the spread viewer.

## Changes Made

### 1. Article Model Update (`the-archive/src/TheArchive/Models/Article.cs`)
Added `FirstImagePath` property to store the path to the first image in each article:
```csharp
public string? FirstImagePath { get; set; }
```

### 2. Database Query Update (`the-archive/src/TheArchive/Services/ArchiveDatabase.cs`)
Modified `GetArticlesByIssueAsync` to include the first image path for each article:
- Added LEFT JOIN to get the first image from Content table
- Uses `DISTINCT ON (c2.ArticleId)` to get only the first image per article
- Orders by Page to ensure it's the first page's image
- Added `FirstImagePath` to SELECT and GROUP BY clauses

### 3. View Update (`the-archive/src/TheArchive/Views/Issues/Detail.cshtml`)
Modified the article row layout:
- Removed the "View Spread" button
- Made the entire article row clickable by adding `onclick="openSpread(@article.PageStart, @ViewBag.Issue.IssueId)"` to the row
- Added `event.stopPropagation()` to the model link to prevent triggering the row click when clicking the link
- Thumbnail is positioned between the article body and the end of the row
- Thumbnail only displays if `FirstImagePath` is not null and category is Cover, Model, Group, or Feature

### 4. CSS Styling (`the-archive/src/TheArchive/wwwroot/css/archive.css`)
Updated grid layout and thumbnail styles:
```css
.article-row {
    display: grid;
    grid-template-columns: 48px 1fr 64px;
    /* columns: page number, article body, thumbnail */
    cursor: pointer;
}

.article-row:hover {
    background: rgba(245, 240, 232, 0.06);
}

.article-thumb {
    width: 64px;
    height: 64px;
    background-size: cover;
    background-position: center;
    background-repeat: no-repeat;
    border-radius: 2px;
    border: 0.5px solid rgba(245, 240, 232, 0.15);
    background-color: rgba(245, 240, 232, 0.05);
}
```

## Design Details
- **Thumbnail size**: 64x64 pixels
- **Position**: Third column in the grid, at the end of each row
- **Interaction**: Entire row is clickable to open the spread viewer at the article's starting page
- **Model links**: Clicking a model name opens the model detail page without triggering the spread viewer
- **Behavior**: Thumbnail only displays if the article has an image and is in a visual category (Cover, Model, Group, Feature)
- **Layout**: Uses CSS Grid with 3 columns: page number (48px), article body (flexible), thumbnail (64px)
- **Styling**: Subtle border and background color for consistency with the design theme
- **Hover effect**: Row background brightens slightly to indicate it's clickable

## Benefits
- Quick visual preview of article content
- Helps users identify articles more easily
- Simplified interaction - click anywhere on the row to view
- Cleaner interface without multiple buttons
- Maintains smooth scrolling performance

## Date
April 3, 2026


