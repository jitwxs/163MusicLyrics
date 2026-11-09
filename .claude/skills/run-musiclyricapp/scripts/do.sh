#!/bin/zsh
# Runs one input tool against our app after the frontmost check, e.g.:
#   do.sh click 75 145      do.sh key 36 cmd      do.sh scroll 640 500 -20      do.sh drag 600 20 120 0
# Coordinates are window points from the top-left (screenshots are 2x: divide pixel positions by 2).
source "$(dirname "$0")/env.sh"
"$(dirname "$0")/safe.sh" || exit 1
export MLA_PID=$(cat "$STATE/app.pid")
tool=$1; shift
case $tool in
  key) "$TOOLS/key" "$@" ;;
  *)   "$TOOLS/$tool" MusicLyricApp "$@" >/dev/null ;;
esac
sleep ${MLA_WAIT:-0.6}
