#!/bin/bash

# Parse all Mayfair magazine issues
MAYFAIR_DIR="/media/justin/New Volume/Magazines/Mayfair"
PARSER="/home/justin/bin/magazine-parser"

# Start and end parameters (format: volume-number, e.g., 17-03)
# Leave empty to process all issues
START_ISSUE="${1:-}"  # e.g., 17-01
END_ISSUE="${2:-}"    # e.g., 17-12

# Check if the Mayfair directory exists
if [ ! -d "$MAYFAIR_DIR" ]; then
    echo "ERROR: Mayfair directory not found: $MAYFAIR_DIR"
    exit 1
fi

# Check if the parser exists
if [ ! -f "$PARSER" ]; then
    echo "ERROR: Parser not found: $PARSER"
    echo "Run ./publish-tools.sh first to build the parser"
    exit 1
fi

# Function to extract volume and number from folder name
extract_vol_num() {
    local folder="$1"
    # Extract pattern like "17-03" or "17-3" from folder name
    if [[ "$folder" =~ ([0-9]+)-0?([0-9]+) ]]; then
        local vol="${BASH_REMATCH[1]}"
        local num="${BASH_REMATCH[2]}"
        printf "%02d-%02d" "$vol" "$num"
    else
        echo ""
    fi
}

# Function to compare issue numbers (returns 0 if equal, 1 if first > second, -1 if first < second)
compare_issues() {
    local issue1="$1"
    local issue2="$2"
    
    if [ "$issue1" = "$issue2" ]; then
        return 0
    elif [[ "$issue1" > "$issue2" ]]; then
        return 1
    else
        return 255  # -1 in bash
    fi
}

# Normalize start and end issues
if [ -n "$START_ISSUE" ]; then
    if [[ "$START_ISSUE" =~ ([0-9]+)-0?([0-9]+) ]]; then
        START_ISSUE=$(printf "%02d-%02d" "${BASH_REMATCH[1]}" "${BASH_REMATCH[2]}")
    fi
fi

if [ -n "$END_ISSUE" ]; then
    if [[ "$END_ISSUE" =~ ([0-9]+)-0?([0-9]+) ]]; then
        END_ISSUE=$(printf "%02d-%02d" "${BASH_REMATCH[1]}" "${BASH_REMATCH[2]}")
    fi
fi

# Count total folders
total=$(find "$MAYFAIR_DIR" -mindepth 1 -maxdepth 1 -type d | wc -l)
current=0
processed=0
skipped=0

echo "Mayfair Magazine Parser"
echo "=========================================="
if [ -n "$START_ISSUE" ]; then
    echo "Start: $START_ISSUE"
fi
if [ -n "$END_ISSUE" ]; then
    echo "End: $END_ISSUE"
fi
echo "Total folders: $total"
echo "=========================================="

# Loop through each subdirectory in the Mayfair folder (sorted)
while IFS= read -r issue_dir; do
    current=$((current + 1))
    folder_name=$(basename "$issue_dir")
    issue_id=$(extract_vol_num "$folder_name")
        
        # Check if we should process this issue
        should_process=true
        
        if [ -n "$issue_id" ]; then
            # Check if before start
            if [ -n "$START_ISSUE" ]; then
                if [[ "$issue_id" < "$START_ISSUE" ]]; then
                    should_process=false
                fi
            fi
            
            # Check if after end
            if [ -n "$END_ISSUE" ]; then
                if [[ "$issue_id" > "$END_ISSUE" ]]; then
                    should_process=false
                fi
            fi
        fi
        
        if [ "$should_process" = true ]; then
            processed=$((processed + 1))
            echo ""
            echo "[$processed] Processing: $folder_name (Issue: $issue_id)"
            echo "=========================================="
            
            # Run the parser on this directory
            "$PARSER" "$issue_dir"
            
            # Check exit status
            if [ $? -eq 0 ]; then
                echo "✓ Successfully processed: $folder_name"
            else
                echo "✗ Failed to process: $folder_name"
            fi
        else
            skipped=$((skipped + 1))
            echo "⊘ Skipping: $folder_name (Issue: $issue_id)"
        fi
done < <(find "$MAYFAIR_DIR" -mindepth 1 -maxdepth 1 -type d | sort)

echo ""
echo "=========================================="
echo "Summary:"
echo "  Total folders: $total"
echo "  Processed: $processed"
echo "  Skipped: $skipped"
echo "=========================================="
