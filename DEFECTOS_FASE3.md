# DEFECTOS_FASE3

---

## DEF-F3-001 (CLOSED)

- **Título:** RabbitMQ routing key mismatch — workers no consumían eventos
- **Severidad:** Crítica
- **Caso:** WORKERS validation
- **Archivo:** `RabbitMQEventBus.cs`
- **Root cause:** Publish `Lead.Created` vs bind `LeadCreatedEvent`
- **Fix:** `DomainEventRouting.cs`
- **Estado:** CLOSED (código); runtime BLOCKED sin Docker

---

## DEF-F3-002 (CLOSED)

- **Título:** Sin tenant QA-B para pruebas aislamiento
- **Severidad:** Alta
- **Caso:** TEN-002
- **Fix:** `QaTenantSeeder`, `Tenant.CreateWithId`
- **Estado:** CLOSED — pruebas PASS

---

## DEF-F3-003 (CLOSED)

- **Título:** GET /api/leads/{id} stub (DEF-007 Fase 2)
- **Severidad:** Media
- **Fix:** `GetLeadByIdQuery` + handler
- **Estado:** CLOSED

---

## DEF-F3-004 (CLOSED)

- **Título:** WorkflowEngine acciones TODO
- **Severidad:** Alta
- **Fix:** Implementación Assign/UpdateStatus/CreateTask
- **Estado:** CLOSED — requiere workflow configurado en BD para ver efecto

---

## DEF-F3-005 (OPEN)

- **Título:** Docker/RabbitMQ no validado en CI local
- **Severidad:** Alta operacional
- **Impacto:** Agentes IA no procesan en dev sin compose
- **Estado:** OPEN

---

## DEF-F3-006 (OPEN)

- **Título:** Import CSV parser frágil
- **Severidad:** Media
- **Estado:** OPEN — ver OWASP-01

---

## DEF-F3-007 (OPEN)

- **Título:** UX placeholders restantes (Policies, Workflows, Settings, Users)
- **Severidad:** Baja
- **Estado:** OPEN

---

## DEF-F3-008 (OPEN)

- **Título:** API deal stage + concurrencia no expuesta REST
- **Severidad:** Media
- **Estado:** OPEN
