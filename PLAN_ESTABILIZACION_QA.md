# Plan de estabilización QA — Fase 2 ejecución

| Campo | Valor |
|-------|-------|
| **Prerequisito** | Fase 1 documentación completa (5 archivos) |
| **Estado** | **PENDIENTE DE EJECUCIÓN** — no iniciar cambios hasta aprobación |
| **Regla usuario** | Ejecutar → evidenciar → corregir → re-probar → no detener hasta estabilizar |

---

## 1. Objetivo Fase 2

Ejecutar **caso por caso** el catálogo `CASOS_PRUEBA_E2E_AUTONOMUSFLOW.md`, documentar resultados reales, abrir defectos, **corregir código** donde corresponda, y repetir hasta cumplir criterios GO del `PLAN_MAESTRO_PRUEBAS_OPERACIONALES_AUTONOMUSFLOW.md`.

---

## 2. Precondiciones de arranque

| # | Check | Comando / acción |
|---|-------|------------------|
| 1 | PostgreSQL activo | `pg_isready` |
| 2 | API local | `dotnet run` proyecto API, puerto 5154 |
| 3 | (Opcional AUT) Worker + RabbitMQ | `docker compose` + Workers |
| 4 | Carpeta evidencia | `tests/qa-evidence/YYYY-MM-DD/` |
| 5 | CSV QA | `tests/qa-data/` creados |
| 6 | `stop-dev-api.ps1` si puerto ocupado | Script repo |

---

## 3. Ciclo de vida de un defecto

```text
Ejecutar caso → Estado PASS/FAIL/BLOCKED
  → FAIL: registrar en ERRORES_QA_{fecha}.md (ID, severidad, pasos, evidencia)
  → Priorizar P0/P1
  → Fix en rama (sin migraciones salvo aprobación)
  → Re-ejecutar caso + regresión P0 afectados
  → Cerrar cuando PASS + evidencia
```

---

## 4. Fases de estabilización (orden)

### Fase A — Bloqueantes P0 (semana 1)

| Orden | Acción técnica | Casos que deben pasar después |
|-------|----------------|------------------------------|
| A.1 | Implementar deserialización Event Store | TRZ-001, TRZ-005, AUD-* |
| A.2 | Completar SameTenantHandler | TEN-003, TEN-004 |
| A.3 | Verificar middleware comercial todos POST | SEC-V-*, SEC-S-* |
| A.4 | Enlazar botón Users → `/Users/Roles` o quitar alert | UX-U-01 |

**Criterio salida Fase A:** 100% P0 PASS excepto multi-tenant si solo 1 tenant.

### Fase B — Automatización creíble (semana 2)

| Orden | Acción | Casos |
|-------|--------|-------|
| B.1 | WorkflowEngine: `UpdateStatus` mínimo | AUT-WF-004 |
| B.2 | Documentar + script API+Worker+RabbitMQ | AUT-AG-005 |
| B.3 | Agents UI: indicador Worker offline | AUT-AG-002 |
| B.4 | PolicyEngine en dispatcher O ocultar módulo | AUT-POL-003 |

### Fase C — UX y datos (semana 3)

| Orden | Acción | Casos |
|-------|--------|-------|
| C.1 | Eliminar/reemplazar alerts placeholder | UX-*, Deals, Customers |
| C.2 | Validación import CSV | DAT-* |
| C.3 | Redirect `/Dashboard` → `/` | NAV-003 |
| C.4 | Mensajes sesión expirada | SEC-SES-01 |

### Fase D — Regresión completa (semana 4)

- Re-ejecutar 100% P0 + P1
- Generar `RESULTADOS_EJECUCION_AUTONOMUSFLOW_{fecha}.md`
- Veredicto GO / GO condicionado / NO GO

---

## 5. Reglas durante correcciones

| Permitido | No permitido (sin aprobación explícita) |
|-----------|----------------------------------------|
| Código aplicación/API/Infra | Migraciones BD destructivas |
| Tests automatizados | Cambiar seed producción |
| CSS/UX menores | Inventar módulos nuevos (tareas) salvo plan producto |
| Fixes SameTenant, Audit | Force push main |

---

## 6. Plantilla registro ejecución (por caso)

```markdown
### {ID} — {Nombre}
- **Estado:** PASS | FAIL | BLOCKED | SKIP
- **Ejecutor:** 
- **Fecha/hora:** 
- **Entorno:** local | VPS
- **Resultado observado:** 
- **Defecto:** DEF-xxx (si FAIL)
- **Evidencia:** tests/qa-evidence/.../CAP-xxx.png
```

---

## 7. Criterios de “sistema estabilizado”

| Métrica | Umbral |
|---------|--------|
| P0 | 100% PASS |
| P1 | ≥ 95% PASS; FAIL documentados aceptados por PO |
| P0 re-apertura | 0 en 48h post-fix |
| Regresión E2E-001 | 3 PASS consecutivos |
| Defectos S1 abiertos | 0 |

---

## 8. Entregables Fase 2

| Archivo | Contenido |
|---------|-----------|
| `RESULTADOS_EJECUCION_AUTONOMUSFLOW_{fecha}.md` | Resumen PASS/FAIL |
| `ERRORES_QA_{fecha}.md` | Defectos y fixes |
| `tests/qa-evidence/{fecha}/` | Capturas, exports, logs |
| Actualización casos | Columna Estado en `CASOS_PRUEBA_E2E_AUTONOMUSFLOW.md` |

---

## 9. Nota sobre esta sesión

**Fase 1 (documentación) completada** en la solicitud del usuario. **Fase 2 no se ejecuta en esta sesión** conforme reglas: *“Primero documenta. No hagas cambios todavía.”*

Para iniciar Fase 2, confirmar explícitamente: *“Ejecutar Fase 2 QA”*.

---

*Plan de estabilización — listo para ejecución.*
