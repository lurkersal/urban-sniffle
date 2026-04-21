# Page Links Dropdown Feature

**Date**: April 22, 2026  
**Status**: ✅ **COMPLETED** - Page links dropdown in page viewer

---

## Summary

Added functionality to display page links from `_index.json` files in the page viewer overlay. When viewing pages that have links to other magazine issues, a dropdown menu appears allowing users to select and open those linked issues in a new tab.

---

## Features

### 1. Page Links Dropdown

- **Location**: Page viewer navigation bar (spread overlay), next to Prev/Next buttons
- **Visibility**: Only shown when current page(s) have links in `_index.json`
- **Behavior**: Opens linked issue in new tab when selected

### 2. Smart Filtering

- **Single Page View**: Shows links only for the current page
- **Spread View**: Shows links for both visible pages (left and right)
- **Dynamic Updates**: Dropdown updates when navigating between pages

### 3. Link Resolution

- Resolves magazine name, volume, and issue number to database IssueId
- Opens issue detail page in new tab: `/issues/{issueId}`
- Displays user-friendly error messages if issue not found

---

## Implementation Details

### Files Modified

1. **`_Layout.cshtml`** - Page viewer overlay
   - Added page links dropdown HTML (line 59-61)
   - Added `loadPageLinks()` function to fetch links from API
   - Added `handleLinkSelection()` function to handle dropdown selection
   - Added `updatePageLinksDropdown()` function to filter and display relevant links
   - Integrated link loading into `openArticle()` and `openIssuePages()` functions

2. **`IndexEditorController.cs`** - API controller
   - Added `GET /index-editor/{issueId}/links` endpoint
   - Returns array of page links from `_index.json`

3. **`Api/IssuesController.cs`** - API controller
   - Existing `GET /api/v1/issues/find` endpoint
   - Resolves magazine/volume/number to IssueId

4. **`ArchiveDatabase.cs`** - Database service
   - Existing `FindIssueByMagazineVolNoAsync()` method
   - Queries Issue and Magazine tables

5. **`archive.css`** - Stylesheet
   - Added `.page-links-dropdown` styling (lines 939-960)

---

## How It Works

### Loading Links

When the page viewer opens (either for an article or full issue):

1. **Fetch Links**: Parallel request to `/index-editor/{issueId}/links`
2. **Store Links**: Saved in `currentPageLinks` array
3. **Update Dropdown**: Call `updatePageLinksDropdown()` to show/hide and populate

```javascript
const [pagesResponse, links] = await Promise.all([
    fetch(`/api/v1/pages/article/${articleId}`),
    loadPageLinks(issueId)
]);

currentPageLinks = links;
updatePageLinksDropdown();
```

### Filtering Links

The `updatePageLinksDropdown()` function:

1. Gets current page(s) being displayed
2. Checks if showing a spread (two consecutive pages)
3. Filters links to only those for visible pages
4. Populates dropdown with relevant links
5. Shows/hides dropdown based on whether links exist

```javascript
const visiblePages = [currentPageData.page];
if (isSpread) {
    visiblePages.push(nextPageData.page);
}

const relevantLinks = currentPageLinks.filter(link => 
    visiblePages.includes(link.page)
);
```

### Opening Linked Issues

When user selects a link from the dropdown:

1. **Parse Link Data**: Extract magazine, volume, issue from dropdown value
2. **Resolve Issue ID**: Call `/api/v1/issues/find` with magazine/volume/number
3. **Open in New Tab**: Navigate to `/issues/{issueId}` in new browser tab
4. **Reset Dropdown**: Clear selection back to placeholder

```javascript
const linkData = JSON.parse(selectedValue);
const response = await fetch(
    `/api/v1/issues/find?magazine=${encodeURIComponent(linkData.magazine)}&volume=${linkData.volume}&number=${linkData.issue}`
);

const issue = await response.json();
window.open(`/issues/${issue.issueId}`, '_blank');
```

---

## User Experience

### Viewing Pages with Links

When viewing a page with links to other issues:

```
┌────────────────────────────────────────────────┐
│                                                │
│  ← Issue  Page 42                              │
│                                                │
│  [Prev]  [Next]  [Page 42 → Playboy Vol.45... ▼]  ✕│
│                                                │
│  ╔════════════╗  ╔════════════╗               │
│  ║            ║  ║            ║               │
│  ║  Page 42   ║  ║  Page 43   ║               │
│  ║            ║  ║            ║               │
│  ╚════════════╝  ╚════════════╝               │
└────────────────────────────────────────────────┘
```

### Dropdown Options

Each option shows:
- **Source Page**: The page number that contains the link
- **Target Magazine**: Magazine name
- **Target Volume**: Volume number
- **Target Issue**: Issue/number

Format: `Page {page} → {magazine} Vol.{volume} #{issue}`

Example: `Page 42 → Playboy Vol.45 #12`

### Navigation Flow

1. User opens page viewer (clicks on article or issue page)
2. Pages load and dropdown appears if links exist
3. User clicks dropdown to see available links
4. User selects a link
5. New tab opens with the linked issue
6. Dropdown resets to placeholder
7. User can continue viewing current pages or select another link

---

## API Endpoints

### GET /index-editor/{issueId}/links

**Purpose**: Load page links from _index.json file

**Response Success** (200):
```json
{
  "links": [
    {
      "page": 42,
      "magazine": "Playboy",
      "volume": "45",
      "issue": "12"
    },
    {
      "page": 43,
      "magazine": "Penthouse",
      "volume": "30",
      "issue": "5"
    }
  ]
}
```

**Response Empty** (200):
```json
{
  "links": []
}
```

**Response Error** (404):
```json
{
  "error": "_index.json not found"
}
```

### GET /api/v1/issues/find

**Purpose**: Find an issue by magazine name, volume, and number

**Query Parameters**:
- `magazine` - Magazine name (case-insensitive)
- `volume` - Volume number/string
- `number` - Issue number/string

**Response Success** (200):
```json
{
  "issueId": 123,
  "magazineId": 1,
  "magazineName": "Playboy",
  "volume": "45",
  "number": "12",
  "year": 1998,
  "articleCount": 15,
  "pageCount": 150,
  "coverImagePath": "playboy/1998/1998-12/page-001.jpg"
}
```

**Response Error** (404):
```json
{
  "error": "Issue not found: Playboy Vol.45 #12"
}
```

**Response Error** (400):
```json
{
  "error": "Magazine, volume, and number are required"
}
```

---

## Data Format

### Links in _index.json

Links are stored in the `_index.json` file at the root level:

```json
{
  "metadata": {
    "magazine": "Playboy",
    "volume": "46",
    "number": "03",
    "year": "1999"
  },
  "articles": [...],
  "links": [
    {
      "page": 42,
      "magazine": "Playboy",
      "volume": "45",
      "issue": "12"
    },
    {
      "page": 55,
      "magazine": "Penthouse",
      "volume": "30",
      "issue": "5"
    }
  ]
}
```

### Link Properties

- **page** (int): The page number in the current issue that contains the link
- **magazine** (string): Name of the linked magazine
- **volume** (string): Volume number of the linked issue
- **issue** (string): Issue/number of the linked issue

---

## Visual Design

### Dropdown Styling

```css
.page-links-dropdown {
    background: rgba(245, 240, 232, 0.08);
    border: 0.5px solid rgba(245, 240, 232, 0.2);
    border-radius: 4px;
    color: var(--paper);
    font-size: 12px;
    font-family: var(--sans);
    padding: 6px 12px;
    cursor: pointer;
    transition: background 0.15s;
    max-width: 300px;
    margin-left: 8px;
}
```

- **Consistent Theme**: Matches existing button styling
- **Subtle Appearance**: Semi-transparent background, subtle border
- **Readable**: Paper color text, sans-serif font
- **Compact**: 12px font, 300px max width
- **Smooth Interaction**: Transition on hover

---

## Error Handling

### Missing _index.json

- **Scenario**: Issue has no _index.json file
- **Behavior**: API returns empty links array
- **UI Response**: Dropdown remains hidden

### Invalid Links Array

- **Scenario**: _index.json has no "links" property
- **Behavior**: API returns empty links array
- **UI Response**: Dropdown remains hidden

### Issue Not Found

- **Scenario**: Link points to magazine/volume/issue not in database
- **Behavior**: API returns 404 error
- **UI Response**: Toast notification: "Linked issue not found in database"

### Network Errors

- **Scenario**: API request fails
- **Behavior**: Promise rejection
- **UI Response**: Toast notification with error message

---

## Benefits

✅ **Contextual Navigation** - View related issues mentioned on current page  
✅ **Efficient Workflow** - Open linked issues without leaving page viewer  
✅ **New Tab Navigation** - Original page remains open for reference  
✅ **Smart Filtering** - Only shows links relevant to visible pages  
✅ **Clear Presentation** - Shows page number and full issue reference  
✅ **Graceful Degradation** - Hidden when no links exist  

---

## Technical Notes

### Link Resolution Strategy

The system uses a two-step process:

1. **Store Reference**: `_index.json` stores human-readable magazine/volume/issue
2. **Resolve on Demand**: When user clicks, query database to find IssueId

**Why not store IssueId directly?**
- Magazine/volume/issue is more portable
- Survives database rebuilds
- Matches how users think about issues
- Can link to issues not yet in database

### Spread Detection

The system determines if showing a spread using:

```javascript
const isSpread = nextPageData && 
    (nextPageData.page === currentPageData.page + 1) &&
    (currentPageData.page % 2 === 0) && 
    (nextPageData.page % 2 === 1);
```

This ensures:
- Pages are consecutive (42, 43)
- First page is even (left side)
- Second page is odd (right side)
- Matches physical magazine layout

### Performance Considerations

- **Parallel Loading**: Links load in parallel with page images
- **Cached Issue Data**: Page images cached, but links reloaded per issue
- **Minimal Filtering**: Simple array filter operation
- **Lazy Resolution**: IssueId only resolved when user selects link

---

## Future Enhancements

### Potential Improvements

1. **Preview on Hover** - Show cover thumbnail when hovering over link
2. **Bi-directional Links** - Show "linked from" on target issue
3. **Link Validation** - Warn if linked issue not in database
4. **Keyboard Navigation** - Use number keys to select links
5. **Link Indicators** - Visual badge on pages with links
6. **Link History** - Track which links user has followed
7. **Bulk Link Discovery** - Scan all pages for potential links

### Integration Ideas

1. **Link Editor** - UI to add/edit links in index editor
2. **Auto-detection** - OCR to find issue references in pages
3. **Link Statistics** - Show most linked issues
4. **Link Graph** - Visualize issue connections

---

## Testing

### Manual Testing Checklist

- [x] Navigate to issue with links in _index.json
- [x] Open page viewer (click article)
- [x] Verify dropdown appears for pages with links
- [x] Verify dropdown hidden for pages without links
- [x] Select link from dropdown
- [x] Verify new tab opens with correct issue
- [x] Verify dropdown resets after selection
- [x] Navigate to next page
- [x] Verify dropdown updates for new page
- [x] Test spread view (shows links for both pages)
- [x] Test single page view (shows links for one page)
- [x] Test link to non-existent issue (error message)

### Edge Cases

- [x] Issue with no _index.json file
- [x] _index.json with no "links" property
- [x] _index.json with empty "links" array
- [x] Link with invalid magazine name
- [x] Link with invalid volume/issue
- [x] Multiple links on same page
- [x] Link on first page of article
- [x] Link on last page of article
- [x] Network errors during link resolution

---

## Build Status

```bash
cd /home/justin/repos/urban-sniffle/the-archive/src/TheArchive && dotnet build
# Build succeeded. 0 Error(s), 1 Warning(s) (pre-existing)
```

---

## Summary

Successfully implemented a page links dropdown feature for the page viewer overlay. When viewing pages that contain links to other magazine issues (as defined in `_index.json`), a dropdown menu appears in the navigation bar. The dropdown intelligently filters links to show only those relevant to the currently visible page(s), and when selected, opens the linked issue in a new tab. The feature includes robust error handling, smooth user experience, and integrates seamlessly with the existing page viewer functionality.

**Key Features**:
- **Smart Filtering**: Shows only links for visible pages
- **New Tab Navigation**: Opens linked issues without losing current view
- **Error Handling**: Graceful handling of missing issues or network errors
- **Visual Polish**: Consistent styling with existing UI elements

**User Workflow**:
1. Open page viewer → 2. See dropdown if page has links → 3. Select link → 4. New tab opens with linked issue

---

**End of Feature Implementation**

