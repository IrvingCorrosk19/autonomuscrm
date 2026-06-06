# Iniciar pruebas hoy — TechSolutions Panamá

Guía rápida para ejecutar el escenario **primer cliente real** sin datos demo.

---

## Requisitos

- .NET 8 SDK
- PostgreSQL local (puerto 5432, BD `autonomuscrm`)
- PowerShell

---

## Opción A — Un comando (recomendado)

```powershell
cd c:\Proyectos\autonomuscrm

# BD vacía + API + bootstrap + QA
.\scripts\start-testing-today.ps1 -CleanSlate -StartApi -RunQa
```

**Primera vez:** copie y ajuste `tests\first-client\config.json` (password postgres).

---

## Opción B — Paso a paso

### 1. Configuración

```powershell
copy tests\first-client\config.example.json tests\first-client\config.json
# Edite postgres.password si difiere de Panama2020$
```

`Provisioning:ApiKey` en Development = `dev-bootstrap-key-change-in-production` (ya en `appsettings.Development.json`).

### 2. (Opcional) BD completamente vacía

```powershell
.\scripts\start-testing-today.ps1 -CleanSlate
# Crea backup en ops\postgres\backups\
```

### 3. Arrancar API

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project AutonomusCRM.API
# http://localhost:5154
```

### 4. Bootstrap tenant + 7 usuarios

```powershell
.\deploy\bootstrap-first-client.ps1
```

Crea **TechSolutions Panamá** y usuarios con roles vía API de provisioning.

### 5. Ejecutar QA automatizado

```powershell
.\tests\e2e\run-first-client-qa.ps1
```

Resultados en `tests\qa-evidence\first-client\<fecha>\`.

---

## Credenciales de prueba

| Rol | Email | Password |
|-----|-------|----------|
| Admin | `admin@techsolutions.pa` | `TechSolutions2026!` |
| Admin ops | `ops@techsolutions.pa` | `TechSolutions2026!` |
| Manager | `manager@techsolutions.pa` | `TechSolutions2026!` |
| Sales 1 | `sales1@techsolutions.pa` | `TechSolutions2026!` |
| Sales 2 | `sales2@techsolutions.pa` | `TechSolutions2026!` |
| Support | `support@techsolutions.pa` | `TechSolutions2026!` |
| Viewer | `viewer@techsolutions.pa` | `TechSolutions2026!` |

**URL:** http://localhost:5154/Account/Login

---

## Pruebas manuales (matriz completa)

Ver `ROLE_TEST_MATRIX.md` — 60+ casos por rol.

Flujo mínimo hoy:

1. Login cada rol → verificar home (`/executive`, `/revenue`, `/Customer360`, `/`)
2. Sales crea lead → customer → deal → Closed Won
3. Support/Viewer: confirmar bloqueo en `/Leads/Create`
4. Admin: `/Users`, `/Workflows`, `/Audit`

---

## Scripts creados

| Script | Uso |
|--------|-----|
| `scripts/start-testing-today.ps1` | Orquestador (clean slate, API, bootstrap, QA) |
| `deploy/bootstrap-first-client.ps1` | Provisiona tenant + usuarios |
| `tests/e2e/run-first-client-qa.ps1` | QA automatizado primer cliente |
| `ops/database/11_clean_slate_first_client.sql` | Borra tenants/users (solo pruebas) |

---

## Cambio de código incluido

`CreateUserCommand` acepta `role` opcional — usuarios creados por bootstrap/API ya llevan rol asignado (no requiere paso manual en Edit).

---

## Troubleshooting

| Problema | Solución |
|----------|----------|
| API no responde | `dotnet run --project AutonomusCRM.API` y espere migraciones |
| 401 provisioning | Verifique `Provisioning:ApiKey` = `dev-bootstrap-key-change-in-production` |
| psql no encontrado | Ajuste ruta en scripts o use pgAdmin con `11_clean_slate_first_client.sql` |
| Usuario ya existe | Bootstrap es idempotente — omite duplicados |
| Login falla | Ejecute bootstrap; verifique email `@techsolutions.pa` |

---

## Documentación relacionada

- `FIRST_CUSTOMER_BOOTSTRAP_GUIDE.md` — flujo detallado
- `ROLE_TEST_MATRIX.md` — matriz de pruebas
- `GO_LIVE_READINESS_REPORT.md` — bloqueantes conocidos
