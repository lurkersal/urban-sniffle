# Test Reorganization - Quick Reference

## ✅ What Was Done
All test projects moved from `src/<project>/Tests/` to `tests/<project>/`

## 📊 Current Status
- **Test Projects**: 4
- **Tests Discovered**: 162
- **Working Projects**: 3 (Common, IndexEditor, FileRenamer)
- **Projects with Build Errors**: 1 (MagazineParser - pre-existing issues)

## 📁 New Structure
```
tests/
├── common/               (Common.Tests.csproj)
├── file-renamer/         (FileRenamer.Tests.csproj)
├── index-editor/         (IndexEditor.Tests.csproj)
└── magazine-parser/      (MagazineParser.Tests.csproj)
```

## ✅ Verification
All tests now appear in Rider's test panel at: `View → Tool Windows → Unit Tests`

## 🚀 Commands
```bash
# List all tests
dotnet test --list-tests

# Run all tests  
dotnet test

# Build specific test project
dotnet build tests/index-editor/IndexEditor.Tests.csproj
```

## 📚 Documentation
- Full details: `TEST_REORGANIZATION_COMPLETE.md`
- Rider guide: `RIDER_TEST_VERIFICATION.md`

