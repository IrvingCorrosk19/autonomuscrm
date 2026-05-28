# WORKFLOW_ENGINE_ALIGNMENT

## Motor
Solo ejecuta: trigger `DomainEvent`, condición `EventTypeEquals`, acciones `Assign`, `UpdateStatus`, `CreateTask`.

## Validación al crear (handlers)
- `AddWorkflowTriggerCommandHandler` — rechaza tipos ≠ DomainEvent
- `AddWorkflowConditionCommandHandler` — fuerza EventTypeEquals
- `AddWorkflowActionCommandHandler` — rechaza Communicate / ActivateAgent

## UI Edit
- Triggers: solo DomainEvent
- Condiciones: campo event type (expresión)
- Acciones: parámetros userId, status, title, dueDate, priority

## CreateTask en motor
Lee `dueDate`, `priority`, `taskType` desde `action.Parameters`
