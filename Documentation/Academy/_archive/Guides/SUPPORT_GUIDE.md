# Especialista de Soporte y Éxito del Cliente — Guía Operativa AutonomusCRM Academy

> **Programa:** AutonomusCRM Enterprise Academy  
> **Rol:** Especialista de Soporte y Éxito del Cliente  
> **Usuario de práctica:** `support@autonomuscrm.local`  
> **Entorno:** http://164.68.99.83:8091  

---

## Tabla de contenido

1. [Capítulo 1 — Bienvenida e impacto](#capítulo-1--bienvenida-e-impacto)
2. [Capítulo 2 — Mi primer día](#capítulo-2--mi-primer-día)
3. [Capítulo 3 — Mi jornada laboral completa](#capítulo-3--mi-jornada-laboral-completa)
4. [Capítulo 4 — Procesos y escenarios reales](#capítulo-4--procesos-y-escenarios-reales)
5. [Capítulo 5 — Errores más comunes](#capítulo-5--errores-más-comunes)
6. [Capítulo 6 — Indicadores de desempeño](#capítulo-6--indicadores-de-desempeño)
7. [Capítulo 7 — Uso de IA en tu rol](#capítulo-7--uso-de-ia-en-tu-rol)
8. [Capítulo 8 — Certificación operativa](#capítulo-8--certificación-operativa)

---

# Capítulo 1 — Bienvenida e impacto

## Quién eres como Especialista de Soporte y Éxito del Cliente

Guardián de la experiencia post-venta

Protege ingresos recurrentes, reduce churn y eleva NPS.

### Tu impacto en la empresa

```mermaid
flowchart LR
    A[Tú: Especialista de Soporte y Éxito del Cliente] --> B[Ingresos]
    A --> C[Satisfacción cliente]
    A --> D[Crecimiento sostenible]
    B --> E[Empresa rentable]
    C --> E
    D --> E
```

| Dimensión | Tu contribución |
|-----------|-----------------|
| **Ingresos** | Identifica señales de renovación, expansión y riesgo antes de que impacten MRR. |
| **Satisfacción** | Resuelve incidentes, ejecuta playbooks y documenta cada contacto. |
| **Crecimiento** | Convierte clientes satisfechos en referidos y casos de expansión. |

### Historia real: primer mes en TechSolutions Panamá

Imagina tu primer lunes en **TechSolutions Panamá**, empresa B2B de servicios tecnológicos. El CEO te dice: *"No necesito que aprendas software; necesito que protejas nuestro pipeline y nuestros clientes."* Esta guía te lleva de cero a productivo usando AutonomusCRM como sistema nervioso del negocio — no como formulario digital.

### Áreas de enfoque de tu rol

- Customer Success OS
- Customer 360
- Tickets
- Playbooks
- Tareas
- Trust Studio (lectura)

### Mapa mental de tu rol

```mermaid
mindmap
  root((Especialista de Soporte y Éxito del Cliente))
    Customer_Success_OS
    Customer_360
    Tickets
    Playbooks
    Tareas
    Trust_Studio_(lectura)
```

### Ejercicio 1.1 — Autodiagnóstico

Responde por escrito (15 min):

1. ¿Qué resultado medible debe lograr tu rol este trimestre?
2. ¿Quién depende de tu trabajo diario?
3. ¿Qué pasa en la empresa si no inicias sesión durante una semana?

### Ejercicios adicionales Capítulo 1

- Redacta tu elevator pitch del rol en 30 segundos usando solo lenguaje de negocio.
- Identifica 3 stakeholders que dependen de tu trabajo diario y qué les entregas.
- Enumera 5 resultados medibles que tu manager espera este trimestre.

### Estudios de caso

#### De soporte reactivo a CS proactivo

**Contexto:** NPS 6.2; churn 8% anual.

**Desafío:** Solo tickets; sin playbooks renovación.

**Acciones:** Playbooks 90/60/30; QBR trimestral; health score.

**Resultado:** NPS 8.1; churn 4.2% en 12 meses.

**Lecciones aplicables hoy:**

1. ¿Qué elemento replicarías mañana en tu jornada?
2. ¿Qué riesgo similar existe en tu cartera actual?

#### Recuperación cuenta VIP post-P1

**Contexto:** Banco Nacional incidente 6h; amenaza cancelación.

**Desafío:** Confianza destruida.

**Acciones:** War room; comunicación CEO; crédito servicio; plan prevención.

**Resultado:** Renovación + expansión $50K.

**Lecciones aplicables hoy:**

1. ¿Qué elemento replicarías mañana en tu jornada?
2. ¿Qué riesgo similar existe en tu cartera actual?

---

# Capítulo 2 — Mi primer día

## 2.1 Acceso al sistema

| Paso | Acción | Por qué |
|------|--------|---------|
| 1 | Ir a http://164.68.99.83:8091/Account/Login | Punto de entrada seguro |
| 2 | Email: `support@autonomuscrm.local` | Identidad única auditada |
| 3 | Contraseña: (proporcionada por Admin) | Nunca compartir |
| 4 | TenantId: dejar vacío o cero | Búsqueda por email |
| 5 | Tras login → Command Center `/` | Tu centro de mando |

```mermaid
sequenceDiagram
    participant U as Tú
    participant CRM as AutonomusCRM
    participant CC as Command Center
    U->>CRM: Login email + password
    CRM->>CC: Redirección post-auth
    CC->>U: Métricas + prioridades del día
```

## 2.2 Recorrido guiado — Qué significa cada pantalla

| Pantalla | Ruta | Qué significa para el negocio | Aprender |
|----------|------|--------------------------------|----------|
| Command Center | `/` | Pulso del negocio: ingresos generados, protegidos, decisiones IA 24h. | Siempre |
| Trust Studio | `/TrustInbox` | Bandeja de aprobación humana para decisiones de IA (Human-in-the-Loop). | Día 3 |
| Workforce | `/Agents` | Agentes de IA que automatizan tareas repetitivas. | Día 2 |
| Revenue OS | `/revenue` | Dashboard de ingresos, pipeline y forecast. | Semana 1 |
| Executive OS | `/executive` | Vista ejecutiva para juntas y board. | Semana 2 |
| Pipeline | `/Deals` | Oportunidades comerciales por etapa. | Día 2 |
| Directorio | `/Customers` | Base de clientes activos. | Día 1 |
| Customer 360 | `/Customer360` | Vista unificada: historial, deals, tickets, interacciones. | Día 1 |
| Customer Success | `/customer-success` | Tickets, playbooks y salud de cartera. | Día 1 |
| Leads | `/Leads` | Prospectos no calificados o en calificación. | Semana 1 |
| Tareas | `/Tasks` | Compromisos con fecha y responsable. | Día 1 |
| Auditoría | `/Audit` | Trazabilidad de acciones para cumplimiento. | Opcional |

## 2.3 Configuración personal esencial

1. **Idioma** — Selector en barra superior (es/en).
2. **Tema** — Modo claro/oscuro según preferencia.
3. **Búsqueda global** — `Ctrl+K` para saltar a cualquier registro.
4. **Marcadores mentales** — Memoriza 3 rutas de tu rol para la semana 1.

### Simulación 2.A — Primer login (30 min)

1. Inicia sesión con tu usuario de práctica.
2. Permanece 5 min en Command Center sin hacer clic: solo lee métricas.
3. Abre cada pantalla de la tabla anterior marcada Día 1.
4. Escribe una frase de negocio (no técnica) describiendo cada pantalla.

### Ejercicios Capítulo 2

- Completa login en entorno QA y captura Command Center (anotación, no screenshot obligatorio).
- Visita cada pantalla marcada Día 1 y escribe una frase de valor de negocio.
- Practica Ctrl+K: encuentra un cliente, un deal y una tarea en <2 min cada uno.

### Tabla de decisión — ¿Qué pantalla abro primero?

| Situación | Pantalla | Ruta |
|-----------|----------|------|
| Inicio del día | Command Center | `/` |
| Antes de llamada cliente | Customer 360 | `/Customer360` |
| Revisar pipeline | Deals | `/Deals` |
| Aprobar IA | Trust Studio | `/TrustInbox` |
| Ticket abierto | Customer Success | `/customer-success` |

---

# Capítulo 3 — Mi jornada laboral completa

> Jornada tipo B2B — adapta horarios a tu zona. La lógica es universal.

## 08:00 — Arranque y priorización

| | |
|---|---|
| **Qué hacer** | Revisar Command Center y bandeja de prioridades del día. |
| **Por qué** | Las primeras 60 minutos definen si reaccionas o lideras la jornada. |
| **Resultado esperado** | Lista top 5 acciones con dueño y hora límite. |

**Tips para tu rol:**

- Prioriza `Customer Success OS` si el tiempo se agota.
- Usa Ctrl+K para no perder minutos navegando.
- Registra actividad antes de pasar al siguiente bloque.

**Micro-ejercicio (5 min):** Anota qué métrica de negocio validarías al terminar este bloque.

## 08:30 — Revisión de alertas

| | |
|---|---|
| **Qué hacer** | Trust Studio, deals en riesgo, tickets críticos según tu rol. |
| **Por qué** | La IA y los workflows señalan lo urgente antes que el ruido. |
| **Resultado esperado** | Cero sorpresas a mediodía. |

**Tips para tu rol:**

- Prioriza `Customer Success OS` si el tiempo se agota.
- Usa Ctrl+K para no perder minutos navegando.
- Registra actividad antes de pasar al siguiente bloque.

**Micro-ejercicio (5 min):** Anota qué métrica de negocio validarías al terminar este bloque.

## 09:00 — Bloque de ejecución #1

| | |
|---|---|
| **Qué hacer** | Cola de tickets |
| **Por qué** | Proteger tiempo profundo para trabajo de alto impacto. |
| **Resultado esperado** | Avance medible en métrica clave del rol. |

**Tips para tu rol:**

- Prioriza `Customer Success OS` si el tiempo se agota.
- Usa Ctrl+K para no perder minutos navegando.
- Registra actividad antes de pasar al siguiente bloque.

**Micro-ejercicio (5 min):** Anota qué métrica de negocio validarías al terminar este bloque.

## 09:30 — Seguimiento a interesados

| | |
|---|---|
| **Qué hacer** | Actualizar registros: leads, clientes o tickets con notas de contacto. |
| **Por qué** | Sin registro no hay forecast, handoff ni auditoría. |
| **Resultado esperado** | CRM refleja la realidad del negocio. |

**Tips para tu rol:**

- Prioriza `Customer Success OS` si el tiempo se agota.
- Usa Ctrl+K para no perder minutos navegando.
- Registra actividad antes de pasar al siguiente bloque.

**Micro-ejercicio (5 min):** Anota qué métrica de negocio validarías al terminar este bloque.

## 10:00 — Coordinación interna

| | |
|---|---|
| **Qué hacer** | Sync con ventas, soporte o admin según handoffs pendientes. |
| **Por qué** | El 40% de deals se pierden por falta de coordinación. |
| **Resultado esperado** | Handoffs cerrados con próximo paso definido. |

**Tips para tu rol:**

- Prioriza `Customer Success OS` si el tiempo se agota.
- Usa Ctrl+K para no perder minutos navegando.
- Registra actividad antes de pasar al siguiente bloque.

**Micro-ejercicio (5 min):** Anota qué métrica de negocio validarías al terminar este bloque.

## 10:30 — Bloque de ejecución #2

| | |
|---|---|
| **Qué hacer** | Clientes en riesgo |
| **Por qué** | Mantener momentum en pipeline o cola de servicio. |
| **Resultado esperado** | Etapa avanzada o ticket resuelto. |

**Tips para tu rol:**

- Prioriza `Customer Success OS` si el tiempo se agota.
- Usa Ctrl+K para no perder minutos navegando.
- Registra actividad antes de pasar al siguiente bloque.

**Micro-ejercicio (5 min):** Anota qué métrica de negocio validarías al terminar este bloque.

## 11:00 — Customer 360 / contexto

| | |
|---|---|
| **Qué hacer** | Antes de cada llamada importante, abrir vista 360 del cliente. |
| **Por qué** | Personalización aumenta conversión y confianza. |
| **Resultado esperado** | Conversaciones con contexto completo. |

**Tips para tu rol:**

- Prioriza `Customer Success OS` si el tiempo se agota.
- Usa Ctrl+K para no perder minutos navegando.
- Registra actividad antes de pasar al siguiente bloque.

**Micro-ejercicio (5 min):** Anota qué métrica de negocio validarías al terminar este bloque.

## 11:30 — Tareas y compromisos

| | |
|---|---|
| **Qué hacer** | Completar o reprogramar tareas vencidas en `/Tasks`. |
| **Por qué** | Los compromisos olvidados erosionan credibilidad. |
| **Resultado esperado** | Bandeja de tareas al día. |

**Tips para tu rol:**

- Prioriza `Customer Success OS` si el tiempo se agota.
- Usa Ctrl+K para no perder minutos navegando.
- Registra actividad antes de pasar al siguiente bloque.

**Micro-ejercicio (5 min):** Anota qué métrica de negocio validarías al terminar este bloque.

## 12:00 — Cierre de mañana

| | |
|---|---|
| **Qué hacer** | Verificar que métricas del día AM están registradas. |
| **Por qué** | Gerencia toma decisiones con datos de hoy, no de ayer. |
| **Resultado esperado** | Dashboard actualizado. |

**Tips para tu rol:**

- Prioriza `Customer Success OS` si el tiempo se agota.
- Usa Ctrl+K para no perder minutos navegando.
- Registra actividad antes de pasar al siguiente bloque.

**Micro-ejercicio (5 min):** Anota qué métrica de negocio validarías al terminar este bloque.

## 13:00 — Revisión post-almuerzo

| | |
|---|---|
| **Qué hacer** | Command palette (Ctrl+K) para saltar a registros pendientes. |
| **Por qué** | Recuperar foco rápido tras pausa. |
| **Resultado esperado** | Retoma en <5 minutos. |

**Tips para tu rol:**

- Prioriza `Customer Success OS` si el tiempo se agota.
- Usa Ctrl+K para no perder minutos navegando.
- Registra actividad antes de pasar al siguiente bloque.

**Micro-ejercicio (5 min):** Anota qué métrica de negocio validarías al terminar este bloque.

## 14:00 — Bloque de ejecución #3

| | |
|---|---|
| **Qué hacer** | Renovaciones próximas |
| **Por qué** | Ventana típica de reuniones con clientes. |
| **Resultado esperado** | Propuesta enviada o caso resuelto. |

**Tips para tu rol:**

- Prioriza `Customer Success OS` si el tiempo se agota.
- Usa Ctrl+K para no perder minutos navegando.
- Registra actividad antes de pasar al siguiente bloque.

**Micro-ejercicio (5 min):** Anota qué métrica de negocio validarías al terminar este bloque.

## 15:00 — IA y automatización

| | |
|---|---|
| **Qué hacer** | Revisar sugerencias de Workforce/Agents y aprobar o rechazar en Trust Studio si aplica. |
| **Por qué** | La IA amplifica tu capacidad; tú mantienes el criterio. |
| **Resultado esperado** | Decisiones IA alineadas a política comercial. |

**Tips para tu rol:**

- Prioriza `Customer Success OS` si el tiempo se agota.
- Usa Ctrl+K para no perder minutos navegando.
- Registra actividad antes de pasar al siguiente bloque.

**Micro-ejercicio (5 min):** Anota qué métrica de negocio validarías al terminar este bloque.

## 16:00 — Pipeline / cola de servicio

| | |
|---|---|
| **Qué hacer** | Escalamientos |
| **Por qué** | Última oportunidad del día para desbloquear lo crítico. |
| **Resultado esperado** | Ningún deal/ticket crítico sin próximo paso. |

**Tips para tu rol:**

- Prioriza `Customer Success OS` si el tiempo se agota.
- Usa Ctrl+K para no perder minutos navegando.
- Registra actividad antes de pasar al siguiente bloque.

**Micro-ejercicio (5 min):** Anota qué métrica de negocio validarías al terminar este bloque.

## 16:30 — Documentación y calidad de datos

| | |
|---|---|
| **Qué hacer** | Corregir campos vacíos, etapas incorrectas, duplicados evidentes. |
| **Por qué** | Datos sucios = forecast falso. |
| **Resultado esperado** | Registros listos para reportes. |

**Tips para tu rol:**

- Prioriza `Customer Success OS` si el tiempo se agota.
- Usa Ctrl+K para no perder minutos navegando.
- Registra actividad antes de pasar al siguiente bloque.

**Micro-ejercicio (5 min):** Anota qué métrica de negocio validarías al terminar este bloque.

## 17:00 — Planificación del día siguiente

| | |
|---|---|
| **Qué hacer** | Crear tareas para mañana con recordatorios. |
| **Por qué** | Arrancar con plan vence arrancar con ansiedad. |
| **Resultado esperado** | Agenda del día +1 definida. |

**Tips para tu rol:**

- Prioriza `Customer Success OS` si el tiempo se agota.
- Usa Ctrl+K para no perder minutos navegando.
- Registra actividad antes de pasar al siguiente bloque.

**Micro-ejercicio (5 min):** Anota qué métrica de negocio validarías al terminar este bloque.

## 17:30 — Cierre de jornada

| | |
|---|---|
| **Qué hacer** | Revisar KPIs personales vs meta diaria. |
| **Por qué** | Lo que no se mide no mejora. |
| **Resultado esperado** | Autoevaluación honesta + 1 mejora para mañana. |

**Tips para tu rol:**

- Prioriza `Customer Success OS` si el tiempo se agota.
- Usa Ctrl+K para no perder minutos navegando.
- Registra actividad antes de pasar al siguiente bloque.

**Micro-ejercicio (5 min):** Anota qué métrica de negocio validarías al terminar este bloque.

```mermaid
gantt
    title Jornada tipo AutonomusCRM
    dateFormat HH:mm
    axisFormat %H:%M
    section Mañana
    Priorización     :08:00, 30m
    Ejecución        :09:00, 120m
    Coordinación     :11:00, 60m
    section Tarde
    Clientes         :14:00, 120m
    IA y cierre      :16:00, 90m
```

### Ejercicios Capítulo 3

- Simula una jornada completa en papel: asigna tus tareas reales a cada bloque horario.
- Identifica qué bloque sueles saltarte y diseña un recordatorio.
- Pairing 1h con colega certificado: observa su rutina 08:00-10:00.

---

# Capítulo 4 — Procesos y escenarios reales

## Escenario 4.1 — Cliente VIP ticket P1

**Historia:** Banco Nacional: caída integración API.

**Stakeholders:** Cliente · Tu equipo · Manager · (si aplica) Admin/SuperAdmin

**Precondiciones:** Acceso al entorno QA; Customer 360 disponible; mentor asignado si es primera vez.

**Tiempo estimado:** 45-90 minutos (incluye documentación en CRM).

**Flujo:**

```mermaid
flowchart TD
    S[Inicio]
    S --> A1[/customer-success playbook P1]
    A1 --> A2[Notificar Admin integración]
    A2 --> A3[Touchpoint cada 30 min]
    A3 --> A4[Escalar manager si SLA <2h]
    A4 --> A5[Cierre con confirmación cliente]
    A5 --> O[Resultado]
```

**Pasos detallados:**

1. `/customer-success` playbook P1
   - *Verificación:* Registro actualizado y próximo paso con fecha.

2. Notificar Admin integración
   - *Verificación:* Registro actualizado y próximo paso con fecha.

3. Touchpoint cada 30 min
   - *Verificación:* Registro actualizado y próximo paso con fecha.

4. Escalar manager si SLA <2h
   - *Verificación:* Registro actualizado y próximo paso con fecha.

5. Cierre con confirmación cliente
   - *Verificación:* Registro actualizado y próximo paso con fecha.

**Resultado de negocio:** Incidente resuelto; NPS preservado.

**Cuadro de decisión:**

| Pregunta | Sí | No |
|----------|----|----|
| ¿Tengo contexto 360? | Continuar | Abrir Customer 360 primero |
| ¿Próximo paso definido? | Ejecutar | Crear tarea con fecha |
| ¿Requiere aprobación? | Trust Studio | Proceder |
| ¿Impacta forecast? | Notificar manager | Registrar y continuar |

**Errores comunes en este escenario:**

- Omitir registro de actividad antes de cambiar etapa.
- No definir dueño del siguiente paso.
- Ignorar alertas del Command Center relacionadas.

**Preguntas de reflexión:**

1. ¿Qué KPI de tu rol mejora si ejecutas bien este escenario?
2. ¿Qué habrías hecho diferente con un cliente VIP?
3. ¿Qué documentación dejó el siguiente rol listo para actuar?

**Práctica en entorno QA:**

- URL: http://164.68.99.83:8091
- Usuario: `support@autonomuscrm.local`
- Busca registros seed o crea datos de práctica con prefijo ACADEMY-

---

## Escenario 4.2 — Renovación anual en riesgo

**Historia:** Contrato $120K vence 45 días; uso bajó 30%.

**Stakeholders:** Cliente · Tu equipo · Manager · (si aplica) Admin/SuperAdmin

**Precondiciones:** Acceso al entorno QA; Customer 360 disponible; mentor asignado si es primera vez.

**Tiempo estimado:** 45-90 minutos (incluye documentación en CRM).

**Flujo:**

```mermaid
flowchart TD
    S[Inicio]
    S --> A1[Customer 360 señales salud]
    A1 --> A2[Playbook Renovación]
    A2 --> A3[QBR con sponsor]
    A3 --> A4[Plan valor documentado]
    A4 --> A5[Deal o nota renovación]
    A5 --> O[Resultado]
```

**Pasos detallados:**

1. Customer 360 señales salud
   - *Verificación:* Registro actualizado y próximo paso con fecha.

2. Playbook Renovación
   - *Verificación:* Registro actualizado y próximo paso con fecha.

3. QBR con sponsor
   - *Verificación:* Registro actualizado y próximo paso con fecha.

4. Plan valor documentado
   - *Verificación:* Registro actualizado y próximo paso con fecha.

5. Deal o nota renovación
   - *Verificación:* Registro actualizado y próximo paso con fecha.

**Resultado de negocio:** Renovación firmada o churn con lecciones.

**Cuadro de decisión:**

| Pregunta | Sí | No |
|----------|----|----|
| ¿Tengo contexto 360? | Continuar | Abrir Customer 360 primero |
| ¿Próximo paso definido? | Ejecutar | Crear tarea con fecha |
| ¿Requiere aprobación? | Trust Studio | Proceder |
| ¿Impacta forecast? | Notificar manager | Registrar y continuar |

**Errores comunes en este escenario:**

- Omitir registro de actividad antes de cambiar etapa.
- No definir dueño del siguiente paso.
- Ignorar alertas del Command Center relacionadas.

**Preguntas de reflexión:**

1. ¿Qué KPI de tu rol mejora si ejecutas bien este escenario?
2. ¿Qué habrías hecho diferente con un cliente VIP?
3. ¿Qué documentación dejó el siguiente rol listo para actuar?

**Práctica en entorno QA:**

- URL: http://164.68.99.83:8091
- Usuario: `support@autonomuscrm.local`
- Busca registros seed o crea datos de práctica con prefijo ACADEMY-

---

## Escenario 4.3 — Onboarding día 1 cliente nuevo

**Historia:** Handoff Sales llegó incompleto.

**Stakeholders:** Cliente · Tu equipo · Manager · (si aplica) Admin/SuperAdmin

**Precondiciones:** Acceso al entorno QA; Customer 360 disponible; mentor asignado si es primera vez.

**Tiempo estimado:** 45-90 minutos (incluye documentación en CRM).

**Flujo:**

```mermaid
flowchart TD
    S[Inicio]
    S --> A1[Leer deal Won y notas]
    A1 --> A2[Llamada kickoff 60 min]
    A2 --> A3[Playbook Onboarding]
    A3 --> A4[Tareas 30-60-90]
    A4 --> A5[Health score baseline]
    A5 --> O[Resultado]
```

**Pasos detallados:**

1. Leer deal Won y notas
   - *Verificación:* Registro actualizado y próximo paso con fecha.

2. Llamada kickoff 60 min
   - *Verificación:* Registro actualizado y próximo paso con fecha.

3. Playbook Onboarding
   - *Verificación:* Registro actualizado y próximo paso con fecha.

4. Tareas 30-60-90
   - *Verificación:* Registro actualizado y próximo paso con fecha.

5. Health score baseline
   - *Verificación:* Registro actualizado y próximo paso con fecha.

**Resultado de negocio:** Cliente operativo semana 2.

**Cuadro de decisión:**

| Pregunta | Sí | No |
|----------|----|----|
| ¿Tengo contexto 360? | Continuar | Abrir Customer 360 primero |
| ¿Próximo paso definido? | Ejecutar | Crear tarea con fecha |
| ¿Requiere aprobación? | Trust Studio | Proceder |
| ¿Impacta forecast? | Notificar manager | Registrar y continuar |

**Errores comunes en este escenario:**

- Omitir registro de actividad antes de cambiar etapa.
- No definir dueño del siguiente paso.
- Ignorar alertas del Command Center relacionadas.

**Preguntas de reflexión:**

1. ¿Qué KPI de tu rol mejora si ejecutas bien este escenario?
2. ¿Qué habrías hecho diferente con un cliente VIP?
3. ¿Qué documentación dejó el siguiente rol listo para actuar?

**Práctica en entorno QA:**

- URL: http://164.68.99.83:8091
- Usuario: `support@autonomuscrm.local`
- Busca registros seed o crea datos de práctica con prefijo ACADEMY-

---

## Escenario 4.4 — Ticket repetido mismo cliente

**Historia:** Tercer ticket mismo mes por mismo síntoma.

**Stakeholders:** Cliente · Tu equipo · Manager · (si aplica) Admin/SuperAdmin

**Precondiciones:** Acceso al entorno QA; Customer 360 disponible; mentor asignado si es primera vez.

**Tiempo estimado:** 45-90 minutos (incluye documentación en CRM).

**Flujo:**

```mermaid
flowchart TD
    S[Inicio]
    S --> A1[Etiquetar causa raíz]
    A1 --> A2[Escalar técnico L2]
    A2 --> A3[Comunicar progreso proactivo]
    A3 --> A4[QBR adelantado si VIP]
    A4 --> A5[Actualizar playbook]
    A5 --> O[Resultado]
```

**Pasos detallados:**

1. Etiquetar causa raíz
   - *Verificación:* Registro actualizado y próximo paso con fecha.

2. Escalar técnico L2
   - *Verificación:* Registro actualizado y próximo paso con fecha.

3. Comunicar progreso proactivo
   - *Verificación:* Registro actualizado y próximo paso con fecha.

4. QBR adelantado si VIP
   - *Verificación:* Registro actualizado y próximo paso con fecha.

5. Actualizar playbook
   - *Verificación:* Registro actualizado y próximo paso con fecha.

**Resultado de negocio:** Causa raíz resuelta; confianza restaurada.

**Cuadro de decisión:**

| Pregunta | Sí | No |
|----------|----|----|
| ¿Tengo contexto 360? | Continuar | Abrir Customer 360 primero |
| ¿Próximo paso definido? | Ejecutar | Crear tarea con fecha |
| ¿Requiere aprobación? | Trust Studio | Proceder |
| ¿Impacta forecast? | Notificar manager | Registrar y continuar |

**Errores comunes en este escenario:**

- Omitir registro de actividad antes de cambiar etapa.
- No definir dueño del siguiente paso.
- Ignorar alertas del Command Center relacionadas.

**Preguntas de reflexión:**

1. ¿Qué KPI de tu rol mejora si ejecutas bien este escenario?
2. ¿Qué habrías hecho diferente con un cliente VIP?
3. ¿Qué documentación dejó el siguiente rol listo para actuar?

**Práctica en entorno QA:**

- URL: http://164.68.99.83:8091
- Usuario: `support@autonomuscrm.local`
- Busca registros seed o crea datos de práctica con prefijo ACADEMY-

---

## Escenario 4.5 — NPS detractor recuperación

**Historia:** Cliente puntúa 4/10 post-incidente.

**Stakeholders:** Cliente · Tu equipo · Manager · (si aplica) Admin/SuperAdmin

**Precondiciones:** Acceso al entorno QA; Customer 360 disponible; mentor asignado si es primera vez.

**Tiempo estimado:** 45-90 minutos (incluye documentación en CRM).

**Flujo:**

```mermaid
flowchart TD
    S[Inicio]
    S --> A1[Llamada empatía 24h]
    A1 --> A2[Plan acción concreto]
    A2 --> A3[Seguimiento 7 y 30 días]
    A3 --> A4[Nota en Customer 360]
    A4 --> A5[Alerta manager si VIP]
    A5 --> O[Resultado]
```

**Pasos detallados:**

1. Llamada empatía 24h
   - *Verificación:* Registro actualizado y próximo paso con fecha.

2. Plan acción concreto
   - *Verificación:* Registro actualizado y próximo paso con fecha.

3. Seguimiento 7 y 30 días
   - *Verificación:* Registro actualizado y próximo paso con fecha.

4. Nota en Customer 360
   - *Verificación:* Registro actualizado y próximo paso con fecha.

5. Alerta manager si VIP
   - *Verificación:* Registro actualizado y próximo paso con fecha.

**Resultado de negocio:** NPS recuperado a 7+ en 60 días.

**Cuadro de decisión:**

| Pregunta | Sí | No |
|----------|----|----|
| ¿Tengo contexto 360? | Continuar | Abrir Customer 360 primero |
| ¿Próximo paso definido? | Ejecutar | Crear tarea con fecha |
| ¿Requiere aprobación? | Trust Studio | Proceder |
| ¿Impacta forecast? | Notificar manager | Registrar y continuar |

**Errores comunes en este escenario:**

- Omitir registro de actividad antes de cambiar etapa.
- No definir dueño del siguiente paso.
- Ignorar alertas del Command Center relacionadas.

**Preguntas de reflexión:**

1. ¿Qué KPI de tu rol mejora si ejecutas bien este escenario?
2. ¿Qué habrías hecho diferente con un cliente VIP?
3. ¿Qué documentación dejó el siguiente rol listo para actuar?

**Práctica en entorno QA:**

- URL: http://164.68.99.83:8091
- Usuario: `support@autonomuscrm.local`
- Busca registros seed o crea datos de práctica con prefijo ACADEMY-

---

## Escenario 4.6 — Expansión desde soporte

**Historia:** Cliente pregunta por módulo adicional en ticket.

**Stakeholders:** Cliente · Tu equipo · Manager · (si aplica) Admin/SuperAdmin

**Precondiciones:** Acceso al entorno QA; Customer 360 disponible; mentor asignado si es primera vez.

**Tiempo estimado:** 45-90 minutos (incluye documentación en CRM).

**Flujo:**

```mermaid
flowchart TD
    S[Inicio]
    S --> A1[Documentar interés en 360]
    A1 --> A2[Notificar Sales]
    A2 --> A3[Mantener ticket resuelto]
    A3 --> A4[No vender agresivo]
    A4 --> A5[Seguimiento expansión]
    A5 --> O[Resultado]
```

**Pasos detallados:**

1. Documentar interés en 360
   - *Verificación:* Registro actualizado y próximo paso con fecha.

2. Notificar Sales
   - *Verificación:* Registro actualizado y próximo paso con fecha.

3. Mantener ticket resuelto
   - *Verificación:* Registro actualizado y próximo paso con fecha.

4. No vender agresivo
   - *Verificación:* Registro actualizado y próximo paso con fecha.

5. Seguimiento expansión
   - *Verificación:* Registro actualizado y próximo paso con fecha.

**Resultado de negocio:** Deal expansión creado; CSAT intacto.

**Cuadro de decisión:**

| Pregunta | Sí | No |
|----------|----|----|
| ¿Tengo contexto 360? | Continuar | Abrir Customer 360 primero |
| ¿Próximo paso definido? | Ejecutar | Crear tarea con fecha |
| ¿Requiere aprobación? | Trust Studio | Proceder |
| ¿Impacta forecast? | Notificar manager | Registrar y continuar |

**Errores comunes en este escenario:**

- Omitir registro de actividad antes de cambiar etapa.
- No definir dueño del siguiente paso.
- Ignorar alertas del Command Center relacionadas.

**Preguntas de reflexión:**

1. ¿Qué KPI de tu rol mejora si ejecutas bien este escenario?
2. ¿Qué habrías hecho diferente con un cliente VIP?
3. ¿Qué documentación dejó el siguiente rol listo para actuar?

**Práctica en entorno QA:**

- URL: http://164.68.99.83:8091
- Usuario: `support@autonomuscrm.local`
- Busca registros seed o crea datos de práctica con prefijo ACADEMY-

---

## Escenario 4.7 — SLA P2 en cola alta

**Historia:** 15 tickets P2; capacidad equipo 8/día.

**Stakeholders:** Cliente · Tu equipo · Manager · (si aplica) Admin/SuperAdmin

**Precondiciones:** Acceso al entorno QA; Customer 360 disponible; mentor asignado si es primera vez.

**Tiempo estimado:** 45-90 minutos (incluye documentación en CRM).

**Flujo:**

```mermaid
flowchart TD
    S[Inicio]
    S --> A1[Priorizar VIP y renovaciones]
    A1 --> A2[Comunicar tiempos honestos]
    A2 --> A3[Pedir refuerzo manager]
    A3 --> A4[Templates respuesta]
    A4 --> A5[Post-mortem capacidad]
    A5 --> O[Resultado]
```

**Pasos detallados:**

1. Priorizar VIP y renovaciones
   - *Verificación:* Registro actualizado y próximo paso con fecha.

2. Comunicar tiempos honestos
   - *Verificación:* Registro actualizado y próximo paso con fecha.

3. Pedir refuerzo manager
   - *Verificación:* Registro actualizado y próximo paso con fecha.

4. Templates respuesta
   - *Verificación:* Registro actualizado y próximo paso con fecha.

5. Post-mortem capacidad
   - *Verificación:* Registro actualizado y próximo paso con fecha.

**Resultado de negocio:** SLA cumplido 92%; plan contratación.

**Cuadro de decisión:**

| Pregunta | Sí | No |
|----------|----|----|
| ¿Tengo contexto 360? | Continuar | Abrir Customer 360 primero |
| ¿Próximo paso definido? | Ejecutar | Crear tarea con fecha |
| ¿Requiere aprobación? | Trust Studio | Proceder |
| ¿Impacta forecast? | Notificar manager | Registrar y continuar |

**Errores comunes en este escenario:**

- Omitir registro de actividad antes de cambiar etapa.
- No definir dueño del siguiente paso.
- Ignorar alertas del Command Center relacionadas.

**Preguntas de reflexión:**

1. ¿Qué KPI de tu rol mejora si ejecutas bien este escenario?
2. ¿Qué habrías hecho diferente con un cliente VIP?
3. ¿Qué documentación dejó el siguiente rol listo para actuar?

**Práctica en entorno QA:**

- URL: http://164.68.99.83:8091
- Usuario: `support@autonomuscrm.local`
- Busca registros seed o crea datos de práctica con prefijo ACADEMY-

---

## Escenario 4.8 — Churn inevitable documentado

**Historia:** Cliente confirma no renovará por presupuesto.

**Stakeholders:** Cliente · Tu equipo · Manager · (si aplica) Admin/SuperAdmin

**Precondiciones:** Acceso al entorno QA; Customer 360 disponible; mentor asignado si es primera vez.

**Tiempo estimado:** 45-90 minutos (incluye documentación en CRM).

**Flujo:**

```mermaid
flowchart TD
    S[Inicio]
    S --> A1[Entrevista salida estructurada]
    A1 --> A2[Documentar razones]
    A2 --> A3[Oferta win-back si aplica]
    A3 --> A4[Cierre limpio accesos]
    A4 --> A5[Lecciones a producto]
    A5 --> O[Resultado]
```

**Pasos detallados:**

1. Entrevista salida estructurada
   - *Verificación:* Registro actualizado y próximo paso con fecha.

2. Documentar razones
   - *Verificación:* Registro actualizado y próximo paso con fecha.

3. Oferta win-back si aplica
   - *Verificación:* Registro actualizado y próximo paso con fecha.

4. Cierre limpio accesos
   - *Verificación:* Registro actualizado y próximo paso con fecha.

5. Lecciones a producto
   - *Verificación:* Registro actualizado y próximo paso con fecha.

**Resultado de negocio:** Churn limpio; inteligencia para retención.

**Cuadro de decisión:**

| Pregunta | Sí | No |
|----------|----|----|
| ¿Tengo contexto 360? | Continuar | Abrir Customer 360 primero |
| ¿Próximo paso definido? | Ejecutar | Crear tarea con fecha |
| ¿Requiere aprobación? | Trust Studio | Proceder |
| ¿Impacta forecast? | Notificar manager | Registrar y continuar |

**Errores comunes en este escenario:**

- Omitir registro de actividad antes de cambiar etapa.
- No definir dueño del siguiente paso.
- Ignorar alertas del Command Center relacionadas.

**Preguntas de reflexión:**

1. ¿Qué KPI de tu rol mejora si ejecutas bien este escenario?
2. ¿Qué habrías hecho diferente con un cliente VIP?
3. ¿Qué documentación dejó el siguiente rol listo para actuar?

**Práctica en entorno QA:**

- URL: http://164.68.99.83:8091
- Usuario: `support@autonomuscrm.local`
- Busca registros seed o crea datos de práctica con prefijo ACADEMY-

---

## Escenario 4.9 — Playbook adopción baja

**Historia:** Clientes no usan feature clave; riesgo valor.

**Stakeholders:** Cliente · Tu equipo · Manager · (si aplica) Admin/SuperAdmin

**Precondiciones:** Acceso al entorno QA; Customer 360 disponible; mentor asignado si es primera vez.

**Tiempo estimado:** 45-90 minutos (incluye documentación en CRM).

**Flujo:**

```mermaid
flowchart TD
    S[Inicio]
    S --> A1[Identificar cohorte]
    A1 --> A2[Webinar adopción]
    A2 --> A3[Tareas guiadas en CS]
    A3 --> A4[Métricas uso 30 días]
    A4 --> A5[Celebrar quick wins]
    A5 --> O[Resultado]
```

**Pasos detallados:**

1. Identificar cohorte
   - *Verificación:* Registro actualizado y próximo paso con fecha.

2. Webinar adopción
   - *Verificación:* Registro actualizado y próximo paso con fecha.

3. Tareas guiadas en CS
   - *Verificación:* Registro actualizado y próximo paso con fecha.

4. Métricas uso 30 días
   - *Verificación:* Registro actualizado y próximo paso con fecha.

5. Celebrar quick wins
   - *Verificación:* Registro actualizado y próximo paso con fecha.

**Resultado de negocio:** Adopción +40%; health score verde.

**Cuadro de decisión:**

| Pregunta | Sí | No |
|----------|----|----|
| ¿Tengo contexto 360? | Continuar | Abrir Customer 360 primero |
| ¿Próximo paso definido? | Ejecutar | Crear tarea con fecha |
| ¿Requiere aprobación? | Trust Studio | Proceder |
| ¿Impacta forecast? | Notificar manager | Registrar y continuar |

**Errores comunes en este escenario:**

- Omitir registro de actividad antes de cambiar etapa.
- No definir dueño del siguiente paso.
- Ignorar alertas del Command Center relacionadas.

**Preguntas de reflexión:**

1. ¿Qué KPI de tu rol mejora si ejecutas bien este escenario?
2. ¿Qué habrías hecho diferente con un cliente VIP?
3. ¿Qué documentación dejó el siguiente rol listo para actuar?

**Práctica en entorno QA:**

- URL: http://164.68.99.83:8091
- Usuario: `support@autonomuscrm.local`
- Busca registros seed o crea datos de práctica con prefijo ACADEMY-

---

## Escenario 4.10 — Escalamiento a ingeniería

**Historia:** Bug confirmado bloquea operación cliente.

**Stakeholders:** Cliente · Tu equipo · Manager · (si aplica) Admin/SuperAdmin

**Precondiciones:** Acceso al entorno QA; Customer 360 disponible; mentor asignado si es primera vez.

**Tiempo estimado:** 45-90 minutos (incluye documentación en CRM).

**Flujo:**

```mermaid
flowchart TD
    S[Inicio]
    S --> A1[Reproducir y documentar]
    A1 --> A2[Ticket interno prioridad]
    A2 --> A3[Comunicación cliente cada 4h]
    A3 --> A4[Workaround si existe]
    A4 --> A5[Cierre con confirmación]
    A5 --> O[Resultado]
```

**Pasos detallados:**

1. Reproducir y documentar
   - *Verificación:* Registro actualizado y próximo paso con fecha.

2. Ticket interno prioridad
   - *Verificación:* Registro actualizado y próximo paso con fecha.

3. Comunicación cliente cada 4h
   - *Verificación:* Registro actualizado y próximo paso con fecha.

4. Workaround si existe
   - *Verificación:* Registro actualizado y próximo paso con fecha.

5. Cierre con confirmación
   - *Verificación:* Registro actualizado y próximo paso con fecha.

**Resultado de negocio:** Resolución 18h; cliente informado siempre.

**Cuadro de decisión:**

| Pregunta | Sí | No |
|----------|----|----|
| ¿Tengo contexto 360? | Continuar | Abrir Customer 360 primero |
| ¿Próximo paso definido? | Ejecutar | Crear tarea con fecha |
| ¿Requiere aprobación? | Trust Studio | Proceder |
| ¿Impacta forecast? | Notificar manager | Registrar y continuar |

**Errores comunes en este escenario:**

- Omitir registro de actividad antes de cambiar etapa.
- No definir dueño del siguiente paso.
- Ignorar alertas del Command Center relacionadas.

**Preguntas de reflexión:**

1. ¿Qué KPI de tu rol mejora si ejecutas bien este escenario?
2. ¿Qué habrías hecho diferente con un cliente VIP?
3. ¿Qué documentación dejó el siguiente rol listo para actuar?

**Práctica en entorno QA:**

- URL: http://164.68.99.83:8091
- Usuario: `support@autonomuscrm.local`
- Busca registros seed o crea datos de práctica con prefijo ACADEMY-

---

## Proceso maestro — Ciclo de vida del cliente

```mermaid
flowchart LR
    L[Lead] --> Q{Calificado?}
    Q -->|Sí| C[Cliente]
    Q -->|No| N[Nurture]
    C --> D[Deal]
    D --> W{Won?}
    W -->|Sí| ON[Onboarding CS]
    W -->|No| ARCH[Archivo + razón]
    ON --> R[Renovación / Expansión]
```

### Ejercicios Capítulo 4

- Ejecuta 2 escenarios en entorno QA con mentor validando cada paso.
- Para cada escenario, completa el cuadro de decisión antes de actuar.
- Escribe qué harías diferente si el cliente fuera VIP.

---

# Capítulo 5 — Errores más comunes

> Top 50 errores observados en adopción CRM enterprise — y cómo evitarlos.

### Error 1 — No registrar llamadas ni emails en el CRM

| | |
|---|---|
| **Consecuencia** | Pérdida de tiempo, forecast falso, riesgo de churn o incumplimiento. |
| **Prevención** | Checklist diario, mentoría día 1-7, usar plantillas del rol. |
| **Corrección** | Actualizar registro hoy; documentar lección en nota del cliente/deal. |

**Ejemplo en TechSolutions Panamá:** Un colega cometió este error; el impacto fue retraso de forecast 1 semana. La corrección inmediata fue actualizar Customer 360 y notificar al manager.

### Error 2 — Dejar leads sin contactar más de 24 horas

| | |
|---|---|
| **Consecuencia** | Pérdida de tiempo, forecast falso, riesgo de churn o incumplimiento. |
| **Prevención** | Checklist diario, mentoría día 1-7, usar plantillas del rol. |
| **Corrección** | Actualizar registro hoy; documentar lección en nota del cliente/deal. |

**Ejemplo en TechSolutions Panamá:** Un colega cometió este error; el impacto fue retraso de forecast 1 semana. La corrección inmediata fue actualizar Customer 360 y notificar al manager.

### Error 3 — Crear cliente duplicado en lugar de buscar en directorio

| | |
|---|---|
| **Consecuencia** | Pérdida de tiempo, forecast falso, riesgo de churn o incumplimiento. |
| **Prevención** | Checklist diario, mentoría día 1-7, usar plantillas del rol. |
| **Corrección** | Actualizar registro hoy; documentar lección en nota del cliente/deal. |

**Ejemplo en TechSolutions Panamá:** Un colega cometió este error; el impacto fue retraso de forecast 1 semana. La corrección inmediata fue actualizar Customer 360 y notificar al manager.

### Error 4 — Avanzar etapa de deal sin criterio de la etapa

| | |
|---|---|
| **Consecuencia** | Pérdida de tiempo, forecast falso, riesgo de churn o incumplimiento. |
| **Prevención** | Checklist diario, mentoría día 1-7, usar plantillas del rol. |
| **Corrección** | Actualizar registro hoy; documentar lección en nota del cliente/deal. |

**Ejemplo en TechSolutions Panamá:** Un colega cometió este error; el impacto fue retraso de forecast 1 semana. La corrección inmediata fue actualizar Customer 360 y notificar al manager.

### Error 5 — Cerrar deal Won sin fecha de inicio de contrato

| | |
|---|---|
| **Consecuencia** | Pérdida de tiempo, forecast falso, riesgo de churn o incumplimiento. |
| **Prevención** | Checklist diario, mentoría día 1-7, usar plantillas del rol. |
| **Corrección** | Actualizar registro hoy; documentar lección en nota del cliente/deal. |

**Ejemplo en TechSolutions Panamá:** Un colega cometió este error; el impacto fue retraso de forecast 1 semana. La corrección inmediata fue actualizar Customer 360 y notificar al manager.

### Error 6 — Ignorar alertas de deals en riesgo del Command Center

| | |
|---|---|
| **Consecuencia** | Pérdida de tiempo, forecast falso, riesgo de churn o incumplimiento. |
| **Prevención** | Checklist diario, mentoría día 1-7, usar plantillas del rol. |
| **Corrección** | Actualizar registro hoy; documentar lección en nota del cliente/deal. |

**Ejemplo en TechSolutions Panamá:** Un colega cometió este error; el impacto fue retraso de forecast 1 semana. La corrección inmediata fue actualizar Customer 360 y notificar al manager.

### Error 7 — No usar Customer 360 antes de reuniones importantes

| | |
|---|---|
| **Consecuencia** | Pérdida de tiempo, forecast falso, riesgo de churn o incumplimiento. |
| **Prevención** | Checklist diario, mentoría día 1-7, usar plantillas del rol. |
| **Corrección** | Actualizar registro hoy; documentar lección en nota del cliente/deal. |

**Ejemplo en TechSolutions Panamá:** Un colega cometió este error; el impacto fue retraso de forecast 1 semana. La corrección inmediata fue actualizar Customer 360 y notificar al manager.

### Error 8 — Dejar tareas vencidas sin reprogramar

| | |
|---|---|
| **Consecuencia** | Pérdida de tiempo, forecast falso, riesgo de churn o incumplimiento. |
| **Prevención** | Checklist diario, mentoría día 1-7, usar plantillas del rol. |
| **Corrección** | Actualizar registro hoy; documentar lección en nota del cliente/deal. |

**Ejemplo en TechSolutions Panamá:** Un colega cometió este error; el impacto fue retraso de forecast 1 semana. La corrección inmediata fue actualizar Customer 360 y notificar al manager.

### Error 9 — Aprobar decisiones de IA sin leer contexto en Trust Studio

| | |
|---|---|
| **Consecuencia** | Pérdida de tiempo, forecast falso, riesgo de churn o incumplimiento. |
| **Prevención** | Checklist diario, mentoría día 1-7, usar plantillas del rol. |
| **Corrección** | Actualizar registro hoy; documentar lección en nota del cliente/deal. |

**Ejemplo en TechSolutions Panamá:** Un colega cometió este error; el impacto fue retraso de forecast 1 semana. La corrección inmediata fue actualizar Customer 360 y notificar al manager.

### Error 10 — Rechazar todas las sugerencias de IA por costumbre

| | |
|---|---|
| **Consecuencia** | Pérdida de tiempo, forecast falso, riesgo de churn o incumplimiento. |
| **Prevención** | Checklist diario, mentoría día 1-7, usar plantillas del rol. |
| **Corrección** | Actualizar registro hoy; documentar lección en nota del cliente/deal. |

**Ejemplo en TechSolutions Panamá:** Un colega cometió este error; el impacto fue retraso de forecast 1 semana. La corrección inmediata fue actualizar Customer 360 y notificar al manager.

### Error 11 — No documentar razón al marcar deal como Lost

| | |
|---|---|
| **Consecuencia** | Pérdida de tiempo, forecast falso, riesgo de churn o incumplimiento. |
| **Prevención** | Checklist diario, mentoría día 1-7, usar plantillas del rol. |
| **Corrección** | Actualizar registro hoy; documentar lección en nota del cliente/deal. |

**Ejemplo en TechSolutions Panamá:** Un colega cometió este error; el impacto fue retraso de forecast 1 semana. La corrección inmediata fue actualizar Customer 360 y notificar al manager.

### Error 12 — Mezclar idiomas en notas sin etiqueta clara

| | |
|---|---|
| **Consecuencia** | Pérdida de tiempo, forecast falso, riesgo de churn o incumplimiento. |
| **Prevención** | Checklist diario, mentoría día 1-7, usar plantillas del rol. |
| **Corrección** | Actualizar registro hoy; documentar lección en nota del cliente/deal. |

**Ejemplo en TechSolutions Panamá:** Un colega cometió este error; el impacto fue retraso de forecast 1 semana. La corrección inmediata fue actualizar Customer 360 y notificar al manager.

### Error 13 — Compartir credenciales entre usuarios

| | |
|---|---|
| **Consecuencia** | Pérdida de tiempo, forecast falso, riesgo de churn o incumplimiento. |
| **Prevención** | Checklist diario, mentoría día 1-7, usar plantillas del rol. |
| **Corrección** | Actualizar registro hoy; documentar lección en nota del cliente/deal. |

**Ejemplo en TechSolutions Panamá:** Un colega cometió este error; el impacto fue retraso de forecast 1 semana. La corrección inmediata fue actualizar Customer 360 y notificar al manager.

### Error 14 — Exportar datos sensibles sin autorización

| | |
|---|---|
| **Consecuencia** | Pérdida de tiempo, forecast falso, riesgo de churn o incumplimiento. |
| **Prevención** | Checklist diario, mentoría día 1-7, usar plantillas del rol. |
| **Corrección** | Actualizar registro hoy; documentar lección en nota del cliente/deal. |

**Ejemplo en TechSolutions Panamá:** Un colega cometió este error; el impacto fue retraso de forecast 1 semana. La corrección inmediata fue actualizar Customer 360 y notificar al manager.

### Error 15 — Saltarse calificación BANT en leads enterprise

| | |
|---|---|
| **Consecuencia** | Pérdida de tiempo, forecast falso, riesgo de churn o incumplimiento. |
| **Prevención** | Checklist diario, mentoría día 1-7, usar plantillas del rol. |
| **Corrección** | Actualizar registro hoy; documentar lección en nota del cliente/deal. |

**Ejemplo en TechSolutions Panamá:** Un colega cometió este error; el impacto fue retraso de forecast 1 semana. La corrección inmediata fue actualizar Customer 360 y notificar al manager.

### Error 16 — No convertir lead calificado a cliente antes del cierre

| | |
|---|---|
| **Consecuencia** | Pérdida de tiempo, forecast falso, riesgo de churn o incumplimiento. |
| **Prevención** | Checklist diario, mentoría día 1-7, usar plantillas del rol. |
| **Corrección** | Actualizar registro hoy; documentar lección en nota del cliente/deal. |

**Ejemplo en TechSolutions Panamá:** Un colega cometió este error; el impacto fue retraso de forecast 1 semana. La corrección inmediata fue actualizar Customer 360 y notificar al manager.

### Error 17 — Handoff ventas a soporte sin nota de contexto

| | |
|---|---|
| **Consecuencia** | Pérdida de tiempo, forecast falso, riesgo de churn o incumplimiento. |
| **Prevención** | Checklist diario, mentoría día 1-7, usar plantillas del rol. |
| **Corrección** | Actualizar registro hoy; documentar lección en nota del cliente/deal. |

**Ejemplo en TechSolutions Panamá:** Un colega cometió este error; el impacto fue retraso de forecast 1 semana. La corrección inmediata fue actualizar Customer 360 y notificar al manager.

### Error 18 — Prometer funcionalidades no confirmadas con producto

| | |
|---|---|
| **Consecuencia** | Pérdida de tiempo, forecast falso, riesgo de churn o incumplimiento. |
| **Prevención** | Checklist diario, mentoría día 1-7, usar plantillas del rol. |
| **Corrección** | Actualizar registro hoy; documentar lección en nota del cliente/deal. |

**Ejemplo en TechSolutions Panamá:** Un colega cometió este error; el impacto fue retraso de forecast 1 semana. La corrección inmediata fue actualizar Customer 360 y notificar al manager.

### Error 19 — Usar probabilidad 90% en etapa Discovery

| | |
|---|---|
| **Consecuencia** | Pérdida de tiempo, forecast falso, riesgo de churn o incumplimiento. |
| **Prevención** | Checklist diario, mentoría día 1-7, usar plantillas del rol. |
| **Corrección** | Actualizar registro hoy; documentar lección en nota del cliente/deal. |

**Ejemplo en TechSolutions Panamá:** Un colega cometió este error; el impacto fue retraso de forecast 1 semana. La corrección inmediata fue actualizar Customer 360 y notificar al manager.

### Error 20 — No actualizar forecast después de cambio de etapa

| | |
|---|---|
| **Consecuencia** | Pérdida de tiempo, forecast falso, riesgo de churn o incumplimiento. |
| **Prevención** | Checklist diario, mentoría día 1-7, usar plantillas del rol. |
| **Corrección** | Actualizar registro hoy; documentar lección en nota del cliente/deal. |

**Ejemplo en TechSolutions Panamá:** Un colega cometió este error; el impacto fue retraso de forecast 1 semana. La corrección inmediata fue actualizar Customer 360 y notificar al manager.

### Error 21 — Ignorar políticas ABAC configuradas por Admin

| | |
|---|---|
| **Consecuencia** | Pérdida de tiempo, forecast falso, riesgo de churn o incumplimiento. |
| **Prevención** | Checklist diario, mentoría día 1-7, usar plantillas del rol. |
| **Corrección** | Actualizar registro hoy; documentar lección en nota del cliente/deal. |

**Ejemplo en TechSolutions Panamá:** Un colega cometió este error; el impacto fue retraso de forecast 1 semana. La corrección inmediata fue actualizar Customer 360 y notificar al manager.

### Error 22 — Crear workflows duplicados para el mismo trigger

| | |
|---|---|
| **Consecuencia** | Pérdida de tiempo, forecast falso, riesgo de churn o incumplimiento. |
| **Prevención** | Checklist diario, mentoría día 1-7, usar plantillas del rol. |
| **Corrección** | Actualizar registro hoy; documentar lección en nota del cliente/deal. |

**Ejemplo en TechSolutions Panamá:** Un colega cometió este error; el impacto fue retraso de forecast 1 semana. La corrección inmediata fue actualizar Customer 360 y notificar al manager.

### Error 23 — No revisar eventos fallidos en plataforma

| | |
|---|---|
| **Consecuencia** | Pérdida de tiempo, forecast falso, riesgo de churn o incumplimiento. |
| **Prevención** | Checklist diario, mentoría día 1-7, usar plantillas del rol. |
| **Corrección** | Actualizar registro hoy; documentar lección en nota del cliente/deal. |

**Ejemplo en TechSolutions Panamá:** Un colega cometió este error; el impacto fue retraso de forecast 1 semana. La corrección inmediata fue actualizar Customer 360 y notificar al manager.

### Error 24 — Dejar usuarios inactivos con acceso activo

| | |
|---|---|
| **Consecuencia** | Pérdida de tiempo, forecast falso, riesgo de churn o incumplimiento. |
| **Prevención** | Checklist diario, mentoría día 1-7, usar plantillas del rol. |
| **Corrección** | Actualizar registro hoy; documentar lección en nota del cliente/deal. |

**Ejemplo en TechSolutions Panamá:** Un colega cometió este error; el impacto fue retraso de forecast 1 semana. La corrección inmediata fue actualizar Customer 360 y notificar al manager.

### Error 25 — No cerrar tickets resueltos en Customer Success

| | |
|---|---|
| **Consecuencia** | Pérdida de tiempo, forecast falso, riesgo de churn o incumplimiento. |
| **Prevención** | Checklist diario, mentoría día 1-7, usar plantillas del rol. |
| **Corrección** | Actualizar registro hoy; documentar lección en nota del cliente/deal. |

**Ejemplo en TechSolutions Panamá:** Un colega cometió este error; el impacto fue retraso de forecast 1 semana. La corrección inmediata fue actualizar Customer 360 y notificar al manager.

### Error 26 — Escalar todo en lugar de usar playbooks

| | |
|---|---|
| **Consecuencia** | Pérdida de tiempo, forecast falso, riesgo de churn o incumplimiento. |
| **Prevención** | Checklist diario, mentoría día 1-7, usar plantillas del rol. |
| **Corrección** | Actualizar registro hoy; documentar lección en nota del cliente/deal. |

**Ejemplo en TechSolutions Panamá:** Un colega cometió este error; el impacto fue retraso de forecast 1 semana. La corrección inmediata fue actualizar Customer 360 y notificar al manager.

### Error 27 — No ejecutar playbook correcto para el tipo de incidente

| | |
|---|---|
| **Consecuencia** | Pérdida de tiempo, forecast falso, riesgo de churn o incumplimiento. |
| **Prevención** | Checklist diario, mentoría día 1-7, usar plantillas del rol. |
| **Corrección** | Actualizar registro hoy; documentar lección en nota del cliente/deal. |

**Ejemplo en TechSolutions Panamá:** Un colega cometió este error; el impacto fue retraso de forecast 1 semana. La corrección inmediata fue actualizar Customer 360 y notificar al manager.

### Error 28 — Perder SLA de primera respuesta en tickets P1

| | |
|---|---|
| **Consecuencia** | Pérdida de tiempo, forecast falso, riesgo de churn o incumplimiento. |
| **Prevención** | Checklist diario, mentoría día 1-7, usar plantillas del rol. |
| **Corrección** | Actualizar registro hoy; documentar lección en nota del cliente/deal. |

**Ejemplo en TechSolutions Panamá:** Un colega cometió este error; el impacto fue retraso de forecast 1 semana. La corrección inmediata fue actualizar Customer 360 y notificar al manager.

### Error 29 — No identificar sponsor económico en deals B2B

| | |
|---|---|
| **Consecuencia** | Pérdida de tiempo, forecast falso, riesgo de churn o incumplimiento. |
| **Prevención** | Checklist diario, mentoría día 1-7, usar plantillas del rol. |
| **Corrección** | Actualizar registro hoy; documentar lección en nota del cliente/deal. |

**Ejemplo en TechSolutions Panamá:** Un colega cometió este error; el impacto fue retraso de forecast 1 semana. La corrección inmediata fue actualizar Customer 360 y notificar al manager.

### Error 30 — Trabajar deals fuera del pipeline oficial

| | |
|---|---|
| **Consecuencia** | Pérdida de tiempo, forecast falso, riesgo de churn o incumplimiento. |
| **Prevención** | Checklist diario, mentoría día 1-7, usar plantillas del rol. |
| **Corrección** | Actualizar registro hoy; documentar lección en nota del cliente/deal. |

**Ejemplo en TechSolutions Panamá:** Un colega cometió este error; el impacto fue retraso de forecast 1 semana. La corrección inmediata fue actualizar Customer 360 y notificar al manager.

### Error 31 — No segmentar leads por fuente/campaña

| | |
|---|---|
| **Consecuencia** | Pérdida de tiempo, forecast falso, riesgo de churn o incumplimiento. |
| **Prevención** | Checklist diario, mentoría día 1-7, usar plantillas del rol. |
| **Corrección** | Actualizar registro hoy; documentar lección en nota del cliente/deal. |

**Ejemplo en TechSolutions Panamá:** Un colega cometió este error; el impacto fue retraso de forecast 1 semana. La corrección inmediata fue actualizar Customer 360 y notificar al manager.

### Error 32 — Importar CSV sin validar formato

| | |
|---|---|
| **Consecuencia** | Pérdida de tiempo, forecast falso, riesgo de churn o incumplimiento. |
| **Prevención** | Checklist diario, mentoría día 1-7, usar plantillas del rol. |
| **Corrección** | Actualizar registro hoy; documentar lección en nota del cliente/deal. |

**Ejemplo en TechSolutions Panamá:** Un colega cometió este error; el impacto fue retraso de forecast 1 semana. La corrección inmediata fue actualizar Customer 360 y notificar al manager.

### Error 33 — Borrar registros en lugar de marcar Lost/Inactivo

| | |
|---|---|
| **Consecuencia** | Pérdida de tiempo, forecast falso, riesgo de churn o incumplimiento. |
| **Prevención** | Checklist diario, mentoría día 1-7, usar plantillas del rol. |
| **Corrección** | Actualizar registro hoy; documentar lección en nota del cliente/deal. |

**Ejemplo en TechSolutions Panamá:** Un colega cometió este error; el impacto fue retraso de forecast 1 semana. La corrección inmediata fue actualizar Customer 360 y notificar al manager.

### Error 34 — No usar filtros guardados en listas grandes

| | |
|---|---|
| **Consecuencia** | Pérdida de tiempo, forecast falso, riesgo de churn o incumplimiento. |
| **Prevención** | Checklist diario, mentoría día 1-7, usar plantillas del rol. |
| **Corrección** | Actualizar registro hoy; documentar lección en nota del cliente/deal. |

**Ejemplo en TechSolutions Panamá:** Un colega cometió este error; el impacto fue retraso de forecast 1 semana. La corrección inmediata fue actualizar Customer 360 y notificar al manager.

### Error 35 — Ignorar señales de churn en Customer Success OS

| | |
|---|---|
| **Consecuencia** | Pérdida de tiempo, forecast falso, riesgo de churn o incumplimiento. |
| **Prevención** | Checklist diario, mentoría día 1-7, usar plantillas del rol. |
| **Corrección** | Actualizar registro hoy; documentar lección en nota del cliente/deal. |

**Ejemplo en TechSolutions Panamá:** Un colega cometió este error; el impacto fue retraso de forecast 1 semana. La corrección inmediata fue actualizar Customer 360 y notificar al manager.

### Error 36 — No preparar QBR con datos de Revenue OS

| | |
|---|---|
| **Consecuencia** | Pérdida de tiempo, forecast falso, riesgo de churn o incumplimiento. |
| **Prevención** | Checklist diario, mentoría día 1-7, usar plantillas del rol. |
| **Corrección** | Actualizar registro hoy; documentar lección en nota del cliente/deal. |

**Ejemplo en TechSolutions Panamá:** Un colega cometió este error; el impacto fue retraso de forecast 1 semana. La corrección inmediata fue actualizar Customer 360 y notificar al manager.

### Error 37 — Confundir MRR con valor total del deal

| | |
|---|---|
| **Consecuencia** | Pérdida de tiempo, forecast falso, riesgo de churn o incumplimiento. |
| **Prevención** | Checklist diario, mentoría día 1-7, usar plantillas del rol. |
| **Corrección** | Actualizar registro hoy; documentar lección en nota del cliente/deal. |

**Ejemplo en TechSolutions Panamá:** Un colega cometió este error; el impacto fue retraso de forecast 1 semana. La corrección inmediata fue actualizar Customer 360 y notificar al manager.

### Error 38 — No alinear fecha cierre con ciclo de compra del cliente

| | |
|---|---|
| **Consecuencia** | Pérdida de tiempo, forecast falso, riesgo de churn o incumplimiento. |
| **Prevención** | Checklist diario, mentoría día 1-7, usar plantillas del rol. |
| **Corrección** | Actualizar registro hoy; documentar lección en nota del cliente/deal. |

**Ejemplo en TechSolutions Panamá:** Un colega cometió este error; el impacto fue retraso de forecast 1 semana. La corrección inmediata fue actualizar Customer 360 y notificar al manager.

### Error 39 — Dejar campos obligatorios vacíos para ir rápido

| | |
|---|---|
| **Consecuencia** | Pérdida de tiempo, forecast falso, riesgo de churn o incumplimiento. |
| **Prevención** | Checklist diario, mentoría día 1-7, usar plantillas del rol. |
| **Corrección** | Actualizar registro hoy; documentar lección en nota del cliente/deal. |

**Ejemplo en TechSolutions Panamá:** Un colega cometió este error; el impacto fue retraso de forecast 1 semana. La corrección inmediata fue actualizar Customer 360 y notificar al manager.

### Error 40 — No sincronizar actividades con calendario del equipo

| | |
|---|---|
| **Consecuencia** | Pérdida de tiempo, forecast falso, riesgo de churn o incumplimiento. |
| **Prevención** | Checklist diario, mentoría día 1-7, usar plantillas del rol. |
| **Corrección** | Actualizar registro hoy; documentar lección en nota del cliente/deal. |

**Ejemplo en TechSolutions Panamá:** Un colega cometió este error; el impacto fue retraso de forecast 1 semana. La corrección inmediata fue actualizar Customer 360 y notificar al manager.

### Error 41 — Revisar solo email y olvidar Command Center

| | |
|---|---|
| **Consecuencia** | Pérdida de tiempo, forecast falso, riesgo de churn o incumplimiento. |
| **Prevención** | Checklist diario, mentoría día 1-7, usar plantillas del rol. |
| **Corrección** | Actualizar registro hoy; documentar lección en nota del cliente/deal. |

**Ejemplo en TechSolutions Panamá:** Un colega cometió este error; el impacto fue retraso de forecast 1 semana. La corrección inmediata fue actualizar Customer 360 y notificar al manager.

### Error 42 — No usar búsqueda global Ctrl+K

| | |
|---|---|
| **Consecuencia** | Pérdida de tiempo, forecast falso, riesgo de churn o incumplimiento. |
| **Prevención** | Checklist diario, mentoría día 1-7, usar plantillas del rol. |
| **Corrección** | Actualizar registro hoy; documentar lección en nota del cliente/deal. |

**Ejemplo en TechSolutions Panamá:** Un colega cometió este error; el impacto fue retraso de forecast 1 semana. La corrección inmediata fue actualizar Customer 360 y notificar al manager.

### Error 43 — Crear tareas genéricas sin vínculo a registro

| | |
|---|---|
| **Consecuencia** | Pérdida de tiempo, forecast falso, riesgo de churn o incumplimiento. |
| **Prevención** | Checklist diario, mentoría día 1-7, usar plantillas del rol. |
| **Corrección** | Actualizar registro hoy; documentar lección en nota del cliente/deal. |

**Ejemplo en TechSolutions Panamá:** Un colega cometió este error; el impacto fue retraso de forecast 1 semana. La corrección inmediata fue actualizar Customer 360 y notificar al manager.

### Error 44 — No nombrar deals con convención del equipo

| | |
|---|---|
| **Consecuencia** | Pérdida de tiempo, forecast falso, riesgo de churn o incumplimiento. |
| **Prevención** | Checklist diario, mentoría día 1-7, usar plantillas del rol. |
| **Corrección** | Actualizar registro hoy; documentar lección en nota del cliente/deal. |

**Ejemplo en TechSolutions Panamá:** Un colega cometió este error; el impacto fue retraso de forecast 1 semana. La corrección inmediata fue actualizar Customer 360 y notificar al manager.

### Error 45 — Ignorar capacitación de certificación operativa

| | |
|---|---|
| **Consecuencia** | Pérdida de tiempo, forecast falso, riesgo de churn o incumplimiento. |
| **Prevención** | Checklist diario, mentoría día 1-7, usar plantillas del rol. |
| **Corrección** | Actualizar registro hoy; documentar lección en nota del cliente/deal. |

**Ejemplo en TechSolutions Panamá:** Un colega cometió este error; el impacto fue retraso de forecast 1 semana. La corrección inmediata fue actualizar Customer 360 y notificar al manager.

### Error 46 — Operar en producción sin haber pasado examen del rol

| | |
|---|---|
| **Consecuencia** | Pérdida de tiempo, forecast falso, riesgo de churn o incumplimiento. |
| **Prevención** | Checklist diario, mentoría día 1-7, usar plantillas del rol. |
| **Corrección** | Actualizar registro hoy; documentar lección en nota del cliente/deal. |

**Ejemplo en TechSolutions Panamá:** Un colega cometió este error; el impacto fue retraso de forecast 1 semana. La corrección inmediata fue actualizar Customer 360 y notificar al manager.

### Error 47 — No reportar bugs o fricción UX a Admin

| | |
|---|---|
| **Consecuencia** | Pérdida de tiempo, forecast falso, riesgo de churn o incumplimiento. |
| **Prevención** | Checklist diario, mentoría día 1-7, usar plantillas del rol. |
| **Corrección** | Actualizar registro hoy; documentar lección en nota del cliente/deal. |

**Ejemplo en TechSolutions Panamá:** Un colega cometió este error; el impacto fue retraso de forecast 1 semana. La corrección inmediata fue actualizar Customer 360 y notificar al manager.

### Error 48 — Asumir permisos de otro rol sin verificar

| | |
|---|---|
| **Consecuencia** | Pérdida de tiempo, forecast falso, riesgo de churn o incumplimiento. |
| **Prevención** | Checklist diario, mentoría día 1-7, usar plantillas del rol. |
| **Corrección** | Actualizar registro hoy; documentar lección en nota del cliente/deal. |

**Ejemplo en TechSolutions Panamá:** Un colega cometió este error; el impacto fue retraso de forecast 1 semana. La corrección inmediata fue actualizar Customer 360 y notificar al manager.

### Error 49 — No leer Quick Start impreso del primer día

| | |
|---|---|
| **Consecuencia** | Pérdida de tiempo, forecast falso, riesgo de churn o incumplimiento. |
| **Prevención** | Checklist diario, mentoría día 1-7, usar plantillas del rol. |
| **Corrección** | Actualizar registro hoy; documentar lección en nota del cliente/deal. |

**Ejemplo en TechSolutions Panamá:** Un colega cometió este error; el impacto fue retraso de forecast 1 semana. La corrección inmediata fue actualizar Customer 360 y notificar al manager.

### Error 50 — Mezclar datos de prueba con producción en demos

| | |
|---|---|
| **Consecuencia** | Pérdida de tiempo, forecast falso, riesgo de churn o incumplimiento. |
| **Prevención** | Checklist diario, mentoría día 1-7, usar plantillas del rol. |
| **Corrección** | Actualizar registro hoy; documentar lección en nota del cliente/deal. |

**Ejemplo en TechSolutions Panamá:** Un colega cometió este error; el impacto fue retraso de forecast 1 semana. La corrección inmediata fue actualizar Customer 360 y notificar al manager.

### Ejercicios Capítulo 5

- Auto-evaluación: ¿cuántos de los 50 errores cometiste la semana pasada?
- Crea tu checklist personal de 10 errores a evitar.
- Role-play: compañero simula error; tú aplicas prevención y corrección.

---

# Capítulo 6 — Indicadores de desempeño

## KPIs de tu rol

| KPI | Meta sugerida | Dónde medir | Alerta si... |
|-----|---------------|-------------|--------------|
| CSAT | Definir con manager | Command / Revenue / CS | Tendencia 2 semanas negativa |
| Tiempo primera respuesta | Definir con manager | Command / Revenue / CS | Tendencia 2 semanas negativa |
| Tiempo resolución | Definir con manager | Command / Revenue / CS | Tendencia 2 semanas negativa |
| Churn rate | Definir con manager | Command / Revenue / CS | Tendencia 2 semanas negativa |
| Renovaciones a tiempo | Definir con manager | Command / Revenue / CS | Tendencia 2 semanas negativa |
| NPS | Definir con manager | Command / Revenue / CS | Tendencia 2 semanas negativa |

## Interpretación ejecutiva

```mermaid
flowchart TD
    K[KPI en rojo] --> D{Diagnóstico}
    D -->|Datos| FIX[Calidad de registro]
    D -->|Proceso| TRN[Capacitación]
    D -->|Mercado| STR[Estrategia]
    FIX --> P[Plan 30 días]
    TRN --> P
    STR --> P
```

### Profundización por KPI

#### CSAT

**Definición de negocio:** Métrica acordada con tu manager que refleja contribución del rol.

**Ritual de medición:** Revisar al cierre de jornada (17:30) y en stand-up semanal.

**Acción si está en rojo:** Diagnóstico en 24h; plan de mejora documentado en tarea vinculada.

**Pregunta para tu manager:** ¿Cuál es la meta numérica este trimestre?

#### Tiempo primera respuesta

**Definición de negocio:** Métrica acordada con tu manager que refleja contribución del rol.

**Ritual de medición:** Revisar al cierre de jornada (17:30) y en stand-up semanal.

**Acción si está en rojo:** Diagnóstico en 24h; plan de mejora documentado en tarea vinculada.

**Pregunta para tu manager:** ¿Cuál es la meta numérica este trimestre?

#### Tiempo resolución

**Definición de negocio:** Métrica acordada con tu manager que refleja contribución del rol.

**Ritual de medición:** Revisar al cierre de jornada (17:30) y en stand-up semanal.

**Acción si está en rojo:** Diagnóstico en 24h; plan de mejora documentado en tarea vinculada.

**Pregunta para tu manager:** ¿Cuál es la meta numérica este trimestre?

#### Churn rate

**Definición de negocio:** Métrica acordada con tu manager que refleja contribución del rol.

**Ritual de medición:** Revisar al cierre de jornada (17:30) y en stand-up semanal.

**Acción si está en rojo:** Diagnóstico en 24h; plan de mejora documentado en tarea vinculada.

**Pregunta para tu manager:** ¿Cuál es la meta numérica este trimestre?

#### Renovaciones a tiempo

**Definición de negocio:** Métrica acordada con tu manager que refleja contribución del rol.

**Ritual de medición:** Revisar al cierre de jornada (17:30) y en stand-up semanal.

**Acción si está en rojo:** Diagnóstico en 24h; plan de mejora documentado en tarea vinculada.

**Pregunta para tu manager:** ¿Cuál es la meta numérica este trimestre?

#### NPS

**Definición de negocio:** Métrica acordada con tu manager que refleja contribución del rol.

**Ritual de medición:** Revisar al cierre de jornada (17:30) y en stand-up semanal.

**Acción si está en rojo:** Diagnóstico en 24h; plan de mejora documentado en tarea vinculada.

**Pregunta para tu manager:** ¿Cuál es la meta numérica este trimestre?

### Plan de mejora personal (plantilla)

1. KPI más débil esta semana: ___________
2. Causa raíz probable: ___________
3. Acción concreta mañana: ___________
4. Fecha de revisión con manager: ___________

### Ejercicios Capítulo 6

- Define meta numérica para tu KPI #1 con tu manager.
- Identifica en qué pantalla medirás cada KPI esta semana.
- Redacta plan de mejora 30 días para tu KPI más débil.

---

# Capítulo 7 — Uso de IA en tu rol

## IA para usuarios de negocio (sin tecnicismos)

AutonomusCRM integra IA como **copiloto operativo**: detecta riesgos, sugiere acciones y automatiza tareas repetitivas. **Tú** conservas la decisión final en Trust Studio.

| Capacidad IA | Beneficio | Riesgo si se ignora | Riesgo si se abusa |
|--------------|-----------|---------------------|---------------------|
| Detección deals en riesgo | Salvar ingresos | Churn de pipeline | Falsos positivos sin revisión |
| Sugerencias Workforce | Ahorro de tiempo | Burnout manual | Automatizar sin política |
| Aprobación HITL | Control y cumplimiento | Decisiones no auditadas | Cuello de botella si no revisas |

### Escenario IA — Clasificación ticket

IA sugiere severidad P2 vs P1. Validas síntomas con playbook.

**Tu decisión:** Aprobar · Rechazar · Escalar — siempre con nota de negocio.

### Escenario IA — Riesgo churn

Alerta health score rojo. Ejecutas playbook antes de escalar.

**Tu decisión:** Aprobar · Rechazar · Escalar — siempre con nota de negocio.

### Escenario IA 7.1 — Command Center

El Command Center muestra *3 decisiones pendientes*. Abres Trust Studio, lees el contexto de cada una, apruebas 2 y rechazas 1 porque contradice política comercial. **Eso es operación enterprise con IA responsable.**

```mermaid
sequenceDiagram
    participant CC as Command Center
    participant TS as Trust Studio
    participant U as Tú
    CC->>U: Alerta decisión pendiente
    U->>TS: Revisar contexto
    U->>TS: Aprobar o Rechazar
    TS->>CC: Estado actualizado
```

### Ejercicios Capítulo 7

- Lista 3 tareas delegables a IA y 3 que nunca delegarías.
- Procesa 5 decisiones en Trust Studio (entorno QA) con nota de criterio.
- Discute con manager: ¿dónde está el límite de automatización en tu rol?

---

# Capítulo 8 — Certificación operativa

## Checklist de competencias

- [ ] Inicio de sesión y navegación sin ayuda
- [ ] Explicar Command Center en lenguaje de negocio
- [ ] Resolver ticket usando playbook correcto
- [ ] Escalar incidente P1 en <30 min
- [ ] Usar Customer 360 antes de reunión simulada
- [ ] Completar jornada tipo Capítulo 3 en entorno de práctica
- [ ] Aprobar/rechazar decisión IA con criterio
- [ ] Identificar 10 errores del Capítulo 5 en simulación

## Criterios de aprobación

| Componente | Peso | Mínimo |
|------------|------|--------|
| Examen teórico | 30% | 80% |
| Casos prácticos | 40% | 3/4 aprobados |
| Checklist operativo | 20% | 100% ítems críticos |
| Observación manager | 10% | Satisfactorio |

**Examen completo:** ver `ROLE_CERTIFICATION_EXAMS.md` — sección **Especialista de Soporte y Éxito del Cliente**.

### Ejercicios Capítulo 8

- Completa checklist competencias al 100% ítems críticos.
- Simula examen: responde 10 preguntas muestra sin consultar guía.
- Solicita observación manager en operación real 2h.

### FAQ de certificación

**¿Puedo reintentar el examen?** Sí, tras 5 días y revisión con mentor.

**¿El entorno QA es el mismo que producción?** Misma interfaz; datos de práctica.

**¿Contraseña práctica?** `AutonomusTest123!`

---

*AutonomusCRM Enterprise Academy — Especialista de Soporte y Éxito del Cliente — Documento generado para capacitación operativa.*
