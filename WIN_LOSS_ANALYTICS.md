# WIN_LOSS_ANALYTICS

## Extensión LoseDeal
`Deal.Lose(reason, lossCategory)` persiste:
- `LossReason`, `LossCategory`, `StageAtLoss`

## Agrupaciones (`groupBy`)
| Valor | Dimensión |
|-------|-----------|
| reason | Motivo / categoría |
| rep | Vendedor |
| stage | Etapa al perder |
| industry | Empresa cliente (proxy) |
| amount | Buckets &lt;5K, 5-25K, 25-100K, 100K+ |

## API
`GET /api/revenue/win-loss?tenantId=&groupBy=reason`

## Objetivo
Mejorar cierre atacando motivos recurrentes de pérdida.
