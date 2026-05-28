# REVENUE_AUTOMATIONS

## Event-driven (`IRevenueAutomationEngine`)
- `Lead.Created` → SLA 24h
- `Lead.ScoreUpdated` → assign top rep (score≥70)
- `Lead.Qualified` → SLA follow-up 48h

## Periodic (Worker cada 15 min)
- Deals estancados ≥14 días → `StagnantEscalation`
- Leads sin actividad ≥48h → `Inactivity48h`
- Data quality scan → tareas DQ_*

## Playbooks heredados Fase 11
- At-risk → tarea Urgent
- Won → onboarding CS

## Trigger manual
`POST /api/revenue/scan?tenantId=`
