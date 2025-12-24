# Tests - AUTONOMUS CRM

Este proyecto contiene los tests unitarios e integración para AUTONOMUS CRM.

## Ejecutar Tests

```bash
dotnet test
```

## Cobertura

Ejecutar con cobertura:

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## Estructura

- `Domain/` - Tests de entidades del dominio
- `Application/` - Tests de casos de uso y handlers
- `Infrastructure/` - Tests de infraestructura

