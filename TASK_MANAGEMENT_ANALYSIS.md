# TASK_MANAGEMENT_ANALYSIS

## Pregunta crítica
**¿Qué sucede si un vendedor no da seguimiento?**

**Respuesta hoy: Nada en el sistema.** No hay escalamiento, SLA, ni alerta automática al gerente.

---

## Estado de tareas en AutonomusFlow

### Entidad: `WorkflowTask`
- Campos: título, descripción, assignee, entidad relacionada (Lead/Deal/Customer), status string (`Open` / `Completed`).
- **Creación:** solo acción `CreateTask` del `WorkflowEngine` cuando un workflow está bien configurado.
- **Consulta:** `GetOpenByTenantAsync` en repositorio — **sin página, API ni comando de completar**.
- **Recordatorios:** no existen.
- **Prioridades:** no existen.
- **Vencimientos:** no existen (`DueDate` ausente).

---

## Procesos de seguimiento

| Capacidad | Estado |
|-----------|--------|
| Tarea manual del vendedor | **No** |
| Tarea automática por evento | Parcial (workflow) |
| Recordatorio email/push | **No** |
| Cadencia de contactos | **No** |
| Tareas vencidas visibles | **No** |
| Reasignación por ausencia | **No** |
| Tarea desde DealStrategyAgent | **No** (solo texto en metadata) |

---

## Dependencia del usuario

El seguimiento comercial depende de:
1. Disciplina del vendedor.
2. Herramientas externas (calendario, WhatsApp, email).
3. Configuración manual de workflows (con parámetros que la UI no captura bien).

---

## Impacto en ingresos

Sin gestión de tareas:
- Leads calificados **envejecen** sin alerta.
- Deals estancados **no generan tarea** aunque el agente detecte riesgo.
- Customer Success **no tiene cola** de acciones post-venta.

**Costo estimado de negocio:** 15–30% de oportunidades no trabajadas en CRMs sin cadencias (benchmark industria SMB B2B).

---

## Gaps priorizados

| Prioridad | Mejora de negocio |
|-----------|-------------------|
| P0 | Módulo **Tareas**: listar abiertas, completar, asignar, filtrar por vencimiento |
| P0 | Campo `DueDate` + overdue en dashboard |
| P1 | Workflow: `Lead.Qualified` → CreateTask “Contactar en 24h” |
| P1 | Deal estancado > N días → CreateTask + notificación |
| P2 | Cadencias predefinidas (secuencia de tareas) |
| P2 | Integración calendario |

---

## Conexión con otros módulos

```
DealStrategyAgent (detecta riesgo) ──X──► WorkflowTask
LeadIntelligence (score alto)      ──X──► asignación + tarea
CommunicationAgent                 ──X──► seguimiento
```

La automatización **detecta** pero **no obliga acción**.
