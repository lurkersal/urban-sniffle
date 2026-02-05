#!/bin/bash

if [ -z "$1" ]; then
  echo "Usage: $0 <4-digit-year>"
  exit 1
fi

YEAR="$1"

for dir in ./*; do
  if [[ -d "$dir" && "$dir" =~ ${YEAR}$ ]]; then
    echo "Processing $dir"
    (cd "$dir" && magazine-parser ./)
  fi
done
