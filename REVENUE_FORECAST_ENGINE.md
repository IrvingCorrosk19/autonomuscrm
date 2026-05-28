# REVENUE_FORECAST_ENGINE

## Horizontes
30, 60, 90, 180 días.

## Algoritmo
1. Deals **Open** con `ExpectedCloseDate` ≤ horizonte (o sin fecha → incluidos)
2. **Weighted** = Σ amount × probability/100
3. **HistoricalWinRate** de deals cerrados (won/(won+lost))
4. **ConfidenceFactor** = clamp(0.5 + winRate×0.5, 0.35–0.95)
5. **Forecast** = weighted × confidence

## DTO
`RevenueForecastDto`: HorizonDays, WeightedForecast, UnweightedPipeline, HistoricalWinRate, ConfidenceFactor

## API
`GET /api/revenue/forecast?tenantId=`

## vs Fase 11
Elimina aproximación lineal simple; incorpora win rate histórico y confianza.
