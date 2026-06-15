# PILOT RUNBOOK — Primer cliente de pago (DIP PostgreSQL)

**Versión:** 1.0  
**Fecha:** 2026-05-28  
**Baseline:** `AUTONOMUSCRM_REALITY_CHECK_2026.md`  
**Audiencia:** Admin/Manager del cliente piloto (sin SQL, sin código)

---

## Alcance del piloto

| Incluido | Excluido |
|----------|----------|
| PostgreSQL del cliente (lectura + import explícito) | Oracle, SQL Server, MySQL, MariaDB day-1 |
| Flujo DIP completo (Connect → Rollback) | Data Hub CSV como obligatorio |
| Tenant aislado dedicado | Agents autónomos / Executive Copilot |
| Soporte Autonomus en kickoff + semanas 1–4 | S7 enterprise hardening |

---

## Roles

| Rol | Responsabilidad |
|-----|-----------------|
| **Cliente — Owner/Admin** | Conexión BD, confirmar entidades, ejecutar Operate, importar |
| **Cliente — Manager** | Health, Graph, Insights, preview Operate |
| **Autonomus — CS/Implementación** | Kickoff, firewall checklist, tenant provisioning (una vez) |
| **Autonomus — Soporte** | Escalación según `PILOT_SUPPORT_GUIDE.md` |

---

## Fase 0 — Kickoff (Autonomus + cliente, 1 sesión)

1. Crear tenant piloto aislado (sin datos de otros clientes).
2. Crear usuario Admin + Manager con credenciales entregadas de forma segura.
3. Validar conectividad de red: cliente PostgreSQL accesible desde AutonomusCRM (IP allowlist / VPN).
4. Desactivar en tenant piloto: `AutonomousPlatformGate` / agents (config ops).
5. Entregar `PILOT_CHECKLIST.md` al cliente.

**Criterio de salida:** login OK + tenant vacío o con datos mínimos acordados.

---

## Fase 1 — Conectar PostgreSQL

**Ruta:** `/DatabaseIntelligence/Connect`

1. Iniciar sesión como Admin.
2. Paso 1 — Elegir **PostgreSQL**.
3. Paso 2 — Nombre amigable, host, puerto, base de datos, usuario, contraseña (solo lectura recomendado).
4. Paso 3 — **Test connection** → guardar si pasa.

**Sin SQL.** Si falla: ver `PILOT_RECOVERY_GUIDE.md` § Conexión.

---

## Fase 2 — Discover

**Ruta:** `/DatabaseIntelligence/Explore`

1. Seleccionar la conexión creada.
2. Pulsar **Discover now** (o job de descubrimiento).
3. Esperar progreso SignalR (barra de estado en página).
4. Revisar catálogo en lenguaje de negocio (tablas inferidas).

**Criterio de salida:** catálogo visible con tablas del negocio del cliente.

---

## Fase 3 — Understand

**Ruta:** `/DatabaseIntelligence/Understand`

1. Revisar entidades inferidas (Customer, Invoice, Payment, etc.).
2. Confirmar o ajustar mappings con la UI (sin SQL).
3. Guardar confirmaciones — **obligatorio antes de Operate**.

**Criterio de salida:** mappings confirmados (badge/estado verde en UI).

---

## Fase 4 — Health

**Ruta:** `/DatabaseIntelligence/Health`

1. Ejecutar **Run health scan**.
2. Revisar score global y hallazgos (duplicados, huérfanos, integridad).
3. Anotar IDs de findings críticos para narrativa con stakeholders.

**Escenarios piloto:**

| Dataset | Qué esperar |
|---------|-------------|
| Limpio | Score ≥ 80, pocos hallazgos |
| Dañado / huérfano / duplicado | Hallazgos Critical/High visibles — no bloquean flujo |

---

## Fase 5 — Graph

**Ruta:** `/DatabaseIntelligence/Graph`

1. **Build graph**.
2. Explorar relaciones cliente → factura → pago.
3. Exportar PNG/JSON si se requiere evidencia ejecutiva.

---

## Fase 6 — Insights

**Ruta:** `/DatabaseIntelligence/Insights`

1. Generar insights desde health + grafo.
2. Priorizar recomendaciones de negocio (no SQL).
3. *Opcional:* insights semánticos requieren LLM configurado — no bloquean piloto base.

---

## Fase 7 — Operate (preparar → importar)

**Ruta:** `/DatabaseIntelligence/Operate`

1. Seleccionar conexión → **Start session** (carga staging operacional).
2. En **Filter Studio** — activar reglas si aplica (ej. montos mínimos).
3. **Clean Studio** — normalizar email/teléfono.
4. **Merge Studio** — fusionar duplicados por email.
5. **Enrichment / Exclusion / Transformation** — según necesidad del piloto.
6. **Preview Studio** — revisar before/after e impacto.
7. **Execute** — aplicar transformaciones en staging.
8. **Result Studio** — validar métricas.
9. **Import to CRM** — cargar entidades acordadas.
10. Verificar en CRM (`/Customers`, `/Deals`) que datos aparecen.

**Sin SQL.** Plan visual vía formularios (`OperatePlanBuilder` — no plan hardcoded).

---

## Fase 8 — Rollback

**Ruta:** `/DatabaseIntelligence/Operate` (mismo job) o API rollback documentada en Recovery Guide.

1. Con el job de import reciente, ejecutar **Rollback**.
2. Confirmar que entidades importadas se revierten en CRM.
3. Documentar evidencia (capturas + timestamp audit).

**Criterio de éxito del piloto:** 1 ciclo completo import → verificación CRM → rollback exitoso.

---

## Duración orientativa

| Sesión | Tiempo | Contenido |
|--------|--------|-----------|
| Kickoff | 2 h | Tenant, red, login, Connect |
| Sesión 1 | 2 h | Discover + Understand |
| Sesión 2 | 2 h | Health + Graph + Insights |
| Sesión 3 | 3 h | Operate preview → import |
| Sesión 4 | 1 h | Rollback + retrospectiva |

**Total cliente autónomo post-kickoff:** ~6–8 h en UI, sin desarrollador.

---

## Infraestructura AutonomusCRM (ops, no cliente)

Antes del piloto, Autonomus debe tener:

- PostgreSQL app + migraciones aplicadas
- Redis
- RabbitMQ (progress async Data Hub/DIP workers)
- Secrets: JWT, encryption keys, connection vault
- `Seed:Enabled` off en producción piloto (datos solo del cliente)

Ver `docker-compose.yml` como referencia local.

---

## Comandos de verificación interna (Autonomus, pre-piloto)

```powershell
dotnet build
dotnet test --filter "Category=DatabaseIntelligence"
dotnet test --filter "Category=DatabaseIntelligence|Category=Demo|Category=DataHubE2E|Category=Phase4Validation|Category=DataHubRabbitMq|FullyQualifiedName~DataHubCertification"
dotnet test
```

Objetivo: **0 FAIL** en categorías anteriores antes de abrir piloto.
