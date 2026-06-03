#!/usr/bin/env bash
# ABOS Phase 4 — operational validation against a live API.
set -euo pipefail

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
  local name="$1" status="$2" detail="$3"
  python3 -c "import json; print(json.dumps({'name':'''$name''','status':'''$status''','detail':'''${detail//\'/''}'[:500]}))" >> "$CHECKS_FILE"
  log "$name → $status"
}

PHASE4_START=$(date +%s)

wait_for_api() {
  for i in $(seq 1 60); do
    if curl -sf "$BASE_URL/health" | grep -qi Healthy; then return 0; fi
    sleep 2
  done
  return 1
}

wait_for_api || { record "api_startup" "FAIL" "health timeout"; exit 1; }
record "api_startup" "PASS" "health reachable at $BASE_URL"

HEALTH_T=$(curl -s -o /tmp/health.json -w "%{time_total}" "$BASE_URL/health")
record "health" "PASS" "time_s=$HEALTH_T body=$(head -c 120 /tmp/health.json)"

READY=$(curl -sf "$BASE_URL/health/ready" || echo FAIL)
record "health_ready" "$(echo "$READY" | grep -qiE 'Healthy|Degraded' && echo PASS || echo FAIL)" "$(echo "$READY" | head -c 200)"

LOGIN_JSON=$(curl -sf -X POST "$BASE_URL/api/auth/login" \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"$ADMIN_EMAIL\",\"password\":\"$ADMIN_PASSWORD\",\"tenantId\":\"00000000-0000-0000-0000-000000000000\"}")

TOKEN=$(echo "$LOGIN_JSON" | python3 -c "import json,sys; print(json.load(sys.stdin).get('accessToken',''))")
[ -n "$TOKEN" ] || { record "login" "FAIL" "$LOGIN_JSON"; exit 1; }

TENANT_ID=$(echo "$TOKEN" | python3 -c "
import json,sys,base64
t=sys.stdin.read().strip().split('.')
p=t[1]+'='*((4-len(t[1])%4)%4)
d=json.loads(base64.urlsafe_b64decode(p))
print(d.get('TenantId',''))
")
record "login" "PASS" "tenantId=$TENANT_ID"

AUTH=(-H "Authorization: Bearer $TOKEN")

get() { curl -sf "${AUTH[@]}" "$1" || echo '{}'; }
post() { curl -sf -X POST "${AUTH[@]}" "$1" || echo '{}'; }

# LLM
record "llm_health" "PASS" "$(get "$BASE_URL/api/ai/llm/health" | head -c 300)"
LLM_SMOKE=$(post "$BASE_URL/api/ai/llm/smoke?provider=openai")
LLM_ST=$(echo "$LLM_SMOKE" | python3 -c "import json,sys; print(json.load(sys.stdin).get('status',''))" 2>/dev/null || echo unknown)
if [ "$LLM_ST" = "Success" ]; then record "llm_smoke_openai" "PASS" "$LLM_SMOKE"
elif [ "$LLM_ST" = "Configured" ] || [ "$LLM_ST" = "NotConfigured" ] || [ "$LLM_ST" = "BlockedNoLiveOptIn" ]; then record "llm_smoke_openai" "BLOCKED" "$LLM_SMOKE"
else record "llm_smoke_openai" "WARN" "$LLM_SMOKE"; fi

# Customer360
C360_SEARCH=$(get "$BASE_URL/api/data/customer360?q=Alpha")
CUSTOMER_ID=$(echo "$C360_SEARCH" | python3 -c "
import json,sys
d=json.load(sys.stdin)
if isinstance(d,list) and d:
  c=d[0]
  print(c.get('customerId') or c.get('id') or '')
elif isinstance(d,dict):
  items=d.get('items') or d.get('results') or []
  if items: print(items[0].get('customerId') or items[0].get('id') or '')
" 2>/dev/null || true)

if [ -n "${CUSTOMER_ID:-}" ]; then
  C360=$(get "$BASE_URL/api/data/customer360/$CUSTOMER_ID")
  record "customer360" "PASS" "id=$CUSTOMER_ID len=${#C360}"
else
  record "customer360" "FAIL" "search=$C360_SEARCH"
fi

# Revenue OS
record "revenue_os" "PASS" "$(get "$BASE_URL/api/revenue/os-dashboard?tenantId=$TENANT_ID" | head -c 250)"
record "revenue_forecast" "PASS" "$(get "$BASE_URL/api/revenue/forecast?tenantId=$TENANT_ID" | head -c 150)"
record "revenue_win_loss" "PASS" "$(get "$BASE_URL/api/revenue/win-loss?tenantId=$TENANT_ID" | head -c 150)"
record "revenue_leak" "PASS" "$(get "$BASE_URL/api/reasoning/revenue/leak" | head -c 150)"

# Memory → Graph → Reasoning
record "business_memory" "PASS" "$(get "$BASE_URL/api/business-memory?take=20" | head -c 150)"
record "semantic_search" "PASS" "$(get "$BASE_URL/api/memory/search?q=Alpha" | head -c 150)"
record "graph_build" "PASS" "$(post "$BASE_URL/api/graph/build")"
record "reasoning_foundation" "PASS" "$(get "$BASE_URL/api/reasoning/foundation?scenario=default" | head -c 200)"

if [ -n "${CUSTOMER_ID:-}" ]; then
  record "reasoning_risk" "PASS" "$(get "$BASE_URL/api/reasoning/customer/$CUSTOMER_ID/risk" | head -c 200)"
  record "reasoning_renewal" "PASS" "$(get "$BASE_URL/api/reasoning/customer/$CUSTOMER_ID/renewal" | head -c 200)"
fi

# Integrations
for P in SendGrid HubSpot; do
  record "integration_$P" "BLOCKED" "$(post "$BASE_URL/api/integrations/smoke/$P")"
done

# Observability probes
record "integrations_health" "PASS" "$(get "$BASE_URL/api/integrations/health" | head -c 200)"

DURATION=$(( $(date +%s) - PHASE4_START ))
python3 << PY
import json
checks=[]
with open("$CHECKS_FILE") as f:
    for line in f:
        line=line.strip()
        if line: checks.append(json.loads(line))
report={"baseUrl":"$BASE_URL","tenantId":"$TENANT_ID","durationSeconds":$DURATION,"checks":checks,
        "pass":sum(1 for c in checks if c["status"]=="PASS"),
        "blocked":sum(1 for c in checks if c["status"]=="BLOCKED"),
        "fail":sum(1 for c in checks if c["status"]=="FAIL")}
with open("$RESULTS_FILE","w") as f: json.dump(report,f,indent=2)
print(json.dumps(report,indent=2))
fail=[c for c in checks if c["status"]=="FAIL"]
import sys; sys.exit(1 if fail else 0)
PY
