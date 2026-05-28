# CI_CD_RELEASE_HARDENING

## Pipeline

**Archivo:** `.github/workflows/platform-ci.yml`

| Step | Acción |
|------|--------|
| Postgres service | Health pg_isready |
| dotnet restore/build | Release |
| dotnet test | AutonomusCRM.Tests |
| dotnet publish API | Artifact |

## Release gates recomendados

1. Build 0 errors
2. Tests green
3. `run-p0-qa.ps1` smoke post-deploy
4. `/health` + `/health/ready` 200
5. Migraciones aplicadas

## Rollback

- Re-deploy imagen anterior
- Migraciones: forward-only (planificar down scripts críticos)

## Secrets CI

`ConnectionStrings__DefaultConnection`, `Jwt__Key` via GitHub Secrets — **no** en repo.
