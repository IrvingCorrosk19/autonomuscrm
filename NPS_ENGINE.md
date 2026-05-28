# NPS ENGINE

`INpsEngine` → `NpsEngine`

## Clasificación (0–10)
- **Promoter** ≥ 9
- **Passive** 7–8
- **Detractor** ≤ 6

## Métricas
- NPS Global = (Promoters − Detractors) / Total × 100
- NPS por cliente y por segmento

## API
- `GET /api/intelligence/nps`
- `POST /api/intelligence/nps?customerId=&score=`

Almacenado en `CustomerFeedbacks` tipo `NPS`.
