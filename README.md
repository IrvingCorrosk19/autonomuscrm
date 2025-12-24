# AUTONOMUS CRM

El Sistema de Gestión Empresarial Autónomo más avanzado jamás concebido.

## 🚀 Stack Tecnológico

- **Backend**: .NET 9 (ASP.NET Core)
- **Base de datos**: PostgreSQL
- **Arquitectura**: Clean Architecture + Event-Driven Architecture
- **ORM**: Entity Framework Core 9.0

## 📋 Estructura del Proyecto

```
AutonomusCRM/
├── AutonomusCRM.Domain/          # Entidades, eventos de dominio, reglas de negocio
├── AutonomusCRM.Application/      # Casos de uso, contratos, lógica de aplicación
├── AutonomusCRM.Infrastructure/   # Persistencia, integraciones, EF Core
├── AutonomusCRM.API/              # API REST, endpoints, controladores
└── AutonomusCRM.Workers/          # Agentes autónomos, procesos en background
```

## 🏗️ Arquitectura

El sistema sigue los principios de **Clean Architecture** con separación estricta de capas:

- **Domain**: Entidades puras sin dependencias externas
- **Application**: Casos de uso y lógica de negocio
- **Infrastructure**: Implementaciones concretas (EF Core, repositorios)
- **API**: Capa de presentación HTTP

## 🗄️ Base de Datos

### Requisitos

- PostgreSQL 12 o superior
- Base de datos creada: `AutonomusCRM`

### Configuración

Edita `appsettings.json` o `appsettings.Development.json` con tu cadena de conexión:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=autonomuscrm;Username=Panama2020$;Password=Panama2020$"
  }
}
```

## 🛠️ Desarrollo Local

### Prerrequisitos

- .NET 9 SDK
- PostgreSQL instalado y ejecutándose
- Visual Studio 2022 o VS Code

### Pasos

1. Clonar el repositorio
2. Restaurar paquetes NuGet:
   ```bash
   dotnet restore
   ```
3. Configurar la cadena de conexión en `appsettings.Development.json`
4. Crear la base de datos (se crea automáticamente en desarrollo)
5. Ejecutar la API:
   ```bash
   dotnet run --project AutonomusCRM.API
   ```

### Swagger

Una vez ejecutando, accede a:
- Swagger UI: `https://localhost:5001/swagger`
- API: `https://localhost:5001/api`

## 📦 Entidades Principales

- **Tenant**: Multi-tenancy con aislamiento fuerte
- **Customer**: Clientes del CRM
- **Lead**: Prospectos y oportunidades
- **Deal**: Oportunidades de negocio

## 🎯 Características Implementadas

### ✅ Arquitectura y Diseño
- **Clean Architecture** estricta con separación de capas
- **Event-Driven Architecture** completa
- **Event Sourcing** con Event Store en PostgreSQL
- **Domain Events** para trazabilidad completa
- **Repository Pattern** con Unit of Work
- **Multi-tenant** con aislamiento fuerte

### ✅ Entidades del Dominio
- **Tenant**: Gestión multi-tenant con kill-switch
- **Customer**: Clientes con scoring de riesgo y lifetime value
- **Lead**: Prospectos con scoring automático
- **Deal**: Oportunidades con etapas y probabilidades

### ✅ API REST Completa
- Endpoints para Tenants, Customers, Leads y Deals
- Swagger/OpenAPI documentación
- Validación y manejo de errores
- Logging estructurado con Serilog

### ✅ Event Intelligence Bus
- Event Bus en memoria (preparado para RabbitMQ/Azure Service Bus)
- Event Store para auditoría completa
- Despacho automático de eventos de dominio
- Suscripción de agentes a eventos

### ✅ Agentes Autónomos
- **LeadIntelligenceAgent**: Scoring automático de leads
- **CustomerRiskAgent**: Evaluación de riesgo de clientes
- Worker Service para ejecución continua
- Procesamiento asíncrono de eventos

### ✅ Observabilidad
- Logging estructurado con Serilog
- Logs en consola y archivo
- Trazabilidad de eventos con CorrelationId
- Auditoría completa en Event Store

## 🚀 Uso Rápido

### 1. Iniciar PostgreSQL con Docker

```bash
docker-compose up -d
```

### 2. Ejecutar la API

```bash
cd AutonomusCRM.API
dotnet run
```

La API estará disponible en:
- Swagger: `https://localhost:5001/swagger`
- API: `https://localhost:5001/api`

### 3. Ejecutar Workers (Agentes Autónomos)

En otra terminal:

```bash
cd AutonomusCRM.Workers
dotnet run
```

Los agentes se suscribirán a eventos y procesarán automáticamente.

## 📡 Endpoints Principales

### Tenants
- `POST /api/tenants` - Crear tenant
- `GET /api/tenants/{id}` - Obtener tenant

### Customers
- `POST /api/customers` - Crear customer
- `GET /api/customers/{id}?tenantId={tenantId}` - Obtener customer
- `PUT /api/customers/{id}/status` - Actualizar estado

### Leads
- `POST /api/leads` - Crear lead
- `POST /api/leads/{id}/qualify?tenantId={tenantId}` - Calificar lead

### Deals
- `POST /api/deals` - Crear deal
- `PUT /api/deals/{id}/stage` - Actualizar etapa
- `POST /api/deals/{id}/close` - Cerrar deal

## 🔄 Flujo de Eventos

1. **Usuario crea entidad** → Se dispara Domain Event
2. **DomainEventDispatcher** → Guarda en Event Store y publica en Event Bus
3. **Agentes suscritos** → Procesan el evento automáticamente
4. **Agentes ejecutan lógica** → Actualizan entidades, calculan scores, etc.
5. **Nuevos eventos** → Se disparan y el ciclo continúa

## 🏗️ Estructura de Proyectos

```
AutonomusCRM/
├── Domain/              # Entidades, eventos, reglas de negocio
├── Application/          # Casos de uso, handlers, DTOs
├── Infrastructure/       # EF Core, repositorios, Event Bus, Event Store
├── API/                  # Controllers, endpoints REST
└── Workers/              # Agentes autónomos, background services
```

## 📊 Base de Datos

El sistema crea automáticamente las tablas:
- `Tenants`
- `Customers`
- `Leads`
- `Deals`
- `DomainEvents` (Event Store)

## 🔐 Seguridad (Próximamente)

- [ ] Autenticación JWT
- [ ] MFA obligatorio
- [ ] Autorización RBAC + ABAC
- [ ] Zero Trust
- [ ] Encriptación de datos sensibles

## 📝 Próximas Mejoras

- [ ] Más agentes autónomos (Deal Strategy, Communication, etc.)
- [ ] Automation Engine con triggers y workflows
- [ ] Integración con servicios de IA
- [ ] Dashboard de observabilidad
- [ ] Autenticación y autorización completa
- [ ] API de métricas y analytics

## 📄 Licencia

Este proyecto es parte del sistema AUTONOMUS CRM.

---

**AUTONOMUS CRM** - El Sistema de Gestión Empresarial Autónomo más avanzado jamás concebido.

Consulta `VISION.md` para la visión completa del sistema.

