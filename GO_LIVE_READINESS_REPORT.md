# GO LIVE READINESS REPORT

**Proyecto:** AutonomusCRM — Primer cliente real (TechSolutions Panamá)  
**Fecha:** 2026-05-28  
**Escenario:** BD vacía, sin demo, sin seeds, sin shortcuts  
**Método:** Análisis código + simulación lógica + evidencia entorno reset (Seed=false)

---

## Veredicto final

### ¿Puede operar mañana con BD vacía?

| Pregunta | Respuesta |
|----------|-----------|
| ¿Puede operar? | ⚠️ **Sí, con soporte ops** — no self-service |
| ¿Puede crear usuarios? | ✅ Sí, tras provisioning + Admin UI |
| ¿Puede crear clientes? | ✅ Sí, manualmente (Admin/Manager/Sales) |
| ¿Puede vender? | ✅ Sí, pipeline deals manual |
| ¿Puede usar IA? | ⚠️ Solo con API key externa configurada |
| ¿Puede usar automatizaciones? | ⚠️ Parcial — Assign/UpdateStatus/CreateTask OK; Communicate/Agent solo log |
| ¿Puede administrar el sistema? | ✅ Sí, rol Admin (no SuperAdmin) |
| ¿Puede trabajar sin soporte técnico? | ❌ **No el día 1** — requiere provisioning API, roles manuales, config secretos |

### Estado global

| Categoría | Estado |
|-----------|--------|
| **Listo** | ✅ CRM manual, RBAC UI, migraciones, provisioning API, empty states |
| **Riesgos** | ⚠️ 12 ítems (ver sección 3) |
| **Bloqueantes** | ❌ 5 ítems (ver sección 2) |

**Go/No-Go para venta mañana:** 🟡 **CONDITIONAL GO** — viable con runbook ops y expectativas alineadas. **NO-GO** para cliente que exija zero-touch onboarding o SuperAdmin.

---

## 1. Escenarios E2E — 20 pruebas simuladas

> **Leyenda:** ✅ Esperado OK · ⚠️ Parcial/degradado · ❌ Bloqueado · 🔧 Requiere intervención ops

| # | Escenario | Resultado | Evidencia / Notas |
|---|-----------|-----------|-------------------|
| **1** | Instalación desde cero | ⚠️ | API arranca con config; login imposible sin provisioning |
| **2** | Creación tenant | ✅ | `POST /api/provisioning/tenants` + platform key |
| **3** | Creación usuarios | ⚠️ | UI/API OK; rol debe asignarse manualmente post-create |
| **4** | Login | ✅ | Post-provisioning; multi-tenant Prod riesgo |
| **5** | Permisos | ⚠️ | UI middleware OK; API comercial sin filtro rol |
| **6** | Lead nuevo | ✅ | Admin/Manager/Sales; empty state día 1 |
| **7** | Lead → Cliente | ✅ | Flujo manual qualify + convert |
| **8** | Cliente → Oportunidad | ✅ | `/Deals/Create` |
| **9** | Oportunidad → Venta | ✅ | Stage → Closed Won |
| **10** | Trust Studio | ⚠️ | Cola vacía sin IA; HITL requiere audits + score ≥70 |
| **11** | Workflow | ⚠️ | CRUD OK; Communicate/ActivateAgent no ejecutan |
| **12** | Tasks | ✅ | Via workflow CreateTask o manual |
| **13** | Customer Success | ⚠️ | Funcional; vacío sin tickets |
| **14** | Revenue OS | ⚠️ | Empty hasta pipeline; Sales home OK |
| **15** | Executive OS | ⚠️ | Empty hasta datos; Admin/Manager home OK |
| **16** | Audit | ✅ | Registra tras actividad; vacío día 1 |
| **17** | Billing | ✅ | Plan free auto-creado; Stripe opcional |
| **18** | Integrations | ⚠️ | UI OK; requiere OAuth secrets + encryption key |
| **19** | Memory | ⚠️ | Vacío; requiere actividad + Workers + opcional LLM embeddings |
| **20** | Workers | ⚠️ | Requieren RabbitMQ + proceso separado; reglas sí, LLM no |

---

## 2. Bloqueantes ❌

| ID | Bloqueante | Impacto | Mitigación (ops, no código) |
|----|------------|---------|------------------------------|
| **B1** | Sin `Provisioning:ApiKey` en BD vacía → login imposible | Cliente no entra al sistema | Configurar key + script bootstrap |
| **B2** | No hay wizard / self-service onboarding | Cliente depende de ops | Runbook + llamada provisioning |
| **B3** | VPS `Seed__Enabled=true` hardcodeado | Contamina con demo en deploy | Override env `Seed__Enabled=false` |
| **B4** | SuperAdmin no existe (escenario pide 1) | Confusión contractual | Documentar Admin = máximo rol |
| **B5** | Usuarios creados sin rol por defecto | Permisos rotos hasta Edit | Procedimiento: siempre asignar rol |

---

## 3. Riesgos ⚠️

| ID | Riesgo | Severidad | Notas |
|----|--------|-----------|-------|
| **R1** | Multi-tenant login Prod usa primer tenant oculto | Alta | Usuarios tenant 2+ pueden fallar login |
| **R2** | API POST comercial sin filtro rol | Alta | Support/Viewer podrían escribir vía API |
| **R3** | System Settings no persisten | Media | Cambios se pierden al reiniciar |
| **R4** | Workflow Communicate/ActivateAgent solo log | Media | Promesa de automatización incompleta |
| **R5** | IA requiere LLM externo | Media | `LlmNotConfiguredException` sin key |
| **R6** | Workers no desplegados | Media | Sin agentes background |
| **R7** | ABAC vacío = allow all | Media | Riesgo si Autonomous:Enabled=true |
| **R8** | Email real requerido si AllowSimulation=false | Media | Guard bloquea arranque VPS default |
| **R9** | Trust HITL requiere humanos | Baja | No 100% autónomo — esperado |
| **R10** | Documentación menciona SuperAdmin | Baja | Alinear expectativas |
| **R11** | `POST /api/tenants` no crea admin (docs incorrectos) | Media | Usar solo provisioning |
| **R12** | Provisioning sin key → cualquier autenticado puede provisionar | Alta | Siempre setear API key |

---

## 4. Listo ✅

| Capacidad | Estado |
|-----------|--------|
| Migraciones EF en BD vacía | ✅ |
| Fail-fast Production guard | ✅ |
| Provisioning tenant + Admin | ✅ |
| RBAC 5 roles en UI | ✅ |
| Commercial write middleware UI | ✅ |
| Empty states (Leads, Command, Trust) | ✅ |
| Billing lazy account free | ✅ |
| Health checks PG/Redis/RabbitMQ | ✅ |
| Docker + VPS templates | ✅ |
| Trial 14 días en provisioning | ✅ |
| CRM manual completo (L→C→D→Won) | ✅ |
| Audit trail (post-actividad) | ✅ |

---

## 5. Respuestas directas al cliente

### ¿Qué impediría una implementación real exitosa?

1. **Ops no ejecuta provisioning** antes de entregar credenciales al cliente
2. **Seed habilitado** en VPS → datos demo mezclados con producción
3. **Roles no asignados** tras crear usuarios → equipo bloqueado
4. **Expectativa de IA/automatización completa** sin API keys ni Workers
5. **Expectativa de SuperAdmin** o onboarding self-service
6. **Multi-tenant** en mismo host sin resolver login por email
7. **Email/Stripe/Integraciones** prometidas sin configurar secretos
8. **ABAC restrictivo** esperado pero ninguna policy creada

### Costo de soporte día 1 (estimado)

| Actividad | Quién | Tiempo est. |
|-----------|-------|-------------|
| Deploy infra + secretos | Ops/DevOps | 2–4 h |
| Provisioning tenant | Ops | 15 min |
| Crear 6 usuarios + roles | Admin cliente (guiado) | 1–2 h |
| Primer lead/deal | Cliente | 30 min |
| Config IA (opcional) | Ops | 1 h |
| Config email/Stripe (opcional) | Ops | 2–4 h |

---

## 6. Checklist Go-Live (pre-venta mañana)

### Infraestructura
- [ ] PostgreSQL + Redis + RabbitMQ healthy
- [ ] `Seed__Enabled=false` confirmado en runtime (no solo appsettings)
- [ ] `PROVISIONING_API_KEY` generado
- [ ] `JWT_KEY` ≥ 32 chars
- [ ] `INTEGRATION_ENCRYPTION_KEY` configurado
- [ ] Workers desplegados (si se venden agentes)

### Bootstrap
- [ ] Script/curl provisioning documentado
- [ ] Admin `admin@techsolutions.pa` creado y probado
- [ ] 7 usuarios con roles asignados (matriz ROLE_TEST_MATRIX)
- [ ] Login verificado por cada rol

### Funcional
- [ ] 1 lead → customer → deal → won creado sin demo
- [ ] Permisos Support/Viewer verificados (no escritura UI)
- [ ] Executive/Revenue muestran data real (no CEO_DEMO)
- [ ] Workflows creados si contratados
- [ ] Policies creadas si ABAC contratado

### Expectativas
- [ ] Cliente informado: no SuperAdmin
- [ ] Cliente informado: IA opcional
- [ ] Cliente informado: HITL en Trust
- [ ] Cliente informado: Communicate/ActivateAgent no operativos

---

## 7. Documentos generados (Harvard QA)

| Documento | Fase | Archivo |
|-----------|------|---------|
| Requisitos instalación | 1 | `FIRST_CLIENT_INSTALLATION_REQUIREMENTS.md` |
| Dependencia demo | 2 | `DEMO_DEPENDENCY_REPORT.md` |
| Instalación limpia | 3 | `CLEAN_INSTALLATION_REPORT.md` |
| Bootstrap cliente | 4 | `FIRST_CUSTOMER_BOOTSTRAP_GUIDE.md` |
| Matriz roles | 5 | `ROLE_TEST_MATRIX.md` |
| Go-Live readiness | 6 | `GO_LIVE_READINESS_REPORT.md` (este) |

---

## 8. Prioridades post-descubrimiento (NO implementadas — backlog)

| Prioridad | Item |
|-----------|------|
| P0 | `Seed__Enabled=false` default en VPS compose |
| P0 | Wizard o página `/Setup` para primer tenant |
| P0 | Asignar rol en `CreateUserCommand` |
| P1 | Resolver login multi-tenant por email |
| P1 | Filtro rol en API comercial POST |
| P1 | Persistir System Settings en BD |
| P2 | Separar seed demo de seed mínimo |
| P2 | Implementar Communicate/ActivateAgent en WorkflowEngine |
| P3 | Rol platform-admin o documentar definitivamente Admin-only |

---

## 9. Referencia cruzada

- Instalación: `FIRST_CLIENT_INSTALLATION_REQUIREMENTS.md`
- Demo: `DEMO_DEPENDENCY_REPORT.md`
- Limpia: `CLEAN_INSTALLATION_REPORT.md`
- Bootstrap: `FIRST_CUSTOMER_BOOTSTRAP_GUIDE.md`
- Roles: `ROLE_TEST_MATRIX.md`
- Auditoría previa: `GO_LIVE_AUDIT.md` (scores técnicos — complementario)

---

**Firma análisis:** Harvard QA Prompt — Fases 1–6 completadas por análisis de código. Sin correcciones ni features nuevas aplicadas en esta fase.
