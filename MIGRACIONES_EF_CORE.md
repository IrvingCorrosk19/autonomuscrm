# 📋 Guía Completa de Migraciones EF Core

## ⚠️ IMPORTANTE: Arquitectura Correcta

Este proyecto usa **DesignTimeDbContextFactory** para evitar que EF Core arranque toda la aplicación durante las migraciones. Esto es la práctica correcta y profesional.

---

## 🔧 Paso 1: Instalar la herramienta EF Core

```powershell
dotnet tool install --global dotnet-ef --version 9.0.0
```

Si ya está instalada, verifica con:
```powershell
dotnet tool list --global
```

---

## 📝 Paso 2: Crear una Nueva Migración

Desde la carpeta del proyecto (`C:\Proyectos\CRM`), ejecuta:

```powershell
dotnet ef migrations add NombreDeLaMigracion --project AutonomusCRM.Infrastructure
```

**NOTA**: NO uses `--startup-project` porque tenemos `DesignTimeDbContextFactory`.

Esto creará:
- `AutonomusCRM.Infrastructure/Persistence/Migrations/[timestamp]_NombreDeLaMigracion.cs`
- `AutonomusCRM.Infrastructure/Persistence/Migrations/[timestamp]_NombreDeLaMigracion.Designer.cs`
- Actualizará `ApplicationDbContextModelSnapshot.cs`

---

## 🗄️ Paso 3: Aplicar la Migración a la Base de Datos

### Verificar que PostgreSQL esté corriendo

#### Opción A: Usar Docker Compose
```bash
docker-compose up -d
```

#### Opción B: Verificar conexión manual
```bash
psql -h localhost -U postgres -d autonomuscrm
```

### Aplicar la migración

```powershell
dotnet ef database update --project AutonomusCRM.Infrastructure
```

**NOTA**: NO uses `--startup-project` porque tenemos `DesignTimeDbContextFactory`.

Esto aplicará todas las migraciones pendientes y creará/actualizará las tablas en PostgreSQL.

---

## ✅ Paso 4: Verificar que se Aplicó Correctamente

### Ver migraciones aplicadas:
```powershell
dotnet ef migrations list --project AutonomusCRM.Infrastructure
```

### Verificar tablas en PostgreSQL:
```bash
psql -h localhost -U postgres -d autonomuscrm -c "\dt"
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

## 🔄 Comandos Útiles

### Ver todas las migraciones
```powershell
dotnet ef migrations list --project AutonomusCRM.Infrastructure
```

### Revertir la última migración
```powershell
dotnet ef database update NombreMigracionAnterior --project AutonomusCRM.Infrastructure
```

### Eliminar la última migración (sin aplicar)
```powershell
dotnet ef migrations remove --project AutonomusCRM.Infrastructure
```

### Eliminar y recrear la base de datos
⚠️ **ADVERTENCIA: Esto eliminará todos los datos**

```powershell
# Eliminar la base de datos
dotnet ef database drop --project AutonomusCRM.Infrastructure --force

# Recrear desde cero
dotnet ef database update --project AutonomusCRM.Infrastructure
```

---

## 🔧 Configuración de Conexión

La cadena de conexión está en `AutonomusCRM.API/appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=autonomuscrm;Username=postgres;Password=Panama2020$",
  "Redis": "localhost:6379"
}
```

El `DesignTimeDbContextFactory` lee esta configuración automáticamente.

---

## ⚠️ Solución de Problemas

### Error: "database does not exist"
Crea la base de datos primero:
```bash
psql -h localhost -U postgres -d postgres -c "CREATE DATABASE autonomuscrm;"
```

### Error: "password authentication failed"
Verifica las credenciales en `appsettings.json` y que el usuario tenga permisos.

### Error: "could not connect to server"
Verifica que PostgreSQL esté corriendo:
```powershell
# Windows
Get-Service postgresql*

# O verifica el puerto
netstat -an | findstr 5432
```

### Error: "Build failed"
Ejecuta primero `dotnet build` y corrige errores de compilación.

### Error: "No design-time services were found"
Verifica que `DesignTimeDbContextFactory` esté en el proyecto `AutonomusCRM.Infrastructure` y que implemente `IDesignTimeDbContextFactory<ApplicationDbContext>`.

---

## 🎯 Resumen de Comandos Principales

```powershell
# 1. Crear migración
dotnet ef migrations add InitialCreate --project AutonomusCRM.Infrastructure

# 2. Aplicar migración
dotnet ef database update --project AutonomusCRM.Infrastructure

# 3. Ver migraciones
dotnet ef migrations list --project AutonomusCRM.Infrastructure
```

---

## 🏆 Arquitectura Correcta

Este proyecto sigue las mejores prácticas:

✅ **DesignTimeDbContextFactory**: EF Core no arranca la aplicación completa  
✅ **Separación de responsabilidades**: Migraciones independientes del runtime  
✅ **CI/CD friendly**: Migraciones se pueden ejecutar sin arrancar la API  
✅ **Clean Architecture**: Infrastructure no depende del arranque de la API  

---

**NOTA**: En `Program.cs` NO debe haber `Database.Migrate()` porque las migraciones se aplican manualmente con los comandos anteriores.

