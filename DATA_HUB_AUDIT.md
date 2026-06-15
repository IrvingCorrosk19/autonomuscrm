# DATA HUB AUDIT — AutonomusCRM / AutonomusFlow

**Fecha:** 2026-06-12  
**Alcance:** Importación, migración, sincronización, exportación, procesamiento masivo y calidad de datos  
**Método:** Análisis exclusivo del código fuente (sin suposiciones)

---

## Resumen ejecutivo

AutonomusCRM **no posee un Data Hub empresarial unificado**. Existen **capacidades fragmentadas** de importación CSV/JSON, conectores de sincronización limitados, exportación parcial y servicios de calidad de datos básicos. No hay wizard de migración, colas de importación masiva, historial de jobs ni soporte Excel/XLSX.

**Calificación global: 38 / 100**

**Veredicto:** El Data Hub **debe construirse** como producto cohesivo. Las piezas actuales son un **MVP técnico**, no un módulo enterprise comparable a Salesforce Data Import Wizard, HubSpot Operations Hub o Dynamics Data Migration Framework.

---

## Respuestas directas (10 preguntas)

| # | Pregunta | Respuesta |
|---|----------|-----------|
| 1 | ¿Existe módulo de importación? | **Parcial** — `ICrmImportService` + UI/API para CSV/JSON |
| 2 | ¿Existe módulo de exportación? | **Parcial** — JSON client-side + CSV customers API + HTML executive |
| 3 | ¿Existe módulo de migración? | **No** — conectores pull limitados, sin wizard ni ETL |
| 4 | ¿Existe módulo de sincronización? | **Parcial** — HubSpot, Salesforce, Gmail, Outlook, Stripe (manual) |
| 5 | ¿Existe módulo de carga masiva? | **No** — máx. 5 000 filas síncronas, sin cola |
| 6 | ¿Existe módulo de calidad de datos? | **Parcial** — dedup email, merge, agentes DQ |
| 7 | ¿Qué tan completo está? | **~38%** de un Data Hub enterprise |
| 8 | Calificación 0–100 | **38** |
| 9 | ¿Qué falta vs Salesforce/HubSpot/Dynamics? | Ver §9 |
| 10 | ¿Prioridad de construirlo? | **Alta (P1)** para adopción enterprise y migraciones |

---

## FASE 1 — Importación

### Formatos soportados (código real)

| Formato | Soporte | Evidencia |
|---------|---------|-----------|
| CSV | Sí | `ImportController`, páginas `*/Import.cshtml.cs` |
| JSON | Sí | Mismo stack |
| XLSX / Excel | **No** | Sin `ClosedXML`, `EPPlus`, `Spreadsheet` en solución |
| TXT | **No** | No hay parser dedicado |
| XML | **No** | Solo SAML (auth), no import CRM |

### Componentes encontrados

| Componente | Ruta | Función |
|------------|------|---------|
| `ImportController` | `AutonomusCRM.API/Controllers/ImportController.cs` | API: `POST /api/import/customers|leads|deals` |
| `CrmImportService` | `AutonomusCRM.Infrastructure/Imports/CrmImportService.cs` | Loop síncrono → `Create*Command` |
| `ImportGuard` | `AutonomusCRM.Application/Common/Imports/ImportGuard.cs` | Max 5 MB, max 5 000 filas, solo `.csv`/`.json` |
| `DataAcquisitionService` | `Infrastructure/DataPlatform/DataPlatformServices.cs` | Ingest webhook → `ImportCustomersAsync` |
| UI Import | `/Customers/Import`, `/Leads/Import`, `/Deals/Import`, `/Users/Import`, `/Policies/Import`, `/Workflows/Import` | Form POST multipart |

### Entidades importables

| Entidad | API | UI | Notas |
|---------|-----|-----|-------|
| Customers | Sí | Sí | Name, Email, Phone, Company |
| Leads | Sí | Sí | Name, Source, Email, Phone, Company |
| Deals | Sí | Sí | Title, Amount, Stage, CustomerEmail |
| Users | No API dedicada | Sí | JSON/CSV vía página |
| Policies | No API dedicada | Sí | JSON |
| Workflows | No API dedicada | Sí | JSON |

### Limitaciones críticas

- Parser CSV **naïve** (`Split(',')`) — no escapa comillas ni campos con comas
- **Sin upsert** — siempre crea registros nuevos
- **Sin mapeo de columnas** — esquema fijo
- **Sin historial** de importaciones
- **Sin progreso** async — request HTTP bloqueante

---

## FASE 2 — Migración de datos

### CRM / ERP externos

| Sistema | Código | Estado |
|---------|--------|--------|
| **HubSpot** | `HubSpotConnector.cs` | Pull contacts v3 API, limit 100 |
| **Salesforce** | `SalesforceConnector.cs` | SOQL `Contact LIMIT 100` |
| **Gmail** | `GmailConnector.cs` | Extrae emails de headers (50 msgs) |
| **Outlook** | `OutlookConnector.cs` | Microsoft Graph messages top 50 |
| **Stripe** | `StripeDataConnector.cs` | Customers limit 100 |
| Zoho | **No encontrado** | — |
| Dynamics | **No encontrado** | — |
| Pipedrive | **No encontrado** | — |
| SAP / Oracle | **No encontrado** | — |

### Bases de datos externas

| Motor | Soporte migración directa |
|-------|---------------------------|
| PostgreSQL | Solo como BD propia del producto |
| SQL Server | **No** |
| MySQL | **No** |

### Servicios de migración

| Búsqueda | Resultado |
|----------|-----------|
| `MigrationService` | **No existe** |
| `DataMigration` | **No existe** |
| `ImportWizard` | **No existe** |
| `ExternalConnector` | Patrón `IntegrationConnectorBase` (sync, no migración) |

**Conclusión:** No hay módulo de migración enterprise. Los conectores son **sync incremental manual** con límites duros (~100 registros).

---

## FASE 3 — Sincronización

### Arquitectura

```
IntegrationsController → IntegrationHubService → IIntegrationConnector.SyncBidirectionalAsync
                                                      ↓
                              PullExternalRecordsAsync → ICrmImportService.ImportCustomersAsync
                              PushLocalChangesAsync  → API externa (HubSpot push max 20)
```

| Provider | Pull | Push | OAuth |
|----------|------|------|-------|
| HubSpot | Contacts 100 | Create/update contacts (20) | Sí |
| Salesforce | Contacts 100 | **0** (stub) | Sí |
| Gmail | Emails → customers | **0** | Sí |
| Outlook | Messages → customers | **0** | Sí |
| Stripe | Customers 100 | **0** | Manual key |

### APIs de sync

| Endpoint | Archivo |
|----------|---------|
| `POST /api/integrations/sync/{provider}` | `IntegrationsController.cs` |
| `POST /api/integrations/sync` | Sync all providers |
| `GET /api/integrations/sync/{provider}/conflicts` | `SyncConflictService` |

### Lo que NO existe

- Sync programado (cron/worker) — **Workers no referencian Integration**
- Google Contacts API dedicada — Gmail extrae de headers
- IMAP / POP3 — **No encontrado**
- Microsoft 365 Contacts — solo mensajes Outlook
- Exchange — **No encontrado**
- Sync bidireccional real en Salesforce/Gmail/Outlook/Stripe

### UI

| Ruta | Controller/View |
|------|-----------------|
| `/Integrations` | `Integrations.cshtml` + `IntegrationsModel` |
| Sidebar | `_FlowSidebar.cshtml` → `Nav_Integrations` |

---

## FASE 4 — Exportación

### Formatos

| Formato | Módulos | Implementación |
|---------|---------|----------------|
| **JSON** | Leads, Customers, Users, Policies, Audit | JavaScript client-side (página filtrada) |
| **CSV** | Customers | `WarehouseExportService.ExportCustomersCsvAsync` → `GET /api/data/warehouse/export/customers.csv` |
| **HTML** | Executive, Board | `ExecutiveModel.OnGetExportAsync` |
| **PDF** | **No** | — |
| **Excel** | **No** | — |

### Módulos exportables (evidencia)

| Módulo | Export | Mecanismo |
|--------|--------|-----------|
| Customers | JSON (UI) + CSV (API) | Client blob / `WarehouseExportService` |
| Leads | JSON | `Leads.cshtml` → `exportLeads()` |
| Users | JSON | `Users.cshtml` → `exportUsers()` |
| Policies | JSON | `Policies.cshtml` → `exportPolicies()` |
| Audit / DomainEvents | JSON | `Audit.cshtml.cs` → `OnPostExportAsync` (max 10 000) |
| Executive dashboard | HTML | `/Executive?handler=Export` |
| Deals | **No** export dedicado | — |
| Workflows | **No** | — |
| Settings config | JSON | `Settings.cshtml.cs` → `OnPostExportConfigAsync` |

### ExportController / ExportService

**No existen** como clases dedicadas. Exportación dispersa en páginas y `DataPlatformController`.

---

## FASE 5 — Procesamiento masivo

### Infraestructura

| Tecnología | Uso para import | Evidencia |
|------------|-----------------|-----------|
| **RabbitMQ** | Eventos dominio / agentes | `Worker.cs`, `EventBus:Provider: RabbitMQ` en VPS |
| **BackgroundService** | Agentes AI, no imports | `Worker.cs`, `BusinessMemoryConsolidationWorker.cs` |
| **Hangfire** | **No** | Sin referencias |
| **Cola import** | **No** | Sin `ImportJob`, `ImportHistory` |

### Límites documentados en código

| Límite | Valor | Archivo |
|--------|-------|---------|
| Max filas import | **5 000** | `ImportGuard.MaxRows` |
| Max tamaño archivo | **5 MB** | `ImportGuard.MaxFileBytes` |
| HubSpot pull | **100** contacts | `HubSpotConnector` |
| Salesforce pull | **100** contacts | `SalesforceConnector` |
| Stripe pull | **100** customers | `StripeDataConnector` |
| Gmail messages | **50** | `GmailConnector` |
| Outlook messages | **50** | `OutlookConnector` |
| HubSpot push | **20** | `HubSpotConnector` |
| Audit export | **10 000** events | `Audit.cshtml.cs` |

### Capacidad estimada (evidencia, no benchmark)

| Volumen | ¿Puede importar? | Motivo |
|---------|------------------|--------|
| 1 000 | **Sí** (dentro de límite) | Síncrono ~minutos; 1 SaveChanges por fila |
| 10 000 | **No** | `ImportGuard.MaxRows = 5000` rechaza |
| 100 000 | **No** | Sin cola, sin batch DB |
| 1 000 000 | **No** | Imposible con arquitectura actual |

**Patrón actual:** `foreach (row) { await CreateCommand; }` — N round-trips, sin bulk insert, sin transacción por lote.

---

## FASE 6 — Data Quality

| Capacidad | Existe | Evidencia |
|-----------|--------|-----------|
| Deduplicación (email) | Sí | `IdentityResolutionService.FindDuplicatesByEmailAsync` |
| Merge customers | Sí | `IdentityMergeService.MergeDuplicatesAsync` |
| Merge leads | **No** | — |
| Validación email | Sí | `DataQualityGuardian.IsValidEmail` (regex) |
| Validación teléfono | Sí | `DataQualityGuardian.IsValidPhone` |
| Detección duplicados UI | Sí | `/Customer360` muestra `Duplicates` |
| Tareas DQ automáticas | Sí | `DataQualityRevenueService.ScanAndCreateTasksAsync` |
| Agente autónomo DQ | Parcial | `DataQualityGuardian` — escanea pero `TODO: correcciones` |

### APIs calidad

| Endpoint | Función |
|----------|---------|
| `GET /api/data/identity/duplicates` | Listar duplicados por email |
| `POST /api/data/identity/merge` | Fusionar duplicados |
| Ingest dedup | `DataAcquisitionService` — dedup por email en batch |

---

## FASE 7 — Base de datos

### Tablas relacionadas con Data Hub

| Tabla | Columnas clave | Uso |
|-------|----------------|-----|
| `TenantIntegrations` | Provider, AccessToken, RefreshToken, Settings (jsonb), LastSyncAt, LastSyncStatus | Conexiones OAuth/API |
| `CdpStreamEvents` | TenantId, EventType, CustomerId, Payload (jsonb), OccurredAt | Stream CDP post-merge/ingest |
| `FailedEventMessages` | TenantId, EventType, Payload, FailedAt, RetryCount | Reintentos event bus (no imports) |
| `DomainEvents` | Event store general | Audit trail |
| `Customers`, `Leads`, `Deals` | Entidades CRM | Destino imports |
| `BusinessMemory*` (8 tablas) | Episodios AI | No importación masiva |

### Tablas NO encontradas

- `ImportJobs`, `ImportBatches`, `ImportErrors`
- `ExportJobs`, `Uploads`, `SyncLogs`
- `DataMigrationRuns`, `FieldMappings`

---

## FASE 8 — UX/UI

### Importar

| Ruta | Tipo | Progreso | Errores | Historial |
|------|------|----------|---------|-----------|
| `/Customers` (modal) | CSV/JSON | No | Flash redirect | No |
| `/Leads` (modal) | CSV/JSON | No | TempData | No |
| `/Deals/Import` | CSV/JSON | No | Redirect | No |
| `/Users/Import` | JSON/CSV | No | Redirect | No |
| `/Policies/Import` | JSON | No | Redirect | No |
| `/Workflows/Import` | JSON | No | Redirect | No |
| `/Settings` | Config JSON | No | TempData | No |
| `/Integrations` | Sync providers | No | Message/Error | LastSyncAt only |

### Exportar

| Ruta | Formato |
|------|---------|
| Customers, Leads, Users, Policies | JSON (client) |
| `/api/data/warehouse/export/customers.csv` | CSV |
| `/Audit` | JSON |
| `/Executive?handler=Export` | HTML |
| `/Settings` handler ExportConfig | JSON |

### Data Hub unificado

| Elemento | Existe |
|----------|--------|
| Menú "Data Hub" | **No** |
| `/Customer360` | Vista 360 + duplicados (no import) |
| `/Integrations` | Sync externo |
| Wizard migración | **No** |
| Dashboard progreso import | **No** |

---

## FASE 9 — Brecha vs Salesforce / HubSpot / Dynamics

| Capacidad enterprise | Salesforce | HubSpot | Dynamics | AutonomusCRM |
|---------------------|------------|---------|----------|--------------|
| Import wizard UI | Sí | Sí | Sí | **No** |
| Excel/CSV mapping | Sí | Sí | Sí | CSV fijo |
| Upsert / dedup rules | Sí | Sí | Sí | Solo merge post-hoc |
| Async bulk API | Sí | Sí | Sí | **No** |
| Job monitoring | Sí | Sí | Sí | **No** |
| Migration from competitor | Sí | Sí | Sí | Pull 100 rows |
| Scheduled sync | Sí | Sí | Sí | Manual POST |
| Data quality rules engine | Sí | Ops Hub | DQS | Agentes básicos |
| Export all objects | Sí | Sí | Sí | 4-5 módulos parcial |
| Field-level security on import | Sí | Sí | Sí | Auth global |

---

## FASE 10 — Prioridad

**Prioridad: ALTA (P1 comercial)**

Razones:
1. Clientes enterprise exigen migración desde CRM legacy
2. Competidores ofrecen import wizard como onboarding Day 1
3. Límite 5 000 filas bloquea adopción mid-market
4. Piezas existentes (`ICrmImportService`, conectores) son base reutilizable

---

## Conclusión final

### ¿AutonomusCRM ya posee un Data Hub empresarial?

**No.** Posee **building blocks**:
- Import CSV/JSON básico (customers, leads, deals)
- Conectores sync limitados (HubSpot, Salesforce, Gmail, Outlook, Stripe)
- Export CSV customers + JSON client-side
- Identity resolution / merge por email
- Ingest API con API key

### ¿Debe construirse?

**Sí**, como módulo producto unificado **Autonomus Data Hub**.

---

## Hoja de ruta propuesta

### Fase 1 — Foundation (8–12 semanas) | Complejidad: Media | Impacto: Alto

| Entrega | Esfuerzo | Detalle |
|---------|---------|---------|
| `ImportJob` entity + tabla | 1 sem | Estado, progreso, errores por fila |
| Cola RabbitMQ `import.jobs` | 1 sem | Worker procesa lotes de 500 |
| Parser CSV robusto (CsvHelper) | 3 días | Escaping, encoding |
| UI `/DataHub` — upload + historial | 2 sem | Progreso, errores descargables |
| Upsert por email (customers/leads) | 1 sem | Evitar duplicados en import |
| Elevar límite a 100k con async | 1 sem | Job-based |

**Impacto comercial:** Desbloquea migraciones reales hasta 100k registros.

### Fase 2 — Migration Connectors (10–14 semanas) | Complejidad: Alta | Impacto: Muy alto

| Entrega | Esfuerzo | Detalle |
|---------|---------|---------|
| HubSpot full sync (paginación) | 2 sem | Contacts, companies, deals |
| Salesforce bulk API 2.0 | 3 sem | Jobs async SF → CRM |
| Import wizard con field mapping | 3 sem | UI mapeo columnas |
| Excel/XLSX (ClosedXML) | 1 sem | Segundo formato enterprise |
| Zoho / Pipedrive connectors | 4 sem | Mercado LATAM/EU |

**Impacto comercial:** Paridad mínima con HubSpot inbound migration.

### Fase 3 — Sync & Quality Platform (8–10 semanas) | Complejidad: Alta | Impacto: Alto

| Entrega | Esfuerzo | Detalle |
|---------|---------|---------|
| Scheduled sync worker | 1 sem | Cron por tenant |
| `SyncLog` + conflict resolution UI | 2 sem | Basado en `SyncConflictService` |
| Rules engine DQ configurable | 3 sem | Extender `DataQualityGuardian` |
| Export universal CSV/Excel | 2 sem | Todos los módulos CRM |
| Google Contacts / M365 Contacts | 2 sem | APIs nativas contactos |

**Impacto comercial:** Retención datos limpios + integración continua.

### Fase 4 — Enterprise Scale (12–16 semanas) | Complejidad: Muy alta | Impacto: Enterprise

| Entrega | Esfuerzo | Detalle |
|---------|---------|---------|
| Bulk PostgreSQL COPY / staging tables | 3 sem | 1M+ filas |
| Dynamics / SAP connector (partner) | 6+ sem | Según demanda |
| PDF export reports | 2 sem | Executive, audit |
| Data lineage + GDPR export | 2 sem | Compliance |
| Multi-tenant import quotas | 1 sem | Plan limits |

**Impacto comercial:** Certificación enterprise, RFPs gobierno/gran cuenta.

---

## Inventario de archivos clave (referencia)

```
AutonomusCRM.API/Controllers/ImportController.cs
AutonomusCRM.API/Controllers/DataPlatformController.cs
AutonomusCRM.API/Controllers/IntegrationsController.cs
AutonomusCRM.Infrastructure/Imports/CrmImportService.cs
AutonomusCRM.Application/Common/Imports/ImportGuard.cs
AutonomusCRM.Infrastructure/Integrations/IntegrationConnectorBase.cs
AutonomusCRM.Infrastructure/Integrations/HubSpotConnector.cs
AutonomusCRM.Infrastructure/Integrations/SalesforceConnector.cs
AutonomusCRM.Infrastructure/DataPlatform/IdentityMergeService.cs
AutonomusCRM.Infrastructure/DataPlatform/WarehouseExportService.cs
AutonomusCRM.Workers/Agents/DataQualityGuardian.cs
AutonomusCRM.Infrastructure/Revenue/DataQualityRevenueService.cs
```

---

*Auditoría basada en código en rama `main` post-commit `8287b24`. Sin modificaciones al repositorio.*
