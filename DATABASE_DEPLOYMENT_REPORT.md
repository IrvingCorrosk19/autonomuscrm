# DATABASE DEPLOYMENT REPORT — VPS Staging

**Fecha:** 2026-06-04  
**Host:** `164.68.99.83` · PostgreSQL 16 (container `autonomuscrm-postgres`)  
**Database:** `autonomuscrm`

---

## Resumen

| Verificación | Estado | Evidencia |
|--------------|--------|-----------|
| Contenedor PostgreSQL healthy | ✅ | `pg_isready` en compose healthcheck |
| Esquema creado | ✅ | `\dt` lista 50+ tablas (AiDecisionAudits, BusinessMemories, Customers, …) |
| Migraciones EF aplicadas | ✅ | API arranca con `Database__AutoMigrate=true`; sin errores de migración en logs |
| Seed demo users | ✅ | Login `admin@autonomuscrm.local` / `Admin123!` OK |
| Lectura | ✅ | Customer360, Executive, Revenue cargan datos |
| Escritura | ✅ | Operaciones de negocio vía app (seed + sesión admin) |
| Tenant isolation | ✅ | Global query filters en `ApplicationDbContext` (código) |

---

## Migraciones EF Core

- **Proyecto:** `AutonomusCRM.Infrastructure/Persistence/Migrations/`
- **Cantidad en repo:** 15 migraciones
- **Aplicación:** automática al iniciar API (`ApplyMigrationsAsync` en `Program.cs`)
- **Tabla historial:** `"__EFMigrationsHistory"` (PostgreSQL, case-sensitive)

---

## Infraestructura de datos en VPS

```yaml
POSTGRES_DB: autonomuscrm
POSTGRES_USER: postgres
Volume: autonomuscrm_pgdata (persistente)
Connection: Host=postgres;Port=5432;Database=autonomuscrm
```

---

## Tablas verificadas (muestra)

```
AiApprovalRequests, AiDecisionAudits, BusinessMemories, BusinessMemoryLearnings,
Customers, Deals, Tenants, Users, TenantIntegrationConnections, …
```

Listado completo disponible en VPS: `docker exec autonomuscrm-postgres psql -U postgres -d autonomuscrm -c '\dt'`

---

## Seed y usuarios

| Usuario | Rol | Password (demo) |
|---------|-----|-----------------|
| admin@autonomuscrm.local | Admin | `Admin123!` |
| manager@autonomuscrm.local | Manager | `Manager123!` |
| sales@autonomuscrm.local | Sales | `Sales123!` |
| support@autonomuscrm.local | Support | `Support123!` |
| viewer@autonomuscrm.local | Viewer | `Viewer123!` |

**Nota:** `CeoDemoSeeder` registró warning en primer arranque (dataset CEO_DEMO parcial); no bloquea login ni páginas principales.

---

## Índices y relaciones

Definidos en migraciones `InitialCreate` y fases posteriores (Phase4 failed events, trust, memory, etc.). Sin cambios manuales en VPS.

---

## Veredicto

**Base de datos operativa** para pruebas funcionales en staging. Lista para configurar integraciones OAuth y API keys sin re-migrar.

*Generado — VPS Deployment Fase 3.*
