# Page Viewer - Issue Link Addition

**Date**: April 21, 2026  
**Status**: ✅ **COMPLETED** - Issue link added to page viewer

---

## Summary

Added a link to the current issue in the page viewer overlay, positioned to the left of the page numbers.

---

## Changes Made

### 1. Added Issue Link to Page Info Display

**File**: `/src/TheArchive/Views/Shared/_Layout.cshtml`

**Location**: `displayArticlePages()` function (lines 305-311)

**Before**:
```javascript
// Update page info
if (canShowSpread) {
    document.getElementById('spread-info').innerHTML = `Page <strong>${currentPageData.page}</strong> & <strong>${nextPageData.page}</strong>`;
} else {
    document.getElementById('spread-info').innerHTML = `Page <strong>${currentPageData.page}</strong>`;
}
```

**After**:
```javascript
// Update page info
const issueLinkHtml = currentIssueId ? `<a href="/issues/${currentIssueId}" class="issue-link" onclick="closeSpread()">← Issue</a>` : '';
if (canShowSpread) {
    document.getElementById('spread-info').innerHTML = `${issueLinkHtml} Page <strong>${currentPageData.page}</strong> & <strong>${nextPageData.page}</strong>`;
} else {
    document.getElementById('spread-info').innerHTML = `${issueLinkHtml} Page <strong>${currentPageData.page}</strong>`;
}
```

**Key Features**:
- Only shows link when `currentIssueId` is available
- Closes the overlay when clicked (`onclick="closeSpread()"`)
- Positioned before the page numbers
- Uses arrow (←) for clear navigation indication

---

### 2. Added CSS Styling for Issue Link

**File**: `/src/TheArchive/wwwroot/css/archive.css`

**New CSS**:
```css
.spread-info .issue-link {
    color: var(--paper);
    text-decoration: none;
    padding: 4px 8px;
    border-radius: 4px;
    background: rgba(245, 240, 232, 0.1);
    border: 0.5px solid rgba(245, 240, 232, 0.2);
    font-size: 14px;
    transition: all 0.2s;
    margin-right: 1rem;
    display: inline-block;
}

.spread-info .issue-link:hover {
    background: rgba(245, 240, 232, 0.15);
    border-color: rgba(245, 240, 232, 0.3);
}
```

**Styling Features**:
- Button-like appearance matching the overlay design
- Subtle hover effect for better UX
- Right margin (1rem) to separate from page numbers
- Matches the overall dark overlay aesthetic

---

## Visual Result

### Single Page View

```
┌────────────────────────────────────────────────┐
│ [← Issue]  Page 45                        [✕]  │
│                                                │
│            [Page Image]                        │
│                                                │
│      [← Prev]              [Next →]           │
└────────────────────────────────────────────────┘
```

### Spread View (Two Pages)

```
┌────────────────────────────────────────────────┐
│ [← Issue]  Page 46 & 47                   [✕]  │
│                                                │
│     [Page 46]              [Page 47]          │
│                                                │
│      [← Prev]              [Next →]           │
└────────────────────────────────────────────────┘
```

---

## How It Works

### When Link Appears

The issue link only appears when `currentIssueId` is set, which happens when:

1. **Viewing Issue Pages**: User clicks to view all pages of an issue
2. **Viewing Cover Articles**: Cover articles automatically load all issue pages

The link will NOT appear when:
- Viewing article pages from a model's detail page (no issue context)
- Viewing standalone articles without issue context

### Click Behavior

```javascript
onclick="closeSpread()"
```

When clicked:
1. Closes the page viewer overlay
2. Navigates to the issue detail page
3. User can see the full issue with all articles

---

## Benefits

✅ **Quick Navigation** - Easy return to issue detail page  
✅ **Context Awareness** - Know which issue you're viewing  
✅ **Better UX** - No need to use browser back button  
✅ **Visual Clarity** - Link clearly separated from page numbers  
✅ **Conditional Display** - Only shows when relevant  

---

## Use Cases

### Scenario 1: Browsing Issue Pages

1. User is on issue detail page (`/issues/123`)
2. User clicks article or "View All Pages"
3. Page viewer opens showing page 45
4. **Issue link appears**: `← Issue` next to `Page 45`
5. User clicks `← Issue`
6. Overlay closes and returns to issue detail page

### Scenario 2: Viewing Cover

1. User clicks on a cover article
2. System loads all issue pages (cover behavior)
3. Page viewer opens
4. **Issue link appears**: User can navigate back to issue

### Scenario 3: Viewing Model Article

1. User is on model detail page
2. User clicks article from model's appearances
3. Page viewer opens showing article pages
4. **No issue link**: Article doesn't have issue context in this flow

---

## Technical Details

### currentIssueId Variable

The `currentIssueId` variable is set in `openIssuePages()` function:

```javascript
async function openIssuePages(issueId) {
    // ...
    currentIssueId = issueId;  // Store for later use
    // ...
}
```

This variable persists while the overlay is open, allowing the issue link to be displayed.

### Conditional Rendering

```javascript
const issueLinkHtml = currentIssueId ? 
    `<a href="/issues/${currentIssueId}" class="issue-link" onclick="closeSpread()">← Issue</a>` : 
    '';
```

Uses ternary operator to:
- Generate link HTML if issue ID exists
- Generate empty string if no issue ID

---

## Files Modified

1. **`/src/TheArchive/Views/Shared/_Layout.cshtml`**
   - Updated `displayArticlePages()` function
   - Added conditional issue link before page numbers

2. **`/src/TheArchive/wwwroot/css/archive.css`**
   - Added `.spread-info .issue-link` styling
   - Added hover state for link

---

## Build Status

```bash
cd /home/justin/repos/urban-sniffle/the-archive && dotnet build
# Build succeeded. 0 Error(s), 1 Warning(s) (pre-existing)
```

---

## Testing

### Manual Testing Checklist

- [ ] Navigate to an issue detail page
- [ ] Click "View All Pages" or click an article
- [ ] Verify `← Issue` link appears to the left of page numbers
- [ ] Click the issue link
- [ ] Verify overlay closes and returns to issue page
- [ ] Navigate pages - verify link persists on all pages
- [ ] Check hover effect on the link
- [ ] Test on different screen sizes

### Edge Cases

- [ ] Viewing article from model page - no issue link should appear
- [ ] Viewing cover article - issue link should appear
- [ ] Navigating between pages - link should remain visible
- [ ] Multiple page spreads - link positioned correctly

---

## Future Enhancements

Potential improvements:

1. **Issue Title**: Show issue title instead of just "Issue"
2. **Magazine Name**: Include magazine name in link text
3. **Breadcrumb**: Make it a full breadcrumb (Magazine > Issue > Page)
4. **Keyboard Shortcut**: Add 'I' key to jump to issue
5. **Article Link**: Add link to specific article when viewing article pages

---

## Summary

Added a navigation link to the current issue in the page viewer overlay, positioned to the left of the page numbers. The link appears only when viewing issue pages and provides quick navigation back to the issue detail page.

**User Experience**:
- **Before**: No way to return to issue without closing overlay and clicking back
- **After**: One-click return to issue detail page via `← Issue` link

**Visual Placement**:
```
[← Issue]  Page 45
    ↑         ↑
  New link  Existing
```

---

**End of Enhancement**

