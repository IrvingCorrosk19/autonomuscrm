# SALES_PRODUCTIVITY_ANALYSIS

## Métricas por vendedor
| Métrica | Cálculo |
|---------|---------|
| TasksCompleted | status=Completed |
| TasksOverdue | Open + DueDate pasado |
| AvgLeadResponseHours | QualifiedAt - CreatedAt |
| AvgSalesCycleDays | ClosedWon: ClosedAt - CreatedAt |
| ActivitiesCount | total tareas asignadas |

## API
`GET /api/revenue/productivity?tenantId=`

## Uso
Director identifica reps con muchas tareas vencidas o ciclos de venta largos.
