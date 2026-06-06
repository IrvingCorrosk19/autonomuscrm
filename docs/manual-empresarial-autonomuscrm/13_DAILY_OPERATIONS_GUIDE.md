# 13 — Guía de Operaciones Diarias

## Para Sales (`sales@autonomuscrm.local`)

### Inicio de jornada (20 min)

| Hora | Acción | Ruta |
|------|--------|------|
| 0-5 min | Login → Revenue OS | `/revenue` |
| 5-10 min | Revisar tareas vencidas | `/Tasks?overdueOnly=true` |
| 10-15 min | Leads nuevos (estado New) | `/Leads?status=0` |
| 15-20 min | Deals en Negotiation/Proposal | `/Deals` kanban |

### Durante el día

| Evento comercial | Acción en CRM |
|------------------|---------------|
| Llamada completada | Completar tarea relacionada; actualizar lead a Contacted si aplica |
| Email enviado | Registrar en notas (Details) si módulo disponible |
| Reunión agendada | Actualizar Expected Close Date en deal |
| Propuesta enviada | Mover deal a **Proposal** |
| Objeción precio | Mover a **Negotiation**, ajustar probabilidad |
| Cierre verbal | **Close** deal → verificar tareas onboarding |

### Fin de jornada (15 min)

1. `/Tasks` — cero overdue propios si es posible
2. `/Leads` — ningún lead New sin contacto >24h
3. `/Deals` — actualizar etapas movidas hoy
4. Command `/` — revisar 1 decisión o insight relevante

---

## Para Manager

| Frecuencia | Acción |
|------------|--------|
| Diario | Trust Studio pendientes |
| Diario | Executive OS |
| Semanal | Audit export, Users roles |
| Semanal | Revisar win rate y forecast en Deals |

---

## Para Support

| Frecuencia | Acción |
|------------|--------|
| Diario | Customer Success tickets |
| Diario | Customer 360 búsqueda duplicados |
| Según caso | Escalar a Sales si oportunidad detectada |

---

## Indicadores de alerta (del sistema)

| Señal | Dónde verla | Acción |
|-------|-------------|--------|
| Tarea SLA lead | `/Tasks` | Contactar lead |
| Deal estancado | Revenue OS / tarea auto | Reactivar deal |
| Risk score >70 | `/Customers` | Coordinar con CS |
| Score lead <50 | `/Leads` | Calificar o descartar |
| Failed Events | `/FailedEvents` | Escalar a Admin |
