# 09 — Troubleshooting Operativo

| Problema | Causa probable | Diagnóstico | Solución | Escalamiento |
|----------|---------------|-------------|----------|--------------|
| Access Denied al crear Lead | Rol Support/Viewer | Ver rol en menú usuario | Solicitar rol Sales a Manager | Admin asigna rol |
| No aparece deal tras Qualify | Filtro pipeline / deal borrador $1 | Buscar en `/Deals` todos los stages | Editar monto y etapa del borrador | — |
| Tareas no se crean | Worker caído / RabbitMQ | `docker ps`, logs workers | Reiniciar stack VPS | DevOps |
| Score lead siempre vacío | Worker no procesó evento | `/FailedEvents`, logs LeadIntelligenceAgent | Replay DLQ | Admin |
| Forecast en $0 | Sin deals abiertos con fecha cierre | Revisar ExpectedCloseDate | Actualizar deals | Sales |
| Email no enviado | Comms no configurado | Banner en layout → Settings | Configurar SendGrid/WhatsApp | Admin |
| Integración OAuth falla | Callback URL / credenciales | `/Integrations` mensaje error | Reconectar OAuth | Admin |
| Login MFA bloqueado | MFA habilitado sin setup | Pantalla login MFA | Admin reset MFA en Users | Admin |
| Datos de otro tenant | Sesión incorrecta (raro) | Audit TenantId | Logout/login | Admin |
| API 403 tenant | JWT tenant mismatch | Header vs claim | Usar token correcto | Dev |
| Paginación muestra 50 | Diseño SearchPagedAsync | Normal — ver cards resumen | Usar filtros búsqueda | — |
| Trust Studio vacío | Sin decisiones HITL pendientes | Normal si umbral bajo | Revisar Settings confidence | Manager |
| Churn no visible | Enum sin transición auto | Analytics / ML API | Usar Customer Success | CS lead |
| Workflow no dispara | Workflow inactivo / trigger wrong | `/Workflows/Edit` triggers | Corregir EventType | Admin |
| Communicate no envía | Acción solo log | Código WorkflowEngine | Usar CommunicationAgent vía eventos | Dev |
| Health 503 | Postgres/redis down | `curl /health` | Reiniciar docker compose | DevOps |
| Migraciones fallan | BD desincronizada | Logs API startup | `Database__AutoMigrate` o restore backup | DBA |

**Evidencia logs VPS:** `docker logs autonomuscrm-api`, `docker logs autonomuscrm-workers`
