# VPS QA — Inicio rápido (TechSolutions Panama)

Entorno de pruebas funcional en VPS. **Listo para QA humano tras deploy.**

---

## Acceso

| Campo | Valor |
|-------|-------|
| **URL** | http://164.68.99.83:8091/Account/Login |
| **Health** | http://164.68.99.83:8091/health/live |
| **Tenant** | TechSolutions Panama |
| **TenantId** | `b1000000-0000-4000-8000-000000000001` |
| **Password (todos)** | `AutonomusTest123!` |

> En login dejar **TenantId vacío** o `00000000-0000-0000-0000-000000000000`.

---

## Usuarios por rol

| Rol | Email | Home tras login |
|-----|-------|-----------------|
| Admin (super) | superadmin@autonomuscrm.local | `/executive` |
| Admin | admin@autonomuscrm.local | `/executive` |
| Manager | manager@autonomuscrm.local | `/executive` |
| Sales 1 | sales1@autonomuscrm.local | `/revenue` |
| Sales 2 | sales2@autonomuscrm.local | `/revenue` |
| Support | support@autonomuscrm.local | `/Customer360` |
| Viewer | viewer@autonomuscrm.local | `/` |

---

## Datos precargados (05_FUNCTIONAL_TEST_DATA.sql)

| Entidad | Cantidad |
|---------|----------|
| Clientes | 5 |
| Leads | 10 |
| Deals | 5 (incl. ganada y perdida) |
| Tareas / CS tickets | 8 |
| Workflows | 4 (2 activos) |
| Políticas | 1 |

---

## Smoke routes (Admin)

```
/  /executive  /revenue  /TrustInbox  /Customer360
/Leads  /Customers  /Deals  /Tasks  /Users  /Policies
/Settings  /Integrations  /Audit  /billing  /Workflows
/Memory  /customer-success
```

---

## Validación automatizada (desde PC local)

```powershell
cd c:\Proyectos\autonomuscrm

# Smoke + roles + navegación
.\tests\e2e\run-vps-test-qa.ps1

# Solo smoke de rutas
$config = Get-Content tests\vps-test\config.json | ConvertFrom-Json
# Editar tests\first-client\config.json baseUrl temporalmente o usar:
Copy-Item tests\vps-test\config.json tests\first-client\config.vps.json
# run-rc-smoke.ps1 acepta -ConfigPath tests\first-client\config.vps.json
.\tests\e2e\run-rc-smoke.ps1 -ConfigPath tests\vps-test\config.json
```

Config QA: `tests/vps-test/config.json`

---

## Redeploy limpio (elimina app + DB anterior)

```powershell
.\deploy\deploy-vps-clean-test.ps1
```

Esto: backup → borra contenedores y volumen PostgreSQL → sube versión actual → migraciones → carga SQL de prueba → QA automatizado.

---

## Matriz de pruebas

Ver `ROLE_TEST_MATRIX.md` y `QA_HANDOFF_READY.md` (adaptar URL a VPS).

---

## Configuración VPS (.env)

Plantilla: `deploy/.env.vps.test.example`  
Activa: `deploy/.env.vps` (generada desde `.env.vps.test` en deploy limpio)

- `SEED_ENABLED=false` — sin demo seed automático
- `COMMS_ALLOW_SIMULATION=true` — email/WhatsApp en modo log
- `AI_ENABLED=false` — sin LLM externo en pruebas

---

*Generado para RC Zero — AutonomusCRM*
