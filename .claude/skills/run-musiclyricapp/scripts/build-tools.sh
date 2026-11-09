#!/bin/zsh
# Compiles the Swift input tools (winid, click, key, scroll, drag) into $STATE/bin. Run once per machine/state dir.
source "$(dirname "$0")/env.sh"
for t in winid click key scroll drag; do
  swiftc -O "$(dirname "$0")/$t.swift" -o "$TOOLS/$t" || exit 1
done
echo "tools in $TOOLS"
