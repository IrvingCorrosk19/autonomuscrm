# 14 — Guía del Manager

**Rol:** Manager (`manager@autonomuscrm.local`)  
**Home:** `/executive`

---

## Responsabilidades en AutonomusCRM

1. Supervisar pipeline y forecast (`/Deals`, `/executive`)
2. Gestionar usuarios y roles (`/Users`, `/Users/Roles`)
3. Configurar tenant (`/Settings`)
4. Aprobar decisiones IA (`/TrustInbox`)
5. Definir workflows y políticas (`/Workflows`, `/Policies`)
6. Revisar auditoría (`/Audit`)

---

## Ritual semanal

| Día | Acción |
|-----|--------|
| Lunes | Executive OS + forecast 30/60/90 |
| Miércoles | Trust Studio backlog |
| Viernes | Win rate, leads sin calificar, export Audit |

---

## Gestión de equipo Sales

- Verificar que usen **Qualify** estándar (no tres procesos distintos)
- Revisar tareas overdue por rep en `/Tasks`
- Asignar leads alto score (≥70) vía SmartAssignment automation
- No dar rol Admin a vendedores

---

## KPIs que el sistema calcula (no inventar otros sin API)

- Forecast ponderado Deals
- Win Rate
- Lead conversion % (Qualified/Total)
- Customer LTV y risk aggregates
- Revenue OS leak detection

---

## Escalamiento a Admin

- Integraciones OAuth
- Billing / plan limits
- Failed Events DLQ
- Deploy y backups VPS
