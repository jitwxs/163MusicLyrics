#!/bin/zsh
# Builds (unless --no-build), starts our own MusicLyricApp, waits for its window and prints "pid <pid> window <id> <w>x<h>".
# Only this PID is ever driven or stopped; other running copies are left alone.
source "$(dirname "$0")/env.sh"
if [[ "$1" != "--no-build" ]]; then
  dotnet build "$REPO/cross-platform/MusicLyricApp" -v q 2>&1 | grep -E " error |Build succeeded" | sort -u
fi
[[ -f $STATE/app.pid ]] && kill -0 $(cat $STATE/app.pid) 2>/dev/null && { echo "already running: pid $(cat $STATE/app.pid)"; exit 1; }
cd "$APP_DIR" && nohup ./MusicLyricApp > "$STATE/app.log" 2>&1 &
echo $! > "$STATE/app.pid"
export MLA_PID=$(cat "$STATE/app.pid")
for i in {1..30}; do
  win=$("$TOOLS/winid" MusicLyricApp | head -1)
  [[ -n $win ]] && { echo "pid $MLA_PID window $win"; exit 0; }
  sleep 0.5
done
echo "no window after 15 s; see $STATE/app.log"; exit 1
