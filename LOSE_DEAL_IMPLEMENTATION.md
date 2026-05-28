# LOSE_DEAL_IMPLEMENTATION

## Dominio
`Deal.Lose(reason)` → `DealLostEvent`, `Metadata["LossReason"]`, stage `ClosedLost`

## Application
- `LoseDealCommand` / `LoseDealCommandHandler` + dispatch eventos

## API
- `POST api/deals/{id}/lose` body `{ dealId, tenantId, reason }`

## UI
- `Deals/Details` — modal motivo pérdida → `OnPostLoseDeal`

## Reporting
- **Win rate** en Index/Deals: `won / (won + lost)` desde etapas `ClosedWon` / `ClosedLost`
