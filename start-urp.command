#!/usr/bin/env bash
# ============================================================
#  start-urp.command  —  Double-click in Finder to launch URP
#  Runs all services as background processes in this window.
#  Logs written to logs/ folder in the repo root.
# ============================================================

REPO="$(cd "$(dirname "$0")" && pwd)"
GATEWAY="http://localhost:5080"
LOG_DIR="$REPO/logs"

export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$HOME/.dotnet:$PATH"

BOLD='\033[1m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'
RED='\033[0;31m'; CYAN='\033[0;36m'; NC='\033[0m'
ok()     { echo -e "${GREEN}[OK]${NC} $1"; }
info()   { echo -e "${YELLOW}[..] ${NC}$1"; }
err()    { echo -e "${RED}[!!]${NC} $1"; }
header() { echo -e "\n${BOLD}${CYAN}=====  $1  =====${NC}"; }

mkdir -p "$LOG_DIR"
clear

echo ""
echo "  =================================================="
echo "   Unified Rewards Platform - Starting up..."
echo "  =================================================="
echo "  Repo:  $REPO"
echo "  Logs:  $LOG_DIR"
echo ""

# ── 1. Pre-flight ─────────────────────────────────────────────────────────
header "Checking tools"

if ! command -v dotnet &>/dev/null; then
  err ".NET SDK not found in PATH."
  echo ""
  echo "  Fix: open ~/.zshrc, add these two lines, then open a NEW Terminal:"
  echo '    export DOTNET_ROOT="$HOME/.dotnet"'
  echo '    export PATH="$HOME/.dotnet:$PATH"'
  read -r -p $'\n  Press Enter to exit...'; exit 1
fi
DOTNET_VER="$(dotnet --version 2>/dev/null)"
if [[ "$DOTNET_VER" != 8.* ]]; then
  err ".NET $DOTNET_VER found — need 8.x. Open a NEW Terminal and try again."
  read -r -p $'\n  Press Enter to exit...'; exit 1
fi
ok ".NET $DOTNET_VER"

if ! command -v node &>/dev/null; then
  err "Node.js not found. Install from https://nodejs.org (LTS)."
  read -r -p $'\n  Press Enter to exit...'; exit 1
fi
ok "Node $(node --version)  /  npm $(npm --version)"

# ── 2. Stop anything already running ────────────────────────────────────
header "Stopping existing processes"

# Kill by PID files from previous run
for pidfile in "$LOG_DIR"/*.pid; do
  [[ -f "$pidfile" ]] || continue
  pid=$(cat "$pidfile" 2>/dev/null || true)
  if [[ -n "$pid" ]] && kill -0 "$pid" 2>/dev/null; then
    kill -TERM "$pid" 2>/dev/null || true
    info "Stopped PID $pid ($(basename "$pidfile" .pid))"
  fi
  rm -f "$pidfile"
done

# Also kill by name in case PID files are stale
pkill -TERM -f "UnifiedRewards\." 2>/dev/null || true
for port in 3000 3001 3002 3003 3004; do
  lsof -ti tcp:$port 2>/dev/null | xargs kill -TERM 2>/dev/null || true
done
sleep 2
ok "Clean"

# ── 3. Database option ───────────────────────────────────────────────────
header "Database"
DB_COUNT=$(find "$REPO/services" -name "*.db" 2>/dev/null | wc -l | tr -d ' ')
if [[ "$DB_COUNT" -gt 0 ]]; then
  echo ""
  echo "  Found $DB_COUNT existing SQLite database(s)."
  echo ""
  echo "  Press Enter     = keep existing data (users, history preserved)"
  echo "  Press r + Enter = delete all (completely fresh start, re-seeds accounts)"
  echo ""
  read -r -p "  Your choice > " DB_CHOICE
  if [[ "${DB_CHOICE,,}" == "r" ]]; then
    find "$REPO/services" -name "*.db" -delete 2>/dev/null || true
    ok "Databases deleted — services will create them fresh"
  else
    ok "Keeping existing data"
  fi
else
  ok "No existing databases — will be created on first startup"
fi

# ── 4. Helper: start a process in background with a log file ────────────
_start() {
  local name="$1"
  local log="$LOG_DIR/${name}.log"
  shift
  # "$@" is the command to run
  echo "" > "$log"
  # nohup detaches from this terminal so services survive if this window is closed
  nohup bash -c "$*" >> "$log" 2>&1 &
  echo $! > "$LOG_DIR/${name}.pid"
}

# ── 5. Start all 8 backend services ─────────────────────────────────────
header "Starting backend services (background)"
echo "  First run compiles all 8 projects — takes ~60-90 seconds."
echo ""

_start "employee-profile" \
  "export DOTNET_ROOT=\"$HOME/.dotnet\"; export PATH=\"$HOME/.dotnet:\$PATH\"; cd '$REPO/services/employee-profile/UnifiedRewards.EmployeeProfile' && dotnet run"
info "employee-profile      → logs/employee-profile.log"

_start "benefits-catalogue" \
  "export DOTNET_ROOT=\"$HOME/.dotnet\"; export PATH=\"$HOME/.dotnet:\$PATH\"; cd '$REPO/services/benefits-catalogue/UnifiedRewards.BenefitsCatalogue' && dotnet run"
info "benefits-catalogue    → logs/benefits-catalogue.log"

_start "compensation-rules" \
  "export DOTNET_ROOT=\"$HOME/.dotnet\"; export PATH=\"$HOME/.dotnet:\$PATH\"; cd '$REPO/services/compensation-rules/UnifiedRewards.CompensationRules' && dotnet run"
info "compensation-rules    → logs/compensation-rules.log"

_start "document-processing" \
  "export DOTNET_ROOT=\"$HOME/.dotnet\"; export PATH=\"$HOME/.dotnet:\$PATH\"; cd '$REPO/services/document-processing/UnifiedRewards.DocumentProcessing' && dotnet run"
info "document-processing   → logs/document-processing.log"

_start "reimbursement" \
  "export DOTNET_ROOT=\"$HOME/.dotnet\"; export PATH=\"$HOME/.dotnet:\$PATH\"; cd '$REPO/services/reimbursement-workflow/UnifiedRewards.ReimbursementWorkflow' && dotnet run"
info "reimbursement         → logs/reimbursement.log"

_start "payroll" \
  "export DOTNET_ROOT=\"$HOME/.dotnet\"; export PATH=\"$HOME/.dotnet:\$PATH\"; cd '$REPO/services/payroll-integration/UnifiedRewards.PayrollIntegration' && dotnet run"
info "payroll               → logs/payroll.log"

_start "reporting" \
  "export DOTNET_ROOT=\"$HOME/.dotnet\"; export PATH=\"$HOME/.dotnet:\$PATH\"; cd '$REPO/services/reporting-compliance/UnifiedRewards.ReportingCompliance' && dotnet run"
info "reporting             → logs/reporting.log"

_start "gateway" \
  "export DOTNET_ROOT=\"$HOME/.dotnet\"; export PATH=\"$HOME/.dotnet:\$PATH\"; cd '$REPO/services/gateway/UnifiedRewards.Gateway' && dotnet run"
info "gateway               → logs/gateway.log"

echo ""
echo "  All 8 services launched. Waiting for gateway to be ready..."

# ── 6. Wait for gateway ───────────────────────────────────────────────────
header "Waiting for gateway (up to 3 min)"

WAITED=0
MAX_WAIT=180
while ! curl -sf "$GATEWAY/health" &>/dev/null; do
  printf "\r  Waiting... %ds elapsed" "$WAITED"
  sleep 3
  WAITED=$((WAITED + 3))
  if [[ $WAITED -ge $MAX_WAIT ]]; then
    echo ""
    echo ""
    err "Gateway did not start after ${MAX_WAIT}s."
    echo ""
    echo "  Last 25 lines of logs/gateway.log:"
    echo "  ----------------------------------"
    tail -25 "$LOG_DIR/gateway.log" 2>/dev/null || echo "  (log file empty)"
    echo ""
    echo "  To diagnose: open a Terminal and run:"
    echo "    cd '$REPO/services/gateway/UnifiedRewards.Gateway' && dotnet run"
    read -r -p $'\n  Press Enter to exit...'; exit 1
  fi
done
printf "\r"
ok "Gateway is healthy  (took ${WAITED}s)                    "

# ── 7. Seed demo accounts ────────────────────────────────────────────────
header "Seeding demo accounts"

_register() {
  local email="$1" pass="$2" first="$3" last="$4" role="$5"
  local s
  s=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$GATEWAY/api/auth/register" \
    -H "Content-Type: application/json" \
    -d "{\"email\":\"$email\",\"password\":\"$pass\",\"firstName\":\"$first\",\"lastName\":\"$last\",\"role\":\"$role\"}")
  case "$s" in
    200|201) ok "Registered $role: $email" ;;
    400|409) ok "$email already exists — skipped" ;;
    *)       err "Failed to register $email (HTTP $s)" ;;
  esac
}

_register "hr@urp.local"       "Password123!" "Priya" "Sharma" "HrAdmin"
_register "manager@urp.local"  "Password123!" "Rohan" "Mehta"  "Manager"
_register "finance@urp.local"  "Password123!" "Carol" "Lee"    "Finance"
_register "employee@urp.local" "Password123!" "Alice" "Smith"  "Employee"

info "Seeding benefit plan..."
HR_TOKEN=$(curl -s -X POST "$GATEWAY/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"hr@urp.local","password":"Password123!"}' \
  | python3 -c "import sys,json; d=json.load(sys.stdin); print(d.get('token',''))" 2>/dev/null || echo "")
if [[ -n "$HR_TOKEN" ]]; then
  BP=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$GATEWAY/api/benefit-plans" \
    -H "Authorization: Bearer $HR_TOKEN" -H "Content-Type: application/json" \
    -d '{"name":"Medical Cover","description":"Annual medical reimbursement","coverageAmount":50000,"monthlyCost":500,"currencyCode":"INR"}')
  case "$BP" in
    200|201) ok "Benefit plan: Medical Cover (INR 50,000) created" ;;
    *)       ok "Benefit plan already exists" ;;
  esac
else
  err "Could not log in as HR — benefit plan seed skipped (run again after first login)"
fi

# ── 8. Start frontend ────────────────────────────────────────────────────
header "Starting frontend (background)"

FE_LOG="$LOG_DIR/frontend.log"
echo "" > "$FE_LOG"
nohup bash -c "
  cd '$REPO/frontend'
  if [[ ! -d node_modules ]]; then
    echo '[npm install running...]' >> '$FE_LOG'
    npm install >> '$FE_LOG' 2>&1
  fi
  npm start >> '$FE_LOG' 2>&1
" >> "$FE_LOG" 2>&1 &
echo $! > "$LOG_DIR/frontend.pid"
info "Frontend started  → logs/frontend.log"
echo ""
echo "  Waiting for frontend (npm install on first run takes ~1-2 min)..."

# ── 9. Wait for frontend ──────────────────────────────────────────────────
header "Waiting for frontend (up to 5 min)"

WAITED=0
MAX_WAIT=300
while ! curl -sf "http://localhost:3000" &>/dev/null; do
  printf "\r  Waiting... %ds elapsed" "$WAITED"
  sleep 3
  WAITED=$((WAITED + 3))
  if [[ $WAITED -ge $MAX_WAIT ]]; then
    echo ""
    err "Frontend did not start after ${MAX_WAIT}s."
    echo ""
    echo "  Last 25 lines of logs/frontend.log:"
    echo "  ------------------------------------"
    tail -25 "$FE_LOG" 2>/dev/null || echo "  (log file empty)"
    read -r -p $'\n  Press Enter to exit...'; exit 1
  fi
done
printf "\r"
ok "Frontend ready  (took ${WAITED}s)                    "

# ── 10. Open browser ──────────────────────────────────────────────────────
open "http://localhost:3000" 2>/dev/null || true

# ── Done ─────────────────────────────────────────────────────────────────
echo ""
echo ""
echo "  =================================================="
echo "   ALL SYSTEMS RUNNING"
echo "  =================================================="
echo ""
echo "  App:      http://localhost:3000"
echo "  Gateway:  http://localhost:5080"
echo ""
echo "  Demo accounts (password: Password123!)"
echo "    employee@urp.local  manager@urp.local"
echo "    hr@urp.local        finance@urp.local"
echo ""
echo "  Log files (tail -f to watch live):"
echo "    logs/gateway.log     logs/frontend.log"
echo "    logs/employee-profile.log   (etc.)"
echo ""
echo "  To stop:  double-click Stop URP.app"
echo "    or run: ./stop-urp.command"
echo ""
echo "  Services keep running even if you close this window."
echo ""
read -r -p "  Press Enter to close this window..."
