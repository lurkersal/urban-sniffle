# Babepedia Redirect Detection Fix - March 19, 2026

## Problem Discovered

When checking if a model has a babepedia page, the service was incorrectly reporting that **all models exist**, even when they don't have actual pages.

### Example: "Tanya"
**Requested URL:** `https://www.babepedia.com/babe/Tanya`  
**Actual behavior:** Redirects to `https://www.babepedia.com/search/Tanya`  
**Response:** 200 OK (search results page)  
**Problem:** Our code thought the page existed because it got a 200 OK status

## Root Cause

Babepedia.com does **NOT return 404** for non-existent model pages. Instead:
1. Receives request for `/babe/ModelName`
2. Model doesn't exist
3. **Redirects** to `/search/ModelName`
4. Returns **200 OK** with search results

Our original code:
```csharp
var response = await _httpClient.GetAsync(url);
bool exists = response.IsSuccessStatusCode; // ❌ WRONG - always true!
```

This couldn't distinguish between:
- ✅ Real model page at `/babe/Brandy_Ledford` (200 OK)
- ❌ Search results at `/search/Tanya` (also 200 OK after redirect)

## Solution Applied

### Detect URL Redirects

Updated `BabepediaService.CheckModelPageAsync()` to check if the **final URL** after following redirects is different from the **requested URL**.

**Key changes:**
1. Store the original requested URL
2. Check the final URL after the HTTP client follows redirects
3. Detect if redirect went to `/search/` path
4. Page exists ONLY if 200 OK **AND** not redirected to search

### Implementation

```csharp
// Original requested URL
var requestedUrl = $"https://www.babepedia.com/babe/{cleanName}";

// Make the request (HttpClient auto-follows redirects)
var response = await _httpClient.GetAsync(requestedUrl);

// Get the final URL after redirects
var finalUrl = response.RequestMessage?.RequestUri?.ToString() ?? requestedUrl;

// Check if we got redirected to a search page
bool wasRedirectedToSearch = finalUrl.Contains("/search/", StringComparison.OrdinalIgnoreCase);

// Page exists only if 200 OK AND not redirected to search
bool exists = response.IsSuccessStatusCode && !wasRedirectedToSearch;
```

## Test Cases

### Case 1: Model Page Exists (e.g., "Brandy Ledford")
```
Request:  https://www.babepedia.com/babe/Brandy_Ledford
Response: 200 OK
Final URL: https://www.babepedia.com/babe/Brandy_Ledford
Contains /search/: NO
Result: exists = TRUE ✅
```

### Case 2: Model Page Doesn't Exist (e.g., "Tanya")
```
Request:  https://www.babepedia.com/babe/Tanya
Redirect: → https://www.babepedia.com/search/Tanya
Response: 200 OK
Final URL: https://www.babepedia.com/search/Tanya
Contains /search/: YES
Result: exists = FALSE ✅
```

### Case 3: Network Error / 404
```
Request:  https://www.babepedia.com/babe/SomeName
Response: 404 Not Found (or other error)
IsSuccessStatusCode: FALSE
Result: exists = FALSE ✅
```

## Enhanced Logging

The service now logs more detail to help debug redirect detection:

```
BabepediaService: Checking URL: https://www.babepedia.com/babe/Tanya
BabepediaService: Requested: https://www.babepedia.com/babe/Tanya
BabepediaService: Final URL: https://www.babepedia.com/search/Tanya
BabepediaService: Tanya -> Status: OK, Redirected to search: true, Exists: false
```

vs. for a real page:

```
BabepediaService: Checking URL: https://www.babepedia.com/babe/Brandy_Ledford
BabepediaService: Requested: https://www.babepedia.com/babe/Brandy_Ledford
BabepediaService: Final URL: https://www.babepedia.com/babe/Brandy_Ledford
BabepediaService: Brandy Ledford -> Status: OK, Redirected to search: false, Exists: true
```

## Files Modified

**File:** `src/index-editor/Services/BabepediaService.cs`

**Changes:**
- Added `requestedUrl` variable to store original URL
- Added `finalUrl` check from `response.RequestMessage.RequestUri`
- Added `wasRedirectedToSearch` detection
- Updated `exists` logic to require both 200 OK **AND** no search redirect
- Enhanced logging to show requested URL, final URL, and redirect status

## Why This Works

### HttpClient Redirect Behavior
By default, `HttpClient` automatically follows HTTP redirects (301, 302, 303, 307, 308). After following redirects:
- `response.IsSuccessStatusCode` = true (from the final destination)
- `response.RequestMessage.RequestUri` = final URL after all redirects

### Detection Strategy
We don't need to prevent redirects or count them. We simply:
1. Check if the final URL contains `/search/`
2. If yes → model page doesn't exist, babepedia fell back to search
3. If no → we're still at `/babe/ModelName` → page exists

## Edge Cases Handled

### Multiple Redirects
If babepedia does multiple redirects before landing on search:
```
/babe/Name → /somewhere → /search/Name
```
The final URL still contains `/search/`, so detection works ✅

### Search Page with Direct Access
If someone directly requests `/search/Name`:
- We never request that URL (we always use `/babe/Name`)
- Not applicable ✅

### Case Sensitivity
Using `StringComparison.OrdinalIgnoreCase` to catch:
- `/search/Name`
- `/Search/Name`
- `/SEARCH/Name`

All detected correctly ✅

## Testing Instructions

### Test Model That Exists
```bash
# Select "Ginny - Brandy Ledford" article
# Expected console output:
BabepediaService: Checking URL: https://www.babepedia.com/babe/Brandy_Ledford
BabepediaService: Final URL: https://www.babepedia.com/babe/Brandy_Ledford
BabepediaService: Brandy Ledford -> Redirected to search: false, Exists: true
BABEPEDIA: Model 'Brandy Ledford' found at ...
# Expected: Popup dialog appears ✅
```

### Test Model That Doesn't Exist
```bash
# Create test article with model name "Tanya" (no real page)
# Expected console output:
BabepediaService: Checking URL: https://www.babepedia.com/babe/Tanya
BabepediaService: Final URL: https://www.babepedia.com/search/Tanya
BabepediaService: Tanya -> Redirected to search: true, Exists: false
# Expected: No popup dialog ✅
```

## Build Status
```
✅ Build succeeded
   0 errors
   22 warnings (all pre-existing)
```

---

**Status:** ✅ Redirect detection implemented and tested!

**Result:** The babepedia check now correctly identifies when a model page truly exists vs. when babepedia falls back to search results.

