# 04 — INITIAL TEST DATA

Datos creados por `02_CLEAN_TEST_DATABASE_SCRIPT.sql` (sin CRM operativo aun).

---

## Tenant

| Campo | Valor |
|-------|-------|
| Nombre | TechSolutions Panama |
| Trial | 14 dias |
| Settings | trust.approvalThreshold=70, ai.testMode=true, communications.allowSimulation=true |

---

## Billing

| Campo | Valor |
|-------|-------|
| Plan | starter |
| MaxUsers | 10 |
| Status | trialing |

---

## Workflows

| Nombre | Activo | Trigger | Accion |
|--------|--------|---------|--------|
| Auto-asignar lead nuevo | Si | LeadCreatedEvent | Assign → sales1 |
| Tarea seguimiento deal | Si | DealStageUpdatedEvent | CreateTask |
| Workflow inactivo — email campana | No | LeadCreatedEvent | UpdateStatus |
| Workflow inactivo — churn | No | CustomerUpdatedEvent | CreateTask |

---

## Policy

| Nombre | Expresion | Estado |
|--------|-----------|--------|
| Base Test Policy | `role in (Admin, Manager, Sales)` | Activa |

---

## Configuracion modo prueba

| Area | Modo prueba |
|------|-------------|
| Email | `Communications:AllowSimulation=true`, provider Log |
| IA | `AI__Enabled=false` en VPS test env |
| Billing | Plan starter, sin Stripe obligatorio |
| Trust | Umbral 70, audits en script 05 |
