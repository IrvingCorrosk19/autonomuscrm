# FIRST CLIENT INSTALLATION REQUIREMENTS

**Proyecto:** AutonomusCRM  
**Fecha:** 2026-05-28  
**Alcance:** Instalación limpia para primer cliente real — sin datos demo, sin configuración previa  
**Método:** Análisis de código + configuración de despliegue (sin asumir shortcuts)

---

## Resumen ejecutivo

Un cliente nuevo **no puede operar** con solo desplegar contenedores y abrir el navegador. La base de datos vacía **no tiene usuarios ni tenants**. El único camino soportado en código para el primer arranque es **`POST /api/provisioning/tenants`** con `Provisioning:ApiKey` configurado.

**No existe** wizard de onboarding, registro público ni rol SuperAdmin.

---

## 1. ¿Qué necesita un cliente nuevo para arrancar?

### 1.1 Infraestructura mínima (producción)

| Componente | Obligatorio | Versión referencia | Evidencia |
|------------|-------------|-------------------|-----------|
| **PostgreSQL** | Sí | 16 | `docker-compose.yml`, `deploy/docker-compose.vps.yml` |
| **Redis** | Sí (Production) | 7 | `ProductionConfigurationGuard.cs` L37–38 |
| **RabbitMQ** | Sí (Staging/Production) | 3 | Guard L29–35; `EventBus:Provider` ≠ InMemory |
| **API** (`AutonomusCRM.API`) | Sí | — | `Program.cs`, `Dockerfile.api` |
| **Workers** (`AutonomusCRM.Workers`) | Condicional | — | Requerido si se desean agentes autónomos cross-process |

**Opcional (observabilidad):** OpenTelemetry Collector, Prometheus, Loki, Tempo, Grafana — presentes en `docker-compose.yml` pero no bloquean el CRM.

### 1.2 Secreto de bootstrap (obligatorio en BD vacía)

| Variable | Propósito |
|----------|-----------|
| `Provisioning__ApiKey` | Autentica `POST /api/provisioning/tenants` sin usuario previo |

Sin esta clave y sin usuarios en BD → **sistema inaccesible**.

### 1.3 Secreto de plataforma (obligatorio en Staging/Production)

| Variable | Validación | Archivo |
|----------|------------|---------|
| `ConnectionStrings__DefaultConnection` | No vacío | `ProductionConfigurationGuard.cs` L19–20 |
| `Jwt__Key` | ≥ 32 caracteres | L22–24 |
| `IntegrationEncryption__Key` | Base64, 32+ bytes | L26–27 |
| `RabbitMQ__HostName` | No vacío | L34–35 |
| `ConnectionStrings__Redis` | No vacío (solo Production) | L37–38 |

### 1.4 Procedimiento de bootstrap (orden correcto)

```
1. Desplegar PostgreSQL + Redis + RabbitMQ
2. Configurar variables de entorno (ver sección 3)
3. Confirmar Seed__Enabled=false
4. Iniciar API → migraciones automáticas (Database:AutoMigrate=true)
5. POST /api/provisioning/tenants con X-Platform-Key
6. Login Admin en /Account/Login
7. Crear usuarios adicionales en /Users/Create + asignar roles en /Users/Edit
8. (Opcional) Iniciar Workers cuando RabbitMQ esté healthy
```

**No usar** `POST /api/tenants` para bootstrap — crea tenant sin usuario admin (`CreateTenantCommandHandler.cs`).

---

## 2. Configuraciones obligatorias

### 2.1 Base de datos y migraciones

| Setting | Default | Obligatorio | Comportamiento |
|---------|---------|-------------|----------------|
| `Database:AutoMigrate` | `true` en `appsettings.json` | Sí | `WebApplicationExtensions.ApplyMigrationsAsync` ejecuta `MigrateAsync` al arranque |
| `ConnectionStrings:DefaultConnection` | Vacío en base | Sí | Sin conexión → fallo al iniciar |

**Migraciones:** 17 migraciones EF Core en `AutonomusCRM.Infrastructure/Persistence/Migrations/` desde `20251224185349_InitialCreate` hasta `20260605030856_DatabasePerformanceIndexesPhase2`.

### 2.2 Autenticación

| Setting | Obligatorio | Notas |
|---------|-------------|-------|
| `Jwt:Key` | Sí (≥32 en Prod) | Cookie + JWT Smart scheme en `Program.cs` |
| `Jwt:Issuer` / `Audience` | Recomendado | Default `AutonomusCRM` |

### 2.3 Event bus y mensajería

| Setting | Dev | Staging/Prod |
|---------|-----|--------------|
| `EventBus:Provider` | `InMemory` permitido | **RabbitMQ obligatorio** |
| `RabbitMQ:HostName` | `localhost` | Host real requerido |

**Impacto:** Sin RabbitMQ en producción, Workers no reciben eventos del API (bus in-process no cruza procesos).

### 2.4 Cifrado de integraciones

| Setting | Obligatorio Prod | Uso |
|---------|------------------|-----|
| `IntegrationEncryption:Key` | Sí | Tokens OAuth, secretos de integraciones |

### 2.5 Seed (debe estar desactivado)

| Setting | Valor para primer cliente | Riesgo si `true` |
|---------|---------------------------|------------------|
| `Seed:Enabled` | **`false`** | Inyecta demo tenant, QA-B, CEO_DEMO, usuarios `*@autonomuscrm.local` |

**Atención:** `deploy/docker-compose.vps.yml` L76 tiene `Seed__Enabled: "true"` hardcodeado — **riesgo de contaminación demo en VPS**.

### 2.6 Provisioning

| Setting | Obligatorio BD vacía |
|---------|---------------------|
| `Provisioning:ApiKey` | **Sí** |

---

## 3. Configuraciones opcionales

### 3.1 IA / LLM

| Setting | Default | Sin configurar |
|---------|---------|----------------|
| `AI:Enabled` | `true` en base | CRM manual funciona; features IA lanzan `LlmNotConfiguredException` |
| `AI:OpenAI:ApiKey` / `AzureOpenAI` | Vacío | Command Center, Trust explainability degradados |

**Recomendación primer cliente:** `AI__Enabled=false` hasta tener API key.

### 3.2 Comunicaciones (Email / WhatsApp)

| Setting | Dev | Prod estricto |
|---------|-----|---------------|
| `Communications:EmailProvider` | `Log` (simulado) | SendGrid/SMTP/SES si `AllowSimulation=false` |
| `Communications:AllowSimulation` | Default `true` implícito | VPS default `false` — requiere proveedor real |

### 3.3 Facturación (Stripe)

| Setting | Sin configurar |
|---------|----------------|
| `Stripe:SecretKey` | Billing page funciona con plan `free` auto-creado; checkout falla con mensaje |

### 3.4 SaaS / suscripciones

| Setting | Default | Impacto |
|---------|---------|---------|
| `SaaS:EnforceSubscription` | `false` | Sin bloqueo por plan |
| `SaaS:DefaultTrialDays` | `14` | Aplicado en provisioning |

### 3.5 Autonomía / Workers

| Setting | Default | Sin Workers |
|---------|---------|-------------|
| `Autonomous:Enabled` | `true` | Agentes background no ejecutan; UI manual OK |

### 3.6 Enterprise SSO

| Setting | Default |
|---------|---------|
| `EnterpriseAuth:Enabled` | `false` — OIDC/SAML/SCIM desactivados |

### 3.7 Webhooks / Twilio / OAuth integraciones

Todas opcionales hasta que el cliente configure HubSpot, Salesforce, Google, Microsoft, etc.

### 3.8 Observabilidad

`OpenTelemetry`, Prometheus, Grafana — opcionales para operación CRM básica.

---

## 4. Dependencias que romperían el sistema

| Dependencia ausente/mal configurada | Efecto | Severidad |
|-------------------------------------|--------|-----------|
| PostgreSQL down / connection string vacío | API no arranca | ❌ Bloqueante |
| `Jwt:Key` < 32 chars en Production | `InvalidOperationException` en startup | ❌ Bloqueante |
| `IntegrationEncryption:Key` vacío en Prod | Fail-fast guard | ❌ Bloqueante |
| `RabbitMQ:HostName` vacío en Prod | Fail-fast guard | ❌ Bloqueante |
| `EventBus:Provider=InMemory` en Prod | Fail-fast guard | ❌ Bloqueante |
| Redis ausente en Production | Fail-fast guard | ❌ Bloqueante |
| `Provisioning:ApiKey` vacío + BD sin usuarios | Login imposible; provisioning 401 | ❌ Bloqueante |
| `Seed:Enabled=true` en VPS | Contamina con datos demo en cada deploy | ⚠️ Riesgo operativo |
| RabbitMQ down con Workers activos | Agentes no procesan; health `/health/ready` puede fallar | ⚠️ Degradado |
| Email provider `Log` con `AllowSimulation=false` | Fail-fast guard | ❌ Bloqueante |
| LLM sin API key | IA conversacional falla al invocarse | ⚠️ Parcial |
| Workers no desplegados | Sin agentes autónomos 15min, sin consolidación memoria background | ⚠️ Parcial |

---

## 5. Procesos que fallarían sin configuración

| Proceso | Requiere | Falla sin |
|---------|----------|-----------|
| **Login** | Usuario + tenant en BD | BD vacía → `UnauthorizedAccessException` |
| **Provisioning** | `X-Platform-Key` o JWT autenticado | 401 |
| **Creación tenant vía API** | Admin autenticado | 401 sin login previo |
| **Creación usuarios UI** | Admin/Manager logueado | Redirect login |
| **Escritura comercial (UI)** | Rol Admin/Manager/Sales | Support/Viewer → AccessDenied |
| **Workflows Communicate/ActivateAgent** | Implementación real | Solo log (`WorkflowEngine.cs` L116–118) |
| **Agentes autónomos** | RabbitMQ + Workers + eventos | Sin workers → no ejecutan |
| **Trust HITL** | Decisiones con score ≥ umbral (default 70) | Cola vacía hasta actividad IA |
| **Stripe checkout** | `Stripe:SecretKey` | Error controlado en UI |
| **Integraciones OAuth** | ClientId/Secret + `IntegrationEncryption:Key` | Conexión falla |
| **Email real** | SendGrid/SMTP + `AllowSimulation=false` | Guard bloquea arranque |
| **System Settings persist** | Implementación DB | Cambios solo en log (`UpdateSystemSettingsCommandHandler`) |

---

## 6. Variables de entorno — plantilla primer cliente

```env
# Obligatorias
POSTGRES_PASSWORD=<secret>
RABBITMQ_USER=autonomus
RABBITMQ_PASSWORD=<secret>
JWT_KEY=<minimo-32-caracteres-seguros>
INTEGRATION_ENCRYPTION_KEY=<base64-32-bytes>
PROVISIONING_API_KEY=<secret-bootstrap>

# Desactivar demo
Seed__Enabled=false

# Base
Database__AutoMigrate=true
EventBus__Provider=RabbitMQ
ConnectionStrings__Redis=redis:6379
ASPNETCORE_ENVIRONMENT=Production

# Recomendado hasta configurar
AI__Enabled=false
Autonomous__Enabled=false
SaaS__EnforceSubscription=false
Communications__AllowSimulation=true

# Opcionales (activar cuando el cliente los necesite)
OPENAI_API_KEY=
SENDGRID_API_KEY=
STRIPE_SECRET_KEY=
HUBSPOT_CLIENT_ID=
HUBSPOT_CLIENT_SECRET=
```

---

## 7. Roles y permisos (realidad del código)

| Rol | Existe | Home post-login | Escritura comercial UI |
|-----|--------|-----------------|------------------------|
| Admin | ✅ | `/executive` | ✅ |
| Manager | ✅ | `/executive` | ✅ |
| Sales | ✅ | `/revenue` | ✅ |
| Support | ✅ | `/Customer360` | ❌ (solo lectura) |
| Viewer | ✅ | `/` | ❌ (solo lectura) |
| **SuperAdmin** | ❌ | — | — |

**Claims:** `ClaimTypes.Role` con valores de la tabla anterior.  
**Policies API:** `RequireAdmin`, `RequireManager`, `RequireSales` en `Authorization/Extensions.cs`.

---

## 8. Archivos de referencia

| Tema | Ruta |
|------|------|
| Startup + pipeline | `AutonomusCRM.API/Program.cs` |
| Migraciones/seed hooks | `AutonomusCRM.API/Extensions/WebApplicationExtensions.cs` |
| Guard producción | `AutonomusCRM.Infrastructure/Platform/ProductionConfigurationGuard.cs` |
| Provisioning API | `AutonomusCRM.API/Controllers/ProvisioningController.cs` |
| Provisioning service | `AutonomusCRM.Infrastructure/Tenancy/TenantProvisioningService.cs` |
| Seeder principal | `AutonomusCRM.Infrastructure/Persistence/Seed/DatabaseSeeder.cs` |
| Compose local | `docker-compose.yml` |
| Compose VPS | `deploy/docker-compose.vps.yml` |
| Settings base | `AutonomusCRM.API/appsettings.json` |

---

## 9. Checklist pre-entrega al cliente

- [ ] `Seed__Enabled=false` en todos los entornos del cliente
- [ ] `PROVISIONING_API_KEY` generado y documentado para ops (no para usuarios finales)
- [ ] Migraciones verificadas en BD vacía
- [ ] Primer tenant provisionado vía API (no seed)
- [ ] Admin puede login y crear equipo
- [ ] RabbitMQ + Redis healthy en `/health/ready`
- [ ] Workers desplegados si se prometen automatizaciones
- [ ] Decisión explícita sobre IA (off vs API key)
- [ ] Decisión sobre email (simulación vs SendGrid)
- [ ] Documentar que **no hay SuperAdmin** — Admin es máximo privilegio
