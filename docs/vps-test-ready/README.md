# VPS Test Ready — AutonomusCRM

Preparacion completa para pruebas funcionales reales en VPS desde cero.

## Inicio rapido

```powershell
copy deploy\.env.vps.test.example deploy\.env.vps.test
# Editar secretos
.\deploy\deploy-vps-clean-test.ps1
.\tests\e2e\run-vps-test-qa.ps1
```

Login: http://164.68.99.83:8091 — `superadmin@autonomuscrm.local` / `AutonomusTest123!`

## Indice de documentos

| Doc | Contenido |
|-----|-----------|
| [01_PRE_DEPLOYMENT_AUDIT.md](01_PRE_DEPLOYMENT_AUDIT.md) | Auditoria previa |
| [03_TEST_USERS_AND_ROLES.md](03_TEST_USERS_AND_ROLES.md) | Usuarios y permisos |
| [04_INITIAL_TEST_DATA.md](04_INITIAL_TEST_DATA.md) | Datos iniciales |
| [06_LOCAL_VALIDATION_REPORT.md](06_LOCAL_VALIDATION_REPORT.md) | Validacion local |
| [07_VPS_BACKUP_PLAN.md](07_VPS_BACKUP_PLAN.md) | Backup obligatorio |
| [08_VPS_CLEANUP_EXECUTION.md](08_VPS_CLEANUP_EXECUTION.md) | Limpieza VPS |
| [09_VPS_DEPLOYMENT_GUIDE.md](09_VPS_DEPLOYMENT_GUIDE.md) | Instalacion |
| [10_VPS_POST_DEPLOYMENT_TEST_REPORT.md](10_VPS_POST_DEPLOYMENT_TEST_REPORT.md) | Validacion post-deploy |
| [11_FUNCTIONAL_TEST_PLAN_READY.md](11_FUNCTIONAL_TEST_PLAN_READY.md) | 26 casos manuales |
| [12_GO_LIVE_TEST_READY_REPORT.md](12_GO_LIVE_TEST_READY_REPORT.md) | Veredicto final |

## Scripts SQL

- `ops/database/vps-test/02_CLEAN_TEST_DATABASE_SCRIPT.sql`
- `ops/database/vps-test/05_FUNCTIONAL_TEST_DATA.sql`
