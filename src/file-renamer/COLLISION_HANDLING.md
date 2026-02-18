# File Collision Handling Implementation Summary

## Problem Solved

The file-renamer tool had a critical bug where renaming files could cause collisions and failures. For example:
- Incrementing files `1.txt`, `2.txt`, `3.txt` by 1 would try to rename `1.txt` to `2.txt`, but `2.txt` already exists
- This would cause an IOException and abort the operation

## Solution Implemented

Added comprehensive collision detection and handling using multiple strategies:

### 1. Chain Collision Detection (Two-Phase Rename)

**Problem**: Sequential renames where targets overlap with sources
- Example: 1.txt→2.txt, 2.txt→3.txt, 3.txt→4.txt

**Solution**: Two-phase rename process
- Phase 1: Rename all files to temporary UUIDs
- Phase 2: Rename from temporary names to final targets
- Only used when collision is detected (minimal overhead)

**Implementation**:
- Detects if any target names exist in the source name set
- Uses `__temp_rename_{GUID}` pattern for temporary names
- Processes in correct order based on increment direction

### 2. Duplicate Target Detection

**Problem**: Multiple files renaming to the same target
- Example: "old_2.txt" and "old_old_2.txt" both become "2.txt" after removing "old_"

**Solution**: Pre-execution validation
- Groups rename plan by target name
- Aborts if any target has multiple sources
- Shows clear error message listing all conflicts

### 3. Static File Conflict Detection

**Problem**: Target name exists but won't be moved
- Example: "test_1.txt"→"1.txt" but "1.txt" exists and doesn't contain "test_"

**Solution**: Pre-execution validation
- Identifies files that won't be renamed
- Checks if any targets conflict with static files
- Aborts with clear error message

### 4. Smart Processing Order

**For Renumbering**:
- Positive increments: Process from highest to lowest
- Negative increments: Process from lowest to highest
- Minimizes need for two-phase rename when possible

## Code Changes

### FileRenamer.cs

1. Added `using System.Collections.Generic` for List and HashSet
2. Updated `RenameFiles()`:
   - Build complete rename plan before executing
   - Check for duplicate targets
   - Check for static file conflicts
   - Use two-phase rename for chain collisions
   - Added detailed error messages

3. Updated `RenumberFiles()`:
   - Separate numeric and non-numeric files
   - Smart ordering based on increment direction
   - Check for conflicts with non-numeric files
   - Check for duplicate targets
   - Use two-phase rename for collisions
   - Added detailed error messages and debug output

## Test Results

All collision scenarios tested and working:

✅ **Chain collision (increment)**: 1.txt, 2.txt, 3.txt + 1 → 2.txt, 3.txt, 4.txt
✅ **Chain collision (decrement)**: 2.txt, 3.txt, 4.txt - 1 → 1.txt, 2.txt, 3.txt
✅ **Duplicate target detection**: old_2.txt, old_old_2.txt → ERROR (both would be 2.txt)
✅ **Static file conflict**: test_1.txt + 1.txt → ERROR (conflict)
✅ **Complex chain**: 1.txt, 2.txt, 11.txt + 10 → 11.txt, 12.txt, 21.txt
✅ **Dry-run mode**: No files modified, shows preview correctly

## Error Messages

The tool now provides clear, actionable error messages:

### Duplicate Target Error
```
ERROR: Cannot rename - multiple files would be renamed to the same name:
  Target: '2.txt' ← from:
    - 'old_old_2.txt'
    - 'old_2.txt'
DEBUG: Rename operation aborted due to duplicate target conflicts.
```

### Static File Conflict Error
```
ERROR: Cannot rename - the following target file(s) already exist:
  'test_2.txt' → '2.txt' (CONFLICT: '2.txt' already exists and won't be moved)
  'test_1.txt' → '1.txt' (CONFLICT: '1.txt' already exists and won't be moved)
DEBUG: Rename operation aborted due to 2 conflict(s).
```

### Successful Two-Phase Rename
```
DEBUG: Collision detected, using two-phase rename (temp names first)
✓ RENAMED: '5.txt' → '6.txt' (5 + 1 = 6)
✓ RENAMED: '4.txt' → '5.txt' (4 + 1 = 5)
✓ RENAMED: '3.txt' → '4.txt' (3 + 1 = 4)
✓ RENAMED: '2.txt' → '3.txt' (2 + 1 = 3)
✓ RENAMED: '1.txt' → '2.txt' (1 + 1 = 2)
DEBUG: Renumber operation completed.
```

## Documentation Updates

- **README.md**: Added comprehensive "Collision Handling" section with examples
- **QUICKREF.md**: Added collision handling rules and examples
- Man pages will need updating in future (optional)

## Performance Impact

- Minimal: Collision detection uses HashSet operations (O(n))
- Two-phase rename only triggered when needed
- Smart ordering reduces need for two-phase rename in many cases

## Safety Improvements

- **Data Loss Prevention**: Never overwrites existing files
- **Atomic Operations**: Either all files rename or none (abort on error)
- **Clear Feedback**: User knows exactly what went wrong
- **Dry-Run Support**: Preview still works correctly with collision detection

