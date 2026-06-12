# QUICK START GUIDES — Client First Edition

> Mini-cursos operativos · TechSolutions Panamá · Productivo el día 1

**Entorno de práctica:** [http://164.68.99.83:8091](http://164.68.99.83:8091)

## Índice de módulos

1. [Leads](#módulo-leads) — `/Leads`
2. [Deals](#módulo-deals) — `/Deals`
3. [Customers](#módulo-customers) — `/Customers`
4. [Customer360](#módulo-customer360) — `/Customer360`
5. [Tasks](#módulo-tasks) — `/Tasks`
6. [RevenueOS](#módulo-revenueos) — `/revenue`
7. [ExecutiveOS](#módulo-executiveos) — `/executive`
8. [TrustStudio](#módulo-truststudio) — `/TrustInbox`
9. [CustomerSuccess](#módulo-customersuccess) — `/customer-success`
10. [Agents](#módulo-agents) — `/Agents`
11. [Users](#módulo-users) — `/Users`
12. [Policies](#módulo-policies) — `/Policies`
13. [Audit](#módulo-audit) — `/Audit`

---

## Módulo: Leads

| | |
|---|---|
| **Ruta** | `/Leads` |
| **Rol** | vendedor |
| **Duración** | 45–60 min |
| **Empresa caso** | TechSolutions Panamá |

### 1. ¿Por qué existe?

**Problema empresarial:** Las oportunidades llegan dispersas (web, ferias, referidos) y se pierden en email o WhatsApp.

**Impacto económico:** Cada lead sin contactar en 24 h reduce hasta 80 % la probabilidad de conversión. En B2B panameño, un lead perdido puede representar $15 K–$150 K.

**Qué pasa si no se usa:** Ventas trabaja a ciegas, marketing no puede medir ROI y gerencia no puede proyectar ingresos.

### 2. ¿Cuándo debo usarlo?

- Llega un formulario web (como el de Juan en nuestra historia).
- Alguien te pasa un contacto de feria o LinkedIn.
- Un cliente actual te refiere a otra empresa.
- Importas una lista post-evento.
- Necesitas priorizar a quién llamar hoy.

### 3. Historia — TechSolutions Panamá

**Martes 9:15 AM — TechSolutions Panamá**

Juan Morales, gerente de operaciones en **RetailMax**, descargó el whitepaper «Cloud seguro para retail» en tu web. El sistema creó automáticamente un lead con fuente **Web**.

**Carlos**, vendedor junior contratado ayer (nunca usó un CRM), debe:
1. Ver el lead en `/Leads`
2. Llamar a Juan antes de las 10:00
3. Registrar qué necesita
4. Decidir si califica

Si Carlos sigue esta guía, a las 11:00 Juan estará calificado y con un deal de $42 K en Discovery.

### 4. Recorrido paso a paso

#### PASO 1 — Iniciar sesión y abrir Leads
- **Qué hacer:** Ir a http://164.68.99.83:8091/Account/Login → menú **Leads** (`/Leads`).
- **Qué esperar:** Ves la lista con filtros y botón «Nuevo lead».
- **Qué validar:** Estás autenticado; ves columnas Nombre, Estado, Fuente.

#### PASO 2 — Localizar el lead de Juan
- **Qué hacer:** Filtrar por hoy o buscar «RetailMax» / «Juan».
- **Qué esperar:** Aparece el registro con fuente Web.
- **Qué validar:** Email y empresa coinciden con el formulario.

#### PASO 3 — Abrir detalle y registrar primer contacto
- **Qué hacer:** Clic en el lead → agregar nota de llamada: duración, dolor, próximo paso.
- **Qué esperar:** La nota queda en historial.
- **Qué validar:** Nota con fecha, no vacía.

#### PASO 4 — Calificar con criterio BANT
- **Qué hacer:** Budget, Authority, Need, Timeline — botón **Qualify** si cumple.
- **Qué esperar:** Estado cambia a Calificado.
- **Qué validar:** Al menos 3 de 4 criterios documentados.

#### PASO 5 — Crear oportunidad o nurture
- **Qué hacer:** Si califica: **Crear Deal** o **Convert to Customer** según proceso.
- **Qué esperar:** Deal en pipeline o tarea de seguimiento.
- **Qué validar:** Deal vinculado al lead; no duplicar cliente.

#### PASO 6 — Cerrar el ciclo del día
- **Qué hacer:** Revisar que no queden leads «Nuevo» sin tocar.
- **Qué esperar:** Bandeja limpia o con tareas futuras.
- **Qué validar:** Cada lead tiene estado distinto de Nuevo o tarea asignada.

### 5. Lista de capturas (placeholders)

- **CAPTURA 01** — _Pantalla principal Leads — lista con filtros y métricas_
- **CAPTURA 02** — _Formulario Crear Lead (`/Leads/Create`) con campos obligatorios_
- **CAPTURA 03** — _Detalle del lead Juan Morales — notas y timeline_
- **CAPTURA 04** — _Modal o acción Qualify / Convert to Customer_
- **CAPTURA 05** — _Deal creado desde lead — vínculo visible_

### 6. Escenario completo Lead → Renewal

Lead (Juan) → Contacto <24 h → Calificado → Deal Discovery → Propuesta → Won → Customer → CS Onboarding → Renewal

### 7. 20 errores comunes

1. No contactar lead inbound en las primeras 24 horas
2. Dejar el lead en estado «Nuevo» sin ninguna nota
3. Calificar sin hablar con el prospecto
4. Crear cliente duplicado sin buscar en Directorio
5. No registrar la fuente o campaña del lead
6. Perder el email o teléfono en notas sueltas fuera del CRM
7. Asignar lead a vendedor equivocado sin reasignar en sistema
8. Borrar lead en lugar de marcar descalificado
9. Crear deal gigante sin etapa Discovery
10. No usar BANT en cuentas enterprise
11. Ignorar leads de referido (alta conversión)
12. Importar CSV sin validar duplicados
13. No crear tarea de seguimiento tras la llamada
14. Prometer precio sin deal ni aprobación
15. Mezclar leads de prueba con producción
16. No actualizar estado tras cada touch
17. Dejar campos obligatorios vacíos «para ir rápido»
18. No vincular lead a campaña de marketing
19. Perseguir leads no calificados por ego
20. Olvidar convertir lead antes del cierre formal

### 8. 20 buenas prácticas

1. Regla de oro: primer contacto en <24 h (ideal <1 h en inbound caliente)
2. Cada llamada termina con nota en el CRM el mismo día
3. Un lead = una fuente registrada para medir marketing
4. Buscar en Customers antes de crear registro nuevo
5. Qualify solo con evidencia en notas
6. Crear tarea con fecha para el próximo paso
7. Usar convención de nombres: Empresa — Contacto — Campaña
8. Revisar Command Center por leads sin actividad
9. Handoff claro si el lead es de otro territorio
10. Descalificar con razón (presupuesto, timing) para nurture
11. Vincular deal al lead, no crear oportunidad huérfana
12. Practicar en University antes del primer lead real
13. Ctrl+K para saltar rápido al lead del día
14. Revisar bandeja Leads al iniciar la jornada
15. Coordinar con marketing la definición de MQL
16. No mezclar idiomas en notas sin etiqueta
17. Export solo con autorización de datos
18. Celebrar wins pero documentar losses
19. Pedir introducción al decisor en cuentas B2B
20. Cerrar el día con cero leads «Nuevo» sin tocar

### 9. Checklist operativo

- ☐ Registré cada contacto en notas
- ☐ Actualicé estado del lead
- ☐ Creé tarea de seguimiento
- ☐ Verifiqué duplicados en Customers
- ☐ Si califica: deal o conversión iniciada
- ☐ Fuente/campaña documentada
- ☐ Supervisor revisó mi primer lead real

### 10. Ejercicio práctico (entorno QA)

En entorno QA (http://164.68.99.83:8091): crear lead «Prueba Academy — RetailMax», simular llamada, nota BANT, calificar, crear deal $10 K Discovery.

> Entorno: [http://164.68.99.83:8091](http://164.68.99.83:8091)

### 11. Evaluación — mini examen

**1.** ¿Cuánto tiempo máximo recomendado para primer contacto inbound?
   - A) 24 horas
   - B) 1 semana
   - C) Cuando haya tiempo
   - D) No importa
   - **Respuesta correcta:** A) 24 horas

**2.** ¿Qué significa calificar un lead?
   - A) Validar si hay oportunidad real
   - B) Cambiar color
   - C) Borrarlo
   - D) Enviar spam
   - **Respuesta correcta:** A) Validar si hay oportunidad real

**3.** Juan de RetailMax llegó por web. ¿Primera pantalla?
   - A) /Leads
   - B) /Billing
   - C) /Audit
   - D) /Policies
   - **Respuesta correcta:** A) /Leads

**4.** ¿Qué hacer antes de crear Customer nuevo?
   - A) Buscar en Directorio
   - B) Borrar lead
   - C) Nada
   - D) Crear duplicado
   - **Respuesta correcta:** A) Buscar en Directorio

**5.** Lead calificado sin deal. ¿Riesgo principal?
   - A) Pipeline invisible para gerencia
   - B) Ninguno
   - C) Mejor así
   - D) Marketing feliz
   - **Respuesta correcta:** A) Pipeline invisible para gerencia

**6.** ¿Qué es BANT?
   - A) Budget, Authority, Need, Timeline
   - B) Un tipo de lead
   - C) Un reporte
   - D) Una política
   - **Respuesta correcta:** A) Budget, Authority, Need, Timeline

### 12. Certificación del módulo

Completar ejercicio QA + 5/6 evaluación + supervisor valida primer lead real en <24 h → badge **Lead Hunter**.

---

## Módulo: Deals

| | |
|---|---|
| **Ruta** | `/Deals` |
| **Rol** | vendedor o gerente |
| **Duración** | 50–65 min |
| **Empresa caso** | TechSolutions Panamá |

### 1. ¿Por qué existe?

**Problema empresarial:** Las oportunidades se gestionan en hojas de cálculo sin visibilidad de etapa, probabilidad ni fecha de cierre.

**Impacto económico:** Un pipeline opaco hace que el forecast falle un 30–40 % y que deals estancados consuman tiempo sin retorno.

**Qué pasa si no se usa:** Gerencia promete ingresos que no existen; vendedores persiguen deals muertos; CS recibe clientes sin contexto.

### 2. ¿Cuándo debo usarlo?

- Tienes una propuesta enviada y necesitas avanzar etapa.
- Un deal lleva 14 días sin actividad.
- Debes decidir si marcar Won o Lost.
- Gerente pide revisión de pipeline del trimestre.
- Cliente pide descuento antes de firmar.

### 3. Historia — TechSolutions Panamá

**Jueves 14:00 — TechSolutions Panamá**

**María**, account executive, tiene un deal de **$85 K** con **Banco Regional** en etapa Propuesta desde hace 10 días. El CFO pidió una reunión el lunes.

Su manager **Luis** ve en Revenue OS que el deal tiene probabilidad 70 % pero sin actividad registrada — señal de riesgo.

María abre `/Deals`, actualiza la nota de la llamada con el CFO, baja probabilidad a 50 % (honestidad), crea tarea «Enviar ROI revisado» para mañana y mueve a Negociación. El forecast del equipo mejora porque refleja la realidad.

### 4. Recorrido paso a paso

#### PASO 1 — Abrir pipeline
- **Qué hacer:** Login → **Deals** (`/Deals`).
- **Qué esperar:** Vista kanban o lista por etapa.
- **Qué validar:** Ves deals propios o del equipo según rol.

#### PASO 2 — Localizar deal Banco Regional
- **Qué hacer:** Filtrar por cuenta o buscar «Banco Regional».
- **Qué esperar:** Deal visible con etapa y valor.
- **Qué validar:** Valor $85 K y etapa coherentes.

#### PASO 3 — Revisar actividad y riesgo
- **Qué hacer:** Abrir detalle → leer notas, señales IA si aparecen.
- **Qué esperar:** Identificas días sin touch.
- **Qué validar:** Última nota <7 días o justificación.

#### PASO 4 — Registrar interacción y ajustar probabilidad
- **Qué hacer:** Nueva nota post-llamada CFO; probabilidad alineada a etapa.
- **Qué esperar:** Historial actualizado.
- **Qué validar:** Probabilidad no inflada vs etapa.

#### PASO 5 — Definir próximo paso
- **Qué hacer:** Crear tarea vinculada con fecha.
- **Qué esperar:** Tarea en `/Tasks`.
- **Qué validar:** Fecha antes del cierre esperado.

#### PASO 6 — Avanzar etapa o marcar Lost
- **Qué hacer:** Mover a Negociación, Won (con evidencia) o Lost con razón.
- **Qué esperar:** Pipeline refleja verdad.
- **Qué validar:** Lost siempre con motivo documentado.

### 5. Lista de capturas (placeholders)

- **CAPTURA 01** — _Lista/kanban Deals con etapas del pipeline_
- **CAPTURA 02** — _Detalle deal Banco Regional — valor, etapa, probabilidad_
- **CAPTURA 03** — _Panel de notas e historial de actividad_
- **CAPTURA 04** — _Cambio de etapa y ajuste de probabilidad_
- **CAPTURA 05** — _Deal Won con fecha de cierre y handoff a CS_
- **CAPTURA 06** — _Deal Lost con razón documentada_

### 6. Escenario completo Lead → Renewal

Lead calificado → Deal Discovery → Propuesta → Negociación → Won → Customer → Implementación → Renewal

### 7. 20 errores comunes

1. Mantener probabilidad 90 % en Discovery
2. Avanzar etapa sin actividad registrada
3. Marcar Won sin contrato o PO
4. No documentar razón al perder
5. Crear deal sin vincular a cuenta/lead
6. Duplicar deal para la misma oportunidad
7. Ignorar alertas de deal en riesgo
8. Prometer fecha de cierre irreal cada semana
9. No involucrar al decisor económico
10. Dejar deals zombi más de 30 días
11. Cambiar owner sin notificar al cliente
12. No actualizar valor tras cambio de alcance
13. Mezclar monedas sin conversión clara
14. Cerrar deal sin handoff a CS
15. Usar etapa Propuesta sin documento enviado
16. No revisar competencia en notas
17. Forecast personal sin revisar con manager
18. Split deals para inflar métricas
19. Olvidar vincular productos o líneas
20. Perder deal por no registrar objeciones

### 8. 20 buenas prácticas

1. Probabilidad honesta por etapa — regla del equipo
2. Cada reunión termina con nota y próximo paso
3. Revisar deals en riesgo cada mañana en Command
4. Lost con razón = aprendizaje para el equipo
5. Won solo con evidencia (contrato, PO)
6. Handoff escrito a CS el día del cierre
7. Actualizar valor cuando cambia el alcance
8. Involucrar champion y decisor en notas
9. Pipeline review semanal con manager
10. Usar Customer 360 antes de negociación enterprise
11. Descuento solo con política y aprobación
12. Fecha de cierre = compromiso del cliente, no deseo
13. Un deal = una oportunidad clara
14. Registrar competidor en cada deal grande
15. Reactivar deals estancados con plan, no esperanza
16. Vincular tareas a cada deal activo
17. Celebrar Won en equipo — documentar cómo
18. Trimestre nuevo = limpiar pipeline
19. Practicar escenario Lost en University
20. Ctrl+K para saltar al deal del día

### 9. Checklist operativo

- ☐ Revisé todos mis deals activos
- ☐ Cada deal tiene nota <7 días o plan
- ☐ Probabilidades alineadas a etapa
- ☐ Próximo paso con tarea fechada
- ☐ Deals Lost/Won actualizados esta semana
- ☐ Handoff documentado en deals Won

### 10. Ejercicio práctico (entorno QA)

En QA: localizar o crear deal «Academy — Banco Test» $25 K, registrar nota, ajustar probabilidad, crear tarea, mover etapa.

> Entorno: [http://164.68.99.83:8091](http://164.68.99.83:8091)

### 11. Evaluación — mini examen

**1.** Deal en Discovery con 90 % probabilidad es:
   - A) Error común
   - B) Buena práctica
   - C) Obligatorio
   - D) Recomendado
   - **Respuesta correcta:** A) Error común

**2.** Al marcar deal Lost debes:
   - A) Documentar razón
   - B) Borrar cliente
   - C) Ocultar registro
   - D) Nada
   - **Respuesta correcta:** A) Documentar razón

**3.** ¿Dónde ves el pipeline completo?
   - A) /Deals
   - B) /Audit
   - C) /Users
   - D) /Policies
   - **Respuesta correcta:** A) /Deals

**4.** Won sin contrato genera:
   - A) Problemas en forecast y CS
   - B) Nada
   - C) Más comisión segura
   - D) Menos trabajo
   - **Respuesta correcta:** A) Problemas en forecast y CS

**5.** Deal 14 días sin actividad. ¿Primera acción?
   - A) Llamar y registrar nota
   - B) Borrar
   - C) Subir probabilidad
   - D) Ignorar
   - **Respuesta correcta:** A) Llamar y registrar nota

**6.** Handoff a CS debe incluir:
   - A) Contexto y expectativas
   - B) Solo nombre
   - C) Password
   - D) Nada
   - **Respuesta correcta:** A) Contexto y expectativas

### 12. Certificación del módulo

Ejercicio QA + 5/6 evaluación + manager valida pipeline review → badge **Pipeline Pro**.

---

## Módulo: Customers

| | |
|---|---|
| **Ruta** | `/Customers` |
| **Rol** | vendedor o CS |
| **Duración** | 40–55 min |
| **Empresa caso** | TechSolutions Panamá |

### 1. ¿Por qué existe?

**Problema empresarial:** Los datos de clientes viven en carpetas, Excel y la memoria de cada vendedor — sin dueño ni calidad.

**Impacto económico:** Datos duplicados cuestan ~$15 K/año por cuenta en retrabajo; expansión y renovación fallan por contactos incorrectos.

**Qué pasa si no se usa:** Llamas al contacto equivocado, facturas al email viejo y CS no sabe quién es el sponsor.

### 2. ¿Cuándo debo usarlo?

- Convertiste un lead o cerraste un deal — necesitas la ficha de cliente.
- Buscar si ya existe antes de crear duplicado.
- Actualizar contacto principal tras cambio en la cuenta.
- Preparar visita comercial con datos correctos.
- Segmentar cartera para campaña de expansión.

### 3. Historia — TechSolutions Panamá

**Miércoles 8:30 AM — TechSolutions Panamá**

**Andrea** en ventas recibe un email: «Somos **Grupo Andina Logística**, nos recomendó **RetailMax**». Antes de crear nada, busca en `/Customers` «Andina».

Encuentra un registro incompleto de hace 2 años. Actualiza razón social, agrega al nuevo director de TI **Roberto Vega**, marca a RetailMax como referencia y vincula el deal de expansión $28 K. Evitó un duplicado que habría confundido a facturación.

### 4. Recorrido paso a paso

#### PASO 1 — Abrir directorio
- **Qué hacer:** **Customers** (`/Customers`).
- **Qué esperar:** Lista con búsqueda y filtros.
- **Qué validar:** Ves clientes activos e inactivos.

#### PASO 2 — Buscar antes de crear
- **Qué hacer:** Buscar empresa, RUC o contacto.
- **Qué esperar:** Resultados o vacío confirmado.
- **Qué validar:** Búsqueda documentada en nota si creas nuevo.

#### PASO 3 — Completar ficha
- **Qué hacer:** Razón social, industria, owner, contactos.
- **Qué esperar:** Campos críticos llenos.
- **Qué validar:** Sin campos obligatorios vacíos.

#### PASO 4 — Registrar contactos clave
- **Qué hacer:** Decisor, champion, facturación.
- **Qué esperar:** Mínimo 2 contactos en cuentas B2B.
- **Qué validar:** Emails válidos y roles claros.

#### PASO 5 — Vincular oportunidad
- **Qué hacer:** Crear deal expansión desde la cuenta.
- **Qué esperar:** Deal ligado al customer.
- **Qué validar:** No deal huérfano.

#### PASO 6 — Abrir 360
- **Qué hacer:** Clic en vista 360 para contexto completo.
- **Qué esperar:** Timeline unificado.
- **Qué validar:** Deals, tickets visibles si existen.

### 5. Lista de capturas (placeholders)

- **CAPTURA 01** — _Lista Customers con búsqueda global_
- **CAPTURA 02** — _Formulario crear/editar (`/Customers/Create`)_
- **CAPTURA 03** — _Ficha Grupo Andina — contactos y owner_
- **CAPTURA 04** — _Deal expansión vinculado a la cuenta_
- **CAPTURA 05** — _Enlace a Customer 360 desde la ficha_

### 6. Escenario completo Lead → Renewal

Lead → Deal Won → **Customer creado/actualizado** → Onboarding → Uso → Expansión → Renewal

### 7. 20 errores comunes

1. Crear cliente sin buscar duplicados
2. Dejar owner vacío
3. Mezclar personas de distintas empresas en un registro
4. No actualizar email de facturación
5. Marcar activo a cuenta churned
6. Usar apodos en lugar de razón social
7. No registrar industria o segmento
8. Contacto único sin backup
9. Borrar cliente con historial
10. No vincular deals a la cuenta correcta
11. Import masivo sin reglas de deduplicación
12. Ignorar alertas de datos incompletos
13. Compartir export sin permiso
14. No documentar cambio de sponsor
15. Fusionar cuentas sin revisar 360
16. Tratar prospecto como customer antes de Won
17. No segmentar por tier (SMB/Enterprise)
18. Olvidar idioma/zona horaria del cliente
19. Datos de prueba en producción
20. No revisar cartera asignada cada mes

### 8. 20 buenas prácticas

1. Buscar siempre antes de crear
2. Convención de nombres legal consistente
3. Owner claro por cuenta
4. Mínimo decisor + operativo en contactos
5. Actualizar en 24 h si cambia contacto clave
6. Revisar cuentas incompletas cada viernes
7. Vincular cada deal a customer
8. 360 antes de visita importante
9. Segmentar para campañas de expansión
10. Documentar referidos y partners
11. Offboarding de contactos que salen
12. Tier VIP visible en notas
13. Coordinar con finanzas el email de factura
14. No mezclar cuentas matriz y filial sin relación
15. University antes de primer alta masiva
16. Ctrl+K para ir directo al cliente
17. Health check de datos trimestral
18. Fusionar duplicados con auditoría
19. Celebrar expansión — registrar en 360
20. Mentor revisa primeras 5 altas del nuevo

### 9. Checklist operativo

- ☐ Busqué antes de crear
- ☐ Campos obligatorios completos
- ☐ Contactos decisor y operativo
- ☐ Owner asignado
- ☐ Deal vinculado si aplica
- ☐ 360 revisado

### 10. Ejercicio práctico (entorno QA)

En QA: buscar «Academy Test Corp», actualizar o crear ficha completa, agregar contacto, abrir 360.

> Entorno: [http://164.68.99.83:8091](http://164.68.99.83:8091)

### 11. Evaluación — mini examen

**1.** ¿Primer paso ante empresa «nueva»?
   - A) Buscar en Customers
   - B) Crear directo
   - C) Borrar leads
   - D) Ignorar
   - **Respuesta correcta:** A) Buscar en Customers

**2.** Customer sin owner implica:
   - A) Nadie responsable de la cuenta
   - B) Mejor así
   - C) Automático
   - D) Sin impacto
   - **Respuesta correcta:** A) Nadie responsable de la cuenta

**3.** ¿Dónde ves deals y tickets juntos?
   - A) Customer 360
   - B) Solo Leads
   - C) Audit
   - D) Users
   - **Respuesta correcta:** A) Customer 360

**4.** Duplicados causan principalmente:
   - A) Retrabajo y errores
   - B) Más ventas
   - C) Nada
   - D) Menos trabajo
   - **Respuesta correcta:** A) Retrabajo y errores

**5.** Deal expansión debe:
   - A) Vincularse al customer
   - B) Ser independiente
   - C) Borrarse
   - D) No registrarse
   - **Respuesta correcta:** A) Vincularse al customer

**6.** Contacto de facturación incorrecto afecta:
   - A) Cobranza y renovación
   - B) Solo marketing
   - C) Nada
   - D) UI
   - **Respuesta correcta:** A) Cobranza y renovación

### 12. Certificación del módulo

Ejercicio QA + evaluación + mentor valida ficha sin duplicados → badge **Account Keeper**.

---

## Módulo: Customer360

| | |
|---|---|
| **Ruta** | `/Customer360` |
| **Rol** | todos los roles |
| **Duración** | 60–90 min |
| **Empresa caso** | TechSolutions Panamá |

### 1. ¿Por qué existe?

**Problema empresarial:** Antes de cada llamada importante, el equipo no sabe qué pasó ayer: tickets abiertos, renovación en 60 días, deal en riesgo o caída de uso.

**Impacto económico:** Un ejecutivo sin contexto pierde 20–40 min por llamada reconstruyendo historia; en cuentas de $100 K+ ARR, un error de contexto puede costar la renovación.

**Qué pasa si no se usa:** Prometes lo que otro equipo ya prometió, ignoras un ticket P1, llegas a la renovación sin saber el health score y el cliente siente que «no los conocen».

### 2. ¿Cuándo debo usarlo?

- Llamada de escalamiento con cuenta VIP.
- Preparación de QBR o revisión de negocio.
- Renovación a 90, 60 o 30 días.
- Cliente en riesgo (health bajo, NPS malo).
- Antes de proponer expansión o upsell.
- Handoff ventas → CS o CS → ventas.

### 3. Historia — TechSolutions Panamá

**Viernes 7:45 AM — TechSolutions Panamá**

**Roberto**, CS Manager, tiene QBR a las 10:00 con **Logística del Canal** ($120 K ARR). Ayer soporte cerró un ticket P2 de integración; ventas tiene un deal de expansión $35 K; health bajó a **Ámbar** por uso irregular; renovación en **72 días**.

Roberto abre `/Customer360`, busca la cuenta, en 8 minutos recorre:
- **Timeline** — últimos 90 días de interacciones
- **Health** — factores y tendencia
- **Tickets** — uno abierto de documentación
- **Revenue** — ARR, expansión pipeline
- **Renewal** — fecha y owner
- **Risk** — señales IA y concentración
- **AI insights** — resumen ejecutivo sugerido

Llega al QBR con 3 bullets y un plan de acción. El cliente nota la diferencia.

### 4. Recorrido paso a paso

#### PASO 1 — Acceder a Customer 360
- **Qué hacer:** Menú **Customer 360** (`/Customer360`) o desde ficha `/customers/{id}/360`.
- **Qué esperar:** Buscador de cuentas y lista recientes.
- **Qué validar:** Encuentras la cuenta en <30 s.

#### PASO 2 — Leer Timeline (corazón del 360)
- **Qué hacer:** Scroll cronológico: notas, emails, reuniones, cambios etapa.
- **Qué esperar:** Historia unificada sin saltar pantallas.
- **Qué validar:** Últimos 30 días revisados; huecos identificados.

#### PASO 3 — Revisar Health Score
- **Qué hacer:** Panel salud: adopción, tickets, pagos, engagement.
- **Qué esperar:** Verde/Ámbar/Rojo con drivers.
- **Qué validar:** Entiendes *por qué* está el color, no solo el número.

#### PASO 4 — Tickets y escalaciones
- **Qué hacer:** Abiertos, SLA, severidad, owner.
- **Qué esperar:** Sabes si hay fuego antes de hablar.
- **Qué validar:** P1/P2 con plan; ningún abierto ignorado.

#### PASO 5 — Revenue y renovación
- **Qué hacer:** ARR/MRR, deals activos, fecha renewal, cobertura expansión.
- **Qué esperar:** Cuadro económico claro.
- **Qué validar:** Renewal owner y fecha en calendario mental.

#### PASO 6 — Risk y señales IA
- **Qué hacer:** Alertas de churn, deals estancados, anomalías de uso.
- **Qué esperar:** Lista priorizada de riesgos.
- **Qué validar:** Al menos un riesgo con acción asignada.

#### PASO 7 — Actuar y registrar
- **Qué hacer:** Crear tarea, nota o escalar desde el 360.
- **Qué esperar:** Próximo paso en sistema.
- **Qué validar:** Cliente ve continuidad post-llamada.

#### PASO 8 — Post-QBR / post-llamada
- **Qué hacer:** Resumen en nota vinculada a la cuenta.
- **Qué esperar:** Timeline actualizado.
- **Qué validar:** Compromisos con fecha.

### 5. Lista de capturas (placeholders)

- **CAPTURA 01** — _Entrada Customer 360 — búsqueda y cuentas recientes_
- **CAPTURA 02** — _Timeline unificado — 90 días de actividad_
- **CAPTURA 03** — _Panel Health Score con drivers_
- **CAPTURA 04** — _Sección Tickets — abiertos y SLA_
- **CAPTURA 05** — _Bloque Revenue — ARR y deals vinculados_
- **CAPTURA 06** — _Widget Renewal — cuenta regresiva y owner_
- **CAPTURA 07** — _Panel Risk / alertas_
- **CAPTURA 08** — _Insights IA — resumen ejecutivo sugerido_

### 6. Escenario completo Lead → Renewal

Lead → Deal → Customer → **360 diario en cuentas clave** → Tickets resueltos → Health estable → Renewal ganada → Expansión

### 7. 20 errores comunes

1. Llamar a cliente enterprise sin abrir 360
2. Ignorar ticket abierto visible en el timeline
3. No revisar fecha de renovación antes de QBR
4. Confiar solo en memoria del vendedor
5. Prometer funcionalidad sin leer tickets previos
6. No actuar ante health en rojo
7. Duplicar notas fuera del 360
8. Olvidar deals de expansión al hablar de renovación
9. Escalar sin documentar en la cuenta
10. Leer solo la primera pantalla del timeline
11. Ignorar señales IA sin validar
12. No verificar contacto correcto antes de email masivo
13. QBR sin métricas de valor del 360
14. Mezclar cuentas matriz y filial en la búsqueda
15. No registrar outcome de la reunión
16. Asumir health verde sin mirar tendencia
17. No coordinar con owner de renewal
18. Perder contexto en handoff entre equipos
19. Exportar 360 sin permiso en cuentas sensibles
20. Cerrar ticket en cabeza pero no en sistema

### 8. 20 buenas prácticas

1. 360 antes de toda llamada >$10 K o VIP
2. Orden fijo: Timeline → Health → Tickets → Revenue → Renewal → Risk
3. Registrar resumen post-llamada el mismo día
4. Compartir captura de health en war room de riesgo
5. Vincular tareas a la cuenta desde el 360
6. Renovación a 90 días = primer playbook en 360
7. Cruzar health con uso real del producto
8. Un owner por renewal visible en 360
9. Leer insights IA como borrador, no verdad absoluta
10. Preparar QBR con 3 métricas de valor del timeline
11. Escalar P1 con enlace al registro en 360
12. Ctrl+K → nombre cliente → 360
13. Revisar cuentas ámbar cada lunes
14. Handoff ventas-CS con nota en 360
15. Documentar champion y decisor en contactos
16. No prometer sin revisar compromisos previos en timeline
17. Practicar recorrido 8 min en University
18. Mentor observa primer QBR con checklist 360
19. Celebrar renewal — nota en timeline
20. Auditar cuentas sin actividad 30 días en 360

### 9. Checklist operativo

- ☐ Abrí 360 de la cuenta objetivo
- ☐ Revisé timeline últimos 30 días
- ☐ Health y drivers entendidos
- ☐ Tickets abiertos verificados
- ☐ Renewal y revenue revisados
- ☐ Riesgos con acción asignada
- ☐ Nota post-interacción registrada

### 10. Ejercicio práctico (entorno QA)

En QA: abrir 360 de cuenta demo, completar recorrido 8 min (timeline, health, tickets, revenue, renewal, risk), registrar nota QBR simulada.

> Entorno: [http://164.68.99.83:8091](http://164.68.99.83:8091)

### 11. Evaluación — mini examen

**1.** ¿Módulo para vista unificada pre-llamada?
   - A) Customer 360
   - B) Audit
   - C) Users
   - D) Policies
   - **Respuesta correcta:** A) Customer 360

**2.** Health en ámbar sin revisar drivers es:
   - A) Error de preparación
   - B) Suficiente
   - C) Opcional
   - D) Mejor práctica
   - **Respuesta correcta:** A) Error de preparación

**3.** Renovación a 72 días. ¿Dónde lo ves primero?
   - A) Panel Renewal en 360
   - B) Solo email
   - C) No se registra
   - D) Leads
   - **Respuesta correcta:** A) Panel Renewal en 360

**4.** Ticket P1 abierto. ¿Antes de llamada comercial?
   - A) Revisar y coordinar con CS
   - B) Ignorar
   - C) Cerrar sin leer
   - D) Borrar
   - **Respuesta correcta:** A) Revisar y coordinar con CS

**5.** Timeline sirve para:
   - A) Historia única sin saltar pantallas
   - B) Editar código
   - C) Facturación legal
   - D) Crear usuarios
   - **Respuesta correcta:** A) Historia única sin saltar pantallas

**6.** Insights IA en 360 debes:
   - A) Validar con contexto humano
   - B) Aprobar ciegamente
   - C) Ignorar siempre
   - D) Borrar
   - **Respuesta correcta:** A) Validar con contexto humano

**7.** QBR sin revisar 360 genera:
   - A) Cliente siente desconocimiento
   - B) Más ventas seguras
   - C) Nada
   - D) Menos trabajo
   - **Respuesta correcta:** A) Cliente siente desconocimiento

### 12. Certificación del módulo

Recorrido 360 en QA <10 min + 6/7 evaluación + supervisor observa QBR simulado → badge **360 Navigator** (módulo prioritario).


### Profundización — pilares del Customer 360

| Pilar | Qué mirar | Pregunta clave |
|-------|-----------|----------------|
| **Timeline** | Notas, etapas, reuniones | ¿Qué prometimos la última vez? |
| **Health** | Score y drivers | ¿Sube o baja? ¿Por qué? |
| **Tickets** | Abiertos, SLA, severidad | ¿Hay fuego activo? |
| **Revenue** | ARR, deals, expansión | ¿Cuánto vale y hacia dónde crece? |
| **Renewal** | Fecha, owner, playbook | ¿Quién cierra y cuándo? |
| **Risk** | Alertas, IA, concentración | ¿Qué puede salir mal esta semana? |
| **AI** | Resumen y sugerencias | ¿Qué hipótesis valido con el cliente? |

---

## Módulo: Tasks

| | |
|---|---|
| **Ruta** | `/Tasks` |
| **Rol** | todos |
| **Duración** | 35–45 min |
| **Empresa caso** | TechSolutions Panamá |

### 1. ¿Por qué existe?

**Problema empresarial:** Los compromisos viven en agendas personales, post-its y chats — el equipo no ve quién debe hacer qué ni cuándo.

**Impacto económico:** Sin tareas en CRM, el 30 % de seguimientos comerciales no ocurre; deals se enfrían y clientes perciben abandono.

**Qué pasa si no se usa:** Olvidas llamar al CFO, el manager no puede coacharte y el forecast miente por falta de actividad real.

### 2. ¿Cuándo debo usarlo?

- Terminaste una reunión — ¿qué sigue?
- Te asignaron seguimiento de un deal o ticket.
- Planificas tu semana comercial.
- Tienes tareas vencidas en rojo.
- Coordinas handoff con otro colega.

### 3. Historia — TechSolutions Panamá

**Lunes 8:00 AM — TechSolutions Panamá**

**Pedro**, SDR, abre `/Tasks` con 12 ítems: 4 vencidos (ayer no cerró), 6 hoy, 2 futuras. Prioriza: llamada a **Distribuidora Pacífico** (deal $18 K), envío de propuesta **Clínica San Fernando**, y seguimiento ticket escalado.

En 25 minutos reprograma lo realista, completa 3 tareas con nota breve y deja cero vencidos sin nueva fecha. Su manager ve actividad real en el pipeline.

### 4. Recorrido paso a paso

#### PASO 1 — Abrir bandeja
- **Qué hacer:** **Tasks** (`/Tasks`).
- **Qué esperar:** Lista por fecha, prioridad, estado.
- **Qué validar:** Ves propias y asignadas según rol.

#### PASO 2 — Triaje matutino
- **Qué hacer:** Filtrar vencidas → hoy → esta semana.
- **Qué esperar:** Cola priorizada.
- **Qué validar:** Vencidas con plan o completadas.

#### PASO 3 — Vincular contexto
- **Qué hacer:** Al crear: ligar a lead, deal, customer o ticket.
- **Qué esperar:** Tarea con enlace.
- **Qué validar:** No tareas huérfanas «llamar cliente».

#### PASO 4 — Ejecutar y completar
- **Qué hacer:** Marcar completa + nota de outcome.
- **Qué esperar:** Desaparece de pendientes.
- **Qué validar:** Nota útil para el 360.

#### PASO 5 — Reprogramar con honestidad
- **Qué hacer:** Si no cumpliste: nueva fecha hoy, no «algún día».
- **Qué esperar:** Fecha realista.
- **Qué validar:** Sin vencidas zombies >3 días.

#### PASO 6 — Cierre de jornada
- **Qué hacer:** Revisar mañana; crear tarea post-última reunión.
- **Qué esperar:** Bandeja lista para martes.
- **Qué validar:** Cada reunión del día tiene siguiente paso.

### 5. Lista de capturas (placeholders)

- **CAPTURA 01** — _Bandeja Tasks — filtros vencidas/hoy_
- **CAPTURA 02** — _Crear tarea con vínculo a deal_
- **CAPTURA 03** — _Prioridad y fecha de vencimiento_
- **CAPTURA 04** — _Completar tarea con nota_
- **CAPTURA 05** — _Vista semanal de compromisos_

### 6. Escenario completo Lead → Renewal

Reunión → **Tarea creada** → Ejecución → Nota en registro → Siguiente tarea → Deal avanza → Renewal

### 7. 20 errores comunes

1. Tareas genéricas sin vínculo («llamar»)
2. Dejar vencidas sin reprogramar
3. Marcar completa sin haber hecho nada
4. Urgent en todo
5. No poner fecha
6. Duplicar tareas para el mismo compromiso
7. Ignorar tareas asignadas por otros
8. No revisar bandeja al iniciar día
9. Crear tarea sin owner
10. Olvidar tarea tras cerrar deal
11. Usar Tasks como bloc de notas largas
12. No completar al hacer el trabajo
13. Semana sin revisión de carga
14. Tareas de prueba en producción
15. No coordinar con calendario externo
16. Delegar sin notificar en sistema
17. Cerrar ticket sin tarea de seguimiento al cliente
18. Mezclar personal y trabajo en misma vista sin filtro
19. No usar prioridad en cuentas VIP
20. Fin de mes sin limpiar completadas antiguas

### 8. 20 buenas prácticas

1. Revisar Tasks 2× al día (mañana y cierre)
2. Cada reunión termina con tarea fechada
3. Vincular siempre a registro de negocio
4. Completar con nota de una línea útil
5. Reprogramar el mismo día si no cumples
6. Urgent solo para SLA o VIP
7. Vista semanal los viernes
8. Manager revisa vencidas del equipo
9. Ctrl+K para crear tarea rápida
10. Handoff = tarea asignada al otro owner
11. Practicar triaje en University
12. Cero vencidas >72 h sin explicación
13. Tarea de renovación a 90/60/30 días automática en playbook
14. Bloquear tiempo en calendario para tareas grandes
15. Notificar al cliente solo tras tarea «enviar» completada
16. Celebrar semana sin vencidas
17. Plantillas de tarea por tipo de deal
18. No duplicar — buscar tarea existente
19. Auditar tareas huérfanas mensual
20. Mentor revisa bandeja del nuevo en día 3

### 9. Checklist operativo

- ☐ Revisé vencidas y hoy
- ☐ Tareas vinculadas a registros
- ☐ Completé con notas
- ☐ Reprogramé lo no hecho
- ☐ Mañana tiene tareas claras

### 10. Ejercicio práctico (entorno QA)

En QA: crear 3 tareas (deal, customer, lead), completar 1, reprogramar 1, dejar 1 para mañana.

> Entorno: [http://164.68.99.83:8091](http://164.68.99.83:8091)

### 11. Evaluación — mini examen

**1.** ¿Cuándo crear tarea?
   - A) Tras cada reunión o compromiso
   - B) Nunca
   - C) Solo viernes
   - D) Una vez al mes
   - **Respuesta correcta:** A) Tras cada reunión o compromiso

**2.** Tarea «llamar» sin vínculo es:
   - A) Mala práctica
   - B) Ideal
   - C) Obligatorio
   - D) Recomendado
   - **Respuesta correcta:** A) Mala práctica

**3.** Bandeja al iniciar el día:
   - A) Triaje vencidas → hoy
   - B) Ignorar
   - C) Borrar todo
   - D) Solo urgentes
   - **Respuesta correcta:** A) Triaje vencidas → hoy

**4.** Completar tarea sin nota:
   - A) Pierdes contexto para el equipo
   - B) Perfecto
   - C) Obligatorio
   - D) Mejor
   - **Respuesta correcta:** A) Pierdes contexto para el equipo

**5.** ¿Ruta del módulo?
   - A) /Tasks
   - B) /Leads
   - C) /Audit
   - D) /revenue
   - **Respuesta correcta:** A) /Tasks

### 12. Certificación del módulo

Ejercicio QA + evaluación → badge **Task Master**.

---

## Módulo: RevenueOS

| | |
|---|---|
| **Ruta** | `/revenue` |
| **Rol** | gerente comercial |
| **Duración** | 50–70 min |
| **Empresa caso** | TechSolutions Panamá |

### 1. ¿Por qué existe?

**Problema empresarial:** Gerentes comerciales necesitan responder: ¿llegamos a la cuota?, ¿el pipeline alcanza?, ¿dónde está el riesgo? — sin esperar a fin de mes.

**Impacto económico:** Un forecast erróneo de 10 % en una cuota trimestral de $2 M puede desalinear contratación, marketing y bonus — impacto directo en cash.

**Qué pasa si no se usa:** Prometes al CEO un trimestre que no existe, descuidas deals grandes concentrados y el equipo quema en oportunidades falsas.

### 2. ¿Cuándo debo usarlo?

- Reunión semanal de pipeline con ventas.
- Preparar forecast para dirección.
- Evaluar si necesitas más pipeline (coverage).
- Identificar concentración en un solo deal.
- Explicar brecha cuota vs realidad en lenguaje simple.

### 3. Historia — TechSolutions Panamá

**Martes 16:00 — TechSolutions Panamá**

**Luis**, Gerente Comercial, abre `/revenue` antes del pipeline review. Ve:
- **Cuota Q2:** $1.2 M
- **Forecast commit:** $780 K (gap visible)
- **Pipeline coverage:** 2.1× (bajo el 3× objetivo)
- **Deal concentrado:** 45 % en Banco Regional
- **Riesgo:** 3 deals sin actividad >14 días

Traduce a su equipo en español llano: «Necesitamos $420 K más de commit real o pipeline nuevo; Banco Regional no puede ser la única apuesta». Acciones concretas en 30 min.

### 4. Recorrido paso a paso

#### PASO 1 — Abrir Revenue OS
- **Qué hacer:** **Revenue OS** (`/revenue`).
- **Qué esperar:** Dashboard con período seleccionable.
- **Qué validar:** Cuota y forecast visibles.

#### PASO 2 — Leer forecast en plain language
- **Qué hacer:** Commit / Best case / Pipeline — qué es cada capa.
- **Qué esperar:** Entiendes qué es «firme» vs «posible».
- **Qué validar:** Puedes explicarlo a un vendedor en 2 min.

#### PASO 3 — Coverage (cobertura)
- **Qué hacer:** Pipeline total ÷ cuota restante.
- **Qué esperar:** Ratio y semáforo.
- **Qué validar:** Sabes si necesitas más deals o acelerar cierre.

#### PASO 4 — Cuota y brecha
- **Qué hacer:** Realizado + forecast vs cuota.
- **Qué esperar:** Gap en $ y %.
- **Qué validar:** Plan de acción si gap >10 %.

#### PASO 5 — Riesgo y concentración
- **Qué hacer:** Deals grandes, inactivos, probabilidad inflada.
- **Qué esperar:** Top 5 riesgos listados.
- **Qué validar:** Cada riesgo con owner y fecha.

#### PASO 6 — Accionar con el equipo
- **Qué hacer:** Salir con 3 prioridades: acelerar, descartar, crear pipeline.
- **Qué esperar:** Tareas y deals actualizados.
- **Qué validar:** Próximo review con mismas métricas.

### 5. Lista de capturas (placeholders)

- **CAPTURA 01** — _Dashboard Revenue OS — período Q2_
- **CAPTURA 02** — _Panel Forecast — commit vs best case_
- **CAPTURA 03** — _Métrica Coverage con semáforo_
- **CAPTURA 04** — _Brecha vs cuota en $ y %_
- **CAPTURA 05** — _Lista deals en riesgo / concentración_
- **CAPTURA 06** — _Drill-down a deal desde Revenue OS_

### 6. Escenario completo Lead → Renewal

Leads → Deals actualizados → **Revenue OS semanal** → Acciones → Cierre → Cuota → Renewal ARR

### 7. 20 errores comunes

1. Mirar solo el total sin capas de forecast
2. Ignorar coverage bajo 2×
3. No cuestionar probabilidades infladas
4. Pipeline review sin Revenue OS abierto
5. Prometer cuota sin plan de brecha
6. Concentrar 50 %+ en un deal
7. No actuar sobre deals inactivos
8. Confundir commit con wishful thinking
9. No alinear definiciones con ventas
10. Revisar solo al fin de trimestre
11. Ignorar renewals en ARR
12. No separar new business vs expansión
13. Exportar sin validar datos
14. Castigar por forecast honesto
15. No documentar cambios de commit
16. Usar Revenue OS sin cruzar Deals
17. Olvidar descuentos pendientes de aprobación
18. No comunicar gap a dirección a tiempo
19. Métricas sin owner por acción
20. Celebrar pipeline gordo sin calidad

### 8. 20 buenas prácticas

1. Pipeline review semanal con Revenue OS proyectado
2. Explicar forecast en 3 capas al equipo
3. Coverage objetivo 3× — plan si <2.5×
4. Top 5 deals revisados uno a uno
5. Deals >14 días sin actividad = plan o Lost
6. Cuota = conversación de brecha, no sorpresa
7. Concentración >30 % = plan B documentado
8. Cruzar con Customer 360 en deals enterprise
9. Honestidad premiada en commit
10. Renewals en vista ARR mensual
11. Compartir captura en Slack post-review
12. University para nuevos managers
13. Mismo día: tareas de acción post-review
14. Trimestre nuevo = limpiar pipeline
15. Documentar supuestos del forecast
16. CEO recibe resumen en 5 bullets
17. Riesgo IA como hipótesis, no veredicto
18. Comparar QoQ para tendencia
19. Coaching 1:1 basado en gap individual
20. Certificación RevOps para managers

### 9. Checklist operativo

- ☐ Abrí Revenue OS del período correcto
- ☐ Entendí commit vs pipeline
- ☐ Calculé coverage y brecha
- ☐ Identifiqué top riesgos
- ☐ Salí con 3 acciones asignadas
- ☐ Agendé próximo review

### 10. Ejercicio práctico (entorno QA)

En QA: abrir `/revenue`, identificar cuota/forecast/coverage, listar 3 deals riesgo, simular nota de pipeline review.

> Entorno: [http://164.68.99.83:8091](http://164.68.99.83:8091)

### 11. Evaluación — mini examen

**1.** Coverage 2.1× con objetivo 3× significa:
   - A) Pipeline insuficiente para cuota
   - B) Sobrado
   - C) Perfecto
   - D) No aplica
   - **Respuesta correcta:** A) Pipeline insuficiente para cuota

**2.** Forecast «commit» es:
   - A) Lo que el equipo cree que cerrará
   - B) Todo el pipeline
   - C) Solo wishful thinking
   - D) Histórico
   - **Respuesta correcta:** A) Lo que el equipo cree que cerrará

**3.** 45 % en un solo deal es:
   - A) Riesgo de concentración
   - B) Ideal
   - C) Sin impacto
   - D) Mejor práctica
   - **Respuesta correcta:** A) Riesgo de concentración

**4.** ¿Ruta del módulo?
   - A) /revenue
   - B) /executive
   - C) /Leads
   - D) /Audit
   - **Respuesta correcta:** A) /revenue

**5.** Deal 14 días sin actividad en review:
   - A) Plan de acción o Lost
   - B) Ignorar
   - C) Subir probabilidad
   - D) Celebrar
   - **Respuesta correcta:** A) Plan de acción o Lost

**6.** Revenue OS es para:
   - A) Gerentes y RevOps
   - B) Solo IT
   - C) Solo CS
   - D) Auditoría
   - **Respuesta correcta:** A) Gerentes y RevOps

### 12. Certificación del módulo

Simulación pipeline review en QA + evaluación → badge **Revenue Captain**.

---

## Módulo: ExecutiveOS

| | |
|---|---|
| **Ruta** | `/executive` |
| **Rol** | CEO / dirección |
| **Duración** | 45–60 min |
| **Empresa caso** | TechSolutions Panamá |

### 1. ¿Por qué existe?

**Problema empresarial:** CEOs y dirección necesitan narrativa + cifras en minutos para juntas, inversionistas y decisiones de asignación de capital.

**Impacto económico:** Una junta mal preparada puede retrasar decisiones de hiring o producto semanas; o peor, basarse en números no validados.

**Qué pasa si no se usa:** Presentas solo buenas noticias, el board descubre el gap después y pierdes credibilidad.

### 2. ¿Cuándo debo usarlo?

- Junta directiva semanal o mensual.
- Conversación con inversionista o socio.
- Decisión de contratar o recortar.
- Crisis de cliente VIP o caída de ARR.
- Apertura de nuevo mercado o línea.

### 3. Historia — TechSolutions Panamá

**Jueves 6:30 AM — TechSolutions Panamá**

**Elena**, CEO, tiene junta a las 9:00. Abre `/executive`: ingresos YTD, churn, pipeline ejecutivo, outcomes de IA aprobadas, riesgos top 3.

En 12 minutos valida que el forecast de Luis coincide con Executive OS, detecta churn en segmento SMB y prepara 3 bullets: crecimiento, riesgo, decisión pedida al board. Exporta vista para la presentación.

### 4. Recorrido paso a paso

#### PASO 1 — Executive OS
- **Qué hacer:** **Executive OS** (`/executive`).
- **Qué esperar:** Vista consolidada C-level.
- **Qué validar:** KPIs principales cargados.

#### PASO 2 — Validar ingresos
- **Qué hacer:** ARR, MRR, new vs expansion, churn.
- **Qué esperar:** Cuadro coherente con finanzas.
- **Qué validar:** Diferencias <5 % o explicadas.

#### PASO 3 — Riesgos ejecutivos
- **Qué hacer:** Top cuentas, concentración, SLA, compliance.
- **Qué esperar:** Lista priorizada.
- **Qué validar:** Cada riesgo con owner.

#### PASO 4 — Outcomes y decisiones IA
- **Qué hacer:** Qué aprobó Trust Studio con impacto $.
- **Qué esperar:** Automatización bajo control.
- **Qué validar:** Sin sorpresas de IA no supervisada.

#### PASO 5 — Narrativa en 3 bullets
- **Qué hacer:** Crecimiento / Riesgo / Pedido al board.
- **Qué esperar:** Historia clara.
- **Qué validar:** Una decisión concreta solicitada.

#### PASO 6 — Export y junta
- **Qué hacer:** Exportar o compartir vista acordada.
- **Qué esperar:** Material listo.
- **Qué validar:** Post-junta: tareas a owners.

### 5. Lista de capturas (placeholders)

- **CAPTURA 01** — _Executive OS — vista principal KPIs_
- **CAPTURA 02** — _Panel ingresos y churn_
- **CAPTURA 03** — _Riesgos ejecutivos top 3_
- **CAPTURA 04** — _Outcomes IA / Trust Studio resumen_
- **CAPTURA 05** — _Export para junta_

### 6. Escenario completo Lead → Renewal

Operación diaria → Revenue OS → **Executive OS** → Decisión board → Asignación recursos → Resultado trimestre

### 7. 20 errores comunes

1. Export sin validar con finanzas
2. Solo métricas vanity sin churn
3. Ocultar pipeline débil
4. Junta sin pedido de decisión claro
5. Ignorar concentración de clientes
6. No leer riesgos antes de Q&A
7. Confiar en slide de hace 2 meses
8. No alinear con gerente comercial
9. Descartar señales SMB en churn
10. Presentar IA sin mencionar governance
11. No asignar follow-up post-junta
12. Mezclar datos de distintos períodos
13. Ignorar tickets P1 en cuentas clave
14. Narrativa sin contexto competitivo
15. No preparar respuesta a brecha de cuota
16. Usar Executive OS solo una vez al año
17. No documentar decisiones del board
18. Comparar sin ajuste estacional
19. Olvidar renewals en narrativa ARR
20. Subestimar tiempo de lectura previa

### 8. 20 buenas prácticas

1. Ritual viernes: Executive OS 15 min
2. 3 bullets: crecimiento, riesgo, pedido
3. Validar con CFO antes de board
4. Riesgo primero en la narrativa interna
5. Cruzar Executive con Revenue OS
6. Trust Studio en slide de governance IA
7. Post-junta: tareas con owner en 24 h
8. Churn por segmento, no solo total
9. Preparar Q&A con datos drill-down
10. Mismo formato cada mes — comparabilidad
11. University para nuevos directores
12. No más de 7 KPIs en pantalla principal
13. Celebrar wins con datos, no adjetivos
14. Decisión explícita solicitada al board
15. Export con fecha y versión
16. VIP risks en rojo siempre mencionados
17. Mentor CFO para primeras 3 juntas
18. Revisar outcomes IA mensual
19. Pipeline como leading, ARR como lagging
20. Certificación Executive User

### 9. Checklist operativo

- ☐ Validé KPIs con finanzas
- ☐ Identifiqué top 3 riesgos
- ☐ Preparé 3 bullets narrativos
- ☐ Pedido de decisión claro
- ☐ Export listo para junta
- ☐ Follow-ups asignados

### 10. Ejercicio práctico (entorno QA)

En QA: abrir `/executive`, preparar 3 bullets, identificar 1 riesgo y 1 métrica de churn, simular export.

> Entorno: [http://164.68.99.83:8091](http://164.68.99.83:8091)

### 11. Evaluación — mini examen

**1.** Executive OS es para:
   - A) CEO y dirección
   - B) Solo vendedores
   - C) Solo soporte
   - D) Auditoría
   - **Respuesta correcta:** A) CEO y dirección

**2.** Antes de junta debes:
   - A) Validar cifras con finanzas
   - B) Improvisar
   - C) Ocultar riesgos
   - D) No preparar
   - **Respuesta correcta:** A) Validar cifras con finanzas

**3.** Narrativa ejecutiva ideal:
   - A) 3 bullets: crecimiento, riesgo, pedido
   - B) 50 slides
   - C) Solo marketing
   - D) Sin datos
   - **Respuesta correcta:** A) 3 bullets: crecimiento, riesgo, pedido

**4.** ¿Ruta?
   - A) /executive
   - B) /revenue
   - C) /Leads
   - D) /TrustInbox
   - **Respuesta correcta:** A) /executive

**5.** Churn en SMB visible implica:
   - A) Revisar segmento y acción
   - B) Ignorar
   - C) Solo celebrar
   - D) Borrar dato
   - **Respuesta correcta:** A) Revisar segmento y acción

### 12. Certificación del módulo

Simulación junta en QA + evaluación → badge **Executive Analyst**.

---

## Módulo: TrustStudio

| | |
|---|---|
| **Ruta** | `/TrustInbox` |
| **Rol** | manager, admin, ventas |
| **Duración** | 45–55 min |
| **Empresa caso** | TechSolutions Panamá |

### 1. ¿Por qué existe?

**Problema empresarial:** La IA propone acciones (emails, descuentos, clasificaciones) que sin supervisión humana pueden dañar relaciones o violar políticas.

**Impacto económico:** Un email automático incorrecto a un CEO puede costar un deal de seis cifras; un descuento no autorizado erosiona margen.

**Qué pasa si no se usa:** Aprobas sin leer y el cliente recibe spam; rechazas todo y la IA no aprende; dejas cola >24 h y pierdes velocidad.

### 2. ¿Cuándo debo usarlo?

- Llega alerta de decisión IA pendiente.
- IA sugiere respuesta a ticket o lead.
- Propuesta de descuento o excepción.
- Clasificación automática de prioridad.
- Revisión de compliance antes de envío masivo.

### 3. Historia — TechSolutions Panamá

**Miércoles 11:20 — TechSolutions Panamá**

**Patricia**, Sales Manager, recibe en `/TrustInbox` una propuesta de IA: enviar seguimiento a **Ministerio de Economía** con tono informal y 15 % descuento no aprobado.

Lee contexto, historial en 360, política de descuentos. **Rechaza** con feedback «tono formal sector público; descuento requiere VP». Aprueba otra: clasificar ticket de **RetailMax** como P2 con respuesta plantilla correcta. 8 minutos, cero daño reputacional.

### 4. Recorrido paso a paso

#### PASO 1 — Abrir Trust Studio
- **Qué hacer:** **Trust Inbox** (`/TrustInbox`).
- **Qué esperar:** Cola de decisiones pendientes.
- **Qué validar:** Contador visible; nada >24 h.

#### PASO 2 — Leer contexto completo
- **Qué hacer:** Registro vinculado, historial, propuesta IA.
- **Qué esperar:** Entiendes qué haría el sistema.
- **Qué validar:** Cliente, valor y política revisados.

#### PASO 3 — Validar contra políticas
- **Qué hacer:** Descuentos, tono, datos sensibles.
- **Qué esperar:** Cumple o no.
- **Qué validar:** Si duda → rechazar o escalar.

#### PASO 4 — Aprobar o rechazar
- **Qué hacer:** Un clic + comentario obligatorio si rechazas.
- **Qué esperar:** Decisión auditada.
- **Qué validar:** Feedback útil para mejorar IA.

#### PASO 5 — Seguimiento
- **Qué hacer:** Verificar que la acción ejecutada es correcta.
- **Qué esperar:** Timeline actualizado.
- **Qué validar:** Cliente no nota «robot sin cerebro».

#### PASO 6 — Ritual de equipo
- **Qué hacer:** Turnos de revisión; métricas de cola.
- **Qué esperar:** SLA de Trust cumplido.
- **Qué validar:** Política documentada en University.

### 5. Lista de capturas (placeholders)

- **CAPTURA 01** — _Trust Inbox — cola pendientes_
- **CAPTURA 02** — _Detalle propuesta IA con contexto_
- **CAPTURA 03** — _Comparación con política de descuentos_
- **CAPTURA 04** — _Aprobar con confirmación_
- **CAPTURA 05** — _Rechazar con feedback estructurado_
- **CAPTURA 06** — _Historial de decisiones auditadas_

### 6. Escenario completo Lead → Renewal

IA propone → **Trust Studio** → Humano decide → Acción ejecutada → Audit registra → Cliente impactado → Renewal

### 7. 20 errores comunes

1. Aprobar sin leer el texto propuesto
2. Rechazar todo por miedo
3. Dejar cola >24 horas
4. Aprobar descuento sin política
5. No dar feedback al rechazar
6. Ignorar contexto del 360
7. Turno de revisión sin backup
8. Aprobar en móvil sin pantalla completa
9. No escalar excepciones VIP
10. Mezclar pruebas y producción
11. Confiar ciegamente en clasificación P1/P2
12. No documentar patrón de rechazos
13. Delegar sin capacitar en Trust
14. Aprobar email con datos incorrectos
15. Ignorar auditoría post-decisión
16. No alinear con Legal en sector regulado
17. Aprobar masivo sin muestra
18. Rechazar sin alternativa humana
19. No medir tiempo de cola
20. Desactivar IA en lugar de gobernar

### 8. 20 buenas prácticas

1. Leer propuesta completa siempre
2. Rechazo con feedback específico
3. Cola cero >24 h — turnos definidos
4. Política de descuentos a mano
5. 360 abierto en segunda pantalla
6. Aprobar tono adecuado al sector
7. Escalar VIP a manager
8. Métricas semanales: aprobado/rechazado/tiempo
9. Capacitar en University antes de turno
10. Human-in-the-loop como ventaja, no freno
11. Patrones de rechazo → mejora políticas
12. Sample de 10 % en envíos masivos
13. Alternativa humana si rechazas urgente
14. Audit trail como aliado
15. No aprobar datos PII incorrectos
16. Trust + Agents supervisados juntos
17. Celebrar buen rechazo que evitó crisis
18. Documentar excepciones aprobadas
19. Revisión Legal trimestral
20. Badge Trust Specialist

### 9. Checklist operativo

- ☐ Revisé cola pendientes
- ☐ Leí contexto y política
- ☐ Decidí con comentario si rechacé
- ☐ Verifiqué ejecución
- ☐ Cola <24 h

### 10. Ejercicio práctico (entorno QA)

En QA: abrir `/TrustInbox`, revisar 2 propuestas simuladas, aprobar 1 y rechazar 1 con feedback.

> Entorno: [http://164.68.99.83:8091](http://164.68.99.83:8091)

### 11. Evaluación — mini examen

**1.** Trust Studio sirve para:
   - A) Aprobar decisiones IA con humano
   - B) Crear usuarios
   - C) Facturar
   - D) Borrar leads
   - **Respuesta correcta:** A) Aprobar decisiones IA con humano

**2.** Rechazar sin comentario:
   - A) Pierdes mejora de IA
   - B) Ideal
   - C) Obligatorio
   - D) Mejor
   - **Respuesta correcta:** A) Pierdes mejora de IA

**3.** Cola >24 h genera:
   - A) Riesgo operativo y cliente
   - B) Nada
   - C) Más ventas
   - D) Menos trabajo
   - **Respuesta correcta:** A) Riesgo operativo y cliente

**4.** ¿Ruta?
   - A) /TrustInbox
   - B) /Agents
   - C) /Audit
   - D) /Users
   - **Respuesta correcta:** A) /TrustInbox

**5.** Descuento IA sin política:
   - A) Rechazar o escalar
   - B) Aprobar siempre
   - C) Ignorar
   - D) Borrar cliente
   - **Respuesta correcta:** A) Rechazar o escalar

### 12. Certificación del módulo

Ejercicio Trust en QA + evaluación → badge **Trust Guardian**.

---

## Módulo: CustomerSuccess

| | |
|---|---|
| **Ruta** | `/customer-success` |
| **Rol** | soporte y CS |
| **Duración** | 55–70 min |
| **Empresa caso** | TechSolutions Panamá |

### 1. ¿Por qué existe?

**Problema empresarial:** Post-venta sin sistema unificado: tickets, salud, renovaciones y escalaciones viven en silos — el churn sorprende.

**Impacto económico:** Subir retención 5 % puede aumentar beneficios 25–95 % (Bain); perder una cuenta $80 K ARR por mala renovación duele un trimestre.

**Qué pasa si no se usa:** Tickets SLA rotos, renovación sorpresa a 30 días, escalación sin contexto y cliente que «ya avisó» en tres tickets distintos.

### 2. ¿Cuándo debo usarlo?

- Cola de tickets nuevos o escalados.
- Cuenta en health rojo o ámbar.
- Ventana de renovación 90/60/30 días.
- Playbook de churn prevention.
- Escalación a manager o ejecutivo.

### 3. Historia — TechSolutions Panamá

**Martes 10:00 — TechSolutions Panamá**

**Ana**, CS Lead, ve alerta: **Hotel Plaza Pacífico** health **Rojo**, NPS 4, ticket P2 abierto 36 h, renewal en **58 días**, uso -40 %.

Abre `/customer-success`, ejecuta playbook **Recuperación**, abre 360, agenda QBR de emergencia, escala a Roberto (manager) con resumen de revenue en riesgo $65 K ARR. Plan 30 días documentado.

### 4. Recorrido paso a paso

#### PASO 1 — CS OS
- **Qué hacer:** **Customer Success** (`/customer-success`).
- **Qué esperar:** Cola tickets, health, renewals.
- **Qué validar:** Prioridad clara.

#### PASO 2 — Clasificar ticket
- **Qué hacer:** Severidad, SLA, playbook correcto.
- **Qué esperar:** Ticket en cola adecuada.
- **Qué validar:** P1 <1 h, P2 <8 h según política.

#### PASO 3 — Health y churn signals
- **Qué hacer:** Score, tendencia, uso, pagos.
- **Qué esperar:** Razón del rojo identificada.
- **Qué validar:** Acción en 24 h si rojo.

#### PASO 4 — Renewal tracker
- **Qué hacer:** Cuentas en ventana 90 días.
- **Qué esperar:** Owner y playbook asignado.
- **Qué validar:** Ninguna renewal sin plan.

#### PASO 5 — Playbook
- **Qué hacer:** Ejecutar pasos: contacto, valor, plan.
- **Qué esperar:** Checklist playbook completo.
- **Qué validar:** Cliente confirma próximo paso.

#### PASO 6 — Escalación
- **Qué hacer:** Si VIP o revenue >umbral → manager + nota 360.
- **Qué esperar:** War room si aplica.
- **Qué validar:** Seguimiento cada 24 h.

#### PASO 7 — Cierre y CSAT
- **Qué hacer:** Confirmar resolución con cliente.
- **Qué esperar:** Ticket cerrado con causa raíz.
- **Qué validar:** CSAT o NPS follow-up.

### 5. Lista de capturas (placeholders)

- **CAPTURA 01** — _Customer Success — cola principal_
- **CAPTURA 02** — _Ticket clasificado con severidad_
- **CAPTURA 03** — _Panel health — Hotel Plaza Pacífico_
- **CAPTURA 04** — _Renewal tracker 90 días_
- **CAPTURA 05** — _Playbook Recuperación en ejecución_
- **CAPTURA 06** — _Escalación con resumen revenue_

### 6. Escenario completo Lead → Renewal

Won → Onboarding → Uso → **Tickets + Health** → Renewal playbook → QBR → Renew o Recover → Expansion

### 7. 20 errores comunes

1. Cerrar ticket sin confirmar con cliente
2. Romper SLA sin escalar
3. Ignorar health rojo
4. Renewal a 30 días sin contacto
5. Playbook equivocado
6. No documentar causa raíz
7. Escalar sin datos de 360
8. Prometer fecha sin validar ingeniería
9. Mezclar P1 y P3 en prioridad
10. No involucrar ventas en expansión
11. Churn surprise sin post-mortem
12. CSAT solo en clientes felices
13. Dejar ticket abierto «por si acaso»
14. No registrar llamada en timeline
15. Renovación solo con descuento
16. Ignorar NPS detractor
17. War room sin owner
18. No medir tiempo a resolución
19. Handoff implementación incompleto
20. Olvidar playbook tras cerrar ticket

### 8. 20 buenas prácticas

1. SLA visible en cada ticket
2. Health rojo = acción en 24 h
3. Renewal 90 días inicia playbook
4. Playbook correcto por tipo
5. Causa raíz en todo P1/P2
6. 360 antes de escalación
7. QBR con métricas de valor
8. Confirmación cliente antes de cerrar
9. NPS detractor en 48 h
10. Post-mortem en churn
11. Coordinar con ventas en expansión
12. War room VIP documentada
13. Nota en 360 cada touch
14. University playbooks obligatorios
15. Cola revisada 3× al día
16. Renovación sin solo descuento
17. Seguimiento escalación 24 h
18. CSAT muestra representativa
19. Celebrar renewal en equipo
20. Mentor en primer P1 real

### 9. Checklist operativo

- ☐ Cola priorizada
- ☐ SLA cumplido o escalado
- ☐ Health revisado
- ☐ Renewals en ventana con plan
- ☐ Playbook documentado
- ☐ Cliente confirmó cierre

### 10. Ejercicio práctico (entorno QA)

En QA: abrir `/customer-success`, clasificar ticket simulado, revisar health, iniciar playbook renewal 90 días.

> Entorno: [http://164.68.99.83:8091](http://164.68.99.83:8091)

### 11. Evaluación — mini examen

**1.** Renewal típica se anticipa a:
   - A) 90 días
   - B) 1 día
   - C) 2 años
   - D) Nunca
   - **Respuesta correcta:** A) 90 días

**2.** Health rojo implica:
   - A) Acción en 24 h
   - B) Ignorar
   - C) Solo email
   - D) Cerrar cuenta
   - **Respuesta correcta:** A) Acción en 24 h

**3.** Cerrar ticket sin confirmar cliente:
   - A) Mala práctica
   - B) Ideal
   - C) SLA
   - D) Obligatorio
   - **Respuesta correcta:** A) Mala práctica

**4.** ¿Ruta CS?
   - A) /customer-success
   - B) /Leads
   - C) /revenue
   - D) /Policies
   - **Respuesta correcta:** A) /customer-success

**5.** Playbook Recuperación se usa cuando:
   - A) Churn risk / health bajo
   - B) Nuevo lead
   - C) Crear usuario
   - D) Audit
   - **Respuesta correcta:** A) Churn risk / health bajo

**6.** Escalación VIP requiere:
   - A) Contexto 360 + revenue
   - B) Solo nombre
   - C) Nada
   - D) Borrar ticket
   - **Respuesta correcta:** A) Contexto 360 + revenue

### 12. Certificación del módulo

Simulación ticket + renewal en QA + evaluación → badge **CS Hero**.

---

## Módulo: Agents

| | |
|---|---|
| **Ruta** | `/Agents` |
| **Rol** | admin y managers |
| **Duración** | 40–50 min |
| **Empresa caso** | TechSolutions Panamá |

### 1. ¿Por qué existe?

**Problema empresarial:** Trabajo repetitivo (clasificar, resumir, recordar) consume tiempo; sin agentes IA el equipo no escala.

**Impacto económico:** Automatizar 2 h/día por SDR son ~500 h/año — o riesgo si se hace sin supervisión.

**Qué pasa si no se usa:** Activas agentes sin política, no supervisas, o los apagas por un error y pierdes productividad.

### 2. ¿Cuándo debo usarlo?

- Clasificación automática de leads/tickets.
- Resúmenes pre-llamada.
- Recordatorios de seguimiento.
- Supervisar workforce de IA.
- Ajustar tras feedback de Trust Studio.

### 3. Historia — TechSolutions Panamá

**Viernes 15:00 — TechSolutions Panamá**

**Diego**, Admin, revisa `/Agents`: agente **Lead Qualifier** procesó 47 leads; 3 en Trust Studio pendientes; agente **Ticket Triage** redujo tiempo primera respuesta 22 %.

Ajusta umbral de confianza, desactiva temporalmente agente de emails en sector público y programa capacitación Trust para ventas. Automatización con control.

### 4. Recorrido paso a paso

#### PASO 1 — Workforce
- **Qué hacer:** **Agents** (`/Agents`).
- **Qué esperar:** Lista agentes activos y métricas.
- **Qué validar:** Estado de cada agente visible.

#### PASO 2 — Revisar actividad
- **Qué hacer:** Volúmenes, errores, cola Trust.
- **Qué esperar:** Sin sorpresas.
- **Qué validar:** Anomalías investigadas.

#### PASO 3 — Supervisar decisiones
- **Qué hacer:** Cruzar con Trust Inbox.
- **Qué esperar:** Human-in-the-loop activo.
- **Qué validar:** Nada crítico sin aprobación.

#### PASO 4 — Ajustar configuración
- **Qué hacer:** Umbrales, alcance, horarios.
- **Qué esperar:** Cambio documentado.
- **Qué validar:** Política alineada.

#### PASO 5 — Medir impacto
- **Qué hacer:** Tiempo ahorrado, calidad, SLA.
- **Qué esperar:** ROI narrativo.
- **Qué validar:** Compartir con dirección.

#### PASO 6 — Capacitar usuarios
- **Qué hacer:** University + política clara.
- **Qué esperar:** Equipo confía en el sistema.
- **Qué validar:** Feedback loop activo.

### 5. Lista de capturas (placeholders)

- **CAPTURA 01** — _Panel Agents — workforce overview_
- **CAPTURA 02** — _Detalle agente Lead Qualifier_
- **CAPTURA 03** — _Métricas volumen y errores_
- **CAPTURA 04** — _Vínculo a Trust Studio pendientes_
- **CAPTURA 05** — _Configuración umbral confianza_

### 6. Escenario completo Lead → Renewal

Proceso manual → **Agente activo** → Trust Studio → Acción → Métrica → Ajuste → Escala

### 7. 20 errores comunes

1. Activar agente sin política
2. No supervisar primera semana
3. Desactivar todo tras un error
4. Automatizar decisiones de $ sin Trust
5. No medir impacto
6. Agentes en horario sin backup humano
7. Ignorar feedback de rechazos Trust
8. Múltiples agentes en mismo flujo sin coordinar
9. No documentar cambios de config
10. Probar en producción sin QA
11. Confiar 100 % en clasificación
12. No capacitar usuarios afectados
13. Agente con acceso más allá del necesario
14. Olvidar sector regulado
15. No tener owner del agente
16. Métricas solo de volumen, no calidad
17. Ignorar latencia de cola
18. No plan de rollback
19. Mezclar entornos
20. IA como sustituto de proceso roto

### 8. 20 buenas prácticas

1. Empezar con un agente simple
2. Trust Studio obligatorio en acciones externas
3. Owner por agente
4. Revisión semanal de métricas
5. QA antes de producción
6. Documentar config y cambios
7. Capacitación University
8. Medir tiempo ahorrado y calidad
9. Feedback de rechazos → ajuste
10. Rollback plan documentado
11. Human-in-the-loop como default
12. No automatizar proceso no definido
13. Sector público: reglas estrictas
14. Celebrar win de productividad
15. Comunicar cambios al equipo
16. Cruzar Agents + Audit mensual
17. Umbrales conservadores al inicio
18. Expandir tras 30 días estables
19. Mentor admin para primer agente
20. Badge AI Power User

### 9. Checklist operativo

- ☐ Revisé agentes activos
- ☐ Cola Trust sin críticos
- ☐ Métricas entendidas
- ☐ Config documentada
- ☐ Equipo capacitado

### 10. Ejercicio práctico (entorno QA)

En QA: abrir `/Agents`, revisar estado, simular ajuste umbral, vincular con Trust Inbox.

> Entorno: [http://164.68.99.83:8091](http://164.68.99.83:8091)

### 11. Evaluación — mini examen

**1.** Agents requieren supervisión vía:
   - A) Trust Studio
   - B) Solo email
   - C) Nada
   - D) Borrar datos
   - **Respuesta correcta:** A) Trust Studio

**2.** Primer paso al desplegar agente:
   - A) Política + QA
   - B) Producción directo
   - C) Ignorar
   - D) Desactivar CRM
   - **Respuesta correcta:** A) Política + QA

**3.** Human-in-the-loop significa:
   - A) Humano aprueba acciones críticas
   - B) Sin humanos
   - C) Solo bots
   - D) Audit off
   - **Respuesta correcta:** A) Humano aprueba acciones críticas

**4.** ¿Ruta?
   - A) /Agents
   - B) /Users
   - C) /Leads
   - D) /executive
   - **Respuesta correcta:** A) /Agents

**5.** Automatizar proceso roto:
   - A) Arreglar proceso primero
   - B) Ideal
   - C) Obligatorio
   - D) Más rápido
   - **Respuesta correcta:** A) Arreglar proceso primero

### 12. Certificación del módulo

Revisión workforce QA + evaluación → badge **AI Operator**.

---

## Módulo: Users

| | |
|---|---|
| **Ruta** | `/Users` |
| **Rol** | administrador |
| **Duración** | 45–55 min |
| **Empresa caso** | TechSolutions Panamá |

### 1. ¿Por qué existe?

**Problema empresarial:** Accesos incorrectos = fugas de datos, usuarios fantasma y gente que no puede trabajar el día 1.

**Impacto económico:** Un admin de más en sistema regulado puede costar multas; un vendedor sin acceso pierde deals el primer día.

**Qué pasa si no se usa:** Das admin a todos, no haces offboarding, o creas usuarios duplicados.

### 2. ¿Cuándo debo usarlo?

- Alta de empleado nuevo.
- Cambio de rol o promoción.
- Offboarding el mismo día de salida.
- Auditoría de accesos trimestral.
- Reset de acceso por seguridad.

### 3. Historia — TechSolutions Panamá

**Lunes 9:00 — TechSolutions Panamá**

**Sofía**, Admin, da de alta a **Carlos** (vendedor): rol Sales, sin admin, University asignado, email corporativo. Desactiva a ex-empleado **Miguel** que salió viernes — mismo día, sesiones cerradas.

Checklist: mínimo privilegio, capacitación obligatoria, auditoría limpia.

### 4. Recorrido paso a paso

#### PASO 1 — Users
- **Qué hacer:** **Users** (`/Users`).
- **Qué esperar:** Lista usuarios activos/inactivos.
- **Qué validar:** Sin cuentas sin owner.

#### PASO 2 — Crear usuario
- **Qué hacer:** Nombre, email, rol mínimo necesario.
- **Qué esperar:** Invitación enviada.
- **Qué validar:** Rol correcto, no Admin por defecto.

#### PASO 3 — Asignar rol
- **Qué hacer:** Sales / CS / Manager / Viewer / Admin.
- **Qué esperar:** Permisos acordes.
- **Qué validar:** Principio mínimo privilegio.

#### PASO 4 — University
- **Qué hacer:** Asignar ruta de aprendizaje del rol.
- **Qué esperar:** Progreso visible.
- **Qué validar:** No producción sin Fundamentos.

#### PASO 5 — Comunicar
- **Qué hacer:** Credenciales y primera tarea al manager.
- **Qué esperar:** Usuario login día 1.
- **Qué validar:** Manager confirma acceso OK.

#### PASO 6 — Offboarding
- **Qué hacer:** Desactivar mismo día; revisar Audit.
- **Qué esperar:** Sin acceso post-salida.
- **Qué validar:** Sesiones terminadas.

### 5. Lista de capturas (placeholders)

- **CAPTURA 01** — _Lista Users con roles_
- **CAPTURA 02** — _Formulario alta (`/Users`)_
- **CAPTURA 03** — _Selector de rol Sales_
- **CAPTURA 04** — _Usuario desactivado — offboarding_
- **CAPTURA 05** — _University asignado al usuario_

### 6. Escenario completo Lead → Renewal

Contratación → **User creado** → University → Operación → Cambio rol → Offboarding → Audit

### 7. 20 errores comunes

1. Admin para todos los nuevos
2. No desactivar el día de salida
3. Usuarios fantasma sin uso 90 días
4. Email personal en producción
5. Compartir contraseñas
6. Rol incorrecto por prisa
7. No asignar University
8. Duplicar usuario mismo email
9. Olvidar reasignar registros del que sale
10. No revisar Audit post-offboarding
11. Permisos acumulados sin limpiar
12. Crear usuario sin manager owner
13. No documentar excepciones de acceso
14. Ignorar MFA si está disponible
15. Alta masiva sin checklist
16. No capacitar en seguridad
17. Viewer con datos sensibles sin necesidad
18. No auditar admins trimestral
19. Reactivar sin justificación
20. Mezclar cuentas de prueba y reales

### 8. 20 buenas prácticas

1. Mínimo privilegio siempre
2. Offboarding día 0 de salida
3. University antes de producción
4. Revisión trimestral de accesos
5. Rol = función real del puesto
6. Audit tras cada offboarding
7. MFA donde aplique
8. Documentar excepciones temporales
9. Reasignar pipeline al salir vendedor
10. Checklist alta en onboarding IT
11. Sin admin por comodidad
12. Email corporativo único
13. Manager valida primer login
14. Desactivar, no borrar, con historial
15. Capacitación seguridad anual
16. Lista admins <5 personas
17. Comunicar cambios de rol
18. Probar acceso con usuario nuevo
19. Celebrar adopción University 100 %
20. Certificación Administrator

### 9. Checklist operativo

- ☐ Rol mínimo asignado
- ☐ University en ruta del rol
- ☐ Manager notificado
- ☐ Sin admins innecesarios
- ☐ Offboarding mismo día si aplica
- ☐ Audit revisado

### 10. Ejercicio práctico (entorno QA)

En QA: simular alta usuario «Academy Test», rol Viewer, asignar University, simular desactivación.

> Entorno: [http://164.68.99.83:8091](http://164.68.99.83:8091)

### 11. Evaluación — mini examen

**1.** Principio de acceso:
   - A) Mínimo privilegio
   - B) Admin para todos
   - C) Sin roles
   - D) Compartir login
   - **Respuesta correcta:** A) Mínimo privilegio

**2.** Offboarding debe ser:
   - A) Mismo día de salida
   - B) Nunca
   - C) Un año después
   - D) Opcional
   - **Respuesta correcta:** A) Mismo día de salida

**3.** Usuario nuevo sin University:
   - A) No debería operar en producción
   - B) Ideal
   - C) Obligatorio
   - D) Mejor
   - **Respuesta correcta:** A) No debería operar en producción

**4.** ¿Ruta?
   - A) /Users
   - B) /Audit
   - C) /Leads
   - D) /Agents
   - **Respuesta correcta:** A) /Users

**5.** Admin por comodidad:
   - A) Riesgo de seguridad
   - B) Buena práctica
   - C) Requerido
   - D) Sin impacto
   - **Respuesta correcta:** A) Riesgo de seguridad

### 12. Certificación del módulo

Alta/offboarding simulado QA + evaluación → badge **Access Steward**.

---

## Módulo: Policies

| | |
|---|---|
| **Ruta** | `/Policies` |
| **Rol** | administrador |
| **Duración** | 40–50 min |
| **Empresa caso** | TechSolutions Panamá |

### 1. ¿Por qué existe?

**Problema empresarial:** Descuentos, accesos y excepciones «de palabra» generan inconsistencia, fraude interno y clientes que comparan tratos.

**Impacto económico:** Un 5 % de descuento no autorizado en $200 K de deals erosiona $10 K de margen; en regulados, puede ser multa.

**Qué pasa si no se usa:** Cada vendedor inventa su regla, Legal no sabe qué se prometió y Audit no puede defender nada.

### 2. ¿Cuándo debo usarlo?

- Definir quién aprueba descuentos.
- Restringir datos por territorio o rol.
- Nueva regla post-auditoría.
- Alinear ventas con Legal.
- Comunicar cambio de política al equipo.

### 3. Historia — TechSolutions Panamá

**Jueves 14:30 — TechSolutions Panamá**

**Ricardo**, Admin, publica política **Descuento máximo 10 % sin VP** y **Acceso PII solo CS+Manager**. Ventas recibe aviso en University; Trust Studio alinea rechazos automáticos.

María intenta 15 % — sistema bloquea; escala a VP con justificación en deal. Consistencia sin fricción innecesaria.

### 4. Recorrido paso a paso

#### PASO 1 — Policies
- **Qué hacer:** **Policies** (`/Policies`).
- **Qué esperar:** Lista reglas activas.
- **Qué validar:** Dueño por política.

#### PASO 2 — Revisar vigentes
- **Qué hacer:** Descuentos, PII, territorios.
- **Qué esperar:** Sin contradicciones.
- **Qué validar:** Alineado con Legal.

#### PASO 3 — Crear o editar
- **Qué hacer:** Regla clara, alcance, excepciones.
- **Qué esperar:** Borrador → revisión.
- **Qué validar:** Change log actualizado.

#### PASO 4 — Probar
- **Qué hacer:** Usuario de prueba en QA.
- **Qué esperar:** Bloqueo/permitir correcto.
- **Qué validar:** Sin falsos positivos masivos.

#### PASO 5 — Publicar y comunicar
- **Qué hacer:** University + email equipo.
- **Qué esperar:** Todos enterados.
- **Qué validar:** Fecha efectiva clara.

#### PASO 6 — Auditar cumplimiento
- **Qué hacer:** Cruzar con Audit y Trust.
- **Qué esperar:** Excepciones documentadas.
- **Qué validar:** Revisión trimestral.

### 5. Lista de capturas (placeholders)

- **CAPTURA 01** — _Lista Policies activas_
- **CAPTURA 02** — _Editor regla descuento_
- **CAPTURA 03** — _Prueba bloqueo 15 % descuento_
- **CAPTURA 04** — _Change log de política_
- **CAPTURA 05** — _Aviso en University_

### 6. Escenario completo Lead → Renewal

Necesidad negocio → **Política definida** → Prueba → Comunicación → Operación → Audit → Ajuste

### 7. 20 errores comunes

1. Política sin dueño
2. Publicar sin probar
3. No comunicar cambios
4. Excepciones solo por chat
5. Reglas contradictorias
6. Copiar política de otro tenant sin adaptar
7. Ignorar feedback de ventas
8. Política demasiado laxa en PII
9. No revisar tras incidente
10. Desactivar política sin reemplazo
11. Legal no involucrado en regulados
12. Change log vacío
13. Probar solo en producción
14. No alinear Trust Studio
15. Política que nadie entiende
16. Excepciones sin expiración
17. No capacitar en University
18. Ignorar intentos denegados en Audit
19. Políticas huérfanas de proceso
20. Revisión anual cuando necesitas trimestral

### 8. 20 buenas prácticas

1. Dueño nombrado por política
2. Probar en QA antes de publicar
3. Comunicar 48 h antes si es restrictiva
4. Change log obligatorio
5. Legal en sector regulado
6. Excepciones con ticket y expiración
7. Alinear Trust y Policies
8. Revisión trimestral calendario
9. Lenguaje plain Spanish en reglas
10. University actualizada mismo día
11. Auditar intentos denegados
12. Métricas de excepciones
13. Sin «regla de pasillo»
14. Simular vendedor y CS al probar
15. Rollback plan si falla
16. Celebrar menos excepciones = más margen
17. Documentar racional de negocio
18. Coordinar con Revenue en descuentos
19. Post-incidente = revisión política
20. Certificación Admin incluye Policies

### 9. Checklist operativo

- ☐ Políticas revisadas
- ☐ Cambios en change log
- ☐ Prueba QA OK
- ☐ Equipo comunicado
- ☐ Trust alineado
- ☐ Audit sin anomalías críticas

### 10. Ejercicio práctico (entorno QA)

En QA: revisar `/Policies`, simular regla descuento, verificar bloqueo en deal de prueba.

> Entorno: [http://164.68.99.83:8091](http://164.68.99.83:8091)

### 11. Evaluación — mini examen

**1.** Policies sirven para:
   - A) Reglas de negocio consistentes
   - B) Diseño UI
   - C) Email marketing
   - D) Borrar leads
   - **Respuesta correcta:** A) Reglas de negocio consistentes

**2.** Publicar sin probar es:
   - A) Riesgo operativo
   - B) Ideal
   - C) Obligatorio
   - D) Mejor
   - **Respuesta correcta:** A) Riesgo operativo

**3.** Excepción informal:
   - A) Erosiona margen y compliance
   - B) Buena práctica
   - C) Requerida
   - D) Sin impacto
   - **Respuesta correcta:** A) Erosiona margen y compliance

**4.** ¿Ruta?
   - A) /Policies
   - B) /Users
   - C) /Leads
   - D) /revenue
   - **Respuesta correcta:** A) /Policies

**5.** Change log:
   - A) Trazabilidad de cambios
   - B) Opcional
   - C) Secreto
   - D) Solo IT
   - **Respuesta correcta:** A) Trazabilidad de cambios

### 12. Certificación del módulo

Prueba política en QA + evaluación → badge **Policy Architect**.

---

## Módulo: Audit

| | |
|---|---|
| **Ruta** | `/Audit` |
| **Rol** | admin y compliance |
| **Duración** | 35–45 min |
| **Empresa caso** | TechSolutions Panamá |

### 1. ¿Por qué existe?

**Problema empresarial:** Sin trazabilidad no puedes responder «quién cambió qué» en auditoría, disputa legal o incidente de seguridad.

**Impacto económico:** Multas GDPR/CCPA o pérdida de contrato enterprise por falta de evidencia pueden superar millones.

**Qué pasa si no se usa:** No investigas intentos denegados, no exportas a compliance y descubres el problema cuando ya es crisis.

### 2. ¿Cuándo debo usarlo?

- Investigar acceso sospechoso.
- Preparar informe para compliance.
- Post-incidente de seguridad.
- Revisión semanal admin.
- Validar offboarding correcto.

### 3. Historia — TechSolutions Panamá

**Viernes 8:00 — TechSolutions Panamá**

**Laura**, Compliance, filtra `/Audit` por intentos **denegados** y usuario **Miguel** (desactivado lunes). Detecta 3 intentos martes — IP externa. Confirma offboarding, alerta CISO, exporta evidencia para archivo. Crisis evitada en 20 minutos.

### 4. Recorrido paso a paso

#### PASO 1 — Audit
- **Qué hacer:** **Audit** (`/Audit`).
- **Qué esperar:** Log de eventos con filtros.
- **Qué validar:** Retención según política.

#### PASO 2 — Filtrar evento
- **Qué hacer:** Usuario, acción, fecha, resultado.
- **Qué esperar:** Subconjunto relevante.
- **Qué validar:** Filtros documentados en ticket.

#### PASO 3 — Investigar anomalía
- **Qué hacer:** Denegados, cambios masivos, horarios raros.
- **Qué esperar:** Hipótesis clara.
- **Qué validar:** Timeline reconstruido.

#### PASO 4 — Correlacionar
- **Qué hacer:** Users, Policies, registro afectado.
- **Qué esperar:** Causa raíz.
- **Qué validar:** Acción correctiva asignada.

#### PASO 5 — Export / reportar
- **Qué hacer:** Evidencia para Legal o cliente enterprise.
- **Qué esperar:** Formato acordado.
- **Qué validar:** Cadena de custodia.

#### PASO 6 — Cierre y prevención
- **Qué hacer:** Política o acceso ajustado.
- **Qué esperar:** Incidente cerrado.
- **Qué validar:** Revisión semanal programada.

### 5. Lista de capturas (placeholders)

- **CAPTURA 01** — _Audit log — vista principal_
- **CAPTURA 02** — _Filtro intentos denegados_
- **CAPTURA 03** — _Detalle evento usuario Miguel_
- **CAPTURA 04** — _Export evidencia compliance_
- **CAPTURA 05** — _Correlación con Users desactivado_

### 6. Escenario completo Lead → Renewal

Acción usuario → **Audit registra** → Revisión → Investigación → Corrección → Policy/User update

### 7. 20 errores comunes

1. Nunca revisar Audit
2. Ignorar intentos denegados
3. Export sin autorización
4. Borrar logs manualmente
5. No correlacionar con Users
6. Investigar sin ticket
7. Retención menor a requerimiento contractual
8. No alertar en offboarding fallido
9. Asumir denegado = error benigno
10. No documentar hallazgos
11. Compartir export por email inseguro
12. Revisión solo anual
13. Ignorar cambios masivos nocturnos
14. No capacitar admins en Audit
15. Mezclar entornos en análisis
16. Cerrar incidente sin causa raíz
17. No alinear con Policies
18. Olvidar viewer role en alertas
19. Export sin fecha/hora UTC
20. No probar restauración de evidencia

### 8. 20 buenas prácticas

1. Revisión semanal admin — calendario
2. Intentos denegados siempre investigados
3. Export solo canal seguro
4. Ticket por investigación
5. Correlacionar Users + Policies
6. Retención según contrato
7. Alerta offboarding fallido
8. University para nuevos admins
9. UTC en exports
10. Post-incidente en 24 h
11. Muestra mensual a compliance
12. No borrar — inmutabilidad
13. Cadena de custodia documentada
14. Simulacro trimestral
15. Viewer alertas configuradas
16. Causa raíz obligatoria
17. Celebrar detección temprana
18. Integrar con Executive en crisis
19. Métricas: denegados, cambios rol
20. Badge SuperAdmin Elite path

### 9. Checklist operativo

- ☐ Revisé log semanal
- ☐ Denegados investigados
- ☐ Exports autorizados
- ☐ Hallazgos documentados
- ☐ Acciones correctivas asignadas

### 10. Ejercicio práctico (entorno QA)

En QA: abrir `/Audit`, filtrar por acción, simular investigación, documentar hallazgo ficticio.

> Entorno: [http://164.68.99.83:8091](http://164.68.99.83:8091)

### 11. Evaluación — mini examen

**1.** Audit sirve para:
   - A) Trazabilidad y compliance
   - B) Ventas
   - C) Marketing
   - D) Crear leads
   - **Respuesta correcta:** A) Trazabilidad y compliance

**2.** Intentos denegados:
   - A) Investigar siempre
   - B) Ignorar
   - C) Borrar
   - D) Celebrar
   - **Respuesta correcta:** A) Investigar siempre

**3.** Offboarding + intentos post-salida:
   - A) Alerta de seguridad
   - B) Normal
   - C) Bueno
   - D) Esperado
   - **Respuesta correcta:** A) Alerta de seguridad

**4.** ¿Ruta?
   - A) /Audit
   - B) /Leads
   - C) /Tasks
   - D) /revenue
   - **Respuesta correcta:** A) /Audit

**5.** Export evidencia:
   - A) Canal seguro autorizado
   - B) WhatsApp
   - C) Público
   - D) Sin registro
   - **Respuesta correcta:** A) Canal seguro autorizado

### 12. Certificación del módulo

Investigación simulada QA + evaluación → badge **Compliance Sentinel**.

---
