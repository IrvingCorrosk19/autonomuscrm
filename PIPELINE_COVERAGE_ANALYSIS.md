# PIPELINE_COVERAGE_ANALYSIS

## Fórmula
**Coverage % = Pipeline Abierto Ponderado / Meta Comercial × 100**

Ponderado = Σ (amount × probability / 100) por deals Open.

## Umbral operativo
- **≥ 300%** → cobertura suficiente (regla 3× pipeline típica B2B)
- &lt; 300% → riesgo de no cumplir cuota

## Salida
`PipelineCoverageDto` por rep + fila **Equipo** agregada.

## API
`GET /api/revenue/pipeline-coverage?tenantId=`

## Decisión
Gerente responde: *¿Hay suficiente pipeline para la meta del trimestre?*
