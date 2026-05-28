# Matriz de riesgos operacionales — AutonomusFlow

| Fuente | `ANALISIS_PREMIUM_PROCESOS_AUTONOMUSFLOW.md` §9–12 |
| Fecha | 2026-05-27 |

---

## 1. Matriz de riesgos (R01–R20)

| ID | Riesgo | Categoría | Prob. | Impacto | Prioridad | Brecha | Casos QA | Mitigación / estado |
|----|--------|-----------|:-----:|:-------:|:---------:|:------:|----------|---------------------|
| **R01** | Auditoría UI no muestra eventos reales | Compliance | M | **Alto** | P0 | B06 | TRZ-001–010 | Corregir `EventStore.DeserializeEvents` |
| **R02** | Cross-tenant vía API (mismo ID otro tenant) | Seguridad | M | **Crítico** | P0 | B15 | TEN-003, TEN-004 | SameTenant + validación handlers |
| **R03** | Workflow guardado pero sin acción de negocio | Automatización | **Alta** | Alto | P1 | B03 | AUT-WF-003–006 | Implementar acciones o ocultar UI |
| **R04** | Usuario cree que IA trabaja sin Worker | Operacional | **Alta** | Medio | P1 | B07, B08 | AUT-AG-002–004 | Banner + health Worker |
| **R05** | Ventas sin tareas/recordatorios | Negocio | **Alta** | Alto | P1 | B02 | N/A (gap) | Módulo Activity |
| **R06** | Botones `próximamente` en producción | UX / confianza | M | Medio | P2 | B13 | UX-* | Eliminar o implementar |
| **R07** | Import CSV datos inválidos en BD | Datos | M | Medio | P1 | — | DAT-001–004 | Validación import |
| **R08** | Concurrencia último-write-wins en deal | Operacional | M | Medio | P2 | — | CONC-001–003 | RowVersion |
| **R09** | MFA bloqueado solo en API | Seguridad | B | Medio | P2 | B16 | SEC-MFA-01 | UI MFA o desactivar demo |
| **R10** | Event bus InMemory en dev vs RabbitMQ prod | Técnico | **Alta** | Alto | P1 | B07 | AUT-AG-005 | Documentar arranque |
| **R11** | PolicyEngine nunca evalúa | Gobernanza | **Alta** | Medio | P2 | B04 | AUT-POL-002 | Conectar dispatcher |
| **R12** | Export audit vacío `[]` | Compliance | M | Alto | P1 | B06 | TRZ-005 | Misma que R01 |
| **R13** | Dashboard huérfano `/Dashboard` | UX | B | Bajo | P3 | B12 | NAV-003 | Redirect a Index |
| **R14** | API GET stub engaña integradores | Integración | M | Medio | P2 | B14 | API-003–005 | Implementar queries |
| **R15** | Sin módulo contactos B2B | Negocio | **Alta** | Medio | P2 | B01 | N/A | Roadmap |
| **R16** | Sin email post-lead | Negocio | M | Medio | P2 | B09 | AUT-COM-001 | CommunicationAgent |
| **R17** | Stats audit hardcodeados | UX / compliance | M | Alto | P1 | B06 | TRZ-002 | Quitar mock UI |
| **R18** | Un solo tenant en demo | Multi-tenant | **Alta** | Medio | P1 | — | TEN-001–002 | Segundo tenant QA |
| **R19** | Sesión expirada sin mensaje claro | UX | M | Bajo | P2 | — | SEC-SES-01 | Mejorar redirect login |
| **R20** | Rate limit 200/min en pruebas masivas | Técnico | B | Bajo | P3 | — | API-LOAD-01 | Excluir en dev |

---

## 2. Matriz riesgo × proceso

| Proceso | Riesgos aplicables |
|---------|-------------------|
| P01 Login | R09, R19 |
| P05 Leads | R07, R04 |
| P06 Conversión | R01 (eventos) |
| P07 Deals | R08 |
| P09 Automatización | R03, R04, R10, R11 |
| P13 Auditoría | R01, R12, R17 |
| Multi-tenant | R02, R18 |
| UX general | R06, R13 |

---

## 3. Matriz riesgo × severidad de defecto (escala QA)

| Severidad | Definición | Ejemplo |
|-----------|------------|---------|
| **S1 Crítica** | Pérdida datos, seguridad, bloqueo total | Cross-tenant data leak |
| **S2 Alta** | Proceso core no completable | No convertir lead |
| **S3 Media** | Workaround existe | Audit vacío pero SQL ok |
| **S4 Baja** | Cosmético | Botón alert historial |

---

## 4. Riesgos de producción (GO / NO GO)

| Condición GO piloto (1 tenant) | Riesgos aceptables | Riesgos inaceptables |
|------------------------------|--------------------|-----------------------|
| Flujo ventas 30 días | R03, R04, R06, R13 | R02 (si 2 tenants) |
| GO SaaS | Ninguno P0 abierto | R01, R02, R03 |

---

## 5. Heatmap (probabilidad × impacto)

```text
Impacto →
        Bajo    Medio    Alto    Crítico
Prob
Alta    R13     R04,R10  R03,R05  —
Media   R19     R06,R07  R01,R17  R02
Baja    R20     R09      R14      —
```

---

## 6. Vinculación a plan de estabilización

| Riesgo | Fase estabilización |
|--------|---------------------|
| R01, R12, R17 | QA Fase A — Audit |
| R02 | QA Fase A — SameTenant |
| R03, R11 | QA Fase B — Automatización |
| R04, R10 | QA Fase B — Worker |
| R06 | QA Fase C — UX placeholders |

Ver `PLAN_ESTABILIZACION_QA.md`.

---

*Actualizar columna “Estado mitigación” tras cada sprint.*
