# SECURITY_ENTERPRISE_HARDENING

## Implementado Fase 4

| Control | Estado |
|---------|--------|
| Global tenant filter (anti-IDOR DB) | OK |
| API tenant middleware | OK (Fase 2-3) |
| Login rate limit (`login` policy, 10/min/IP) | OK — UI + API |
| CSP header | OK |
| Security headers (X-Frame, nosniff, HSTS prod) | OK |
| JWT validation (zero clock skew) | OK |
| Refresh tokens (cache scoped) | OK |
| Cookies HttpOnly | OK |

## Pendiente certificación

| Control | Prioridad |
|---------|-----------|
| Vault / Key rotation automatizada | P0 prod |
| CSRF audit todos los formularios Razor | P1 |
| XSS fuzz stored fields | P1 |
| Session revocation list | P2 |
| MFA obligatorio admin | P2 |

## OWASP regresión

Phase3 automatizado: JWT tampering 401, IDOR lead 404, cross-tenant 403 — **PASS** (post-fix login).
