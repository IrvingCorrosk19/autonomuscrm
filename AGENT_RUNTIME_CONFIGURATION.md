# AGENT_RUNTIME_CONFIGURATION

## Servicio
`IAgentConfigurationService` / `AgentConfigurationService`  
Lee `AgentConfig_{AgentName}` desde `Tenant.Settings` (misma persistencia que página Agents).

## Agentes conectados
| Agente | Config aplicada |
|--------|-----------------|
| LeadIntelligenceAgent | IsEnabled, SourceWeights, ContactWeights |
| CustomerRiskAgent | IsEnabled, BaseRiskScore, RiskAdjustments |
| DealStrategyAgent | IsEnabled, umbrales riesgo/días |

## Comportamiento
- `IsEnabled = false` → agente no procesa evento
- Pesos/umbrales sustituyen valores hardcoded anteriores

## Validación
1. Agents → desactivar Lead Intelligence
2. Crear lead → score no cambia automáticamente
