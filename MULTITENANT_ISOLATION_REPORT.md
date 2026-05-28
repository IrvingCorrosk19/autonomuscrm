# MULTITENANT_ISOLATION_REPORT

**Fecha:** 2026-05-27  
**Entorno:** http://localhost:5154

---

## Tenants de prueba

| Tenant | ID | Admin | Datos exclusivos |
|--------|-----|-------|------------------|
| QA-A (demo) | `d7a30c86-7bb7-4303-9c1b-a0518fd78c67` | admin@autonomuscrm.local | Dataset demo original |
| QA-B | `a8f41d97-8cc8-5414-0a2c-b1629fe89d78` | admin-b@qa.autonomusflow.local / Admin123! | Lead "EXCLUSIVO QA-B", Customer "Cliente EXCLUSIVO QA-B" |

Seed: `QaTenantSeeder.EnsureQaTenantBAsync` en cada arranque con `Seed:Enabled`.

---

## Controles de aislamiento

| Capa | Mecanismo | Estado |
|------|-----------|--------|
| JWT claim `TenantId` | Login API/UI | OK |
| API query `tenantId` | `ApiTenantValidationMiddleware` → 403 | OK |
| API body `tenantId` | Mismo middleware POST/PUT | OK |
| Handlers (Customer, Lead by id) | `entity.TenantId != request.TenantId` → 404 | OK |
| SameTenantHandler | Query/route vs claim | OK |
| Repositories | Filtro explícito `Where TenantId` | OK (sin global filter EF aún) |
| UI commercial write | `CommercialWriteAuthorizationMiddleware` | OK |

---

## Pruebas ejecutadas (automáticas)

| Caso | Resultado |
|------|-----------|
| TEN-B-LOGIN | PASS |
| TEN-B-DATA (lead exclusivo visible solo en B) | PASS |
| TEN-CROSS-QUERY (token A + tenantId B) | PASS — HTTP 403 |
| TEN-IDOR-LEAD (id lead B + token A + tenant A) | PASS — HTTP 404 |
| TEN-003 / TEN-004 (Fase 2 regresión) | PASS |

Evidencia: `tests/qa-evidence/2026-05-27/phase3/phase3-*.csv`

---

## Riesgos residuales

| ID | Descripción | Severidad |
|----|-------------|-----------|
| MT-R1 | Sin global query filter EF — depende de disciplina en handlers | Media |
| MT-R2 | Export audit JSON no re-validado en script automatizado | Baja |
| MT-R3 | Background jobs Worker sin prueba multi-tenant en esta sesión | Media |

---

## Conclusión

**Aislamiento API/UI entre QA-A y QA-B: VALIDADO** en alcance ejecutado. Recomendado global query filter antes de SaaS público.
