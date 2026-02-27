# Publish Script Update - Complete

## Summary

Successfully updated `scripts/publish-tools.sh` to build and publish all console applications in the Magazine solution.

## Changes Made

### Projects Now Included

#### Console Applications (Command-Line Tools)
1. ✅ **file-renamer** - `src/file-renamer/file-renamer.csproj`
2. ✅ **magazine-parser** - `src/magazine-parser/magazine-parser.csproj`
3. ✅ **image-splitter** - `src/image-splitter/ImageSplitter.csproj` (newly added)
4. ✅ **find-links** - `src/find-links/find-links.csproj`
5. ✅ **measure-test** - `src/common/measure-test/measure-test.csproj`

#### Desktop Applications (Avalonia/GUI)
6. ✅ **IndexEditor** - `src/index-editor/IndexEditor.csproj`

### Not Included
- **magazine-viewer** - Web application (ASP.NET Core), requires different publish strategy
- **common** - Library project, not an executable
- **MeasureProbe** - Helper/test class without a Main method, cannot be published

## Script Features

- **Self-contained builds**: Each app is published as a standalone executable
- **Single-file publish**: Each app is a single executable file
- **Trimmed**: Unused code is removed to reduce size
- **Linux x64 target**: Compiled for Linux 64-bit
- **Output directory**: `~/bin`
- **Dry run support**: Set `DRY_RUN=true` to test without actually publishing
- **Error handling**: Script stops on any error
- **Missing project handling**: Warns if a project file is not found but continues

## Usage

### Publish all tools
```bash
cd /home/justin/repos/urban-sniffle
./scripts/publish-tools.sh
```

### Dry run (test without publishing)
```bash
DRY_RUN=true ./scripts/publish-tools.sh
```

### Custom output directory
```bash
# Edit BIN_DIR in the script, or:
BIN_DIR=/custom/path ./scripts/publish-tools.sh
```

## Testing

Dry run test completed successfully. All 6 projects are detected and will be published:
```
✓ file-renamer.csproj
✓ magazine-parser.csproj
✓ ImageSplitter.csproj (newly added)
✓ find-links.csproj
✓ measure-test.csproj
✓ IndexEditor.csproj
```

## Post-Installation

After running the script, the executables will be in `~/bin`. If this directory is not in your PATH, add it:

```bash
echo 'export PATH="$HOME/bin:$PATH"' >> ~/.bashrc
source ~/.bashrc
```

## Notes

- The script publishes to `~/bin` by default
- All apps are published as self-contained, single-file executables
- The script includes a solution-wide restore before publishing to speed up the process
- magazine-viewer (web app) should be published separately using standard ASP.NET Core publish commands

