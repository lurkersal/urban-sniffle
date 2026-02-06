#!/usr/bin/env bash
set -euo pipefail

# Attempt to find the IndexEditor window and raise it to the front.
# Usage: ./scripts/raise-indexeditor.sh

command -v wmctrl >/dev/null 2>&1 || echo "Note: wmctrl not installed. The script will try xdotool as a fallback."
command -v xdotool >/dev/null 2>&1 || echo "Note: xdotool not installed. Some operations may fail."

# Find candidate dotnet processes running IndexEditor
pids=$(ps -ef | egrep "dotnet .*IndexEditor|IndexEditor\.dll|IndexEditor\b" | egrep -v egrep | awk '{print $2}' || true)

if [ -z "${pids}" ]; then
  echo "No IndexEditor/dotnet process found. Run the app first (./scripts/run-index-editor.sh) and then re-run this script." 
  exit 1
fi

echo "Found IndexEditor PIDs: $pids"

# Try wmctrl to find windows owned by those PIDs
if command -v wmctrl >/dev/null 2>&1; then
  echo "Searching windows with wmctrl -lp..."
  wmctrl -lp | awk '{print $1, $2, substr($0, index($0,$5))}' > /tmp/wmctrl-list.txt || true
  echo "wmctrl windows (filtered):"
  grep -Ei "IndexEditor|indexeditor|Index Editor|Index" /tmp/wmctrl-list.txt || true

  for pid in $pids; do
    # find any window with this PID
    winid=$(awk -v p=$pid '$2==p {print $1; exit}' /tmp/wmctrl-list.txt || true)
    if [ -n "$winid" ]; then
      echo "Activating window id $winid for pid $pid"
      wmctrl -i -a $winid || echo "wmctrl -i -a failed for $winid"
      echo "Done (wmctrl)"
      exit 0
    fi
  done
  echo "No window owned by IndexEditor PIDs found via wmctrl. Listing all windows for manual inspection:"
  wmctrl -l || true
fi

# Fallback: use xdotool to search by name
if command -v xdotool >/dev/null 2>&1; then
  echo "Attempting xdotool search by name 'IndexEditor'..."
  wid=$(xdotool search --name IndexEditor 2>/dev/null || true)
  if [ -n "$wid" ]; then
    echo "Found window(s) by name: $wid"
    for w in $wid; do
      xdotool windowactivate $w || echo "xdotool windowactivate failed for $w"
    done
    exit 0
  fi

  echo "Attempting xdotool search by class 'IndexEditor'..."
  wid2=$(xdotool search --class IndexEditor 2>/dev/null || true)
  if [ -n "$wid2" ]; then
    echo "Found window(s) by class: $wid2"
    for w in $wid2; do
      xdotool windowactivate $w || echo "xdotool windowactivate failed for $w"
    done
    exit 0
  fi
fi

# Last attempt: print helpful diagnostics
echo "Could not find or raise the IndexEditor window automatically. Helpful diagnostics:" 
echo "1) Process list for IndexEditor/dotnet:"
ps -ef | egrep "dotnet .*IndexEditor|IndexEditor\.dll|IndexEditor\b" | egrep -v egrep || true

if command -v wmctrl >/dev/null 2>&1; then
  echo "2) wmctrl -l output:"
  wmctrl -l || true
fi

if command -v xwininfo >/dev/null 2>&1; then
  echo "3) xwininfo -root -tree (top 200 lines):"
  xwininfo -root -tree | sed -n '1,200p' || true
fi

echo "If the window is still invisible, try running the app from a terminal inside your desktop session (not from Rider) and then run this script; or attach Rider to the running dotnet process." 
