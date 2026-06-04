#!/usr/bin/env bash
# ABOS Phase 4 — operational validation against a live API.
set -uo pipefail

BASE_URL="${BASE_URL:-http://127.0.0.1:8080}"
ADMIN_EMAIL="${ADMIN_EMAIL:-admin@autonomuscrm.local}"
ADMIN_PASSWORD="${ADMIN_PASSWORD:-Admin123!}"
RESULTS_DIR="${RESULTS_DIR:-./TestResults/phase4}"
CHECKS_FILE="${RESULTS_DIR}/checks.jsonl"
RESULTS_FILE="${RESULTS_DIR}/phase4-validation.json"
mkdir -p "$RESULTS_DIR"
: > "$CHECKS_FILE"

log() { echo "[phase4] $*"; }

record() {
  python3 - "$1" "$2" "$3" <<'PY' >> "$CHECKS_FILE"
import json, sys
name, status, detail = sys.argv[1], sys.argv[2], sys.argv[3]
print(json.dumps({"name": name, "status": status, "detail": detail[:800]}))
PY
  log "$1 → $2"
}

curl_json() {
  local method="$1" url="$2"
  shift 2
  curl -s -w "\n__HTTP__%{http_code}" -X "$method" "$url" "$@"
}

PHASE4_START=$(date +%s)

wait_for_api() {
  for _ in $(seq 1 60); do
    if curl -sf "$BASE_URL/health" | grep -qi Healthy; then return 0; fi
    sleep 2
  done
  return 1
}

wait_for_api || { record "api_startup" "FAIL" "health timeout"; exit 1; }
record "api_startup" "PASS" "health reachable at $BASE_URL"

HEALTH_RAW=$(curl_json GET "$BASE_URL/health")
HEALTH_CODE=$(echo "$HEALTH_RAW" | sed -n 's/^__HTTP__//p')
record "health" "$([ "$HEALTH_CODE" = "200" ] && echo PASS || echo FAIL)" "http=$HEALTH_CODE"

READY_RAW=$(curl_json GET "$BASE_URL/health/ready")
READY_CODE=$(echo "$READY_RAW" | sed -n 's/^__HTTP__//p')
record "health_ready" "$([ "$READY_CODE" = "200" ] && echo PASS || echo FAIL)" "http=$READY_CODE"

LOGIN_RAW=$(curl_json POST "$BASE_URL/api/auth/login" \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"$ADMIN_EMAIL\",\"password\":\"$ADMIN_PASSWORD\",\"tenantId\":\"00000000-0000-0000-0000-000000000000\"}")
LOGIN_CODE=$(echo "$LOGIN_RAW" | sed -n 's/^__HTTP__//p')
LOGIN_BODY=$(echo "$LOGIN_RAW" | sed '/^__HTTP__/d')

TOKEN=$(echo "$LOGIN_BODY" | python3 -c "import json,sys; print(json.load(sys.stdin).get('accessToken',''))" 2>/dev/null || echo "")
if [ -z "$TOKEN" ]; then
  record "login" "FAIL" "http=$LOGIN_CODE body=$LOGIN_BODY"
  exit 1
fi

TENANT_ID=$(echo "$TOKEN" | python3 -c "
import json,sys,base64
t=sys.stdin.read().strip().split('.')
p=t[1]+'='*((4-len(t[1])%4)%4)
d=json.loads(base64.urlsafe_b64decode(p))
print(d.get('TenantId',''))
")
record "login" "PASS" "tenantId=$TENANT_ID http=$LOGIN_CODE"

AUTH=(-H "Authorization: Bearer $TOKEN")

api_get() {
  local path="$1" name="$2"
  local raw code body
  raw=$(curl_json GET "$BASE_URL$path" "${AUTH[@]}")
  code=$(echo "$raw" | sed -n 's/^__HTTP__//p')
  body=$(echo "$raw" | sed '/^__HTTP__/d' | head -c 400)
  record "$name" "$([ "$code" = "200" ] && echo PASS || echo FAIL)" "http=$code $body"
  echo "$raw" | sed '/^__HTTP__/d'
}

api_post() {
  local path="$1" name="$2"
  local raw code body
  raw=$(curl_json POST "$BASE_URL$path" "${AUTH[@]}")
  code=$(echo "$raw" | sed -n 's/^__HTTP__//p')
  body=$(echo "$raw" | sed '/^__HTTP__/d' | head -c 400)
  record "$name" "$([ "$code" = "200" ] && echo PASS || echo FAIL)" "http=$code $body"
  echo "$raw" | sed '/^__HTTP__/d'
}

# LLM
api_get "/api/ai/llm/health" "llm_health" >/dev/null
LLM_BODY=$(api_post "/api/ai/llm/smoke?provider=openai" "llm_smoke_openai")
LLM_ST=$(echo "$LLM_BODY" | python3 -c "import json,sys; print(json.load(sys.stdin).get('status',''))" 2>/dev/null || echo "")
if [ "$LLM_ST" = "Success" ]; then
  : # already recorded
elif [ "$LLM_ST" = "Configured" ] || [ "$LLM_ST" = "NotConfigured" ] || [ "$LLM_ST" = "BlockedNoLiveOptIn" ]; then
  record "llm_smoke_openai" "BLOCKED" "$LLM_ST"
fi

# Customer360
C360_SEARCH=$(api_get "/api/data/customer360?q=Alpha" "customer360_search")
CUSTOMER_ID=$(echo "$C360_SEARCH" | python3 -c "
import json,sys
try:
  d=json.load(sys.stdin)
  if isinstance(d,list) and d: print(d[0].get('customerId',''))
except: pass
" 2>/dev/null || true)

if [ -z "${CUSTOMER_ID:-}" ]; then
  C360_ALL=$(api_get "/api/data/customer360" "customer360_list")
  CUSTOMER_ID=$(echo "$C360_ALL" | python3 -c "
import json,sys
try:
  d=json.load(sys.stdin)
  if isinstance(d,list) and d: print(d[0].get('customerId',''))
except: pass
" 2>/dev/null || true)
fi

if [ -n "${CUSTOMER_ID:-}" ]; then
  api_get "/api/data/customer360/$CUSTOMER_ID" "customer360_detail" >/dev/null
else
  record "customer360" "FAIL" "no customer id from search"
fi

# Revenue OS
api_get "/api/revenue/os-dashboard?tenantId=$TENANT_ID" "revenue_os" >/dev/null
api_get "/api/revenue/forecast?tenantId=$TENANT_ID" "revenue_forecast" >/dev/null
api_get "/api/revenue/win-loss?tenantId=$TENANT_ID" "revenue_win_loss" >/dev/null
api_get "/api/reasoning/revenue/leak" "revenue_leak" >/dev/null

# Memory → Graph → Reasoning
api_get "/api/business-memory?take=20" "business_memory" >/dev/null
api_get "/api/memory/search?q=Alpha" "semantic_search" >/dev/null
api_post "/api/graph/build" "graph_build" >/dev/null
api_get "/api/reasoning/foundation?scenario=default" "reasoning_foundation" >/dev/null

if [ -n "${CUSTOMER_ID:-}" ]; then
  api_get "/api/reasoning/customer/$CUSTOMER_ID/risk" "reasoning_risk" >/dev/null
  api_get "/api/reasoning/customer/$CUSTOMER_ID/renewal" "reasoning_renewal" >/dev/null
  api_get "/api/reasoning/customer/$CUSTOMER_ID/risk" "demo_scenario_at_risk" >/dev/null
  api_get "/api/reasoning/customer/$CUSTOMER_ID/renewal" "demo_scenario_renewal" >/dev/null
fi

api_get "/api/revenue/win-loss?tenantId=$TENANT_ID" "demo_scenario_deals" >/dev/null

# Integrations (expect BLOCKED without creds)
for P in SendGrid HubSpot; do
  SMOKE=$(api_post "/api/integrations/smoke/$P" "integration_$P")
  echo "$SMOKE" | grep -qi BLOCKED && record "integration_$P" "BLOCKED" "no credentials" || true
done
api_get "/api/integrations/health" "integrations_health" >/dev/null

DURATION=$(( $(date +%s) - PHASE4_START ))
python3 << PY
import json, sys
checks=[]
with open("$CHECKS_FILE") as f:
    for line in f:
        line=line.strip()
        if line: checks.append(json.loads(line))
report={
  "baseUrl":"$BASE_URL",
  "tenantId":"$TENANT_ID",
  "durationSeconds":$DURATION,
  "checks":checks,
  "pass":sum(1 for c in checks if c["status"]=="PASS"),
  "blocked":sum(1 for c in checks if c["status"]=="BLOCKED"),
  "fail":sum(1 for c in checks if c["status"]=="FAIL")
}
with open("$RESULTS_FILE","w") as f: json.dump(report,f,indent=2)
print(json.dumps(report,indent=2))
critical=[c for c in checks if c["status"]=="FAIL" and c["name"] in ("health","login","api_startup","revenue_os")]
sys.exit(1 if critical else 0)
PY
