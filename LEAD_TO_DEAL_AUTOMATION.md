# LEAD_TO_DEAL_AUTOMATION

## Evento
`Lead.Qualified` → `OperationalAutomationService.OnLeadQualifiedAsync`

## Flujo automático
1. Cliente por email existente o `CreateCustomerCommand`
2. Deal borrador `Oportunidad: {nombre}` amount=1, metadata `LeadId`, `IsDraft`
3. Asignación al owner del lead si existe
4. Tarea seguimiento **High**, due +1 día, tipo `FollowUp`

## Disparador
`QualifyLeadCommandHandler` ya publicaba eventos; automation se engancha en `DomainEventDispatcher`.

## Idempotencia
- No duplica deal si metadata `LeadId` ya existe
- No duplica tarea `FollowUp` abierta
