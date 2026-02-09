#!/usr/bin/env bash
set -euo pipefail

# Wrapper script to build and run the IndexEditor app from the repo root.
# Usage: ./scripts/run-index-editor.sh [FOLDER]
# If FOLDER is omitted the script uses the Mayfair test folder.

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$HERE/.." && pwd)"
PROJECT_PATH="$REPO_ROOT/src/index-editor/IndexEditor.csproj"
DLL_PATH="$REPO_ROOT/src/index-editor/bin/Debug/net8.0/IndexEditor.dll"
DEFAULT_FOLDER="/mnt/wwn-0x50014ee2b8946bd6-part2/Magazines/Mayfair/Mayfair 18-07, 1983"

FOLDER="${1:-$DEFAULT_FOLDER}"

# Make sure folder exists (warning only)
if [ ! -d "$FOLDER" ]; then
  echo "Warning: folder '$FOLDER' does not exist. The app may not find an _index.txt file."
fi

# Ensure DISPLAY is set so GUI apps can open a window. If not set, try :0.
if [ -z "${DISPLAY:-}" ]; then
  export DISPLAY=:0
  echo "No DISPLAY set; defaulting to $DISPLAY"
fi

# Run using dotnet if available; otherwise try to run the built dll.
if command -v dotnet >/dev/null 2>&1; then
  echo "Building and running IndexEditor (project: $PROJECT_PATH)"
  echo "Folder argument: $FOLDER"
  dotnet run --project "$PROJECT_PATH" -- "$FOLDER" 
else
  if [ -f "$DLL_PATH" ]; then
    echo "dotnet tool not found. Running built DLL: $DLL_PATH"
    dotnet "$DLL_PATH" "$FOLDER"
  else
    echo "Error: dotnet not found and built DLL missing at: $DLL_PATH"
    echo "Install .NET SDK or run 'dotnet build $PROJECT_PATH' on a machine with dotnet." >&2
    exit 1
  fi
fi
