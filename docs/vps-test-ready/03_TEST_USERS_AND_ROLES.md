# 03 — TEST USERS AND ROLES

**Tenant:** TechSolutions Panama  
**TenantId:** `b1000000-0000-4000-8000-000000000001`  
**Password (todos):** `AutonomusTest123!`

---

## Usuarios

| Email | Rol sistema | Nombre | Home post-login |
|-------|-------------|--------|-----------------|
| superadmin@autonomuscrm.local | **Admin** | Super Admin | `/executive` |
| admin@autonomuscrm.local | Admin | Admin Operaciones | `/executive` |
| manager@autonomuscrm.local | Manager | Roberto Castillo | `/executive` |
| sales1@autonomuscrm.local | Sales | Ana Rodriguez | `/revenue` |
| sales2@autonomuscrm.local | Sales | Diego Herrera | `/revenue` |
| support@autonomuscrm.local | Support | Maria Gomez | `/Customer360` |
| viewer@autonomuscrm.local | Viewer | Pedro Santos | `/` |

> **Nota:** `superadmin@` no es un rol RBAC separado. En codigo es **Admin** con email distinto para pruebas de maximo privilegio.

---

## Permisos por rol (UI)

| Accion | Admin | Manager | Sales | Support | Viewer |
|--------|-------|---------|-------|---------|--------|
| Crear lead/deal/cliente | Si | Si | Si | No | No |
| Ver dashboards ejecutivos | Si | Si | Parcial | Parcial | Lectura |
| Gestionar usuarios | Si | Si | No | No | No |
| Trust Studio HITL | Si | Si | No | Lectura | No |
| Settings sistema | Si | Parcial | No | No | No |
| Billing | Si | Lectura | No | No | No |

---

## BCrypt hash (referencia)

```
AutonomusTest123! → $2a$11$hOsKcM44lZ5yDelfLT2RbOJ8DuvD4r2QuNSIwAqutgDfH0r9KI782
```

Generado con BCrypt.Net-Next 4.0.3.

---

## Carga de usuarios

```powershell
# En VPS (post-migraciones)
.\deploy\apply-vps-test-data.ps1

# Local
psql -f ops/database/vps-test/02_CLEAN_TEST_DATABASE_SCRIPT.sql
```
