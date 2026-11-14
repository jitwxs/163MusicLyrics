#!/bin/zsh
# Stops only the copy started by launch.sh, with SIGKILL. A plain kill (SIGTERM) is a graceful shutdown to Avalonia:
# the window closes and the settings are saved, so in-memory changes made while driving the app would reach the
# user's settings file. SIGKILL never runs that path.
source "$(dirname "$0")/env.sh"
[[ -f $STATE/app.pid ]] && kill -KILL $(cat "$STATE/app.pid") 2>/dev/null && echo "stopped $(cat "$STATE/app.pid")"
rm -f "$STATE/app.pid"
