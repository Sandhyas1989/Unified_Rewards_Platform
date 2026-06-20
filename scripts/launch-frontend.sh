#!/usr/bin/env bash
# Internal helper — called by start-urp.command via osascript.
FRONTEND_DIR="${1:-$(dirname "$0")/../frontend}"

printf '\033]0;URP: Frontend\007'
echo ""
echo "  ══════════════════════════════════════════"
echo "    URP: Frontend  (Module Federation Shell)"
echo "  ══════════════════════════════════════════"
echo ""

cd "$FRONTEND_DIR"

if [[ ! -d node_modules ]]; then
  echo "  node_modules not found — running npm install (first-time only, ~1-2 min)..."
  echo ""
  npm install
fi

echo "  Starting webpack-dev-server on ports 3000-3004..."
echo ""
npm start

echo ""
echo "  ── Frontend exited. This window stays open. ──"
exec bash
