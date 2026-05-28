# LOAD_AND_STRESS_TESTING

## Script

`tests/load/run-load-phase4.ps1`

| Escenario | Parámetros default |
|-----------|-------------------|
| Login concurrente | 20 workers |
| GET /api/leads | 20 × 10 requests |

## Ejecución

```powershell
# API en http://localhost:5154
powershell -File tests/load/run-load-phase4.ps1
```

Evidencia: `tests/qa-evidence/{fecha}/load/load-phase4-*.csv`

## Estado sesión Fase 4

Regresión funcional P0/Phase3 ejecutada post global filters.  
**Load formal 100+ usuarios:** ejecutar script en entorno dedicado (no ejecutado en CI esta sesión).

## Métricas a capturar (producción)

- p50/p95/p99 latencia API
- Error rate 5xx/4xx
- Pool conexiones Npgsql
- RabbitMQ queue depth
- CPU/memoria API y Workers

## Bottlenecks conocidos

- Event storm sin rate limit en bus (roadmap)
- Imports CSV single-threaded (roadmap paralelización)
