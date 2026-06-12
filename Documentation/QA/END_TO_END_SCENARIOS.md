# END_TO_END_SCENARIOS — AutonomusCRM QA

**Entorno:** http://164.68.99.83:8091 | **Password:** `AutonomusTest123!`

---

## Escenario E2E-01 — Lead → Customer → Deal → Won

| Campo | Valor |
|-------|-------|
| **Roles** | Sales (`sales1@`) o Admin |
| **Duración** | ~15 min |
| **Datos** | Nuevo lead o seed `Lead Calificado VIP` |

### Pasos
1. Login → `/revenue`
2. `/Leads/Create` — crear lead *E2E Pipeline QA*
3. `/Leads/Details/{id}` — Qualify → Convert to Customer
4. `/Customers/Details/{id}` — modal Create Deal — *E2E Deal QA* $25,000
5. `/Deals/Details/{id}` — Update Stage → Negociación → Close Won
6. Verificar en `/Deals` status Won
7. `/Audit` — eventos DomainEvents registrados

### Criterio éxito
- Sin 500/404
- Entidades visibles en listados
- Deal en stage cerrado ganado
- Audit con actividad

---

## Escenario E2E-02 — Workflow → Approval → Audit

| Campo | Valor |
|-------|-------|
| **Roles** | Admin + Manager |
| **Duración** | ~20 min |

### Pasos
1. Admin: `/Workflows/Edit/b1000004-0000-4000-8000-000000000001`
2. Modal Add Action — CreateTask on LeadCreated
3. Sales: crear lead → verificar task auto-creada en `/Tasks`
4. Admin: `/TrustInbox` — aprobar/rechazar audit si AI generó decisión
5. `/Audit` — export CSV — verificar eventos workflow + trust

### Criterio éxito
- Workflow persiste trigger/action
- Task aparece post-lead (si event bus activo)
- Trust action registrada
- Audit export OK

---

## Escenario E2E-03 — Customer360 → Support → Resolution

| Campo | Valor |
|-------|-------|
| **Roles** | Support (`support@`) |
| **Duración** | ~10 min |
| **Datos** | Cliente seed *Logistica Express SA* |

### Pasos
1. Login → `/Customer360`
2. Buscar *Logistica* → abrir `/customers/c1000001-...002/360`
3. `/customer-success` — CreateTicket *Onboarding E2E*
4. `/Tasks` — verificar ticket CS_Ticket
5. CloseTicket en customer-success
6. Intentar `/Leads/Create` → **AccessDenied** (validación RBAC)

### Criterio éxito
- 360 carga con datos cliente
- Ticket lifecycle completo
- Write comercial bloqueado

---

## Escenario E2E-04 — Executive → Revenue → Decision

| Campo | Valor |
|-------|-------|
| **Roles** | Manager (`manager@`) |
| **Duración** | ~10 min |

### Pasos
1. Login → `/executive`
2. Revisar KPIs con datos seed (5 deals, pipeline)
3. `/executive?handler=Export&type=executive` — descargar HTML
4. `/revenue` — revisar revenue leak / health scores
5. Click insight CTA → `/FlowActions` CreatePlan o CreateTask
6. `/command/decisions` — ver historial decisiones

### Criterio éxito
- Dashboards con datos (no empty state crítico)
- Export descarga
- FlowAction crea entidad

---

## Escenario E2E-05 — Billing → Audit → Reporting

| Campo | Valor |
|-------|-------|
| **Roles** | Admin (`admin@`) |
| **Duración** | ~10 min |

### Pasos
1. `/billing` — ver plan starter, usage users/customers vs limits
2. Verificar ProductUsageEvents seed en BD (2 eventos billing)
3. `/Audit` — filtrar actividad del día
4. Modal detalle evento — JSON válido
5. POST Export audit
6. (Opcional API) `POST /api/billing/checkout` con Stripe configurado

### Criterio éxito
- Billing UI read-only OK
- Audit filtra y exporta
- Sin errores 500

---

## Escenario E2E-06 — Multi-role RBAC validation

| Paso | Rol | Acción | Esperado |
|------|-----|--------|----------|
| 1 | Viewer | `/Leads/Create` | AccessDenied |
| 2 | Support | `/Deals/Create` | AccessDenied |
| 3 | Sales | `/Users` | Denied |
| 4 | Manager | `/Users/Create` | OK |
| 5 | Admin | `/Settings` | OK |
| 6 | SuperAdmin | `POST /api/users` | 201 |

Automatizable: `.\tests\e2e\run-vps-test-qa.ps1`
