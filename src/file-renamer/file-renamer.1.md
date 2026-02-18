# file-renamer(1) - rename files by removing strings or renumbering numeric filenames
## SYNOPSIS
`file-renamer` [`-d`|`--folder` *DIRECTORY*] (`-s`|`--string` *STRING* | `-r`|`--renumber` *INCREMENT*) [`-n`|`--no`]
## DESCRIPTION
**file-renamer** is a utility for batch renaming files in a directory. It supports two modes of operation: removing a specified string from filenames, or incrementing numeric filenames by a specified value.
The tool processes all files in the specified directory (or current directory if not specified). In string removal mode, it replaces all occurrences of the specified string in each filename. In renumber mode, it only processes files whose names (excluding extension) are numeric.
A dry-run mode is available to preview changes before applying them.
## OPTIONS
**-d, --folder** *DIRECTORY*  
Specify the directory containing files to rename. If not provided, uses the current working directory.
**-s, --string** *STRING*  
Remove all occurrences of *STRING* from filenames. Cannot be used with `-r`.
**-r, --renumber** *INCREMENT*  
Increment numeric filenames by *INCREMENT*. Only affects files whose names (without extension) are valid integers. Cannot be used with `-s`. Negative values are allowed.
**-n, --no**  
Dry-run mode. Show what would be done without actually renaming any files. Useful for previewing changes before applying them.
## EXAMPLES
Remove "_old" from all filenames in /path/to/files:
    file-renamer -d /path/to/files -s "_old"
Increment all numeric filenames by 10 in the current directory:
    file-renamer -r 10
Preview renumbering without making changes:
    file-renamer -r 5 -n
Decrement numeric filenames by 3:
    file-renamer --renumber -3
Remove "test_" prefix using current directory:
    file-renamer -s "test_"
## EXIT STATUS
**0** - Success  
**1** - Invalid arguments or usage error
## BEHAVIOR
### String Removal Mode
In string removal mode (`-s`), the tool:
* Processes all files in the directory
* Replaces all occurrences of the specified string (case-sensitive)
* Skips files that don't contain the string
* Preserves file extensions
### Renumber Mode
In renumber mode (`-r`), the tool:
* Only processes files whose names (without extension) are integers
* Adds the increment value to the numeric filename
* Skips non-numeric filenames
* Preserves file extensions
* Supports negative increments for decrementing
Example: With `-r 10`, file "5.txt" becomes "15.txt", and "test.txt" is skipped.
### Dry-Run Mode
When `-n` or `--no` is specified:
* No files are actually renamed
* Output shows what would happen with "WOULD RENAME" and "WOULD SKIP" messages
* Useful for verifying operations before executing them
## NOTES
* The `-s` and `-r` options are mutually exclusive. You must specify one, but not both.
* String matching is case-sensitive.
* If a directory is not specified, the current working directory is used.
* Files are processed in alphabetical order in renumber mode.
* The tool does not recurse into subdirectories.
## AUTHOR
Written for the urban-sniffle project.
## SEE ALSO
mv(1), rename(1), mmv(1)
