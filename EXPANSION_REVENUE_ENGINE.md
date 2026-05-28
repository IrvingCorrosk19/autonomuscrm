# EXPANSION REVENUE ENGINE

## Servicio
`IExpansionRevenueEngine` → `ExpansionRevenueEngine`

## Detección
| Tipo | Criterio |
|------|----------|
| Upsell | Health Healthy + VIP o won ≥ 50k |
| CrossSell | Metadata `ProductLine` multi-producto |
| Expansion | Adoption ≥ 70 y Engagement ≥ 70 |

## Acciones
- Tarea `Expansion_Opportunity` con recomendación y monto sugerido
- Agente `ExpansionAgent` en scan 15 min

## API
`GET /api/customer/expansion?tenantId=`

## Relación Revenue
Oportunidades de expansión alimentan pipeline futuro; LTV actualizado en `Deal.ClosedWon`.
