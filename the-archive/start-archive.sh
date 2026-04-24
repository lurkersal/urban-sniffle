#!/bin/bash
# Start TheArchive - Stop any existing instance and start fresh

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
APP_DIR="$SCRIPT_DIR/src/TheArchive"

echo "🚀 Starting The Archive..."
echo ""

# First, stop any running instances
echo "1️⃣  Stopping existing instances..."
bash "$SCRIPT_DIR/stop-archive.sh"
echo ""

# Navigate to app directory
echo "2️⃣  Navigating to $APP_DIR"
cd "$APP_DIR" || exit 1
echo ""

# Start the application
echo "3️⃣  Starting application..."
echo "   Running on: http://localhost:5163"
echo ""
echo "   Press Ctrl+C to stop"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

dotnet run

