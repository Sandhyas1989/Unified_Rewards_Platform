#!/usr/bin/env bash
# ============================================================
#  stop-urp.command  —  Double-click in Finder to stop URP
# ============================================================

REPO="$(cd "$(dirname "$0")" && pwd)"
LOG_DIR="$REPO/logs"

GREEN='\033[0;32m'; YELLOW='\033[1;33m'; RED='\033[0;31m'
BOLD='\033[1m'; CYAN='\033[0;36m'; NC='\033[0m'
ok()     { echo -e "${GREEN}[OK]${NC} $1"; }
info()   { echo -e "${YELLOW}[..] ${NC}$1"; }
header() { echo -e "\n${BOLD}${CYAN}=====  $1  =====${NC}"; }

clear
echo ""
echo "  =================================================="
echo "   Unified Rewards Platform - Stopping..."
echo "  =================================================="
echo ""

# ── Kill by PID files ─────────────────────────────────────────────────────
header "Stopping services"

STOPPED=0
if [[ -d "$LOG_DIR" ]]; then
  for pidfile in "$LOG_DIR"/*.pid; do
    [[ -f "$pidfile" ]] || continue
    name=$(basename "$pidfile" .pid)
    pid=$(cat "$pidfile" 2>/dev/null || true)
    if [[ -n "$pid" ]] && kill -0 "$pid" 2>/dev/null; then
      kill -TERM "$pid" 2>/dev/null || true
      ok "Stopped $name (PID $pid)"
      STOPPED=$((STOPPED + 1))
    else
      info "$name — not running (stale PID file)"
    fi
    rm -f "$pidfile"
  done
fi

# ── Also kill by name in case PID files are missing ──────────────────────
for svc in UnifiedRewards.EmployeeProfile UnifiedRewards.BenefitsCatalogue \
           UnifiedRewards.CompensationRules UnifiedRewards.DocumentProcessing \
           UnifiedRewards.ReimbursementWorkflow UnifiedRewards.PayrollIntegration \
           UnifiedRewards.ReportingCompliance UnifiedRewards.Gateway; do
  pids=$(pgrep -f "$svc" 2>/dev/null || true)
  if [[ -n "$pids" ]]; then
    echo "$pids" | xargs kill -TERM 2>/dev/null || true
    ok "Stopped $svc"
    STOPPED=$((STOPPED + 1))
  fi
done

# ── Kill frontend ports ───────────────────────────────────────────────────
header "Stopping frontend"
for port in 3000 3001 3002 3003 3004; do
  pid=$(lsof -ti tcp:$port 2>/dev/null || true)
  if [[ -n "$pid" ]]; then
    kill -TERM "$pid" 2>/dev/null || true
    ok "Stopped port $port (PID $pid)"
    STOPPED=$((STOPPED + 1))
  fi
done

# ── Wait for graceful shutdown ────────────────────────────────────────────
if [[ $STOPPED -gt 0 ]]; then
  info "Waiting 5s for graceful shutdown..."
  sleep 5
  # Force-kill anything still alive
  pkill -KILL -f "UnifiedRewards\." 2>/dev/null || true
  for port in 3000 3001 3002 3003 3004; do
    lsof -ti tcp:$port 2>/dev/null | xargs kill -KILL 2>/dev/null || true
  done
fi

echo ""
header "Done"
echo ""
if [[ $STOPPED -eq 0 ]]; then
  info "No URP services were running."
else
  ok "Stopped $STOPPED process(es)."
fi
echo ""
echo "  To restart: double-click Start URP.app"
echo ""
read -r -p "  Press Enter to close..."
