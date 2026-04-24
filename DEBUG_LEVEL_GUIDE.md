# How to Set Debug Level - IndexEditor

> **Note**: As of March 19, 2026, the command line argument parsing has been fixed. Previously, using `--log-level` would cause the app to try opening a folder named "--log-level". This is now resolved. See `COMMAND_LINE_ARGS_FIX_2026_03_19.md` for details.

## Quick Answer
Use the `--log-level` (or `-l`) command line argument when starting the application.

## Usage

### Command Line
```bash
# Set to debug level (most verbose)
dotnet run --project src/index-editor/IndexEditor.csproj -- --log-level debug

# Or using the short form
dotnet run --project src/index-editor/IndexEditor.csproj -- -l debug

# Other available levels
dotnet run --project src/index-editor/IndexEditor.csproj -- --log-level info     # Default
dotnet run --project src/index-editor/IndexEditor.csproj -- --log-level warning
dotnet run --project src/index-editor/IndexEditor.csproj -- --log-level error
dotnet run --project src/index-editor/IndexEditor.csproj -- --log-level none     # Disable logging
```

### From JetBrains Rider or IDE
1. Edit your run configuration
2. Add program arguments: `--log-level debug`
3. Run the application

### From Published Executable
```bash
# Linux/Mac
./IndexEditor --log-level debug

# Windows
IndexEditor.exe --log-level debug
```

## Available Log Levels

| Level | Description | When to Use |
|-------|-------------|-------------|
| `debug` | Most verbose - shows all debug messages | Development, troubleshooting issues |
| `info` | Normal informational messages (default) | General usage |
| `warning` | Warning messages only | Production |
| `error` | Error messages only | Production |
| `none` | No logging | Benchmarking, performance testing |

## Default Behavior
- **Default level**: `Info`
- **Log output**: Console and temp file at `%TEMP%/index-editor-debug.log` (or `/tmp/index-editor-debug.log` on Linux)

## Log Locations
- **Console**: Real-time output visible in terminal/IDE console
- **File**: `%TEMP%/index-editor-debug.log` (Windows) or `/tmp/index-editor-debug.log` (Linux)

## Examples of Debug Messages
With `--log-level debug`, you'll see messages like:
```
[2026-03-19T10:30:15.1234567Z] [INFO] IndexEditor starting with log level: Debug
[2026-03-19T10:30:15.2345678Z] [DEBUG] Loading folder: /path/to/magazine
[2026-03-19T10:30:15.3456789Z] [DEBUG] Found 150 image files
[2026-03-19T10:30:15.4567890Z] [INFO] Link discovery completed. Found 12 links
```

## Programmatic Access
You can also set the log level programmatically in code:

```csharp
// Set log level
IndexEditor.Shared.DebugLogger.SetMinimumLogLevel(IndexEditor.Shared.LogLevel.Debug);

// Get current level
var currentLevel = IndexEditor.Shared.DebugLogger.GetMinimumLogLevel();

// Use debug logging
IndexEditor.Shared.DebugLogger.Debug("This is a debug message");
IndexEditor.Shared.DebugLogger.Info("This is an info message");
IndexEditor.Shared.DebugLogger.Warning("This is a warning");
IndexEditor.Shared.DebugLogger.Error("This is an error");
IndexEditor.Shared.DebugLogger.LogException("Context info", exception);
```

## Implementation Details
The logging system is implemented in:
- **DebugLogger**: `src/index-editor/Shared/DebugLogger.cs`
- **Program.cs**: Parses command line args and initializes logging
- **Log levels**: Custom enum in `IndexEditor.Shared.LogLevel`
- **Backend**: Uses `Microsoft.Extensions.Logging` with console provider

## Tips
1. **For development**: Use `debug` level to see all diagnostic messages
2. **For troubleshooting**: Start with `debug`, then narrow down to specific components
3. **For production**: Use `info` or `warning` to reduce noise
4. **For performance testing**: Use `none` to eliminate logging overhead
5. **View log file**: Check the temp directory for persistent logs

## Viewing Logs
```bash
# On Linux
tail -f /tmp/index-editor-debug.log

# On Windows PowerShell
Get-Content $env:TEMP\index-editor-debug.log -Wait

# View entire log
cat /tmp/index-editor-debug.log  # Linux
type %TEMP%\index-editor-debug.log  # Windows
```

