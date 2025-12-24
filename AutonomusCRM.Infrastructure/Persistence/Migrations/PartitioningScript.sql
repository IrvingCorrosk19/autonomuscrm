-- Script de particionado para PostgreSQL
-- Este script debe ejecutarse manualmente en la base de datos

-- Particionado de DomainEvents por TenantId y tiempo
CREATE TABLE IF NOT EXISTS "DomainEvents" (
    "Id" UUID NOT NULL PRIMARY KEY,
    "EventType" VARCHAR(200) NOT NULL,
    "TenantId" UUID,
    "CorrelationId" UUID,
    "AggregateId" UUID,
    "OccurredOn" TIMESTAMP NOT NULL,
    "EventData" JSONB NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL
) PARTITION BY RANGE ("OccurredOn");

-- Crear particiones mensuales (ejemplo para 2024-2025)
CREATE TABLE "DomainEvents_2024_01" PARTITION OF "DomainEvents"
    FOR VALUES FROM ('2024-01-01') TO ('2024-02-01');

CREATE TABLE "DomainEvents_2024_02" PARTITION OF "DomainEvents"
    FOR VALUES FROM ('2024-02-01') TO ('2024-03-01');

CREATE TABLE "DomainEvents_2024_03" PARTITION OF "DomainEvents"
    FOR VALUES FROM ('2024-03-01') TO ('2024-04-01');

-- Continuar para otros meses según necesidad

-- Índices en cada partición
CREATE INDEX "IX_DomainEvents_TenantId_2024_01" ON "DomainEvents_2024_01" ("TenantId");
CREATE INDEX "IX_DomainEvents_EventType_2024_01" ON "DomainEvents_2024_01" ("EventType");
CREATE INDEX "IX_DomainEvents_AggregateId_2024_01" ON "DomainEvents_2024_01" ("AggregateId");

-- Particionado de TimeSeriesMetrics por TenantId y tiempo
CREATE TABLE IF NOT EXISTS "TimeSeriesMetrics" (
    "Id" UUID NOT NULL PRIMARY KEY,
    "TenantId" UUID NOT NULL,
    "MetricName" VARCHAR(200) NOT NULL,
    "Value" DOUBLE PRECISION NOT NULL,
    "Tags" JSONB,
    "Timestamp" TIMESTAMP NOT NULL
) PARTITION BY RANGE ("Timestamp");

-- Crear particiones mensuales para TimeSeriesMetrics
CREATE TABLE "TimeSeriesMetrics_2024_01" PARTITION OF "TimeSeriesMetrics"
    FOR VALUES FROM ('2024-01-01') TO ('2024-02-01');

-- Índices
CREATE INDEX "IX_TimeSeriesMetrics_TenantId_MetricName_Timestamp_2024_01" 
    ON "TimeSeriesMetrics_2024_01" ("TenantId", "MetricName", "Timestamp");

-- Función para crear particiones automáticamente (ejecutar mensualmente)
CREATE OR REPLACE FUNCTION create_monthly_partition(table_name TEXT, start_date DATE)
RETURNS VOID AS $$
DECLARE
    partition_name TEXT;
    end_date DATE;
BEGIN
    end_date := start_date + INTERVAL '1 month';
    partition_name := table_name || '_' || TO_CHAR(start_date, 'YYYY_MM');
    
    EXECUTE format('CREATE TABLE IF NOT EXISTS %I PARTITION OF %I FOR VALUES FROM (%L) TO (%L)',
        partition_name, table_name, start_date, end_date);
END;
$$ LANGUAGE plpgsql;

