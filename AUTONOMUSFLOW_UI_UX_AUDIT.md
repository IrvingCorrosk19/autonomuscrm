# AUTONOMUSFLOW — Auditoría UX/UI completa

**Fecha:** 2026-06-02  
**Alcance:** Razor Pages + `site.css` + `site.js` + AdminLTE 3.2 / Bootstrap 4.6  
**Método:** Análisis estático de código (sin cambios aplicados, sin refactor)  
**Marca en UI:** Predominantemente **«Autonomus CRM»** (no «AutonomusFlow») — desalineación de producto.

---

## 1. Clasificación global (las 5 categorías)

| Categoría | ¿Encaja? | Evidencia |
|-----------|----------|-----------|
| 1. Startup amateur | Parcial | Login con tabla de passwords demo; Agentes con KPIs hardcodeados (`1,247`, `7/7`); `Dashboard.cshtml` con datos ficticios. |
| 2. Sistema corporativo tradicional | **Fuerte** | AdminLTE sidebar oscuro, `small-box` KPIs, tablas Bootstrap, formularios `form-control`, badges `badge-warning`. |
| 3. SaaS moderno | Parcial | Tokens CSS (`--crm-card-shadow`), `_PageHeader`, filtros `crm-filter-card`, runtime bar, empty states, densidad de tabla. |
| 4. Producto premium enterprise | Débil | Sin identidad visual propia; sin billing/settings enterprise; integraciones con campos de token en claro. |
| 5. Producto categoría mundial | No | Dos sistemas visuales coexistiendo; sin motion system coherente; sin design system documentado en UI. |

**Veredicto de categoría:** Entre **corporativo tradicional (AdminLTE)** y **SaaS en modernización parcial** — aspiración a SaaS moderno en CSS, realidad operativa aún CRM clásico 2018–2021.

---

## 2. Scores globales

| Dimensión | Score | Justificación breve |
|-----------|-------|---------------------|
| **UX global** | **58/100** | Flujos CRM funcionales y filtros repetibles; fricción alta en módulos ABOS; inconsistencia de patrones; datos mock en pantallas clave erosionan confianza. |
| **UI global** | **52/100** | AdminLTE + Bootstrap azul genérico; capa CSS enterprise extensa pero no unifica todas las vistas; jerarquía y densidad variables. |

---

## 3. Score por módulo (UI / UX)

| Módulo | UI | UX | Qué transmite | Problemas principales | Desconfianza | Antiguo | Moderno |
|--------|----|----|---------------|----------------------|--------------|---------|---------|
| **Login** | 42 | 38 | Demo interno / laboratorio | Tabla de credenciales demo; campo **Tenant ID**; gradiente AdminLTE login | Passwords visibles en prod | AdminLTE login-page | Gradiente custom ligero |
| **Dashboard** (`/`, Index) | 64 | 66 | CRM operativo serio | `small-box` + alerta texto largo; doble barra ops; muchos enlaces | Banner comms ok | AdminLTE KPI boxes | `_PageHeader`, filtros, pipeline card |
| **Dashboard** (`/Dashboard`) | 50 | 35 | Mockup de producto IA | **Datos estáticos** (128 leads, $84,900); no usa layout estándar igual | CEO detecta demo al instante | Topbar custom | Pipeline visual, feed IA (falso) |
| **Leads** | 64 | 68 | Lista comercial usable | Tabla densa; KPIs duplicados (small-box + posible stats); muchos botones header | — | Tabla Bootstrap | `crm-filter-card`, table-desc |
| **Customers** | 58 | 60 | Cartera + analytics lite | Mezcla `_PageHeader` + `stats` gradient + tablas; identidad mixta | — | Tablas | Stats gradient cards |
| **Deals / Pipeline** | 60 | 62 | Pipeline clásico | Vista kanban/table híbrida larga; mucho scroll; IA en tabla secundaria | — | Pipeline por etapas manual | Filtros, métricas reales |
| **Revenue** *(sin vista dedicada)* | 45 | 48 | Métricas dispersas | Solo KPIs en Index/Deals; sin Revenue OS visual | No hay «command center» revenue | — | Números en footer small-box |
| **Customer Success** *(sin vista dedicada)* | 40 | 42 | CS embebido en Agents/Support | Sin módulo CS; churn/NPS no en UI ejecutiva | Promesa ABOS no visible | — | Texto en Agents mock |
| **Trust Inbox** | 48 | 54 | HITL funcional pero crudo | Cards apiladas; badges inline; forms en footer sin diseño de decisión | Parece «cola de aprobación» interna, no producto Trust | card-outline warning | Métricas badges |
| **Customer360** | 45 | 50 | Data cloud MVP | Grid de cards texto; sin timeline, sin gráficos, sin merge UI | «Database» icon + cards planas | card-outline primary | Búsqueda simple |
| **AI Command Center** | 46 | 56 | Consola IA en construcción | 6 `small-box` en fila (col-md-2); listas list-group; sin gráficos ni estado agentes vivo | Revenue IA `N0` sin contexto | AdminLTE small-box | Secciones riesgo/expansión |
| **Integrations** | 40 | 44 | Panel técnico devops | Tokens en inputs planos; OAuth + manual en mismo card | **Campos access token en UI** = red flag enterprise | card-success/secondary | OAuth button |
| **Voice Calls** | 42 | 40 | Formulario admin | Pide **Customer/Lead/Deal GUID**; tabla básica; label «MVP» en copy | MVP explícito en subtítulo | form-row Bootstrap | table-responsive |
| **Billing** *(no existe página)* | 18 | 15 | Producto incompleto | Sin UI Stripe/planes/límites | Imposible evaluar pricing en app | — | — |
| **Settings** | 52 | 45 | Config «concepto» | Topbar con search/chips **no funcionales**; feed estilo notificación para settings | Botones «Guardar» sin claridad de persistencia | feed/item pattern | Pills, grid cards |
| **Administration** (Users, Audit, Policies, Workflows) | 55 | 58 | Admin enterprise-lite | Patrón topbar+stats+tabla repetido; Audit real data ✅ | Audit: botón PDF disabled | Tablas largas | Export, filtros fecha |
| **Agents** | 48 | 46 | «War room» IA fake | **KPIs inventados** (1,247 decisiones, 0.84 confianza) | Destruye credibilidad ABOS | stats + feed | Tutorial blocks en CSS |
| **Support** | 54 | 52 | Ops / status page | Tablas de servicios; útil pero genérico | — | Bootstrap table | aria-labels |

**Promedio módulos evaluados (UI):** ~49 · **(UX):** ~51  

---

## 4. Evaluación por los 10 criterios

| Criterio | Score | Hallazgos |
|----------|-------|-----------|
| 1. Visual Design | 52 | Paleta Bootstrap `#007bff`; Source Sans Pro; sin logo SVG propio; iconografía Font Awesome genérica. |
| 2. UX | 58 | Navegación lateral clara; runtime bar útil; pero flujos ABOS (Trust, Integrations) poco guiados. |
| 3. Arquitectura visual | 45 | **Dos familias:** AdminLTE (`content-header` + `small-box`) vs custom (`topbar` + `stats` + `grid`). |
| 4. Consistencia | 42 | Títulos: `_PageHeader` vs `content-header` vs `topbar.headline`; KPIs: small-box vs `.stat` gradient. |
| 5. Responsive | 55 | Media queries extensas en `site.css`; tablas con `data-label` móvil; 6 KPIs en Command Center colapsan mal. |
| 6. Accesibilidad | 48 | Algunos `aria-label`; **sin `focus-visible` sistemático**; contraste badges; login sin skip link. |
| 7. Navegación | 62 | Sidebar agrupada PRINCIPAL / AUTONOMÍA / ADMIN; falta Revenue, Billing, CS. |
| 8. Densidad información | 50 | Index y Deals sobrecargados; Trust demasiado verboso por card; Command Center KPI strip saturada. |
| 9. Legibilidad | 58 | Tablas 0.875rem ok; párrafos `text-muted` repetitivos; mezcla ES/EN en nav («AI Command Center»). |
| 10. Jerarquía visual | 54 | `crm-title` 1.5rem vs `h1.m-0` AdminLTE; acciones primarias compiten (múltiples `btn-primary`). |

---

## 5. Top 50 problemas visuales / UX

1. Dos sistemas de diseño en paralelo (AdminLTE vs topbar/stats custom).  
2. Marca «Autonomus CRM» vs producto «AutonomusFlow».  
3. Login muestra passwords demo en tabla.  
4. Login exige Tenant ID (UX enterprise rota).  
5. `Dashboard.cshtml` con métricas hardcodeadas no reales.  
6. `Agents.cshtml` KPIs ficticios (1,247 / 7/7 / 2.3s).  
7. AdminLTE 3 + Bootstrap 4 (stack visualmente datado vs 2024–2026).  
8. Color primario Bootstrap default `#007bff`.  
9. Tipografía Source Sans Pro (misma familia que millones de admin panels).  
10. `small-box` KPIs (patrón AdminLTE 2014).  
11. AI Command Center: 6 small-box en una fila (ilegible en laptop).  
12. Trust Inbox sin diseño de «decisión» (compare lado a lado, diff, impacto $).  
13. Integrations: inputs de token visibles (aspecto panel de desarrollador).  
14. Voice: formulario pide GUIDs crudos.  
15. Voice: copy «MVP» visible al usuario.  
16. Customer360: cards solo texto, sin visualización de relaciones.  
17. No existe página Billing / planes / uso.  
18. No existe módulo Revenue / Customer Success en navegación.  
19. Settings: barra de búsqueda decorativa (chips no funcionales).  
20. Búsqueda global en topbar (Agents, Settings) sin implementación visible.  
21. Botón «Menú» `toggleSidebar()` en páginas custom pero sidebar es AdminLTE pushmenu.  
22. Mezcla español/inglés en labels de navegación.  
23. Footer: «Event-Driven · Multi-tenant» (mensaje técnico, no valor negocio).  
24. Navbar muestra nombre de entorno (`Development`/`Production`) — ok ops, mal demo comercial.  
25. Alertas info largas en Dashboard (muro de texto KPI).  
26. Exceso de botones en headers (Importar, Masivas, Nuevo, Filtrar, Limpiar).  
27. Tablas sin estados vacíos consistentes en todas las listas.  
28. Formularios `form-control` estándar sin validación inline visual unificada.  
29. Badges `badge-warning/success` sin sistema semántico de color de marca.  
30. Cards `card-outline card-warning` repetitivas (fatiga visual Trust).  
31. Sin ilustraciones / empty states ilustrados en módulos ABOS.  
32. Sin gráficos (charts) en dashboard ejecutivo real (`Index`).  
33. Pipeline en Index vs Deals — patrones visuales distintos.  
34. Gradient `.stat` cards chocan con `small-box` en misma sesión (Customers).  
35. `content-header` duplicado conceptualmente con `_PageHeader`.  
36. Integraciones: OAuth y manual en mismo card (confusión jerárquica).  
37. AI Command Center: listas `list-group` sin priorización visual fuerte.  
38. Sin dark mode activo (solo preparación en docs previos, no en UI auditada).  
39. Sin skeleton loading en navegación entre páginas (componente existe, uso parcial).  
40. Modales Bootstrap genéricos (import/bulk).  
41. Iconos solo decorativos sin significado de estado unificado.  
42. Densidad: falta modo compacto global excepto tablas (toggle parcial).  
43. Trust: inputs rechazo/rollback inline pequeños (error prone).  
44. Customer360: alerta duplicados sin CTA merge.  
45. Comms banner amarillo/verde — útil pero estilo alert Bootstrap crudo.  
46. Runtime bar extra — buena UX ops, suma ruido visual en mobile.  
47. Login: tipografía `login-logo` AdminLTE genérica.  
48. Sin microcopy de confianza (SOC2, SLA, cifrado) en login enterprise.  
49. Políticas/Workflows: mismas plantillas que Users (monotonía).  
50. Falta identidad premium: sin motion brand, sin gradient propio, sin spacing scale documentado en UI.

---

## 6. Quick Wins (1–3 días, sin refactor arquitectónico)

| # | Acción | Impacto |
|---|--------|---------|
| Q1 | Ocultar tabla demo en Login fuera de `Development` | Confianza +40% en demo CEO |
| Q2 | Unificar título de marca a **AutonomusFlow** en layout, login, footer | Identidad |
| Q3 | Eliminar o redirigir `/Dashboard` mock → `/` | Evita «producto falso» |
| Q4 | Reemplazar KPIs hardcodeados en Agents por datos reales o «—» | Credibilidad ABOS |
| Q5 | AI Command Center: 3 KPIs principales + resto en accordion | Legibilidad |
| Q6 | Trust Inbox: un botón primario verde, secundarios outline | Jerarquía |
| Q7 | Integrations: colapsar «manual token» bajo «Avanzado» | Menos miedo seguridad |
| Q8 | Voice: selectors de cliente/deal, no GUID text fields | UX operador |
| Q9 | Añadir ítem nav «Facturación» placeholder o enlace Settings | Percepción SaaS completo |
| Q10 | Unificar headers: usar `_PageHeader` en Trust, Command Center, Integrations | Consistencia |

---

## 7. Medium Wins (2–6 semanas)

| # | Acción | Impacto |
|---|--------|---------|
| M1 | **Congelar AdminLTE** en shell; migrar todas las páginas a `_PageHeader` + `crm-*` | Consistencia → SaaS moderno |
| M2 | Design tokens: primary, success, danger, radius, spacing (1 archivo CSS) | UI +15 pts |
| M3 | Trust Inbox 2.0: layout 3 columnas (contexto | explicación | acciones) | Enterprise HITL |
| M4 | Customer360: timeline + health score visual | Data Cloud creíble |
| M5 | Command Center: gráfico sparkline revenue + tabla agentes | «Consola IA» real |
| M6 | Billing page (plan, uso, límites Stripe) | SaaS monetizable |
| M7 | Revenue + CS hubs mínimos en nav | Completitud producto |
| M8 | Charts en Index (Chart.js o similar) pipeline trend | Dashboard ejecutivo |
| M9 | Eliminar topbar search falso o implementar búsqueda global | UX honesta |
| M10 | Auditoría contraste WCAG AA en badges y small-box | Accesibilidad |

---

## 8. World Class Improvements (3–12 meses)

| # | Mejora | Referente |
|---|--------|-----------|
| W1 | Design system propio (Figma + tokens + componentes Razor partials) | Linear, Stripe |
| W2 | Tipografía distintiva + paleta no-Bootstrap | Attio, Clay |
| W3 | Command Center como **home autónomo** post-login (no Dashboard CRM clásico) | Categoría ABOS nueva |
| W4 | Trust: explainability panel con evidence JSON humanizado + simulación impacto | Salesforce Einstein Trust |
| W5 | Integrations marketplace visual (logos, estado, última sync animada) | HubSpot |
| W6 | Customer360 grafo relacional + merge UI | Attio |
| W7 | Motion system sutil (page transitions, skeleton, optimistic UI) | Linear |
| W8 | Dark mode nativo coherente | Notion, Linear |
| W9 | Mobile-first ops para Trust approve/reject | — |
| W10 | Storytelling vacío: onboarding product-led por rol | HubSpot |
| W11 | Billing self-serve + upgrade path in-app | Stripe Dashboard |
| W12 | Localización i18n + terminología consistente ES/EN | Enterprise global |

---

## 9. Comparación contra referentes

### 9.1 Salesforce

| Aspecto | Salesforce | AutonomusFlow hoy |
|---------|------------|-------------------|
| Densidad | Alta pero sistematizada (Lightning) | Alta, menos sistemática |
| Confianza enterprise | Marca, compliance, consistencia | AdminLTE + demo login |
| IA en UI | Einstein copiloto integrado en registros | Command Center separado, visual débil |
| Pipeline | Kanban Lightning maduro | Tabla + secciones pipeline mixtas |
| **Gap principal** | 15+ años de polish visual | Aspecto «custom CRM sobre plantilla» |

**Score relativo vs SF:** UI 35/100 · UX 40/100 (como sustituto visual enterprise)

---

### 9.2 HubSpot

| Aspecto | HubSpot | AutonomusFlow |
|---------|---------|---------------|
| Onboarding | Guiado, vacíos amigables | Onboarding parcial (`data-crm-onboarding-reset`) |
| Claridad comercial | CRM + Marketing claro | ABOS mezclado con CRM clásico |
| Color / marca | Naranja distintivo | Azul Bootstrap |
| Tablas | Limpias, avatares, deals inline | Tablas funcionales, menos «humanas» |
| **Gap** | Producto «feliz» y vendible | Producto «ingeniería» |

**Score relativo vs HubSpot:** UI 40/100 · UX 45/100

---

### 9.3 Attio

| Aspecto | Attio | AutonomusFlow |
|---------|-------|---------------|
| Estética | Minimal, espacio blanco, tipografía moderna | Admin panel denso |
| Relaciones | Grafos, vistas flexibles | Customer360 cards planas |
| Densidad | Baja-media, premium | Media-alta, tradicional |
| **Gap** | Identidad «CRM nuevo siglo» | Identidad «admin template» |

**Score relativo vs Attio:** UI 30/100 · UX 38/100

---

### 9.4 Clay

| Aspecto | Clay | AutonomusFlow |
|---------|------|---------------|
| Wow factor | Datos enriquecidos, UI brillante | Sin enriquecimiento visual |
| Tablas / listas | Altamente curadas | Bootstrap table |
| **Gap** | Data-forward beauty | Backend-forward UI |

**Score relativo vs Clay:** UI 28/100 · UX 35/100

---

### 9.5 Notion

| Aspecto | Notion | AutonomusFlow |
|---------|--------|---------------|
| Tipografía / espacio | Excelente respiración | Compacto AdminLTE |
| Jerarquía | Bloques claros | Cards + tablas mezcladas |
| **Gap** | Calm UX | Ops-heavy UX |

**Score relativo vs Notion:** UI 32/100 · UX 40/100 (contexto CRM distinto)

---

### 9.6 Stripe

| Aspecto | Stripe | AutonomusFlow |
|---------|--------|---------------|
| Billing UI | Referente mundial | **No existe página** |
| Confianza | Cada pixel transmite pagos seguros | Tokens en pantalla Integrations |
| Densidad | Baja, precisión | Alta |
| **Gap** | Monetización visible y bella | Monetización invisible |

**Score relativo vs Stripe:** UI 25/100 · UX 20/100 (billing)

---

### 9.7 Linear

| Aspecto | Linear | AutonomusFlow |
|---------|--------|---------------|
| Velocidad percibida | Keyboard-first, dark, rápido | jQuery + AdminLTE |
| Consistencia | Sistema único | Sistema dual |
| Command metaphor | Command-K | Sin command palette |
| **Gap** | Producto «equipo elite» | Producto «ops CRM» |

**Score relativo vs Linear:** UI 28/100 · UX 42/100 (para operadores que toleran densidad)

---

### 9.8 Vercel

| Aspecto | Vercel | AutonomusFlow |
|---------|--------|---------------|
| Identidad | Negro/blanco, precisión | Azul/gris AdminLTE |
| Dashboard | Métricas claras, pocos KPIs | Muchos KPIs competindo |
| **Gap** | Dev aesthetic premium | Enterprise template |

**Score relativo vs Vercel:** UI 30/100 · UX 38/100

---

## 10. Detección explícita (checklist solicitado)

| Señal | ¿Presente? | Dónde |
|-------|------------|-------|
| Tablas viejas | Sí | Leads, Deals, Users, Audit, Voice |
| Bootstrap genérico | Sí | Global AdminLTE 3.2 |
| Formularios obsoletos | Sí | Voice, Integrations, Login |
| Exceso de texto | Sí | Trust cards, Dashboard alert, Settings feed |
| Exceso de botones | Sí | Headers Leads/Deals, Trust footer |
| Colores inconsistentes | Sí | small-box vs `.stat` gradients |
| Espaciados incorrectos | Parcial | Command Center 6 cols; mobile KPI |
| Falta identidad visual | Sí | Logo = icono rayo en círculo azul |
| Falta diseño premium | Sí | Sin billing, sin charts, mock data |

---

## 11. Fortalezas (lo que sí parece moderno)

- `site.css` (~1.7k líneas) con tokens, sombras suaves, sidebar refinado.  
- `_PageHeader`, `crm-filter-card`, tablas con `table-desc` y hover.  
- `site.js`: densidad tabla, keyboard rows, reduced motion, runtime bar.  
- Componentes `_CrmEmptyState`, `_CrmLoadingSkeleton`, `_CrmToastContainer`.  
- Index Dashboard con **datos reales** y pipeline accionable.  
- Audit/Users con filtros funcionales y export.  
- Banner comms honesto (simulación vs live).  
- Agrupación nav AUTONOMÍA (diferenciador vs CRM puro).

---

## 12. Pregunta final — CEO, 30 segundos, pricing percibido

### ¿Qué precio parece?

**Rango más probable: $49–$199/mes** (plan equipo SMB), **no** $999+ ni $5,000+/mes.

| Precio | ¿Creíble? | Por qué |
|--------|-----------|--------|
| **$49/mes** | Sí (starter) | Aspecto herramienta interna / CRM open-source template bien customizado; login demo; sin billing UI. |
| **$199/mes** | Sí (techo SMB) | Si solo ve **Index + Leads + Deals** con datos reales; parece «CRM serio para equipo ventas». |
| **$499/mes** | Difícil | Exige confianza enterprise: sin SAML visual, Trust rudimentario, Integrations dev-facing. |
| **$999/mes** | No hoy | Falta polish HubSpot/Attio + módulos completos visibles. |
| **$5,000+/mes** | No | CEO vería AdminLTE, passwords en login, Agents con números falsos → «proyecto interno», no plataforma categoría mundial. |

### Justificación exacta (30 segundos de escaneo)

1. **Primera impresión:** Sidebar oscuro AdminLTE + azul Bootstrap = «otro CRM corporativo», no producto IA de nueva categoría.  
2. **Segunda impresión:** Si abre Login o Agents → **datos demo/mock** → destruye narrativa ABOS premium.  
3. **Tercera impresión:** Módulos diferenciadores (Trust, Command Center, 360) existen pero **se ven como pantallas admin añadidas**, no como experiencia unificada tipo Linear/Attio.  
4. **Cuarta impresión:** No hay **Billing/Stripe UI** → no percibe SaaS maduro monetizable enterprise.  
5. **Quinta impresión:** Copy técnico (Event-Driven, Multi-tenant, MVP, Tenant ID) → audiencia ingeniería, no comprador C-level.

**Para percibir $999–$5,000+** haría falta: identidad visual única, cero mock en producción, Trust/Command Center con diseño «decisión ejecutiva», integraciones con logos y estado (no tokens), billing visible, y demo de outcome/revenue IA en un solo hero screen — ninguno de eso domina hoy en los primeros 30 segundos.

---

## 13. Resumen ejecutivo (1 párrafo)

AutonomusFlow en UI es hoy un **CRM corporativo AdminLTE en evolución hacia SaaS moderno**, con trabajo real en CSS/UX operativa (filtros, tablas, accesibilidad parcial), pero **penalizado por inconsistencia arquitectónica, datos mock en pantallas estratégicas, ausencia de Billing/Revenue/CS visuales, y módulos ABOS con aspecto administrativo**. No transmite startup amateur total ni producto mundial; transmite **producto B2B funcional de fase growth** que un CEO cotizaría como **$49–$199/mes** hasta ver Trust/IA con polish enterprise y sin señales de demo.

---

*Documento generado por auditoría estática. No se modificó código del repositorio.*
