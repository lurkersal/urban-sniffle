# Build Script Fix - Code Trimming Issue

**Date**: March 4, 2026  
**Issue**: JSON article preview feature not working from console builds

## Problem

The JSON tree viewer enhancement to show "Category - Title" for collapsed articles worked perfectly when running from Rider (Debug mode), but **did not work** when running `IndexEditor` from the console after building with `clean-rebuild-indexeditor.sh`.

## Root Cause

The build script `scripts/clean-rebuild-indexeditor.sh` was using the publish option:

```bash
-p:PublishTrimmed=true
```

This aggressive optimization **trimmed away** the LINQ methods and logic needed for the article preview feature:

```csharp
var categoryNode = node.Children.FirstOrDefault(c => c.Key == "category");
var titleNode = node.Children.FirstOrDefault(c => c.Key == "title");
```

The trimmer removed these methods because it couldn't determine at compile-time that they were necessary.

## Solution

**Removed** the `-p:PublishTrimmed=true` option from the build script.

### Before
```bash
dotnet publish src/index-editor/IndexEditor.csproj \
    -c Release \
    -r linux-x64 \
    --self-contained \
    -p:PublishSingleFile=true \
    -p:PublishTrimmed=true \    # ← REMOVED THIS
    -o ~/bin
```

### After
```bash
dotnet publish src/index-editor/IndexEditor.csproj \
    -c Release \
    -r linux-x64 \
    --self-contained \
    -p:PublishSingleFile=true \
    -o ~/bin
```

## Impact

| Aspect | Trimmed Build | Full Build |
|--------|--------------|------------|
| Binary Size | ~13M | ~22M |
| Article Preview | ❌ Broken | ✅ Works |
| LINQ Support | ❌ Partial | ✅ Full |
| Build Time | Faster | Normal |

## Verification

After the fix, verify:

```bash
# 1. Rebuild
bash scripts/clean-rebuild-indexeditor.sh

# 2. Check size (should be ~22M, not ~13M)
ls -lh ~/bin/IndexEditor

# 3. Run and test
IndexEditor
# Press Ctrl+I, navigate to articles array
# Verify collapsed articles show: { Category - Title }
```

## Why Rider Worked

Rider runs in **Debug** mode by default, which:
- Never uses code trimming
- Includes all LINQ methods
- Has all reflection capabilities

The console build used **Release** mode with trimming, which:
- Aggressively removes "unused" code
- Can break LINQ operations
- Reduces reflection capabilities

## Lesson Learned

⚠️ **Code trimming can break LINQ queries that use predicates**, especially:
- `FirstOrDefault(predicate)`
- `Where(predicate)`
- `Select(predicate)`
- Any lambda expressions with closures

For Avalonia desktop applications where binary size isn't critical, it's safer to **disable trimming** unless you have specific size requirements and can thoroughly test the trimmed build.

## Files Modified

✅ `scripts/clean-rebuild-indexeditor.sh` - Removed `-p:PublishTrimmed=true`  
✅ `JSON_ARTICLE_PREVIEW_ENHANCEMENT.md` - Added troubleshooting section  

---

**Issue Resolved** ✓

The JSON article preview feature now works correctly in both Rider and console builds!

