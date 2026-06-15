# Data Hub — UI/UX Specification (Enterprise Supreme)

## Design principles

- **Zero ETL vocabulary** in user-facing copy (use: "clean", "check", "import", "fix")
- **Wizard-first**: primary entry `/DataHub/Wizard`
- **Confidence everywhere**: column mapping shows % bar (green gradient)
- **HubSpot/Salesforce feel**: cards, stat grids, progress bars, step indicator

## Main menu (10 submodules)

| Module | Route | User language |
|--------|-------|---------------|
| Import Center | `/DataHub/Wizard` | "Guided import" |
| Mapping Studio | `/DataHub/Mapping` | "Match your columns" |
| Rules Engine | `/DataHub/Rules` | "Automatic cleaning rules" |
| Validation Center | `/DataHub/Validation` | "Check your data" |
| Data Quality Center | `/DataHub/Quality` | "Score 0–100" |
| Jobs Monitor | `/DataHub/Jobs` | "Live progress" |
| Import History | `/DataHub/History` | "Past imports" |
| Error Center | `/DataHub/Errors` | "Fix failed rows" |
| Rollback Center | `/DataHub/Rollback` | "Undo import" |
| Templates Center | `/DataHub/Templates` | "Save setup" |

## Smart Analysis panel

Shows:
- Suggested module (Lead, Customer, Deal)
- Overall confidence %
- Detected content types (Leads, Companies, Contacts)
- Issues list (warnings, not errors)

## Data cleaning dashboard

Stat cards: Total | Valid | Warnings | Errors | Duplicates

Button: **Fix automatically** — trim, email, phone, title case

## Jobs Monitor

Progress %, rows/min, ETA, status badge, cancel/retry/download errors

## CSS

`flow-datahub.css` — wizard steps, confidence bars, stat grid, AI panel, dropzone

## Roles

All Data Hub pages: `RequireManager` (Admin, Manager)
