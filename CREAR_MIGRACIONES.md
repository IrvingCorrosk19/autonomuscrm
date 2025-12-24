# 📋 Instrucciones para Crear Migraciones EF Core

## ⚠️ IMPORTANTE

**NO se han creado migraciones EF Core todavía.** El sistema actualmente usa `EnsureCreated()` que no es recomendado para producción.

---

## 🔧 Paso 1: Instalar la herramienta EF Core

Abre una **nueva terminal de PowerShell** (importante: nueva terminal) y ejecuta:

```powershell
dotnet tool install --global dotnet-ef --version 9.0.0
```

Si ya está instalada, verifica con:
```powershell
dotnet tool list --global
```

---

## 📝 Paso 2: Crear la Migración Inicial

Desde la carpeta del proyecto (`C:\Proyectos\CRM`), ejecuta:

```powershell
dotnet ef migrations add InitialCreate --project AutonomusCRM.Infrastructure --startup-project AutonomusCRM.API
```

Esto creará:
- `AutonomusCRM.Infrastructure/Persistence/Migrations/InitialCreate.cs`
- `AutonomusCRM.Infrastructure/Persistence/Migrations/[timestamp]_InitialCreate.Designer.cs`
- `AutonomusCRM.Infrastructure/Persistence/Migrations/ApplicationDbContextModelSnapshot.cs`

---

## 🗄️ Paso 3: Aplicar la Migración a la Base de Datos

Una vez creada la migración, aplícala a PostgreSQL:

```powershell
dotnet ef database update --project AutonomusCRM.Infrastructure --startup-project AutonomusCRM.API
```

Esto creará todas las tablas en la base de datos `autonomuscrm`.

---

## ✅ Paso 4: Actualizar Program.cs

Después de crear las migraciones, **elimina o comenta** esta línea en `Program.cs`:

```csharp
// ELIMINAR ESTA LÍNEA (solo para desarrollo):
context.Database.EnsureCreated();
```

Y reemplázala con:

```csharp
// Aplicar migraciones automáticamente (opcional, solo para desarrollo)
// En producción, usar: dotnet ef database update
context.Database.Migrate();
```

O mejor aún, elimina todo el bloque y aplica migraciones manualmente en producción.

---

## 🔍 Verificar que Funcionó

### Verificar migraciones creadas:
```powershell
dotnet ef migrations list --project AutonomusCRM.Infrastructure --startup-project AutonomusCRM.API
```

Deberías ver:
```
InitialCreate
```

### Verificar tablas en PostgreSQL:
Conecta a PostgreSQL y ejecuta:
```sql
\dt
```

Deberías ver todas las tablas:
- Tenants
- Customers
- Leads
- Deals
- Users
- Workflows
- Policies
- DomainEvents
- Snapshots
- TimeSeriesData
- etc.

---

## 📋 Resumen de Comandos

```powershell
# 1. Instalar herramienta (en nueva terminal)
dotnet tool install --global dotnet-ef --version 9.0.0

# 2. Crear migración
dotnet ef migrations add InitialCreate --project AutonomusCRM.Infrastructure --startup-project AutonomusCRM.API

# 3. Aplicar migración
dotnet ef database update --project AutonomusCRM.Infrastructure --startup-project AutonomusCRM.API

# 4. Verificar
dotnet ef migrations list --project AutonomusCRM.Infrastructure --startup-project AutonomusCRM.API
```

---

## ⚠️ Problemas Comunes

### Error: "dotnet-ef does not exist"
- **Solución**: Instala la herramienta y **reinicia PowerShell**
- O usa: `dotnet tool install --global dotnet-ef`

### Error: "Build failed"
- **Solución**: Ejecuta primero `dotnet build` y corrige errores

### Error: "Cannot connect to database"
- **Solución**: Verifica que PostgreSQL esté corriendo y la cadena de conexión en `appsettings.json`

### Error: "Database already exists"
- **Solución**: Si usaste `EnsureCreated()`, elimina la base de datos y vuelve a crear:
  ```sql
  DROP DATABASE autonomuscrm;
  CREATE DATABASE autonomuscrm;
  ```

---

## 🎯 Estado Actual

- ❌ **Migraciones NO creadas**
- ❌ **Migraciones NO aplicadas**
- ✅ **Estructura de base de datos lista** (ApplicationDbContext configurado)
- ✅ **Paquetes EF Core instalados**

---

**Una vez que crees y apliques las migraciones, el sistema estará listo para producción.**


