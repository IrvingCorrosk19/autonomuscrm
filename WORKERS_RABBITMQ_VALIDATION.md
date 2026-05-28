# WORKERS_RABBITMQ_VALIDATION

**Fecha:** 2026-05-27

---

## Estado infraestructura local

| Componente | Estado sesión |
|------------|---------------|
| Docker Desktop | **NO disponible** (pipe no encontrado) |
| RabbitMQ | **NO validado en runtime** |
| AutonomusCRM.Workers | Build OK, no ejecutado contra broker real |

---

## Fixes implementados (pre-requisito validación)

### 1. Routing RabbitMQ (crítico)

**Problema:** Publish usaba `EventType` (`Lead.Created`) pero Subscribe enlazaba cola a `LeadCreatedEvent` → **workers nunca recibían eventos**.

**Fix:** `DomainEventRouting.cs` — routing key unificado desde registro de tipos.

Archivos: `RabbitMQEventBus.cs`, `DomainEventTypeRegistry.cs`

### 2. Deserialización en consumer

Consumer usa `DomainEventTypeRegistry.TryDeserialize` antes de fallback `JsonSerializer.Deserialize<T>`.

---

## Procedimiento validación (cuando Docker esté activo)

```bash
docker compose up -d postgres rabbitmq
# API: EventBus__Provider=RabbitMQ, RabbitMQ__HostName=localhost
dotnet run --project AutonomusCRM.Workers
# Crear lead en UI → ver logs LeadIntelligenceAgent
```

Agentes suscritos (`Worker.cs`):

- LeadIntelligenceAgent → LeadCreatedEvent
- CustomerRiskAgent → CustomerCreatedEvent
- DealStrategyAgent → DealCreatedEvent, DealStageChangedEvent
- CommunicationAgent → Customer/Lead created
- ComplianceSecurityAgent → IDomainEvent (todos)

---

## Conclusión

| Veredicto | Detalle |
|-----------|---------|
| Código readiness | **MEJORADO** (routing fix) |
| Runtime validation | **BLOCKED** — requiere Docker + RabbitMQ |
| Riesgo operacional | Sin Worker real, agentes IA son **no operativos** (comunicar en UI) |
