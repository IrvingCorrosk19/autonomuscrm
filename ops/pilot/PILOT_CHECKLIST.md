# PILOT CHECKLIST — Go / No-Go

**Cliente:** ______________________  
**Tenant ID:** ______________________  
**Fecha kickoff:** ______________________  
**Motor BD:** PostgreSQL únicamente ☐ confirmado

---

## A. Pre-requisitos Autonomus (interno)

| # | Item | OK | Notas |
|---|------|:--:|-------|
| A1 | `dotnet build` PASS | ☐ | |
| A2 | DIP tests 149/149 PASS | ☐ | `Category=DatabaseIntelligence` |
| A3 | Demo path 182/182 PASS | ☐ | Filtro demo completo (Sprint 3) |
| A4 | Full suite 520/520 PASS | ☐ | `dotnet test` |
| A5 | RabbitMQ operativo en entorno piloto | ☐ | |
| A6 | Agents autónomos desactivados en tenant piloto | ☐ | |
| A7 | Tenant aislado creado | ☐ | |
| A8 | Usuarios Admin + Manager entregados | ☐ | |
| A9 | Runbook + Recovery entregados al cliente | ☐ | |

---

## B. Pre-requisitos cliente (red + BD)

| # | Item | OK | Notas |
|---|------|:--:|-------|
| B1 | PostgreSQL accesible desde AutonomusCRM (host/puerto) | ☐ | |
| B2 | Usuario BD con SELECT (lectura) verificado | ☐ | |
| B3 | Firewall / allowlist configurado | ☐ | |
| B4 | Volumen acordado (< 500 tablas o muestreo OK) | ☐ | |
| B5 | Ventana de mantenimiento acordada | ☐ | |
| B6 | Contacto escalación designado | ☐ | |

---

## C. Flujo DIP — ejecución cliente

Marcar cuando el **cliente** completa sin ayuda de desarrollador:

| # | Paso | Ruta | OK | Fecha |
|---|------|------|:--:|-------|
| C1 | Conectar PostgreSQL | `/DatabaseIntelligence/Connect` | ☐ | |
| C2 | Test conexión PASS | Connect paso 3 | ☐ | |
| C3 | Discover completado | `/DatabaseIntelligence/Explore` | ☐ | |
| C4 | Entidades confirmadas | `/DatabaseIntelligence/Understand` | ☐ | |
| C5 | Health scan ejecutado | `/DatabaseIntelligence/Health` | ☐ | |
| C6 | Grafo generado | `/DatabaseIntelligence/Graph` | ☐ | |
| C7 | Insights generados | `/DatabaseIntelligence/Insights` | ☐ | |
| C8 | Operate — session started | `/DatabaseIntelligence/Operate` | ☐ | |
| C9 | Preview revisado | Operate Preview Studio | ☐ | |
| C10 | Execute completado | Operate | ☐ | |
| C11 | Import to CRM | Operate Result | ☐ | |
| C12 | Datos visibles en CRM | `/Customers` | ☐ | |
| C13 | Rollback ejecutado | Operate | ☐ | |
| C14 | CRM revertido post-rollback | `/Customers` | ☐ | |

---

## D. Escenarios de datos (validación)

| Escenario | Cómo probar | OK | Evidencia |
|-----------|-------------|:--:|-----------|
| D1 | Tenant nuevo (vacío) | Kickoff tenant limpio | ☐ | |
| D2 | Tenant existente (con CRM previo) | Segundo ciclo import | ☐ | |
| D3 | Datos limpios | Health score alto | ☐ | |
| D4 | Datos dañados | Health findings validity | ☐ | |
| D5 | Datos duplicados | Merge studio + health duplicates | ☐ | |
| D6 | Datos huérfanos | Health orphan findings | ☐ | |

*Tests automatizados de referencia:* `DataHealthSyntheticDatasets`, `OperationSyntheticDatasets`, `DbOperationIntegrationTests`.

---

## E. Go / No-Go final

| Criterio | Requerido |
|----------|-----------|
| C1–C14 completados por cliente | Sí |
| C13–C14 rollback OK | Sí |
| Sin intervención SQL manual | Sí |
| Sin acceso a código repositorio | Sí |

**Decisión piloto:** ☐ GO  ☐ NO-GO  

**Firma Autonomus:** ______________________ **Fecha:** __________  

**Firma cliente:** ______________________ **Fecha:** __________  

---

## Fuera de alcance (marcar si el cliente lo pidió — escalar)

- ☐ Oracle / SQL Server / MySQL day-1  
- ☐ Data Hub CSV masivo concurrente  
- ☐ Agents / Copilot / ABOS  
- ☐ SSO SAML producción  
- ☐ SLA 99.9%  
