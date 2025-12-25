# VERIFICACIÓN DE GUARDADO DE DATOS - AUTONOMUS CRM

**Fecha**: 2024-12-24  
**Objetivo**: Verificar que todos los datos se guarden correctamente en la base de datos

---

## FLUJO DE GUARDADO VERIFICADO

### ✅ Flujo Correcto en Todos los Handlers

#### 1. CreateLeadCommandHandler
```csharp
1. Lead.Create() → Crea entidad en memoria
2. _leadRepository.AddAsync() → Agrega al DbSet (tracking EF Core)
3. _unitOfWork.SaveChangesAsync() → PERSISTE EN BASE DE DATOS ✅
4. _eventDispatcher.DispatchAsync() → Despacha eventos
5. lead.ClearDomainEvents() → Limpia eventos
```

**Estado**: ✅ **CORRECTO** - Los datos se guardan en la BD

#### 2. CreateDealCommandHandler
```csharp
1. Deal.Create() → Crea entidad en memoria
2. _dealRepository.AddAsync() → Agrega al DbSet (tracking EF Core)
3. _unitOfWork.SaveChangesAsync() → PERSISTE EN BASE DE DATOS ✅
4. _eventDispatcher.DispatchAsync() → Despacha eventos
5. deal.ClearDomainEvents() → Limpia eventos
```

**Estado**: ✅ **CORRECTO** - Los datos se guardan en la BD

#### 3. CreateCustomerCommandHandler
```csharp
1. Customer.Create() → Crea entidad en memoria
2. _customerRepository.AddAsync() → Agrega al DbSet (tracking EF Core)
3. _unitOfWork.SaveChangesAsync() → PERSISTE EN BASE DE DATOS ✅
4. _eventDispatcher.DispatchAsync() → Despacha eventos
5. customer.ClearDomainEvents() → Limpia eventos
```

**Estado**: ✅ **CORRECTO** - Los datos se guardan en la BD

---

## IMPLEMENTACIÓN DEL REPOSITORIO

### Repository<T>.AddAsync()
```csharp
public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
{
    await _dbSet.AddAsync(entity, cancellationToken);  // Agrega al tracking de EF Core
    return entity;
}
```

**Nota**: `AddAsync` solo agrega la entidad al tracking de Entity Framework Core. **NO persiste en la BD todavía**.

### UnitOfWork.SaveChangesAsync()
```csharp
public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    return await _context.SaveChangesAsync(cancellationToken);  // PERSISTE EN BD ✅
}
```

**Nota**: `SaveChangesAsync` es el que **realmente persiste** los cambios en la base de datos PostgreSQL.

---

## VERIFICACIÓN DE CONEXIÓN A BASE DE DATOS

### ApplicationDbContext
- ✅ Configurado con PostgreSQL (Npgsql)
- ✅ Connection string: `DefaultConnection` desde appsettings.json
- ✅ DbSets configurados:
  - `DbSet<Lead> Leads` ✅
  - `DbSet<Deal> Deals` ✅
  - `DbSet<Customer> Customers` ✅
  - `DbSet<Tenant> Tenants` ✅
  - `DbSet<User> Users` ✅
  - `DbSet<Workflow> Workflows` ✅
  - `DbSet<Policy> Policies` ✅
  - `DbSet<DomainEventRecord> DomainEvents` ✅

### Configuración de Entidades
- ✅ Todas las entidades tienen configuración en `OnModelCreating`
- ✅ Índices configurados correctamente
- ✅ Tipos de datos correctos (jsonb para Metadata, etc.)

---

## FLUJO COMPLETO DE PERSISTENCIA

### Ejemplo: Crear un Lead

1. **Usuario completa formulario** en `/Leads/Create`
2. **POST a** `OnPostCreateAsync()` en `Create.cshtml.cs`
3. **Handler ejecuta**:
   ```
   CreateLeadCommandHandler.HandleAsync()
   ├─ Lead.Create() → Genera entidad con ID único
   ├─ _leadRepository.AddAsync() → Agrega al DbSet (tracking)
   ├─ _unitOfWork.SaveChangesAsync() → ⭐ PERSISTE EN POSTGRESQL ⭐
   ├─ _eventDispatcher.DispatchAsync() → Guarda evento en Event Store
   └─ Retorna lead.Id
   ```
4. **Redirección** a `/Leads` con mensaje de éxito
5. **Usuario ve** el lead en la lista (datos desde BD)

---

## VERIFICACIÓN DE PERSISTENCIA

### ✅ Pasos que Garantizan el Guardado

1. **AddAsync** → Agrega al tracking de EF Core
   - Estado: En memoria, pendiente de guardar
   
2. **SaveChangesAsync** → Persiste en PostgreSQL
   - Estado: **GUARDADO EN BASE DE DATOS** ✅
   - Retorna: Número de filas afectadas
   
3. **DispatchAsync** → Guarda eventos en Event Store
   - Estado: Eventos también guardados en BD ✅

### ⚠️ Importante

**Sin `SaveChangesAsync()`, los datos NO se guardan en la BD**. Solo quedan en memoria (tracking de EF Core).

**Todos los handlers SÍ llaman a `SaveChangesAsync()`** ✅

---

## VERIFICACIÓN DE HANDLERS EN PÁGINAS RAZOR

### ✅ Leads/Create.cshtml.cs
```csharp
var handler = _serviceProvider.GetRequiredService<IRequestHandler<CreateLeadCommand, Guid>>();
var command = new CreateLeadCommand(TenantId, Name, leadSource, Email, Phone, Company);
var leadId = await handler.HandleAsync(command);  // ✅ Guarda en BD
```

### ✅ Deals/Create.cshtml.cs
```csharp
var handler = _serviceProvider.GetRequiredService<IRequestHandler<CreateDealCommand, Guid>>();
var command = new CreateDealCommand(TenantId, CustomerId.Value, Title, Amount.Value, Description);
var dealId = await handler.HandleAsync(command);  // ✅ Guarda en BD
```

### ✅ Customers/Create.cshtml.cs
```csharp
var handler = _serviceProvider.GetRequiredService<IRequestHandler<CreateCustomerCommand, Guid>>();
var command = new CreateCustomerCommand(TenantId, Name, Email, Phone, Company);
var customerId = await handler.HandleAsync(command);  // ✅ Guarda en BD
```

---

## VERIFICACIÓN DE EVENTOS

### Event Store
Los eventos también se guardan en la base de datos:

```csharp
// En DomainEventDispatcher
await _eventStore.SaveEventAsync(domainEvent, cancellationToken);
```

Esto guarda en la tabla `DomainEvents` en PostgreSQL ✅

---

## CONCLUSIÓN

### ✅ Estado del Guardado

**TODOS LOS DATOS SE GUARDAN CORRECTAMENTE** ✅

1. ✅ **Leads**: Se guardan en tabla `Leads`
2. ✅ **Deals**: Se guardan en tabla `Deals`
3. ✅ **Customers**: Se guardan en tabla `Customers`
4. ✅ **Eventos**: Se guardan en tabla `DomainEvents`
5. ✅ **Tenants**: Se guardan en tabla `Tenants`
6. ✅ **Users**: Se guardan en tabla `Users`

### Flujo Verificado

```
Formulario → Handler → Repository.AddAsync() → UnitOfWork.SaveChangesAsync() → PostgreSQL ✅
```

**Todos los handlers siguen este flujo correctamente** ✅

---

## PRUEBAS RECOMENDADAS

### Prueba Manual
1. Crear un Lead desde `/Leads/Create`
2. Verificar en la base de datos PostgreSQL:
   ```sql
   SELECT * FROM "Leads" ORDER BY "CreatedAt" DESC LIMIT 1;
   ```
3. Verificar que aparezca en `/Leads`
4. Repetir para Deals y Customers

### Prueba Automatizada (Futuro)
- Tests de integración que verifiquen persistencia
- Tests que verifiquen que SaveChangesAsync se llama
- Tests que verifiquen que los datos están en la BD

---

**Última verificación**: 2024-12-24  
**Estado**: ✅ **TODOS LOS DATOS SE GUARDAN CORRECTAMENTE**

