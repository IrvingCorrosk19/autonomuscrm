# UNLOCALIZED CONTENT REPORT — AutonomusCRM

**Date:** 2026-05-28  
**Scope:** All user-visible surfaces (UI, API, email, export, validation)

---

## Summary

| Category | Items found | Severity |
|----------|-------------|----------|
| Razor hardcoded literals | 120+ instances across 25+ files | High |
| PageModel Spanish-only messages | 18+ strings (Import flows) | High |
| API controller hardcoded errors | 25+ strings | High |
| Application exception messages | 30+ strings (mixed ES/EN) | High |
| Email / WhatsApp templates | 11 templates (Spanish only) | Critical |
| JS hardcoded / Spanish fallbacks | 15+ strings | Medium |
| es.json keys with English values | 161 keys | High |
| ValidationMessages encoding | 1 file | Medium |
| CSV export headers | 6 columns (English only) | Medium |
| PDF | Not applicable | — |

---

## 1. UI — Buttons, labels, dropdowns

### Not localized (hardcoded in markup)

| Element | Location | Current text | Expected |
|---------|----------|--------------|----------|
| Filter option | `Command/Decisions.cshtml:14` | `Pending` | `@L["Status_Pending"]` |
| Filter option | `Command/Decisions.cshtml:15` | `Executed` | Resource key |
| Filter option | `Command/Decisions.cshtml:16` | `Failed` | Resource key |
| Filter option | `Tasks.cshtml:34` | `Completed` | Resource key |
| Role label | `Users.cshtml:204-219` | `Admin`, `Manager`, `Sales`, `Viewer` | `@L["Role_*"]` |
| KPI label | `Revenue.cshtml:161` | `Win rate` | Resource key |
| KPI label | `Revenue.cshtml:162` | `Deal size` | Resource key |
| Section title | `Revenue.cshtml:142` | `Win / Loss Center` | Resource key |
| Kanban hint | `Deals.cshtml:81` | Spanish drag hint | Resource key |
| Card title | `Customer360/Detail.cshtml:22` | `Customer Health` | Resource key |
| Card title | `Customer360/Detail.cshtml:97` | `Customer Success` | Resource key |
| Link text | `Customer360/Detail.cshtml:257` | `Volver al directorio` | Resource key |
| Metric | `Audit.cshtml:41` | `Eventos registrados` | Resource key |
| CTA | `Audit.cshtml:213` | `Ver documentación` | Resource key |
| Outcome chain | `_FlowOutcomeChain.cshtml:7-28` | 6 English labels | Resource keys |
| Agent card | `_FlowAgentCard.cshtml:19` | `Outcomes` | Resource key |

### Partially localized (uses L[] but mixed)

| Page | Issue |
|------|-------|
| `Revenue.cshtml` | Shell localized; tables/metrics English |
| `Deals.cshtml` | Pipeline localized; forecast block English |
| `Dashboard.cshtml` | Framework localized; demo table rows Spanish |
| `Customer360/Detail.cshtml` | 5 L[] calls vs 20+ literals |
| `Executive.cshtml` | Mostly L[]; some metric suffixes hardcoded |

---

## 2. Placeholders and tooltips

| File | Line | Text | Type |
|------|------|------|------|
| `Integrations.cshtml` | 51 | `Access token` | placeholder |
| `Workflows/Edit.cshtml` | 178 | `Ej: Lead.Created` | placeholder (ES) |
| `Deals.cshtml` | 49 | `aria-label="Forecast pipeline"` | a11y (EN) |
| `Deals.cshtml` | 136 | `aria-label="Deals"` | a11y (EN) |
| `_FlowRelationshipGraph.cshtml` | — | Spanish aria labels | a11y (ES) |

---

## 3. Validations

| Source | Localized? | Notes |
|--------|------------|-------|
| DataAnnotations via `ValidationMessages.resx` | Partial | 4 keys only |
| `ValidationMessages.es.resx` | Broken encoding | `direcciÃ³n` |
| Import PageModels | **No** | All Spanish hardcoded |
| Domain entities (`Deal.cs`, `Customer.cs`) | **No** | Spanish throws |
| Domain entities (`CustomerContract.cs`) | **No** | English throws |
| FluentValidation | N/A | Not used |

### Import validation strings (all unlocalized)

```
"Por favor selecciona un archivo"
"Formato de archivo no soportado. Use JSON o CSV"
"El archivo no contiene clientes válidos"
"Error al importar leads: ..."
"Error al importar clientes: ..."
"Error al importar usuarios: ..."
```

Files: `Leads/Import.cshtml.cs`, `Customers/Import.cshtml.cs`, `Users/Import.cshtml.cs`, `Deals/Import.cshtml.cs`, `Policies/Import.cshtml.cs`, `Workflows/Import.cshtml.cs`

---

## 4. Toast, modals, SweetAlert

| Component | Status |
|-----------|--------|
| `_CrmToastContainer.cshtml` | Container only — messages from JS/localizer |
| `FlowClientStringsProvider` toast keys | Localized (6 keys) |
| `site.js` operation bar | Hardcoded `'Operación'` fallback |
| SweetAlert | **Not found** in codebase |
| Modals | Use `_FlowDrawer.cshtml` — localized titles via parent pages |

---

## 5. API error messages (user/API client visible)

| Controller | Sample unlocalized messages |
|------------|----------------------------|
| `ProvisioningController` | `Invalid platform key`, `Name, AdminEmail, AdminPassword required` |
| `ImportController` | `Provide JSON body or CSV/JSON file (max 5MB, 5000 rows).` |
| `DealsController` | `ID mismatch` |
| `CustomersController` | `ID mismatch` |
| `WebhooksController` | `customerId required`, `records required` |
| `DataPlatformController` | `Invalid or missing X-Data-Ingest-Key.` |
| `EnterpriseAuthController` | SAML messages in Spanish |
| `AuthController` | Passes `ex.Message` — language follows exception source |
| `HealthController` | `Metrics service not available` |

**Impact:** API consumers and SPA clients receive English or Spanish regardless of selected UI culture.

---

## 6. Logs visible to user

| Source | Example | Localized? |
|--------|---------|------------|
| `TempData["ErrorMessage"]` | Mixed — some `_localizer`, some literals | Partial |
| `Settings.cshtml.cs:71` | `Error al actualizar la configuración:` + ex | No |
| `FailedEvents.cshtml` | Uses L[] for chrome; event payloads raw | Partial |
| Flash via `_FlashMessages.cshtml` | Renders TempData as-is | Depends on writer |

---

## 7. Emails

**File:** `Infrastructure/CustomerSuccess/CommunicationTemplates.cs`

| Template key | Subject (ES only) | English variant |
|--------------|-------------------|-----------------|
| welcome | Bienvenido a AutonomusFlow | **Missing** |
| onboarding | Tu onboarding ha comenzado | **Missing** |
| followup | Seguimiento de tu cuenta | **Missing** |
| renewal | Próxima renovación | **Missing** |
| risk | Importante: tu cuenta necesita atención | **Missing** |
| reengagement | Te extrañamos | **Missing** |

WhatsApp: 5 templates — all Spanish only.

`EmailAutomationEngine.cs` fallback: `Notificación AutonomusFlow` / `Mensaje del sistema.`

**Billing emails:** Stripe-driven — not audited for locale (external).

**Auth emails:** MFA/reset — verify if implemented (no template files found in Infrastructure).

---

## 8. PDF / Excel / CSV / HTML exports

| Format | Found | Localized? |
|--------|-------|------------|
| PDF | No generator | N/A |
| Excel | No dedicated export | N/A |
| CSV | `WarehouseExportService.cs` | **No** — English headers |
| HTML reports | In-app Razor only | Follows page localization (partial) |
| Dashboard export | Not found | N/A |
| Audit export | Not found | N/A |

---

## 9. Dashboard cards and widgets

| Module | Unlocalized content |
|--------|---------------------|
| Revenue OS | Win/Loss table, KPI row, Decision/Learning columns |
| Executive OS | Mostly OK; verify empty-state sublabels |
| Dashboard | Demo recommendation rows in Spanish |
| Trust Studio | L[] coverage good |
| Memory | L[] coverage good |
| Billing | L[] coverage good |

---

## 10. AI prompts and automations

| Area | Status |
|------|--------|
| AI agent system prompts | Infrastructure/Autonomous — English technical prompts (not user UI) |
| Workflow step labels in UI | Localized via Workflows pages |
| HITL Trust explanations | Partial via `_FlowExplainability.cshtml` |
| Voice module UI | Localized |

---

## 11. Seed data (user-visible)

| Source | Issue |
|--------|-------|
| VPS test SQL | Spanish company names — data, not UI |
| `DatabaseSeeder` | Demo content when Seed enabled — mixed |
| Dashboard demo rows | Hardcoded Spanish in view |

---

## 12. Priority remediation queue

### P0 — Blocks bilingual certification

1. Localize `Revenue.cshtml`, `Customer360/Detail.cshtml`, `Audit.cshtml` literals
2. Translate 161 identical es.json values
3. Bilingual email templates
4. Import PageModels → `_localizer`
5. API controller error localization

### P1 — High user impact

6. Application exception → error code mapping
7. JS remove Spanish-only fallbacks
8. Fix `ValidationMessages.es.resx` encoding
9. CSV export headers

### P2 — Completeness

10. Add `es-PA` culture
11. Localize `Flow/Components.cshtml` (dev catalog)
12. Role labels in Users table
13. Automated localization tests in CI

---

## 13. Files with zero `L[` usage (22)

These inherit `L` but do not use it — review for hidden literals:

- `Shared/Flow/_FlowOutcomeChain.cshtml` ⚠️ hardcoded
- `Shared/Flow/_FlowAgentCard.cshtml` ⚠️ hardcoded
- `Shared/Flow/_FlowRelationshipGraph.cshtml` ⚠️ hardcoded
- `Flow/Components.cshtml` ⚠️ design catalog
- `Shared/_FlashMessages.cshtml` — passes through TempData
- `Shared/_CrmLoadingSkeleton.cshtml` — structural only
- Others: stubs, redirects, script partials

---

*Generated from static analysis. Re-run audit: `.\scripts\audit-localization.ps1`*
