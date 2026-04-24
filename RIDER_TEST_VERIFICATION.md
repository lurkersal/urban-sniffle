# Rider Test Panel Verification Guide

## Summary
All test projects have been successfully reorganized from `src/<project>/Tests/` to `tests/<project>/`. This follows standard .NET conventions and ensures all tests are properly discovered by Rider.

## What Changed
✅ Moved `Common.Tests` from `src/common/Tests/` → `tests/common/`  
✅ Moved `IndexEditor.Tests` from `src/index-editor/Tests/` → `tests/index-editor/`  
✅ Consolidated `FileRenamer.Tests` in `tests/file-renamer/`  
✅ Updated all project references in .csproj files  
✅ Updated Magazine.sln to reference new test locations  

## Verification Results

### Test Projects in Solution (4 total)
```
tests/common/Common.Tests.csproj
tests/file-renamer/FileRenamer.Tests.csproj
tests/index-editor/IndexEditor.Tests.csproj
tests/magazine-parser/MagazineParser.Tests.csproj
```

### Build Status
✅ **Common.Tests**: Build succeeded  
✅ **IndexEditor.Tests**: Build succeeded  
✅ **FileRenamer.Tests**: Build succeeded  
⚠️ **MagazineParser.Tests**: Has pre-existing compilation errors (unrelated to this reorganization)

### Test Discovery
- **FileRenamer.Tests.dll**: 2 tests
- **Common.Tests.dll**: Approximately 30 tests
- **IndexEditor.Tests.dll**: Approximately 130 tests
- **Total**: 162 tests discovered

## How to Verify in Rider

### Method 1: Check Test Panel
1. Open Rider
2. Navigate to `View` → `Tool Windows` → `Unit Tests` (or press `Ctrl+Alt+U` / `Cmd+Alt+U`)
3. Click the refresh button in the Test Explorer if needed
4. You should now see all test projects organized under a tree structure:
   ```
   Magazine (Solution)
   ├── Common.Tests
   │   ├── IndexEditorIntegrationTests
   │   ├── IndexFileParserTests
   │   ├── MeasurementsValidatorTests
   │   ├── PipeSeparatedValuesTests
   │   └── SegmentLifecycleTests
   ├── FileRenamer.Tests
   │   └── FileRenamerTests
   ├── IndexEditor.Tests
   │   ├── ActiveSegmentDisplayTests
   │   ├── ArticleEditorMeasurementsIntegrationTests
   │   ├── EditorActionsRefactoringTests
   │   ├── Services/FullscreenImageServiceTests
   │   ├── Services/OverlayManagerTests
   │   └── [... more tests]
   └── MagazineParser.Tests (may show as disabled due to build errors)
   ```

### Method 2: Run All Tests
1. In the Test Explorer, right-click on "Magazine" (the solution root)
2. Select "Run All Tests from Solution"
3. You should see 162 tests execute (excluding MagazineParser.Tests which has build errors)

### Method 3: Verify in Solution Explorer
1. Navigate to `Solution Explorer`
2. Expand the `tests` folder
3. You should see 4 test projects:
   - `common` (Common.Tests.csproj)
   - `file-renamer` (FileRenamer.Tests.csproj)
   - `index-editor` (IndexEditor.Tests.csproj)
   - `magazine-parser` (MagazineParser.Tests.csproj)

### Method 4: Run from Command Line
From the project root:
```bash
# List all tests
dotnet test --list-tests

# Run all tests
dotnet test

# Run specific test project
dotnet test tests/index-editor/IndexEditor.Tests.csproj
```

## Troubleshooting

### If tests don't appear in Rider:
1. **Reload Solution**: `File` → `Invalidate Caches / Restart`
2. **Rebuild**: Right-click solution → `Rebuild All`
3. **Check Settings**: `Settings` → `Build, Execution, Deployment` → `Unit Testing` → Ensure test runner is configured

### If you see duplicate tests:
- This should no longer happen since old test directories have been removed
- If it does occur, clean the solution: `dotnet clean && dotnet build`

## Next Steps

### Fix MagazineParser.Tests (Optional)
The MagazineParser.Tests project has compilation errors:
- Missing `Services` namespace in MagazineParser
- Missing `Repositories` namespace in MagazineParser

These are pre-existing issues unrelated to the test reorganization.

### Run Tests Regularly
Now that all tests are properly organized and discoverable:
- Run tests before committing code
- Use Rider's continuous testing feature
- Monitor test coverage

---
**Status**: ✅ Complete  
**Date**: February 19, 2026  
**Tests Discovered**: 162 (from 3 working test projects)

