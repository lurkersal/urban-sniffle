#!/bin/bash

set -euo pipefail
IFS=$'\n\t'

# Resolve repo root (parent of scripts folder)
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

# Publish self-contained builds to ~/bin
BIN_DIR="${HOME%/}/bin"
DRY_RUN="${DRY_RUN:-false}"

run_cmd() {
    # $@ is the command to run
    if [ "$DRY_RUN" = "true" ]; then
        echo "DRY_RUN: $*"
        return 0
    fi
    echo "+ $*"
    if ! $*; then
        echo "ERROR: command failed: $*" >&2
        exit 1
    fi
}

# Ensure dotnet exists
if ! command -v dotnet >/dev/null 2>&1; then
    echo "ERROR: dotnet CLI not found in PATH" >&2
    exit 1
fi

# Create bin directory if it doesn't exist
mkdir -p "$BIN_DIR"

echo "Publishing tools to $BIN_DIR..."

# Run a single restore to speed up multiple publishes
echo "Restoring solution..."
run_cmd dotnet restore "$REPO_ROOT/Magazine.sln"

# Helper that publishes a project if it exists
publish_project() {
    local proj_path="$1"
    local proj_abs="$REPO_ROOT/$proj_path"
    if [ ! -f "$proj_abs" ]; then
        echo "Warning: project file not found: $proj_abs - skipping"
        return 0
    fi

    echo "Publishing $(basename "$proj_abs")..."
    run_cmd dotnet publish "$proj_abs" \
        -c Release \
        -r linux-x64 \
        --self-contained true \
        -p:PublishSingleFile=true \
        -p:PublishTrimmed=true \
        -o "$BIN_DIR"
}

# List of projects to publish (edit as needed)
projects=(
    "src/file-renamer/file-renamer.csproj"
    "src/magazine-parser/magazine-parser.csproj"
    "src/image-splitter/image-splitter.csproj"
    "src/find-links/find-links.csproj"
)

for p in "${projects[@]}"; do
    publish_project "$p"
done


echo "Done! All tools published to $BIN_DIR"
if ! echo "$PATH" | tr ':' '\n' | grep -xq "$BIN_DIR"; then
    echo "Make sure $BIN_DIR is in your PATH (e.g. add 'export PATH=\"$BIN_DIR:$PATH\"' to your shell profile)"
fi
