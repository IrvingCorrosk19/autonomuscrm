# CUSTOMER SEGMENTATION ENGINE

`ICustomerSegmentationEngine` → `CustomerSegmentationEngine`

## Segmentos automáticos
| Segmento | Reglas |
|----------|--------|
| VIP | Status VIP o LTV ≥ 50k |
| Growth | Health ≥ 70, no VIP |
| Stable | Default saludable |
| AtRisk | Critical o churn prob ≥ 70% |
| Churned | Status Churned |

Persiste `Metadata.Segment` y actualiza status VIP en scan.

## API
`GET /api/intelligence/segmentation?tenantId=`
