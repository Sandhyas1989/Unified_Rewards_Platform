#!/usr/bin/env bash
# Internal helper — called by start-urp.command via osascript.
# Usage: launch-service.sh "Window Title" "/abs/path/to/project"
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$HOME/.dotnet:$PATH"

TITLE="${1:-URP Service}"
PROJ_DIR="${2:-.}"

# Set Terminal window/tab title
printf '\033]0;%s\007' "$TITLE"

echo ""
echo "  ══════════════════════════════════════════"
echo "    $TITLE"
echo "  ══════════════════════════════════════════"
echo "  Dir: $PROJ_DIR"
echo ""

cd "$PROJ_DIR"
dotnet run

echo ""
echo "  ── Process exited. This window stays open. ──"
exec bash
