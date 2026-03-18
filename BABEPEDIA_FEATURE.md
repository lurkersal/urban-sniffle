# Babepedia Model Search Feature - March 19, 2026

## Feature Description
When selecting a Model or Cover article in the index-editor, the application automatically searches babepedia.com for the model's name. If a page exists for that model, a debug popup dialog is displayed showing the model name and URL.

## Implementation Details

### Files Created

#### 1. BabepediaService.cs
**Location:** `src/index-editor/Services/BabepediaService.cs`

**Purpose:** HTTP service to check if a model has a page on babepedia.com

**Key Methods:**
- `CheckModelPageAsync(string modelName)` - Checks if a model page exists
  - Cleans model name (replaces spaces with underscores, removes quotes)
  - Constructs URL: `https://www.babepedia.com/babe/{ModelName}`
  - Returns tuple: `(bool exists, string url)`
  - 5-second timeout for HTTP requests

- `CheckArticleModelAsync(ArticleLine article)` - Checks babepedia for article's model
  - Only processes Model and Cover category articles
  - Gets first model name from article
  - Returns tuple: `(bool exists, string modelName, string url)`

**Features:**
- Static HttpClient with proper user agent
- Timeout handling (5 seconds)
- Exception handling and logging
- URL encoding for special characters

#### 2. BabepediaDebugDialog.axaml & .axaml.cs
**Location:** `src/index-editor/Views/BabepediaDebugDialog.axaml[.cs]`

**Purpose:** Debug popup dialog to display babepedia search results

**UI Elements:**
- Title: "✓ Model Found on Babepedia.com"
- Model Name display
- Clickable URL (opens in default browser)
- OK button to close dialog

**Features:**
- Centered on owner window
- Fixed size (500x250)
- Non-resizable
- URL click handler to open browser

### Files Modified

#### SelectArticleCommand.cs
**Location:** `src/index-editor/Views/SelectArticleCommand.cs`

**Changes:**
- Added `using System.Threading.Tasks`
- Added `CheckBabepediaAsync()` method
- Integrated check into `Execute()` method after article selection

**Behavior:**
```csharp
public void Execute(object? parameter)
{
    // ...existing article selection logic...
    
    // Check babepedia.com for Model articles
    CheckBabepediaAsync(toSelect);
}
```

The check happens asynchronously after article selection, so it doesn't block the UI.

## User Experience Flow

### Scenario 1: Selecting a Model Article with Babepedia Page
```
1. User clicks on a Model article (e.g., "Ginny" - Brandy Ledford)
2. Article is selected normally (editor pane updates)
3. Background: HTTP request sent to babepedia.com
4. If page exists:
   - Debug log: "BABEPEDIA: Model 'Brandy Ledford' found at https://..."
   - Popup dialog appears showing:
     ✓ Model Found on Babepedia.com
     Model Name: Brandy Ledford
     Babepedia URL: https://www.babepedia.com/babe/Brandy_Ledford
     [OK button]
5. User can click the URL to open in browser
6. User clicks OK to close dialog
```

### Scenario 2: Selecting a Model Article without Babepedia Page
```
1. User clicks on a Model article
2. Article is selected normally
3. Background: HTTP request sent to babepedia.com
4. No page found (404 response)
5. No popup shown (silent failure)
6. Debug log: "BabepediaService: [ModelName] -> Status: NotFound, Exists: false"
```

### Scenario 3: Selecting Non-Model Article
```
1. User clicks on Editorial/Review/Letters/etc. article
2. Article is selected normally
3. No babepedia check performed (category filter)
4. No popup shown
```

## Technical Details

### Categories Checked
- **Model** - Always checked
- **Cover** - Always checked
- All other categories - Skipped

### HTTP Request Details
- **Timeout:** 5 seconds
- **User Agent:** "Mozilla/5.0 (compatible; IndexEditor/1.0)"
- **URL Format:** `https://www.babepedia.com/babe/{ModelName}`
- **Name Transformations:**
  - Spaces → Underscores
  - Single quotes → Removed
  - Double quotes → Removed
  - URL encoded

### Error Handling
- **Timeout:** Logged, returns (false, empty)
- **Network errors:** Logged, returns (false, empty)
- **HTTP errors:** Status code logged, returns (false, empty) for non-200
- **Dialog errors:** Falls back to toast notification

## Performance Considerations

### Async Implementation
- HTTP request runs asynchronously (doesn't block UI)
- Article selection completes immediately
- Dialog shown on UI thread after response received

### Timeout
- 5-second timeout prevents long waits
- User can continue working immediately after selection

### Caching
- No caching implemented (each selection triggers new request)
- Could be enhanced to cache results per session

## Testing Checklist

### Manual Tests
- [ ] Select Model article with known babepedia page → Dialog appears
- [ ] Click URL in dialog → Browser opens to correct page
- [ ] Click OK button → Dialog closes
- [ ] Select Model article without babepedia page → No dialog (silent)
- [ ] Select Cover article with babepedia page → Dialog appears
- [ ] Select Editorial article → No dialog (not checked)
- [ ] Select multiple Model articles in sequence → Each triggers check
- [ ] Network timeout (disconnect internet) → Silent failure, logged

### Edge Cases
- [ ] Model name with spaces (e.g., "Jane Doe") → Converts to "Jane_Doe"
- [ ] Model name with apostrophe (e.g., "O'Brien") → Removes apostrophe
- [ ] Empty model name → Skipped, no request
- [ ] Multiple model names in article → Uses first name only
- [ ] Rapid article selection → Each gets checked independently

## Debug Logging

All babepedia checks are logged to help with debugging:

```
BabepediaService: Checking URL: https://www.babepedia.com/babe/Brandy_Ledford
BabepediaService: Brandy Ledford -> Status: OK, Exists: true
BABEPEDIA: Model 'Brandy Ledford' found at https://www.babepedia.com/babe/Brandy_Ledford
```

Or on failure:
```
BabepediaService: Checking URL: https://www.babepedia.com/babe/Unknown_Model
BabepediaService: Unknown Model -> Status: NotFound, Exists: false
```

## Future Enhancements

### Potential Improvements
1. **Caching:** Store results for session to avoid repeated requests
2. **All model names:** Check all model names in article (not just first)
3. **Configuration:** Toggle feature on/off in settings
4. **Batch checking:** Check all models in current issue
5. **Alternative sources:** Check other model databases (IAFD, etc.)
6. **Model info:** Fetch and display additional model info from babepedia
7. **Link in editor:** Add clickable link in editor pane for quick access

## Build Status
```
✅ Build succeeded (0 errors, 22 pre-existing warnings)
```

## Files Summary
**Created:**
- `src/index-editor/Services/BabepediaService.cs`
- `src/index-editor/Views/BabepediaDebugDialog.axaml`
- `src/index-editor/Views/BabepediaDebugDialog.axaml.cs`

**Modified:**
- `src/index-editor/Views/SelectArticleCommand.cs`

---

**Status:** ✅ Feature complete and ready for testing!

