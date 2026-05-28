# TASK_MODULE_IMPLEMENTATION

## Entregable
Módulo operativo de tareas sobre `WorkflowTask`.

## Capacidades
- Listar (`/Tasks`, `GET api/tasks`)
- Completar (`OnPostComplete`, `POST api/tasks/{id}/complete`)
- Asignar (`OnPostAssign`, `POST api/tasks/{id}/assign`)
- Filtrar: status, priority, overdue
- **DueDate**, **Priority**, **TaskType**, **IsOverdue**

## Modelo
`AutonomusCRM.Application/Automation/Workflows/WorkflowTask.cs` — campos `DueDate`, `Priority`, `TaskType`

## Repositorio
`IWorkflowTaskRepository.GetByTenantAsync`, `ExistsOpenTaskAsync`

## Servicio sistema
`IOperationalTaskService` — tareas con `OperationalConstants.SystemWorkflowId`
