# CUSTOMER SUCCESS INTELLIGENCE

## Servicio central
`ICustomerSuccessIntelligenceService` — orquesta motores y playbooks; **no decorativo**.

## Agentes (Workers)
| Agente | Entrada | Salida accionable |
|--------|---------|-------------------|
| CustomerHealthAgent | Customer.Created | Playbook Adoption/Rescue + tareas |
| ChurnRiskAgent | RiskScoreUpdated (≥60) | Alertas churn + Rescue |
| RenewalAgent | Scan 15 min | Tareas Renewal_90/60/30 |
| ExpansionAgent | Scan 15 min | Tareas Expansion_Opportunity |

## API manual
`POST /api/customer/intelligence/{health|churn|renewal|expansion}?tenantId=&customerId=`

Retorna `CustomerIntelligenceActionDto[]` con `TaskCreated=true/false`.
