# Migraciones EF Core

Para crear una nueva migración:

```bash
dotnet ef migrations add NombreMigracion --project AutonomusCRM.Infrastructure --startup-project AutonomusCRM.API
```

Para aplicar migraciones:

```bash
dotnet ef database update --project AutonomusCRM.Infrastructure --startup-project AutonomusCRM.API
```

Para revertir migración:

```bash
dotnet ef database update NombreMigracionAnterior --project AutonomusCRM.Infrastructure --startup-project AutonomusCRM.API
```

