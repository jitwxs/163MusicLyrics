#!/bin/zsh
# Stops only the copy started by launch.sh. A kill does not save settings (they are saved on leaving settings or quitting).
source "$(dirname "$0")/env.sh"
[[ -f $STATE/app.pid ]] && kill $(cat "$STATE/app.pid") 2>/dev/null && echo "stopped $(cat "$STATE/app.pid")"
rm -f "$STATE/app.pid"
