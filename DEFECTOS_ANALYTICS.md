# DEFECTOS ANALYTICS — Fase 14

## P1
| ID | Defecto | Mitigación |
|----|---------|------------|
| A-P1-01 | Usage depende de API POST / logins — sin SDK frontend | Instrumentar páginas en fase UI futura |
| A-P1-02 | Segmentación N+1 churn predict en loop | Batch predict en optimización |
| A-P1-03 | NPS/CSAT requieren captura manual/API | Integrar encuestas email post-CSAT |

## P2
| ID | Defecto | Mitigación |
|----|---------|------------|
| A-P2-04 | Sin modelo ML — reglas heurísticas | ML pipeline fase 15 |
| A-P2-05 | Industry usage parcial | Metadata tenant industry |

## Resueltos Fase 14
- Sin product analytics → ProductUsageEvents + DAU/WAU/MAU
- Sin NPS/CSAT → Feedback engine
- Sin data mart → CustomerAnalyticsSnapshots
- Sin API intelligence → `/api/intelligence/dashboard`
