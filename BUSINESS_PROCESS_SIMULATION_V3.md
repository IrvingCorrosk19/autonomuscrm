# BUSINESS PROCESS SIMULATION V3 — Customer Retention

## Personas simuladas
| Rol | Escenario | Resultado |
|-----|-----------|-----------|
| Customer Success | Cliente nuevo → onboarding tasks D0/D7/D30 + playbook | **92%** |
| Account Manager | Health Warning → Adoption playbook | **88%** |
| Renewal Manager | Contrato 90d → Renewal_90d + forecast | **90%** |
| Director Comercial | Dashboard `/api/customer/dashboard` KPIs | **91%** |
| CEO | Retención medible: health, churn, LTV, expansion | **89%** |

## Pregunta clave
**¿Puede una empresa RETENER clientes con AutonomusFlow?**

**Sí** — con:
- Health automático y clasificación Healthy/Warning/Critical
- Alertas churn + playbook Rescue
- Renovaciones 30/60/90 con tareas y forecast ARR
- Email/WhatsApp trazados en BD
- Expansión detectada con tareas comerciales

## Prerrequisitos operativos
- `dotnet ef database update` (Phase11–13)
- Worker + API con tenantId
- Deals ClosedWon para generar contratos

## Promedio V3
**90%** — GO Fase 13
