# PILOT VALIDATION GUIDE

**Objetivo:** Validar que el cliente piloto puede ejecutar el flujo completo sin desarrollador, SQL ni código.  
**Evidencia:** tests automatizados + checklist manual UI.

---

## Matriz de validación

| # | Escenario | Validación automatizada | Validación manual (cliente) | Criterio PASS |
|---|-----------|-------------------------|----------------------------|---------------|
| V1 | Tenant nuevo | `IntegrationTestTenantHelper`, seed vacío | Checklist C1–C14 tenant limpio | Flujo completo sin datos previos |
| V2 | Tenant existente | CRM seed + second import tests | Import incremental post-CRM | Datos nuevos sin pisar existentes acordados |
| V3 | Datos limpios | `DataHealthSyntheticDatasets.HealthyDataset()` | Health score ≥ 80 | Pocos hallazgos |
| V4 | Datos dañados | `BrokenIntegrityDataset`, `MixedDataset` | Health validity findings | Findings visibles, Operate continúa |
| V5 | Datos duplicados | `DuplicateDataset`, `OperationSyntheticDatasets.DuplicateCustomers()` | Merge studio + preview | Duplicados reducidos en preview |
| V6 | Datos huérfanos | `OrphanDataset` | Health orphan findings | Documentados; exclude opcional |

---

## Flujo obligatorio (9 pasos producto)

```
Connect → Discover → Understand → Health → Graph → Insights → Operate → Import → Rollback
```

| Paso | Test DIP principal | Página |
|------|-------------------|--------|
| Connect | `DbIntelligenceConnectionApiTests` | Connect |
| Discover | `DbIntelligenceDiscoveryPostgresTests` | Explore |
| Understand | `DbIntelligenceBusinessDiscoveryIntegrationTests` | Understand |
| Health | `DbIntelligenceDataHealthIntegrationTests` | Health |
| Graph | `DbIntelligenceGraphIntegrationTests` | Graph |
| Insights | `DbIntelligenceInsightIntegrationTests` | Insights |
| Operate | `DbOperationIntegrationTests`, `DbOperationPageTests` | Operate |
| Import | `DbOperationIntegrationTests` execute+import | Operate |
| Rollback | `DbOperationIntegrationTests` rollback | Operate |

**Suite:** `dotnet test --filter Category=DatabaseIntelligence` → **149 PASS / 0 FAIL / 0 SKIP** (2026-05-28).

Demo path: **182 PASS / 0 FAIL / 0 SKIP**. Full suite: **520 PASS / 0 FAIL / 0 SKIP**.

---

## Comandos pre-piloto (Autonomus)

```powershell
dotnet build
dotnet test --filter "Category=DatabaseIntelligence"
dotnet test --filter "Category=DatabaseIntelligence|Category=Demo|Category=DataHubE2E|Category=Phase4Validation|Category=DataHubRabbitMq|FullyQualifiedName~DataHubCertification"
dotnet test
```

| Suite | Objetivo piloto | Estado verificado |
|-------|-----------------|-------------------|
| DIP | Motor flujo completo | 149/149 PASS |
| Demo path | Estabilidad integración | 182/182 PASS |
| Full suite | Madurez plataforma | 520/520 PASS |

---

## Validación Operate (studios)

Post Sprint 1 OC-UX — sin `BuildDefaultPlan()`:

| Studio | Test unit/integration |
|--------|----------------------|
| Filter | `DbOperationUnitTests` filter amount |
| Clean | Phone/email normalize |
| Merge | Duplicate email keep newest |
| Exclude | Exclude test set |
| Transform | Split full name |
| Preview | `PreviewAsync` integration |
| Import | `ImportOnlyPlan` integration |
| Rollback | Rollback removes CRM entities |

Archivo datasets: `AutonomusCRM.Tests/DatabaseIntelligence/OperationSyntheticDatasets.cs`

---

## Validación tenant isolation

Obligatorio en piloto multi-tenant SaaS:

- `TenantIsolationIntegrationTests`
- `DbOperationIntegrationTests.TenantIsolation_OtherTenantCannotReadJob`
- DIP `TenantIsolation_*` en connection, health, graph, sync, operations

**Criterio:** 0 fugas cross-tenant en tests antes de GO.

---

## Validación SignalR

| Hub | Tests |
|-----|-------|
| `/hubs/db-intelligence` | `DbIntelligenceProgressHubTests`, `DbOperationPageTests` |
| Operate progress | UI `db-intelligence-operate.js` |

Cliente debe ver progreso sin refrescar manualmente (excepto reconexión red).

---

## Sesión de validación UI (half-day, Autonomus + cliente)

1. Cliente ejecuta C1–C14 del checklist con observador Autonomus **sin tomar el teclado**.
2. Anotar desviaciones UX (tiempo > 15 min por paso = fricción).
3. Forzar rollback al final — verificar CRM.
4. Completar Go/No-Go en `PILOT_CHECKLIST.md`.

---

## Criterios GO piloto

| # | Criterio |
|---|----------|
| G1 | Cliente completa C1–C14 sin SQL |
| G2 | Rollback verificado |
| G3 | DIP 149/149 PASS en entorno piloto |
| G4 | PostgreSQL único motor en contrato |
| G5 | Agents desactivados |

---

## Criterios NO-GO

- Conexión BD no resoluble en 2 sesiones
- Rollback falla en entorno piloto
- Cliente requiere SQL para completar flujo
- Scope creep (Oracle day-1, agents, etc.)
