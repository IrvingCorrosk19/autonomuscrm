# RUNTIME_ENTERPRISE_QA

## Build
- `dotnet build AutonomusCRM.sln -c Release` — **OK** (0 errores, warnings preexistentes).

## Cambios verificados
| Item | Resultado |
|------|-----------|
| `_CrmRuntimeBar` render en layout autenticado | OK |
| `ModuleActive` sidebar subrutas | OK |
| `crmUi.initRuntimeBar` + sessionStorage | OK |
| Densidad persistida | OK |
| Index ops bar sticky | OK |
| Dark prep selectors | OK |

## QA manual recomendado
### Runtime
1. Login → Dashboard: ver barra runtime con contexto “Dashboard”.
2. Ir a `/Leads` → sidebar Leads activo; runtime “Leads” activo.
3. Ir a `/Agents` → aparece “Continuar: Leads” (o último módulo).
4. Click Continuar → vuelve a Leads.

### Densidad
5. Index → compact → reload → sigue compact en otra página con toggle.

### Accesibilidad
6. Tab por runtime bar; Enter en enlace.
7. Tabla: foco fila + ArrowDown.

### Regresión
8. Logout POST con antiforgery (fase previa).
9. Agents modal focus trap.
10. Onboarding dismiss/reset Index.

## Riesgos residuales
- Bajo: doble sticky en dashboard desktop (aceptable).
- Medio-bajo: páginas CRM no migradas visualmente.

## Entorno
ASPNETCORE_ENVIRONMENT Local/Staging; validar en 1280px y 375px.
