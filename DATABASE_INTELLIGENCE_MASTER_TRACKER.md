# DATABASE INTELLIGENCE PLATFORM — MASTER TRACKER

**Producto:** Database Intelligence Platform (DIP)  
**Código interno:** `DatabaseIntelligence` (`AutonomusCRM.Application/DatabaseIntelligence/`)  
**Estado:** ✅ **S5 completado** — Sync Engine  
**Última actualización:** 2026-06-14  
**Score actual:** **85/100**

---

## Posicionamiento

| Es | No es |
|----|-------|
| Capa de inteligencia empresarial sobre bases de datos del cliente | Importador de archivos (→ Data Hub, mantenimiento only) |
| Descubrimiento automático de negocio desde esquemas reales | ETL genérico ni administrador de BD para DBAs |
| Salud, relaciones y grafo comprensible para usuarios de negocio | Herramienta para desarrolladores |

**Meta de producto:** Un cliente conecta su base de datos y AutonomusCRM entiende automáticamente cómo funciona su negocio — clientes, ventas, facturas, pagos — aunque los nombres de tablas sean distintos.

**Relación con Data Hub:**

| Data Hub | Database Intelligence |
|----------|----------------------|
| Archivos, CRMs, migraciones programadas | Conexión directa a BD del cliente |
| Pipeline import → staging → CRM | Lectura + inferencia + salud + grafo + sync opcional |
| ✅ Completado — solo mantenimiento correctivo | 🚀 Nueva misión principal |

---

## Principios de diseño

1. **Read-first:** conexiones de descubrimiento en modo lectura; escritura solo en sync explícito.
2. **Business-native UX:** lenguaje de negocio, no SQL ni DDL.
3. **Tenant isolation:** cada conexión, catálogo y grafo scoped por tenant — sin excepciones Admin.
4. **Reuse, don't rebuild:** tenant guard, cifrado, auditoría forense, RabbitMQ, SignalR, smart matching.
5. **Evidence over claims:** cada fase cierra con tests ejecutables + evidencia en tracker.

---

## Arquitectura objetivo

```
┌─────────────────────────────────────────────────────────────────┐
│  UX (Business Users)                                            │
│  Connect → Discover → Understand → Health → Graph → Sync        │
└────────────────────────────┬────────────────────────────────────┘
                             │
┌────────────────────────────▼────────────────────────────────────┐
│  AutonomusCRM.API                                               │
│  Pages/DatabaseIntelligence/*  ·  /api/db-intelligence/*        │
│  Hub: /hubs/db-intelligence  (SignalR progress)                 │
└────────────────────────────┬────────────────────────────────────┘
                             │
┌────────────────────────────▼────────────────────────────────────┐
│  AutonomusCRM.Application/DatabaseIntelligence                │
│  Contracts · DTOs · Engines · Sync policies                     │
└────────────────────────────┬────────────────────────────────────┘
                             │
┌────────────────────────────▼────────────────────────────────────┐
│  AutonomusCRM.Infrastructure/DatabaseIntelligence             │
│  Connectors · Discovery · BusinessInference · Health · Graph    │
│  Sync workers (RabbitMQ) · Encrypted connection vault           │
└────────────────────────────┬────────────────────────────────────┘
                             │
        ┌────────────────────┼────────────────────┐
        ▼                    ▼                    ▼
   PostgreSQL           SQL Server            MySQL/MariaDB/Oracle
   (cliente)            (cliente)             (cliente)
```

**Reutilización directa desde plataforma existente:**

| Capacidad | Origen | Uso en DIP |
|-----------|--------|------------|
| Tenant isolation | `DataHubTenantGuard` pattern | Conexiones, jobs, grafo |
| Encryption at-rest | `DataHubSecurityOptions` / AES-GCM | Connection strings, samples |
| Forensic audit | `DataHubForensicAudits` pattern | Connect, discover, sync, export graph |
| RabbitMQ workers | `DataHubImportWorker` pattern | Discovery jobs, sync jobs, health scans |
| SignalR progress | `DataHubProgressHub` pattern | Long discovery / full sync |
| Smart matching | `DataHubSmartMatchingEngine` | Column → business entity inference |
| Scheduled sync | `DataHubScheduledImportWorker` | Full / Delta / cron |
| Duplicate detection | Data Hub Duplicate Engine | Cross-table duplicate health |
| Quality scoring | Data Hub Quality Center | Data health baseline |
| Graph reasoning | `KnowledgeGraph` | AI insights sobre grafo |
| Business memory | `BusinessMemory` | Persistir inferencias confirmadas |

---

## Roadmap completo

### Resumen por fase

| Fase | Nombre | Duración est. | Score target | Estado |
|------|--------|---------------|--------------|--------|
| **S0** | Foundation & Connection Vault | 2 semanas | 10/100 | ✅ Done |
| **S1** | Database Discovery | 3 semanas | 25/100 | ✅ Done |
| **S2** | Business Discovery | 3 semanas | 45/100 | ✅ Done |
| **S3** | Data Health | 2 semanas | 60/100 | ✅ Done |
| **S4** | Database Graph | 3 semanas | 75/100 | ✅ Done |
| **S5** | Sync Engine | 3 semanas | 85/100 | ✅ Done |
| **S6** | AI Insights | 2 semanas | 92/100 | ⬜ Not started |
| **S7** | Enterprise Hardening & GA | 2 semanas | 100/100 | ⬜ Not started |

**Duración total estimada:** 20 semanas (~5 meses) con equipo paralelo API + Infra + UX.

---

## S0 — Foundation & Connection Vault

**Objetivo:** Infraestructura segura para registrar conexiones externas sin descubrir aún.

| ID | Entregable | Status | Notas |
|----|------------|--------|-------|
| S0-01 | Módulo `Application/DatabaseIntelligence/` — contratos base | ✅ Done | `DbIntelligenceContracts.cs` — 5 interfaces, enum `DbEngineType`, 4 DTOs |
| S0-02 | Entidades EF: `DbConnectionProfile`, `DbDiscoveryJob`, `DbCatalogSnapshot` | ✅ Done | Migration `20260613150754_DatabaseIntelligenceS0Foundation` |
| S0-03 | Connection vault cifrado (AES-GCM key ring) | ✅ Done | `DbIntelligenceConnectionVault` — magic `DBIV`, v1/v2 rotation test PASS |
| S0-04 | Connector abstraction + `TestConnectionAsync` read-only | ✅ Done | `IDbConnector`, `DbConnectorFactory`, 5 engine connectors |
| S0-05 | Seguridad tenant/RBAC/validación/timeout | ✅ Done | `DbIntelligenceTenantGuard`, `DbConnectionStringValidator`, read-only default |
| S0-06 | API `/api/db-intelligence/connections/*` | ✅ Done | `DatabaseIntelligenceController` — CRUD + test, no secretos en response |
| S0-07 | UI `/DatabaseIntelligence` + `/Connect` wizard 3 pasos | ✅ Done | Engine → details → test & save; nav sidebar |
| S0-08 | Forensic audit | ✅ Done | `DbIntelligenceForensicAudits` — Created/Tested/TestFailed/Deleted |
| S0-09 | Tests | ✅ Done | **16 PASS / 0 FAIL / 0 SKIP** (Category=DatabaseIntelligence) |

**Criterio de salida S0:** ✅ Cumplido — PostgreSQL local registrable, test PASS, credenciales cifradas (`DBIV` blob), tenant isolation PASS, auditoría registrada.

### S0 — Validation log

| Date | Build | Tests DIP | Tests DataHub | Notes |
|------|-------|-----------|---------------|-------|
| 2026-06-14 | ✅ PASS | ✅ 16/16 / 0 SKIP | ⚠️ 122/137 PASS (8 fail pre-existentes quota E2E + 7 RabbitMQ SKIP sin broker) | S0 no introduce regresión en código Data Hub |

```powershell
dotnet build                                          # PASS
dotnet test --filter Category=DatabaseIntelligence    # 16 PASS / 0 FAIL / 0 SKIP
dotnet test --filter FullyQualifiedName~DataHub       # 122 PASS / 8 FAIL / 7 SKIP (estado ambiental previo)
```

### S0 — Deliverables (code)

| Área | Archivo |
|------|---------|
| Contratos | `AutonomusCRM.Application/DatabaseIntelligence/DbIntelligenceContracts.cs` |
| Vault | `Infrastructure/DatabaseIntelligence/DbIntelligenceConnectionVault.cs` |
| Connectors | `Infrastructure/DatabaseIntelligence/Connectors/*.cs` (5 engines) |
| Services | `DbConnectionProfileService`, `DbIntelligenceAuditService`, `DbIntelligenceTenantGuard` |
| API | `AutonomusCRM.API/Controllers/DatabaseIntelligenceController.cs` |
| UI | `Pages/DatabaseIntelligence/Index.cshtml`, `Connect.cshtml` |
| Migration | `Persistence/Migrations/20260613150754_DatabaseIntelligenceS0Foundation.cs` |
| Tests | `AutonomusCRM.Tests/DatabaseIntelligence/*.cs` |

### S0 — Riesgos restantes

| Riesgo | Impacto | Mitigación (S1+) |
|--------|---------|------------------|
| SQL Server / MySQL / Oracle test E2E solo vía unit factory | Medio | Testcontainers matrix en S1 |
| Oracle SERVICE_NAME = DatabaseName (convención S0) | Bajo | Documentar en wizard S1 |
| Manager puede `POST /connections/test` sin guard adicional | Bajo | Aceptable S0; revisar en S7 RBAC |
| DataHub E2E quota 429 en DB compartida | Medio | Higiene DB test / aislamiento forense (Data Hub mantenimiento) |

---

## S1 — Database Discovery

**Objetivo:** Introspección automática del esquema físico (read-only).

| ID | Entregable | Status | Notas |
|----|------------|--------|-------|
| S1-01 | `IDbSchemaDiscoveryService` | ✅ Done | `StartDiscoveryAsync`, `GetDiscoveryJobAsync`, `GetCatalogSnapshotAsync`, `DiscoverNowAsync`; tenant scope + audit + vault decrypt |
| S1-02 | PostgreSQL introspector | ✅ Done | `PostgreSqlSchemaIntrospector` — `information_schema` + `pg_catalog`; E2E real PASS |
| S1-03 | SQL Server introspector | ✅ Done | `SqlServerSchemaIntrospector` — `INFORMATION_SCHEMA` + `sys.*`; unit fixture PASS (sin SQL Server local) |
| S1-04 | MySQL / MariaDB introspector | ✅ Done | `MySqlSchemaIntrospector` (+ MariaDB alias); unit fixture PASS |
| S1-05 | Oracle introspector | ✅ Done | `OracleSchemaIntrospector` — `ALL_*` views; unit fixture PASS |
| S1-06 | Catálogo versionado | ✅ Done | `DbCatalogSnapshot`, `DbCatalogSchema/Table/View/Column/Index/Relationship/Constraint`; migration `20260614130500_DatabaseIntelligenceS1Discovery` |
| S1-07 | Relaciones FK + naming heuristics | ✅ Done | Explicit FK = 100%; naming heuristic 60–85% (`DbRelationshipHeuristics`) |
| S1-08 | Discovery job async | ✅ Done | `DbDiscoveryJob` (Pending→Running→Completed/Failed); `DbDiscoveryBackgroundWorker` local; arquitectura RabbitMQ-ready |
| S1-09 | SignalR progress | ✅ Done | Hub `/hubs/db-intelligence`; eventos Started/Schema/Table/Progress/Completed/Failed; tenant isolation test PASS |
| S1-10 | API discovery + catalog | ✅ Done | `POST .../discover`, `GET .../discovery-jobs/{id}`, `GET .../catalog`, `/catalog/tables`, `/catalog/relationships`; RBAC + audit; sin secretos |
| S1-11 | UI `/DatabaseIntelligence/Explore` | ✅ Done | Conexión, estado discovery, tablas/vistas/columnas, badges PK/FK/Index/Nullable, relaciones; lenguaje negocio |
| S1-12 | Tests | ✅ Done | **29 PASS / 0 FAIL / 0 SKIP** (Category=DatabaseIntelligence) |

**Criterio de salida S1:** ✅ Cumplido — PostgreSQL discovery real PASS; catálogo físico persistido (tablas, columnas, PK/FK, índices, row counts); UI Explore funcional; SignalR + tenant isolation probados; 0 SKIP en suite DIP.

### S1 — Validation log

| Date | Build | Tests DIP | Tests DataHub | Notes |
|------|-------|-----------|---------------|-------|
| 2026-05-28 | ✅ PASS | ✅ **29/29 / 0 SKIP** | ⚠️ 122/137 PASS (8 fail pre-existentes quota E2E + 7 RabbitMQ SKIP sin broker) | S1 no introduce regresión en código Data Hub |

```powershell
dotnet build                                          # PASS
dotnet test --filter Category=DatabaseIntelligence    # 29 PASS / 0 FAIL / 0 SKIP
dotnet test --filter FullyQualifiedName~DataHub       # 122 PASS / 8 FAIL / 7 SKIP (estado ambiental previo)
```

### S1 — Tests ejecutados (Category=DatabaseIntelligence)

| Suite | Tests | Cobertura S1 |
|-------|-------|--------------|
| `DbIntelligenceDiscoveryUnitTests` | 8 | SQL guard read-only, naming heuristics, SQL Server/MySQL/Oracle fixtures, registry |
| `DbIntelligenceDiscoveryPostgresTests` | 3 | DiscoverNow real PG, catalog persist PK/FK/index/rows, tenant isolation catalog, API no secretos |
| `DbIntelligenceProgressHubTests` | 2 | Subscribe tenant, cross-tenant rejection |
| `DbIntelligenceConnectionApiTests` | 7 | S0 regression (connections CRUD/RBAC) |
| `DbIntelligenceVaultTests` | 3 | S0 regression (AES-GCM vault) |
| `DbIntelligenceSecurityTests` | 6 | S0 regression (DTO masking, validation) |

**Checklist S1-12 (user spec):**

| # | Criterio | Resultado |
|---|----------|-----------|
| 1 | PostgreSQL discovery real | ✅ PASS |
| 2 | Snapshot persiste tablas/columnas | ✅ PASS |
| 3 | PK/FK detectadas | ✅ PASS |
| 4 | Índices detectados | ✅ PASS |
| 5 | Estimated row count presente | ✅ PASS |
| 6 | Naming relationship heuristic | ✅ PASS |
| 7 | Tenant A no ve snapshot Tenant B | ✅ PASS |
| 8 | SignalR no cross-tenant | ✅ PASS |
| 9 | API no expone secretos | ✅ PASS |
| 10 | Discovery no SQL peligroso | ✅ PASS (`DbDiscoverySqlGuard`) |
| 11 | SQL Server introspector unit | ✅ PASS |
| 12 | MySQL introspector unit | ✅ PASS |
| 13 | Oracle introspector unit | ✅ PASS |
| 14 | Build PASS | ✅ PASS |
| 15 | No SKIP = PASS | ✅ 0 SKIP |

### S1 — Deliverables (code)

| Área | Archivo |
|------|---------|
| Contratos discovery | `Application/DatabaseIntelligence/DbIntelligenceDiscoveryContracts.cs` |
| Entidades catálogo | `Application/DatabaseIntelligence/DbIntelligenceCatalogEntities.cs` |
| Discovery service | `Infrastructure/DatabaseIntelligence/Discovery/DbSchemaDiscoveryService.cs` |
| Background worker | `DbDiscoveryBackgroundWorker` (same file) |
| Introspectors | `Infrastructure/DatabaseIntelligence/Discovery/*Introspector*.cs` (PG, SQL Server, MySQL, Oracle) |
| Helpers | `DbDiscoveryHelpers.cs` — SQL guard + relationship heuristics |
| SignalR | `API/Hubs/DbIntelligenceProgressHub.cs`, `DbIntelligenceProgressNotifier` |
| API | Endpoints discovery/catalog en `DatabaseIntelligenceController.cs` |
| UI | `Pages/DatabaseIntelligence/Explore.cshtml` |
| Migration | `Persistence/Migrations/20260614130500_DatabaseIntelligenceS1Discovery.cs` |
| Tests | `AutonomusCRM.Tests/DatabaseIntelligence/*.cs` |

### S1 — Riesgos restantes

| Riesgo | Impacto | Mitigación (S2+) |
|--------|---------|------------------|
| SQL Server / MySQL / Oracle sin E2E real en CI | Medio | Testcontainers matrix en S7; unit fixtures cubren S1 |
| Async discovery vía RabbitMQ no cableado aún | Bajo | Worker local operativo; cola dedicada en S5/S7 |
| `EnsureConnectionAsync` en tests reutiliza conexión PG existente (127.0.0.1) | Bajo | Documentar host Testcontainers en wizard S2 |
| DataHub E2E quota 429 en DB compartida | Medio | Higiene DB test (Data Hub mantenimiento only) |
| Explore UI sin SignalR live wiring en browser E2E | Bajo | Hub probado en integración; polish UX en S7 |

---

## S2 — Business Discovery

**Objetivo:** Inferir entidades de negocio aunque los nombres difieran (sin SQL expuesto).

| ID | Entregable | Status | Notas |
|----|------------|--------|-------|
| S2-01 | `IBusinessEntityInferenceEngine` | ✅ Done | `BusinessEntityInferenceEngine` — tablas, columnas, relaciones, cardinalidad, tipos, PK/FK |
| S2-02 | Taxonomía `BusinessEntityType` | ✅ Done | Customer, Company, Contact, Sale, Invoice, Payment, Product, Activity, User, Unknown |
| S2-03 | Signals engine (tabla/columna/FK/tipos) | ✅ Done | `BusinessEntitySignals` — perfiles EN/ES, pesos 40/30/15/15 |
| S2-04 | Multilanguage discovery | ✅ Done | `cliente_master`, `customer_master`, `tbl_cli`, `facturacion`, etc. PASS |
| S2-05 | Sample data analysis (TOP N read-only) | ✅ Done | `DbBusinessSampleReader` — LIMIT/TOP/FETCH FIRST; fallback catálogo si vault/DB falla |
| S2-06 | Confidence engine | ✅ Done | Score 40–99%; tablas fuertes ≥85% con piso de confianza |
| S2-07 | Explainability | ✅ Done | Razones por inferencia (`Reasons[]`); texto negocio en UI |
| S2-08 | `DbTableBusinessMapping` | ✅ Done | Persistencia versionada por snapshot + job; migration `DatabaseIntelligenceS2BusinessDiscovery` |
| S2-09 | UI `/DatabaseIntelligence/Understand` | ✅ Done | Confirmar / Corregir / Ignorar — lenguaje negocio |
| S2-10 | Business Memory + KnowledgeGraph | ✅ Done | Episodio + fact en confirmación; edge `MapsTo` en grafo |
| S2-11 | API business discovery | ✅ Done | `GET .../business-discovery/{id}`, `POST .../run`, `GET .../mappings`, `POST .../confirm` |
| S2-12 | SignalR progreso | ✅ Done | `BusinessDiscoveryStarted/Progress/Completed/Failed`; `SubscribeBusinessDiscoveryJob` |
| S2-13 | Datasets sintéticos | ✅ Done | `BusinessDiscoverySyntheticCatalogs` — tbl_cli, inv_hdr, pagos, sales_header, etc. |
| S2-14 | Tests | ✅ Done | **50 PASS / 0 FAIL / 0 SKIP** (Category=DatabaseIntelligence, incluye S0+S1 regression) |

**Criterio de salida S2:** ✅ Cumplido — Customer/Contact/Invoice/Payment/Product inferidos en datasets sintéticos; multilanguage PASS; mappings persistidos; tenant isolation + API security + SignalR isolation probados; 0 SKIP.

### S2 — Validation log

| Date | Build | Tests DIP | Tests DataHub | Notes |
|------|-------|-----------|---------------|-------|
| 2026-05-28 | ✅ PASS | ✅ **50/50 / 0 SKIP** | ⚠️ 122/137 PASS (8 fail pre-existentes quota E2E + 7 RabbitMQ SKIP) | S2 no introduce regresión en código Data Hub |

```powershell
dotnet build                                          # PASS
dotnet test --filter Category=DatabaseIntelligence    # 50 PASS / 0 FAIL / 0 SKIP
dotnet test --filter FullyQualifiedName~DataHub       # 122 PASS / 8 FAIL / 7 SKIP (estado ambiental previo)
```

### S2 — Tests ejecutados (nuevos S2)

| Suite | Tests | Cobertura S2 |
|-------|-------|--------------|
| `DbIntelligenceBusinessDiscoveryUnitTests` | 13 | Customer, Contact, Company, Invoice, Payment, Sale, Product, Activity, multilanguage, confidence, explainability, samples, progress stages |
| `DbIntelligenceBusinessDiscoveryIntegrationTests` | 5 | Mapping persistence, confirm, tenant isolation, API no secretos, GET latest |
| `DbIntelligenceUnderstandPageTests` | 1 | UI confirmation flow (page load Manager) |
| `DbIntelligenceProgressHubTests` (+2) | 4 | Business discovery job subscribe + cross-tenant rejection |
| S0+S1 regression suites | 27 | Vault, security, connections API, physical discovery, hub |

**Checklist S2-14:**

| # | Criterio | Resultado |
|---|----------|-----------|
| 1 | Customer inference | ✅ PASS |
| 2 | Contact inference | ✅ PASS |
| 3 | Company inference | ✅ PASS |
| 4 | Invoice inference | ✅ PASS |
| 5 | Payment inference | ✅ PASS |
| 6 | Product inference | ✅ PASS |
| 7 | Activity inference | ✅ PASS |
| 8 | Multilanguage inference | ✅ PASS |
| 9 | Confidence scoring | ✅ PASS |
| 10 | Explainability | ✅ PASS |
| 11 | Mapping persistence | ✅ PASS |
| 12 | Tenant isolation | ✅ PASS |
| 13 | API security | ✅ PASS |
| 14 | UI confirmation flow | ✅ PASS |
| 15 | SignalR isolation | ✅ PASS |
| 16 | Build PASS | ✅ PASS |
| 17 | No SKIP = PASS | ✅ 0 SKIP |

### S2 — Deliverables (code)

| Área | Archivo |
|------|---------|
| Contratos | `Application/DatabaseIntelligence/DbIntelligenceBusinessDiscoveryContracts.cs` |
| Inference engine | `Infrastructure/DatabaseIntelligence/BusinessDiscovery/BusinessEntityInferenceEngine.cs` |
| Signals | `BusinessEntitySignals.cs` |
| Orchestrator | `BusinessDiscoveryService.cs` |
| Sample reader | `DbBusinessSampleReader.cs` |
| SignalR | Extend `DbIntelligenceProgressHub.cs` + `IDbIntelligenceBusinessProgressNotifier` |
| API | Endpoints en `DatabaseIntelligenceController.cs` |
| UI | `Pages/DatabaseIntelligence/Understand.cshtml` |
| Migration | `Persistence/Migrations/*DatabaseIntelligenceS2BusinessDiscovery*` |
| Synthetic fixtures | `Tests/DatabaseIntelligence/BusinessDiscoverySyntheticCatalogs.cs` |
| Tests | `DbIntelligenceBusinessDiscoveryUnitTests.cs`, `DbIntelligenceBusinessDiscoveryIntegrationTests.cs`, `DbIntelligenceUnderstandPageTests.cs` |

### S2 — Riesgos restantes

| Riesgo | Impacto | Mitigación (S3+) |
|--------|---------|------------------|
| Sample TOP N requiere conexión válida en vault | Medio | Inferencia por catálogo siempre disponible; samples opcionales |
| order_lines puede clasificarse Product vs Sale line | Bajo | Confirmación humana en Understand; refinar en S3 |
| Bridge tables N:M heurística básica | Medio | S3 Data Health + S4 Graph |
| KnowledgeGraph edge usa mapping Id como target | Bajo | Enriquecer nodos de negocio en S4 |
| DataHub E2E quota 429 en DB compartida | Medio | Higiene DB test (Data Hub mantenimiento only) |

**Heurísticas de inferencia (v1 — implementadas):**

| Entidad | Señales fuertes | Señales débiles |
|---------|-----------------|-----------------|
| Customer / Company | `company`, `cliente`, `account`, `org` + name | FK desde orders |
| Contact | `contact`, `person`, email/phone columns | FK → company |
| Sale | `order`, `deal`, `venta`, amount + date | line items child table |
| Invoice | `invoice`, `factura`, number + total + date | FK → customer |
| Payment | `payment`, `pago`, amount + method | FK → invoice |
| Product | `product`, `sku`, `item`, price | FK desde order lines |
| Activity | `activity`, `task`, `call`, `meeting`, timestamp | FK → contact/customer |

---

## S3 — Data Health

**Objetivo:** Salud de datos comprensible para negocio, no métricas DBA.

| ID | Entregable | Status | Notas |
|----|------------|--------|-------|
| S3-01 | `IDataHealthEngine` + `DataHealthEngine` | ✅ Done | Orquesta scans, consolida hallazgos, calcula score global y por entidad |
| S3-02 | Calidad por columna | ✅ Done | Campos vacíos, emails/teléfonos/fechas inválidos, montos negativos, formatos inconsistentes; Completeness / Validity / Consistency scores |
| S3-03 | Detección de duplicados | ✅ Done | `DataHealthDuplicateDetector` — normalización alineada con Data Hub (email, tax id, customer, company); sin modificar Data Hub |
| S3-04 | Detección de huérfanos | ✅ Done | Facturas sin cliente, pagos sin factura, contactos sin empresa, ventas sin cliente; impacto de negocio en cada hallazgo |
| S3-05 | Integridad referencial | ✅ Done | FK rotas, relaciones inconsistentes, registros imposibles — lenguaje negocio ("relación rota"), no DDL |
| S3-06 | Consistencia de negocio | ✅ Done | Total factura ≠ líneas, pago > factura, ingresos/cantidades negativas, números de factura/pago duplicados |
| S3-07 | Health score 0–100 | ✅ Done | Customer, Company, Invoice, Payment, Product + global; bandas Excellent / Good / Fair / Critical |
| S3-08 | Health findings clasificados | ✅ Done | Critical / High / Medium / Low; explicación, impacto, evidencia, recomendación |
| S3-09 | UI `/DatabaseIntelligence/Health` | ✅ Done | Score global, score por entidad, hallazgos, tendencia, acciones sugeridas; sin SQL ni DDL |
| S3-10 | `DataHealthJob` + scan modes | ✅ Done | Pending → Running → Completed / CompletedWithWarnings / Failed; Full + Incremental |
| S3-11 | SignalR progreso health | ✅ Done | Scanning Customers / Invoices / Payments / Calculating Score / Completed; `SubscribeHealthScanJob`; tenant isolation PASS |
| S3-12 | API health | ✅ Done | `POST /health/run`, `GET /health/{jobId}`, `GET /health/latest`, `GET /health/findings`; RBAC + audit + tenant scoped |
| S3-13 | Datasets sintéticos | ✅ Done | `DataHealthSyntheticDatasets` — Healthy, Duplicate, Orphan, BrokenIntegrity, Mixed, Incremental |
| S3-14 | Tests | ✅ Done | **69 PASS / 0 FAIL / 0 SKIP** (Category=DatabaseIntelligence, incluye S0+S1+S2 regression) |

**Criterio de salida S3:** ✅ Cumplido — duplicados, huérfanos e inconsistencias detectados en fixtures; score calculado; dashboard funcional; API + SignalR + tenant isolation probados; 0 SKIP.

### S3 — Validation log

| Date | Build | Tests DIP | Tests DataHub | Notes |
|------|-------|-----------|---------------|-------|
| 2026-05-28 | ✅ PASS | ✅ **69/69 / 0 SKIP** | ⚠️ 122/137 PASS (8 fail pre-existentes quota E2E + 7 RabbitMQ SKIP sin broker) | S3 no introduce regresión en código Data Hub |

```powershell
dotnet build                                          # PASS
dotnet test --filter Category=DatabaseIntelligence    # 69 PASS / 0 FAIL / 0 SKIP
dotnet test --filter FullyQualifiedName~DataHub       # 122 PASS / 8 FAIL / 7 SKIP (estado ambiental previo)
```

### S3 — Tests ejecutados (nuevos S3)

| Suite | Tests | Cobertura S3 |
|-------|-------|--------------|
| `DbIntelligenceDataHealthUnitTests` | 12 | Customer/invoice/payment quality, duplicates, orphans, broken FK, consistency, scores, findings, incremental, progress stages |
| `DbIntelligenceDataHealthIntegrationTests` | 4 | Full scan persistence, tenant isolation, API no secretos, GET job metadata |
| `DbIntelligenceHealthPageTests` | 1 | Dashboard load (Manager) |
| `DbIntelligenceHealthHubTests` | 2 | Health job subscribe + cross-tenant rejection |
| S0+S1+S2 regression suites | 50 | Vault, security, connections, discovery, business inference, hub |

**Checklist S3-14 (user spec):**

| # | Criterio | Resultado |
|---|----------|-----------|
| 1 | Customer quality | ✅ PASS |
| 2 | Invoice quality | ✅ PASS |
| 3 | Payment quality | ✅ PASS |
| 4 | Duplicate detection | ✅ PASS |
| 5 | Orphan detection | ✅ PASS |
| 6 | Broken FK | ✅ PASS |
| 7 | Consistency rules | ✅ PASS |
| 8 | Health score | ✅ PASS |
| 9 | Health findings | ✅ PASS |
| 10 | API security | ✅ PASS |
| 11 | Tenant isolation | ✅ PASS |
| 12 | SignalR isolation | ✅ PASS |
| 13 | Dashboard load | ✅ PASS |
| 14 | Full scan | ✅ PASS |
| 15 | Incremental scan | ✅ PASS |
| 16 | Build PASS | ✅ PASS |
| 17 | No SKIP = PASS | ✅ 0 SKIP |

### S3 — Deliverables (code)

| Área | Archivo |
|------|---------|
| Contratos | `Application/DatabaseIntelligence/DbIntelligenceHealthContracts.cs` |
| Engine | `Infrastructure/DatabaseIntelligence/Health/DataHealthEngine.cs` |
| Duplicate detector | `Infrastructure/DatabaseIntelligence/Health/DataHealthDuplicateDetector.cs` |
| Orchestrator | `Infrastructure/DatabaseIntelligence/Health/DataHealthService.cs` |
| SignalR | Extend `API/Hubs/DbIntelligenceProgressHub.cs` + `IDbIntelligenceHealthProgressNotifier` |
| API | Endpoints health en `DatabaseIntelligenceController.cs` |
| UI | `Pages/DatabaseIntelligence/Health.cshtml` |
| Migration | `Persistence/Migrations/20260614134510_DatabaseIntelligenceS3DataHealth.cs` |
| Synthetic fixtures | `Tests/DatabaseIntelligence/DataHealthSyntheticDatasets.cs` |
| Tests | `DbIntelligenceDataHealthUnitTests.cs`, `DbIntelligenceDataHealthIntegrationTests.cs`, `DbIntelligenceHealthPageTests.cs` |

### S3 — Riesgos restantes

| Riesgo | Impacto | Mitigación (S4+) |
|--------|---------|------------------|
| Duplicate detection replica lógica Data Hub en DIP (no `IDataHubDuplicateEngine` directo — requiere import job rows) | Bajo | Mantener normalización alineada; unificar en S7 si aplica |
| `POST /health/run` requiere catálogo físico + mappings S2 previos | Medio | Wizard UX encadena Discover → Understand → Health |
| Incremental scan usa ventana temporal básica (no CDC) | Medio | Watermark / `updated_at` en S5 sync |
| Sample TOP N opcional — health opera con catálogo + samples sintéticos en tests | Bajo | Conexión vault válida mejora cobertura en producción |
| DataHub E2E quota 429 en DB compartida | Medio | Higiene DB test (Data Hub mantenimiento only) |
| RabbitMQ worker dedicado health no cableado (scan síncrono en API) | Bajo | Cola `db-intelligence.health.jobs` en S7 |

---

## S4 — Database Graph

**Objetivo:** Grafo visual del negocio del cliente — no diagrama ER técnico.

| ID | Entregable | Status | Notas |
|----|------------|--------|-------|
| S4-01 | `IDbBusinessGraphBuilder` + `DbBusinessGraphBuilder` | ✅ Done | Construye nodos, relaciones y vistas de negocio desde mappings S2 + catálogo S1 |
| S4-02 | Modelo de negocio (nodos + edges) | ✅ Done | Company, Customer, Contact, Sale, Invoice, Payment, Product, Activity; HasContacts, GeneratedSale, GeneratedInvoice, GeneratedPayment, PurchasedProduct, HasActivity |
| S4-03 | Graph confidence | ✅ Done | Confidence %, source mappings, business names por nodo |
| S4-04 | Health integration (S3) | ✅ Done | Score, band, risk level, top findings por nodo |
| S4-05 | Graph metrics | ✅ Done | Record count, relationships, duplicates, orphans, summary global |
| S4-06 | Graph API | ✅ Done | `GET graph/{id}`, `/nodes`, `/edges`, `/summary`, `POST build`, `POST export`; RBAC + audit + tenant scoped |
| S4-07 | UI `/DatabaseIntelligence/Graph` | ✅ Done | Zoom, pan, search, filters (Product/Activity), node click → health/sources/findings |
| S4-08 | Business view | ✅ Done | Labels de negocio únicamente en vista principal — sin SQL ni nombres técnicos |
| S4-09 | Graph export | ✅ Done | PNG (SVG), PDF, JSON snapshot — para gerencia/auditoría |
| S4-10 | KnowledgeGraph + BusinessMemory | ✅ Done | Edges `DipBusinessEntity` + `MapsTo`; episodio `dbi-graph-{connectionId}` |
| S4-11 | SignalR progreso | ✅ Done | BuildingGraph → CreatingNodes → CreatingRelationships → CalculatingMetrics → Completed; `SubscribeGraphBuildJob` |
| S4-12 | Datasets sintéticos | ✅ Done | `GraphSyntheticDatasets` — SMB, Enterprise, Mixed, BrokenRelationship, Large |
| S4-13 | Tests | ✅ Done | **89 PASS / 0 FAIL / 0 SKIP** (Category=DatabaseIntelligence, incluye S0–S3 regression) |

**Criterio de salida S4:** ✅ Cumplido — grafo generado con nodos/relaciones correctos; health integrado; export funcional; UI + API + SignalR + tenant isolation probados; 0 SKIP.

### S4 — Validation log

| Date | Build | Tests DIP | Tests DataHub | Notes |
|------|-------|-----------|---------------|-------|
| 2026-05-28 | ✅ PASS | ✅ **89/89 / 0 SKIP** | ⚠️ 122/137 PASS (8 fail pre-existentes quota E2E + 7 RabbitMQ SKIP sin broker) | S4 no introduce regresión en código Data Hub |

```powershell
dotnet build                                          # PASS
dotnet test --filter Category=DatabaseIntelligence    # 89 PASS / 0 FAIL / 0 SKIP
dotnet test --filter FullyQualifiedName~DataHub       # 122 PASS / 8 FAIL / 7 SKIP (estado ambiental previo)
```

### S4 — Tests ejecutados (nuevos S4)

| Suite | Tests | Cobertura S4 |
|-------|-------|--------------|
| `DbIntelligenceGraphUnitTests` | 12 | Graph build, nodes, edges, confidence, health, metrics, export, broken relationships, summary, large dataset, progress, business view |
| `DbIntelligenceGraphIntegrationTests` | 5 | API persistence, tenant isolation, API security, export PNG, KnowledgeGraph persistence |
| `DbIntelligenceGraphPageTests` | 1 | Dashboard load (Manager) |
| `DbIntelligenceGraphHubTests` | 2 | Graph job subscribe + cross-tenant rejection |
| S0+S1+S2+S3 regression suites | 69 | Vault, discovery, business inference, health, hub |

**Checklist S4-13 (user spec):**

| # | Criterio | Resultado |
|---|----------|-----------|
| 1 | Graph build | ✅ PASS |
| 2 | Node creation | ✅ PASS |
| 3 | Edge creation | ✅ PASS |
| 4 | Confidence | ✅ PASS |
| 5 | Health integration | ✅ PASS |
| 6 | Metrics | ✅ PASS |
| 7 | Graph export | ✅ PASS |
| 8 | API security | ✅ PASS |
| 9 | Tenant isolation | ✅ PASS |
| 10 | SignalR isolation | ✅ PASS |
| 11 | UI graph load | ✅ PASS |
| 12 | Large dataset | ✅ PASS |
| 13 | Broken relationships | ✅ PASS |
| 14 | Summary generation | ✅ PASS |
| 15 | KnowledgeGraph persistence | ✅ PASS |
| 16 | Build PASS | ✅ PASS |
| 17 | No SKIP = PASS | ✅ 0 SKIP |

### S4 — Deliverables (code)

| Área | Archivo |
|------|---------|
| Contratos | `Application/DatabaseIntelligence/DbIntelligenceGraphContracts.cs` |
| Builder | `Infrastructure/DatabaseIntelligence/Graph/DbBusinessGraphBuilder.cs` |
| Service | `Infrastructure/DatabaseIntelligence/Graph/DbBusinessGraphService.cs` |
| Export | `Infrastructure/DatabaseIntelligence/Graph/DbBusinessGraphExporter.cs` |
| SignalR | Extend `API/Hubs/DbIntelligenceProgressHub.cs` + `IDbIntelligenceGraphProgressNotifier` |
| API | Endpoints graph en `DatabaseIntelligenceController.cs` |
| UI | `Pages/DatabaseIntelligence/Graph.cshtml` |
| Migration | `Persistence/Migrations/*DatabaseIntelligenceS4BusinessGraph*` |
| Synthetic fixtures | `Tests/DatabaseIntelligence/GraphSyntheticDatasets.cs` |
| Tests | `DbIntelligenceGraphUnitTests.cs`, `DbIntelligenceGraphIntegrationTests.cs`, `DbIntelligenceGraphPageTests.cs` |

### S4 — Riesgos restantes

| Riesgo | Impacto | Mitigación (S5+) |
|--------|---------|------------------|
| Layout jerárquico vertical básico (no force-directed avanzado) | Bajo | Polish canvas en S7 |
| PNG export es SVG (no raster) — suficiente para ejecutivos | Bajo | Rasterización server-side en S7 si requerido |
| `POST graph/build` requiere catálogo + mappings + health opcional | Medio | Wizard UX encadena flujo completo |
| KnowledgeGraph edges DIP coexisten con edges CRM existentes | Bajo | Filtrar por `DipBusinessEntity` en traversals S6 |
| DataHub E2E quota 429 en DB compartida | Medio | Higiene DB test (Data Hub mantenimiento only) |

---

## S5 — Sync Engine

**Objetivo:** Sincronizar datos inferidos desde bases externas hacia AutonomusCRM — complemento de Data Hub, no reemplazo. Solo lectura en BD externa; staging interno obligatorio.

| ID | Entregable | Status | Notas |
|----|------------|--------|-------|
| S5-01 | `IDbSyncOrchestrator` + `DbSyncOrchestrator` | ✅ Done | Full / Delta / Scheduled; historial; rollback; `ProcessPendingJobAsync` |
| S5-02 | Full Sync | ✅ Done | Customer, Company→Customer, Contact/Activity→Lead, Sale→Deal desde entidades inferidas |
| S5-03 | Delta Sync | ✅ Done | Watermarks `updated_at` / `modified_at`; `DbSyncWatermark`; filtro en extract |
| S5-04 | Scheduled Sync | ✅ Done | Once / Hourly / Daily / Weekly; lease model; `DbSyncScheduledWorker` |
| S5-05 | Staging layer | ✅ Done | External → `DbSyncStagingRows` → Validation → Mapping → CRM; nunca directo al CRM |
| S5-06 | Conflict resolution | ✅ Done | SourceWins / CrmWins / ManualReview; decisión persistida en job + staging |
| S5-07 | Rollback | ✅ Done | Patrón Data Hub; por job/batch; `DbSyncRollbackService` + snapshots |
| S5-08 | Sync history | ✅ Done | Inicio, fin, registros, errores, duración, usuario en `DbSyncJob` |
| S5-09 | RabbitMQ `db-intelligence.sync.jobs` | ✅ Done | `DbSyncDispatcher`, `DbSyncRabbitWorker`, in-process fallback `DbSyncInProcessJobQueue` |
| S5-10 | SignalR progreso sync | ✅ Done | ReadingSource → BuildingStaging → Validating → Importing → Completed; `SubscribeSyncJob` |
| S5-11 | UI `/DatabaseIntelligence/Sync` | ✅ Done | Full / Delta / Scheduled / Historial / Estado |
| S5-12 | API sync | ✅ Done | `POST sync/full`, `POST sync/delta`, `POST sync/schedule`, `GET history`, `GET {id}`, `POST {id}/rollback`; RBAC + audit + tenant scoped |
| S5-13 | Safety read-only | ✅ Done | `DbDiscoverySqlGuard`; nunca UPDATE/DELETE/INSERT/DROP/ALTER externo |
| S5-14 | Test datasets | ✅ Done | `SyncSyntheticDatasets` — SMB, Enterprise, Large, Delta, Conflict |
| S5-15 | Tests | ✅ Done | **112 PASS / 0 FAIL / 0 SKIP** (Category=DatabaseIntelligence, incluye S0–S4 regression) |

**Criterio de salida S5:** ✅ Cumplido — Full/Delta/Scheduled sync operativos; staging + rollback + RabbitMQ + SignalR + tenant isolation probados; 0 SKIP en suite DIP; Data Hub sin regresión de código.

### S5 — Validation log

| Date | Build | Tests DIP | Tests DataHub | Notes |
|------|-------|-----------|---------------|-------|
| 2026-06-14 | ✅ PASS | ✅ **112/112 / 0 SKIP** | ⚠️ 122/137 PASS (8 fail pre-existentes quota E2E + 7 RabbitMQ SKIP sin broker) | S5 no modifica código Data Hub; fallos ambientales previos |

```powershell
dotnet build                                          # PASS
dotnet test --filter Category=DatabaseIntelligence    # 112 PASS / 0 FAIL / 0 SKIP
dotnet test --filter FullyQualifiedName~DataHub       # 122 PASS / 8 FAIL / 7 SKIP (estado ambiental previo)
```

### S5 — Tests ejecutados (nuevos S5)

| Suite | Tests | Cobertura S5 |
|-------|-------|--------------|
| `DbIntelligenceSyncUnitTests` | 10 | Conflict policies, SQL guard safety, delta filter, queue name, staging validation, CRM mapping rules |
| `DbIntelligenceSyncIntegrationTests` | 11 | Full sync, delta watermark, rollback, history API, tenant isolation, API security, scheduled sync, CRM mapping, watermark, recovery |
| `DbIntelligenceSyncPageTests` | 1 | UI Sync page load (Manager) |
| `DbIntelligenceSyncHubTests` | 2 | Sync job subscribe + cross-tenant rejection |
| S0+S1+S2+S3+S4 regression suites | 89 | Vault, discovery, business inference, health, graph, hub |

**Checklist S5-15 (user spec):**

| # | Criterio | Resultado |
|---|----------|-----------|
| 1 | Full Sync | ✅ PASS |
| 2 | Delta Sync | ✅ PASS |
| 3 | Scheduled Sync | ✅ PASS |
| 4 | Conflict Resolution | ✅ PASS |
| 5 | Rollback | ✅ PASS |
| 6 | Staging | ✅ PASS |
| 7 | RabbitMQ | ✅ PASS |
| 8 | SignalR | ✅ PASS |
| 9 | Tenant Isolation | ✅ PASS |
| 10 | API Security | ✅ PASS |
| 11 | Sync History | ✅ PASS |
| 12 | Large Dataset | ✅ PASS |
| 13 | Watermark | ✅ PASS |
| 14 | CRM Mapping | ✅ PASS |
| 15 | Recovery | ✅ PASS |
| 16 | Build PASS | ✅ PASS |
| 17 | No SKIP = PASS | ✅ 0 SKIP |

### S5 — Deliverables (code)

| Área | Archivo |
|------|---------|
| Contratos | `Application/DatabaseIntelligence/DbIntelligenceSyncContracts.cs` |
| Orchestrator | `Infrastructure/DatabaseIntelligence/Sync/DbSyncOrchestrator.cs` |
| Pipeline | `DbSyncPipeline.cs` — extract → stage → validate → load → watermarks |
| Extract / Staging / Load | `DbSyncExtractService.cs`, `DbSyncStagingService.cs`, `DbSyncLoadService.cs` |
| Rollback / Conflict / Schedule | `DbSyncRollbackService.cs`, `DbSyncConflictResolver.cs`, `DbSyncScheduleService.cs` |
| Workers / MQ | `DbSyncWorkers.cs` — cola `db-intelligence.sync.jobs` |
| SignalR | Extend `API/Hubs/DbIntelligenceProgressHub.cs` + `IDbIntelligenceSyncProgressNotifier` |
| API | Endpoints sync en `DatabaseIntelligenceController.cs` |
| UI | `Pages/DatabaseIntelligence/Sync.cshtml` |
| Migration | `Persistence/Migrations/20260614143645_DatabaseIntelligenceS5SyncEngine.cs` |
| Synthetic fixtures | `Tests/DatabaseIntelligence/SyncSyntheticDatasets.cs` |
| Tests | `DbIntelligenceSyncUnitTests.cs`, `DbIntelligenceSyncIntegrationTests.cs`, `DbIntelligenceSyncPageTests.cs`, `DbIntelligenceSyncHubTests.cs` |

### S5 — Riesgos restantes

| Riesgo | Impacto | Mitigación (S6+) |
|--------|---------|------------------|
| Domain events en `CreateCustomerCommand` pueden dejar entidades tracked si RabbitMQ falla | Medio | `DetachNonSyncEntities` en pipeline final; considerar import silencioso en S7 |
| `DbSyncRabbitWorker` requiere broker para E2E operacional completo | Medio | In-process queue operativo en tests; broker en S7 hardening |
| Deal sync requiere customer email en mismo dataset / orden | Bajo | Documentar en UI Sync; enriquecer mapping en S6 |
| Companies/Contacts/Activities mapean a Customer/Lead (no entidades CRM dedicadas aún) | Bajo | Extender mapping cuando CRM exponga Companies/Activities nativos |
| DataHub E2E quota 429 en DB compartida | Medio | Higiene DB test (Data Hub mantenimiento only) |
| Large dataset test (200 rows) es sintético in-memory, no PG externo 1K | Bajo | Benchmark PG demo en S7 |

---

## S6 — AI Insights

**Objetivo:** Recomendaciones accionables para usuarios de negocio.

| ID | Entregable | Status | Insight type |
|----|------------|--------|--------------|
| S6-01 | `IDbIntelligenceInsightEngine` | ⬜ | Orquestador |
| S6-02 | Tablas críticas | ⬜ | High fan-out FK, high row count, low health |
| S6-03 | Datos sin uso | ⬜ | Tablas huérfanas, columnas 100% null, stale data |
| S6-04 | Oportunidades migración | ⬜ | "Unificar 3 tablas cliente en Customer" |
| S6-05 | Riesgos calidad | ⬜ | Duplicados revenue, pagos sin factura |
| S6-06 | Entidades negocio no mapeadas | ⬜ | Tablas con señales fuertes sin confirmar |
| S6-07 | UI `/DatabaseIntelligence/Insights` — feed priorizado | ⬜ | Impact / effort / confidence |
| S6-08 | Opcional: embeddings semánticos (`SemanticMemory`) | ⬜ | Similaridad tabla ↔ entidad CRM |
| S6-09 | Explainability | ⬜ | "Por qué creemos que X es Facturas" |
| S6-10 | Tests: insight generation on fixture | ⬜ | ≥5 insight types con assertions |

**Criterio de salida S6:** ≥8 insights generados en BD demo; cada uno con evidencia y acción sugerida en lenguaje negocio.

---

## S7 — Enterprise Hardening & GA

**Objetivo:** Production-ready, certificable, mantenible.

| ID | Entregable | Status | Notas |
|----|------------|--------|-------|
| S7-01 | Rate limits + quotas por tenant | ⬜ | Patrón DataHubSecurityQuotaService |
| S7-02 | Connection credential rotation | ⬜ | Re-encrypt con nueva key |
| S7-03 | Observability: metrics, traces, structured logs | ⬜ | OTel existente |
| S7-04 | RBAC granular | ⬜ | Viewer read graph; Manager sync; Owner connections |
| S7-05 | Documentación operativa `ops/db-intelligence/` | ⬜ | Runbooks, no marketing docs |
| S7-06 | Certification test suite `DatabaseIntelligence*` | ⬜ | Target: 0 SKIP en CI |
| S7-07 | Performance: discovery ≤5 min / 500 tables | ⬜ | Benchmark documentado |
| S7-08 | UX accessibility pass | ⬜ | Business user WCAG baseline |
| S7-09 | `DATABASE_INTELLIGENCE_CERTIFICATION_EVIDENCE.md` | ⬜ | Evidencia ejecutable |
| S7-10 | GA sign-off tracker update | ⬜ | Score 100/100 |

**Criterio de salida S7:** Suite certificación verde reproducible; score 100/100; listo para clientes enterprise.

---

## UX — Flujo principal (usuarios de negocio)

```
[Conectar BD] → [Descubrir automáticamente] → [Entender negocio]
       ↓                    ↓                         ↓
  Wizard simple      "Analizando..."           Tarjetas confirmación
  Sin SQL            SignalR progress          "¿Es esto Facturas?"
       ↓                    ↓                         ↓
[Ver salud]  ←────── [Ver grafo] ──────→  [Sincronizar con CRM]
  Semáforos           Visual                 Full / Delta / Programado
  Acciones            Empresa→Pagos          Historial + rollback
       ↓
[Insights IA]
  Priorizados por impacto
```

**Pantallas planificadas:**

| Ruta | Audiencia | Propósito |
|------|-----------|-----------|
| `/DatabaseIntelligence` | Todos | Hub — conexiones activas, health global, insights |
| `/DatabaseIntelligence/Connect` | Owner/Admin | Alta conexión |
| `/DatabaseIntelligence/Explore` | Manager+ | Catálogo tablas (negocio, no DDL) |
| `/DatabaseIntelligence/Understand` | Manager+ | Confirmar entidades inferidas |
| `/DatabaseIntelligence/Health` | Todos | Salud datos ✅ S3 |
| `/DatabaseIntelligence/Graph` | Todos | Grafo visual ✅ S4 |
| `/DatabaseIntelligence/Sync` | Manager+ | Sync policies ✅ S5 |
| `/DatabaseIntelligence/Insights` | Todos | AI recommendations |

---

## Seguridad (reutilización obligatoria)

| Control | Implementación | Fase |
|---------|----------------|------|
| Tenant isolation | `DbTenantGuard` (fail-closed, sin Admin bypass) | S0 |
| Encryption | Connection vault AES-GCM + key ring | S0 |
| Audit | `DbForensicAudits` — connect, discover, sync, export | S0 |
| Read-only default | Connectors sin DDL/DML salvo sync explícito | S1 |
| Secrets | Nunca loguear connection strings; mask en UI | S0 |
| RabbitMQ | Jobs aislados por tenantId en payload | S1 |
| SignalR | Hub auth Admin/Manager/Owner; subscribe scoped | S1 |
| Quotas | Max connections/tenant, max discovery/hour | S7 |

---

## Conectores — matriz de capacidades

| Engine | S0 Test | S1 Discovery | S5 Sync read | S5 Sync write | Notas |
|--------|---------|--------------|--------------|---------------|-------|
| PostgreSQL | ✅ Done | ✅ Done | ✅ Done | ✅ Done | Referencia; Npgsql; discovery + sync E2E PASS |
| SQL Server | ✅ Done | ✅ Done (unit) | ✅ Plan | ⚠️ v1 read-only sync | Microsoft.Data.SqlClient |
| MySQL | ✅ Done | ✅ Done (unit) | ✅ Plan | ⚠️ v1 read-only sync | MySqlConnector |
| MariaDB | ✅ Done | ✅ Done (unit) | ✅ Plan | ⚠️ v1 read-only sync | Compatible MySQL |
| Oracle | ✅ Done | ✅ Done (unit) | ✅ Plan | ⚠️ Phase 2 | Oracle.ManagedDataAccess |

---

## Dependencias técnicas (NuGet / infra)

| Paquete | Uso |
|---------|-----|
| `Npgsql` | PostgreSQL connector (existente) |
| `Microsoft.Data.SqlClient` | SQL Server |
| `MySqlConnector` | MySQL / MariaDB |
| `Oracle.ManagedDataAccess.Core` | Oracle |
| RabbitMQ.Client | Jobs async (existente) |
| SignalR | Progress (existente) |

**Infra CI:** Testcontainers matrix o servicios docker-compose dedicados `ops/db-intelligence/docker-compose.fixtures.yml`.

---

## Métricas de éxito (producto)

| Métrica | Target GA |
|---------|-----------|
| Time-to-understand | ≤15 min desde conectar hasta grafo confirmado (BD típica SMB) |
| Inference accuracy | ≥85% entidades core confirmadas sin editar (benchmark interno) |
| Discovery performance | ≤5 min / 500 tablas |
| Health scan | ≤10 min / 1M rows sampled |
| Business user completion | ≥90% wizard sin soporte técnico (UX test) |
| Zero cross-tenant leaks | 100% tests isolation PASS |

---

## Riesgos y mitigaciones

| Riesgo | Prob. | Impacto | Mitigación |
|--------|-------|---------|------------|
| Oracle / SQL Server variants en clientes legacy | Alta | Medio | Test matrix ampliado; introspectors por versión |
| Inferencia incorrecta en esquemas caóticos | Media | Alto | Confirmación humana obligatoria; confidence thresholds |
| Sync sobrescribe CRM | Media | Crítico | Dry-run default; rollback; conflict policies |
| Performance discovery en BD grandes | Alta | Medio | Sampling, parallel table scan, RabbitMQ jobs |
| Cliente bloquea IP / firewall | Media | Alto | Documentar allowlist; agent on-premise (futuro) |
| Confusión con Data Hub | Baja | Medio | UX copy claro; hub separado en nav |

---

## Reglas de gobernanza

1. **Data Hub:** mantenimiento correctivo únicamente — no nuevas features.
2. **DIP:** toda feature nueva va a `DatabaseIntelligence` namespace.
3. **Reuse first:** extender Data Hub / Intelligence antes de duplicar.
4. **No SKIP = PASS:** certificación igual que Data Hub.
5. **Business UX gate:** ninguna pantalla DIP expone SQL, DDL ni nombres técnicos por default.

---

## Validation log

| Date | Phase | Build | Tests DIP | Notes |
|------|-------|-------|-----------|-------|
| 2026-06-14 | S0 | ✅ PASS | ✅ 16/16 / 0 SKIP | Foundation & Connection Vault complete |
| 2026-05-28 | S1 | ✅ PASS | ✅ 29/29 / 0 SKIP | Database Discovery complete |
| 2026-05-28 | S2 | ✅ PASS | ✅ **50/50 / 0 SKIP** | Business Discovery Engine complete |
| 2026-05-28 | S3 | ✅ PASS | ✅ **69/69 / 0 SKIP** | Data Health Engine complete |
| 2026-05-28 | S4 | ✅ PASS | ✅ **89/89 / 0 SKIP** | Database Graph complete |
| 2026-06-14 | S5 | ✅ PASS | ✅ **112/112 / 0 SKIP** | Sync Engine complete |

---

## Próximo paso inmediato

**S6 AI Insights** — pendiente de aprobación de producto (no iniciar sin go-ahead).

---

## Score tracker

| Milestone | Score | Status |
|-----------|-------|--------|
| S0 complete | 10/100 | ✅ |
| S1 complete | 25/100 | ✅ |
| S2 complete | 45/100 | ✅ |
| S3 complete | 60/100 | ✅ |
| S4 complete | 75/100 | ✅ |
| S5 complete | 85/100 | ✅ |
| S6 complete | 92/100 | ⬜ |
| S7 complete (GA) | **100/100** | ⬜ |

**Current score:** **85/100** — S5 complete; next: S6 AI Insights (on hold per roadmap).
