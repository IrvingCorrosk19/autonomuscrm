# CUSTOMER_JOURNEY_ANALYSIS

## Mapa del journey

```
Primer contacto ──► Lead ──► [Calificación] ──► [Oportunidad/Deal] ──► Won
                                                      │
                                                      ▼
                                                  Customer
                                                      │
                    ┌─────────────────────────────────┼─────────────────────┐
                    ▼                                 ▼                     ▼
                  Uso                            Renovación              Referidos
              (no medido)                      (no existe)            (no existe)
                    │
                    ▼
              Retención / Churn (manual)
```

---

## Etapas — madurez operacional

| Etapa | Sistema | Automatización | Fuga |
|-------|---------|----------------|------|
| Primer contacto | Lead.Create | Score automático | Sin respuesta al lead |
| Calificación | Qualify / status | Workflow opcional | Sin SLA |
| Oportunidad | Deal manual | Strategy hints ocultos | Lead≠Deal desconectado |
| Propuesta/negociación | Deal stages | Prob por etapa | Sin doc tracking |
| Cierre | Close | Evento sin CS | Comunicación externa |
| Cliente activo | Customer | Risk inicial | Sin health |
| Uso/adopción | — | — | **Abandono invisible** |
| Renovación | — | — | **100% manual** |
| Upsell | UI mock | — | Sin oportunidad hijo |
| Referidos | — | — | No modelado |

---

## Cuellos de botella

1. **Lead → Deal:** salto manual; mayor pérdida en SMB.
2. **Post-close vacuum:** 30–90 días sin touchpoints sistemáticos.
3. **Datos duplicados:** Lead convertido + Deal separado sin vínculo fuerte.
4. **Bulk ops:** rompen cadena de eventos.

---

## Abandonos típicos simulados

| Persona | Abandono | Causa en producto |
|---------|----------|-------------------|
| Vendedor | Deja leads en New | Sin tarea día 1 |
| Gerente | No confía en forecast 90d | Panel mock |
| CS | No usa CRM post-venta | Sin cola ni health |
| CEO | Mira Excel | Sin reporte único verdad |
| Cliente | No recibe follow-up | Communication stub |

---

## Oportunidades de conexión (mínima intervención humana)

| Conexión | Evento disparador | Acción objetivo |
|----------|-------------------|-----------------|
| Lead → Deal | `Lead.Qualified` | CreateDeal draft + tarea |
| Won → CS | `Deal.Closed` (Won) | Playbook 3 tareas |
| Inactividad | Job diario | Risk↑ + alerta CS |
| LTV | `PurchaseRecorded` | UpdateLifetimeValue |
| Renovación | T-90 contrato | Deal renovación + email |

---

## Métricas journey (faltantes)

- Time-to-first-contact
- Lead-to-deal conversion rate
- Deal cycle length
- Net revenue retention (NRR)
- Logo churn rate
- Expansion revenue %

Solo **conversion lead calificado/total** está parcialmente en Index.
