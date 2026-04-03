#!/bin/bash

# Test script to verify metadata parsing fix
# March 19, 2026

echo "========================================"
echo "METADATA PARSING FIX - VERIFICATION TEST"
echo "========================================"
echo ""

TEST_FOLDER="/tmp/test-no-index/Club International 20-05, 1991"

echo "Step 1: Verify test folder exists..."
if [ ! -d "$TEST_FOLDER" ]; then
    echo "❌ Test folder not found: $TEST_FOLDER"
    echo "Creating test folder..."
    mkdir -p "$TEST_FOLDER"
    cd "$TEST_FOLDER"
    convert -size 800x1200 xc:gray -pointsize 72 -draw "text 300,600 'Page 1'" 001.jpg
    echo "✓ Test folder created"
else
    echo "✓ Test folder exists"
fi

echo ""
echo "Step 2: Verify no index file exists..."
if [ -f "$TEST_FOLDER/_index.json" ]; then
    echo "Removing existing _index.json for clean test..."
    rm "$TEST_FOLDER/_index.json"
fi
if [ -f "$TEST_FOLDER/_index.json~" ]; then
    rm "$TEST_FOLDER/_index.json~"
fi
echo "✓ No index file present"

echo ""
echo "Step 3: Check folder name format..."
FOLDER_NAME=$(basename "$TEST_FOLDER")
echo "Folder name: '$FOLDER_NAME'"
if [[ "$FOLDER_NAME" =~ ^(.+)[[:space:]]([0-9]{2})-([0-9]{2}),[[:space:]]*([0-9]{4})$ ]]; then
    MAGAZINE="${BASH_REMATCH[1]}"
    VOLUME="${BASH_REMATCH[2]}"
    NUMBER="${BASH_REMATCH[3]}"
    YEAR="${BASH_REMATCH[4]}"
    echo "✓ Valid format detected:"
    echo "  Magazine: $MAGAZINE"
    echo "  Volume:   $VOLUME"
    echo "  Number:   $NUMBER"
    echo "  Year:     $YEAR"
else
    echo "❌ Folder name doesn't match expected format"
    exit 1
fi

echo ""
echo "Step 4: Run IndexEditor and capture logs..."
echo "Command: IndexEditor \"$TEST_FOLDER\" (running for 3 seconds)"
timeout 3s IndexEditor "$TEST_FOLDER" > /tmp/indexeditor-test.log 2>&1 &
APP_PID=$!
sleep 3

echo ""
echo "Step 5: Check console output for metadata parsing..."
if grep -q "Parsed metadata.*'Club International'.*'20'.*'05'.*'1991'" /tmp/indexeditor-test.log; then
    echo "✓ Metadata parsing confirmed in logs:"
    grep "Parsed metadata" /tmp/indexeditor-test.log | head -1
else
    echo "❌ Metadata parsing not found in logs"
    echo "Log contents:"
    cat /tmp/indexeditor-test.log
    exit 1
fi

echo ""
echo "========================================"
echo "AUTOMATED TESTS PASSED ✓"
echo "========================================"
echo ""
echo "MANUAL TESTS REQUIRED:"
echo "1. Run: IndexEditor \"$TEST_FOLDER\""
echo "2. Verify TopBar shows:"
echo "   - Magazine: Club International"
echo "   - Vol: 20"
echo "   - No: 05"
echo "   - Year: 1991"
echo "3. Press Ctrl+S to save"
echo "4. Run: cat \"$TEST_FOLDER/_index.json\""
echo "5. Verify metadata section contains correct values"
echo ""

