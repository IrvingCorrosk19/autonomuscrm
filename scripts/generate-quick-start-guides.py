#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Genera QUICK_START_GUIDES.md — mini-cursos operativos estilo Trailhead."""

from __future__ import annotations

from pathlib import Path
from textwrap import dedent

ROOT = Path(__file__).resolve().parent.parent
OUT_PATH = ROOT / "Documentation" / "University" / "QUICK_START_GUIDES.md"
QA_URL = "http://164.68.99.83:8091"
COMPANY = "TechSolutions Panamá"

MODULE_ORDER = [
    "Leads", "Deals", "Customers", "Customer360", "Tasks",
    "RevenueOS", "ExecutiveOS", "TrustStudio", "CustomerSuccess",
    "Agents", "Users", "Policies", "Audit",
]

MODULE_META = {
    "Leads": {"route": "/Leads", "create": "/Leads/Create", "detail": "/Leads/Details", "role": "vendedor", "time": "45–60 min"},
    "Deals": {"route": "/Deals", "create": "/Deals/Create", "detail": "/Deals/Details", "role": "vendedor o gerente", "time": "50–65 min"},
    "Customers": {"route": "/Customers", "create": "/Customers/Create", "detail": "/Customers/Details", "role": "vendedor o CS", "time": "40–55 min"},
    "Customer360": {"route": "/Customer360", "create": None, "detail": "/customers/{id}/360", "role": "todos los roles", "time": "60–90 min"},
    "Tasks": {"route": "/Tasks", "create": "/Tasks", "detail": None, "role": "todos", "time": "35–45 min"},
    "RevenueOS": {"route": "/revenue", "create": None, "detail": None, "role": "gerente comercial", "time": "50–70 min"},
    "ExecutiveOS": {"route": "/executive", "create": None, "detail": None, "role": "CEO / dirección", "time": "45–60 min"},
    "TrustStudio": {"route": "/TrustInbox", "create": None, "detail": None, "role": "manager, admin, ventas", "time": "45–55 min"},
    "CustomerSuccess": {"route": "/customer-success", "create": None, "detail": None, "role": "soporte y CS", "time": "55–70 min"},
    "Agents": {"route": "/Agents", "create": None, "detail": None, "role": "admin y managers", "time": "40–50 min"},
    "Users": {"route": "/Users", "create": "/Users", "detail": None, "role": "administrador", "time": "45–55 min"},
    "Policies": {"route": "/Policies", "create": "/Policies", "detail": None, "role": "administrador", "time": "40–50 min"},
    "Audit": {"route": "/Audit", "create": None, "detail": None, "role": "admin y compliance", "time": "35–45 min"},
}

# fmt: off
MODULES: dict[str, dict] = {}
# fmt: on


def _reg(
    key: str,
    problem: str,
    economic: str,
    if_not: str,
    when: list[str],
    story: str,
    steps: list[tuple[str, str, str, str]],
    captures: list[tuple[str, str]],
    chain: str,
    errors: list[str],
    best: list[str],
    checklist: list[str],
    exercise: str,
    quiz: list[tuple[str, list[str], int]],
    cert: str,
    extra: str = "",
) -> None:
    assert len(errors) == 20, f"{key}: errors={len(errors)}"
    assert len(best) == 20, f"{key}: best={len(best)}"
    assert len(quiz) >= 5, f"{key}: quiz={len(quiz)}"
    MODULES[key] = {
        "problem": problem, "economic": economic, "if_not": if_not,
        "when": when, "story": story, "steps": steps, "captures": captures,
        "chain": chain, "errors": errors, "best": best, "checklist": checklist,
        "exercise": exercise, "quiz": quiz, "cert": cert, "extra": extra,
    }


# ─── LEADS ───────────────────────────────────────────────────────────────────
_reg(
    "Leads",
    "Las oportunidades llegan dispersas (web, ferias, referidos) y se pierden en email o WhatsApp.",
    "Cada lead sin contactar en 24 h reduce hasta 80 % la probabilidad de conversión. En B2B panameño, un lead perdido puede representar $15 K–$150 K.",
    "Ventas trabaja a ciegas, marketing no puede medir ROI y gerencia no puede proyectar ingresos.",
    [
        "Llega un formulario web (como el de Juan en nuestra historia).",
        "Alguien te pasa un contacto de feria o LinkedIn.",
        "Un cliente actual te refiere a otra empresa.",
        "Importas una lista post-evento.",
        "Necesitas priorizar a quién llamar hoy.",
    ],
    """**Martes 9:15 AM — TechSolutions Panamá**

Juan Morales, gerente de operaciones en **RetailMax**, descargó el whitepaper «Cloud seguro para retail» en tu web. El sistema creó automáticamente un lead con fuente **Web**.

**Carlos**, vendedor junior contratado ayer (nunca usó un CRM), debe:
1. Ver el lead en `/Leads`
2. Llamar a Juan antes de las 10:00
3. Registrar qué necesita
4. Decidir si califica

Si Carlos sigue esta guía, a las 11:00 Juan estará calificado y con un deal de $42 K en Discovery.""",
    [
        ("PASO 1 — Iniciar sesión y abrir Leads", f"Ir a {QA_URL}/Account/Login → menú **Leads** (`/Leads`).", "Ves la lista con filtros y botón «Nuevo lead».", "Estás autenticado; ves columnas Nombre, Estado, Fuente."),
        ("PASO 2 — Localizar el lead de Juan", "Filtrar por hoy o buscar «RetailMax» / «Juan».", "Aparece el registro con fuente Web.", "Email y empresa coinciden con el formulario."),
        ("PASO 3 — Abrir detalle y registrar primer contacto", "Clic en el lead → agregar nota de llamada: duración, dolor, próximo paso.", "La nota queda en historial.", "Nota con fecha, no vacía."),
        ("PASO 4 — Calificar con criterio BANT", "Budget, Authority, Need, Timeline — botón **Qualify** si cumple.", "Estado cambia a Calificado.", "Al menos 3 de 4 criterios documentados."),
        ("PASO 5 — Crear oportunidad o nurture", "Si califica: **Crear Deal** o **Convert to Customer** según proceso.", "Deal en pipeline o tarea de seguimiento.", "Deal vinculado al lead; no duplicar cliente."),
        ("PASO 6 — Cerrar el ciclo del día", "Revisar que no queden leads «Nuevo» sin tocar.", "Bandeja limpia o con tareas futuras.", "Cada lead tiene estado distinto de Nuevo o tarea asignada."),
    ],
    [
        ("CAPTURA 01", "Pantalla principal Leads — lista con filtros y métricas"),
        ("CAPTURA 02", "Formulario Crear Lead (`/Leads/Create`) con campos obligatorios"),
        ("CAPTURA 03", "Detalle del lead Juan Morales — notas y timeline"),
        ("CAPTURA 04", "Modal o acción Qualify / Convert to Customer"),
        ("CAPTURA 05", "Deal creado desde lead — vínculo visible"),
    ],
    "Lead (Juan) → Contacto <24 h → Calificado → Deal Discovery → Propuesta → Won → Customer → CS Onboarding → Renewal",
    [
        "No contactar lead inbound en las primeras 24 horas",
        "Dejar el lead en estado «Nuevo» sin ninguna nota",
        "Calificar sin hablar con el prospecto",
        "Crear cliente duplicado sin buscar en Directorio",
        "No registrar la fuente o campaña del lead",
        "Perder el email o teléfono en notas sueltas fuera del CRM",
        "Asignar lead a vendedor equivocado sin reasignar en sistema",
        "Borrar lead en lugar de marcar descalificado",
        "Crear deal gigante sin etapa Discovery",
        "No usar BANT en cuentas enterprise",
        "Ignorar leads de referido (alta conversión)",
        "Importar CSV sin validar duplicados",
        "No crear tarea de seguimiento tras la llamada",
        "Prometer precio sin deal ni aprobación",
        "Mezclar leads de prueba con producción",
        "No actualizar estado tras cada touch",
        "Dejar campos obligatorios vacíos «para ir rápido»",
        "No vincular lead a campaña de marketing",
        "Perseguir leads no calificados por ego",
        "Olvidar convertir lead antes del cierre formal",
    ],
    [
        "Regla de oro: primer contacto en <24 h (ideal <1 h en inbound caliente)",
        "Cada llamada termina con nota en el CRM el mismo día",
        "Un lead = una fuente registrada para medir marketing",
        "Buscar en Customers antes de crear registro nuevo",
        "Qualify solo con evidencia en notas",
        "Crear tarea con fecha para el próximo paso",
        "Usar convención de nombres: Empresa — Contacto — Campaña",
        "Revisar Command Center por leads sin actividad",
        "Handoff claro si el lead es de otro territorio",
        "Descalificar con razón (presupuesto, timing) para nurture",
        "Vincular deal al lead, no crear oportunidad huérfana",
        "Practicar en University antes del primer lead real",
        "Ctrl+K para saltar rápido al lead del día",
        "Revisar bandeja Leads al iniciar la jornada",
        "Coordinar con marketing la definición de MQL",
        "No mezclar idiomas en notas sin etiqueta",
        "Export solo con autorización de datos",
        "Celebrar wins pero documentar losses",
        "Pedir introducción al decisor en cuentas B2B",
        "Cerrar el día con cero leads «Nuevo» sin tocar",
    ],
    [
        "Registré cada contacto en notas",
        "Actualicé estado del lead",
        "Creé tarea de seguimiento",
        "Verifiqué duplicados en Customers",
        "Si califica: deal o conversión iniciada",
        "Fuente/campaña documentada",
        "Supervisor revisó mi primer lead real",
    ],
    f"En entorno QA ({QA_URL}): crear lead «Prueba Academy — RetailMax», simular llamada, nota BANT, calificar, crear deal $10 K Discovery.",
    [
        ("¿Cuánto tiempo máximo recomendado para primer contacto inbound?", ["24 horas", "1 semana", "Cuando haya tiempo", "No importa"], 0),
        ("¿Qué significa calificar un lead?", ["Validar si hay oportunidad real", "Cambiar color", "Borrarlo", "Enviar spam"], 0),
        ("Juan de RetailMax llegó por web. ¿Primera pantalla?", ["/Leads", "/Billing", "/Audit", "/Policies"], 0),
        ("¿Qué hacer antes de crear Customer nuevo?", ["Buscar en Directorio", "Borrar lead", "Nada", "Crear duplicado"], 0),
        ("Lead calificado sin deal. ¿Riesgo principal?", ["Pipeline invisible para gerencia", "Ninguno", "Mejor así", "Marketing feliz"], 0),
        ("¿Qué es BANT?", ["Budget, Authority, Need, Timeline", "Un tipo de lead", "Un reporte", "Una política"], 0),
    ],
    "Completar ejercicio QA + 5/6 evaluación + supervisor valida primer lead real en <24 h → badge **Lead Hunter**.",
)

# ─── DEALS ───────────────────────────────────────────────────────────────────
_reg(
    "Deals",
    "Las oportunidades se gestionan en hojas de cálculo sin visibilidad de etapa, probabilidad ni fecha de cierre.",
    "Un pipeline opaco hace que el forecast falle un 30–40 % y que deals estancados consuman tiempo sin retorno.",
    "Gerencia promete ingresos que no existen; vendedores persiguen deals muertos; CS recibe clientes sin contexto.",
    [
        "Tienes una propuesta enviada y necesitas avanzar etapa.",
        "Un deal lleva 14 días sin actividad.",
        "Debes decidir si marcar Won o Lost.",
        "Gerente pide revisión de pipeline del trimestre.",
        "Cliente pide descuento antes de firmar.",
    ],
    """**Jueves 14:00 — TechSolutions Panamá**

**María**, account executive, tiene un deal de **$85 K** con **Banco Regional** en etapa Propuesta desde hace 10 días. El CFO pidió una reunión el lunes.

Su manager **Luis** ve en Revenue OS que el deal tiene probabilidad 70 % pero sin actividad registrada — señal de riesgo.

María abre `/Deals`, actualiza la nota de la llamada con el CFO, baja probabilidad a 50 % (honestidad), crea tarea «Enviar ROI revisado» para mañana y mueve a Negociación. El forecast del equipo mejora porque refleja la realidad.""",
    [
        ("PASO 1 — Abrir pipeline", f"Login → **Deals** (`/Deals`).", "Vista kanban o lista por etapa.", "Ves deals propios o del equipo según rol."),
        ("PASO 2 — Localizar deal Banco Regional", "Filtrar por cuenta o buscar «Banco Regional».", "Deal visible con etapa y valor.", "Valor $85 K y etapa coherentes."),
        ("PASO 3 — Revisar actividad y riesgo", "Abrir detalle → leer notas, señales IA si aparecen.", "Identificas días sin touch.", "Última nota <7 días o justificación."),
        ("PASO 4 — Registrar interacción y ajustar probabilidad", "Nueva nota post-llamada CFO; probabilidad alineada a etapa.", "Historial actualizado.", "Probabilidad no inflada vs etapa."),
        ("PASO 5 — Definir próximo paso", "Crear tarea vinculada con fecha.", "Tarea en `/Tasks`.", "Fecha antes del cierre esperado."),
        ("PASO 6 — Avanzar etapa o marcar Lost", "Mover a Negociación, Won (con evidencia) o Lost con razón.", "Pipeline refleja verdad.", "Lost siempre con motivo documentado."),
    ],
    [
        ("CAPTURA 01", "Lista/kanban Deals con etapas del pipeline"),
        ("CAPTURA 02", "Detalle deal Banco Regional — valor, etapa, probabilidad"),
        ("CAPTURA 03", "Panel de notas e historial de actividad"),
        ("CAPTURA 04", "Cambio de etapa y ajuste de probabilidad"),
        ("CAPTURA 05", "Deal Won con fecha de cierre y handoff a CS"),
        ("CAPTURA 06", "Deal Lost con razón documentada"),
    ],
    "Lead calificado → Deal Discovery → Propuesta → Negociación → Won → Customer → Implementación → Renewal",
    [
        "Mantener probabilidad 90 % en Discovery",
        "Avanzar etapa sin actividad registrada",
        "Marcar Won sin contrato o PO",
        "No documentar razón al perder",
        "Crear deal sin vincular a cuenta/lead",
        "Duplicar deal para la misma oportunidad",
        "Ignorar alertas de deal en riesgo",
        "Prometer fecha de cierre irreal cada semana",
        "No involucrar al decisor económico",
        "Dejar deals zombi más de 30 días",
        "Cambiar owner sin notificar al cliente",
        "No actualizar valor tras cambio de alcance",
        "Mezclar monedas sin conversión clara",
        "Cerrar deal sin handoff a CS",
        "Usar etapa Propuesta sin documento enviado",
        "No revisar competencia en notas",
        "Forecast personal sin revisar con manager",
        "Split deals para inflar métricas",
        "Olvidar vincular productos o líneas",
        "Perder deal por no registrar objeciones",
    ],
    [
        "Probabilidad honesta por etapa — regla del equipo",
        "Cada reunión termina con nota y próximo paso",
        "Revisar deals en riesgo cada mañana en Command",
        "Lost con razón = aprendizaje para el equipo",
        "Won solo con evidencia (contrato, PO)",
        "Handoff escrito a CS el día del cierre",
        "Actualizar valor cuando cambia el alcance",
        "Involucrar champion y decisor en notas",
        "Pipeline review semanal con manager",
        "Usar Customer 360 antes de negociación enterprise",
        "Descuento solo con política y aprobación",
        "Fecha de cierre = compromiso del cliente, no deseo",
        "Un deal = una oportunidad clara",
        "Registrar competidor en cada deal grande",
        "Reactivar deals estancados con plan, no esperanza",
        "Vincular tareas a cada deal activo",
        "Celebrar Won en equipo — documentar cómo",
        "Trimestre nuevo = limpiar pipeline",
        "Practicar escenario Lost en University",
        "Ctrl+K para saltar al deal del día",
    ],
    [
        "Revisé todos mis deals activos",
        "Cada deal tiene nota <7 días o plan",
        "Probabilidades alineadas a etapa",
        "Próximo paso con tarea fechada",
        "Deals Lost/Won actualizados esta semana",
        "Handoff documentado en deals Won",
    ],
    f"En QA: localizar o crear deal «Academy — Banco Test» $25 K, registrar nota, ajustar probabilidad, crear tarea, mover etapa.",
    [
        ("Deal en Discovery con 90 % probabilidad es:", ["Error común", "Buena práctica", "Obligatorio", "Recomendado"], 0),
        ("Al marcar deal Lost debes:", ["Documentar razón", "Borrar cliente", "Ocultar registro", "Nada"], 0),
        ("¿Dónde ves el pipeline completo?", ["/Deals", "/Audit", "/Users", "/Policies"], 0),
        ("Won sin contrato genera:", ["Problemas en forecast y CS", "Nada", "Más comisión segura", "Menos trabajo"], 0),
        ("Deal 14 días sin actividad. ¿Primera acción?", ["Llamar y registrar nota", "Borrar", "Subir probabilidad", "Ignorar"], 0),
        ("Handoff a CS debe incluir:", ["Contexto y expectativas", "Solo nombre", "Password", "Nada"], 0),
    ],
    "Ejercicio QA + 5/6 evaluación + manager valida pipeline review → badge **Pipeline Pro**.",
)

# ─── CUSTOMERS ───────────────────────────────────────────────────────────────
_reg(
    "Customers",
    "Los datos de clientes viven en carpetas, Excel y la memoria de cada vendedor — sin dueño ni calidad.",
    "Datos duplicados cuestan ~$15 K/año por cuenta en retrabajo; expansión y renovación fallan por contactos incorrectos.",
    "Llamas al contacto equivocado, facturas al email viejo y CS no sabe quién es el sponsor.",
    [
        "Convertiste un lead o cerraste un deal — necesitas la ficha de cliente.",
        "Buscar si ya existe antes de crear duplicado.",
        "Actualizar contacto principal tras cambio en la cuenta.",
        "Preparar visita comercial con datos correctos.",
        "Segmentar cartera para campaña de expansión.",
    ],
    """**Miércoles 8:30 AM — TechSolutions Panamá**

**Andrea** en ventas recibe un email: «Somos **Grupo Andina Logística**, nos recomendó **RetailMax**». Antes de crear nada, busca en `/Customers` «Andina».

Encuentra un registro incompleto de hace 2 años. Actualiza razón social, agrega al nuevo director de TI **Roberto Vega**, marca a RetailMax como referencia y vincula el deal de expansión $28 K. Evitó un duplicado que habría confundido a facturación.""",
    [
        ("PASO 1 — Abrir directorio", f"**Customers** (`/Customers`).", "Lista con búsqueda y filtros.", "Ves clientes activos e inactivos."),
        ("PASO 2 — Buscar antes de crear", "Buscar empresa, RUC o contacto.", "Resultados o vacío confirmado.", "Búsqueda documentada en nota si creas nuevo."),
        ("PASO 3 — Completar ficha", "Razón social, industria, owner, contactos.", "Campos críticos llenos.", "Sin campos obligatorios vacíos."),
        ("PASO 4 — Registrar contactos clave", "Decisor, champion, facturación.", "Mínimo 2 contactos en cuentas B2B.", "Emails válidos y roles claros."),
        ("PASO 5 — Vincular oportunidad", "Crear deal expansión desde la cuenta.", "Deal ligado al customer.", "No deal huérfano."),
        ("PASO 6 — Abrir 360", "Clic en vista 360 para contexto completo.", "Timeline unificado.", "Deals, tickets visibles si existen."),
    ],
    [
        ("CAPTURA 01", "Lista Customers con búsqueda global"),
        ("CAPTURA 02", "Formulario crear/editar (`/Customers/Create`)"),
        ("CAPTURA 03", "Ficha Grupo Andina — contactos y owner"),
        ("CAPTURA 04", "Deal expansión vinculado a la cuenta"),
        ("CAPTURA 05", "Enlace a Customer 360 desde la ficha"),
    ],
    "Lead → Deal Won → **Customer creado/actualizado** → Onboarding → Uso → Expansión → Renewal",
    [
        "Crear cliente sin buscar duplicados",
        "Dejar owner vacío",
        "Mezclar personas de distintas empresas en un registro",
        "No actualizar email de facturación",
        "Marcar activo a cuenta churned",
        "Usar apodos en lugar de razón social",
        "No registrar industria o segmento",
        "Contacto único sin backup",
        "Borrar cliente con historial",
        "No vincular deals a la cuenta correcta",
        "Import masivo sin reglas de deduplicación",
        "Ignorar alertas de datos incompletos",
        "Compartir export sin permiso",
        "No documentar cambio de sponsor",
        "Fusionar cuentas sin revisar 360",
        "Tratar prospecto como customer antes de Won",
        "No segmentar por tier (SMB/Enterprise)",
        "Olvidar idioma/zona horaria del cliente",
        "Datos de prueba en producción",
        "No revisar cartera asignada cada mes",
    ],
    [
        "Buscar siempre antes de crear",
        "Convención de nombres legal consistente",
        "Owner claro por cuenta",
        "Mínimo decisor + operativo en contactos",
        "Actualizar en 24 h si cambia contacto clave",
        "Revisar cuentas incompletas cada viernes",
        "Vincular cada deal a customer",
        "360 antes de visita importante",
        "Segmentar para campañas de expansión",
        "Documentar referidos y partners",
        "Offboarding de contactos que salen",
        "Tier VIP visible en notas",
        "Coordinar con finanzas el email de factura",
        "No mezclar cuentas matriz y filial sin relación",
        "University antes de primer alta masiva",
        "Ctrl+K para ir directo al cliente",
        "Health check de datos trimestral",
        "Fusionar duplicados con auditoría",
        "Celebrar expansión — registrar en 360",
        "Mentor revisa primeras 5 altas del nuevo",
    ],
    [
        "Busqué antes de crear",
        "Campos obligatorios completos",
        "Contactos decisor y operativo",
        "Owner asignado",
        "Deal vinculado si aplica",
        "360 revisado",
    ],
    f"En QA: buscar «Academy Test Corp», actualizar o crear ficha completa, agregar contacto, abrir 360.",
    [
        ("¿Primer paso ante empresa «nueva»?", ["Buscar en Customers", "Crear directo", "Borrar leads", "Ignorar"], 0),
        ("Customer sin owner implica:", ["Nadie responsable de la cuenta", "Mejor así", "Automático", "Sin impacto"], 0),
        ("¿Dónde ves deals y tickets juntos?", ["Customer 360", "Solo Leads", "Audit", "Users"], 0),
        ("Duplicados causan principalmente:", ["Retrabajo y errores", "Más ventas", "Nada", "Menos trabajo"], 0),
        ("Deal expansión debe:", ["Vincularse al customer", "Ser independiente", "Borrarse", "No registrarse"], 0),
        ("Contacto de facturación incorrecto afecta:", ["Cobranza y renovación", "Solo marketing", "Nada", "UI"], 0),
    ],
    "Ejercicio QA + evaluación + mentor valida ficha sin duplicados → badge **Account Keeper**.",
)

# ─── CUSTOMER 360 (profundidad extra) ────────────────────────────────────────
_reg(
    "Customer360",
    "Antes de cada llamada importante, el equipo no sabe qué pasó ayer: tickets abiertos, renovación en 60 días, deal en riesgo o caída de uso.",
    "Un ejecutivo sin contexto pierde 20–40 min por llamada reconstruyendo historia; en cuentas de $100 K+ ARR, un error de contexto puede costar la renovación.",
    "Prometes lo que otro equipo ya prometió, ignoras un ticket P1, llegas a la renovación sin saber el health score y el cliente siente que «no los conocen».",
    [
        "Llamada de escalamiento con cuenta VIP.",
        "Preparación de QBR o revisión de negocio.",
        "Renovación a 90, 60 o 30 días.",
        "Cliente en riesgo (health bajo, NPS malo).",
        "Antes de proponer expansión o upsell.",
        "Handoff ventas → CS o CS → ventas.",
    ],
    """**Viernes 7:45 AM — TechSolutions Panamá**

**Roberto**, CS Manager, tiene QBR a las 10:00 con **Logística del Canal** ($120 K ARR). Ayer soporte cerró un ticket P2 de integración; ventas tiene un deal de expansión $35 K; health bajó a **Ámbar** por uso irregular; renovación en **72 días**.

Roberto abre `/Customer360`, busca la cuenta, en 8 minutos recorre:
- **Timeline** — últimos 90 días de interacciones
- **Health** — factores y tendencia
- **Tickets** — uno abierto de documentación
- **Revenue** — ARR, expansión pipeline
- **Renewal** — fecha y owner
- **Risk** — señales IA y concentración
- **AI insights** — resumen ejecutivo sugerido

Llega al QBR con 3 bullets y un plan de acción. El cliente nota la diferencia.""",
    [
        ("PASO 1 — Acceder a Customer 360", f"Menú **Customer 360** (`/Customer360`) o desde ficha `/customers/{{id}}/360`.", "Buscador de cuentas y lista recientes.", "Encuentras la cuenta en <30 s."),
        ("PASO 2 — Leer Timeline (corazón del 360)", "Scroll cronológico: notas, emails, reuniones, cambios etapa.", "Historia unificada sin saltar pantallas.", "Últimos 30 días revisados; huecos identificados."),
        ("PASO 3 — Revisar Health Score", "Panel salud: adopción, tickets, pagos, engagement.", "Verde/Ámbar/Rojo con drivers.", "Entiendes *por qué* está el color, no solo el número."),
        ("PASO 4 — Tickets y escalaciones", "Abiertos, SLA, severidad, owner.", "Sabes si hay fuego antes de hablar.", "P1/P2 con plan; ningún abierto ignorado."),
        ("PASO 5 — Revenue y renovación", "ARR/MRR, deals activos, fecha renewal, cobertura expansión.", "Cuadro económico claro.", "Renewal owner y fecha en calendario mental."),
        ("PASO 6 — Risk y señales IA", "Alertas de churn, deals estancados, anomalías de uso.", "Lista priorizada de riesgos.", "Al menos un riesgo con acción asignada."),
        ("PASO 7 — Actuar y registrar", "Crear tarea, nota o escalar desde el 360.", "Próximo paso en sistema.", "Cliente ve continuidad post-llamada."),
        ("PASO 8 — Post-QBR / post-llamada", "Resumen en nota vinculada a la cuenta.", "Timeline actualizado.", "Compromisos con fecha."),
    ],
    [
        ("CAPTURA 01", "Entrada Customer 360 — búsqueda y cuentas recientes"),
        ("CAPTURA 02", "Timeline unificado — 90 días de actividad"),
        ("CAPTURA 03", "Panel Health Score con drivers"),
        ("CAPTURA 04", "Sección Tickets — abiertos y SLA"),
        ("CAPTURA 05", "Bloque Revenue — ARR y deals vinculados"),
        ("CAPTURA 06", "Widget Renewal — cuenta regresiva y owner"),
        ("CAPTURA 07", "Panel Risk / alertas"),
        ("CAPTURA 08", "Insights IA — resumen ejecutivo sugerido"),
    ],
    "Lead → Deal → Customer → **360 diario en cuentas clave** → Tickets resueltos → Health estable → Renewal ganada → Expansión",
    [
        "Llamar a cliente enterprise sin abrir 360",
        "Ignorar ticket abierto visible en el timeline",
        "No revisar fecha de renovación antes de QBR",
        "Confiar solo en memoria del vendedor",
        "Prometer funcionalidad sin leer tickets previos",
        "No actuar ante health en rojo",
        "Duplicar notas fuera del 360",
        "Olvidar deals de expansión al hablar de renovación",
        "Escalar sin documentar en la cuenta",
        "Leer solo la primera pantalla del timeline",
        "Ignorar señales IA sin validar",
        "No verificar contacto correcto antes de email masivo",
        "QBR sin métricas de valor del 360",
        "Mezclar cuentas matriz y filial en la búsqueda",
        "No registrar outcome de la reunión",
        "Asumir health verde sin mirar tendencia",
        "No coordinar con owner de renewal",
        "Perder contexto en handoff entre equipos",
        "Exportar 360 sin permiso en cuentas sensibles",
        "Cerrar ticket en cabeza pero no en sistema",
    ],
    [
        "360 antes de toda llamada >$10 K o VIP",
        "Orden fijo: Timeline → Health → Tickets → Revenue → Renewal → Risk",
        "Registrar resumen post-llamada el mismo día",
        "Compartir captura de health en war room de riesgo",
        "Vincular tareas a la cuenta desde el 360",
        "Renovación a 90 días = primer playbook en 360",
        "Cruzar health con uso real del producto",
        "Un owner por renewal visible en 360",
        "Leer insights IA como borrador, no verdad absoluta",
        "Preparar QBR con 3 métricas de valor del timeline",
        "Escalar P1 con enlace al registro en 360",
        "Ctrl+K → nombre cliente → 360",
        "Revisar cuentas ámbar cada lunes",
        "Handoff ventas-CS con nota en 360",
        "Documentar champion y decisor en contactos",
        "No prometer sin revisar compromisos previos en timeline",
        "Practicar recorrido 8 min en University",
        "Mentor observa primer QBR con checklist 360",
        "Celebrar renewal — nota en timeline",
        "Auditar cuentas sin actividad 30 días en 360",
    ],
    [
        "Abrí 360 de la cuenta objetivo",
        "Revisé timeline últimos 30 días",
        "Health y drivers entendidos",
        "Tickets abiertos verificados",
        "Renewal y revenue revisados",
        "Riesgos con acción asignada",
        "Nota post-interacción registrada",
    ],
    f"En QA: abrir 360 de cuenta demo, completar recorrido 8 min (timeline, health, tickets, revenue, renewal, risk), registrar nota QBR simulada.",
    [
        ("¿Módulo para vista unificada pre-llamada?", ["Customer 360", "Audit", "Users", "Policies"], 0),
        ("Health en ámbar sin revisar drivers es:", ["Error de preparación", "Suficiente", "Opcional", "Mejor práctica"], 0),
        ("Renovación a 72 días. ¿Dónde lo ves primero?", ["Panel Renewal en 360", "Solo email", "No se registra", "Leads"], 0),
        ("Ticket P1 abierto. ¿Antes de llamada comercial?", ["Revisar y coordinar con CS", "Ignorar", "Cerrar sin leer", "Borrar"], 0),
        ("Timeline sirve para:", ["Historia única sin saltar pantallas", "Editar código", "Facturación legal", "Crear usuarios"], 0),
        ("Insights IA en 360 debes:", ["Validar con contexto humano", "Aprobar ciegamente", "Ignorar siempre", "Borrar"], 0),
        ("QBR sin revisar 360 genera:", ["Cliente siente desconocimiento", "Más ventas seguras", "Nada", "Menos trabajo"], 0),
    ],
    "Recorrido 360 en QA <10 min + 6/7 evaluación + supervisor observa QBR simulado → badge **360 Navigator** (módulo prioritario).",
    extra=dedent("""
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
    """).strip(),
)

# ─── TASKS ───────────────────────────────────────────────────────────────────
_reg(
    "Tasks",
    "Los compromisos viven en agendas personales, post-its y chats — el equipo no ve quién debe hacer qué ni cuándo.",
    "Sin tareas en CRM, el 30 % de seguimientos comerciales no ocurre; deals se enfrían y clientes perciben abandono.",
    "Olvidas llamar al CFO, el manager no puede coacharte y el forecast miente por falta de actividad real.",
    [
        "Terminaste una reunión — ¿qué sigue?",
        "Te asignaron seguimiento de un deal o ticket.",
        "Planificas tu semana comercial.",
        "Tienes tareas vencidas en rojo.",
        "Coordinas handoff con otro colega.",
    ],
    """**Lunes 8:00 AM — TechSolutions Panamá**

**Pedro**, SDR, abre `/Tasks` con 12 ítems: 4 vencidos (ayer no cerró), 6 hoy, 2 futuras. Prioriza: llamada a **Distribuidora Pacífico** (deal $18 K), envío de propuesta **Clínica San Fernando**, y seguimiento ticket escalado.

En 25 minutos reprograma lo realista, completa 3 tareas con nota breve y deja cero vencidos sin nueva fecha. Su manager ve actividad real en el pipeline.""",
    [
        ("PASO 1 — Abrir bandeja", f"**Tasks** (`/Tasks`).", "Lista por fecha, prioridad, estado.", "Ves propias y asignadas según rol."),
        ("PASO 2 — Triaje matutino", "Filtrar vencidas → hoy → esta semana.", "Cola priorizada.", "Vencidas con plan o completadas."),
        ("PASO 3 — Vincular contexto", "Al crear: ligar a lead, deal, customer o ticket.", "Tarea con enlace.", "No tareas huérfanas «llamar cliente»."),
        ("PASO 4 — Ejecutar y completar", "Marcar completa + nota de outcome.", "Desaparece de pendientes.", "Nota útil para el 360."),
        ("PASO 5 — Reprogramar con honestidad", "Si no cumpliste: nueva fecha hoy, no «algún día».", "Fecha realista.", "Sin vencidas zombies >3 días."),
        ("PASO 6 — Cierre de jornada", "Revisar mañana; crear tarea post-última reunión.", "Bandeja lista para martes.", "Cada reunión del día tiene siguiente paso."),
    ],
    [
        ("CAPTURA 01", "Bandeja Tasks — filtros vencidas/hoy"),
        ("CAPTURA 02", "Crear tarea con vínculo a deal"),
        ("CAPTURA 03", "Prioridad y fecha de vencimiento"),
        ("CAPTURA 04", "Completar tarea con nota"),
        ("CAPTURA 05", "Vista semanal de compromisos"),
    ],
    "Reunión → **Tarea creada** → Ejecución → Nota en registro → Siguiente tarea → Deal avanza → Renewal",
    [
        "Tareas genéricas sin vínculo («llamar»)",
        "Dejar vencidas sin reprogramar",
        "Marcar completa sin haber hecho nada",
        "Urgent en todo",
        "No poner fecha",
        "Duplicar tareas para el mismo compromiso",
        "Ignorar tareas asignadas por otros",
        "No revisar bandeja al iniciar día",
        "Crear tarea sin owner",
        "Olvidar tarea tras cerrar deal",
        "Usar Tasks como bloc de notas largas",
        "No completar al hacer el trabajo",
        "Semana sin revisión de carga",
        "Tareas de prueba en producción",
        "No coordinar con calendario externo",
        "Delegar sin notificar en sistema",
        "Cerrar ticket sin tarea de seguimiento al cliente",
        "Mezclar personal y trabajo en misma vista sin filtro",
        "No usar prioridad en cuentas VIP",
        "Fin de mes sin limpiar completadas antiguas",
    ],
    [
        "Revisar Tasks 2× al día (mañana y cierre)",
        "Cada reunión termina con tarea fechada",
        "Vincular siempre a registro de negocio",
        "Completar con nota de una línea útil",
        "Reprogramar el mismo día si no cumples",
        "Urgent solo para SLA o VIP",
        "Vista semanal los viernes",
        "Manager revisa vencidas del equipo",
        "Ctrl+K para crear tarea rápida",
        "Handoff = tarea asignada al otro owner",
        "Practicar triaje en University",
        "Cero vencidas >72 h sin explicación",
        "Tarea de renovación a 90/60/30 días automática en playbook",
        "Bloquear tiempo en calendario para tareas grandes",
        "Notificar al cliente solo tras tarea «enviar» completada",
        "Celebrar semana sin vencidas",
        "Plantillas de tarea por tipo de deal",
        "No duplicar — buscar tarea existente",
        "Auditar tareas huérfanas mensual",
        "Mentor revisa bandeja del nuevo en día 3",
    ],
    [
        "Revisé vencidas y hoy",
        "Tareas vinculadas a registros",
        "Completé con notas",
        "Reprogramé lo no hecho",
        "Mañana tiene tareas claras",
    ],
    f"En QA: crear 3 tareas (deal, customer, lead), completar 1, reprogramar 1, dejar 1 para mañana.",
    [
        ("¿Cuándo crear tarea?", ["Tras cada reunión o compromiso", "Nunca", "Solo viernes", "Una vez al mes"], 0),
        ("Tarea «llamar» sin vínculo es:", ["Mala práctica", "Ideal", "Obligatorio", "Recomendado"], 0),
        ("Bandeja al iniciar el día:", ["Triaje vencidas → hoy", "Ignorar", "Borrar todo", "Solo urgentes"], 0),
        ("Completar tarea sin nota:", ["Pierdes contexto para el equipo", "Perfecto", "Obligatorio", "Mejor"], 0),
        ("¿Ruta del módulo?", ["/Tasks", "/Leads", "/Audit", "/revenue"], 0),
    ],
    "Ejercicio QA + evaluación → badge **Task Master**.",
)

# ─── REVENUE OS ──────────────────────────────────────────────────────────────
_reg(
    "RevenueOS",
    "Gerentes comerciales necesitan responder: ¿llegamos a la cuota?, ¿el pipeline alcanza?, ¿dónde está el riesgo? — sin esperar a fin de mes.",
    "Un forecast erróneo de 10 % en una cuota trimestral de $2 M puede desalinear contratación, marketing y bonus — impacto directo en cash.",
    "Prometes al CEO un trimestre que no existe, descuidas deals grandes concentrados y el equipo quema en oportunidades falsas.",
    [
        "Reunión semanal de pipeline con ventas.",
        "Preparar forecast para dirección.",
        "Evaluar si necesitas más pipeline (coverage).",
        "Identificar concentración en un solo deal.",
        "Explicar brecha cuota vs realidad en lenguaje simple.",
    ],
    """**Martes 16:00 — TechSolutions Panamá**

**Luis**, Gerente Comercial, abre `/revenue` antes del pipeline review. Ve:
- **Cuota Q2:** $1.2 M
- **Forecast commit:** $780 K (gap visible)
- **Pipeline coverage:** 2.1× (bajo el 3× objetivo)
- **Deal concentrado:** 45 % en Banco Regional
- **Riesgo:** 3 deals sin actividad >14 días

Traduce a su equipo en español llano: «Necesitamos $420 K más de commit real o pipeline nuevo; Banco Regional no puede ser la única apuesta». Acciones concretas en 30 min.""",
    [
        ("PASO 1 — Abrir Revenue OS", f"**Revenue OS** (`/revenue`).", "Dashboard con período seleccionable.", "Cuota y forecast visibles."),
        ("PASO 2 — Leer forecast en plain language", "Commit / Best case / Pipeline — qué es cada capa.", "Entiendes qué es «firme» vs «posible».", "Puedes explicarlo a un vendedor en 2 min."),
        ("PASO 3 — Coverage (cobertura)", "Pipeline total ÷ cuota restante.", "Ratio y semáforo.", "Sabes si necesitas más deals o acelerar cierre."),
        ("PASO 4 — Cuota y brecha", "Realizado + forecast vs cuota.", "Gap en $ y %.", "Plan de acción si gap >10 %."),
        ("PASO 5 — Riesgo y concentración", "Deals grandes, inactivos, probabilidad inflada.", "Top 5 riesgos listados.", "Cada riesgo con owner y fecha."),
        ("PASO 6 — Accionar con el equipo", "Salir con 3 prioridades: acelerar, descartar, crear pipeline.", "Tareas y deals actualizados.", "Próximo review con mismas métricas."),
    ],
    [
        ("CAPTURA 01", "Dashboard Revenue OS — período Q2"),
        ("CAPTURA 02", "Panel Forecast — commit vs best case"),
        ("CAPTURA 03", "Métrica Coverage con semáforo"),
        ("CAPTURA 04", "Brecha vs cuota en $ y %"),
        ("CAPTURA 05", "Lista deals en riesgo / concentración"),
        ("CAPTURA 06", "Drill-down a deal desde Revenue OS"),
    ],
    "Leads → Deals actualizados → **Revenue OS semanal** → Acciones → Cierre → Cuota → Renewal ARR",
    [
        "Mirar solo el total sin capas de forecast",
        "Ignorar coverage bajo 2×",
        "No cuestionar probabilidades infladas",
        "Pipeline review sin Revenue OS abierto",
        "Prometer cuota sin plan de brecha",
        "Concentrar 50 %+ en un deal",
        "No actuar sobre deals inactivos",
        "Confundir commit con wishful thinking",
        "No alinear definiciones con ventas",
        "Revisar solo al fin de trimestre",
        "Ignorar renewals en ARR",
        "No separar new business vs expansión",
        "Exportar sin validar datos",
        "Castigar por forecast honesto",
        "No documentar cambios de commit",
        "Usar Revenue OS sin cruzar Deals",
        "Olvidar descuentos pendientes de aprobación",
        "No comunicar gap a dirección a tiempo",
        "Métricas sin owner por acción",
        "Celebrar pipeline gordo sin calidad",
    ],
    [
        "Pipeline review semanal con Revenue OS proyectado",
        "Explicar forecast en 3 capas al equipo",
        "Coverage objetivo 3× — plan si <2.5×",
        "Top 5 deals revisados uno a uno",
        "Deals >14 días sin actividad = plan o Lost",
        "Cuota = conversación de brecha, no sorpresa",
        "Concentración >30 % = plan B documentado",
        "Cruzar con Customer 360 en deals enterprise",
        "Honestidad premiada en commit",
        "Renewals en vista ARR mensual",
        "Compartir captura en Slack post-review",
        "University para nuevos managers",
        "Mismo día: tareas de acción post-review",
        "Trimestre nuevo = limpiar pipeline",
        "Documentar supuestos del forecast",
        "CEO recibe resumen en 5 bullets",
        "Riesgo IA como hipótesis, no veredicto",
        "Comparar QoQ para tendencia",
        "Coaching 1:1 basado en gap individual",
        "Certificación RevOps para managers",
    ],
    [
        "Abrí Revenue OS del período correcto",
        "Entendí commit vs pipeline",
        "Calculé coverage y brecha",
        "Identifiqué top riesgos",
        "Salí con 3 acciones asignadas",
        "Agendé próximo review",
    ],
    f"En QA: abrir `/revenue`, identificar cuota/forecast/coverage, listar 3 deals riesgo, simular nota de pipeline review.",
    [
        ("Coverage 2.1× con objetivo 3× significa:", ["Pipeline insuficiente para cuota", "Sobrado", "Perfecto", "No aplica"], 0),
        ("Forecast «commit» es:", ["Lo que el equipo cree que cerrará", "Todo el pipeline", "Solo wishful thinking", "Histórico"], 0),
        ("45 % en un solo deal es:", ["Riesgo de concentración", "Ideal", "Sin impacto", "Mejor práctica"], 0),
        ("¿Ruta del módulo?", ["/revenue", "/executive", "/Leads", "/Audit"], 0),
        ("Deal 14 días sin actividad en review:", ["Plan de acción o Lost", "Ignorar", "Subir probabilidad", "Celebrar"], 0),
        ("Revenue OS es para:", ["Gerentes y RevOps", "Solo IT", "Solo CS", "Auditoría"], 0),
    ],
    "Simulación pipeline review en QA + evaluación → badge **Revenue Captain**.",
)

# ─── EXECUTIVE OS ────────────────────────────────────────────────────────────
_reg(
    "ExecutiveOS",
    "CEOs y dirección necesitan narrativa + cifras en minutos para juntas, inversionistas y decisiones de asignación de capital.",
    "Una junta mal preparada puede retrasar decisiones de hiring o producto semanas; o peor, basarse en números no validados.",
    "Presentas solo buenas noticias, el board descubre el gap después y pierdes credibilidad.",
    [
        "Junta directiva semanal o mensual.",
        "Conversación con inversionista o socio.",
        "Decisión de contratar o recortar.",
        "Crisis de cliente VIP o caída de ARR.",
        "Apertura de nuevo mercado o línea.",
    ],
    """**Jueves 6:30 AM — TechSolutions Panamá**

**Elena**, CEO, tiene junta a las 9:00. Abre `/executive`: ingresos YTD, churn, pipeline ejecutivo, outcomes de IA aprobadas, riesgos top 3.

En 12 minutos valida que el forecast de Luis coincide con Executive OS, detecta churn en segmento SMB y prepara 3 bullets: crecimiento, riesgo, decisión pedida al board. Exporta vista para la presentación.""",
    [
        ("PASO 1 — Executive OS", f"**Executive OS** (`/executive`).", "Vista consolidada C-level.", "KPIs principales cargados."),
        ("PASO 2 — Validar ingresos", "ARR, MRR, new vs expansion, churn.", "Cuadro coherente con finanzas.", "Diferencias <5 % o explicadas."),
        ("PASO 3 — Riesgos ejecutivos", "Top cuentas, concentración, SLA, compliance.", "Lista priorizada.", "Cada riesgo con owner."),
        ("PASO 4 — Outcomes y decisiones IA", "Qué aprobó Trust Studio con impacto $.", "Automatización bajo control.", "Sin sorpresas de IA no supervisada."),
        ("PASO 5 — Narrativa en 3 bullets", "Crecimiento / Riesgo / Pedido al board.", "Historia clara.", "Una decisión concreta solicitada."),
        ("PASO 6 — Export y junta", "Exportar o compartir vista acordada.", "Material listo.", "Post-junta: tareas a owners."),
    ],
    [
        ("CAPTURA 01", "Executive OS — vista principal KPIs"),
        ("CAPTURA 02", "Panel ingresos y churn"),
        ("CAPTURA 03", "Riesgos ejecutivos top 3"),
        ("CAPTURA 04", "Outcomes IA / Trust Studio resumen"),
        ("CAPTURA 05", "Export para junta"),
    ],
    "Operación diaria → Revenue OS → **Executive OS** → Decisión board → Asignación recursos → Resultado trimestre",
    [
        "Export sin validar con finanzas",
        "Solo métricas vanity sin churn",
        "Ocultar pipeline débil",
        "Junta sin pedido de decisión claro",
        "Ignorar concentración de clientes",
        "No leer riesgos antes de Q&A",
        "Confiar en slide de hace 2 meses",
        "No alinear con gerente comercial",
        "Descartar señales SMB en churn",
        "Presentar IA sin mencionar governance",
        "No asignar follow-up post-junta",
        "Mezclar datos de distintos períodos",
        "Ignorar tickets P1 en cuentas clave",
        "Narrativa sin contexto competitivo",
        "No preparar respuesta a brecha de cuota",
        "Usar Executive OS solo una vez al año",
        "No documentar decisiones del board",
        "Comparar sin ajuste estacional",
        "Olvidar renewals en narrativa ARR",
        "Subestimar tiempo de lectura previa",
    ],
    [
        "Ritual viernes: Executive OS 15 min",
        "3 bullets: crecimiento, riesgo, pedido",
        "Validar con CFO antes de board",
        "Riesgo primero en la narrativa interna",
        "Cruzar Executive con Revenue OS",
        "Trust Studio en slide de governance IA",
        "Post-junta: tareas con owner en 24 h",
        "Churn por segmento, no solo total",
        "Preparar Q&A con datos drill-down",
        "Mismo formato cada mes — comparabilidad",
        "University para nuevos directores",
        "No más de 7 KPIs en pantalla principal",
        "Celebrar wins con datos, no adjetivos",
        "Decisión explícita solicitada al board",
        "Export con fecha y versión",
        "VIP risks en rojo siempre mencionados",
        "Mentor CFO para primeras 3 juntas",
        "Revisar outcomes IA mensual",
        "Pipeline como leading, ARR como lagging",
        "Certificación Executive User",
    ],
    [
        "Validé KPIs con finanzas",
        "Identifiqué top 3 riesgos",
        "Preparé 3 bullets narrativos",
        "Pedido de decisión claro",
        "Export listo para junta",
        "Follow-ups asignados",
    ],
    f"En QA: abrir `/executive`, preparar 3 bullets, identificar 1 riesgo y 1 métrica de churn, simular export.",
    [
        ("Executive OS es para:", ["CEO y dirección", "Solo vendedores", "Solo soporte", "Auditoría"], 0),
        ("Antes de junta debes:", ["Validar cifras con finanzas", "Improvisar", "Ocultar riesgos", "No preparar"], 0),
        ("Narrativa ejecutiva ideal:", ["3 bullets: crecimiento, riesgo, pedido", "50 slides", "Solo marketing", "Sin datos"], 0),
        ("¿Ruta?", ["/executive", "/revenue", "/Leads", "/TrustInbox"], 0),
        ("Churn en SMB visible implica:", ["Revisar segmento y acción", "Ignorar", "Solo celebrar", "Borrar dato"], 0),
    ],
    "Simulación junta en QA + evaluación → badge **Executive Analyst**.",
)

# ─── TRUST STUDIO ────────────────────────────────────────────────────────────
_reg(
    "TrustStudio",
    "La IA propone acciones (emails, descuentos, clasificaciones) que sin supervisión humana pueden dañar relaciones o violar políticas.",
    "Un email automático incorrecto a un CEO puede costar un deal de seis cifras; un descuento no autorizado erosiona margen.",
    "Aprobas sin leer y el cliente recibe spam; rechazas todo y la IA no aprende; dejas cola >24 h y pierdes velocidad.",
    [
        "Llega alerta de decisión IA pendiente.",
        "IA sugiere respuesta a ticket o lead.",
        "Propuesta de descuento o excepción.",
        "Clasificación automática de prioridad.",
        "Revisión de compliance antes de envío masivo.",
    ],
    """**Miércoles 11:20 — TechSolutions Panamá**

**Patricia**, Sales Manager, recibe en `/TrustInbox` una propuesta de IA: enviar seguimiento a **Ministerio de Economía** con tono informal y 15 % descuento no aprobado.

Lee contexto, historial en 360, política de descuentos. **Rechaza** con feedback «tono formal sector público; descuento requiere VP». Aprueba otra: clasificar ticket de **RetailMax** como P2 con respuesta plantilla correcta. 8 minutos, cero daño reputacional.""",
    [
        ("PASO 1 — Abrir Trust Studio", f"**Trust Inbox** (`/TrustInbox`).", "Cola de decisiones pendientes.", "Contador visible; nada >24 h."),
        ("PASO 2 — Leer contexto completo", "Registro vinculado, historial, propuesta IA.", "Entiendes qué haría el sistema.", "Cliente, valor y política revisados."),
        ("PASO 3 — Validar contra políticas", "Descuentos, tono, datos sensibles.", "Cumple o no.", "Si duda → rechazar o escalar."),
        ("PASO 4 — Aprobar o rechazar", "Un clic + comentario obligatorio si rechazas.", "Decisión auditada.", "Feedback útil para mejorar IA."),
        ("PASO 5 — Seguimiento", "Verificar que la acción ejecutada es correcta.", "Timeline actualizado.", "Cliente no nota «robot sin cerebro»."),
        ("PASO 6 — Ritual de equipo", "Turnos de revisión; métricas de cola.", "SLA de Trust cumplido.", "Política documentada en University."),
    ],
    [
        ("CAPTURA 01", "Trust Inbox — cola pendientes"),
        ("CAPTURA 02", "Detalle propuesta IA con contexto"),
        ("CAPTURA 03", "Comparación con política de descuentos"),
        ("CAPTURA 04", "Aprobar con confirmación"),
        ("CAPTURA 05", "Rechazar con feedback estructurado"),
        ("CAPTURA 06", "Historial de decisiones auditadas"),
    ],
    "IA propone → **Trust Studio** → Humano decide → Acción ejecutada → Audit registra → Cliente impactado → Renewal",
    [
        "Aprobar sin leer el texto propuesto",
        "Rechazar todo por miedo",
        "Dejar cola >24 horas",
        "Aprobar descuento sin política",
        "No dar feedback al rechazar",
        "Ignorar contexto del 360",
        "Turno de revisión sin backup",
        "Aprobar en móvil sin pantalla completa",
        "No escalar excepciones VIP",
        "Mezclar pruebas y producción",
        "Confiar ciegamente en clasificación P1/P2",
        "No documentar patrón de rechazos",
        "Delegar sin capacitar en Trust",
        "Aprobar email con datos incorrectos",
        "Ignorar auditoría post-decisión",
        "No alinear con Legal en sector regulado",
        "Aprobar masivo sin muestra",
        "Rechazar sin alternativa humana",
        "No medir tiempo de cola",
        "Desactivar IA en lugar de gobernar",
    ],
    [
        "Leer propuesta completa siempre",
        "Rechazo con feedback específico",
        "Cola cero >24 h — turnos definidos",
        "Política de descuentos a mano",
        "360 abierto en segunda pantalla",
        "Aprobar tono adecuado al sector",
        "Escalar VIP a manager",
        "Métricas semanales: aprobado/rechazado/tiempo",
        "Capacitar en University antes de turno",
        "Human-in-the-loop como ventaja, no freno",
        "Patrones de rechazo → mejora políticas",
        "Sample de 10 % en envíos masivos",
        "Alternativa humana si rechazas urgente",
        "Audit trail como aliado",
        "No aprobar datos PII incorrectos",
        "Trust + Agents supervisados juntos",
        "Celebrar buen rechazo que evitó crisis",
        "Documentar excepciones aprobadas",
        "Revisión Legal trimestral",
        "Badge Trust Specialist",
    ],
    [
        "Revisé cola pendientes",
        "Leí contexto y política",
        "Decidí con comentario si rechacé",
        "Verifiqué ejecución",
        "Cola <24 h",
    ],
    f"En QA: abrir `/TrustInbox`, revisar 2 propuestas simuladas, aprobar 1 y rechazar 1 con feedback.",
    [
        ("Trust Studio sirve para:", ["Aprobar decisiones IA con humano", "Crear usuarios", "Facturar", "Borrar leads"], 0),
        ("Rechazar sin comentario:", ["Pierdes mejora de IA", "Ideal", "Obligatorio", "Mejor"], 0),
        ("Cola >24 h genera:", ["Riesgo operativo y cliente", "Nada", "Más ventas", "Menos trabajo"], 0),
        ("¿Ruta?", ["/TrustInbox", "/Agents", "/Audit", "/Users"], 0),
        ("Descuento IA sin política:", ["Rechazar o escalar", "Aprobar siempre", "Ignorar", "Borrar cliente"], 0),
    ],
    "Ejercicio Trust en QA + evaluación → badge **Trust Guardian**.",
)

# ─── CUSTOMER SUCCESS ────────────────────────────────────────────────────────
_reg(
    "CustomerSuccess",
    "Post-venta sin sistema unificado: tickets, salud, renovaciones y escalaciones viven en silos — el churn sorprende.",
    "Subir retención 5 % puede aumentar beneficios 25–95 % (Bain); perder una cuenta $80 K ARR por mala renovación duele un trimestre.",
    "Tickets SLA rotos, renovación sorpresa a 30 días, escalación sin contexto y cliente que «ya avisó» en tres tickets distintos.",
    [
        "Cola de tickets nuevos o escalados.",
        "Cuenta en health rojo o ámbar.",
        "Ventana de renovación 90/60/30 días.",
        "Playbook de churn prevention.",
        "Escalación a manager o ejecutivo.",
    ],
    """**Martes 10:00 — TechSolutions Panamá**

**Ana**, CS Lead, ve alerta: **Hotel Plaza Pacífico** health **Rojo**, NPS 4, ticket P2 abierto 36 h, renewal en **58 días**, uso -40 %.

Abre `/customer-success`, ejecuta playbook **Recuperación**, abre 360, agenda QBR de emergencia, escala a Roberto (manager) con resumen de revenue en riesgo $65 K ARR. Plan 30 días documentado.""",
    [
        ("PASO 1 — CS OS", f"**Customer Success** (`/customer-success`).", "Cola tickets, health, renewals.", "Prioridad clara."),
        ("PASO 2 — Clasificar ticket", "Severidad, SLA, playbook correcto.", "Ticket en cola adecuada.", "P1 <1 h, P2 <8 h según política."),
        ("PASO 3 — Health y churn signals", "Score, tendencia, uso, pagos.", "Razón del rojo identificada.", "Acción en 24 h si rojo."),
        ("PASO 4 — Renewal tracker", "Cuentas en ventana 90 días.", "Owner y playbook asignado.", "Ninguna renewal sin plan."),
        ("PASO 5 — Playbook", "Ejecutar pasos: contacto, valor, plan.", "Checklist playbook completo.", "Cliente confirma próximo paso."),
        ("PASO 6 — Escalación", "Si VIP o revenue >umbral → manager + nota 360.", "War room si aplica.", "Seguimiento cada 24 h."),
        ("PASO 7 — Cierre y CSAT", "Confirmar resolución con cliente.", "Ticket cerrado con causa raíz.", "CSAT o NPS follow-up."),
    ],
    [
        ("CAPTURA 01", "Customer Success — cola principal"),
        ("CAPTURA 02", "Ticket clasificado con severidad"),
        ("CAPTURA 03", "Panel health — Hotel Plaza Pacífico"),
        ("CAPTURA 04", "Renewal tracker 90 días"),
        ("CAPTURA 05", "Playbook Recuperación en ejecución"),
        ("CAPTURA 06", "Escalación con resumen revenue"),
    ],
    "Won → Onboarding → Uso → **Tickets + Health** → Renewal playbook → QBR → Renew o Recover → Expansion",
    [
        "Cerrar ticket sin confirmar con cliente",
        "Romper SLA sin escalar",
        "Ignorar health rojo",
        "Renewal a 30 días sin contacto",
        "Playbook equivocado",
        "No documentar causa raíz",
        "Escalar sin datos de 360",
        "Prometer fecha sin validar ingeniería",
        "Mezclar P1 y P3 en prioridad",
        "No involucrar ventas en expansión",
        "Churn surprise sin post-mortem",
        "CSAT solo en clientes felices",
        "Dejar ticket abierto «por si acaso»",
        "No registrar llamada en timeline",
        "Renovación solo con descuento",
        "Ignorar NPS detractor",
        "War room sin owner",
        "No medir tiempo a resolución",
        "Handoff implementación incompleto",
        "Olvidar playbook tras cerrar ticket",
    ],
    [
        "SLA visible en cada ticket",
        "Health rojo = acción en 24 h",
        "Renewal 90 días inicia playbook",
        "Playbook correcto por tipo",
        "Causa raíz en todo P1/P2",
        "360 antes de escalación",
        "QBR con métricas de valor",
        "Confirmación cliente antes de cerrar",
        "NPS detractor en 48 h",
        "Post-mortem en churn",
        "Coordinar con ventas en expansión",
        "War room VIP documentada",
        "Nota en 360 cada touch",
        "University playbooks obligatorios",
        "Cola revisada 3× al día",
        "Renovación sin solo descuento",
        "Seguimiento escalación 24 h",
        "CSAT muestra representativa",
        "Celebrar renewal en equipo",
        "Mentor en primer P1 real",
    ],
    [
        "Cola priorizada",
        "SLA cumplido o escalado",
        "Health revisado",
        "Renewals en ventana con plan",
        "Playbook documentado",
        "Cliente confirmó cierre",
    ],
    f"En QA: abrir `/customer-success`, clasificar ticket simulado, revisar health, iniciar playbook renewal 90 días.",
    [
        ("Renewal típica se anticipa a:", ["90 días", "1 día", "2 años", "Nunca"], 0),
        ("Health rojo implica:", ["Acción en 24 h", "Ignorar", "Solo email", "Cerrar cuenta"], 0),
        ("Cerrar ticket sin confirmar cliente:", ["Mala práctica", "Ideal", "SLA", "Obligatorio"], 0),
        ("¿Ruta CS?", ["/customer-success", "/Leads", "/revenue", "/Policies"], 0),
        ("Playbook Recuperación se usa cuando:", ["Churn risk / health bajo", "Nuevo lead", "Crear usuario", "Audit"], 0),
        ("Escalación VIP requiere:", ["Contexto 360 + revenue", "Solo nombre", "Nada", "Borrar ticket"], 0),
    ],
    "Simulación ticket + renewal en QA + evaluación → badge **CS Hero**.",
)

# ─── AGENTS ──────────────────────────────────────────────────────────────────
_reg(
    "Agents",
    "Trabajo repetitivo (clasificar, resumir, recordar) consume tiempo; sin agentes IA el equipo no escala.",
    "Automatizar 2 h/día por SDR son ~500 h/año — o riesgo si se hace sin supervisión.",
    "Activas agentes sin política, no supervisas, o los apagas por un error y pierdes productividad.",
    [
        "Clasificación automática de leads/tickets.",
        "Resúmenes pre-llamada.",
        "Recordatorios de seguimiento.",
        "Supervisar workforce de IA.",
        "Ajustar tras feedback de Trust Studio.",
    ],
    """**Viernes 15:00 — TechSolutions Panamá**

**Diego**, Admin, revisa `/Agents`: agente **Lead Qualifier** procesó 47 leads; 3 en Trust Studio pendientes; agente **Ticket Triage** redujo tiempo primera respuesta 22 %.

Ajusta umbral de confianza, desactiva temporalmente agente de emails en sector público y programa capacitación Trust para ventas. Automatización con control.""",
    [
        ("PASO 1 — Workforce", f"**Agents** (`/Agents`).", "Lista agentes activos y métricas.", "Estado de cada agente visible."),
        ("PASO 2 — Revisar actividad", "Volúmenes, errores, cola Trust.", "Sin sorpresas.", "Anomalías investigadas."),
        ("PASO 3 — Supervisar decisiones", "Cruzar con Trust Inbox.", "Human-in-the-loop activo.", "Nada crítico sin aprobación."),
        ("PASO 4 — Ajustar configuración", "Umbrales, alcance, horarios.", "Cambio documentado.", "Política alineada."),
        ("PASO 5 — Medir impacto", "Tiempo ahorrado, calidad, SLA.", "ROI narrativo.", "Compartir con dirección."),
        ("PASO 6 — Capacitar usuarios", "University + política clara.", "Equipo confía en el sistema.", "Feedback loop activo."),
    ],
    [
        ("CAPTURA 01", "Panel Agents — workforce overview"),
        ("CAPTURA 02", "Detalle agente Lead Qualifier"),
        ("CAPTURA 03", "Métricas volumen y errores"),
        ("CAPTURA 04", "Vínculo a Trust Studio pendientes"),
        ("CAPTURA 05", "Configuración umbral confianza"),
    ],
    "Proceso manual → **Agente activo** → Trust Studio → Acción → Métrica → Ajuste → Escala",
    [
        "Activar agente sin política",
        "No supervisar primera semana",
        "Desactivar todo tras un error",
        "Automatizar decisiones de $ sin Trust",
        "No medir impacto",
        "Agentes en horario sin backup humano",
        "Ignorar feedback de rechazos Trust",
        "Múltiples agentes en mismo flujo sin coordinar",
        "No documentar cambios de config",
        "Probar en producción sin QA",
        "Confiar 100 % en clasificación",
        "No capacitar usuarios afectados",
        "Agente con acceso más allá del necesario",
        "Olvidar sector regulado",
        "No tener owner del agente",
        "Métricas solo de volumen, no calidad",
        "Ignorar latencia de cola",
        "No plan de rollback",
        "Mezclar entornos",
        "IA como sustituto de proceso roto",
    ],
    [
        "Empezar con un agente simple",
        "Trust Studio obligatorio en acciones externas",
        "Owner por agente",
        "Revisión semanal de métricas",
        "QA antes de producción",
        "Documentar config y cambios",
        "Capacitación University",
        "Medir tiempo ahorrado y calidad",
        "Feedback de rechazos → ajuste",
        "Rollback plan documentado",
        "Human-in-the-loop como default",
        "No automatizar proceso no definido",
        "Sector público: reglas estrictas",
        "Celebrar win de productividad",
        "Comunicar cambios al equipo",
        "Cruzar Agents + Audit mensual",
        "Umbrales conservadores al inicio",
        "Expandir tras 30 días estables",
        "Mentor admin para primer agente",
        "Badge AI Power User",
    ],
    [
        "Revisé agentes activos",
        "Cola Trust sin críticos",
        "Métricas entendidas",
        "Config documentada",
        "Equipo capacitado",
    ],
    f"En QA: abrir `/Agents`, revisar estado, simular ajuste umbral, vincular con Trust Inbox.",
    [
        ("Agents requieren supervisión vía:", ["Trust Studio", "Solo email", "Nada", "Borrar datos"], 0),
        ("Primer paso al desplegar agente:", ["Política + QA", "Producción directo", "Ignorar", "Desactivar CRM"], 0),
        ("Human-in-the-loop significa:", ["Humano aprueba acciones críticas", "Sin humanos", "Solo bots", "Audit off"], 0),
        ("¿Ruta?", ["/Agents", "/Users", "/Leads", "/executive"], 0),
        ("Automatizar proceso roto:", ["Arreglar proceso primero", "Ideal", "Obligatorio", "Más rápido"], 0),
    ],
    "Revisión workforce QA + evaluación → badge **AI Operator**.",
)

# ─── USERS ───────────────────────────────────────────────────────────────────
_reg(
    "Users",
    "Accesos incorrectos = fugas de datos, usuarios fantasma y gente que no puede trabajar el día 1.",
    "Un admin de más en sistema regulado puede costar multas; un vendedor sin acceso pierde deals el primer día.",
    "Das admin a todos, no haces offboarding, o creas usuarios duplicados.",
    [
        "Alta de empleado nuevo.",
        "Cambio de rol o promoción.",
        "Offboarding el mismo día de salida.",
        "Auditoría de accesos trimestral.",
        "Reset de acceso por seguridad.",
    ],
    """**Lunes 9:00 — TechSolutions Panamá**

**Sofía**, Admin, da de alta a **Carlos** (vendedor): rol Sales, sin admin, University asignado, email corporativo. Desactiva a ex-empleado **Miguel** que salió viernes — mismo día, sesiones cerradas.

Checklist: mínimo privilegio, capacitación obligatoria, auditoría limpia.""",
    [
        ("PASO 1 — Users", f"**Users** (`/Users`).", "Lista usuarios activos/inactivos.", "Sin cuentas sin owner."),
        ("PASO 2 — Crear usuario", "Nombre, email, rol mínimo necesario.", "Invitación enviada.", "Rol correcto, no Admin por defecto."),
        ("PASO 3 — Asignar rol", "Sales / CS / Manager / Viewer / Admin.", "Permisos acordes.", "Principio mínimo privilegio."),
        ("PASO 4 — University", "Asignar ruta de aprendizaje del rol.", "Progreso visible.", "No producción sin Fundamentos."),
        ("PASO 5 — Comunicar", "Credenciales y primera tarea al manager.", "Usuario login día 1.", "Manager confirma acceso OK."),
        ("PASO 6 — Offboarding", "Desactivar mismo día; revisar Audit.", "Sin acceso post-salida.", "Sesiones terminadas."),
    ],
    [
        ("CAPTURA 01", "Lista Users con roles"),
        ("CAPTURA 02", "Formulario alta (`/Users`)"),
        ("CAPTURA 03", "Selector de rol Sales"),
        ("CAPTURA 04", "Usuario desactivado — offboarding"),
        ("CAPTURA 05", "University asignado al usuario"),
    ],
    "Contratación → **User creado** → University → Operación → Cambio rol → Offboarding → Audit",
    [
        "Admin para todos los nuevos",
        "No desactivar el día de salida",
        "Usuarios fantasma sin uso 90 días",
        "Email personal en producción",
        "Compartir contraseñas",
        "Rol incorrecto por prisa",
        "No asignar University",
        "Duplicar usuario mismo email",
        "Olvidar reasignar registros del que sale",
        "No revisar Audit post-offboarding",
        "Permisos acumulados sin limpiar",
        "Crear usuario sin manager owner",
        "No documentar excepciones de acceso",
        "Ignorar MFA si está disponible",
        "Alta masiva sin checklist",
        "No capacitar en seguridad",
        "Viewer con datos sensibles sin necesidad",
        "No auditar admins trimestral",
        "Reactivar sin justificación",
        "Mezclar cuentas de prueba y reales",
    ],
    [
        "Mínimo privilegio siempre",
        "Offboarding día 0 de salida",
        "University antes de producción",
        "Revisión trimestral de accesos",
        "Rol = función real del puesto",
        "Audit tras cada offboarding",
        "MFA donde aplique",
        "Documentar excepciones temporales",
        "Reasignar pipeline al salir vendedor",
        "Checklist alta en onboarding IT",
        "Sin admin por comodidad",
        "Email corporativo único",
        "Manager valida primer login",
        "Desactivar, no borrar, con historial",
        "Capacitación seguridad anual",
        "Lista admins <5 personas",
        "Comunicar cambios de rol",
        "Probar acceso con usuario nuevo",
        "Celebrar adopción University 100 %",
        "Certificación Administrator",
    ],
    [
        "Rol mínimo asignado",
        "University en ruta del rol",
        "Manager notificado",
        "Sin admins innecesarios",
        "Offboarding mismo día si aplica",
        "Audit revisado",
    ],
    f"En QA: simular alta usuario «Academy Test», rol Viewer, asignar University, simular desactivación.",
    [
        ("Principio de acceso:", ["Mínimo privilegio", "Admin para todos", "Sin roles", "Compartir login"], 0),
        ("Offboarding debe ser:", ["Mismo día de salida", "Nunca", "Un año después", "Opcional"], 0),
        ("Usuario nuevo sin University:", ["No debería operar en producción", "Ideal", "Obligatorio", "Mejor"], 0),
        ("¿Ruta?", ["/Users", "/Audit", "/Leads", "/Agents"], 0),
        ("Admin por comodidad:", ["Riesgo de seguridad", "Buena práctica", "Requerido", "Sin impacto"], 0),
    ],
    "Alta/offboarding simulado QA + evaluación → badge **Access Steward**.",
)

# ─── POLICIES ────────────────────────────────────────────────────────────────
_reg(
    "Policies",
    "Descuentos, accesos y excepciones «de palabra» generan inconsistencia, fraude interno y clientes que comparan tratos.",
    "Un 5 % de descuento no autorizado en $200 K de deals erosiona $10 K de margen; en regulados, puede ser multa.",
    "Cada vendedor inventa su regla, Legal no sabe qué se prometió y Audit no puede defender nada.",
    [
        "Definir quién aprueba descuentos.",
        "Restringir datos por territorio o rol.",
        "Nueva regla post-auditoría.",
        "Alinear ventas con Legal.",
        "Comunicar cambio de política al equipo.",
    ],
    """**Jueves 14:30 — TechSolutions Panamá**

**Ricardo**, Admin, publica política **Descuento máximo 10 % sin VP** y **Acceso PII solo CS+Manager**. Ventas recibe aviso en University; Trust Studio alinea rechazos automáticos.

María intenta 15 % — sistema bloquea; escala a VP con justificación en deal. Consistencia sin fricción innecesaria.""",
    [
        ("PASO 1 — Policies", f"**Policies** (`/Policies`).", "Lista reglas activas.", "Dueño por política."),
        ("PASO 2 — Revisar vigentes", "Descuentos, PII, territorios.", "Sin contradicciones.", "Alineado con Legal."),
        ("PASO 3 — Crear o editar", "Regla clara, alcance, excepciones.", "Borrador → revisión.", "Change log actualizado."),
        ("PASO 4 — Probar", "Usuario de prueba en QA.", "Bloqueo/permitir correcto.", "Sin falsos positivos masivos."),
        ("PASO 5 — Publicar y comunicar", "University + email equipo.", "Todos enterados.", "Fecha efectiva clara."),
        ("PASO 6 — Auditar cumplimiento", "Cruzar con Audit y Trust.", "Excepciones documentadas.", "Revisión trimestral."),
    ],
    [
        ("CAPTURA 01", "Lista Policies activas"),
        ("CAPTURA 02", "Editor regla descuento"),
        ("CAPTURA 03", "Prueba bloqueo 15 % descuento"),
        ("CAPTURA 04", "Change log de política"),
        ("CAPTURA 05", "Aviso en University"),
    ],
    "Necesidad negocio → **Política definida** → Prueba → Comunicación → Operación → Audit → Ajuste",
    [
        "Política sin dueño",
        "Publicar sin probar",
        "No comunicar cambios",
        "Excepciones solo por chat",
        "Reglas contradictorias",
        "Copiar política de otro tenant sin adaptar",
        "Ignorar feedback de ventas",
        "Política demasiado laxa en PII",
        "No revisar tras incidente",
        "Desactivar política sin reemplazo",
        "Legal no involucrado en regulados",
        "Change log vacío",
        "Probar solo en producción",
        "No alinear Trust Studio",
        "Política que nadie entiende",
        "Excepciones sin expiración",
        "No capacitar en University",
        "Ignorar intentos denegados en Audit",
        "Políticas huérfanas de proceso",
        "Revisión anual cuando necesitas trimestral",
    ],
    [
        "Dueño nombrado por política",
        "Probar en QA antes de publicar",
        "Comunicar 48 h antes si es restrictiva",
        "Change log obligatorio",
        "Legal en sector regulado",
        "Excepciones con ticket y expiración",
        "Alinear Trust y Policies",
        "Revisión trimestral calendario",
        "Lenguaje plain Spanish en reglas",
        "University actualizada mismo día",
        "Auditar intentos denegados",
        "Métricas de excepciones",
        "Sin «regla de pasillo»",
        "Simular vendedor y CS al probar",
        "Rollback plan si falla",
        "Celebrar menos excepciones = más margen",
        "Documentar racional de negocio",
        "Coordinar con Revenue en descuentos",
        "Post-incidente = revisión política",
        "Certificación Admin incluye Policies",
    ],
    [
        "Políticas revisadas",
        "Cambios en change log",
        "Prueba QA OK",
        "Equipo comunicado",
        "Trust alineado",
        "Audit sin anomalías críticas",
    ],
    f"En QA: revisar `/Policies`, simular regla descuento, verificar bloqueo en deal de prueba.",
    [
        ("Policies sirven para:", ["Reglas de negocio consistentes", "Diseño UI", "Email marketing", "Borrar leads"], 0),
        ("Publicar sin probar es:", ["Riesgo operativo", "Ideal", "Obligatorio", "Mejor"], 0),
        ("Excepción informal:", ["Erosiona margen y compliance", "Buena práctica", "Requerida", "Sin impacto"], 0),
        ("¿Ruta?", ["/Policies", "/Users", "/Leads", "/revenue"], 0),
        ("Change log:", ["Trazabilidad de cambios", "Opcional", "Secreto", "Solo IT"], 0),
    ],
    "Prueba política en QA + evaluación → badge **Policy Architect**.",
)

# ─── AUDIT ───────────────────────────────────────────────────────────────────
_reg(
    "Audit",
    "Sin trazabilidad no puedes responder «quién cambió qué» en auditoría, disputa legal o incidente de seguridad.",
    "Multas GDPR/CCPA o pérdida de contrato enterprise por falta de evidencia pueden superar millones.",
    "No investigas intentos denegados, no exportas a compliance y descubres el problema cuando ya es crisis.",
    [
        "Investigar acceso sospechoso.",
        "Preparar informe para compliance.",
        "Post-incidente de seguridad.",
        "Revisión semanal admin.",
        "Validar offboarding correcto.",
    ],
    """**Viernes 8:00 — TechSolutions Panamá**

**Laura**, Compliance, filtra `/Audit` por intentos **denegados** y usuario **Miguel** (desactivado lunes). Detecta 3 intentos martes — IP externa. Confirma offboarding, alerta CISO, exporta evidencia para archivo. Crisis evitada en 20 minutos.""",
    [
        ("PASO 1 — Audit", f"**Audit** (`/Audit`).", "Log de eventos con filtros.", "Retención según política."),
        ("PASO 2 — Filtrar evento", "Usuario, acción, fecha, resultado.", "Subconjunto relevante.", "Filtros documentados en ticket."),
        ("PASO 3 — Investigar anomalía", "Denegados, cambios masivos, horarios raros.", "Hipótesis clara.", "Timeline reconstruido."),
        ("PASO 4 — Correlacionar", "Users, Policies, registro afectado.", "Causa raíz.", "Acción correctiva asignada."),
        ("PASO 5 — Export / reportar", "Evidencia para Legal o cliente enterprise.", "Formato acordado.", "Cadena de custodia."),
        ("PASO 6 — Cierre y prevención", "Política o acceso ajustado.", "Incidente cerrado.", "Revisión semanal programada."),
    ],
    [
        ("CAPTURA 01", "Audit log — vista principal"),
        ("CAPTURA 02", "Filtro intentos denegados"),
        ("CAPTURA 03", "Detalle evento usuario Miguel"),
        ("CAPTURA 04", "Export evidencia compliance"),
        ("CAPTURA 05", "Correlación con Users desactivado"),
    ],
    "Acción usuario → **Audit registra** → Revisión → Investigación → Corrección → Policy/User update",
    [
        "Nunca revisar Audit",
        "Ignorar intentos denegados",
        "Export sin autorización",
        "Borrar logs manualmente",
        "No correlacionar con Users",
        "Investigar sin ticket",
        "Retención menor a requerimiento contractual",
        "No alertar en offboarding fallido",
        "Asumir denegado = error benigno",
        "No documentar hallazgos",
        "Compartir export por email inseguro",
        "Revisión solo anual",
        "Ignorar cambios masivos nocturnos",
        "No capacitar admins en Audit",
        "Mezclar entornos en análisis",
        "Cerrar incidente sin causa raíz",
        "No alinear con Policies",
        "Olvidar viewer role en alertas",
        "Export sin fecha/hora UTC",
        "No probar restauración de evidencia",
    ],
    [
        "Revisión semanal admin — calendario",
        "Intentos denegados siempre investigados",
        "Export solo canal seguro",
        "Ticket por investigación",
        "Correlacionar Users + Policies",
        "Retención según contrato",
        "Alerta offboarding fallido",
        "University para nuevos admins",
        "UTC en exports",
        "Post-incidente en 24 h",
        "Muestra mensual a compliance",
        "No borrar — inmutabilidad",
        "Cadena de custodia documentada",
        "Simulacro trimestral",
        "Viewer alertas configuradas",
        "Causa raíz obligatoria",
        "Celebrar detección temprana",
        "Integrar con Executive en crisis",
        "Métricas: denegados, cambios rol",
        "Badge SuperAdmin Elite path",
    ],
    [
        "Revisé log semanal",
        "Denegados investigados",
        "Exports autorizados",
        "Hallazgos documentados",
        "Acciones correctivas asignadas",
    ],
    f"En QA: abrir `/Audit`, filtrar por acción, simular investigación, documentar hallazgo ficticio.",
    [
        ("Audit sirve para:", ["Trazabilidad y compliance", "Ventas", "Marketing", "Crear leads"], 0),
        ("Intentos denegados:", ["Investigar siempre", "Ignorar", "Borrar", "Celebrar"], 0),
        ("Offboarding + intentos post-salida:", ["Alerta de seguridad", "Normal", "Bueno", "Esperado"], 0),
        ("¿Ruta?", ["/Audit", "/Leads", "/Tasks", "/revenue"], 0),
        ("Export evidencia:", ["Canal seguro autorizado", "WhatsApp", "Público", "Sin registro"], 0),
    ],
    "Investigación simulada QA + evaluación → badge **Compliance Sentinel**.",
)


def render_module(name: str) -> str:
    data = MODULES[name]
    meta = MODULE_META[name]
    lines = [
        f"## Módulo: {name}",
        "",
        f"| | |",
        f"|---|---|",
        f"| **Ruta** | `{meta['route']}` |",
        f"| **Rol** | {meta['role']} |",
        f"| **Duración** | {meta['time']} |",
        f"| **Empresa caso** | {COMPANY} |",
        "",
        "### 1. ¿Por qué existe?",
        "",
        f"**Problema empresarial:** {data['problem']}",
        "",
        f"**Impacto económico:** {data['economic']}",
        "",
        f"**Qué pasa si no se usa:** {data['if_not']}",
        "",
        "### 2. ¿Cuándo debo usarlo?",
        "",
    ]
    for w in data["when"]:
        lines.append(f"- {w}")
    lines.extend(["", "### 3. Historia — TechSolutions Panamá", "", data["story"], ""])
    lines.extend(["### 4. Recorrido paso a paso", ""])
    for title, action, expect, validate in data["steps"]:
        lines.extend([
            f"#### {title}",
            f"- **Qué hacer:** {action}",
            f"- **Qué esperar:** {expect}",
            f"- **Qué validar:** {validate}",
            "",
        ])
    lines.extend(["### 5. Lista de capturas (placeholders)", ""])
    for cap_id, desc in data["captures"]:
        lines.append(f"- **{cap_id}** — _{desc}_")
    lines.extend(["", "### 6. Escenario completo Lead → Renewal", "", data["chain"], ""])
    lines.extend(["### 7. 20 errores comunes", ""])
    for i, e in enumerate(data["errors"], 1):
        lines.append(f"{i}. {e}")
    lines.extend(["", "### 8. 20 buenas prácticas", ""])
    for i, b in enumerate(data["best"], 1):
        lines.append(f"{i}. {b}")
    lines.extend(["", "### 9. Checklist operativo", ""])
    for c in data["checklist"]:
        lines.append(f"- ☐ {c}")
    lines.extend([
        "",
        "### 10. Ejercicio práctico (entorno QA)",
        "",
        data["exercise"],
        "",
        f"> Entorno: [{QA_URL}]({QA_URL})",
        "",
        "### 11. Evaluación — mini examen",
        "",
    ])
    for i, (q, opts, correct) in enumerate(data["quiz"], 1):
        lines.append(f"**{i}.** {q}")
        for j, o in enumerate(opts):
            lines.append(f"   - {chr(65 + j)}) {o}")
        lines.append(f"   - **Respuesta correcta:** {chr(65 + correct)}) {opts[correct]}")
        lines.append("")
    lines.extend(["### 12. Certificación del módulo", "", data["cert"], ""])
    if data.get("extra"):
        lines.extend(["", data["extra"], ""])
    lines.append("---\n")
    return "\n".join(lines)


def build_quick_start_guides() -> str:
    parts = [
        "# QUICK START GUIDES — Client First Edition",
        "",
        f"> Mini-cursos operativos · {COMPANY} · Productivo el día 1",
        "",
        f"**Entorno de práctica:** [{QA_URL}]({QA_URL})",
        "",
        "## Índice de módulos",
        "",
    ]
    for i, name in enumerate(MODULE_ORDER, 1):
        route = MODULE_META[name]["route"]
        parts.append(f"{i}. [{name}](#módulo-{name.lower()}) — `{route}`")
    parts.append("")
    parts.append("---\n")
    for name in MODULE_ORDER:
        if name not in MODULES:
            raise KeyError(f"Módulo sin contenido: {name}")
        parts.append(render_module(name))
    return "\n".join(parts)


def write_quick_start_guides(path: Path | None = None) -> Path:
    target = path or OUT_PATH
    target.parent.mkdir(parents=True, exist_ok=True)
    content = build_quick_start_guides()
    target.write_text(content, encoding="utf-8")
    return target


def main() -> None:
    path = write_quick_start_guides()
    line_count = len(path.read_text(encoding="utf-8").splitlines())
    modules = [m for m in MODULE_ORDER if f"## Módulo: {m}" in path.read_text(encoding="utf-8")]
    print(f"OK {path.relative_to(ROOT)}")
    print(f"Líneas: {line_count}")
    print(f"Módulos: {len(modules)}/13 — {', '.join(modules)}")


if __name__ == "__main__":
    main()
