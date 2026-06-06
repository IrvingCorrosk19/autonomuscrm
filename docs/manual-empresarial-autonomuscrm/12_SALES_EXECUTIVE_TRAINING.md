# 12 — Capacitación: Ejecutivo de Ventas (Sales Executive)

**Perfil:** `sales@autonomuscrm.local` / `Sales123!`  
**Home tras login:** `/revenue` (Revenue OS)  
**Audiencia:** 0 años de experiencia en CRM

---

## Módulo 1 — Qué es tu trabajo en AutonomusCRM (30 min)

### Objetivo del rol Sales
Convertir **Leads** (personas interesadas) en **Deals cerrados** (ventas) y colaborar en la retención vía tareas de seguimiento.

### Qué NO es tu trabajo
- Crear usuarios (`/Users`) — solo Admin/Manager
- Cambiar settings del tenant (`/Settings`)
- Aprobar decisiones IA en Trust Studio (típico de Manager/Admin)

### Las 4 pantallas que usarás cada día
1. **Revenue OS** (`/revenue`) — prioridades de ingresos
2. **Leads** (`/Leads`) — nuevos contactos
3. **Pipeline** (`/Deals`) — oportunidades activas
4. **Tasks** (`/Tasks`) — qué debes hacer hoy

---

## Módulo 2 — Primer login (15 min)

1. Ir a `/Account/Login`
2. Email: `sales@autonomuscrm.local`
3. Password: `Sales123!`
4. Verás **Revenue OS** automáticamente
5. Explorar menú lateral: Leads, Pipeline, Tasks
6. Cambiar idioma (selector ES/EN) si prefieres español

---

## Módulo 3 — Leads para principiantes (45 min)

### ¿Qué es un Lead?
Alguien que **aún no es cliente**. Ejemplo: descargó un PDF, llenó un formulario, te dio tarjeta en feria.

### Crear lead manual
1. `/Leads` → **Nuevo lead**
2. Completar: Nombre (obligatorio), Email, Teléfono, Empresa, Fuente
3. Guardar → estado **New**

### Calificar un lead (muy importante)
1. Abrir `/Leads/Details/{id}`
2. Clic **Qualify** (Calificar)
3. El sistema automáticamente:
   - Puede crear un **cliente** si no existía
   - Crea un **deal borrador**
   - Crea una **tarea** de seguimiento urgente

**Regla de oro:** No dejes leads **New** más de 24h — el sistema crea SLA de contacto.

### Convertir vs Calificar
| Acción | Cuándo usarla |
|--------|---------------|
| **Qualify** | Interés confirmado, quieres pipeline automático |
| **Convert to Customer** | Ya cerraste administrativamente como cliente sin deal |
| **Create Deal** | Listo para vender con monto y etapa |

---

## Módulo 4 — Pipeline y Deals (60 min)

### Etapas del embudo (en el sistema)
1. **Prospecting** (10%) — primer contacto
2. **Qualification** (25%) — validaste necesidad
3. **Proposal** (50%) — enviaste propuesta
4. **Negotiation** (75%) — negociando precio/términos
5. **Closed Won** — ganaste
6. **Closed Lost** — perdiste

### Crear deal
1. `/Deals/Create` o desde Lead Details
2. **Cliente obligatorio** — selecciona o crea antes
3. Título, monto, descripción
4. Deal inicia en **Open / Prospecting**

### Mover deal en kanban
`/Deals` → arrastrar tarjeta entre columnas (si UI kanban activa) o editar etapa en Details.

### Cerrar deal ganado
`/Deals/Details` → acción Close → dispara automatizaciones de onboarding (tareas D0, D7, D30).

### Leer forecast
En `/Deals` verás **Forecast 30/60/90 días** y **Win Rate** — son proyecciones para tu manager.

---

## Módulo 5 — Tareas diarias (30 min)

1. `/Tasks` al iniciar el día
2. Filtrar **Open** y **Overdue**
3. Completar cada tarea al terminar la acción
4. Las tareas vienen de: workflows, calificación de leads, cierre de deals, automatización revenue

---

## Módulo 6 — IA para vendedores (30 min)

### Qué hace la IA en AutonomusCRM (real)
- **Puntúa leads** automáticamente (`LeadIntelligenceAgent`)
- **Sugiere acciones** en Command (`/`) y Revenue OS
- **Crea tareas** cuando detecta riesgo (deal estancado, lead inactivo)
- **No reemplaza** tu llamada ni tu criterio comercial

### Qué NO esperar
- El worker **no** usa ChatGPT para redactar emails automáticamente en todos los casos
- Trust Studio es para aprobar decisiones — Sales normalmente **consulta** resultados en Command

### Mejor práctica
Revisa score del lead (>70 = prioridad alta según UI pills).

---

## Módulo 7 — Errores que debes evitar

| Error | Impacto |
|-------|---------|
| Crear deal sin cliente | Imposible — sistema lo exige |
| Calificar y olvidar tarea auto-creada | SLA incumplido |
| Duplicar clientes con mismo email | Confusión en 360 |
| Cerrar deal sin monto real | Forecast distorsionado |
| Ignorar `/Tasks` | Automatizaciones parecen "no funcionar" |

---

## Módulo 8 — Evaluación (checklist)

- [ ] Creé un lead y lo califiqué
- [ ] Abrí la tarea generada y la completé
- [ ] Creé un deal vinculado a cliente
- [ ] Moví deal a Proposal
- [ ] Interpreté Forecast 30d en `/Deals`
- [ ] Revisé Revenue OS y identifiqué 1 prioridad
- [ ] Exporté lista de leads (JSON) de mi página actual

---

## Referencias

- Operación diaria: `13_DAILY_OPERATIONS_GUIDE.md`
- FAQ: `08_FAQ.md`
- Manual completo: `AUTONOMUSCRM_ENTERPRISE_USER_MANUAL.md`
