# Shared settings for the run-musiclyricapp scripts; sourced, not run.
REPO=$(git -C "$(dirname "$0")" rev-parse --show-toplevel)
APP_DIR=$REPO/cross-platform/MusicLyricApp/bin/Debug/net10.0
STATE=${MLA_STATE:-${TMPDIR:-/tmp}/musiclyricapp-run}
TOOLS=$STATE/bin
export DOTNET_ROOT=$HOME/.dotnet PATH=$HOME/.dotnet:$TOOLS:$PATH
mkdir -p "$STATE/shots" "$TOOLS"
