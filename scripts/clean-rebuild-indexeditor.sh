#!/bin/bash
set -e

echo "========================================="
echo "CLEAN REBUILD OF INDEXEDITOR"
echo "========================================="
echo ""

cd /home/justin/repos/urban-sniffle

echo "Step 1: Removing old binary..."
rm -f ~/bin/IndexEditor
echo "✓ Old binary removed"
echo ""

echo "Step 2: Cleaning build artifacts..."
dotnet clean src/index-editor/IndexEditor.csproj > /dev/null 2>&1
echo "✓ Build cleaned"
echo ""

echo "Step 3: Publishing IndexEditor..."
dotnet publish src/index-editor/IndexEditor.csproj \
    -c Release \
    -r linux-x64 \
    --self-contained \
    -p:PublishSingleFile=true \
    -o ~/bin \
    2>&1 | grep -v "warning" | tail -10

if [ -f ~/bin/IndexEditor ]; then
    echo ""
    echo "✓ Build successful!"
    echo "Binary: ~/bin/IndexEditor"
    ls -lh ~/bin/IndexEditor
else
    echo ""
    echo "✗ Build failed - binary not created"
    exit 1
fi

