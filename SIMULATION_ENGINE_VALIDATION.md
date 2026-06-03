# Simulation Engine Validation

## Before (FALSO)

`BusinessSimulationEngine.cs` returned fixed impacts: -5000, 10000, 15000, 25000, etc.

## After (REAL)

**`RevenueSimulationCalculator.LoadBaselineAsync`** reads from PostgreSQL:
- MRR from closed-won deals (12-month rolling)
- Open pipeline (probability-weighted)
- Win rate (won / (won + lost))
- Churn rate (negative outcomes / customers)
- Lead velocity (leads / 90 days)
- Avg deal size

**`ProjectScenarioImpact(scenario, baseline)`** computes:
- `customer_loss` → -MRR × churn clamp
- `renewal` → MRR × win rate
- `expansion` → 15% MRR × win rate factor
- `deal_won` → avgDeal × win rate
- `deal_lost` → negative avg deal × (1 - win rate)
- `churn_increase` → -MRR × 3× churn
- `campaign_executed` → leadVelocity × avgDeal × win rate × 0.1

## Tests

`RevenueSimulationCalculatorTests.cs` — verifies no hardcoded constants remain  
`RevenueIntelligenceTruthTests.cs` — directional impact tests

## Evidence

```
dotnet test --filter "FullyQualifiedName~RevenueSimulation"
→ PASS
```
