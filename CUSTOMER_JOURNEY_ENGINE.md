# CUSTOMER JOURNEY ENGINE

## Servicio
`ICustomerJourneyEngine` → `CustomerJourneyEngine`

## Etapas medidas
```
Lead → Deal → Customer → Onboarding → Active → Renewal → Expansion
```

## Métricas por etapa
- **Count** — volumen en etapa
- **AvgDurationDays** — ciclo deal (donde aplica)
- **ConversionPercent** — lead→deal, deal→customer
- **AvgHealthScore** — salud promedio en etapas post-venta

## Metadata journey
`JourneyStage`, `OnboardingStarted`, `RenewalInProgress`, `ExpansionOpportunity` en `Customer.Metadata`.

## API
`GET /api/customer/journey?tenantId=`
