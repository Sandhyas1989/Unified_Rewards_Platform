#!/usr/bin/env bash
# Seed demo accounts and sample data for local development.
# Run AFTER all 8 backend services are up: ./scripts/seed-local.sh
# Safe to run multiple times — duplicate-email errors are ignored.

set -euo pipefail
GATEWAY="http://localhost:5080"
BOLD='\033[1m'
GREEN='\033[0;32m'
RED='\033[0;31m'
NC='\033[0m'

ok()   { echo -e "${GREEN}✓${NC} $1"; }
fail() { echo -e "${RED}✗${NC} $1"; }

register() {
  local email=$1 pass=$2 first=$3 last=$4 role=$5
  local status
  status=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$GATEWAY/api/auth/register" \
    -H "Content-Type: application/json" \
    -d "{\"email\":\"$email\",\"password\":\"$pass\",\"firstName\":\"$first\",\"lastName\":\"$last\",\"role\":\"$role\"}")
  if [[ "$status" == "201" || "$status" == "200" ]]; then
    ok "Registered $role: $email"
  elif [[ "$status" == "409" || "$status" == "400" ]]; then
    ok "$email already exists — skipped"
  else
    fail "Failed to register $email (HTTP $status)"
  fi
}

token() {
  local email=$1 pass=$2
  curl -s -X POST "$GATEWAY/api/auth/login" \
    -H "Content-Type: application/json" \
    -d "{\"email\":\"$email\",\"password\":\"$pass\"}" \
    | python3 -c "import sys,json; d=json.load(sys.stdin); print(d.get('token',''))" 2>/dev/null
}

echo ""
echo -e "${BOLD}Unified Rewards Platform — Local Seed${NC}"
echo "Gateway: $GATEWAY"
echo ""

# ── 1. Check gateway is reachable ─────────────────────────────────────────
echo "Checking gateway health..."
if ! curl -sf "$GATEWAY/health" > /dev/null; then
  echo ""
  fail "Gateway is not reachable at $GATEWAY."
  echo "   Start all 8 services first (see docs/Deployment_and_Testing_Guide.md),"
  echo "   then re-run this script."
  exit 1
fi
ok "Gateway is healthy"
echo ""

# ── 2. Register demo accounts ─────────────────────────────────────────────
echo -e "${BOLD}Creating demo accounts (password: Password123!)${NC}"
register "hr@urp.local"       "Password123!" "Priya"  "Sharma"  "HrAdmin"
register "manager@urp.local"  "Password123!" "Rohan"  "Mehta"   "Manager"
register "finance@urp.local"  "Password123!" "Carol"  "Lee"     "Finance"
register "employee@urp.local" "Password123!" "Alice"  "Smith"   "Employee"
echo ""

# ── 3. Seed a benefit plan (as HR Admin) ──────────────────────────────────
echo -e "${BOLD}Creating sample benefit plan${NC}"
HR_TOKEN=$(token "hr@urp.local" "Password123!")
if [[ -z "$HR_TOKEN" ]]; then
  fail "Could not get HR token — skipping benefit plan seed"
else
  STATUS=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$GATEWAY/api/benefit-plans" \
    -H "Authorization: Bearer $HR_TOKEN" \
    -H "Content-Type: application/json" \
    -d '{"name":"Medical Cover","description":"Annual medical reimbursement","coverageAmount":50000,"monthlyCost":500,"currencyCode":"INR"}')
  if [[ "$STATUS" == "201" || "$STATUS" == "200" ]]; then
    ok "Created benefit plan: Medical Cover (INR 50,000)"
  else
    ok "Benefit plan may already exist — skipped (HTTP $STATUS)"
  fi
fi
echo ""

# ── Done ──────────────────────────────────────────────────────────────────
echo -e "${BOLD}Seed complete.${NC}"
echo ""
echo "Log in at http://localhost:3000 with any of these accounts:"
echo "  Email                Password"
echo "  employee@urp.local   Password123!"
echo "  manager@urp.local    Password123!"
echo "  hr@urp.local         Password123!"
echo "  finance@urp.local    Password123!"
echo ""
