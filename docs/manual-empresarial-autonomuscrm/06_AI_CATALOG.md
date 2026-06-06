# 06 — Catálogo de Inteligencia Artificial

## A. UI orientada al usuario

| Módulo | Ruta | Función |
|--------|------|---------|
| Command Center | `/` | Métricas flujo, decisiones, workforce |
| Trust Studio | `/TrustInbox` | Aprobación humana (HITL) |
| Workforce | `/Agents` | Agentes y decisiones recientes |
| Decision history | `/command/decisions` | Historial filtrable |
| Outcome Fabric | `/command/outcomes` | Impacto de outcomes |
| Playbooks | `/command/playbooks` | Estados autónomos |
| Revenue OS | `/revenue` | Fugas ingreso + grafo explicativo |
| Memory | `/Memory` | Memoria semántica empresarial |

---

## B. ML / Enterprise AI (API + background)

| Servicio | Endpoint / trigger | Salida |
|----------|-------------------|--------|
| ChurnPredictionModel | `GET api/ai/ml/churn` | Lista predicciones churn |
| ExpansionPredictionModel | `GET api/ai/ml/expansion` | Oportunidades expansión |
| RevenuePredictionModel | `GET api/ai/ml/revenue` | Forecast ML |
| NextBestActionMl | NBA scoring | Score acciones |
| EnterpriseAiCycle | `POST api/ai/enterprise-cycle` | Train + drift + graph |
| ExecutiveAiAnalytics | `GET api/ai/analytics` | Analytics ejecutivo |
| AiGovernance | `GET api/ai/governance` | Reporte gobernanza |

---

## C. LLM (`AutonomusCRM.AI`)

| Componente | Uso real en producción |
|------------|------------------------|
| OpenAI / Azure / Anthropic / Gemini providers | Config `AI` section |
| LlmAgentService | Tests; no usado por Workers |
| LlmSmokeService | Health API `api/ai/llm/*` |
| Embeddings | API/Infrastructure adapter; Workers default unconfigured |

---

## D. Decisiones autónomas (reglas + ML)

`AutonomousRevenueDecisionEngine` combina:
- Health engine, Churn V2, expansion, NPS/CSAT
- Semantic memory context
- Ejecuta playbooks + comunicaciones + audit

**Gate:** `AutonomousPlatformGate` + kill-switch en Settings

---

## E. Casos de uso Sales

1. Ver score automático en lista Leads
2. Priorizar leads score ≥70
3. Revisar Revenue OS para deals en riesgo
4. Completar tareas generadas por IA operativa (no conversacional)
5. **No** depender de chat LLM integrado en workers — no está cableado

---

## F. Limitaciones y errores frecuentes

| Expectativa incorrecta | Realidad |
|------------------------|----------|
| "La IA cierra ventas sola" | Crea tareas y decisiones; humano ejecuta |
| "ChatGPT en cada email" | CommunicationAgent usa templates configurados |
| "Trust vacío = IA rota" | Puede significar sin decisiones pendientes HITL |
| "Score 0" | Lead sin procesar por agente aún |
