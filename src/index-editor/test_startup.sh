#!/bin/bash
# Test startup with ./ argument
cd bin/Debug/net8.0
./IndexEditor "./" 2>&1 | grep -E "(LoadArticlesFromFolder|CurrentFolder|FolderPicker)" | head -20
