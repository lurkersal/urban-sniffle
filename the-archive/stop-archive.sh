#!/bin/bash
# Stop TheArchive - Kill any running instance

echo "🔍 Checking for running TheArchive processes..."

# Find process using port 5163
PORT_PID=$(lsof -ti:5163 2>/dev/null)

if [ -z "$PORT_PID" ]; then
    echo "✅ No process found using port 5163"
else
    echo "⚠️  Found process using port 5163 (PID: $PORT_PID)"
    echo "   Killing process..."
    kill -9 $PORT_PID 2>/dev/null
    sleep 1
    echo "✅ Process killed"
fi

# Also check for any TheArchive dotnet processes
ARCHIVE_PIDS=$(pgrep -f "TheArchive.dll" 2>/dev/null)

if [ -n "$ARCHIVE_PIDS" ]; then
    echo "⚠️  Found TheArchive dotnet processes: $ARCHIVE_PIDS"
    echo "   Killing processes..."
    pkill -9 -f "TheArchive.dll" 2>/dev/null
    sleep 1
    echo "✅ All TheArchive processes killed"
fi

echo ""
echo "🎉 Port 5163 is now available!"
echo "   You can now run: cd the-archive/src/TheArchive && dotnet run"

