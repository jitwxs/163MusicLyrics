#!/bin/zsh
# Brings our app (pid in $STATE/app.pid) to the front and checks it is frontmost. Exits 1 and sends nothing otherwise.
# Call before EVERY click or key press: input must never land in another app.
source "$(dirname "$0")/env.sh"
pid=$(cat "$STATE/app.pid" 2>/dev/null) || { echo "ABORT: app not started"; exit 1; }
osascript -e "tell application \"System Events\" to set frontmost of (first process whose unix id is $pid) to true" >/dev/null 2>&1
sleep 0.4
front=$(osascript -e 'tell application "System Events" to get unix id of first application process whose frontmost is true' 2>/dev/null)
[[ "$front" == "$pid" ]] || { echo "ABORT: frontmost pid is '$front', expected $pid"; exit 1; }
