# LANGUAGE COVERAGE MATRIX — AutonomusCRM

**Date:** 2026-05-28  
**Cultures audited:** `en`, `es`  
**Cultures requested but missing:** `es-PA`

**Legend:** ✅ ≥95% · ⚠️ 70–94% · ❌ <70% · N/A not applicable · 🔧 infra only

---

## Global scores

| Dimension | English | Spanish | Notes |
|-----------|---------|---------|-------|
| Resource key parity | 100% | 100% | 1,069 / 1,069 keys |
| Resource value quality | 100% | **84.9%** | 161 es keys identical to en |
| Razor UI (shell/nav/forms) | ⚠️ 88% | ⚠️ 85% | Major CRUD pages good |
| Razor UI (analytics modules) | ⚠️ 75% | ❌ 62% | Revenue, C360, Audit mixed |
| PageModels / TempData | ⚠️ 80% | ⚠️ 72% | Import flows fail |
| JavaScript client strings | ⚠️ 85% | ⚠️ 80% | Spanish fallbacks |
| REST API error payloads | ❌ 5% | ❌ 40% | ex.Message mixed; controllers EN |
| Application exceptions | ❌ 30% | ❌ 55% | Mixed throw languages |
| Email notifications | ❌ 0% | ✅ 100% | Spanish only |
| WhatsApp templates | ❌ 0% | ✅ 100% | Spanish only |
| DataAnnotations validation | ⚠️ 90% | ⚠️ 85% | 4 keys; encoding bug in es |
| CSV exports | ✅ 100% | ❌ 0% | English headers |
| PDF exports | N/A | N/A | Not implemented |
| **Weighted overall** | **~72%** | **~68%** | Not certifiable |

---

## Module matrix

| Module | UI en | UI es | API en | API es | Email | Export | Roles tested | Verdict |
|--------|-------|-------|--------|--------|-------|--------|--------------|---------|
| **Authentication** | ✅ | ✅ | ❌ | ⚠️ | N/A | N/A | All | Partial |
| **Login / MFA** | ✅ | ✅ | ❌ | ⚠️ | N/A | N/A | All | Partial |
| **Access Denied** | ✅ | ✅ | — | — | — | — | All | OK |
| **Landing / Marketing** | ✅ | ✅ | — | — | — | — | Anonymous | OK |
| **Pricing / Demo** | ✅ | ✅ | — | — | — | — | Anonymous | OK |
| **Dashboard (Index)** | ⚠️ | ⚠️ | — | — | — | — | Admin, Manager | Mixed demo |
| **Executive OS** | ⚠️ | ⚠️ | ❌ | ❌ | — | — | Admin, Manager | Partial |
| **Revenue OS** | ❌ | ❌ | ❌ | ❌ | — | — | Sales, Manager | **Fail** |
| **Trust Studio** | ✅ | ✅ | ❌ | ❌ | — | — | Admin, Manager | UI OK |
| **Leads** | ✅ | ✅ | ❌ | ⚠️ | — | — | Sales, Support, Viewer | Partial |
| **Customers** | ✅ | ⚠️ | ❌ | ⚠️ | — | CSV ❌ | Sales | Partial |
| **Deals / Pipeline** | ⚠️ | ⚠️ | ❌ | ⚠️ | — | — | Sales, Manager | Mixed |
| **Customer 360** | ❌ | ❌ | ❌ | ❌ | — | — | Support, Sales | **Fail** |
| **Customer Success** | ✅ | ⚠️ | ❌ | ❌ | ❌ | — | Support | Partial |
| **Tasks** | ⚠️ | ⚠️ | ❌ | ❌ | — | — | Manager, Support | Partial |
| **Workflows** | ✅ | ✅ | ❌ | ❌ | — | — | Admin | Partial |
| **Policies** | ✅ | ✅ | ❌ | ❌ | — | — | Admin | Partial |
| **Memory** | ✅ | ✅ | ❌ | ❌ | — | — | Admin | Partial |
| **Agents / Workforce** | ✅ | ✅ | ❌ | ❌ | — | — | Admin | Partial |
| **Command / Decisions** | ⚠️ | ⚠️ | ❌ | ❌ | — | — | Admin | Mixed |
| **Command / Outcomes** | ✅ | ✅ | ❌ | ❌ | — | — | Admin | Partial |
| **Command / Playbooks** | ✅ | ✅ | ❌ | ❌ | — | — | Admin | Partial |
| **Audit** | ⚠️ | ⚠️ | ❌ | ❌ | — | ❌ | Admin | Mixed |
| **Users / Roles** | ⚠️ | ⚠️ | ❌ | ❌ | — | Import ❌ | Admin, Manager | Partial |
| **Settings** | ✅ | ⚠️ | — | — | — | — | Admin, Manager | Partial |
| **Billing** | ✅ | ✅ | ❌ | ❌ | ⚠️ Stripe | — | Admin | Partial |
| **Integrations** | ⚠️ | ⚠️ | ❌ | ❌ | — | — | Admin | Partial |
| **Voice** | ✅ | ✅ | ❌ | ❌ | — | — | Admin | Partial |
| **Import (all)** | ❌ | ⚠️ | ❌ | ❌ | — | — | Admin | **Fail** |
| **Failed Events** | ✅ | ✅ | ❌ | ❌ | — | — | Admin | Partial |
| **AI / Intelligence API** | 🔧 | 🔧 | ❌ | ❌ | — | — | Admin | API only |
| **Webhooks** | — | — | ❌ | ❌ | — | — | Ops | EN only |
| **Provisioning** | — | — | ❌ | ❌ | — | — | Ops | EN only |

---

## Role-based flow coverage (FASE 5)

| Flow | en complete? | es complete? | Breaks on culture switch? |
|------|--------------|--------------|----------------------------|
| Login → Executive (Admin) | ⚠️ | ⚠️ | API errors stay Spanish |
| Login → Revenue (Sales) | ❌ | ❌ | English KPIs in both modes |
| Lead CRUD (Sales) | ✅ | ✅ | Import errors Spanish only |
| Deal pipeline (Manager) | ⚠️ | ⚠️ | Forecast block English |
| Customer 360 (Support) | ❌ | ❌ | Mixed EN/ES labels |
| Trust HITL (Admin) | ✅ | ✅ | — |
| Settings (Admin) | ✅ | ⚠️ | Error messages Spanish |
| Billing (Admin) | ✅ | ✅ | — |
| User create + role (Admin) | ⚠️ | ⚠️ | Role names English in table |
| Import CSV (Admin) | ❌ | ⚠️ | Errors Spanish; API EN |
| Audit review (Admin) | ⚠️ | ⚠️ | Mixed labels |
| Viewer blocked paths | ✅ | ✅ | — |
| SuperAdmin | N/A | N/A | Role not implemented |

---

## Resource file coverage

| File | Keys | en complete | es complete | es-PA |
|------|------|-------------|-------------|-------|
| `localization-en.json` | 1,069 | ✅ | — | ❌ |
| `localization-es.json` | 1,069 | — | ⚠️ 84.9% | ❌ |
| `SharedResource.resx` (neutral) | 1,069 | Mirrors es | — | ❌ |
| `ValidationMessages.en.resx` | 4 | ✅ | — | ❌ |
| `ValidationMessages.es.resx` | 4 | — | ⚠️ encoding | ❌ |
| `FlowClientStringsProvider` | ~25 | ✅ | ✅ | ❌ |

---

## Certification threshold vs actual

| Criterion | Target | Actual | Pass? |
|-----------|--------|--------|-------|
| English UI coverage | 100% | ~72% | ❌ |
| Spanish UI coverage | 100% | ~68% | ❌ |
| Zero hardcoded UI strings | 0 | 120+ | ❌ |
| Zero mixed screens | 0 | 8+ modules | ❌ |
| Missing resource keys | 0 | 0 | ✅ |
| Broken translations | 0 | 1+ (encoding) | ❌ |
| English in Spanish mode | 0 | 161+ keys + Revenue UI | ❌ |
| Spanish in English mode | 0 | Dashboard demo + Import API mix | ❌ |
| es-PA support | Required by program | Not implemented | ❌ |

---

## Recommended completion order

1. **Revenue + Customer360 + Audit** → largest mixed-language surfaces  
2. **161 es.json translations** → quick win for nav/product names  
3. **Import subsystem** → 6 PageModels + ImportController + ImportGuard  
4. **API layer** → shared `LocalizedProblemDetails` helper  
5. **Email templates** → culture-aware template resolver  
6. **es-PA** → register culture + optional regional overrides  
7. **Automated tests** → `LocalizationCoverageTests` in CI  

---

*Matrix derived from `MULTILANGUAGE_AUDIT_REPORT.md` and `scripts/audit-localization.ps1`.*
