# OWASP_SECURITY_AUDIT

**Fecha:** 2026-05-27  
**Alcance:** API + UI local, pruebas automatizadas + revisión estática

---

## Hallazgos ejecutados

| OWASP / Vector | Prueba | Resultado | Severidad |
|----------------|--------|-----------|-----------|
| Broken Access Control (IDOR lead) | GET lead tenant B con JWT A | **PASS** — 404 | — |
| Broken Access Control (tenant query) | GET leads?tenantId=B con JWT A | **PASS** — 403 | — |
| Auth JWT tampering | Token alterado +1 char | **PASS** — 401 | — |
| Security headers | Login page | **PASS** — X-Content-Type-Options, CSP/frame | — |
| SEC-V-01 Viewer Create | GET /Leads/Create | **PASS** — AccessDenied | — |
| SEC-S-01 Anónimo | /Users sin auth | **PASS** — Login | — |

---

## Hallazgos abiertos (no falsos PASS)

| ID | Categoría | Descripción | Severidad | Fix recomendado |
|----|-----------|-------------|-----------|-----------------|
| OWASP-01 | A03 Injection | CSV parser naive (split comma) — riesgo en imports maliciosos | Media | CsvHelper + validación columnas |
| OWASP-02 | A05 Misconfiguration | HTTPS no forzado en dev (5154 HTTP) | Baja prod | HSTS prod + redirect |
| OWASP-03 | A07 XSS | No fuzz automatizado de campos rich text | Media | Pentest manual + encode Razor |
| OWASP-04 | A01 CSRF | API JWT sin CSRF (OK); verificar todos los POST Razor tienen antiforgery | Media | Auditoría formularios |
| OWASP-05 | A04 Rate limit | 200 req/min global — brute force login parcial | Media | Política dedicada `/Account/Login` |
| OWASP-06 | A02 Crypto | JWT key en appsettings dev | Alta prod | Key vault / env secrets |

---

## Evidencia

- `tests/e2e/run-phase3-qa.ps1` — OWASP-JWT, OWASP-HEADERS, TEN-IDOR
- `tests/e2e/run-p0-qa.ps1` — regresión seguridad P0

---

## Conclusión auditoría

**No se detectaron bypasses críticos** en pruebas automatizadas Fase 3. **Pentest manual completo pendiente** antes de certificación enterprise.
