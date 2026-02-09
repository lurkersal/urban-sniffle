#!/bin/bash

for dir in ./*; do
  if [[ -d "$dir" && "$dir" =~ [0-9]{4}$ ]]; then
    echo "Processing $dir"
    (cd "$dir" && magazine-parser ./)
  fi
done
