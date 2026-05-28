# DEFECTOS_FASE4

## DEF-F4-001 (CLOSED)

- **Título:** Global query filter bloqueaba login (users invisibles sin tenant)
- **Severidad:** Crítica
- **Fix:** `LoginCommandHandler` + `Login.cshtml.cs` establecen `ICurrentTenantAccessor`; bypass en listado tenants
- **Estado:** CLOSED — P0 19/19 PASS

## DEF-F4-002 (OPEN)

- **Título:** OTel exporter NuGet advisory GHSA-4625-4j76-fww9
- **Severidad:** Media
- **Fix:** Actualizar paquete cuando parche disponible

## DEF-F4-003 (OPEN)

- **Título:** Load test 100+ usuarios no ejecutado en CI
- **Severidad:** Media operacional
- **Estado:** OPEN

## DEF-F4-004 (OPEN)

- **Título:** RabbitMQ resilient sin soak 24h
- **Severidad:** Alta para IA producción
- **Estado:** OPEN
