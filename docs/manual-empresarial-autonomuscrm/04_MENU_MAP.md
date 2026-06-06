# 04 — Mapa de Menús y Navegación

**Shell principal:** `Shared/_Layout.cshtml` + `Flow/_FlowSidebar.cshtml`  
**Búsqueda global:** Ctrl+K → `/api/flow/search`

---

## Menú lateral (19 ítems)

| # | Sección | Etiqueta (EN) | Ruta | Cuándo usarlo | Rol típico |
|---|---------|---------------|------|---------------|------------|
| 1 | Command | Command | `/` | Inicio operativo: decisiones IA, métricas flujo | Todos |
| 2 | Command | Trust Studio | `/TrustInbox` | Aprobar/rechazar decisiones IA (HITL) | Admin, Manager |
| 3 | Command | Workforce | `/Agents` | Ver agentes y decisiones recientes | Admin, Manager |
| 4 | Revenue | Revenue OS | `/revenue` | Dashboard ingresos, fugas, explicación grafo | **Sales**, Manager |
| 5 | Revenue | Executive | `/executive` | Vista ejecutiva consolidada | Admin, Manager |
| 6 | Revenue | Pipeline | `/Deals` | Kanban y lista de oportunidades | **Sales** |
| 7 | Customers | Directory | `/Customers` | Directorio de clientes paginado | Sales, Support |
| 8 | Customers | Customer 360 | `/Customer360` | Búsqueda y vista 360 | Support, Sales |
| 9 | Customers | Customer Success | `/customer-success` | Tickets, casos, playbooks CS | Support |
| 10 | Commerce | Leads | `/Leads` | Gestión de prospectos | **Sales** |
| 11 | Intelligence | Memory | `/Memory` | Memoria empresarial semántica | Admin, Manager |
| 12 | Operations | Tasks | `/Tasks` | Tareas de workflow y operativas | **Sales**, todos |
| 13 | Platform | Integrations | `/Integrations` | HubSpot, Salesforce, email, Stripe | Admin, Manager |
| 14 | Platform | Voice | `/VoiceCalls` | Registro de llamadas | Sales |
| 15 | Admin | Users | `/Users` | Usuarios del sistema | Admin, Manager |
| 16 | Admin | Policies | `/Policies` | Políticas de control ABAC | Admin, Manager |
| 17 | Admin | Audit | `/Audit` | Event sourcing / auditoría | Admin, Manager |
| 18 | Admin | Settings | `/Settings` | Perfil tenant, MFA, kill-switch | Admin, Manager |
| 19 | Admin | Billing | `/billing` | Suscripción y facturación | Admin |

---

## Rutas comerciales críticas (no en sidebar)

| Ruta | Propósito |
|------|-----------|
| `/Leads/Create`, `/Edit`, `/Details` | CRUD lead |
| `/Customers/Create`, `/Edit`, `/Details` | CRUD cliente |
| `/Deals/Create`, `/Edit`, `/Details` | CRUD deal |
| `/Workflows` | Automatizaciones configurables |
| `/command/decisions` | Historial de decisiones |
| `/command/outcomes` | Outcome Fabric |
| `/command/playbooks` | Playbooks autónomos |
| `/customers/{id}/360` | Vista 360 individual |
| `/FailedEvents` | DLQ de eventos fallidos |

---

## Marketing (público, sin login)

| Ruta | Propósito |
|------|-----------|
| `/landing` | Landing producto |
| `/roi` | Calculadora ROI |
| `/demo` | Guía demo CEO |
| `/stories` | Casos de impacto |
| `/pricing` | Precios |

---

## Errores comunes de navegación

| Error | Consecuencia | Solución |
|-------|--------------|----------|
| Sales intenta `/Users` | Access Denied | Pedir a Manager/Admin |
| Support intenta crear Lead | Redirect Access Denied | Usar solo lectura o escalar |
| Confundir `/` con `/revenue` | Sales pierde vista pipeline | Usar home post-login `/revenue` |
| No usar `/Tasks` | SLA y automatizaciones ignoradas | Revisar tareas diarias |

---

## Impacto en el negocio

- **Leads + Deals + Tasks** = ciclo diario del vendedor
- **Revenue OS** = priorización y forecast
- **Customer 360 / CS** = post-venta y retención
- **Trust Studio** = gobernanza de decisiones autónomas
- **Audit** = cumplimiento y trazabilidad
