#!/bin/bash

# Publish self-contained builds to ~/bin
BIN_DIR="$HOME/bin"

# Create bin directory if it doesn't exist
mkdir -p "$BIN_DIR"

echo "Publishing tools to $BIN_DIR..."


# Publish file-renamer
echo "Publishing file-renamer..."
dotnet publish src/file-renamer/file-renamer.csproj \
    -c Release \
    -r linux-x64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:PublishTrimmed=true \
    -o "$BIN_DIR"

# Publish magazine-parser
echo "Publishing magazine-parser..."
dotnet publish src/magazine-parser/magazine-parser.csproj \
    -c Release \
    -r linux-x64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:PublishTrimmed=true \
    -o "$BIN_DIR"



# Publish image-splitter
echo "Publishing image-splitter..."
dotnet publish src/image-splitter/image-splitter.csproj \
    -c Release \
    -r linux-x64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:PublishTrimmed=true \
    -o "$BIN_DIR"

# Publish find-links
echo "Publishing find-links..."
dotnet publish src/find-links/find-links.csproj \
    -c Release \
    -r linux-x64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:PublishTrimmed=true \
    -o "$BIN_DIR"


echo "Done! All tools published to $BIN_DIR"
echo "Make sure $BIN_DIR is in your PATH"
