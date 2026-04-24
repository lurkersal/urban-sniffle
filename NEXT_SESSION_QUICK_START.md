# Quick Start Guide for Next Session
## Current Status ✅
- ✅ **Build:** 0 Errors, 0 Warnings
- ✅ **Keyboard Handlers:** 100% Migrated (4/4)
- ✅ **Static State:** 50% Complete (+40% this session)
- 🔄 **MainWindow:** Still 1,308 lines (target: <500)
---
## What to Do Next
### Priority 1: Extract FileOperationsService (2-3 hours)
**Goal:** Reduce MainWindow by ~200 lines
**Methods to Extract:**
1. `LoadArticlesFromFolder()` - Main loading logic
2. `SaveIndexFile()` - File saving
3. `CreateBackup()` - Backup creation
4. File validation helpers
**Pattern to Follow:**
```csharp
// 1. Create interface
public interface IFileOperationsService
{
    Task<List<ArticleLine>> LoadArticlesFromFolder(string path);
    Task SaveIndexFile(string folder, List<ArticleLine> articles);
    Task CreateBackup(string filePath);
}
// 2. Implement service
public class FileOperationsService : IFileOperationsService { }
// 3. Register in DI (if available)
services.AddSingleton<IFileOperationsService, FileOperationsService>();
// 4. Inject into MainWindow
private readonly IFileOperationsService _fileOps;
public MainWindow(IFileOperationsService fileOps) { _fileOps = fileOps; }
// 5. Replace calls
// BEFORE: await LoadArticlesFromFolder(path);
// AFTER:  await _fileOps.LoadArticlesFromFolder(path);
```
---
## Build Verification
```bash
# Always run before and after changes
cd /home/justin/repos/urban-sniffle
dotnet build src/index-editor/IndexEditor.csproj
# Expected: 0 Error(s). 0 Warning(s).
```
---
## Documentation to Read First
1. **KEYBOARD_HANDLER_MIGRATION_PATTERN.md** - Migration approach
2. **SESSION_COMPLETE_MARCH_5_2026.md** - What we just finished
3. **CRITICAL_ISSUES_STATUS.txt** - Overall progress
---
## Files to Focus On
- `src/index-editor/MainWindow.axaml.cs` (lines 1-400)
- Look for file I/O operations
- Look for methods >100 lines
---
## Success Criteria for Next Session
- ✅ FileOperationsService created and injected
- ✅ MainWindow reduced by ~200 lines
- ✅ Zero warnings maintained
- ✅ All functionality preserved
- ✅ Documentation updated
---
## Time Estimate
- **Planning:** 30 minutes
- **Interface Creation:** 30 minutes  
- **Service Implementation:** 60 minutes
- **MainWindow Refactor:** 45 minutes
- **Testing & Verification:** 30 minutes
- **Documentation:** 15 minutes
- **Total:** ~3 hours
---
## Quick Commands
```bash
# Check MainWindow size
wc -l src/index-editor/MainWindow.axaml.cs
# Search for file operations
grep -n "File\|Load\|Save" src/index-editor/MainWindow.axaml.cs | head -20
# Build
dotnet build src/index-editor/IndexEditor.csproj
```
---
**Remember:** Follow the established pattern from keyboard handler migration!
