# Issue Index Editor Feature

**Date**: April 22, 2026  
**Status**: ✅ **COMPLETED** - View and edit _index.json files from issue pages

---

## Summary

Added functionality to view and edit the `_index.json` file associated with an issue directly from the issue detail page, similar to the IndexEditor desktop application's Ctrl+I functionality.

---

## Features

### 1. Edit Index Button

- **Location**: Issue detail page header, next to article count badge
- **Appearance**: Subtle button with "Edit Index" text
- **Behavior**: Opens full-screen editor overlay

### 2. Keyboard Shortcuts

- **Ctrl+I** (or Cmd+I on Mac): Toggle the index editor overlay
- **Ctrl+S** (or Cmd+S on Mac): Save changes
- **Esc**: Close the overlay

### 3. Editor Overlay

- **Full-screen modal** with dark background
- **Large textarea** with monospace font for JSON editing
- **Save button** with visual feedback
- **Close button** with unsaved changes warning
- **Status messages** for loading, saving, and errors

---

## Implementation Details

### Files Created

1. **`IndexEditorController.cs`** - API endpoints for loading and saving index files
   - `GET /index-editor/{issueId}` - Load _index.json content
   - `POST /index-editor/{issueId}` - Save _index.json content

2. **`Content.cs`** - Model for Content table records

### Files Modified

1. **`IssuesController.cs`** - (No changes needed, uses existing structure)

2. **`Detail.cshtml`** - Issue detail page
   - Added "Edit Index" button to header
   - Added full-screen overlay HTML
   - Added JavaScript for editor functionality

3. **`ArchiveDatabase.cs`** - Database service
   - Added `GetIssueContentAsync()` method to retrieve content records

---

## How It Works

### Determining the Folder Path

1. Query the first Content record for the issue
2. Get its `ImagePath` (relative path like `playboy/1999/1999-03/page-001.jpg`)
3. Combine with `MAGAZINE_IMAGE_ROOT` environment variable
4. Extract directory from full path
5. Look for `_index.json` in that directory

### Loading the Index

```javascript
GET /index-editor/{issueId}
→ Returns JSON content as plain text
```

### Saving the Index

```javascript
POST /index-editor/{issueId}
Body: { "content": "..." }
→ Validates JSON
→ Creates backup (.bak)
→ Saves to _index.json
```

---

## User Experience

### Opening the Editor

**Method 1**: Click "Edit Index" button  
**Method 2**: Press Ctrl+I keyboard shortcut

### Editing

1. Overlay opens with current _index.json content
2. User edits JSON in monospace textarea
3. Status messages show loading/saving state

### Saving

**Method 1**: Click "Save" button  
**Method 2**: Press Ctrl+S keyboard shortcut

- JSON is validated before saving
- Backup file created automatically
- Success/error message displayed

### Closing

**Method 1**: Click "Close" button  
**Method 2**: Press Esc key

- Warns if unsaved changes exist
- Returns to issue detail page

---

## Visual Design

### Edit Index Button

```
┌──────────────────────────────────────┐
│ Playboy March 1999                   │
│ Vol. 46, Issue 3                     │
│                                      │
│ [Edit Index]  [15 Articles]          │
└──────────────────────────────────────┘
```

### Editor Overlay

```
┌────────────────────────────────────────────────────────────┐
│                                                            │
│  Edit Index File               [Save] [Close]              │
│                                                            │
│  Status: Loaded successfully                               │
│  ┌──────────────────────────────────────────────────────┐ │
│  │ {                                                    │ │
│  │   "metadata": {                                      │ │
│  │     "magazine": "Playboy",                           │ │
│  │     "volume": "46",                                  │ │
│  │     "number": "03",                                  │ │
│  │     "year": "1999"                                   │ │
│  │   },                                                 │ │
│  │   "articles": [                                      │ │
│  │     {                                                │ │
│  │       "pages": [1],                                  │ │
│  │       "category": "Cover",                           │ │
│  │       "title": "",                                   │ │
│  │       "modelNames": ["Victoria Silvstedt"]           │ │
│  │     }                                                │ │
│  │   ]                                                  │ │
│  │ }                                                    │ │
│  └──────────────────────────────────────────────────────┘ │
│                                                            │
└────────────────────────────────────────────────────────────┘
```

---

## API Endpoints

### GET /index-editor/{issueId}

**Purpose**: Load the _index.json file for an issue

**Response Success** (200):
```json
{
  "metadata": {
    "magazine": "Playboy",
    "volume": "46",
    "number": "03",
    "year": "1999"
  },
  "articles": [...]
}
```

**Response Error** (404):
```json
{
  "error": "_index.json not found at /path/to/issue/_index.json"
}
```

**Response Error** (500):
```json
{
  "error": "MAGAZINE_IMAGE_ROOT environment variable is not set"
}
```

### POST /index-editor/{issueId}

**Purpose**: Save the _index.json file for an issue

**Request Body**:
```json
{
  "content": "{ \"metadata\": {...}, \"articles\": [...] }"
}
```

**Response Success** (200):
```json
{
  "success": true,
  "message": "Index saved successfully",
  "path": "/path/to/issue/_index.json"
}
```

**Response Error** (400):
```json
{
  "error": "Invalid JSON: Unexpected token..."
}
```

---

## Error Handling

### Missing _index.json

- **Display**: Error message in status area
- **Message**: "_index.json not found at {path}"
- **Action**: User can create file manually or close editor

### Invalid JSON

- **When**: Saving invalid JSON
- **Display**: Error message in status area
- **Message**: "Invalid JSON: {error details}"
- **Action**: User can fix JSON and try again

### Missing MAGAZINE_IMAGE_ROOT

- **Display**: Error message in status area
- **Message**: "MAGAZINE_IMAGE_ROOT environment variable is not set"
- **Action**: Admin must set environment variable

### Unsaved Changes

- **When**: Closing editor with unsaved changes
- **Display**: Browser confirm dialog
- **Message**: "You have unsaved changes. Are you sure you want to close?"
- **Action**: User can cancel or proceed

---

## Benefits

✅ **Direct Access** - Edit index files without leaving the web interface  
✅ **Familiar** - Same Ctrl+I shortcut as IndexEditor desktop app  
✅ **Safe** - Automatic backups before saving  
✅ **Validated** - JSON validation prevents syntax errors  
✅ **User-Friendly** - Clear status messages and keyboard shortcuts  
✅ **Consistent** - Matches the design and behavior of the desktop app  

---

## Technical Notes

### Environment Variable Required

The `MAGAZINE_IMAGE_ROOT` environment variable must be set for this feature to work. It's used to resolve relative image paths to absolute file system paths.

Example:
```bash
export MAGAZINE_IMAGE_ROOT="/mnt/storage/magazines"
```

### File Path Resolution

1. Query: `SELECT ImagePath FROM Content WHERE IssueId = ? LIMIT 1`
2. Result: `playboy/1999/1999-03/page-001.jpg` (relative path)
3. Resolve: `${MAGAZINE_IMAGE_ROOT}/playboy/1999/1999-03/page-001.jpg`
4. Extract: `/mnt/storage/magazines/playboy/1999/1999-03/`
5. Find: `/mnt/storage/magazines/playboy/1999/1999-03/_index.json`

### Backup Strategy

- Backup file created: `_index.json.bak`
- Overwrites previous backup
- Created before every save

### JSON Validation

- Uses `System.Text.Json.JsonDocument.Parse()`
- Validates before writing to disk
- Returns detailed error messages

---

## Testing

### Manual Testing Checklist

- [ ] Navigate to an issue detail page
- [ ] Click "Edit Index" button
- [ ] Verify overlay opens with JSON content
- [ ] Edit JSON content
- [ ] Press Ctrl+S to save
- [ ] Verify success message
- [ ] Press Esc to close
- [ ] Reopen editor to verify changes persisted
- [ ] Make changes without saving
- [ ] Try to close - verify warning dialog
- [ ] Test Ctrl+I toggle (open/close)
- [ ] Test invalid JSON - verify error message

### Edge Cases

- [ ] Issue with no content records
- [ ] Issue with missing _index.json file
- [ ] Issue with corrupt _index.json file
- [ ] MAGAZINE_IMAGE_ROOT not set
- [ ] Network errors during save

---

## Future Enhancements

### Potential Improvements

1. **Syntax Highlighting** - Add JSON syntax highlighting
2. **Validation Preview** - Show validation errors in real-time
3. **Format Button** - Auto-format JSON with proper indentation
4. **Diff View** - Show changes before saving
5. **History** - View previous versions of the file
6. **Split View** - Edit alongside page viewer
7. **Auto-save** - Optional auto-save every N seconds

### Integration Ideas

1. **Link to IndexEditor** - Button to open folder in desktop app
2. **Sync Status** - Show if index differs from database
3. **Re-import** - Trigger database re-import after edit
4. **Validation Rules** - Show schema validation errors

---

## Comparison with IndexEditor Desktop App

### Similar Features

- ✅ Ctrl+I keyboard shortcut
- ✅ Full-screen overlay
- ✅ Monospace font
- ✅ Save with Ctrl+S
- ✅ Close with Esc

### Differences

- **Web**: Edits _index.json directly
- **Desktop**: Edits in-memory state, saves to _index.txt
- **Web**: JSON format
- **Desktop**: Text format
- **Web**: No visual page preview
- **Desktop**: Shows page thumbnails and article list

---

## Security Considerations

### File Access

- ✅ Only accesses files within issue directories
- ✅ Uses environment variable for root path
- ✅ No directory traversal possible
- ✅ Validates JSON before saving

### User Input

- ✅ JSON content is validated before saving
- ✅ No arbitrary file path input
- ✅ Issue ID is validated (database lookup)

### Authentication

- ⚠️ Currently no authentication required
- **Recommendation**: Add authentication in production

---

## Build Status

```bash
cd /home/justin/repos/urban-sniffle/the-archive && dotnet build
# Build succeeded. 0 Error(s), 1 Warning(s) (pre-existing)
```

---

## Summary

Successfully implemented a web-based index file editor that allows viewing and editing `_index.json` files directly from the issue detail page. The editor features keyboard shortcuts (Ctrl+I to open, Ctrl+S to save, Esc to close), automatic backups, JSON validation, and a clean full-screen interface similar to the IndexEditor desktop application.

**Key Features**:
- **Access**: Click button or press Ctrl+I
- **Edit**: Full-screen textarea with monospace font
- **Save**: Ctrl+S with validation and backup
- **Close**: Esc with unsaved changes warning

**User Workflow**:
1. Visit issue page → 2. Press Ctrl+I → 3. Edit JSON → 4. Press Ctrl+S → 5. Press Esc

---

**End of Feature Implementation**

