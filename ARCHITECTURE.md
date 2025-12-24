# Arquitectura de AUTONOMUS CRM

## Visión General

AUTONOMUS CRM está construido siguiendo los principios de **Clean Architecture** y **Event-Driven Architecture**, diseñado para ser autónomo, escalable y mantenible.

## Capas del Sistema

### 1. Domain Layer (`AutonomusCRM.Domain`)

**Responsabilidad**: Lógica de negocio pura, sin dependencias externas.

**Componentes**:
- **Entidades**: `Tenant`, `Customer`, `Lead`, `Deal`
- **Aggregate Roots**: Raíces de agregado con eventos de dominio
- **Value Objects**: `TenantId`, etc.
- **Domain Events**: Eventos que representan cambios en el dominio
- **Enums**: Estados y tipos del dominio

**Principios**:
- No depende de frameworks
- No depende de infraestructura
- Contiene reglas de negocio invariantes
- Genera eventos de dominio para cambios importantes

### 2. Application Layer (`AutonomusCRM.Application`)

**Responsabilidad**: Orquestación de casos de uso y coordinación entre dominios.

**Componentes**:
- **Commands**: `CreateCustomerCommand`, `UpdateDealStageCommand`, etc.
- **Queries**: `GetCustomerByIdQuery`, etc.
- **Handlers**: Implementación de casos de uso
- **Interfaces**: Contratos para repositorios y servicios
- **DTOs**: Objetos de transferencia de datos

**Principios**:
- Depende solo de Domain
- Define contratos (interfaces)
- Orquesta operaciones de negocio
- Valida entrada y coordina salida

### 3. Infrastructure Layer (`AutonomusCRM.Infrastructure`)

**Responsabilidad**: Implementaciones concretas de interfaces y acceso a recursos externos.

**Componentes**:
- **Persistence**: 
  - `ApplicationDbContext` (EF Core)
  - Repositorios implementados
  - `UnitOfWork`
  - `EventStore` (Event Sourcing)
- **Events**:
  - `DomainEventDispatcher`
  - `IEventBus` / `InMemoryEventBus`
- **DependencyInjection**: Configuración de servicios

**Principios**:
- Implementa interfaces de Application
- Maneja detalles técnicos (EF Core, PostgreSQL)
- Puede ser reemplazado sin afectar otras capas

### 4. API Layer (`AutonomusCRM.API`)

**Responsabilidad**: Punto de entrada HTTP, exposición de funcionalidades.

**Componentes**:
- **Controllers**: Endpoints REST
- **Program.cs**: Configuración y startup
- **Middleware**: Logging, autenticación (futuro)

**Principios**:
- Depende de Application e Infrastructure
- Expone casos de uso como endpoints
- Maneja HTTP y serialización

### 5. Workers Layer (`AutonomusCRM.Workers`)

**Responsabilidad**: Ejecución de agentes autónomos y procesos en background.

**Componentes**:
- **Agents**: 
  - `LeadIntelligenceAgent`
  - `CustomerRiskAgent`
- **Worker**: Background service que suscribe agentes a eventos

**Principios**:
- Procesa eventos de forma asíncrona
- Ejecuta lógica autónoma
- Escalable horizontalmente

## Event-Driven Architecture

### Flujo de Eventos

```
1. Usuario → API → Command Handler
2. Command Handler → Domain Entity
3. Domain Entity → Genera Domain Event
4. DomainEventDispatcher → Event Store + Event Bus
5. Event Bus → Agentes suscritos
6. Agentes → Procesan y generan nuevos eventos
```

### Event Store

Todos los eventos se guardan en la tabla `DomainEvents`:
- Trazabilidad completa
- Auditoría absoluta
- Reconstrucción de estado
- Análisis histórico

### Event Bus

- **InMemoryEventBus**: Implementación en memoria (desarrollo)
- **Futuro**: RabbitMQ, Azure Service Bus, etc.
- Permite desacoplamiento total entre componentes

## Patrones de Diseño

### Repository Pattern
- Abstrae acceso a datos
- Facilita testing
- Permite cambiar implementación

### Unit of Work
- Maneja transacciones
- Coordina múltiples repositorios
- Garantiza consistencia

### CQRS (Command Query Responsibility Segregation)
- Commands para escritura
- Queries para lectura
- Separación clara de responsabilidades

### Domain Events
- Desacoplamiento entre agregados
- Permite reacciones automáticas
- Trazabilidad completa

## Multi-Tenancy

- Cada entidad tiene `TenantId`
- Aislamiento a nivel de datos
- Filtrado automático por tenant
- Kill-switch por tenant

## Escalabilidad

### Horizontal
- APIs stateless
- Workers escalables independientemente
- Event Bus distribuido (futuro)

### Vertical
- Optimización de queries
- Índices en PostgreSQL
- Caching (futuro)

## Seguridad (Futuro)

- Zero Trust
- JWT + Refresh Tokens
- MFA obligatorio
- RBAC + ABAC
- Encriptación de datos sensibles

## Observabilidad

- **Logging**: Serilog con estructuración
- **Event Store**: Auditoría completa
- **Métricas**: (Futuro: Prometheus, Grafana)
- **Trazabilidad**: CorrelationId en todos los eventos

## Testing (Futuro)

- Unit Tests: Domain y Application
- Integration Tests: API y Infrastructure
- E2E Tests: Flujos completos

## Migraciones

- EF Core Migrations
- Versionado de esquema
- Migraciones sin downtime (futuro)

---

Esta arquitectura está diseñada para evolucionar y escalar durante décadas, no solo versiones.
