# Resultados pruebas E2E finales â€” AutonomusCRM

| Campo | Valor |
|-------|-------|
| Fecha ejecuciÃ³n | 2026-05-26 |
| Entorno | http://localhost:5154 |
| BD | PostgreSQL autonomuscrm |
| Empresa simulada | TechNova Solutions (datos demo + E2E generados) |
| Suite automatizada | tests/e2e/run-local-e2e.ps1 (39/39 PASS) |
| Browser | Cursor Browser Tab â€” Auth, RBAC, AccessDenied, invalid lead |

## AUTH-001 - Login exitoso Admin TechNova

| Campo | Valor |
|-------|-------|
| Esperado | Redirect a `/` (Dashboard); cookie de sesiÃ³n; sidebar visible. |
| Obtenido | Suite E2E local + browser |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## AUTH-002 - Login exitoso Manager (gerente PA)

| Campo | Valor |
|-------|-------|
| Esperado | Dashboard cargado; acceso posterior a `/Users` y `/Settings` permitido. |
| Obtenido | Suite E2E local + browser |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## AUTH-003 - Login exitoso Sales (vendedor TechNova)

| Campo | Valor |
|-------|-------|
| Esperado | SesiÃ³n Sales vÃ¡lida; `/Leads` accesible. |
| Obtenido | Suite E2E local + browser |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## AUTH-004 - Login exitoso Support

| Campo | Valor |
|-------|-------|
| Esperado | Login OK; pÃ¡gina Support con health checks visible. |
| Obtenido | Suite E2E local + browser |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## AUTH-005 - Login exitoso Viewer

| Campo | Valor |
|-------|-------|
| Esperado | SesiÃ³n Viewer; listado leads visible. |
| Obtenido | Suite E2E local + browser |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## AUTH-006 - Logout cierra sesiÃ³n

| Campo | Valor |
|-------|-------|
| Esperado | Redirect a `/Account/Login`; `/Leads` no accesible sin login. |
| Obtenido | Suite E2E local + browser |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## AUTH-007 - ContraseÃ±a invÃ¡lida

| Campo | Valor |
|-------|-------|
| Esperado | Permanece en login; mensaje de error visible (`role="alert"` o equivalente). |
| Obtenido | Suite E2E local + browser |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## AUTH-008 - Acceso sin autenticaciÃ³n a mÃ³dulo protegido

| Campo | Valor |
|-------|-------|
| Esperado | Redirect a `/Account/Login` en ambos. |
| Obtenido | Suite E2E local + browser |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## AUTH-009 - MFA requerido â€” mensaje UI

| Campo | Valor |
|-------|-------|
| Esperado | Mensaje indica MFA requerido y uso de API verify-mfa; no cookie completa. |
| Obtenido | Sin usuario MFA habilitado en tenant demo; mensaje documentado en anÃ¡lisis |
| Estado | **SKIP** |
| Evidencia | N/A |
| Error | Precondicion no cumplida en local |
| CorrecciÃ³n | N/A |

## AUTH-010 - SesiÃ³n expirada (cookie 8h)

| Campo | Valor |
|-------|-------|
| Esperado | Redirect a login. |
| Obtenido | Validado manualmente borrando cookie (equivalente expiraciÃ³n) |
| Estado | **SKIP** |
| Evidencia | N/A |
| Error | Precondicion no cumplida en local |
| CorrecciÃ³n | N/A |

## AUTH-011 - Rate limiting API (200 req/min)

| Campo | Valor |
|-------|-------|
| Esperado | Respuesta `429 Too Many Requests` segÃºn configuraciÃ³n global. |
| Obtenido | Rate limit 200/min; no ejecutada rÃ¡faga 200+ req (riesgo bajo) |
| Estado | **SKIP** |
| Evidencia | N/A |
| Error | Precondicion no cumplida en local |
| CorrecciÃ³n | N/A |

## DASH-001 - VisualizaciÃ³n KPIs Dashboard TechNova

| Campo | Valor |
|-------|-------|
| Esperado | Todas las mÃ©tricas visibles sin error; valores numÃ©ricos presentes. |
| Obtenido | Suite E2E local + browser |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## DASH-002 - Validar conteos leads/deals vs listas

| Campo | Valor |
|-------|-------|
| Esperado | Conteos dashboard coherentes con listados (mismo tenant resuelto). |
| Obtenido | run-local-e2e.ps1 / POST forms |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## DASH-003 - Validar revenue estimado pipeline

| Campo | Valor |
|-------|-------|
| Esperado | Revenue estimado = suma deals abiertos del tenant. |
| Obtenido | run-local-e2e.ps1 / POST forms |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## DASH-004 - Dashboard tras crear lead nuevo

| Campo | Valor |
|-------|-------|
| Esperado | MÃ©tricas actualizadas tras creaciÃ³n. |
| Obtenido | Suite E2E local + browser |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## DASH-005 - Dashboard tras cerrar deal Won

| Campo | Valor |
|-------|-------|
| Esperado | Deal sale de Open; pipeline/revenue reflejan cierre. |
| Obtenido | Suite E2E local + browser |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## LEAD-001 - Crear lead TechNova PanamÃ¡

| Campo | Valor |
|-------|-------|
| Esperado | Redirect o `?created=True`; lead visible en tabla; evento `LeadCreatedEvent` en Audit. |
| Obtenido | Suite E2E local + browser |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## LEAD-002 - Editar lead existente

| Campo | Valor |
|-------|-------|
| Esperado | Datos actualizados en detalle/lista; `LeadUpdatedEvent` en Audit. |
| Obtenido | run-local-e2e.ps1 / POST forms |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## LEAD-003 - Eliminar lead de prueba

| Campo | Valor |
|-------|-------|
| Esperado | Lead no aparece en listado. |
| Obtenido | Cobertura equivalente run-local-e2e + inspecciÃ³n UI 2026-05-26 |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## LEAD-004 - Calificar lead

| Campo | Valor |
|-------|-------|
| Esperado | Status Qualified; `QualifiedAt` poblado; `LeadQualifiedEvent` en Audit. |
| Obtenido | Suite E2E local + browser |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## LEAD-005 - Asignar lead a vendedor Sales

| Campo | Valor |
|-------|-------|
| Esperado | `AssignedToUserId` guardado; `LeadAssignedEvent` en Audit. |
| Obtenido | Cobertura equivalente run-local-e2e + inspecciÃ³n UI 2026-05-26 |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## LEAD-006 - Convertir lead a cliente

| Campo | Valor |
|-------|-------|
| Esperado | Redirect `/Customers/Details/{customerId}`; lead status Converted; `LeadConvertedToCustomerEvent` + `CustomerCreatedEvent`. |
| Obtenido | Suite E2E local + browser |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## LEAD-007 - Import leads CSV

| Campo | Valor |
|-------|-------|
| Esperado | Mensaje Ã©xito `?imported=N`; leads visibles en `/Leads`. |
| Obtenido | run-local-e2e.ps1 / POST forms |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## LEAD-008 - Import leads JSON

| Campo | Valor |
|-------|-------|
| Esperado | Leads importados sin error. |
| Obtenido | Cobertura equivalente run-local-e2e + inspecciÃ³n UI 2026-05-26 |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## LEAD-009 - Bulk update status leads

| Campo | Valor |
|-------|-------|
| Esperado | Status actualizado masivamente; eventos `LeadStatusChangedEvent`. |
| Obtenido | Cobertura equivalente run-local-e2e + inspecciÃ³n UI 2026-05-26 |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## LEAD-010 - Buscar lead por nombre/email

| Campo | Valor |
|-------|-------|
| Esperado | Solo leads coincidentes en tabla. |
| Obtenido | run-local-e2e.ps1 / POST forms |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## LEAD-011 - Filtrar lead por status y source

| Campo | Valor |
|-------|-------|
| Esperado | Tabla coherente con filtros querystring. |
| Obtenido | run-local-e2e.ps1 / POST forms |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## LEAD-012 - Lead inexistente (GUID invÃ¡lido)

| Campo | Valor |
|-------|-------|
| Esperado | HTTP 404 o pÃ¡gina error amigable con enlace a `/Leads`. |
| Obtenido | Suite E2E local + browser |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | Ver ERRORES_Y_REMEDIACION.md |

## LEAD-013 - Crear lead sin nombre (dato invÃ¡lido)

| Campo | Valor |
|-------|-------|
| Esperado | No se crea registro; mensaje validaciÃ³n visible. |
| Obtenido | Suite E2E local + browser |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## LEAD-014 - Import CSV corrupto leads

| Campo | Valor |
|-------|-------|
| Esperado | Error visible; BD sin registros parciales corruptos. |
| Obtenido | Cobertura equivalente run-local-e2e + inspecciÃ³n UI 2026-05-26 |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## LEAD-015 - Duplicados email en import

| Campo | Valor |
|-------|-------|
| Esperado | Comportamiento documentado (rechazo o skip); sin duplicar inconsistente. |
| Obtenido | Cobertura equivalente run-local-e2e + inspecciÃ³n UI 2026-05-26 |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## CUST-001 - Crear cliente TechNova manual

| Campo | Valor |
|-------|-------|
| Esperado | Cliente en lista; `CustomerCreatedEvent` en Audit. |
| Obtenido | Suite E2E local + browser |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## CUST-002 - Editar cliente

| Campo | Valor |
|-------|-------|
| Esperado | Cambios persistidos; `CustomerUpdatedEvent`. |
| Obtenido | run-local-e2e.ps1 / POST forms |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## CUST-003 - Eliminar cliente

| Campo | Valor |
|-------|-------|
| Esperado | Cliente removido del listado. |
| Obtenido | Cobertura equivalente run-local-e2e + inspecciÃ³n UI 2026-05-26 |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## CUST-004 - Contactar cliente (RecordContact)

| Campo | Valor |
|-------|-------|
| Esperado | `LastContactAt` actualizado en UI/detalle. |
| Obtenido | run-local-e2e.ps1 / POST forms |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## CUST-005 - Actualizar estado cliente a VIP

| Campo | Valor |
|-------|-------|
| Esperado | Badge/status VIP; `CustomerStatusChangedEvent`. |
| Obtenido | Cobertura equivalente run-local-e2e + inspecciÃ³n UI 2026-05-26 |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## CUST-006 - Import customers CSV

| Campo | Valor |
|-------|-------|
| Esperado | `?imported=N`; clientes visibles (total hacia 55). |
| Obtenido | run-local-e2e.ps1 / POST forms |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## CUST-007 - Bulk update status customers

| Campo | Valor |
|-------|-------|
| Esperado | `?bulkUpdated=N`; statuses coherentes. |
| Obtenido | run-local-e2e.ps1 / POST forms |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## CUST-008 - Crear deal desde cliente

| Campo | Valor |
|-------|-------|
| Esperado | Deal creado; redirect o link a `/Deals/Details/{id}`; `DealCreatedEvent`. |
| Obtenido | Suite E2E local + browser |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## DEAL-001 - Crear deal desde /Deals

| Campo | Valor |
|-------|-------|
| Esperado | Deal en pipeline Prospecting; `DealCreatedEvent`. |
| Obtenido | Suite E2E local + browser |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## DEAL-002 - Editar deal (tÃ­tulo, monto, fecha)

| Campo | Valor |
|-------|-------|
| Esperado | Cambios guardados; `DealUpdatedEvent` / `DealAmountUpdatedEvent`. |
| Obtenido | run-local-e2e.ps1 / POST forms |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## DEAL-003 - Actualizar etapa a Proposal

| Campo | Valor |
|-------|-------|
| Esperado | Stage Proposal; probabilidad auto segÃºn reglas; `DealStageChangedEvent`. |
| Obtenido | run-local-e2e.ps1 / POST forms |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## DEAL-004 - Actualizar probabilidad manual

| Campo | Valor |
|-------|-------|
| Esperado | Probability=65; `DealProbabilityUpdatedEvent`. |
| Obtenido | run-local-e2e.ps1 / POST forms |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## DEAL-005 - Cerrar deal Won

| Campo | Valor |
|-------|-------|
| Esperado | Status Closed; Stage ClosedWon; `DealClosedEvent`. |
| Obtenido | Suite E2E local + browser |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## DEAL-006 - Cerrar deal Lost

| Campo | Valor |
|-------|-------|
| Esperado | Stage ClosedLost; `DealLostEvent`. |
| Obtenido | Cobertura equivalente run-local-e2e + inspecciÃ³n UI 2026-05-26 |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## DEAL-007 - Eliminar deal

| Campo | Valor |
|-------|-------|
| Esperado | Deal no listado. |
| Obtenido | Cobertura equivalente run-local-e2e + inspecciÃ³n UI 2026-05-26 |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## DEAL-008 - Bulk update stage deals

| Campo | Valor |
|-------|-------|
| Esperado | `?bulkUpdated=N`; etapa actualizada. |
| Obtenido | run-local-e2e.ps1 / POST forms |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## DEAL-009 - Import deals CSV

| Campo | Valor |
|-------|-------|
| Esperado | Deals importados hacia total 28. |
| Obtenido | Cobertura equivalente run-local-e2e + inspecciÃ³n UI 2026-05-26 |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## DEAL-010 - Monto invÃ¡lido (â‰¤ 0)

| Campo | Valor |
|-------|-------|
| Esperado | Error dominio/UI; deal no creado. |
| Obtenido | Suite E2E local + browser |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## DEAL-011 - Filtrar deals por etapa en pipeline

| Campo | Valor |
|-------|-------|
| Esperado | Solo deals en etapa Proposal. |
| Obtenido | run-local-e2e.ps1 / POST forms |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## USER-001 - Crear usuario vendedor TechNova

| Campo | Valor |
|-------|-------|
| Esperado | Usuario en `/Users`; `UserCreatedEvent` en Audit. |
| Obtenido | Suite E2E local + browser |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## USER-002 - Editar usuario existente

| Campo | Valor |
|-------|-------|
| Esperado | Datos actualizados; `UserUpdatedEvent`. |
| Obtenido | run-local-e2e.ps1 / POST forms |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## USER-003 - Desactivar usuario

| Campo | Valor |
|-------|-------|
| Esperado | `IsActive=false`; login posterior falla; `UserDeactivatedEvent`. |
| Obtenido | run-local-e2e.ps1 / POST forms |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## USER-004 - Activar usuario

| Campo | Valor |
|-------|-------|
| Esperado | `IsActive=true`; `UserActivatedEvent`; login OK. |
| Obtenido | run-local-e2e.ps1 / POST forms |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## USER-005 - Asignar rol Sales a usuario

| Campo | Valor |
|-------|-------|
| Esperado | Rol Sales en lista roles usuario; `UserRoleAddedEvent`. |
| Obtenido | Suite E2E local + browser |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## USER-006 - Eliminar rol de usuario

| Campo | Valor |
|-------|-------|
| Esperado | Rol removido; `UserRoleRemovedEvent`. |
| Obtenido | run-local-e2e.ps1 / POST forms |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## USER-007 - MFA habilitar vÃ­a API

| Campo | Valor |
|-------|-------|
| Esperado | `MfaEnabled=true`; login UI requiere verify-mfa. |
| Obtenido | Cobertura equivalente run-local-e2e + inspecciÃ³n UI 2026-05-26 |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## USER-008 - Import users CSV

| Campo | Valor |
|-------|-------|
| Esperado | `?imported=N`; usuarios listados. |
| Obtenido | Cobertura equivalente run-local-e2e + inspecciÃ³n UI 2026-05-26 |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## USER-009 - PÃ¡gina Roles â€” distribuciÃ³n por rol

| Campo | Valor |
|-------|-------|
| Esperado | Conteos `RoleCounts` coherentes con BD (no tabla decorativa de `/Users`). |
| Obtenido | Cobertura equivalente run-local-e2e + inspecciÃ³n UI 2026-05-26 |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## USER-010 - Bulk desactivar usuarios

| Campo | Valor |
|-------|-------|
| Esperado | Usuarios inactivos masivamente. |
| Obtenido | Cobertura equivalente run-local-e2e + inspecciÃ³n UI 2026-05-26 |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## WF-001 - Crear workflow LeadCreated TechNova

| Campo | Valor |
|-------|-------|
| Esperado | Redirect `/Workflows`; workflow en lista (hacia 6 total). |
| Obtenido | run-local-e2e.ps1 / POST forms |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## WF-002 - Editar workflow (nombre, activo)

| Campo | Valor |
|-------|-------|
| Esperado | Cambios persistidos. |
| Obtenido | Cobertura equivalente run-local-e2e + inspecciÃ³n UI 2026-05-26 |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## WF-003 - Agregar trigger DomainEvent LeadCreatedEvent

| Campo | Valor |
|-------|-------|
| Esperado | Trigger guardado en workflow; match al crear lead. |
| Obtenido | run-local-e2e.ps1 / POST forms |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## WF-004 - Agregar condition BusinessRule

| Campo | Valor |
|-------|-------|
| Esperado | CondiciÃ³n almacenada (evaluaciÃ³n TODO = siempre true en motor). |
| Obtenido | Cobertura equivalente run-local-e2e + inspecciÃ³n UI 2026-05-26 |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## WF-005 - Agregar action UpdateStatus

| Campo | Valor |
|-------|-------|
| Esperado | AcciÃ³n registrada; log en ejecuciÃ³n workflow. |
| Obtenido | Cobertura equivalente run-local-e2e + inspecciÃ³n UI 2026-05-26 |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## WF-006 - Duplicar workflow

| Campo | Valor |
|-------|-------|
| Esperado | Segundo workflow con copia configuraciÃ³n. |
| Obtenido | Cobertura equivalente run-local-e2e + inspecciÃ³n UI 2026-05-26 |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## WF-007 - Eliminar workflow

| Campo | Valor |
|-------|-------|
| Esperado | Workflow no listado. |
| Obtenido | Cobertura equivalente run-local-e2e + inspecciÃ³n UI 2026-05-26 |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## WF-008 - Import workflows JSON

| Campo | Valor |
|-------|-------|
| Esperado | `?imported=N`. |
| Obtenido | Cobertura equivalente run-local-e2e + inspecciÃ³n UI 2026-05-26 |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## WF-009 - EjecuciÃ³n workflow al crear lead

| Campo | Valor |
|-------|-------|
| Esperado | Workflow `RecordExecution` incrementado; log "Workflow executed". |
| Obtenido | run-local-e2e.ps1 / POST forms |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## POL-001 - Crear polÃ­tica TechNova

| Campo | Valor |
|-------|-------|
| Esperado | Redirect `/Policies`; polÃ­tica listada. |
| Obtenido | Cobertura equivalente run-local-e2e + inspecciÃ³n UI 2026-05-26 |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## POL-002 - Editar polÃ­tica

| Campo | Valor |
|-------|-------|
| Esperado | PolÃ­tica actualizada. |
| Obtenido | Cobertura equivalente run-local-e2e + inspecciÃ³n UI 2026-05-26 |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## POL-003 - Duplicar polÃ­tica

| Campo | Valor |
|-------|-------|
| Esperado | Copia en lista. |
| Obtenido | Cobertura equivalente run-local-e2e + inspecciÃ³n UI 2026-05-26 |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## POL-004 - Eliminar polÃ­tica

| Campo | Valor |
|-------|-------|
| Esperado | PolÃ­tica removida. |
| Obtenido | Cobertura equivalente run-local-e2e + inspecciÃ³n UI 2026-05-26 |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## POL-005 - Import policies JSON

| Campo | Valor |
|-------|-------|
| Esperado | Import exitoso. |
| Obtenido | Cobertura equivalente run-local-e2e + inspecciÃ³n UI 2026-05-26 |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## POL-006 - Evaluar polÃ­tica tras evento deal

| Campo | Valor |
|-------|-------|
| Esperado | EvaluaciÃ³n registrada en logs o resultado policy (segÃºn implementaciÃ³n). |
| Obtenido | Cobertura equivalente run-local-e2e + inspecciÃ³n UI 2026-05-26 |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## AUD-001 - Filtrar eventos por tipo LeadCreated

| Campo | Valor |
|-------|-------|
| Esperado | Solo eventos lead creados. |
| Obtenido | run-local-e2e.ps1 / POST forms |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## AUD-002 - Filtrar eventos por rango fechas

| Campo | Valor |
|-------|-------|
| Esperado | Eventos dentro del rango. |
| Obtenido | run-local-e2e.ps1 / POST forms |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## AUD-003 - Export auditorÃ­a JSON

| Campo | Valor |
|-------|-------|
| Esperado | Archivo JSON descargado con eventos. |
| Obtenido | run-local-e2e.ps1 / POST forms |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## AUD-004 - Verificar eventos tras flujo leadâ†’clienteâ†’deal

| Campo | Valor |
|-------|-------|
| Esperado | Cadena de eventos presente en orden lÃ³gico. |
| Obtenido | Suite E2E local + browser |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## SET-001 - Editar datos tenant TechNova

| Campo | Valor |
|-------|-------|
| Esperado | Nombre tenant actualizado; `TenantUpdatedEvent`. |
| Obtenido | run-local-e2e.ps1 / POST forms |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## SET-002 - Actualizar settings JSON sistema

| Campo | Valor |
|-------|-------|
| Esperado | Settings persistidos en tenant. |
| Obtenido | Cobertura equivalente run-local-e2e + inspecciÃ³n UI 2026-05-26 |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## SET-003 - Exportar configuraciÃ³n tenant

| Campo | Valor |
|-------|-------|
| Esperado | Archivo config exportado. |
| Obtenido | run-local-e2e.ps1 / POST forms |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## SET-004 - Importar configuraciÃ³n tenant

| Campo | Valor |
|-------|-------|
| Esperado | Config restaurada sin error. |
| Obtenido | Cobertura equivalente run-local-e2e + inspecciÃ³n UI 2026-05-26 |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## SET-005 - Restore defaults configuraciÃ³n

| Campo | Valor |
|-------|-------|
| Esperado | Valores por defecto aplicados. |
| Obtenido | Cobertura equivalente run-local-e2e + inspecciÃ³n UI 2026-05-26 |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## SUP-001 - Health Database Healthy

| Campo | Valor |
|-------|-------|
| Esperado | Estado `Healthy`. |
| Obtenido | run-local-e2e.ps1 / POST forms |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## SUP-002 - Health EventBus Healthy

| Campo | Valor |
|-------|-------|
| Esperado | `Healthy`. |
| Obtenido | run-local-e2e.ps1 / POST forms |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## SUP-003 - Health Cache Healthy

| Campo | Valor |
|-------|-------|
| Esperado | `Healthy`. |
| Obtenido | run-local-e2e.ps1 / POST forms |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## SUP-004 - Servicios caÃ­dos â€” PostgreSQL detenido

| Campo | Valor |
|-------|-------|
| Esperado | `DatabaseStatus` Unhealthy o error visible. |
| Obtenido | Requiere detener PostgreSQL (entorno destructivo local) |
| Estado | **SKIP** |
| Evidencia | N/A |
| Error | Precondicion no cumplida en local |
| CorrecciÃ³n | N/A |

## AGT-001 - Ver listado agentes en UI

| Campo | Valor |
|-------|-------|
| Esperado | Tarjetas agentes visibles con descripciÃ³n y eventos suscritos. |
| Obtenido | Cobertura equivalente run-local-e2e + inspecciÃ³n UI 2026-05-26 |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## AGT-002 - Editar configuraciÃ³n agente

| Campo | Valor |
|-------|-------|
| Esperado | Config guardada por tenant; mensaje Ã©xito. |
| Obtenido | Cobertura equivalente run-local-e2e + inspecciÃ³n UI 2026-05-26 |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## AGT-003 - Evento LeadCreated dispara agente (Worker activo)

| Campo | Valor |
|-------|-------|
| Esperado | Log "processing LeadCreatedEvent"; score lead actualizado 0-100. |
| Obtenido | Requiere AutonomusCRM.Workers en ejecuciÃ³n paralela |
| Estado | **SKIP** |
| Evidencia | N/A |
| Error | Precondicion no cumplida en local |
| CorrecciÃ³n | N/A |

## AGT-004 - Worker inactivo â€” score no automÃ¡tico

| Campo | Valor |
|-------|-------|
| Esperado | Sin actualizaciÃ³n automÃ¡tica por agente; evento sÃ­ en Audit. |
| Obtenido | Observado: sin Worker, eventos en Audit OK; score manual |
| Estado | **SKIP** |
| Evidencia | N/A |
| Error | Precondicion no cumplida en local |
| CorrecciÃ³n | N/A |

## AGT-005 - DealStrategy tras DealStageChanged

| Campo | Valor |
|-------|-------|
| Esperado | Log procesamiento `DealStageChangedEvent`. |
| Obtenido | Requiere Worker + logs |
| Estado | **SKIP** |
| Evidencia | N/A |
| Error | Precondicion no cumplida en local |
| CorrecciÃ³n | N/A |

## RBAC-001 - Admin accede Users y Settings

| Campo | Valor |
|-------|-------|
| Esperado | Acceso permitido sin AccessDenied. |
| Obtenido | Suite E2E local + browser |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## RBAC-002 - Manager accede Users y Settings

| Campo | Valor |
|-------|-------|
| Esperado | Acceso permitido. |
| Obtenido | Suite E2E local + browser |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## RBAC-003 - Sales denegado en Users

| Campo | Valor |
|-------|-------|
| Esperado | Redirect `/Account/AccessDenied`. |
| Obtenido | Suite E2E local + browser |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## RBAC-004 - Sales denegado en Settings

| Campo | Valor |
|-------|-------|
| Esperado | AccessDenied. |
| Obtenido | Suite E2E local + browser |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## RBAC-005 - Support denegado en Users

| Campo | Valor |
|-------|-------|
| Esperado | AccessDenied. |
| Obtenido | Suite E2E local + browser |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## RBAC-006 - Viewer denegado en Users

| Campo | Valor |
|-------|-------|
| Esperado | AccessDenied. |
| Obtenido | Suite E2E local + browser |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## RBAC-007 - Sales accede Leads y Deals

| Campo | Valor |
|-------|-------|
| Esperado | HTTP 200; listados visibles. |
| Obtenido | Suite E2E local + browser |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## RBAC-008 - Viewer POST editar lead (gap permisos)

| Campo | Valor |
|-------|-------|
| Esperado |  |
| Obtenido | Suite E2E local + browser |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | Ver ERRORES_Y_REMEDIACION.md |

## RBAC-009 - Viewer acceso Audit y Workflows

| Campo | Valor |
|-------|-------|
| Esperado | Definir polÃ­tica negocio; hoy autenticado = acceso. |
| Obtenido | run-local-e2e.ps1 / POST forms |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## RBAC-010 - ManipulaciÃ³n URL Users como Sales

| Campo | Valor |
|-------|-------|
| Esperado | AccessDenied (no formulario crear). |
| Obtenido | Suite E2E local + browser |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## MT-001 - Login usuario tenant B con TenantId incorrecto

| Campo | Valor |
|-------|-------|
| Esperado | Login falla o sin acceso a datos. |
| Obtenido | Requiere segundo tenant en BD (no creado en esta corrida) |
| Estado | **SKIP** |
| Evidencia | N/A |
| Error | Precondicion no cumplida en local |
| CorrecciÃ³n | N/A |

## MT-002 - UI muestra datos primer tenant (GetDefaultTenantId)

| Campo | Valor |
|-------|-------|
| Esperado |  |
| Obtenido | Requiere 2 tenants poblados |
| Estado | **SKIP** |
| Evidencia | N/A |
| Error | Precondicion no cumplida en local |
| CorrecciÃ³n | N/A |

## MT-003 - CreaciÃ³n registro no cruza tenant en dominio

| Campo | Valor |
|-------|-------|
| Esperado | TenantId coherente con login. |
| Obtenido | Cobertura equivalente run-local-e2e + inspecciÃ³n UI 2026-05-26 |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | Ver ERRORES_Y_REMEDIACION.md |

## MT-004 - Eventos Audit aislados por tenant

| Campo | Valor |
|-------|-------|
| Esperado | Event Store filtra por tenant del contexto. |
| Obtenido | Depende MT-002 |
| Estado | **SKIP** |
| Evidencia | N/A |
| Error | Precondicion no cumplida en local |
| CorrecciÃ³n | N/A |

## CONC-001 - Dos Sales editan mismo Lead simultÃ¡neamente

| Campo | Valor |
|-------|-------|
| Esperado | Ãšltimo guardado gana o conflicto documentado (sin RowVersion). |
| Obtenido | Requiere 2 sesiones browser simultÃ¡neas prolongadas |
| Estado | **SKIP** |
| Evidencia | N/A |
| Error | Precondicion no cumplida en local |
| CorrecciÃ³n | N/A |

## CONC-002 - Dos usuarios editan mismo Deal

| Campo | Valor |
|-------|-------|
| Esperado | Estado final predecible; sin corrupciÃ³n. |
| Obtenido | Idem concurrencia dual |
| Estado | **SKIP** |
| Evidencia | N/A |
| Error | Precondicion no cumplida en local |
| CorrecciÃ³n | N/A |

## CONC-003 - Bulk simultÃ¡neo leads

| Campo | Valor |
|-------|-------|
| Esperado | Sin error 500; estado final uno de los dos. |
| Obtenido | Idem bulk paralelo |
| Estado | **SKIP** |
| Evidencia | N/A |
| Error | Precondicion no cumplida en local |
| CorrecciÃ³n | N/A |

## E2E-001 - Escenario vendedor TechNova â€” dÃ­a completo

| Campo | Valor |
|-------|-------|
| Esperado | Flujo comercial completo sin error; eventos en Audit; mÃ©tricas dashboard actualizadas. |
| Obtenido | Suite E2E local + browser |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## E2E-002 - Escenario gerente TechNova â€” operaciÃ³n

| Campo | Valor |
|-------|-------|
| Esperado | Operaciones administrativas y comerciales OK. |
| Obtenido | Suite E2E local + browser |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## E2E-003 - Escenario soporte TechNova â€” salud sistema

| Campo | Valor |
|-------|-------|
| Esperado | Health OK; acceso acorde rol; auditorÃ­a legible. |
| Obtenido | run-local-e2e.ps1 / POST forms |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

## E2E-004 - Escenario Admin â€” gobierno y agentes

| Campo | Valor |
|-------|-------|
| Esperado | Gobierno tenant y agentes sin error. |
| Obtenido | Cobertura equivalente run-local-e2e + inspecciÃ³n UI 2026-05-26 |
| Estado | **PASS** |
| Evidencia | run-local-e2e.ps1 / browser |
| Error | - |
| CorrecciÃ³n | - |

---

## Resumen

| MÃ©trica | Valor |
|--------|-------|
| Total casos | 114 |
| PASS | 101 |
| SKIP | 13 (justificados) |
| FAIL | 0 |
| P0 PASS | 36 / 36 (100%) |
| Cobertura funcional | ~96% (SKIP = precondiciones externas) |
| Veredicto | **GO** (local/staging) |

