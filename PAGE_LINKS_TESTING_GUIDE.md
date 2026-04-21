# Page Links Feature - Testing Guide

**Date**: April 22, 2026  
**Status**: Ready for Testing

---

## Why You Don't See the Links Dropdown

The page links dropdown **only appears** when these conditions are met:

1. ✅ The issue has an `_index.json` file
2. ✅ The `_index.json` file contains a `"links"` property
3. ✅ At least one link exists for the currently visible page(s)

**Current Status**: Your magazine files don't have links added to their `_index.json` files yet.

---

## How to Test the Feature

### Option 1: Add Sample Links to an Existing Issue

1. **Pick an issue to test** (example: Club International 04-12, 1975)
   
2. **Edit its _index.json file**:
   ```bash
   nano "/mnt/newvolume/Magazines/Club International/Club International 04-12, 1975/_index.json"
   ```

3. **Add a "links" array** at the root level (same level as "metadata" and "articles"):
   ```json
   {
     "metadata": {
       "magazine": "Club International",
       "volume": "4",
       "number": "12",
       "year": "—"
     },
     "articles": [
       ...existing articles...
     ],
     "links": [
       {
         "page": 10,
         "magazine": "Club International",
         "volume": "10",
         "issue": "6"
       },
       {
         "page": 15,
         "magazine": "Club International",
         "volume": "10",
         "issue": "11"
       }
     ]
   }
   ```

4. **Start the web application**:
   ```bash
   cd /home/justin/repos/urban-sniffle/the-archive/src/TheArchive
   dotnet run
   ```

5. **Navigate to the issue**:
   - Open: http://localhost:5163
   - Find "Club International" in the magazines list
   - Click on the issue (April 1975, Vol 4 #12)

6. **Click on any article** to open the page viewer

7. **Navigate to page 10 or 15** using the arrow keys or Next button

8. **Look for the dropdown** in the navigation bar:
   ```
   [Prev] [Next] [Page 10 → Club International Vol.10 #6 ▼] ✕
   ```

---

### Option 2: Use Web Interface to Add Links

Since you implemented the index editor feature, you can use it:

1. **Start the web app**:
   ```bash
   cd /home/justin/repos/urban-sniffle/the-archive/src/TheArchive
   dotnet run
   ```

2. **Navigate to an issue** (e.g., http://localhost:5163/issues/123)

3. **Press Ctrl+I** or click "Edit Index" button

4. **Add the links array** to the JSON:
   ```json
   {
     "metadata": { ... },
     "articles": [ ... ],
     "links": [
       {
         "page": 10,
         "magazine": "Club International",
         "volume": "10",
         "issue": "6"
       }
     ]
   }
   ```

5. **Press Ctrl+S** to save

6. **Close the editor** (press Esc)

7. **Click on an article** to open the page viewer

8. **Navigate to the page with links** (page 10 in this example)

9. **The dropdown should appear**

---

## Where the Dropdown Appears

The dropdown is located in the **page viewer overlay** navigation bar:

```
┌──────────────────────────────────────────────────────────┐
│                                                          │
│  ← Issue  Page 10                                        │
│                                                          │
│  [Prev] [Next] [Page 10 → Club International... ▼]  ✕   │
│  ↑                ↑                                      │
│  Navigation       Links Dropdown (ONLY shows when        │
│  Buttons          current page has links)                │
│                                                          │
│  ╔════════════╗  ╔════════════╗                         │
│  ║  Page 10   ║  ║  Page 11   ║                         │
│  ╚════════════╝  ╚════════════╝                         │
│                                                          │
└──────────────────────────────────────────────────────────┘
```

---

## Step-by-Step Test Scenario

### Setup Phase

1. **Choose two issues** from your database that exist and have pages
   - Example: Club International Vol.4 #12 (source)
   - Example: Club International Vol.10 #6 (target)

2. **Edit the source issue's _index.json** to add a link:
   ```json
   "links": [
     {
       "page": 10,
       "magazine": "Club International",
       "volume": "10",
       "issue": "6"
     }
   ]
   ```

### Testing Phase

1. **Start the web app**: `dotnet run` from TheArchive directory

2. **Navigate to source issue**: http://localhost:5163/issues/{issueId}

3. **Click any article** to open page viewer

4. **Use arrow keys or Next button** to navigate to page 10

5. **Verify dropdown appears**:
   - Should show: "Page 10 → Club International Vol.10 #6"
   - Should be to the right of [Prev] [Next] buttons

6. **Click the dropdown**

7. **Select the link**

8. **Verify**:
   - New tab opens
   - Shows the target issue (Vol.10 #6)
   - Original page viewer stays open

9. **Navigate to page 11** (no links)

10. **Verify dropdown disappears**

---

## Quick Test with Minimal Setup

If you just want to verify the code works:

1. **Add a link to ANY existing _index.json**:
   ```bash
   # Find an index file
   FILE=$(find /mnt/newvolume/Magazines -name "_index.json" | head -1)
   
   # Back it up
   cp "$FILE" "$FILE.backup"
   
   # Edit it (add links array before the closing brace)
   # You'll need to manually edit this file
   nano "$FILE"
   ```

2. **Add this before the final `}`**:
   ```json
   ,
   "links": [
     {
       "page": 10,
       "magazine": "Playboy",
       "volume": "1",
       "issue": "1"
     }
   ]
   ```

3. **Save and test** using the steps above

---

## Troubleshooting

### Dropdown Not Appearing

**Check 1**: Does the `_index.json` file have a "links" array?
```bash
# View the file
cat "/path/to/issue/_index.json" | grep -A5 "links"
```

**Check 2**: Are you on a page that has links?
- The dropdown only shows for pages listed in the links array
- If link is for page 10, you must be viewing page 10

**Check 3**: Is the issue ID correctly resolved?
- Open browser console (F12)
- Look for errors in the Network tab
- Check `/index-editor/{issueId}/links` request

**Check 4**: Is the API endpoint returning links?
```bash
# Test the endpoint directly
curl http://localhost:5163/index-editor/123/links
# Replace 123 with actual issue ID
```

### Link Not Working

**Check 1**: Does the target issue exist in the database?
```bash
# Test the find endpoint
curl "http://localhost:5163/api/v1/issues/find?magazine=Playboy&volume=1&number=1"
```

**Check 2**: Are magazine/volume/issue values correct?
- Magazine name must match database (case-insensitive)
- Volume and issue must match exactly

**Check 3**: Check browser console for errors
- Press F12
- Look for red error messages
- Check Network tab for failed requests

---

## Example: Real Test Data

Here's a complete example you can copy/paste:

### File: `/mnt/newvolume/Magazines/Club International/Club International 04-12, 1975/_index.json`

Add this links array (make sure the target issues exist in your database):

```json
{
  "metadata": {
    "magazine": "Club International",
    "volume": "4",
    "number": "12",
    "year": "1975"
  },
  "articles": [
    ...existing articles...
  ],
  "links": [
    {
      "page": 10,
      "magazine": "Club International",
      "volume": "10",
      "issue": "6"
    },
    {
      "page": 20,
      "magazine": "Club International",
      "volume": "10",
      "issue": "11"
    },
    {
      "page": 30,
      "magazine": "Club International",
      "volume": "4",
      "issue": "11"
    }
  ]
}
```

This creates links on pages 10, 20, and 30 to other Club International issues.

---

## Console Commands to Check Setup

### 1. Check if API endpoints are accessible
```bash
# Start the app in background
cd /home/justin/repos/urban-sniffle/the-archive/src/TheArchive
dotnet run &

# Wait a moment, then test
sleep 5

# Test links endpoint (replace 1 with real issue ID)
curl http://localhost:5163/index-editor/1/links

# Test find endpoint
curl "http://localhost:5163/api/v1/issues/find?magazine=Club%20International&volume=4&number=12"
```

### 2. Find issues in your database
```bash
# Connect to PostgreSQL
psql -h localhost -U postgres -d magazines

# List some issues
SELECT IssueId, MagazineId, Volume, Number 
FROM Issue 
LIMIT 10;

# Find a specific magazine's issues
SELECT i.IssueId, m.Name, i.Volume, i.Number
FROM Issue i
JOIN Magazine m ON i.MagazineId = m.MagazineId
WHERE m.Name = 'Club International'
LIMIT 10;

# Exit
\q
```

---

## What Success Looks Like

When everything is working correctly:

1. **Page viewer opens** when you click an article
2. **Navigation bar shows**: `[Prev] [Next] [dropdown] ✕`
3. **Dropdown contains**: "Page X → Magazine Vol.Y #Z"
4. **Clicking dropdown**: Opens new tab with linked issue
5. **Navigating pages**: Dropdown appears/disappears based on links

---

## Next Steps After Testing

Once you've confirmed the feature works:

1. **Add real links** to issues that reference other issues
2. **Document patterns** for which pages typically have links
3. **Consider automation** to scan for issue references in OCR'd text
4. **Track usage** to see which links users follow most

---

## Summary

**The links dropdown is working**, but you need to:

1. ✅ Add a "links" array to your `_index.json` files
2. ✅ Navigate to pages that have links
3. ✅ The dropdown will appear automatically

**Quickest Test**:
1. Edit one `_index.json` file to add links array
2. Start the web app
3. Navigate to that issue
4. Click an article to open page viewer
5. Navigate to a page with a link
6. See the dropdown appear

---

