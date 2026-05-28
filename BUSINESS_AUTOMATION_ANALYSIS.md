# BUSINESS_AUTOMATION_ANALYSIS

## Pregunta de negocio
**¿Qué tareas siguen siendo manuales?**

Casi todas las que generan ingresos: seguimiento, comunicación, escalamiento, renovación, y la mayoría de transiciones entre entidades.

---

## Motor de workflows — capacidad real

### Triggers implementados
- **`DomainEvent`** + `EventType` exacto (ej. `Lead.Created`)

### Triggers en UI NO implementados
- StateChange, Webhook, Schedule, BusinessRule, Threshold, Prediction

### Condiciones implementadas
- **`EventTypeEquals`** únicamente

### Acciones implementadas
| Acción | Efecto |
|--------|--------|
| Assign | Asigna lead/deal a usuario |
| UpdateStatus | Cambia estado lead/customer o etapa deal |
| CreateTask | Persiste `WorkflowTask` |
| Communicate | **Log** |
| ActivateAgent | **Log** |

---

## Eventos de dominio vs automatización

| Evento | Workflow (si config) | Agente |
|--------|---------------------|--------|
| Lead.Created | Sí | Lead Intelligence, Communication (stub) |
| Lead.Qualified | Solo si config | — |
| Lead.ConvertedToCustomer | Solo si config | — |
| Customer.Created | Sí | Customer Risk |
| Deal.Created / StageChanged | Sí | Deal Strategy |
| Deal.Closed / Lost | Solo si config | — |
| Customer.PurchaseRecorded | Solo si config | — |

**Cobertura autónoma:** ~4 eventos de 20+ tipos registrados.

---

## Tareas aún manuales (lista operativa)

1. Calificar lead y decidir siguiente paso
2. Crear deal desde lead ganado
3. Convertir lead a cliente (formulario explícito)
4. Enviar propuesta / email / WhatsApp
5. Registrar seguimiento y próxima acción
6. Cerrar o perder deal con análisis
7. Onboarding cliente post-venta
8. Detectar churn y actuar
9. Renovar contrato
10. Configurar workflows con parámetros correctos (JSON técnico)

---

## Automatizaciones de alto valor no implementadas

| Automatización | Impacto comercial |
|----------------|-------------------|
| Lead score > X → asignar top rep | + conversión |
| 48h sin contacto → tarea + alerta | + velocidad pipeline |
| Deal estancado → escalamiento | + win rate |
| ClosedWon → CS playbook | + retención |
| Risk > 70 → alerta CS | - churn |
| Compra registrada → actualizar LTV | + upsell precision |

---

## Policy & Decision engines

- **PolicyEngine:** expresiones **siempre pasan** — no bloquea operaciones.
- **DecisionEngine:** reglas básicas; **no invocado** por workflows ni agentes.

**Riesgo de negocio:** compliance y políticas comerciales son **cosméticas**.

---

## Bulk operations — agujero operativo

`BulkUpdateLeadStatus` / `BulkUpdateDealStage` **no publican eventos** → workflows y agentes **no se ejecutan**.

Gerentes que hacen limpieza masiva **desconectan** la automatización sin saberlo.

---

## Recomendación estratégica

**Fase 12:** cerrar brecha UI↔motor (parámetros de acciones) + 5 playbooks predefinidos por industria B2B, antes de más tipos de triggers.
