# OPEN_TELEMETRY_OBSERVABILITY

## Configuración

- **Extensión:** `Infrastructure/Platform/PlatformExtensions.AddPlatformOpenTelemetry`
- **Servicios:** `AutonomusCRM.API`, `AutonomusCRM.Workers`
- **App settings:** `OpenTelemetry:OtlpEndpoint`, `OpenTelemetry:EnableConsoleExporter`

## Instrumentación

| Señal | Fuente |
|-------|--------|
| Traces HTTP | AspNetCore + HttpClient |
| Traces DB | EntityFrameworkCore (SQL text) |
| Traces EventBus | `ActivitySource("AutonomusCRM.EventBus")` |
| Traces Workers | `ActivitySource("AutonomusCRM.Workers")` |
| Metrics | Runtime + ASP.NET |

## Correlation

- Header: `X-Correlation-Id` (`CorrelationIdMiddleware`)
- Propagado a `ICurrentTenantAccessor.CorrelationId`
- RabbitMQ: `CorrelationId` en message properties

## Logs

- Serilog: consola + `logs/autonomuscrm-*.txt`
- Enriquecimiento LogContext (extensible con tenant en scope)

## Evidencia ejecución

Consola dev muestra activities `Microsoft.AspNetCore`, `OpenTelemetry.Instrumentation.EntityFrameworkCore` con `service.name: AutonomusCRM.API`.

## Próximo paso producción

Export OTLP a Grafana Tempo / Jaeger + dashboards Prometheus.
