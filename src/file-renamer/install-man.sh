#!/bin/bash
# Install man page for file-renamer

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MAN_FILE="$SCRIPT_DIR/file-renamer.1"

if [ ! -f "$MAN_FILE" ]; then
    echo "Error: Man page file not found: $MAN_FILE"
    exit 1
fi

# Check if user wants system-wide or user-local installation
if [ "$1" = "--system" ] || [ "$1" = "-s" ]; then
    echo "Installing man page system-wide (requires sudo)..."
    sudo cp "$MAN_FILE" /usr/local/share/man/man1/
    sudo mandb
    echo "Man page installed to /usr/local/share/man/man1/"
    echo "You can now run: man file-renamer"
else
    echo "Installing man page for current user..."
    USER_MAN_DIR="$HOME/.local/share/man/man1"
    mkdir -p "$USER_MAN_DIR"
    cp "$MAN_FILE" "$USER_MAN_DIR/"
    
    # Check if user's man directory is in MANPATH
    if ! echo "$MANPATH" | grep -q "$HOME/.local/share/man"; then
        echo ""
        echo "Man page installed to $USER_MAN_DIR"
        echo ""
        echo "To use it, add this line to your ~/.bashrc or ~/.bash_profile:"
        echo "  export MANPATH=\"\$HOME/.local/share/man:\$MANPATH\""
        echo ""
        echo "Then run: source ~/.bashrc"
        echo ""
        echo "Or run this command to add it automatically:"
        echo "  echo 'export MANPATH=\"\$HOME/.local/share/man:\$MANPATH\"' >> ~/.bashrc && source ~/.bashrc"
    else
        echo "Man page installed to $USER_MAN_DIR"
        echo "You can now run: man file-renamer"
    fi
fi

echo ""
echo "Installation complete!"

