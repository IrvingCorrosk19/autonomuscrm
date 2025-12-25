# MANUAL DE PRUEBAS DETALLADO - AUTONOMUS CRM

**Versión del Sistema**: 1.0  
**Fecha**: 2024-12-24  
**Objetivo**: Guía completa para probar todas las funcionalidades del sistema

---

## ÍNDICE

1. [Preparación del Entorno](#1-preparación-del-entorno)
2. [Pruebas de Configuración](#2-pruebas-de-configuración)
3. [Pruebas de Navegación y Rutas](#3-pruebas-de-navegación-y-rutas)
4. [Pruebas de Creación de Datos](#4-pruebas-de-creación-de-datos)
5. [Pruebas de Consulta y Visualización](#5-pruebas-de-consulta-y-visualización)
6. [Pruebas de Actualización](#6-pruebas-de-actualización)
7. [Pruebas de Eventos y Agentes](#7-pruebas-de-eventos-y-agentes)
8. [Pruebas de Autenticación y Autorización](#8-pruebas-de-autenticación-y-autorización)
9. [Pruebas de Integración](#9-pruebas-de-integración)
10. [Checklist de Pruebas](#10-checklist-de-pruebas)

---

## 1. PREPARACIÓN DEL ENTORNO

### 1.1 Requisitos Previos

**Software Necesario**:
- ✅ .NET 9 SDK instalado
- ✅ PostgreSQL 12+ ejecutándose
- ✅ Docker (opcional, para PostgreSQL)
- ✅ Navegador web moderno (Chrome, Firefox, Edge)
- ✅ Herramienta para consultas SQL (pgAdmin, DBeaver, o psql)

**Servicios Requeridos**:
- ✅ PostgreSQL en `localhost:5432`
- ✅ Base de datos `autonomuscrm` creada
- ✅ Usuario `postgres` con contraseña configurada

### 1.2 Configuración Inicial

#### Paso 1: Verificar Base de Datos
```bash
# Conectar a PostgreSQL
psql -U postgres -d autonomuscrm

# Verificar que la base de datos existe
\l

# Verificar tablas creadas
\dt
```

**Resultado Esperado**: 
- Base de datos `autonomuscrm` existe
- Tablas principales creadas: `Tenants`, `Leads`, `Deals`, `Customers`, `Users`, etc.

#### Paso 2: Aplicar Migraciones
```bash
# Desde la raíz del proyecto
dotnet ef database update --project AutonomusCRM.Infrastructure
```

**Resultado Esperado**: 
- Migraciones aplicadas exitosamente
- Sin errores

#### Paso 3: Iniciar la Aplicación
```bash
# Desde la raíz del proyecto
dotnet run --project AutonomusCRM.API
```

**Resultado Esperado**: 
- Aplicación inicia sin errores
- Mensaje: "Starting AUTONOMUS CRM API"
- URL disponible: `https://localhost:7235` o `http://localhost:5000`

#### Paso 4: Verificar Health Checks
```bash
# En el navegador o con curl
curl https://localhost:7235/health
```

**Resultado Esperado**: 
- Status: `Healthy`
- Respuesta JSON con estado de servicios

---

## 2. PRUEBAS DE CONFIGURACIÓN

### 2.1 Verificar Configuración de Base de Datos

**Objetivo**: Confirmar que la conexión a PostgreSQL funciona

**Pasos**:
1. Abrir `appsettings.json` o `appsettings.Development.json`
2. Verificar connection string:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Port=5432;Database=autonomuscrm;Username=postgres;Password=Panama2020$"
   }
   ```
3. Iniciar la aplicación
4. Verificar logs: no debe haber errores de conexión

**Resultado Esperado**: ✅
- Sin errores de conexión en logs
- Health check `/health` retorna `Healthy`

**Criterios de Éxito**:
- ✅ Aplicación inicia sin errores
- ✅ Health check de base de datos pasa
- ✅ Logs muestran conexión exitosa

---

### 2.2 Verificar Configuración de JWT

**Objetivo**: Confirmar que JWT está configurado correctamente

**Pasos**:
1. Verificar `appsettings.json`:
   ```json
   "Jwt": {
     "Key": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
     "Issuer": "AutonomusCRM",
     "Audience": "AutonomusCRM"
   }
   ```
2. Iniciar la aplicación
3. Verificar que no hay errores relacionados con JWT

**Resultado Esperado**: ✅
- Sin errores de configuración JWT
- Aplicación inicia correctamente

---

## 3. PRUEBAS DE NAVEGACIÓN Y RUTAS

### 3.1 Prueba: Navegación Principal

**Objetivo**: Verificar que todas las rutas principales son accesibles

**Pasos**:

#### 3.1.1 Dashboard
1. Abrir navegador: `https://localhost:7235/`
2. Verificar que carga el Dashboard
3. Verificar elementos:
   - ✅ Sidebar visible
   - ✅ Estadísticas mostradas
   - ✅ Gráficos o métricas visibles

**Resultado Esperado**: ✅ Dashboard carga correctamente

#### 3.1.2 Página de Leads
1. Click en "Leads" en el sidebar
2. URL debe ser: `https://localhost:7235/Leads`
3. Verificar elementos:
   - ✅ Tabla de leads visible
   - ✅ Botón "Nuevo Lead" visible
   - ✅ Estadísticas de leads mostradas

**Resultado Esperado**: ✅ Página de Leads carga correctamente

#### 3.1.3 Página de Deals (Pipeline)
1. Click en "Pipeline" en el sidebar
2. URL debe ser: `https://localhost:7235/Deals`
3. Verificar elementos:
   - ✅ Pipeline visual visible
   - ✅ Botón "Nuevo Deal" visible
   - ✅ Estadísticas de deals mostradas

**Resultado Esperado**: ✅ Página de Deals carga correctamente

#### 3.1.4 Página de Customers
1. Click en "Clientes" en el sidebar
2. URL debe ser: `https://localhost:7235/Customers`
3. Verificar elementos:
   - ✅ Tabla de clientes visible
   - ✅ Botón "Nuevo Cliente" visible
   - ✅ Estadísticas de clientes mostradas

**Resultado Esperado**: ✅ Página de Customers carga correctamente

#### 3.1.5 Página de Agents
1. Click en "Agentes IA" en el sidebar
2. URL debe ser: `https://localhost:7235/Agents`
3. Verificar elementos:
   - ✅ Lista de 7 agentes visible
   - ✅ Estado de cada agente mostrado
   - ✅ Descripción de cada agente visible

**Resultado Esperado**: ✅ Página de Agents carga correctamente

#### 3.1.6 Página de Workflows
1. Click en "Workflows" en el sidebar
2. URL debe ser: `https://localhost:7235/Workflows`
3. Verificar elementos:
   - ✅ Lista de workflows visible
   - ✅ Información de workflows mostrada

**Resultado Esperado**: ✅ Página de Workflows carga correctamente

#### 3.1.7 Página de Policies
1. Click en "Políticas" en el sidebar
2. URL debe ser: `https://localhost:7235/Policies`
3. Verificar elementos:
   - ✅ Lista de políticas visible
   - ✅ Información de políticas mostrada

**Resultado Esperado**: ✅ Página de Policies carga correctamente

#### 3.1.8 Página de Users
1. Click en "Usuarios y Roles" en el sidebar
2. URL debe ser: `https://localhost:7235/Users`
3. Verificar elementos:
   - ✅ Lista de usuarios visible
   - ✅ Información de usuarios mostrada

**Resultado Esperado**: ✅ Página de Users carga correctamente

#### 3.1.9 Página de Audit
1. Click en "Auditoría" en el sidebar
2. URL debe ser: `https://localhost:7235/Audit`
3. Verificar elementos:
   - ✅ Registro de auditoría visible
   - ✅ Eventos mostrados

**Resultado Esperado**: ✅ Página de Audit carga correctamente

#### 3.1.10 Página de Settings
1. Click en "Configuración" en el sidebar
2. URL debe ser: `https://localhost:7235/Settings`
3. Verificar elementos:
   - ✅ Configuración del tenant visible
   - ✅ Opciones de configuración mostradas

**Resultado Esperado**: ✅ Página de Settings carga correctamente

---

### 3.2 Prueba: Rutas de Creación

**Objetivo**: Verificar que las páginas Create son accesibles

#### 3.2.1 Ruta Leads/Create
1. Navegar a: `https://localhost:7235/Leads/Create`
2. Verificar elementos:
   - ✅ Formulario visible
   - ✅ Campos: Nombre, Email, Teléfono, Empresa, Fuente
   - ✅ Botones: "Crear Lead", "Cancelar"

**Resultado Esperado**: ✅ Página Create de Leads carga correctamente

#### 3.2.2 Ruta Deals/Create
1. Navegar a: `https://localhost:7235/Deals/Create`
2. Verificar elementos:
   - ✅ Formulario visible
   - ✅ Campos: Cliente (dropdown), Título, Monto, Descripción
   - ✅ Botones: "Crear Deal", "Cancelar"

**Resultado Esperado**: ✅ Página Create de Deals carga correctamente

#### 3.2.3 Ruta Customers/Create
1. Navegar a: `https://localhost:7235/Customers/Create`
2. Verificar elementos:
   - ✅ Formulario visible
   - ✅ Campos: Nombre, Email, Teléfono, Empresa
   - ✅ Botones: "Crear Cliente", "Cancelar"

**Resultado Esperado**: ✅ Página Create de Customers carga correctamente

---

## 4. PRUEBAS DE CREACIÓN DE DATOS

### 4.1 Prueba: Crear Lead

**Objetivo**: Verificar que se puede crear un lead y se guarda en la base de datos

**Precondiciones**: 
- Aplicación ejecutándose
- Base de datos conectada
- Tenant por defecto existe (se crea automáticamente si no existe)

**Pasos**:

1. **Navegar a la página de creación**:
   - Ir a: `https://localhost:7235/Leads/Create`

2. **Completar el formulario**:
   - **Nombre**: "María González"
   - **Email**: "maria.gonzalez@empresa.com"
   - **Teléfono**: "+52 555 987 6543"
   - **Empresa**: "Tech Solutions S.A."
   - **Fuente**: Seleccionar "Sitio Web"

3. **Enviar el formulario**:
   - Click en botón "Crear Lead"

4. **Verificar redirección**:
   - Debe redirigir a: `https://localhost:7235/Leads`
   - Debe mostrar mensaje de éxito: "✓ Éxito: El lead ha sido creado correctamente."

5. **Verificar en la lista**:
   - El lead "María González" debe aparecer en la tabla
   - Verificar que muestra:
     - ✅ Nombre correcto
     - ✅ Email correcto
     - ✅ Empresa correcta
     - ✅ Score calculado (puede tomar unos segundos por el agente)

6. **Verificar en la base de datos**:
   ```sql
   SELECT * FROM "Leads" 
   WHERE "Name" = 'María González' 
   ORDER BY "CreatedAt" DESC 
   LIMIT 1;
   ```
   - Debe retornar 1 fila
   - Verificar campos:
     - ✅ `Name` = 'María González'
     - ✅ `Email` = 'maria.gonzalez@empresa.com'
     - ✅ `Phone` = '+52 555 987 6543'
     - ✅ `Company` = 'Tech Solutions S.A.'
     - ✅ `Source` = 1 (Website)
     - ✅ `Status` = 0 (New)
     - ✅ `TenantId` no es NULL
     - ✅ `Id` no es NULL
     - ✅ `CreatedAt` tiene fecha/hora actual

7. **Verificar evento guardado**:
   ```sql
   SELECT * FROM "DomainEvents" 
   WHERE "EventType" = 'LeadCreatedEvent' 
   ORDER BY "OccurredOn" DESC 
   LIMIT 1;
   ```
   - Debe retornar 1 fila
   - Verificar que `EventData` contiene información del lead

**Resultado Esperado**: ✅
- Lead creado exitosamente
- Aparece en la lista
- Guardado en base de datos
- Evento registrado

**Criterios de Éxito**:
- ✅ Formulario se envía sin errores
- ✅ Redirección correcta
- ✅ Mensaje de éxito visible
- ✅ Lead visible en la lista
- ✅ Lead guardado en BD
- ✅ Evento guardado en Event Store

---

### 4.2 Prueba: Crear Customer

**Objetivo**: Verificar que se puede crear un customer y se guarda en la base de datos

**Pasos**:

1. **Navegar a la página de creación**:
   - Ir a: `https://localhost:7235/Customers/Create`

2. **Completar el formulario**:
   - **Nombre**: "Carlos Rodríguez"
   - **Email**: "carlos.rodriguez@empresa.com"
   - **Teléfono**: "+52 555 123 4567"
   - **Empresa**: "Innovación Digital S.A."

3. **Enviar el formulario**:
   - Click en botón "Crear Cliente"

4. **Verificar redirección**:
   - Debe redirigir a: `https://localhost:7235/Customers`
   - Debe mostrar mensaje de éxito

5. **Verificar en la lista**:
   - El customer "Carlos Rodríguez" debe aparecer en la tabla

6. **Verificar en la base de datos**:
   ```sql
   SELECT * FROM "Customers" 
   WHERE "Name" = 'Carlos Rodríguez' 
   ORDER BY "CreatedAt" DESC 
   LIMIT 1;
   ```
   - Debe retornar 1 fila
   - Verificar campos:
     - ✅ `Name` = 'Carlos Rodríguez'
     - ✅ `Email` = 'carlos.rodriguez@empresa.com'
     - ✅ `Phone` = '+52 555 123 4567'
     - ✅ `Company` = 'Innovación Digital S.A.'
     - ✅ `Status` = 0 (Prospect)
     - ✅ `TenantId` no es NULL
     - ✅ `Id` no es NULL

7. **Verificar evento guardado**:
   ```sql
   SELECT * FROM "DomainEvents" 
   WHERE "EventType" = 'CustomerCreatedEvent' 
   ORDER BY "OccurredOn" DESC 
   LIMIT 1;
   ```
   - Debe retornar 1 fila

**Resultado Esperado**: ✅
- Customer creado exitosamente
- Aparece en la lista
- Guardado en base de datos
- Evento registrado
- RiskScore calculado por agente (puede tomar unos segundos)

---

### 4.3 Prueba: Crear Deal

**Objetivo**: Verificar que se puede crear un deal y se guarda en la base de datos

**Precondiciones**: 
- Debe existir al menos un Customer (crear uno si no existe)

**Pasos**:

1. **Crear un Customer primero** (si no existe):
   - Seguir pasos de prueba 4.2

2. **Navegar a la página de creación**:
   - Ir a: `https://localhost:7235/Deals/Create`

3. **Verificar que hay clientes disponibles**:
   - El dropdown de "Cliente" debe tener opciones
   - Si no hay clientes, debe mostrar mensaje: "No hay clientes disponibles. Crea uno primero"

4. **Completar el formulario**:
   - **Cliente**: Seleccionar un cliente del dropdown (ej: "Carlos Rodríguez")
   - **Título**: "Implementación CRM Enterprise"
   - **Monto**: 50000.00
   - **Descripción**: "Implementación completa del sistema CRM con integraciones"

5. **Enviar el formulario**:
   - Click en botón "Crear Deal"

6. **Verificar redirección**:
   - Debe redirigir a: `https://localhost:7235/Deals`
   - Debe mostrar mensaje de éxito

7. **Verificar en la lista**:
   - El deal "Implementación CRM Enterprise" debe aparecer en el pipeline
   - Verificar que muestra:
     - ✅ Título correcto
     - ✅ Monto correcto
     - ✅ Cliente asociado
     - ✅ Stage: Prospecting
     - ✅ Probability: 10% (o calculada)

8. **Verificar en la base de datos**:
   ```sql
   SELECT * FROM "Deals" 
   WHERE "Title" = 'Implementación CRM Enterprise' 
   ORDER BY "CreatedAt" DESC 
   LIMIT 1;
   ```
   - Debe retornar 1 fila
   - Verificar campos:
     - ✅ `Title` = 'Implementación CRM Enterprise'
     - ✅ `Amount` = 50000.00
     - ✅ `Description` contiene el texto
     - ✅ `CustomerId` no es NULL
     - ✅ `Status` = 0 (Open)
     - ✅ `Stage` = 0 (Prospecting)
     - ✅ `Probability` = 10 (o calculada)
     - ✅ `TenantId` no es NULL

9. **Verificar evento guardado**:
   ```sql
   SELECT * FROM "DomainEvents" 
   WHERE "EventType" = 'DealCreatedEvent' 
   ORDER BY "OccurredOn" DESC 
   LIMIT 1;
   ```
   - Debe retornar 1 fila

**Resultado Esperado**: ✅
- Deal creado exitosamente
- Aparece en el pipeline
- Guardado en base de datos
- Evento registrado
- Estrategia sugerida por agente (puede tomar unos segundos)

---

### 4.4 Prueba: Validación de Campos Requeridos

**Objetivo**: Verificar que los campos requeridos son validados

#### 4.4.1 Validación en Leads/Create

**Pasos**:
1. Ir a: `https://localhost:7235/Leads/Create`
2. Dejar campo "Nombre" vacío
3. Dejar campo "Fuente" sin seleccionar
4. Click en "Crear Lead"
5. Verificar que muestra error: "El nombre es requerido" o "La fuente es requerida"

**Resultado Esperado**: ✅
- Formulario no se envía
- Mensaje de error visible
- Campos requeridos marcados

#### 4.4.2 Validación en Deals/Create

**Pasos**:
1. Ir a: `https://localhost:7235/Deals/Create`
2. Dejar campo "Cliente" sin seleccionar
3. Dejar campo "Título" vacío
4. Dejar campo "Monto" vacío o en 0
5. Click en "Crear Deal"
6. Verificar que muestra errores apropiados

**Resultado Esperado**: ✅
- Formulario no se envía
- Mensajes de error visibles

#### 4.4.3 Validación en Customers/Create

**Pasos**:
1. Ir a: `https://localhost:7235/Customers/Create`
2. Dejar campo "Nombre" vacío
3. Click en "Crear Cliente"
4. Verificar que muestra error: "El nombre es requerido"

**Resultado Esperado**: ✅
- Formulario no se envía
- Mensaje de error visible

---

## 5. PRUEBAS DE CONSULTA Y VISUALIZACIÓN

### 5.1 Prueba: Visualizar Leads

**Objetivo**: Verificar que los leads se muestran correctamente

**Precondiciones**: 
- Al menos un lead creado (usar prueba 4.1)

**Pasos**:

1. **Navegar a Leads**:
   - Ir a: `https://localhost:7235/Leads`

2. **Verificar elementos de la página**:
   - ✅ Tabla de leads visible
   - ✅ Estadísticas mostradas:
     - Total leads
     - Score promedio
     - Conversión
     - Pendientes
   - ✅ Botón "Nuevo Lead" visible

3. **Verificar datos en la tabla**:
   - ✅ Leads creados aparecen en la lista
   - ✅ Columnas visibles: Lead, Score, Estado, Recomendación IA, Acciones
   - ✅ Datos correctos mostrados

4. **Verificar que los datos coinciden con BD**:
   ```sql
   SELECT COUNT(*) FROM "Leads";
   ```
   - Comparar con el número mostrado en "Total leads"

**Resultado Esperado**: ✅
- Lista de leads visible
- Estadísticas correctas
- Datos coinciden con BD

---

### 5.2 Prueba: Visualizar Deals (Pipeline)

**Objetivo**: Verificar que el pipeline se muestra correctamente

**Precondiciones**: 
- Al menos un deal creado (usar prueba 4.3)

**Pasos**:

1. **Navegar a Deals**:
   - Ir a: `https://localhost:7235/Deals`

2. **Verificar elementos de la página**:
   - ✅ Pipeline visual visible
   - ✅ Estadísticas mostradas:
     - Pipeline total
     - Deals activos
     - Tasa de cierre
     - Valor promedio
   - ✅ Botón "Nuevo Deal" visible

3. **Verificar datos en el pipeline**:
   - ✅ Deals creados aparecen en el pipeline
   - ✅ Deals organizados por etapa
   - ✅ Información correcta mostrada

4. **Verificar que los datos coinciden con BD**:
   ```sql
   SELECT COUNT(*) FROM "Deals" WHERE "Status" = 0;
   ```
   - Comparar con "Deals activos"

**Resultado Esperado**: ✅
- Pipeline visible
- Estadísticas correctas
- Datos coinciden con BD

---

### 5.3 Prueba: Visualizar Customers

**Objetivo**: Verificar que los customers se muestran correctamente

**Precondiciones**: 
- Al menos un customer creado (usar prueba 4.2)

**Pasos**:

1. **Navegar a Customers**:
   - Ir a: `https://localhost:7235/Customers`

2. **Verificar elementos de la página**:
   - ✅ Tabla de customers visible
   - ✅ Estadísticas mostradas:
     - Total clientes
     - LTV promedio
     - Riesgo promedio
     - Clientes VIP
   - ✅ Botón "Nuevo Cliente" visible

3. **Verificar datos en la tabla**:
   - ✅ Customers creados aparecen en la lista
   - ✅ Información correcta mostrada

**Resultado Esperado**: ✅
- Lista de customers visible
- Estadísticas correctas
- Datos coinciden con BD

---

### 5.4 Prueba: Dashboard con Estadísticas

**Objetivo**: Verificar que el dashboard muestra estadísticas correctas

**Precondiciones**: 
- Al menos un lead, un customer y un deal creados

**Pasos**:

1. **Navegar al Dashboard**:
   - Ir a: `https://localhost:7235/` o `https://localhost:7235/Index`

2. **Verificar elementos**:
   - ✅ Estadísticas principales visibles
   - ✅ Gráficos o visualizaciones
   - ✅ Pipeline por etapa
   - ✅ Recomendaciones de IA

3. **Verificar cálculos**:
   - ✅ Total Leads = número correcto
   - ✅ Total Deals = número correcto
   - ✅ Estimated Revenue = suma de deals abiertos
   - ✅ Conversion Rate = cálculo correcto

4. **Verificar que los datos coinciden con BD**:
   ```sql
   -- Total Leads
   SELECT COUNT(*) FROM "Leads";
   
   -- Total Deals
   SELECT COUNT(*) FROM "Deals";
   
   -- Estimated Revenue
   SELECT SUM("Amount") FROM "Deals" WHERE "Status" = 0;
   ```

**Resultado Esperado**: ✅
- Dashboard carga correctamente
- Estadísticas correctas
- Datos coinciden con BD

---

## 6. PRUEBAS DE ACTUALIZACIÓN

### 6.1 Prueba: Calificar Lead

**Objetivo**: Verificar que se puede calificar un lead

**Precondiciones**: 
- Al menos un lead con Status = New

**Pasos**:

1. **Obtener ID de un lead**:
   ```sql
   SELECT "Id", "Name", "Status" FROM "Leads" WHERE "Status" = 0 LIMIT 1;
   ```

2. **Usar API para calificar** (o implementar UI):
   ```bash
   # Nota: Esto requiere autenticación JWT
   # Por ahora, verificar que el handler existe y funciona
   ```

3. **Verificar en BD**:
   ```sql
   SELECT "Status" FROM "Leads" WHERE "Id" = '<lead-id>';
   ```
   - Debe ser: `Status = 2` (Qualified)

4. **Verificar evento**:
   ```sql
   SELECT * FROM "DomainEvents" 
   WHERE "EventType" = 'LeadQualifiedEvent' 
   ORDER BY "OccurredOn" DESC 
   LIMIT 1;
   ```

**Resultado Esperado**: ✅
- Lead calificado
- Status actualizado en BD
- Evento registrado

---

### 6.2 Prueba: Avanzar Deal de Etapa

**Objetivo**: Verificar que se puede avanzar un deal

**Precondiciones**: 
- Al menos un deal con Stage = Prospecting

**Pasos**:

1. **Obtener ID de un deal**:
   ```sql
   SELECT "Id", "Title", "Stage" FROM "Deals" WHERE "Stage" = 0 LIMIT 1;
   ```

2. **Actualizar etapa** (usar API o implementar UI):
   - Cambiar de Prospecting (0) a Qualification (1)

3. **Verificar en BD**:
   ```sql
   SELECT "Stage", "Probability" FROM "Deals" WHERE "Id" = '<deal-id>';
   ```
   - Debe ser: `Stage = 1` (Qualification)
   - `Probability` debe ser 25% (o calculada)

4. **Verificar evento**:
   ```sql
   SELECT * FROM "DomainEvents" 
   WHERE "EventType" = 'DealStageChangedEvent' 
   ORDER BY "OccurredOn" DESC 
   LIMIT 1;
   ```

**Resultado Esperado**: ✅
- Deal avanzado
- Stage y Probability actualizados
- Evento registrado

---

## 7. PRUEBAS DE EVENTOS Y AGENTES

### 7.1 Prueba: Verificar Event Store

**Objetivo**: Verificar que los eventos se guardan en Event Store

**Pasos**:

1. **Crear un Lead** (usar prueba 4.1)

2. **Verificar en Event Store**:
   ```sql
   SELECT 
     "EventType",
     "TenantId",
     "OccurredOn",
     "AggregateId"
   FROM "DomainEvents" 
   WHERE "EventType" = 'LeadCreatedEvent' 
   ORDER BY "OccurredOn" DESC 
   LIMIT 5;
   ```

3. **Verificar contenido del evento**:
   ```sql
   SELECT "EventData" 
   FROM "DomainEvents" 
   WHERE "EventType" = 'LeadCreatedEvent' 
   ORDER BY "OccurredOn" DESC 
   LIMIT 1;
   ```
   - Debe contener JSON con información del lead

**Resultado Esperado**: ✅
- Eventos guardados en Event Store
- EventData contiene información correcta
- Timestamps correctos

---

### 7.2 Prueba: Verificar Agente LeadIntelligenceAgent

**Objetivo**: Verificar que el agente calcula el score del lead

**Precondiciones**: 
- Lead creado (usar prueba 4.1)
- Worker ejecutándose (opcional, para procesamiento en tiempo real)

**Pasos**:

1. **Crear un Lead** con información completa:
   - Nombre: "Test Lead Agent"
   - Email: "test@example.com"
   - Teléfono: "+52 555 111 2222"
   - Empresa: "Test Company"
   - Fuente: "Referral"

2. **Esperar unos segundos** (para que el agente procese)

3. **Verificar score en BD**:
   ```sql
   SELECT "Name", "Score", "Source" 
   FROM "Leads" 
   WHERE "Name" = 'Test Lead Agent';
   ```
   - `Score` debe estar calculado (no NULL)
   - Score esperado para Referral + Email + Phone + Company: ~75

4. **Verificar evento de score**:
   ```sql
   SELECT * FROM "DomainEvents" 
   WHERE "EventType" = 'LeadScoreUpdatedEvent' 
   ORDER BY "OccurredOn" DESC 
   LIMIT 1;
   ```

**Resultado Esperado**: ✅
- Score calculado automáticamente
- Score guardado en BD
- Evento de actualización registrado

**Nota**: Si el Worker no está ejecutándose, el agente se ejecutará la próxima vez que el Worker inicie.

---

### 7.3 Prueba: Verificar Agente CustomerRiskAgent

**Objetivo**: Verificar que el agente calcula el risk score del customer

**Pasos**:

1. **Crear un Customer** (usar prueba 4.2)

2. **Esperar unos segundos**

3. **Verificar risk score en BD**:
   ```sql
   SELECT "Name", "RiskScore", "Email", "Company" 
   FROM "Customers" 
   WHERE "Name" = 'Carlos Rodríguez';
   ```
   - `RiskScore` debe estar calculado (no NULL)
   - Score esperado: ~35 (bajo riesgo si tiene email y company)

4. **Verificar evento**:
   ```sql
   SELECT * FROM "DomainEvents" 
   WHERE "EventType" = 'CustomerRiskScoreUpdatedEvent' 
   ORDER BY "OccurredOn" DESC 
   LIMIT 1;
   ```

**Resultado Esperado**: ✅
- RiskScore calculado automáticamente
- RiskScore guardado en BD
- Evento registrado

---

## 8. PRUEBAS DE AUTENTICACIÓN Y AUTORIZACIÓN

### 8.1 Prueba: Crear Usuario

**Objetivo**: Verificar que se puede crear un usuario

**Pasos**:

1. **Usar API para crear usuario**:
   ```bash
   # Nota: Requiere autenticación Admin
   curl -X POST https://localhost:7235/api/Users \
     -H "Content-Type: application/json" \
     -H "Authorization: Bearer <admin-token>" \
     -d '{
       "tenantId": "<tenant-id>",
       "email": "test@example.com",
       "password": "Test123!",
       "firstName": "Test",
       "lastName": "User"
     }'
   ```

2. **Verificar en BD**:
   ```sql
   SELECT "Email", "FirstName", "LastName", "IsActive" 
   FROM "Users" 
   WHERE "Email" = 'test@example.com';
   ```

**Resultado Esperado**: ✅
- Usuario creado
- Password hasheado (no visible en texto plano)
- Guardado en BD

---

### 8.2 Prueba: Login

**Objetivo**: Verificar que el login funciona

**Precondiciones**: 
- Usuario creado (usar prueba 8.1)

**Pasos**:

1. **Hacer login**:
   ```bash
   curl -X POST https://localhost:7235/api/Auth/login \
     -H "Content-Type: application/json" \
     -d '{
       "tenantId": "<tenant-id>",
       "email": "test@example.com",
       "password": "Test123!"
     }'
   ```

2. **Verificar respuesta**:
   - Debe retornar JWT token
   - Debe retornar refresh token
   - Debe retornar expiresAt

3. **Verificar en BD**:
   ```sql
   SELECT "LastLoginAt" 
   FROM "Users" 
   WHERE "Email" = 'test@example.com';
   ```
   - `LastLoginAt` debe tener fecha/hora actual

**Resultado Esperado**: ✅
- Login exitoso
- Token JWT generado
- LastLoginAt actualizado

---

## 9. PRUEBAS DE INTEGRACIÓN

### 9.1 Prueba: Flujo Completo Lead → Customer → Deal

**Objetivo**: Verificar el flujo completo de negocio

**Pasos**:

1. **Crear Lead** (usar prueba 4.1)
   - Nombre: "Flujo Completo Test"
   - Email: "flujo@test.com"
   - Empresa: "Test Company"

2. **Verificar Lead creado**:
   - ✅ Aparece en `/Leads`
   - ✅ Guardado en BD

3. **Calificar Lead** (usar prueba 6.1)
   - Cambiar Status a Qualified

4. **Crear Customer desde Lead**:
   - Usar mismo nombre/email del lead
   - Crear customer (usar prueba 4.2)

5. **Verificar Customer creado**:
   - ✅ Aparece en `/Customers`
   - ✅ Guardado en BD

6. **Crear Deal para el Customer**:
   - Seleccionar el customer creado
   - Crear deal (usar prueba 4.3)

7. **Verificar Deal creado**:
   - ✅ Aparece en `/Deals`
   - ✅ Guardado en BD
   - ✅ CustomerId correcto

8. **Verificar en Dashboard**:
   - ✅ Estadísticas actualizadas
   - ✅ Pipeline muestra el deal
   - ✅ Métricas correctas

**Resultado Esperado**: ✅
- Flujo completo ejecutado
- Todos los datos guardados
- Relaciones correctas
- Estadísticas actualizadas

---

### 9.2 Prueba: Multi-Tenant (Aislamiento)

**Objetivo**: Verificar que los datos están aislados por tenant

**Pasos**:

1. **Crear Tenant 1** (si no existe):
   ```sql
   INSERT INTO "Tenants" ("Id", "Name", "Description", "IsActive", "CreatedAt")
   VALUES (gen_random_uuid(), 'Tenant 1', 'Test Tenant 1', true, NOW());
   ```

2. **Crear Lead para Tenant 1**:
   - Usar TenantId del Tenant 1
   - Nombre: "Lead Tenant 1"

3. **Crear Tenant 2**:
   ```sql
   INSERT INTO "Tenants" ("Id", "Name", "Description", "IsActive", "CreatedAt")
   VALUES (gen_random_uuid(), 'Tenant 2', 'Test Tenant 2', true, NOW());
   ```

4. **Crear Lead para Tenant 2**:
   - Usar TenantId del Tenant 2
   - Nombre: "Lead Tenant 2"

5. **Verificar aislamiento**:
   ```sql
   -- Leads del Tenant 1
   SELECT * FROM "Leads" WHERE "TenantId" = '<tenant-1-id>';
   
   -- Leads del Tenant 2
   SELECT * FROM "Leads" WHERE "TenantId" = '<tenant-2-id>';
   ```
   - Cada tenant solo debe ver sus propios leads

**Resultado Esperado**: ✅
- Datos aislados por tenant
- No hay mezcla de datos entre tenants

---

## 10. CHECKLIST DE PRUEBAS

### 10.1 Checklist de Configuración

- [ ] Base de datos PostgreSQL ejecutándose
- [ ] Base de datos `autonomuscrm` creada
- [ ] Migraciones aplicadas
- [ ] Aplicación inicia sin errores
- [ ] Health checks pasan
- [ ] Connection string correcta

### 10.2 Checklist de Navegación

- [ ] Dashboard carga (`/`)
- [ ] Leads carga (`/Leads`)
- [ ] Deals carga (`/Deals`)
- [ ] Customers carga (`/Customers`)
- [ ] Agents carga (`/Agents`)
- [ ] Workflows carga (`/Workflows`)
- [ ] Policies carga (`/Policies`)
- [ ] Users carga (`/Users`)
- [ ] Audit carga (`/Audit`)
- [ ] Settings carga (`/Settings`)

### 10.3 Checklist de Creación

- [ ] `/Leads/Create` accesible
- [ ] Crear Lead funciona
- [ ] Lead guardado en BD
- [ ] Evento guardado
- [ ] `/Customers/Create` accesible
- [ ] Crear Customer funciona
- [ ] Customer guardado en BD
- [ ] Evento guardado
- [ ] `/Deals/Create` accesible
- [ ] Crear Deal funciona
- [ ] Deal guardado en BD
- [ ] Evento guardado

### 10.4 Checklist de Validación

- [ ] Validación de campos requeridos en Leads
- [ ] Validación de campos requeridos en Customers
- [ ] Validación de campos requeridos en Deals
- [ ] Mensajes de error visibles
- [ ] Formularios no se envían con datos inválidos

### 10.5 Checklist de Visualización

- [ ] Lista de Leads muestra datos correctos
- [ ] Pipeline de Deals muestra datos correctos
- [ ] Lista de Customers muestra datos correctos
- [ ] Dashboard muestra estadísticas correctas
- [ ] Datos coinciden con BD

### 10.6 Checklist de Eventos

- [ ] Eventos se guardan en Event Store
- [ ] EventData contiene información correcta
- [ ] Timestamps correctos
- [ ] CorrelationId presente

### 10.7 Checklist de Agentes

- [ ] LeadIntelligenceAgent calcula scores
- [ ] CustomerRiskAgent calcula risk scores
- [ ] DealStrategyAgent sugiere estrategias
- [ ] Eventos de agentes guardados

### 10.8 Checklist de Integración

- [ ] Flujo Lead → Customer → Deal funciona
- [ ] Relaciones correctas en BD
- [ ] Estadísticas actualizadas
- [ ] Multi-tenant funciona

---

## 11. CASOS DE PRUEBA ESPECÍFICOS

### 11.1 Caso de Prueba: Crear Lead con Todos los Campos

**Datos de Prueba**:
- Nombre: "Juan Pérez"
- Email: "juan.perez@empresa.com"
- Teléfono: "+52 555 123 4567"
- Empresa: "Empresa XYZ S.A."
- Fuente: "Referral"

**Resultado Esperado**:
- Lead creado con Score alto (~75-80)
- Todos los campos guardados correctamente
- Evento LeadCreatedEvent guardado

---

### 11.2 Caso de Prueba: Crear Lead con Mínimos Campos

**Datos de Prueba**:
- Nombre: "María López"
- Fuente: "Website"
- (Sin email, teléfono, empresa)

**Resultado Esperado**:
- Lead creado con Score bajo (~20-25)
- Solo campos requeridos guardados
- Evento guardado

---

### 11.3 Caso de Prueba: Crear Deal sin Cliente

**Datos de Prueba**:
- Intentar crear deal sin seleccionar cliente

**Resultado Esperado**:
- Error: "Debes seleccionar un cliente"
- Formulario no se envía
- Mensaje de error visible

---

### 11.4 Caso de Prueba: Crear Deal con Monto Negativo

**Datos de Prueba**:
- Monto: -1000

**Resultado Esperado**:
- Error: "El monto debe ser mayor a cero"
- Formulario no se envía

---

### 11.5 Caso de Prueba: Crear Customer Duplicado

**Datos de Prueba**:
- Crear customer con email existente

**Resultado Esperado**:
- Depende de la validación implementada
- Puede permitir o rechazar duplicados

---

## 12. PRUEBAS DE RENDIMIENTO BÁSICAS

### 12.1 Prueba: Tiempo de Respuesta

**Objetivo**: Verificar que las páginas cargan en tiempo razonable

**Pasos**:
1. Abrir DevTools del navegador (F12)
2. Ir a pestaña "Network"
3. Navegar a cada página
4. Verificar tiempo de carga:
   - ✅ Dashboard: < 2 segundos
   - ✅ Leads: < 1 segundo
   - ✅ Deals: < 1 segundo
   - ✅ Customers: < 1 segundo

**Resultado Esperado**: ✅
- Todas las páginas cargan en < 2 segundos

---

### 12.2 Prueba: Crear Múltiples Leads

**Objetivo**: Verificar que se pueden crear múltiples leads rápidamente

**Pasos**:
1. Crear 10 leads consecutivos
2. Verificar que todos se guardan
3. Verificar que todos aparecen en la lista

**Resultado Esperado**: ✅
- Todos los leads creados
- Todos guardados en BD
- Lista muestra todos

---

## 13. PRUEBAS DE ERRORES Y CASOS LÍMITE

### 13.1 Prueba: Conexión a BD Perdida

**Objetivo**: Verificar manejo de errores cuando BD no está disponible

**Pasos**:
1. Detener PostgreSQL
2. Intentar crear un lead
3. Verificar que muestra error apropiado
4. Reiniciar PostgreSQL
5. Verificar que vuelve a funcionar

**Resultado Esperado**: ✅
- Error manejado correctamente
- Mensaje de error visible al usuario
- No crashea la aplicación

---

### 13.2 Prueba: Datos Muy Largos

**Objetivo**: Verificar validación de longitud de campos

**Pasos**:
1. Intentar crear lead con nombre de 500 caracteres
2. Verificar que se valida o trunca

**Resultado Esperado**: ✅
- Validación o truncamiento apropiado

---

## 14. INSTRUCCIONES PARA REGISTRAR RESULTADOS

### 14.1 Plantilla de Registro

Para cada prueba, registrar:

```
PRUEBA: [Nombre de la prueba]
FECHA: [Fecha]
EJECUTADO POR: [Nombre]
RESULTADO: [✅ PASÓ / ❌ FALLÓ]
OBSERVACIONES: [Notas adicionales]
EVIDENCIA: [Screenshots, logs, etc.]
```

### 14.2 Ejemplo de Registro

```
PRUEBA: Crear Lead
FECHA: 2024-12-24
EJECUTADO POR: Tester
RESULTADO: ✅ PASÓ
OBSERVACIONES: Lead creado correctamente, score calculado en 3 segundos
EVIDENCIA: Screenshot_lead_created.png, log_2024-12-24.txt
```

---

## 15. COMANDOS ÚTILES PARA PRUEBAS

### 15.1 Consultas SQL Útiles

```sql
-- Ver todos los leads
SELECT * FROM "Leads" ORDER BY "CreatedAt" DESC;

-- Ver todos los deals
SELECT * FROM "Deals" ORDER BY "CreatedAt" DESC;

-- Ver todos los customers
SELECT * FROM "Customers" ORDER BY "CreatedAt" DESC;

-- Ver eventos recientes
SELECT "EventType", "OccurredOn", "TenantId" 
FROM "DomainEvents" 
ORDER BY "OccurredOn" DESC 
LIMIT 20;

-- Contar leads por tenant
SELECT "TenantId", COUNT(*) 
FROM "Leads" 
GROUP BY "TenantId";

-- Ver leads con scores
SELECT "Name", "Score", "Status", "Source" 
FROM "Leads" 
WHERE "Score" IS NOT NULL 
ORDER BY "Score" DESC;

-- Limpiar datos de prueba (CUIDADO: elimina datos)
-- DELETE FROM "Leads" WHERE "Name" LIKE 'Test%';
-- DELETE FROM "Customers" WHERE "Name" LIKE 'Test%';
-- DELETE FROM "Deals" WHERE "Title" LIKE 'Test%';
```

### 15.2 Comandos de Terminal

```bash
# Ver logs de la aplicación
tail -f logs/autonomuscrm-*.txt

# Verificar que la aplicación está ejecutándose
curl https://localhost:7235/health

# Reiniciar la aplicación
# Ctrl+C para detener
dotnet run --project AutonomusCRM.API
```

---

## 16. TROUBLESHOOTING

### 16.1 Problema: No se guardan los datos

**Síntomas**: 
- Formulario se envía pero datos no aparecen
- No hay errores visibles

**Verificaciones**:
1. Verificar logs de la aplicación
2. Verificar conexión a BD:
   ```sql
   SELECT 1;
   ```
3. Verificar que `SaveChangesAsync()` se llama
4. Verificar que no hay transacciones abiertas

**Solución**:
- Revisar logs para errores
- Verificar connection string
- Verificar que migraciones están aplicadas

---

### 16.2 Problema: Página no carga

**Síntomas**: 
- Error 404 o página en blanco

**Verificaciones**:
1. Verificar que la ruta es correcta
2. Verificar que el archivo existe
3. Verificar logs de la aplicación
4. Verificar que compila sin errores

**Solución**:
- Verificar rutas en `_Layout.cshtml`
- Recompilar la aplicación
- Verificar logs

---

### 16.3 Problema: Agentes no procesan eventos

**Síntomas**: 
- Scores no se calculan
- Risk scores no se calculan

**Verificaciones**:
1. Verificar que el Worker está ejecutándose:
   ```bash
   dotnet run --project AutonomusCRM.Workers
   ```
2. Verificar eventos en Event Store
3. Verificar logs del Worker

**Solución**:
- Iniciar el Worker
- Verificar suscripciones de eventos
- Verificar logs

---

## 17. CONCLUSIÓN

Este manual cubre todas las pruebas necesarias para verificar el funcionamiento completo del sistema AUTONOMUS CRM.

**Próximos Pasos**:
1. Ejecutar todas las pruebas en orden
2. Registrar resultados
3. Reportar problemas encontrados
4. Verificar correcciones

**Estado Esperado**: 
- ✅ Todas las rutas funcionan
- ✅ Todos los formularios funcionan
- ✅ Todos los datos se guardan
- ✅ Todos los eventos se registran
- ✅ Todos los agentes procesan eventos

---

**Última actualización**: 2024-12-24  
**Versión del Manual**: 1.0

