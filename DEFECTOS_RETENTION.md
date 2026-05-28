# DEFECTOS RETENTION — Fase 13

## P1 (post-lanzamiento)
| ID | Defecto | Mitigación |
|----|---------|------------|
| R-P1-01 | Email/WhatsApp usan provider log, no SMTP real | Registrar `IEmailDeliveryProvider` producción en DI |
| R-P1-02 | Tickets soporte = proxy tareas, sin módulo tickets | Integrar helpdesk o ampliar TaskType Support_* |
| R-P1-03 | Renewal rate KPI es proxy contractual | Campo `RenewedAt` en contrato en fase 14 |

## P2
| ID | Defecto | Mitigación |
|----|---------|------------|
| R-P2-01 | ProductLine cross-sell requiere metadata manual | Import o DQ post-venta |
| R-P2-02 | Duplicación onboarding Deal vs Customer playbooks | Consolidar en un solo disparador |
| R-P2-03 | NPS/CSAT no modelado | TimeSeries metrics fase 14 |

## Resueltos en Fase 13
- CommunicationAgent stub → engines reales + logs BD
- Sin contrato renovación → `CustomerContracts`
- Sin API ejecutiva customer → `/api/customer/dashboard`
- LTV solo manual → actualización en ClosedWon
