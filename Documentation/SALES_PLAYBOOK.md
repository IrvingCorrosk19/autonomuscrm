# Playbook Comercial — AutonomusCRM

**Audiencia:** Ejecutivos de ventas (`Sales`), supervisores (`Manager`)  
**Perfil demo:** `sales@autonomuscrm.local` / `Sales123!`  
**Home post-login:** `/revenue` (Revenue OS)  
**Base:** funcionalidades verificadas en dominio y UI

---

## 1. Visión del ciclo comercial

```
Lead (New) → Contacto → Calificación → Deal (Pipeline) → Cierre → Handoff CS
```

AutonomusCRM no tiene entidad "Prospecto" separada: el contacto inicial es un **Lead** con estado `New`. La oportunidad comercial es un **Deal** vinculado obligatoriamente a un **Customer**.

---

## 2. Pipeline — gestión de oportunidades

### 2.1 Pantalla principal

**Ruta:** `/Deals`  
**Vistas:** Kanban por etapa y listado paginado.

### 2.2 Etapas del embudo (`DealStage`)

| Etapa | Código | Probabilidad típica | Acción del vendedor |
|-------|--------|---------------------|---------------------|
| Prospección | `Prospecting` | 10% | Primer contacto, validar interés |
| Calificación | `Qualification` | 25% | Confirmar necesidad y presupuesto |
| Propuesta | `Proposal` | 50% | Enviar propuesta formal |
| Negociación | `Negotiation` | 75% | Ajustar términos y precio |
| Ganado | `ClosedWon` | 100% | Cierre exitoso |
| Perdido | `ClosedLost` | 0% | Documentar razón de pérdida |

### 2.3 Crear oportunidad

1. `/Deals/Create` o desde `/Leads/Details/{id}` → **Create Deal**.
2. **Cliente obligatorio** — seleccionar existente o crear antes.
3. Completar: título, monto (`Amount`), descripción.
4. Deal inicia en `Prospecting`.

### 2.4 Avanzar etapas

- **UI:** editar deal en `/Deals/Edit/{id}` o arrastrar en Kanban.
- **Masivo:** `/Deals/BulkActions` con `BulkUpdateDealStageCommand`.
- **Automatización:** workflows en `/Workflows` pueden cambiar etapa por regla.

### 2.5 Deals estancados

El motor `CommercialSlaEngine` crea tareas `SLA_DealAtRisk` cuando detecta deals sin movimiento. Revisar en `/Tasks` con filtro de prioridad.

**Playbook deal estancado:**

1. Abrir deal en `/Deals/Details/{id}`.
2. Actualizar etapa, monto o fecha esperada de cierre.
3. Si no hay viabilidad → marcar `ClosedLost` con razón.
4. Manager revisa win rate en `/revenue` o `/executive` semanalmente.

---

## 3. Forecast — previsión de ingresos

### 3.1 Revenue OS (`/revenue`)

Dashboard unificado vía `IRevenueOsService`:

- KPIs de ingresos actuales y ponderados.
- Fugas de pipeline identificadas.
- Insights priorizados (`RevenueInsightDto`) con score de prioridad.
- Forecast predictivo (`PredictiveRevenueForecastDto`).

### 3.2 API de forecast

```http
GET /api/revenue/forecast?tenantId={guid}
```

Devuelve proyecciones por horizonte (30, 60, 90 días) vía `IRevenueForecastEngine`.

### 3.3 Interpretación para ventas

| Métrica | Uso diario |
|---------|------------|
| Forecast 30d | Compromisos de cierre del mes |
| Forecast 90d | Planificación trimestral |
| Weighted forecast | Pipeline realista (monto × probabilidad etapa) |
| Fugas | Deals/de clientes que requieren acción inmediata |

### 3.4 Rutina de forecast (viernes)

1. Revisar `/revenue` — comparar forecast vs cuota.
2. Validar deals en `Negotiation` y `Proposal` con fecha de cierre.
3. Mover deals irreales a etapa correcta o `ClosedLost`.
4. Compartir hallazgos con Manager en `/executive`.

---

## 4. Calificación de leads

### 4.1 Estados del lead (`LeadStatus`)

`New` → `Contacted` → `Qualified` → `Converted` / `Lost` / `Unqualified`

### 4.2 Fuentes de origen (`LeadSource`)

`Unknown` · `Website` · `Referral` · `SocialMedia` · `EmailCampaign` · `ColdCall` · `Partner` · `Event` · `Other`

### 4.3 Calificar un lead (`Qualify`)

**Ruta:** `/Leads/Details/{id}` → botón **Qualify**  
**Comando:** `QualifyLeadCommand` → `lead.Qualify()`

**Efectos automáticos tras calificación:**

1. Lead pasa a estado `Qualified`.
2. Se dispara `LeadQualifiedEvent`.
3. `RevenueAutomationEngine` puede crear tarea `SLA_QualifiedFollowUp`.
4. Workers de scoring actualizan prioridad según fuente.

### 4.4 SLA de contacto 24h

`CommercialSlaEngine` crea tarea `SLA_LeadContact24h` para leads en `New`. **Regla:** contactar en menos de 24 horas.

### 4.5 Playbook lead inbound

| Paso | Acción | Sistema |
|------|--------|---------|
| 1 | Lead creado (UI, API o import) | Estado `New` |
| 2 | Sales contacta prospecto | Cambiar a `Contacted` |
| 3 | Interés confirmado | **Qualify** |
| 4 | Revisar tarea auto y deal borrador | `/Tasks` |
| 5 | Actualizar monto y etapa | `/Deals` |

### 4.6 Convertir vs calificar

| Acción | Cuándo |
|--------|--------|
| **Qualify** | Interés confirmado; quiere pipeline automático |
| **Convert to Customer** | Cliente administrativo sin deal activo |
| **Create Deal** | Listo para vender con monto definido |

---

## 5. Cierre de deals

### 5.1 Cerrar como ganado

**UI:** `/Deals/Details/{id}` → acción de cierre.  
**API:**

```http
POST /api/deals/{id}/close
Content-Type: application/json

{
  "dealId": "{guid}",
  "tenantId": "{guid}",
  "finalAmount": 25000.00
}
```

**Comando:** `CloseDealCommand` → `deal.Close(utcNow, finalAmount)` → etapa `ClosedWon`.

### 5.2 Cerrar como perdido

Actualizar etapa a `ClosedLost` en `/Deals/Edit/{id}`. Documentar razón en descripción para análisis de win rate.

### 5.3 Efectos post-cierre

- `DealClosedEvent` alimenta `OutcomeAttributionService` y métricas de Revenue OS.
- `RetentionAutomationEngine` puede iniciar tareas de onboarding CS para deals `ClosedWon`.
- Manager ve impacto en `/executive` y forecast.

---

## 6. Handoff a retención (Customer Success)

### 6.1 Cuándo hacer handoff

Inmediatamente después de `ClosedWon`:

1. Verificar que el **Customer** tiene datos completos (email, teléfono, empresa).
2. Notificar a Support/CS del cierre.
3. Support ejecuta playbook **Onboarding** en `/customer-success`.

### 6.7 Playbook Onboarding (CS)

Ejecutado por Support desde `/customer-success` → `RunPlaybookAsync` con tipo `Onboarding`:

| Tarea creada | Plazo | Prioridad |
|--------------|-------|-----------|
| Kick-off onboarding | 1 día | High |
| Configuración cuenta | 3 días | Normal |
| Capacitación | 7 días | Normal |

### 6.3 Información a transferir

| Campo | Destino |
|-------|---------|
| Contacto principal | Customer en `/Customers/Details` |
| Monto cerrado | Deal `ClosedWon` |
| Notas de implementación | Descripción del deal |
| Expectativas del cliente | Caso CS en `/customer-success` |

### 6.4 Coordinación Sales ↔ Support

- Sales mantiene relación comercial durante onboarding (primeros 30 días).
- Support lidera tickets (`CS_Ticket`) y casos (`CS_Case_*`).
- Si health score cae a `Critical`, CS ejecuta playbook **Rescue** — Sales apoya si hay oportunidad de expansión.

---

## 7. Rutina diaria del vendedor

| Momento | Pantalla | Acción |
|---------|----------|--------|
| 08:00 | `/revenue` | Revisar prioridades y forecast |
| 08:15 | `/Tasks` | Completar SLA y tareas vencidas |
| 09:00 | `/Leads` | Contactar leads `New` < 24h |
| 10:00–16:00 | `/Deals` | Avanzar pipeline activo |
| 17:00 | `/Tasks` | Cerrar tareas del día |
| Viernes | `/revenue` | Revisión forecast semanal |

---

## 8. Métricas clave

| Métrica | Dónde verla |
|---------|-------------|
| Win rate | `/revenue`, `/executive` |
| Pipeline total | `/Deals`, `/revenue` |
| Leads por fuente | `/Leads` (stats `LeadSourceStat`) |
| SLA vencidos | `/Tasks` (filtro overdue) |
| Performance por vendedor | `/executive` (`SalesPerformanceEngine`) |

---

## 9. Errores comunes y solución

| Error | Causa | Solución |
|-------|-------|----------|
| No puedo crear deal sin cliente | Validación de dominio | Crear Customer primero en `/Customers/Create` |
| Lead no califica | Ya en estado final | Verificar estado en `/Leads/Details` |
| Access Denied en `/Users` | Rol Sales sin permiso admin | Escalar a Manager |
| Forecast no cuadra | Etapas desactualizadas | Actualizar deals en `Negotiation`/`Proposal` |
| Sin tarea SLA | Lead ya contactado | Verificar estado `Contacted` |

---

## 10. Integraciones relevantes para ventas

| Integración | Uso comercial |
|-------------|---------------|
| HubSpot | Sync bidireccional de leads/deals |
| Salesforce | Sync bidireccional de oportunidades |
| Gmail/Outlook | Comunicación (vía integración) |
| Stripe | Facturación post-cierre |

Configurar en `/Integrations` (Admin/Manager). Sales consume datos sincronizados, no configura integraciones.

---

*Documento basado en: `Deal.cs`, `Lead.cs`, `QualifyLeadCommand`, `CloseDealCommand`, `RevenueOsService`, `CommercialSlaEngine`, `CustomerPlaybookService`, `RoleHomeRedirect.cs`.*
