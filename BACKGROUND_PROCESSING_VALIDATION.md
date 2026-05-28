# BACKGROUND_PROCESSING_VALIDATION

## Cambios Fase 4

- `Worker.cs`: scope DI por mensaje + `ICurrentTenantAccessor` desde evento
- `ResilientRabbitMQEventBus` reemplaza `RabbitMQEventBus` en DI
- OpenTelemetry en Workers

## Agentes

| Agente | Eventos |
|--------|---------|
| LeadIntelligenceAgent | LeadCreated |
| CustomerRiskAgent | CustomerCreated |
| DealStrategyAgent | DealCreated, DealStageChanged |
| CommunicationAgent | Customer/Lead Created |
| ComplianceSecurityAgent | IDomainEvent |

## Validación

| Check | Estado |
|-------|--------|
| Build Workers | OK |
| Runtime con RabbitMQ | **Pendiente** Docker |
| Event replay UI | Roadmap |
| Scheduled scans | TODO en Worker loop |

## No pérdida de eventos

- Mensajes persistentes
- DLQ + `FailedEventMessages`
- Idempotencia consumer
