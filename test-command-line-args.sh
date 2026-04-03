#!/bin/bash
# Test script to verify command line argument parsing fix
# Tests that --log-level doesn't get treated as a folder path

echo "=== Testing Command Line Argument Parsing Fix ==="
echo ""

# Test 1: --log-level debug (should not try to open "--log-level" folder)
echo "Test 1: Running with --log-level debug"
echo "Expected: App starts with debug logging, no attempt to open '--log-level' folder"
echo "Command: dotnet run --project src/index-editor/IndexEditor.csproj -- --log-level debug"
echo ""
# Uncomment to run:
# dotnet run --project src/index-editor/IndexEditor.csproj -- --log-level debug

# Test 2: -l info (short form)
echo "Test 2: Running with -l info (short form)"
echo "Expected: App starts with info logging"
echo "Command: dotnet run --project src/index-editor/IndexEditor.csproj -- -l info"
echo ""
# Uncomment to run:
# dotnet run --project src/index-editor/IndexEditor.csproj -- -l info

# Test 3: --log-level debug with folder path
echo "Test 3: Running with --log-level debug /tmp"
echo "Expected: App starts with debug logging AND opens /tmp folder"
echo "Command: dotnet run --project src/index-editor/IndexEditor.csproj -- --log-level debug /tmp"
echo ""
# Uncomment to run:
# dotnet run --project src/index-editor/IndexEditor.csproj -- --log-level debug /tmp

echo "=== Test script complete ==="
echo "Uncomment the test commands in this script to run them"

