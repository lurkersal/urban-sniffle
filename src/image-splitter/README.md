# Image Splitter

A command-line tool for splitting double-page spread scans into separate left and right page images.

## Description

This tool processes image files with numeric filenames (page numbers) and splits double-page spreads (landscape images where width > height) into two separate pages. The original even-numbered page becomes the left page, and a new odd-numbered page is created for the right page.

## Requirements

- .NET 8.0 or later
- ImageMagick (specifically the `identify` and `convert` commands)

## Usage

```bash
image-splitter [options]
```

### Options

- `-d, --folder <directory>` - Directory to process (default: current directory)
- `-f <file>` - Split a single image file
- `--force` - Skip creating backup files

**Note:** `-f` and `-d` options are mutually exclusive.

### Examples

Process all eligible images in the current directory:
```bash
image-splitter
```

Process all eligible images in a specific directory:
```bash
image-splitter -d /path/to/magazine/issue
# or
image-splitter --folder /path/to/magazine/issue
```

Split a single file:
```bash
image-splitter -f 24.jpg
```

Process without creating backups:
```bash
image-splitter --force
```

## How It Works

1. **File Selection**: Only processes files with numeric names (e.g., `24.jpg`, `26.png`)
2. **Even Numbers Only**: Only processes even-numbered pages (assumes double-page spreads start on even pages)
3. **Dimension Check**: Only processes images where width > height (landscape orientation)
4. **Existence Check**: Skips if the next odd-numbered page already exists
5. **Splitting**: 
   - Left half: Overwrites the original even-numbered file
   - Right half: Creates a new odd-numbered file (original number + 1)
6. **Backup**: Unless `--force` is used, creates `.backup` files before splitting

## Supported Image Formats

- JPEG (.jpg, .jpeg)
- PNG (.png)
- TIFF (.tif, .tiff)

## Example

If you have a file named `24.jpg` that is a double-page spread:

```bash
image-splitter -f 24.jpg
```

This will:
1. Create `24.jpg.backup` (original file)
2. Replace `24.jpg` with the left half of the image
3. Create `25.jpg` with the right half of the image

## Notes

- The tool requires ImageMagick to be installed and accessible via command line
- Files are processed in order by filename
- Non-numeric filenames are ignored when processing directories
- Backup files (`.backup`) are not automatically cleaned up

