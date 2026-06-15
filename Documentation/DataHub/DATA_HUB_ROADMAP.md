# Data Hub — Roadmap (Enterprise Supreme)

## ✅ Supreme MVP (current)

Visual wizard, smart analysis, rules engine, auto-fix, quality score, templates, job queue

## Q2 — Scale to 1M rows

- PostgreSQL COPY into staging
- Partitioned batches table
- Worker process in `AutonomusCRM.Workers` consuming job queue via RabbitMQ

## Q3 — Salesforce / HubSpot parity

- Duplicate management UI (merge preview)
- Field matching rules library
- Import scheduling
- OAuth migration wizards reusing Integrations connectors

## Q4 — AI native

- Optional GPT analysis when `AI:Enabled`
- Natural language rule creation ("fix all phone numbers in Panama")
- Anomaly detection on import preview

## Non-goals (stay CRM-aligned)

- Separate Contact/Company entities (use Lead/Customer + Company string until domain evolves)
- Generic ETL scripting language
