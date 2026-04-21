# Page Links Feature - Quick Reference

**Status**: ✅ Fully Implemented and Ready for Testing

---

## What It Does

Shows a dropdown menu in the page viewer when the current page(s) have links to other magazine issues. Clicking a link opens that issue in a new tab.

---

## User Experience

```
┌─────────────────────────────────────────────────┐
│  ← Issue  Page 42                               │
│  [Prev] [Next] [Page 42 → Playboy Vol.45 #12 ▼] │
│                                                 │
│  [Page Image]  [Page Image]                     │
└─────────────────────────────────────────────────┘
```

1. User opens page viewer
2. Dropdown appears if page has links
3. User selects link
4. New tab opens with linked issue

---

## API Endpoints

### Load Links
```
GET /index-editor/{issueId}/links
→ { "links": [{ "page": 42, "magazine": "Playboy", "volume": "45", "issue": "12" }] }
```

### Resolve Issue
```
GET /api/v1/issues/find?magazine=Playboy&volume=45&number=12
→ { "issueId": 123, "magazineName": "Playboy", ... }
```

---

## Data Format (_index.json)

```json
{
  "metadata": { ... },
  "articles": [ ... ],
  "links": [
    {
      "page": 42,
      "magazine": "Playboy",
      "volume": "45",
      "issue": "12"
    }
  ]
}
```

---

## Key Files

- **Frontend**: `/the-archive/src/TheArchive/Views/Shared/_Layout.cshtml` (lines 59-300)
- **CSS**: `/the-archive/src/TheArchive/wwwroot/css/archive.css` (lines 939-960)
- **Backend**: `/the-archive/src/TheArchive/Controllers/IndexEditorController.cs` (lines 93-136)
- **API**: `/the-archive/src/TheArchive/Controllers/Api/IssuesController.cs` (lines 57-76)

---

## JavaScript Functions

```javascript
loadPageLinks(issueId)              // Fetch links from API
updatePageLinksDropdown()           // Filter and show/hide dropdown
handleLinkSelection(selectElement)  // Open linked issue in new tab
```

---

## Features

✅ Smart filtering (only shows links for visible pages)  
✅ Spread support (shows links for both pages)  
✅ New tab navigation  
✅ Error handling (missing files, invalid links)  
✅ Auto-hide when no links exist  

---

## Build Status

```bash
cd /home/justin/repos/urban-sniffle/the-archive/src/TheArchive
dotnet build
# ✅ Build succeeded. 0 Error(s)
```

---

## Full Documentation

See **PAGE_LINKS_DROPDOWN_FEATURE.md** for complete details.

---

