# BULK_EVENTS_VALIDATION

## Handlers corregidos
- `BulkUpdateLeadStatusCommandHandler`
- `BulkUpdateDealStageCommandHandler`

## Comportamiento
1. Acumula `IDomainEvent` por entidad actualizada
2. `SaveChangesAsync`
3. `IDomainEventDispatcher.DispatchAsync(pendingEvents)`

## Efecto
Bulk dispara **workflows**, **operational automation** y **agentes RabbitMQ** igual que operaciones unitarias.

## Validación manual
1. Bulk cambiar etapa de 2+ deals
2. Verificar logs WorkflowEngine / DealStrategyAgent
3. Verificar Event Store entradas nuevas
