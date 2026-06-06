# 11 — FUNCTIONAL TEST PLAN READY

**Password:** `AutonomusTest123!`  
**Base URL:** http://164.68.99.83:8091

Completar columna **Resultado obtenido** y **Estado** durante pruebas.

---

| # | Caso | Usuario | Rol | Ruta | Accion | Datos prueba | Esperado | Obtenido | Estado |
|---|------|---------|-----|------|--------|--------------|----------|----------|--------|
| 1 | Login Admin principal | superadmin@autonomuscrm.local | Admin | /Account/Login | Email+pass | AutonomusTest123! | Redirect /executive | | |
| 2 | Permisos Admin | superadmin@ | Admin | /Users | Abrir pagina | — | HTTP 200, lista usuarios | | |
| 3 | Permisos Manager | manager@autonomuscrm.local | Manager | /executive | Ver dashboard | — | KPIs visibles | | |
| 4 | Permisos Sales write | sales1@autonomuscrm.local | Sales | /Leads/Create | Crear lead | Lead Test QA | Redirect /Leads | | |
| 5 | Permisos Support read | support@autonomuscrm.local | Support | /Leads | Ver lista | — | 10 leads, sin boton crear | | |
| 6 | Permisos Viewer block | viewer@autonomuscrm.local | Viewer | /Leads/Create | Intentar crear | — | AccessDenied | | |
| 7 | Crear lead | sales1@ | Sales | /Leads/Create | POST form | nombre+email nuevos | Lead en tabla | | |
| 8 | Editar lead | sales1@ | Sales | /Leads/Edit?id= | Cambiar status | f1000001-...001 Qualified | Guardado OK | | |
| 9 | Convertir lead→cliente | sales1@ | Sales | /Leads/Details | Qualify+Convert | lead calificado | Customer creado | | |
| 10 | Crear oportunidad | sales1@ | Sales | /Deals/Create | Nuevo deal | cliente Banco Regional | Deal en pipeline | | |
| 11 | Mover pipeline | manager@ | Manager | /Deals/Edit | Stage→Negotiation | d1000001-...001 | Stage actualizado | | |
| 12 | Cerrar ganada | sales1@ | Sales | /Deals/Edit | ClosedWon | deal Salud Integral ya won | Stage 4, amount visible | | |
| 13 | Cerrar perdida | sales2@ | Sales | /Deals | Ver lost | CDC deal stage 5 | ClosedLost visible | | |
| 14 | Crear tarea | manager@ | Manager | Tasks/Workflows | Ver tareas | 8 tasks seed | Lista con tareas | | |
| 15 | Completar tarea | support@ | Support | Customer360/Tasks | Completar CS ticket | e2000002-...003 Completed | Status Completed | | |
| 16 | Crear usuario | admin@ | Admin | /Users/Create | Nuevo user+rol | qa.new@test.local | Usuario en lista | | |
| 17 | Cambiar rol | admin@ | Admin | /Users/Edit | AssignRole | Viewer→Sales | Rol actualizado | | |
| 18 | Executive OS | manager@ | Manager | /executive | Dashboard | deals+audits seed | HasData=true, metricas | | |
| 19 | Revenue OS | sales1@ | Sales | /revenue | Pipeline | 5 deals | Grafico/lista poblada | | |
| 20 | Trust Studio | admin@ | Admin | /TrustInbox | Cola HITL | audit score 78 pending | 1 decision pendiente | | |
| 21 | Audit | admin@ | Admin | /Audit | Ver eventos | 3 DomainEvents | Eventos reales listados | | |
| 22 | Settings | admin@ | Admin | /Settings | Ver/editar | region Panama | Pagina carga (persist parcial) | | |
| 23 | Billing | admin@ | Admin | /billing | Ver plan | starter | Plan starter, usage | | |
| 24 | Integrations | admin@ | Admin | /Integrations | Abrir | sin OAuth | Empty state OK | | |
| 25 | Workers | ops | — | SSH docker logs | Ver workers | rabbitmq events | Sin error critico loop | | |
| 26 | Logs API | ops | — | docker logs api | Revisar | post-login | Sin FTL/Exception loop | | |

---

## Datos de referencia (pre-cargados)

| Entidad | IDs ejemplo |
|---------|-------------|
| Tenant | b1000000-0000-4000-8000-000000000001 |
| Customer Banco Regional | c1000001-0000-4000-8000-000000000001 |
| Lead Fintech | f1000001-0000-4000-8000-000000000001 |
| Deal negociacion | d1000001-0000-4000-8000-000000000001 |
| Trust pending | a1000001-0000-4000-8000-000000000001 |

---

## Automatizacion

```powershell
.\tests\e2e\run-vps-test-qa.ps1
```

Cubre casos 1, 3-6, 7 parcial, 18-21 parcial (18 tests).
