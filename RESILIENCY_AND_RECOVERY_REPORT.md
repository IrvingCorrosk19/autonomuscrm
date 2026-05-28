# RESILIENCY_AND_RECOVERY_REPORT

## PostgreSQL

- `EnableRetryOnFailure` (5 reintentos, 15s max delay)
- Command timeout 30s
- Migraciones en startup (`InitializeDatabaseAsync`)

## RabbitMQ — `ResilientRabbitMQEventBus`

| Capacidad | Detalle |
|-----------|---------|
| Reconexión | `AutomaticRecoveryEnabled` |
| Durabilidad | Exchange/queue durable, `Persistent` messages |
| DLX | `{exchange}.dlx` + cola `.dlq` por routing key |
| Reintentos | Hasta 3 → luego poison persist |
| Idempotencia | Cache key `evt:processed:{messageId}` (7 días) |
| Poison store | Tabla `FailedEventMessages` |

## Workers

- Procesamiento **scoped** por mensaje (DbContext + tenant correcto)
- Heartbeat log cada 30s

## Recuperación manual poison messages

1. Consultar `FailedEventMessages`
2. Corregir causa
3. Re-publicar payload o replay tool (roadmap)

## Validación runtime

Docker/RabbitMQ soak test pendiente en entorno local (Docker no disponible en sesión Fase 3).
