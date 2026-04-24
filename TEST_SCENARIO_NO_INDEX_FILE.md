# Test Scenario: Opening Folder Without Index File

## Purpose
Verify that opening a folder without an `_index.json` file now correctly populates metadata fields.

## Test Setup
1. Create a test folder with the correct naming format:
   ```bash
   mkdir -p "/tmp/test-magazine/Creative Computing 17-03, 1982"
   ```

2. Add some image files to the folder:
   ```bash
   touch "/tmp/test-magazine/Creative Computing 17-03, 1982/001.jpg"
   touch "/tmp/test-magazine/Creative Computing 17-03, 1982/002.jpg"
   touch "/tmp/test-magazine/Creative Computing 17-03, 1982/003.jpg"
   ```

3. Make sure there's NO `_index.json` or `_index.txt` file:
   ```bash
   ls -la "/tmp/test-magazine/Creative Computing 17-03, 1982/"
   # Should show only the image files
   ```

## Test Steps

### Test Case 1: Valid Folder Name Format

1. **Run the application**:
   ```bash
   dotnet run --project src/index-editor/IndexEditor.csproj
   ```

2. **Open the test folder**:
   - Click "Open Folder" or press Ctrl+O
   - Navigate to `/tmp/test-magazine/Creative Computing 17-03, 1982`
   - Select the folder

3. **Expected Results**:
   - ✅ No error message appears
   - ✅ Folder opens successfully
   - ✅ Metadata fields are populated:
     - Magazine: "Creative Computing"
     - Volume: "17"
     - Number: "03"
     - Year: "1982"
   - ✅ Image viewer shows "No folder opened" initially (until the PageController fix is also applied)
   - ✅ Article list is empty (no articles yet)

4. **Create an article**:
   - Press Ctrl+A to create a new article
   - Fill in article details
   - Save with Ctrl+S

5. **Verify saved file**:
   ```bash
   cat "/tmp/test-magazine/Creative Computing 17-03, 1982/_index.json"
   ```
   
   Expected JSON structure:
   ```json
   {
     "metadata": {
       "magazine": "Creative Computing",
       "volume": "17",
       "number": "03",
       "year": "1982"
     },
     "articles": [
       {
         "title": "...",
         "pages": [...],
         ...
       }
     ],
     "links": []
   }
   ```

### Test Case 2: Invalid Folder Name Format

1. **Create folder with non-standard name**:
   ```bash
   mkdir -p "/tmp/test-magazine/Random Folder Name"
   touch "/tmp/test-magazine/Random Folder Name/001.jpg"
   ```

2. **Open the folder** in the application

3. **Expected Results**:
   - ✅ No error message
   - ✅ Folder opens successfully
   - ✅ Metadata fields:
     - Magazine: "Random Folder Name"
     - Volume: "—"
     - Number: "—"
     - Year: "—"
   - ✅ User can manually edit these fields
   - ✅ When saved, uses the current values from the UI

## Cleanup
```bash
rm -rf "/tmp/test-magazine"
```

## Before Fix
- ❌ Error: "Failed to load index file: No index file found in folder..."
- ❌ Folder would not open
- ❌ Could not create articles
- ❌ Metadata fields remained empty

## After Fix
- ✅ No error
- ✅ Folder opens successfully
- ✅ Metadata parsed from folder name
- ✅ Can create articles immediately
- ✅ Saved _index.json includes metadata

