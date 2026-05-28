# DISASTER_RECOVERY_VALIDATION

## Escenarios

| Escenario | Mitigación código | Prueba ejecutada |
|-----------|-------------------|------------------|
| PostgreSQL transient | Npgsql retry | Automático en runtime |
| RabbitMQ caída | Auto-recovery + reconnect | Pendiente Docker |
| Worker crash | K8s/Docker restart | Pendiente |
| Poison message | FailedEventMessages | Esquema migrado |
| DB corrupta | Restore backup | Manual ops |

## RPO/RTO (objetivos piloto)

| Métrica | Objetivo piloto |
|---------|----------------|
| RPO | 24h (backup diario) |
| RTO | 4h |

## Event replay

Roadmap: herramienta admin re-publish desde `FailedEventMessages` y event store.
