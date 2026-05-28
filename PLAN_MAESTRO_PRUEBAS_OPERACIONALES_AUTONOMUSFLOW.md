# Plan maestro de pruebas operacionales — AutonomusFlow

| Campo | Valor |
|-------|-------|
| **Producto** | AutonomusFlow (AutonomusCRM) |
| **Versión documental** | 1.0 |
| **Fecha** | 2026-05-27 |
| **Fuente de verdad funcional** | `ANALISIS_PREMIUM_PROCESOS_AUTONOMUSFLOW.md` |
| **Modo actual** | **Fase 1 — Documentación** (sin ejecución ni cambios código/BD) |
| **Fase 2** | Ver `PLAN_ESTABILIZACION_QA.md` |

---

## 1. Propósito

Definir una estrategia **enterprise** de pruebas funcionales humanas y operacionales que simule el uso real de un CRM SaaS multi-tenant: no validación de botones aislados, sino **procesos de negocio completos** con roles, datos creíbles, trazabilidad, seguridad y recuperación.

---

## 2. Principios de diseño de pruebas

| # | Principio | Implicación |
|---|-----------|-------------|
| 1 | **Proceso antes que pantalla** | Cada caso cierra un objetivo de negocio (ej. “cerrar oportunidad ganada”) |
| 2 | **Rol explícito** | Misma acción se prueba con Sales vs Viewer (resultado debe diferir) |
| 3 | **No inventar alcance** | Contactos, tareas, campañas, email → casos marcados **N/A** o **GAP conocido** |
| 4 | **Evidencia obligatoria** | Captura, URL, export, log, fila BD (cuando aplique) |
| 5 | **Brechas del análisis = expectativa documentada** | Audit UI, workflows sin acciones, agentes sin Worker |
| 6 | **Datos aislados por corrida** | Prefijo `QA-{fecha}-` en entidades creadas |
| 7 | **Repetibilidad** | Orden de ejecución con dependencias en §8 |

---

## 3. Alcance y exclusiones

### 3.1 En alcance

- UI Razor (43 rutas)
- API REST documentada en análisis premium
- RBAC 5 roles + middleware comercial
- Event Store (escritura y validación export/SQL)
- Worker + RabbitMQ (escenarios condicionales)
- Importación CSV/JSON
- Multi-tenant (limitado por brecha B15)
- UX usuario no técnico (criterios heurísticos)

### 3.2 Fuera de alcance (no existe — no se diseñan casos “verdes”)

| Capacidad | Referencia análisis |
|-----------|---------------------|
| Módulo Contactos | §4.3 B01 |
| Tareas / actividades / recordatorios | §4.3 B02 |
| Campañas marketing | §4.3 B10 |
| Email/SMS saliente | §4.3 B09, P10 |
| Tickets soporte cliente | Support = health only |
| Reportes analíticos dedicados | §4.3 B11 |

### 3.3 Parcial — casos con resultado esperado “limitado”

| Capacidad | Comportamiento esperado en prueba |
|-----------|----------------------------------|
| Workflows | Definición OK; acciones sin efecto (B03) |
| Policies | CRUD OK; sin efecto runtime (B04) |
| Agents UI | Config guarda; score solo con Worker+RabbitMQ (B07) |
| Audit UI | Export puede estar vacío; lista UI vacía (B06) |
| API GET by id | Stub `{ id }` (B14) |
| Botones `alert(próximamente)` | No funcional (B13) |

---

## 4. Entorno y datos de prueba

### 4.1 Entornos

| Entorno | URL | Uso |
|---------|-----|-----|
| **Local DEV** | `http://localhost:5154` | Ejecución principal Fase 2 |
| **VPS staging** | `http://164.68.99.83:8091` | Smoke post-deploy |
| **API** | Mismo host + `/api/*`, Swagger en dev | Seguridad e integración |

### 4.2 Tenant y credenciales (seed demo)

| Rol | Email | Password | Tenant |
|-----|-------|----------|--------|
| Admin | `admin@autonomuscrm.local` | `Admin123!` | Autocompletado en login |
| Manager | `manager@autonomuscrm.local` | `Manager123!` | Igual |
| Sales | `sales@autonomuscrm.local` | `Sales123!` | Igual |
| Support | `support@autonomuscrm.local` | `Support123!` | Igual |
| Viewer | `viewer@autonomuscrm.local` | `Viewer123!` | Igual |

### 4.3 Empresa simulada — TechNova Solutions

| Atributo | Valor |
|----------|-------|
| **Industria** | Software B2B / integración cloud |
| **Mercados** | Panamá, Costa Rica, Colombia |
| **Tenant lógico** | TechNova (demo seed o tenant dedicado QA) |

**Personas comerciales (ficticias):**

| Nombre | Rol en prueba | Email interno |
|--------|---------------|---------------|
| Carolina Méndez | Sales ejecutiva | `carolina.mendez@technova.test` |
| Roberto Solís | Manager regional | `roberto.solis@technova.test` |
| Ana Villalobos | Admin operaciones | (usa admin demo) |

**Leads de muestra (crear en corrida):**

| ID lógico | Nombre | Empresa | Fuente | Estado inicial |
|-----------|--------|---------|--------|----------------|
| L-QA-01 | Diego Ramírez | Finanzas del Istmo SA | Website | New |
| L-QA-02 | Laura Castillo | Retail Pacífico | Referral | New |
| L-QA-03 | (CSV bulk) | 10 filas mixtas | EmailCampaign | variado |

**Clientes / deals:**

| Deal | Cliente | Monto USD | Etapa inicial |
|------|---------|-----------|---------------|
| Implementación CRM Q1 | Finanzas del Istmo | 25,000 | Qualification |
| Renovación anual | Retail Pacífico | 48,000 | Proposal |

### 4.4 Archivos de importación

| Archivo | Contenido | Uso |
|---------|-----------|-----|
| `qa-leads-valid.csv` | 5 leads válidos | IMP-positivo |
| `qa-leads-invalid.csv` | emails mal formados, nombre vacío | IMP-negativo |
| `qa-leads-duplicate.json` | duplicados email | IMP-recuperación |

*(Generar en Fase 2 en `tests/qa-data/`)*

---

## 5. Clasificación de prioridades

| Prioridad | Definición | % aprox. casos | Criterio GO LIVE |
|-----------|------------|----------------|------------------|
| **P0** | Bloquea operación comercial o seguridad crítica | 35% | **100% PASS** obligatorio |
| **P1** | Alto impacto negocio o compliance | 30% | ≥95% PASS; 0 FAIL en seguridad |
| **P2** | Medio; workaround existe | 25% | ≥85% PASS documentado |
| **P3** | Bajo; cosmético o huérfano | 10% | Best effort |

### 5.1 Criterios P0 (mínimo GO LIVE piloto)

1. Login/logout todos los roles (AUTH-* P0)
2. Flujo dorado Sales: Lead → Calificar → Cliente → Deal → Cerrar (E2E-001)
3. Viewer/Support bloqueados en POST comercial (SEC-* P0)
4. Importación lead válida (IMP-001)
5. Manager supervisa pipeline sin editar usuarios ajenos (ROL-Manager)
6. Usuario Admin crea Sales (USR-001)
7. Health `/Support` y `/health` (OPS-001)
8. No regresión tenant claim en páginas principales (TEN-001)

**NO bloqueante GO piloto (pero documentar FAIL):** Audit lista, workflow acciones, agentes sin worker, multi-tenant API cross-id.

---

## 6. Tipos de escenario (15 categorías)

| Cat. | Código | Descripción | Cant. casos ref. |
|------|--------|-------------|------------------|
| 1 | `ROL` | Por rol | 25 |
| 2 | `PROC` | Por proceso de negocio | 30 |
| 3 | `E2E` | Cadena completa | 12 |
| 4 | `POS` | Positivos | (transversal) |
| 5 | `NEG` | Negativos | 15 |
| 6 | `HUM` | Error humano | 10 |
| 7 | `MULTI` | Multiusuario secuencial | 8 |
| 8 | `CONC` | Concurrencia | 5 |
| 9 | `SEC` | Seguridad / RBAC | 20 |
| 10 | `TEN` | Aislamiento tenant | 8 |
| 11 | `TRZ` | Trazabilidad / audit | 10 |
| 12 | `REC` | Recuperación | 8 |
| 13 | `DAT` | Datos corruptos | 8 |
| 14 | `AUT` | Automatización | 12 |
| 15 | `UX` | Usuario no técnico | 10 |

**Detalle de cada caso:** `CASOS_PRUEBA_E2E_AUTONOMUSFLOW.md`

---

## 7. Matrices (resumen — detalle en archivos dedicados)

### 7.1 Cobertura proceso × módulo

| Proceso | Login | Dash | Leads | Cust | Deals | Users | WF | Pol | Agt | Set | Aud | API |
|---------|:-----:|:----:|:-----:|:----:|:-----:|:-----:|:--:|:---:|:---:|:---:|:---:|:---:|
| P01 Login | ● | | | | | | | | | | | | ○ |
| P05 Leads | ○ | ○ | ● | | | | | | ○ | | | ○ |
| P06 Conversión | | | ● | ● | ○ | | | | | | | | |
| P07 Deals | | ○ | | ○ | ● | | | | | | | ○ |
| P09 Automatización | | | ○ | ○ | ○ | | ◐ | ◐ | ◐ | | ◐ | |
| P13 Auditoría | | | | | | | | | | | ◐ | | ○ |

**Leyenda:** ● completo | ◐ parcial | ○ no aplica / N/A

### 7.2 Riesgo × prioridad

Ver `MATRIZ_RIESGOS_OPERACIONALES.md` (R01–R20).

### 7.3 Rol × funcionalidad

Ver `MATRIZ_ROLES_PERMISOS.md`.

---

## 8. Plan de ejecución (orden recomendado)

### Ola 0 — Preparación (1 día)

| Orden | Actividad | Responsable |
|-------|-----------|-------------|
| 0.1 | Verificar API + PostgreSQL + seed | QA |
| 0.2 | Crear carpeta evidencias `tests/qa-evidence/{fecha}/` | QA |
| 0.3 | Generar CSV/JSON en `tests/qa-data/` | QA |
| 0.4 | (Opcional) Segundo tenant `QA-Tenant-B` solo para TEN-* | DevOps |

### Ola 1 — P0 fundación (día 1–2)

```text
AUTH (todos roles) → OPS-001 → TEN-001 → E2E-001 (Sales flujo dorado)
→ SEC-Viewer-POST → SEC-Support-POST → IMP-001
```

### Ola 2 — P0/P1 comercial (día 3–4)

```text
PROC-Leads* → PROC-Conversion* → PROC-Deals* → ROL-Manager pipeline
→ USR-* → NEG-* credenciales
```

### Ola 3 — P1 seguridad y API (día 5)

```text
SEC-* tokens → API-* → TEN-* (si 2 tenants)
```

### Ola 4 — P1/P2 automatización y trazabilidad (día 6–7)

```text
AUT-* (Worker+RabbitMQ corrida separada) → TRZ-* → AUD-*
Documentar FAIL esperados B06, B03, B07
```

### Ola 5 — P2/P3 UX, concurrencia, recuperación (día 8–9)

```text
UX-* → CONC-* → REC-* → DAT-*
```

### Ola 6 — Regresión y veredicto (día 10)

| Entregable | Criterio |
|------------|----------|
| Informe ejecución | Actualizar `Resultado observado` y `Estado` en cada caso |
| Dashboard QA | % PASS por prioridad |
| Veredicto | GO / GO condicionado / NO GO |

---

## 9. Funcionalidades bloqueantes (pre-ejecución)

Según análisis premium — **asumir FAIL o BLOCKED hasta corrección:**

| ID | Bloqueante | Impacto GO SaaS | Impacto GO piloto |
|----|------------|-----------------|-------------------|
| B06 | Audit UI sin eventos | Alto | Medio |
| B15 | SameTenant incompleto | Crítico | Bajo (1 tenant) |
| B03 | Workflow sin acciones | Alto | Bajo |
| B07 | Agentes sin Worker | Alto | Medio |
| B02 | Sin tareas | Alto | Medio (proceso ventas) |

---

## 10. Evidencia estándar por tipo de caso

| Tipo | Evidencia mínima |
|------|------------------|
| UI flujo | Captura pantalla + URL + timestamp |
| RBAC negativo | Captura 403 / redirect AccessDenied / mensaje middleware |
| API | Request/response (Postman/curl) + status code |
| Trazabilidad | Export JSON Audit + query `SELECT` en `DomainEvents` |
| Import | Archivo fuente + captura listado post-import |
| Concurrencia | Dos navegadores + estado final entidad |
| UX | Nota cualitativa escala 1–5 + fricción descrita |

---

## 11. Roles y responsabilidades

| Rol QA | Responsabilidad |
|--------|-----------------|
| **QA Lead** | Plan, veredicto, matrices riesgo |
| **Tester funcional** | Ejecución humano E2E UI |
| **Tester API** | SEC, TEN, API |
| **Dev support** | Worker/RabbitMQ, SQL event store |
| **Product owner** | Aceptación GO condicionado |

---

## 12. Relación con documentos existentes

| Documento | Relación |
|-----------|----------|
| `CASOS_PRUEBA_FUNCIONALES_E2E_AUTONOMUSCRM.md` | Casos TechNova amplios; **este plan los supersede en gobernanza** |
| `RESULTADOS_PRUEBAS_E2E_FINAL.md` | Evidencia histórica local; no sustituye nueva corrida |
| `ANALISIS_PREMIUM_PROCESOS_AUTONOMUSFLOW.md` | Fuente brechas y veredicto GO condicionado |

---

## 13. Veredicto esperado pre-ejecución

| Escenario | Expectativa |
|-----------|-------------|
| Piloto comercial 1 tenant | **GO condicionado** tras Ola 1–2 PASS |
| SaaS multi-tenant + audit + automatización | **NO GO** hasta cerrar B06, B15, B03 |
| Fase 2 ejecución | Ver `PLAN_ESTABILIZACION_QA.md` |

---

*Fin del plan maestro — Fase 1 documentación completa.*
