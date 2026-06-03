# CEO FIRST 30 SECONDS — AutonomusFlow

**Versión:** 1.0 · **Documento más importante del programa de diseño**  
**Metodología:** Basado en auditoría UI/UX real + capacidades backend v0.9 documentadas — sin inventar features no existentes en código.

---

## 1. Escenario del test

| Variable | Valor |
|----------|-------|
| Persona | CEO / fundador / CRO de empresa B2B $10M–$100M ARR |
| Contexto | Demo post-login; tenant con datos seed o piloto real |
| Dispositivo | MacBook 14" (primario); iPhone ignorado primeros 30s |
| Conocimiento previo | «Me dijeron que es un CRM con IA autónoma» |
| Competencia mental | Salesforce, HubSpot, o Attio reciente |

---

## 2. Qué ve HOY (realidad auditada) — segundo a segundo

### 0–3 segundos: Login o primera pintura

| Ve | Siente | Entiende |
|----|--------|----------|
| «**Autonomus CRM**» (no Flow) | «Otro CRM» | Es CRM, no plataforma nueva |
| AdminLTE login; gradiente azul gris | Familiar / genérico | Nada premium |
| Tabla **passwords demo** (si seed) | «Esto es juguete» | No es producción |
| Campo **Tenant ID** | «Sistema interno» | Fricción enterprise |

**Percepción precio en T+3s:** $49/mes tooling.

---

### 3–10 segundos: Layout carga

| Ve | Siente | Entiende |
|----|--------|----------|
| Sidebar oscuro AdminLTE | «Lo he visto antes» | Panel admin |
| Iconos Font Awesome | 2018 vibes | — |
| Grupos PRINCIPAL / AUTONOMÍA | Mezcla idiomas | Hay algo de IA en menú |
| Banner amarillo comms (si sim) | Honestidad o alarma | Comms no live |
| Footer «Event-Driven · Multi-tenant» | **Ingeniería, no negocio** | Para devops |
| Navbar badge `Production` | Ops, no venta | — |

**Percepción T+10s:** $99–$199/mes; «equipo construyó sobre plantilla».

---

### 10–20 segundos: Dashboard (`/` Index)

| Ve | Siente | Entiende |
|----|--------|----------|
| 4 cajas coloridas small-box (azul/verde/amarillo) | HubSpot 2016 | KPIs ventas |
| Barra «Operación diaria» + 6 botones | Ruido | Muchas cosas |
| Alert azul texto largo reporting | Muro texto | Hay pipeline |
| Tabla pipeline | CRM serio | Gestión deals |
| **No ve IA protagonista** | «IA es un menú más» | Autonomía no es centro |

**Percepción T+20s:** $199/mes CRM competente; **no** $5k ABOS.

---

### 20–30 segundos: Si abre AI Command Center o Agents

| Ruta | Ve | Siente |
|------|-----|--------|
| **Command Center** | 6 small-box apretados; listas | «Panel extra» |
| **Agents** | **1,247 decisiones**, 7/7, 0.84 | **«Mienten»** — destruye confianza |

**Percepción T+30s:** Si ve Agents → **baja a $49** (demo fake). Si solo Index → se queda en $199.

---

## 3. Qué debe ver en la VISIÓN (post-rebuild Fase 1–2)

### 0–3 segundos

| Ve | Siente | Entiende |
|----|--------|----------|
| Login split: marca **AutonomusFlow** + «Autonomous Business OS» | Curiosidad | Categoría nueva |
| Botones **Continuar con SSO** | Enterprise | Seguro |
| Sin passwords en pantalla | Profesional | Producción |

---

### 3–10 segundos

| Ve | Siente | Entiende |
|----|--------|----------|
| Sidebar slate minimal; iconos limpios | Moderno, calmado | Premium |
| Landing **Flow Command** (no «Dashboard») | «Esto es diferente» | No empecé en CRM |
| Topbar: punto verde «Sistema autónomo activo» | **La máquina trabaja** | IA operativa |
| Badge «12» en Trust | Urgencia controlada | Debo supervisar |

---

### 10–20 segundos

| Ve | Siente | Entiende |
|----|--------|----------|
| Hero: **«$284,000 protegidos este mes por IA»** (dato real API) | **Impacto $** | ROI inmediato |
| Sub: «12 decisiones requieren tu aprobación» | Control | HITL |
| CTA primario dorado/indigo: **Revisar decisiones** | Claridad | Una acción |
| Tres columnas: Riesgo / Expansión / Renovación con **nombres cuenta** | Inteligencia | Sabe dónde mirar |
| Derecha: **6 agentes** con barras actividad | «Tengo workforce digital» | ABOS |

---

### 20–30 segundos

| Ve | Siente | Entiende |
|----|--------|----------|
| Feed: «DECISIÓN · Renovación Acme · Confianza 0.82 · +$45k» | Transparencia | Puede confiar |
| Mini pipeline con deals reales | CRM debajo | Completo |
| Sin arcoíris KPI | Madurez | Stripe/Linear tier |

**Percepción T+30s visión:** **$2,500–$5,000+/mes** plausible.

---

## 4. Por qué pagaría $5,000+ mensuales (narrativa CEO)

### 4.1 No paga por «otro CRM»

Paga porque ve **cuatro pruebas** en 30 segundos:

| # | Prueba | Evidencia visual | Backend real (v0.9) |
|---|--------|------------------|---------------------|
| 1 | **Dinero** | Hero revenue impact IA | `RevenueGeneratedByAi7d`, Outcome Fabric |
| 2 | **Control** | Trust badge + CTA | `AiApprovalRequests`, HITL |
| 3 | **Trabajo autónomo** | Agentes activos con métricas | 6 agents, Worker cycle, audits |
| 4 | **Enterprise** | SSO, sin demo, Billing visible (fase 4) | SCIM, SAML prep, Stripe service |

### 4.2 ROI mental del CEO (aritmética simple)

```
Si la plataforma protege/genera $284k/mes
y cuesta $5k/mes
→ 57× ROI
→ «barato»
```

La UI actual **no muestra** esta ecuación. Muestra «Leads nuevos 24h».

### 4.3 Comparación precio competencia mental

| Producto | Precio mental | Por qué |
|----------|---------------|---------|
| HubSpot Pro | ~$800/mes | Marketing + CRM |
| Salesforce Enterprise | $5k+/mes | Ecosistema |
| Attio | ~$500/mes | CRM moderno |
| **AutonomusFlow ABOS** | **$5k/mes** | **CRM + 6 agentes + HITL + outcome $** — sustituye headcount |

**Pitch en 1 frase:** «Un analista de revenue + un CSM + un SDR coordinator por $5k/mes, supervisados por usted en Trust.»

La UI debe **mostrar** esa sustitución (Workforce panel), no decirla.

---

## 5. Qué lo impresiona (visión)

| Elemento | Por qué impresiona |
|----------|-------------------|
| Hero $ con número grande | CEOs piensan en $, no en leads |
| Trust con SLA críticos | Governance sin burocracia |
| Agentes con workload | «Future of work» tangible |
| Feed decisiones con confianza | Transparencia IA (vs caja negra) |
| Estética calmada Inter/indigo | Mismo lenguaje que fintech moderno |
| Cero datos fake | Integridad — raro en demos |
| ⌘K | «Equipo A-player construyó esto» |

---

## 6. Qué lo convence (vs solo impresiona)

| Elemento | Convicción |
|----------|------------|
| Click Trust → ver explicación + impacto $ + aprobar en 2 clicks | «Puedo operar esto» |
| Customer 360 timeline eventos reales | «Conoce mi negocio» |
| Integrations logos + «Conectado» sin pegar tokens | «Encaja en stack» |
| Billing: plan Enterprise + uso | «Es SaaS real» |
| Audit export | «Compliance» |

**Impression = Command home. Conviction = Trust + Integrations + Billing.**

---

## 7. Qué lo haría RECHAZAR (evitar en rebuild)

| Elemento | Efecto |
|----------|--------|
| KPIs inventados | Muerte instantánea |
| Tenant ID login | «Interno» |
| Tokens OAuth visibles | «Inseguro» |
| MVP en copy | «Inacabado» |
| AdminLTE small-box | «Barato» |
| IA es página 8 del menú | «Feature bolt-on» |
| Sin Billing | «No es SaaS maduro» |

---

## 8. Storyboard 30 segundos (para video demo)

| Seg | Frame |
|-----|-------|
| 0–5 | Login SSO → flash marca Flow |
| 5–12 | Command hero $ + pending 12 |
| 12–18 | Scroll agentes + feed decisión |
| 18–24 | Click Trust → approve 1 decisión |
| 24–30 | Flash Customer 360 timeline → corte logo «ABOS» |

**Música:** ninguna. **Voz en off:** una frase por frame, máximo 40 palabras total.

---

## 9. Checklist CEO test (validación post-fase)

| # | Pregunta post-30s | Pass si |
|---|-------------------|---------|
| 1 | ¿Qué hace este producto? | Menciona autonomía / agentes / $ |
| 2 | ¿Es CRM? | «Más que CRM» o «sistema operativo» |
| 3 | ¿Confía en los números? | Sí / pregunta fuente (aceptable) |
| 4 | ¿Cuánto cree que cuesta? | ≥ $1,000/mes |
| 5 | ¿Quiere ver Trust? | Sí |
| 6 | ¿Qué le preocupa? | (registrar) — no «parece fake» |

**Target pass rate:** 80% en ≥$1k perception; 50% en ≥$5k post fase 2.

---

## 10. Gap actual → visión (tabla honesta)

| Segundo | Hoy (auditado) | Visión |
|---------|----------------|--------|
| 0–3 | CRM login demo | Flow SSO enterprise |
| 3–10 | AdminLTE sidebar | Flow shell |
| 10–20 | CRM KPI boxes | Command $ hero |
| 20–30 | Fake agents o small-box IA | Workforce real + feed |

**Sin gap backend:** Worker, Outcome Fabric, Trust API, Command service **existen** (v0.9). Gap es **100% presentación**.

---

## 11. Frases que el CEO debe poder repetir (test de éxito)

Después de 30 segundos, si puede decir al board:

1. «La plataforma **generó o protegió** $X este mes.»
2. «Tengo **12 decisiones** esperando mi OK.»
3. «**Seis agentes** están trabajando cuentas ahora.»
4. «No es Salesforce — es un **sistema operativo autónomo**.»

→ **Diseño exitoso.**

Hoy probablemente dice:

1. «Es un CRM con dashboard de leads.»
2. «Hay un menú de IA.»
3. (Si vio Agents) «Los números parecen demo.»

---

## 12. Relación precio → pantalla

| Precio/mes | Pantalla mínima requerida |
|------------|---------------------------|
| $49 | Login + 1 tabla CRM |
| $199 | Index con datos reales |
| $499 | + Command + Trust usable |
| $999 | + 360 timeline + Revenue charts |
| **$2,500** | + Workforce real + Integrations logos |
| **$5,000+** | + Billing + SSO + cero mock + estética Flow completa |

**Hoy el producto alcanza ~$199 en UI** (auditoría) con **~$2,500–$5,000 de capacidad backend** (MASTER_CONTEXT ~90).

**El trabajo de diseño es cerrar ese gap de percepción.**

---

## 13. Una imagen vale $5,000/mes

Descripción del frame único para website y deck:

> Pantalla clara. Hero tipográfico: **$284,000** en 48px Inter semibold, subtítulo «impacto autónomo este mes». A la derecha, badge ámbar «12 pendientes». Debajo, tres tarjetas blancas con borde izquierdo rojo/verde/azul — nombres de empresas reales. Panel derecho: seis filas «agentes» con barras de progreso teal. Sin gradientes arcoíris. Sin tabla Bootstrap visible. Logo AutonomusFlow arriba izquierda. Sensación: **Stripe encontró Linear en la sala de control de tu empresa.**

Esa imagen **es** el objetivo de los 30 segundos.

---

*Documento de diseño. Validar con 5 CEOs reales tras Fase 2 implementación.*
