# Diagnostic Checklist - March 19, 2026

## Context
The user reports that the previous fixes (from March 18, 2026) are not working:
1. Page image not loading after opening folder
2. Missing metadata when opening folder without index file

## Build Status
✅ **Build successful**
- Command: `dotnet build src/index-editor/IndexEditor.csproj`
- Warnings: 21 (all pre-existing, no new errors)
- Errors: 0

## Code Verification
✅ **Both fixes ARE present in the code:**

### Fix #1: PageControllerView Event Subscription
- File: `src/index-editor/Views/PageControllerView.axaml.cs`
- Lines 30: `_stateChangedHandler` field added
- Lines 42-70: `SetEditorState()` properly unsubscribes/resubscribes
- Lines 401-424: `_stateChangedHandler` created in `InitializeUI()`
- Line 427: Handler subscribed to StateChanged

### Fix #2: IndexFileService Missing Metadata
- File: `src/index-editor/Services/IndexFileService.cs`
- Lines 46-61: Handles missing index file case
- Uses `FolderMetadataParser.ParseFolderMetadata()` to parse folder name
- Returns parsed metadata with empty article/link lists

## Runtime Verification
✅ **App starts correctly:**
```
[DEBUG] PageControllerView.SetEditorState: CurrentFolder = '(null)'
[DEBUG] PageControllerView.SetEditorState: Re-subscribed to StateChanged
07:32:40 info: IndexEditor[0] PageControllerView.SetEditorState: Re-subscribed to StateChanged
```

This confirms that `SetEditorState()` IS being called and IS resubscribing properly.

## Diagnostic Questions

### For Fix #1 (Page Image Loading):
1. **When exactly does the problem occur?**
   - On first app launch?
   - After opening a specific folder?
   - When switching between folders?
   - All the time or intermittently?

2. **What do you see in the image area?**
   - "No folder opened"?
   - Blank/black screen?
   - Previous image still visible?
   - Loading spinner?

3. **Does the page number update correctly?**
   - Does the page number textbox show the correct page?
   - Can you manually type a page number and press Enter?

4. **Check console output:**
   - Look for `[DEBUG] LoadCurrentPageImage: folder = ...` messages
   - Look for `[DEBUG] PageControllerView.SetEditorState: Re-subscribed to StateChanged`

### For Fix #2 (Missing Metadata):
1. **What folder are you testing with?**
   - Does it follow the naming convention: `"Magazine Name VV-NN, YYYY"`?
   - Example: `"Creative Computing 17-03, 1982"`

2. **What fields are showing as empty/missing?**
   - Magazine name?
   - Volume?
   - Number?
   - Year?
   - All of them?

3. **Does saving create the _index.json file?**
   - After opening folder without index, try saving (Ctrl+S)
   - Check if `_index.json` is created
   - Open the file and check if metadata is present

## Test Scenarios

### Scenario 1: Fresh Folder (No Index File)
```bash
# Create test folder
mkdir -p "/tmp/test-folder/Creative Computing 17-03, 1982"
# Add some test images
cp ~/test-image.jpg "/tmp/test-folder/Creative Computing 17-03, 1982/001.jpg"
cp ~/test-image.jpg "/tmp/test-folder/Creative Computing 17-03, 1982/002.jpg"
```

Expected behavior:
1. Open folder in app (Ctrl+O)
2. Should parse: Magazine="Creative Computing", Volume="17", Number="03", Year="1982"
3. Image should load for page 1
4. Save (Ctrl+S) should create _index.json with metadata

### Scenario 2: Existing Folder (With Index File)
```bash
# Use existing magazine folder
cd ~/Magazines/Club\ International/Club\ International\ 20-02\,\ 1991/
ls -la _index.json  # Verify index file exists
```

Expected behavior:
1. Open folder in app
2. Should load metadata from _index.json
3. Image for page 1 should load immediately
4. Console should show "Re-subscribed to StateChanged"

## Debugging Steps

### Step 1: Enable verbose logging
Add to `Program.cs` or run with environment variable:
```bash
export DOTNET_ENVIRONMENT=Development
dotnet run --project src/index-editor/IndexEditor.csproj
```

### Step 2: Check for null reference issues
The fixes assume certain objects exist. Check:
- Is `_editorState` null when SetEditorState is called?
- Is `_stateChangedHandler` created before SetEditorState is called?

### Step 3: Verify call order
The fix assumes `InitializeUI()` is called BEFORE `SetEditorState()`. Check:
```csharp
// In PageControllerView constructor:
public PageControllerView() {
    InitializeComponent();
    // ... other init ...
    InitializeUI();  // <-- This MUST happen before SetEditorState()
}
```

## Potential Issues NOT Covered by the Fixes

### Issue A: Race Condition
If `SetEditorState()` is called BEFORE `InitializeUI()`:
- `_stateChangedHandler` would be null
- The re-subscription would not happen
- Fix: Ensure InitializeUI() is called first

### Issue B: Multiple SetEditorState Calls
If `SetEditorState()` is called multiple times:
- Should still work, but might cause duplicate subscriptions
- Check logs for multiple "Re-subscribed" messages

### Issue C: EditorState Instance Issues
If the wrong EditorState instance is injected:
- Events might fire on a different instance
- Verify MainWindow is injecting the same instance used elsewhere

## Next Steps

**Please provide:**
1. Exact steps to reproduce the problem
2. What you see vs. what you expect
3. Console output (especially DEBUG lines)
4. Folder name you're testing with
5. Whether it works with some folders but not others

