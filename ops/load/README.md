# k6 Load Test Scripts — AutonomusCRM

Minimal smoke/load scripts for staging validation. **Do not run against production without approval.**

## Prerequisites

- [k6](https://k6.io/docs/get-started/installation/) installed
- Running AutonomusCRM API (staging or local with Postgres)

## Environment variables

| Variable | Default | Description |
|----------|---------|-------------|
| `BASE_URL` | `http://localhost:5000` | API base URL |
| `TENANT_ID` | *(required for auth)* | Tenant GUID |
| `ADMIN_EMAIL` | `admin@autonomuscrm.local` | Login email |
| `ADMIN_PASSWORD` | `Admin123!` | Login password |
| `AUTH_TOKEN` | *(optional)* | Pre-issued JWT; skips login if set |

## Run all smoke checks

```bash
export BASE_URL=https://staging.example.com
export TENANT_ID=your-tenant-guid

k6 run ops/load/health.js
k6 run ops/load/login.js
k6 run ops/load/revenue.js
k6 run ops/load/customer360.js
k6 run ops/load/trust.js
k6 run ops/load/memory.js
```

## Run with auth token (skip login)

```bash
export BASE_URL=https://staging.example.com
export AUTH_TOKEN=eyJ...
k6 run ops/load/revenue.js
```

## Notes

- Scripts use low VUs (1–5) suitable for smoke, not capacity testing.
- `/api/ai/llm/smoke` is **not** included — requires `INTEGRATION_SMOKE_LIVE=1` on server and API keys.
- Integration tests cover DB paths; k6 covers HTTP latency and availability.
