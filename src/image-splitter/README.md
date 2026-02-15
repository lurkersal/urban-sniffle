# Image Splitter

A console application for splitting images.

## Usage

```bash
image-splitter (<file-path> | [<working-folder>] -f <file-name>) [--force]
```

### Arguments

The image-splitter accepts arguments in two **mutually exclusive** modes:

#### Mode 1: Direct File Path
- `<file-path>` - Full path to the image file (relative or absolute)
- `--force` - (Optional) Force overwrite of existing output files

#### Mode 2: Working Folder + File Name
- `<working-folder>` - (Optional) Path to the working directory. If not specified, uses the current directory.
- `-f <file-name>` - (Required for this mode) Name of the image file to split
- `--force` - (Optional) Force overwrite of existing output files

**Note:** You cannot mix Mode 1 and Mode 2. Either provide a file path directly, OR use the `-f` flag with a file name.

### Examples

#### Mode 1: Direct File Path

**Split an image using a relative path:**
```bash
dotnet run -- myimage.jpg
```

**Split an image using an absolute path:**
```bash
dotnet run -- /home/user/images/myimage.jpg
```

**With force overwrite:**
```bash
dotnet run -- /home/user/images/myimage.jpg --force
```

#### Mode 2: Working Folder + File Name

**Split an image in the current directory:**
```bash
dotnet run -- -f myimage.jpg
```

**Split an image in a specific directory:**
```bash
dotnet run -- /home/user/images -f myimage.jpg
```

**With force overwrite:**
```bash
dotnet run -- /home/user/images -f myimage.jpg --force
```

## Development Status

The image splitting functionality is currently not implemented. The application validates arguments and file paths but does not yet perform the actual image splitting operation.


