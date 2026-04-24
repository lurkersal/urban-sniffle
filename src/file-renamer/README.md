# File Renamer

A .NET console application for batch renaming files. Supports removing strings from filenames or incrementing numeric filenames.

## Features

- **String Removal**: Remove a specified string from all filenames in a directory
- **Renumbering**: Increment or decrement numeric filenames by a specified value
- **Dry-Run Mode**: Preview changes before applying them
- **Flexible Options**: Optional directory path (defaults to current directory)
- **Man Page**: Full manual page documentation included

## Prerequisites

- .NET SDK 8.0 or later installed. Install from the official [.NET website](https://dotnet.microsoft.com/download).

## Building the Application

1. Navigate to the project directory:

   ```bash
   cd file-renamer
   ```

2. Build the application:

   ```bash
   dotnet build
   ```

## Usage

### Command Syntax

```bash
file-renamer [(-d|--folder) <directory>] (-s|--string <string> | -r|--renumber <increment>) [-n|--no]
```

### Options

- `-d, --folder <path>` - Directory path (optional, defaults to current directory)
- `-s, --string <text>` - String to remove from file names
- `-r, --renumber <increment>` - Increment numeric file names by the specified value
- `-n, --no` - Dry run - show what would be done without renaming files

**Note:** `-s` and `-r` are mutually exclusive.

### Examples

Remove "_old" from all filenames in a specific directory:
```bash
dotnet run -- -d /path/to/files -s "_old"
```

Increment all numeric filenames by 10 in current directory:
```bash
dotnet run -- -r 10
```

Preview renumbering without making changes:
```bash
dotnet run -- -r 5 -n
```

Decrement numeric filenames by 3:
```bash
dotnet run -- --renumber -3
```

Remove "test_" prefix using current directory:
```bash
dotnet run -- -s "test_"
```

## Man Page

A manual page is included for detailed documentation.

### Viewing the Man Page

From the project directory:
```bash
man ./file-renamer.1
```

Or with groff:
```bash
groff -man -Tascii file-renamer.1 | less
```

### Installing the Man Page

To install the man page system-wide:

```bash
# Copy to local man directory
sudo cp file-renamer.1 /usr/local/share/man/man1/

# Update man page database
sudo mandb

# Now you can use:
man file-renamer
```

Or install for current user only:
```bash
# Create user man directory if it doesn't exist
mkdir -p ~/.local/share/man/man1

# Copy man page
cp file-renamer.1 ~/.local/share/man/man1/

# Add to MANPATH in ~/.bashrc if needed
echo 'export MANPATH="$HOME/.local/share/man:$MANPATH"' >> ~/.bashrc
source ~/.bashrc

# Now you can use:
man file-renamer
```

## How It Works

### String Removal Mode (`-s`)
- Processes all files in the directory
- Replaces all occurrences of the specified string (case-sensitive)
- Skips files that don't contain the string
- Preserves file extensions

### Renumber Mode (`-r`)
- Only processes files whose names (without extension) are integers
- Adds the increment value to the numeric filename
- Skips non-numeric filenames
- Preserves file extensions
- Supports negative increments

Example: With `-r 10`, file `5.txt` becomes `15.txt`, and `test.txt` is skipped.

### Dry-Run Mode (`-n`)
- No files are actually renamed
- Output shows what would happen with "WOULD RENAME" and "WOULD SKIP" messages
- Useful for verifying operations before executing them

## Collision Handling

The tool automatically detects and handles file name collisions to prevent data loss:

### Types of Collisions Detected

1. **Chain Collisions**: When files rename in a sequence (e.g., 1.txt→2.txt, 2.txt→3.txt)
   - **Solution**: Uses two-phase rename with temporary names
   - **Example**: Incrementing consecutive files by 1

2. **Duplicate Targets**: When multiple files would rename to the same target name
   - **Solution**: Aborts operation with error message
   - **Example**: Removing "old_" from both "old_2.txt" and "old_old_2.txt" (both→"2.txt")

3. **Static File Conflicts**: When target name exists but won't be moved
   - **Solution**: Aborts operation with error message
   - **Example**: Renaming "test_1.txt" to "1.txt" when "1.txt" already exists and doesn't contain "test_"

### How It Works

**For Renumbering:**
- Detects if target numbers overlap with source numbers
- Processes files in correct order (high-to-low for positive increments, low-to-high for negative)
- Uses temporary names when collisions detected
- Automatically handles consecutive file renumbering

**For String Removal:**
- Builds complete rename plan before executing
- Checks for duplicate targets and existing file conflicts
- Uses two-phase rename when files swap names or create chains
- Aborts if irresolvable conflicts detected

**Example of Successful Chain Handling:**
```bash
# Before: 1.txt, 2.txt, 3.txt, 4.txt, 5.txt
file-renamer -r 1
# After: 2.txt, 3.txt, 4.txt, 5.txt, 6.txt
# (automatically uses temp names to avoid collisions)
```

## Notes

- Ensure that you have the necessary permissions to rename files in the specified directory.
- Use caution when renaming files, as this action cannot be undone.