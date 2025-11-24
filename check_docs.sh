#!/bin/bash
find . -name "*.cs" | while read file; do
  awk '
  BEGIN { prev = "" }
  /^ *\/\/\// { prev = $0; next }
  /^ *public/ {
    if (prev !~ /^ *\/\/\//) {
      print FILENAME ":" NR ":" $0
    }
    prev = ""
  }
  ' "$file"
done
