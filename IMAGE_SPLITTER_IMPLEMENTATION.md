# Image Splitter Implementation Complete

## Summary

Successfully implemented the image-splitter command-line application based on the old implementation from `/mnt/newvolume/repos/magazine/src/image-splitter`.

## What Was Done

### 1. Project Creation
- Created new project directory: `src/image-splitter/`
- Created `ImageSplitter.csproj` with .NET 8.0 configuration
- Created `Program.cs` with full implementation
- Created `README.md` with comprehensive documentation

### 2. Solution Integration
- Added image-splitter project to `Magazine.sln`
- Created solution folder for organization
- Added build configurations (Debug/Release)
- Added nested project mappings

### 3. Git Integration
- Added all project files to git staging area:
  - `ImageSplitter.csproj`
  - `Program.cs`
  - `README.md`
  - Updated `Magazine.sln`

### 4. Implementation Details

#### Command-Line Arguments
The tool now supports the following arguments:

- **`-d, --folder <directory>`** - Specify directory to process (default: current directory)
- **`-f <file>`** - Split a single image file
- **`--force`** - Skip creating backup files

**Important**: `-f` and `-d` are mutually exclusive as requested.

#### Features
1. **Automatic Directory Processing**: Defaults to current directory if no path specified
2. **Single File Mode**: Can process a single file with `-f` option
3. **Backup Management**: Creates `.backup` files unless `--force` is used
4. **Smart Detection**: Only processes:
   - Files with numeric names
   - Even-numbered pages
   - Landscape images (width > height)
   - When the next odd page doesn't exist
5. **Image Splitting**: Uses ImageMagick (`identify` and `convert` commands)
   - Left half: Overwrites original even-numbered file
   - Right half: Creates new odd-numbered file

#### Supported Formats
- JPEG (.jpg, .jpeg)
- PNG (.png)
- TIFF (.tif, .tiff)

### 5. Build Verification
- Project builds successfully with no errors
- Solution builds successfully (only pre-existing warnings from other projects)
- All code passes error checking

## Usage Examples

Process current directory:
```bash
cd /path/to/magazine/issue
dotnet run --project ~/repos/urban-sniffle/src/image-splitter/ImageSplitter.csproj
```

Process specific directory:
```bash
dotnet run --project src/image-splitter/ImageSplitter.csproj -- -d /path/to/magazine/issue
```

Split single file:
```bash
dotnet run --project src/image-splitter/ImageSplitter.csproj -- -f 24.jpg
```

Process without backups:
```bash
dotnet run --project src/image-splitter/ImageSplitter.csproj -- --force
```

## Requirements

- .NET 8.0 SDK
- ImageMagick installed and accessible via command line (`identify` and `convert` commands)

## Files Changed

- ✅ Created: `src/image-splitter/ImageSplitter.csproj`
- ✅ Created: `src/image-splitter/Program.cs`
- ✅ Created: `src/image-splitter/README.md`
- ✅ Modified: `Magazine.sln`

## Git Status

All changes are staged and ready to commit:
```
Changes to be committed:
  modified:   Magazine.sln
  new file:   src/image-splitter/ImageSplitter.csproj
  new file:   src/image-splitter/Program.cs
  new file:   src/image-splitter/README.md
```

## Next Steps

The project is ready to use. To commit the changes:
```bash
git commit -m "Add image-splitter project to solution"
```

## Notes

- The implementation is based on the old version from `/mnt/newvolume/repos/magazine/src/image-splitter`
- Command-line arguments have been updated to match the requirements:
  - `-f` for file (instead of `--file`)
  - `-d` or `--folder` for directory (instead of positional argument)
  - `--force` remains the same
- The tool now defaults to the current directory if no path is specified
- Project is properly integrated into the Magazine solution and visible in Rider

