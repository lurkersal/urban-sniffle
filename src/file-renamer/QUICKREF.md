# file-renamer - Quick Reference

## Synopsis
```
file-renamer [-d DIR] (-s STRING | -r NUM) [-n]
```

## Common Commands

### String Removal
```bash
# Remove "_old" from filenames
file-renamer -s "_old"

# Remove "test_" from files in specific directory  
file-renamer -d /path/to/files -s "test_"

# Preview removal without changes
file-renamer -s "_backup" -n
```

### Renumbering
```bash
# Increment numeric files by 10
file-renamer -r 10

# Decrement numeric files by 5
file-renamer -r -5

# Preview renumbering
file-renamer -r 100 -n

# Renumber files in specific directory
file-renamer -d /path/to/files -r 20
```

## Options Quick Reference

| Short | Long         | Description                              |
|-------|--------------|------------------------------------------|
| `-d`  | `--folder`   | Directory (default: current)             |
| `-s`  | `--string`   | String to remove from filenames          |
| `-r`  | `--renumber` | Increment for numeric filenames          |
| `-n`  | `--no`       | Dry-run (preview only)                   |

## Rules

1. **Mutually Exclusive**: Cannot use `-s` and `-r` together
2. **Must Specify One**: Either `-s` or `-r` is required
3. **Case Sensitive**: String matching is case-sensitive
4. **No Recursion**: Only processes files in specified directory (not subdirectories)
5. **Renumber Mode**: Only affects files with numeric names (e.g., "1.txt", "42.jpg")
6. **Collision Handling**: Automatically handles file name collisions during renaming

### Collision Handling

The tool automatically detects and handles various collision scenarios:

- **Chain collisions**: When renaming 1.txt→2.txt, 2.txt→3.txt, etc., uses two-phase rename
- **Duplicate targets**: Aborts if multiple files would rename to the same name
- **Existing files**: Aborts if target exists and won't be moved (prevents data loss)

Example: Incrementing `1.txt`, `2.txt`, `3.txt` by 1 automatically handles the chain collision.

## Examples by Use Case

### Cleanup Downloaded Files
```bash
# Remove browser download suffixes
file-renamer -s " (1)" -n          # Preview first
file-renamer -s " (1)"             # Apply changes
```

### Resequence Image Files
```bash
# Start numbering from 100 instead of 1
file-renamer -d ~/photos -r 99 -n  # Preview
file-renamer -d ~/photos -r 99     # Apply
```

### Remove Prefixes/Suffixes
```bash
# Remove "IMG_" prefix
file-renamer -s "IMG_"

# Remove "_backup" suffix
file-renamer -s "_backup"
```

## Tips

- **Always use `-n` first** to preview changes
- Check the current directory with `pwd` before running
- Use quotes around strings with spaces: `-s "text with spaces"`
- Negative numbers for decrementing: `-r -10`

## See Also

- Full documentation: `man file-renamer`
- README: See README.md for detailed usage

