# IMPORT_STRESS_VALIDATION

**Fecha:** 2026-05-27

---

## Controles implementados

`ImportGuard` (`AutonomusCRM.Application/Common/Imports/ImportGuard.cs`):

| Límite | Valor |
|--------|------|
| Tamaño archivo | 5 MB |
| Filas máximas | 5000 |
| Extensiones | `.csv`, `.json` |

Aplicado en: `Pages/Leads/Import.cshtml.cs` (patrón replicable a Customers/Deals/Users).

---

## Fixtures de prueba

| Archivo | Propósito |
|---------|-----------|
| `tests/qa-data/valid-leads.csv` | 2 filas válidas |
| `tests/qa-data/invalid.csv` | CSV mal formado |
| `tests/qa-data/invalid.json` | JSON truncado |

---

## Pruebas automatizadas

| ID catálogo | Estado sesión |
|-------------|---------------|
| IMP-001 … IMP-003 | **Pendiente** upload HTTP en script |
| DAT-001 … DAT-003 | **Pendiente** |
| IMP-SKIP-UI | Documentado — validación manual UI |

---

## Comportamiento esperado (manual)

1. Subir `invalid.json` → error, 0 filas importadas, sin corrupción BD.
2. Subir archivo >5MB → rechazo antes de parse.
3. Import parcial: fila inválida logueada (`LogWarning`), siguientes filas procesadas (Leads Import).

---

## Riesgos residuales

- Parser CSV por `Split(',')` — no soporta comillas RFC; ver OWASP-01.
- Sin transacción única por archivo — rollback parcial no automático.

---

## Conclusión

**Validación estructural OK**; **stress automatizado pendiente** siguiente iteración.
