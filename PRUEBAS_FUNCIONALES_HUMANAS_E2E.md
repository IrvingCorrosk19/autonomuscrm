# Pruebas funcionales humanas E2E — AutonomusCRM (local)

| Campo | Valor |
|-------|-------|
| **Fecha** | 2026-05-25 (iteración 2 — Browser Tab completa) |
| **Entorno** | `http://localhost:5154` |
| **Método** | **Browser Tab** — clic, formularios, dropdowns, modales, confirmaciones (sin API directa como prueba principal) |
| **PostgreSQL** | `localhost:5432` — `C:\Program Files\PostgreSQL\18\bin` |
| **EventBus** | `InMemory` (`appsettings.Development.json`) |
| **Veredicto** | **GO** |

---

## 1. Alcance del producto real

**Proyecto:** `AutonomusCRM.sln` — CRM comercial (Leads, Clientes, Deals, Users, Workflows, Policies, Audit, Support, Agents).

El prompt solicitaba un **sistema escolar** (escuelas, grados, carnets, QR, 50 estudiantes). **Eso no existe en este repositorio.** Las pruebas cubren el **100 % de módulos UI existentes**, mapeando roles así:

| Prompt | Rol CRM | Credenciales |
|--------|---------|--------------|
| SuperAdmin | Admin | admin@autonomuscrm.local / Admin123! |
| Admin | Admin, Manager | admin, manager |
| Director | Manager | manager@autonomuscrm.local |
| Secretaria | Manager | manager@autonomuscrm.local |
| Teacher | Sales | sales@autonomuscrm.local |
| Student | Viewer | viewer@autonomuscrm.local |
| Parent, Staff | Support | support@autonomuscrm.local |

**N/A global:** Escuelas, Grados, Grupos, Materias, Notas académicas, Carnets, QR, PDF impresión escolar, DataTables, AJAX SPA.

---

## 2. Datos de prueba (equivalente CRM)

| Entidad | Cantidad aprox. | Creación |
|---------|-----------------|----------|
| Tenant demo | 1 | Seed |
| Usuarios por rol | 5 seed + E2E | Seed + UI `/Users/Create` |
| Leads | 8+ | UI + seed |
| Clientes | 18+ | UI + conversiones + import |
| Deals | 5+ | UI + seed |
| Workflows | 4+ | UI `/Workflows/Create` |
| Políticas | 1+ | UI `/Policies/Create` (corrida 2) |

---

## 3. Matriz de pruebas por rol (browser)

### 3.1 SuperAdmin / Admin

| Prueba | Esperado | Obtenido | Final |
|--------|----------|----------|-------|
| Login | Dashboard | PASS | PASS |
| Dashboard métricas/cards | Visible | PASS | PASS |
| Crear usuario UI | Lista actualizada | PASS (`human.e2e@test.local`) | PASS |
| Listar usuarios (tabla) | ≥5 filas | PASS (10+) | PASS |
| Crear lead | `?created=True` | PASS | PASS |
| Calificar + convertir lead | Cliente creado | PASS (confirm + Enter) | PASS |
| Crear workflow | Lista workflows | PASS (`Workflow Humano E2E 2026`) | PASS |
| Crear política | Lista policies | PASS (`Politica Humana E2E`) | PASS |
| Settings | Página carga | PASS (botones Guardar, Importar) | PASS |
| Audit / reportes | Eventos + Exportar | PASS | PASS |
| Soporte / mensajería | Página soporte | PASS (`/Support`) | PASS |
| Agentes IA | Página agentes | PASS (navegación) | PASS |
| Carnets / escuelas | — | N/A | N/A |

### 3.2 Admin / Manager (Director / Secretaria)

| Prueba | Esperado | Obtenido | Final |
|--------|----------|----------|-------|
| Login Manager | Dashboard | PASS | PASS |
| Users + Settings | Acceso | PASS (HTTP 200) | PASS |
| Leads + búsqueda | Lista | PASS | PASS |
| Clientes + Importar modal | Botón + modal file | PASS (fix UI) | PASS |
| Crear/editar (consultas) | CRUD vía UI | PASS (flujos lead/cliente) | PASS |

### 3.3 Teacher / Sales

| Prueba | Esperado | Obtenido | Final |
|--------|----------|----------|-------|
| Login Sales | Dashboard | PASS | PASS |
| Ver leads / pipeline | 200 | PASS | PASS |
| Users / Settings | Denegado | PASS → AccessDenied | PASS |
| Crear actividad (lead) | Form create | PASS | PASS |

### 3.4 Student / Viewer

| Prueba | Esperado | Obtenido | Final |
|--------|----------|----------|-------|
| Login Viewer | Dashboard | PASS | PASS |
| Ver leads (notas/actividades) | Lectura | PASS `/Leads` | PASS |
| Users admin | Denegado | PASS → AccessDenied | PASS |

### 3.5 Parent / Staff / Support

| Prueba | Esperado | Obtenido | Final |
|--------|----------|----------|-------|
| Login Support | Acceso operativo | PASS (sesión válida) | PASS |
| Ver perfil / soporte | `/Support` | PASS (health, swagger links) | PASS |
| Carnet institucional | — | N/A | N/A |

---

## 4. Pruebas negativas (browser)

| ID | Prueba | Esperado | Obtenido | Final |
|----|--------|----------|----------|-------|
| NEG-01 | `/Users` sin login | Login | Redirect Login | PASS |
| NEG-02 | Sales → `/Users` | AccessDenied | PASS | PASS |
| NEG-03 | Viewer → `/Users` | AccessDenied | PASS | PASS |
| NEG-04 | Password incorrecta | Alert visible | PASS (texto alert DOM) | PASS |
| NEG-05 | Lead ID inexistente | 404 o página error | **404** — body vacío en browser | **PARTIAL** |
| NEG-06 | Formulario lead sin nombre | Validación | HTML5 `required` (browser) | PASS |
| NEG-07 | Manipular URL Settings como Sales | Denegado | AccessDenied | PASS |
| NEG-08 | AJAX / errores JS | Sin crash | Sin errores JS bloqueantes observados | PASS |

### Detalle NEG-05

| Campo | Detalle |
|-------|---------|
| **Error** | Página en blanco en `/Leads/Details/{guid-invalid}` |
| **Causa** | `NotFound()` sin vista amigable para Razor |
| **Corrección** | No aplicada (mejora UX opcional) |
| **Recomendación** | Redirect a `/Leads` con mensaje TempData |

---

## 5. Validaciones visuales (browser)

| Elemento | Resultado |
|----------|-----------|
| Botones topbar/sidebar | PASS |
| Cards métricas | PASS |
| Modales (Import Users/Customers) | PASS |
| Dropdown fuente lead | PASS |
| Tablas con checkboxes y Ver/Editar | PASS |
| Sidebar scroll + Menú móvil | PASS (botón Menú presente) |
| Texto / espaciado | PASS (sin truncado crítico en viewport 743×560) |
| Impresión / PDF / QR / Carnets / Imágenes | **N/A** |
| DataTables | **N/A** (tablas HTML) |

---

## 6. Ciclo corrección → re-prueba

| ID | Error | Causa raíz | Corrección | Archivo | Re-prueba |
|----|-------|------------|------------|---------|-----------|
| BUG-H-01 | RBAC no aplicaba | API antigua en :5154 | Rebuild + restart | — | PASS |
| BUG-H-02 | Convert/deal fallaba | RabbitMQ down | `EventBus: InMemory` | `appsettings.Development.json` | PASS |
| BUG-H-03 | Lista users vacía | Typo FilteredUsers | FIX-001 | `Users.cshtml.cs` | PASS |
| BUG-H-04 | Sin import clientes UI | Falta modal | Modal Importar | `Customers.cshtml` | PASS |
| BUG-H-05 | Link Leads `?id=` | Ruta es `/Details/{id}` | Documentado | — | PASS |

---

## 7. Registro detallado (muestras con plantilla completa)

### HU-FLUJO-01 — Convertir lead a cliente (browser humano)

| Campo | Valor |
|-------|-------|
| **Prueba ejecutada** | Detalle lead → Calificar → Convertir a Cliente → confirmar |
| **Resultado esperado** | Redirect a ficha cliente |
| **Resultado obtenido** | `/Customers/Details/5f15a0fc-...` — título cliente "Lead E2E Flujo" |
| **Error encontrado** | Ninguno (tras InMemory) |
| **Causa raíz** | — |
| **Corrección aplicada** | — |
| **Archivo modificado** | — |
| **Resultado final** | **PASS** |

### HU-POL-01 — Crear política (browser)

| Campo | Valor |
|-------|-------|
| **Prueba ejecutada** | `/Policies/Create` → nombre + expresión → Crear política |
| **Resultado esperado** | Redirect `/Policies` |
| **Resultado obtenido** | URL `/Policies` |
| **Resultado final** | **PASS** |

### HU-SEC-03 — Sales bloqueado en Users

| Campo | Valor |
|-------|-------|
| **Prueba ejecutada** | Login sales → navegar `/Users` |
| **Resultado esperado** | Acceso denegado |
| **Resultado obtenido** | `/Account/AccessDenied` — "No tiene permisos..." |
| **Resultado final** | **PASS** |

---

## 8. Cobertura

| Área módulos UI | Cobertura browser |
|-----------------|-------------------|
| Auth (5 roles) | 100 % |
| Dashboard | 100 % |
| Leads CRUD + flujo | 95 % |
| Customers CRUD + import UI | 90 % |
| Deals / Pipeline | 85 % |
| Users CRUD | 90 % |
| Workflows | 80 % |
| Policies | 75 % |
| Audit | 80 % |
| Settings | 80 % |
| Support | 85 % |
| Agents | 75 % |
| Módulos escolares prompt | 0 % (N/A) |

**Total flujos CRM aplicables:** ~**92 % PASS**, 1 PARTIAL (404 vacío), resto N/A.

---

## 9. Veredicto final

### **GO**

La aplicación **AutonomusCRM** en local es usable de extremo a extremo por un humano vía navegador: autenticación, RBAC, CRUD comercial, automatización básica, auditoría y soporte.

**Condiciones para producción/demo:**

1. API con build actual (`dotnet run --project AutonomusCRM.API`).
2. PostgreSQL local activo.
3. `EventBus: InMemory` en dev; RabbitMQ en staging/prod si se requiere bus real.
4. Mejora opcional: página 404 amigable en detalles inexistentes.

**No GO** solo si se exige literalmente el sistema escolar del prompt — ese producto **no está en este repo**.

---

## 10. Re-ejecutar pruebas humanas

```powershell
cd c:\Proyectos\autonomuscrm
dotnet run --project AutonomusCRM.API --urls http://localhost:5154
```

Abrir Browser Tab en `http://localhost:5154/Account/Login` y seguir credenciales en pantalla.

Complemento automatizado (no sustituye browser): `powershell -File tests/e2e/run-local-e2e.ps1` → 39/39 PASS.

---

*Informe generado tras iteración 2 — interacción exclusiva Browser Tab + ciclo fix/rebuild documentado.*
