#!/bin/zsh
# Captures our app's window to $STATE/shots/<name>.png and a 1080-wide copy <name>-s.png for reading; prints the path.
source "$(dirname "$0")/env.sh"
export MLA_PID=$(cat "$STATE/app.pid")
win=$("$TOOLS/winid" MusicLyricApp | head -1 | cut -d' ' -f1)
[[ -n $win ]] || { echo "no window"; exit 1; }
screencapture -x -o -l$win "$STATE/shots/$1.png" && sips -Z 1080 "$STATE/shots/$1.png" --out "$STATE/shots/$1-s.png" >/dev/null
echo "$STATE/shots/$1-s.png"
