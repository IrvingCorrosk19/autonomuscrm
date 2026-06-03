#!/usr/bin/env bash
# k6 baseline runner — 10 / 50 / 100 VUs (stops early if error rate > 5%)
set -euo pipefail

BASE_URL="${BASE_URL:-http://localhost:8080}"
TENANT_ID="${TENANT_ID:?TENANT_ID required}"

run_tier() {
  local vus=$1
  local script=$2
  echo "=== k6 $script @ ${vus} VUs ==="
  k6 run --vus "$vus" --duration 30s \
    -e BASE_URL="$BASE_URL" \
    -e TENANT_ID="$TENANT_ID" \
    -e ADMIN_EMAIL="${ADMIN_EMAIL:-admin@autonomuscrm.local}" \
    -e ADMIN_PASSWORD="${ADMIN_PASSWORD:-Admin123!}" \
    "$script" || return 1
}

SCRIPTS=(
  ops/load/health.js
  ops/load/login.js
  ops/load/revenue.js
)

for vus in 10 50 100; do
  for script in "${SCRIPTS[@]}"; do
    if ! run_tier "$vus" "$script"; then
      echo "Baseline stopped at ${vus} VUs — script $script exceeded thresholds"
      exit 1
    fi
  done
done

echo "Baseline complete: 10/50/100 VUs passed for health, login, revenue"
