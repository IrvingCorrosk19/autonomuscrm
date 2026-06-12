#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""AutonomusCRM University — Trailhead-style platform generator."""

from __future__ import annotations

import importlib.util
import json
import random
import sys
from pathlib import Path
from textwrap import dedent

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "Documentation" / "University"
CATALOG_PATH = ROOT / "AutonomusCRM.API" / "wwwroot" / "data" / "university-catalog.json"
QA_URL = "http://164.68.99.83:8091"

MODULES = {
    "Leads": {"route": "/Leads", "path": "commercial", "mins": 25},
    "Deals": {"route": "/Deals", "path": "commercial", "mins": 30},
    "Customers": {"route": "/Customers", "path": "commercial", "mins": 20},
    "Customer360": {"route": "/Customer360", "path": "commercial", "mins": 25},
    "Tasks": {"route": "/Tasks", "path": "commercial", "mins": 15},
    "RevenueOS": {"route": "/revenue", "path": "commercial", "mins": 20},
    "ExecutiveOS": {"route": "/executive", "path": "executive", "mins": 20},
    "TrustStudio": {"route": "/TrustInbox", "path": "admin", "mins": 25},
    "CustomerSuccess": {"route": "/customer-success", "path": "cs", "mins": 30},
    "Agents": {"route": "/Agents", "path": "admin", "mins": 20},
    "Users": {"route": "/Users", "path": "admin", "mins": 25},
    "Policies": {"route": "/Policies", "path": "admin", "mins": 20},
    "Audit": {"route": "/Audit", "path": "admin", "mins": 15},
}

MODULE_COPY = {
    "Leads": {
        "what": "Prospectos antes de ser clientes. Captura interés comercial.",
        "why": "Sin leads calificados no hay pipeline ni forecast.",
        "how": "Lista → Detalle → Contactar → Calificar → Convertir o Deal.",
        "flow": ["Recibir lead", "Contactar <24h", "Registrar nota", "Calificar BANT", "Crear deal o nurture"],
        "errors": ["No contactar a tiempo", "Calificar sin criterio", "Duplicar en Customers"],
        "best": ["Un lead una fuente", "Nota tras cada touch", "Qualify antes de deal grande"],
    },
    "Deals": {
        "what": "Oportunidades comerciales con valor y etapa en el pipeline.",
        "why": "El pipeline es el termómetro de ingresos futuros.",
        "how": "Crear deal → Avanzar etapas → Actualizar probabilidad → Won/Lost.",
        "flow": ["Discovery", "Propuesta", "Negociación", "Cierre", "Handoff CS"],
        "errors": ["Etapa sin actividad", "Probabilidad inflada", "Won sin contrato"],
        "best": ["Próximo paso siempre", "Lost con razón", "Revisar riesgo IA diario"],
    },
    "Customers": {
        "what": "Directorio de clientes activos post-venta o convertidos.",
        "why": "Fuente de verdad de relaciones y expansión.",
        "how": "Buscar → Detalle → Registrar contacto → Crear deal expansión.",
        "flow": ["Alta o conversión", "Datos completos", "Contactos registrados", "360 actualizado"],
        "errors": ["Duplicados", "Campos vacíos", "Sin owner"],
        "best": ["Buscar antes de crear", "Convención de nombres", "Segmentar por estado"],
    },
    "Customer360": {
        "what": "Vista unificada: deals, tickets, interacciones, salud.",
        "why": "Contexto antes de cada conversación importante.",
        "how": "Buscar cliente → Abrir 360 → Revisar historial → Actuar.",
        "flow": ["Búsqueda", "Timeline", "Señales", "Acción", "Registro"],
        "errors": ["Llamar sin 360", "Ignorar duplicados", "No leer tickets abiertos"],
        "best": ["360 antes de QBR", "360 antes de renovación", "360 en escalamiento"],
    },
    "Tasks": {
        "what": "Compromisos con fecha, prioridad y responsable.",
        "why": "Lo no agendado se olvida; el CRM es tu agenda de negocio.",
        "how": "Crear tarea → Vincular contexto → Completar o reprogramar.",
        "flow": ["Crear", "Priorizar", "Ejecutar", "Completar", "Siguiente paso"],
        "errors": ["Tareas genéricas", "Vencidas sin acción", "Sin vínculo a registro"],
        "best": ["Tarea tras cada reunión", "Revisar bandeja 2x/día", "Urgent solo si lo es"],
    },
    "RevenueOS": {
        "what": "Dashboard de ingresos, pipeline y métricas de forecast.",
        "why": "Visibilidad de cobertura vs cuota y concentración de riesgo.",
        "how": "Abrir Revenue OS → Filtrar periodo → Interpretar → Accionar.",
        "flow": ["MRR/ARR", "Pipeline", "Coverage", "Riesgo", "Plan"],
        "errors": ["Datos desactualizados", "Mirar solo total", "Ignorar concentración"],
        "best": ["Revisión semanal", "Cruzar con Deals", "Compartir con equipo"],
    },
    "ExecutiveOS": {
        "what": "Vista ejecutiva para juntas y decisiones estratégicas.",
        "why": "Liderazgo necesita narrativa + cifras en minutos.",
        "how": "Executive OS → Validar → Export board → Presentar.",
        "flow": ["Ingresos", "Riesgos", "Outcomes", "Export", "Junta"],
        "errors": ["Export sin validar", "Solo buenas noticias", "Sin plan de acción"],
        "best": ["Ritual viernes", "3 bullets ejecutivos", "Riesgo primero"],
    },
    "TrustStudio": {
        "what": "Aprobación humana de decisiones propuestas por IA.",
        "why": "Control, cumplimiento y calidad en automatización.",
        "how": "Bandeja → Leer contexto → Aprobar/Rechazar → Auditoría.",
        "flow": ["Alerta", "Revisar", "Decidir", "Documentar", "Seguimiento"],
        "errors": ["Aprobar sin leer", "Rechazar todo", "Dejar >24h"],
        "best": ["Política clara", "Turnos de revisión", "Feedback a IA"],
    },
    "CustomerSuccess": {
        "what": "Tickets, playbooks y salud de cartera post-venta.",
        "why": "Protege MRR, NPS y renovaciones.",
        "how": "Cola → Clasificar → Playbook → Resolver → Cerrar.",
        "flow": ["Ticket", "Severidad", "Playbook", "Resolución", "CSAT"],
        "errors": ["Cerrar sin confirmar", "SLA roto", "Sin causa raíz"],
        "best": ["Playbook correcto", "Escalar a tiempo", "Nota en 360"],
    },
    "Agents": {
        "what": "Agentes de IA que automatizan trabajo repetitivo.",
        "why": "Escala operación sin multiplicar headcount.",
        "how": "Workforce → Ver agentes → Supervisar → Trust Studio.",
        "flow": ["Activar", "Monitorear", "Aprobar", "Medir", "Ajustar"],
        "errors": ["Automatizar sin política", "No supervisar", "Desactivar por miedo"],
        "best": ["Empezar simple", "Medir ahorro", "Human-in-the-loop"],
    },
    "Users": {
        "what": "Gestión de accesos, roles y usuarios del tenant.",
        "why": "Seguridad y productividad empiezan por permisos correctos.",
        "how": "Users → Crear → Rol mínimo → Activar → Revisar.",
        "flow": ["Alta", "Rol", "Comunicar", "Capacitar", "Auditar"],
        "errors": ["Admin por defecto", "Usuarios fantasma", "Sin offboarding"],
        "best": ["Mínimo privilegio", "Offboarding día 1", "University obligatorio"],
    },
    "Policies": {
        "what": "Reglas ABAC de negocio y acceso.",
        "why": "Consistencia en descuentos, accesos y compliance.",
        "how": "Policies → Revisar → Alinear con proceso → Comunicar.",
        "flow": ["Definir", "Probar", "Publicar", "Capacitar", "Auditar"],
        "errors": ["Política sin dueño", "No comunicar cambios", "Excepciones informales"],
        "best": ["Change log", "Alineación legal", "Revisión trimestral"],
    },
    "Audit": {
        "what": "Trazabilidad de acciones para cumplimiento.",
        "why": "Enterprise exige evidencia de quién hizo qué.",
        "how": "Audit → Filtrar → Investigar → Reportar.",
        "flow": ["Evento", "Filtro", "Análisis", "Acción", "Cierre"],
        "errors": ["No revisar nunca", "Ignorar intentos denegados", "Sin retención"],
        "best": ["Revisión semanal admin", "Alertas Viewer", "Export para compliance"],
    },
}

PATHS = [
    {
        "id": "foundations",
        "title": "Fundamentos",
        "badge": "crm-explorer",
        "units": [
            {"id": "f-crm", "title": "Introducción a CRM", "mins": 10, "points": 100},
            {"id": "f-revops", "title": "Revenue Operations", "mins": 15, "points": 100},
            {"id": "f-cs", "title": "Customer Success", "mins": 15, "points": 100},
            {"id": "f-ai", "title": "IA Empresarial", "mins": 15, "points": 100},
            {"id": "f-nav", "title": "Navegación del Sistema", "mins": 20, "points": 150},
        ],
    },
    {
        "id": "commercial",
        "title": "Ruta Comercial",
        "badge": "sales-expert",
        "units": [
            {"id": "c-leads", "title": "Leads", "module": "Leads", "mins": 25, "points": 200},
            {"id": "c-deals", "title": "Deals y Pipeline", "module": "Deals", "mins": 30, "points": 200},
            {"id": "c-customers", "title": "Customers", "module": "Customers", "mins": 20, "points": 150},
            {"id": "c-360", "title": "Customer 360", "module": "Customer360", "mins": 25, "points": 200},
            {"id": "c-revenue", "title": "Revenue OS", "module": "RevenueOS", "mins": 20, "points": 200},
            {"id": "c-forecast", "title": "Forecast", "mins": 20, "points": 200},
        ],
    },
    {
        "id": "cs",
        "title": "Ruta Customer Success",
        "badge": "cs-hero",
        "units": [
            {"id": "s-tickets", "title": "Tickets", "module": "CustomerSuccess", "mins": 20, "points": 200},
            {"id": "s-playbooks", "title": "Playbooks", "mins": 25, "points": 200},
            {"id": "s-health", "title": "Customer Health", "module": "Customer360", "mins": 20, "points": 150},
            {"id": "s-renewals", "title": "Renewals", "mins": 25, "points": 200},
            {"id": "s-nps", "title": "NPS", "mins": 15, "points": 100},
            {"id": "s-churn", "title": "Churn Prevention", "mins": 25, "points": 200},
        ],
    },
    {
        "id": "admin",
        "title": "Ruta Administrativa",
        "badge": "trust-specialist",
        "units": [
            {"id": "a-users", "title": "Usuarios y Roles", "module": "Users", "mins": 25, "points": 200},
            {"id": "a-perms", "title": "Permisos", "mins": 20, "points": 150},
            {"id": "a-policies", "title": "Políticas ABAC", "module": "Policies", "mins": 20, "points": 200},
            {"id": "a-audit", "title": "Auditoría", "module": "Audit", "mins": 15, "points": 150},
            {"id": "a-trust", "title": "Trust Studio", "module": "TrustStudio", "mins": 25, "points": 250},
            {"id": "a-agents", "title": "Workforce / Agents", "module": "Agents", "mins": 20, "points": 200},
        ],
    },
    {
        "id": "executive",
        "title": "Ruta Ejecutiva",
        "badge": "executive-analyst",
        "units": [
            {"id": "e-exec", "title": "Executive OS", "module": "ExecutiveOS", "mins": 20, "points": 200},
            {"id": "e-revenue", "title": "Revenue OS Ejecutivo", "module": "RevenueOS", "mins": 20, "points": 200},
            {"id": "e-dash", "title": "Dashboards Command", "mins": 15, "points": 150},
            {"id": "e-kpis", "title": "Indicadores clave", "mins": 20, "points": 200},
            {"id": "e-ai-dec", "title": "Decisiones con IA", "module": "TrustStudio", "mins": 25, "points": 250},
        ],
    },
]

PLAYBOOKS = [
    ("Cliente Nuevo", ["Lead calificado o referido", "Crear/verificar Customer", "Customer 360 completo", "Deal onboarding si aplica", "Handoff a CS con nota", "Tareas día 1-7"]),
    ("Deal Estancado", ["Detectar en Command/Revenue", "Abrir deal y 360", "Identificar bloqueo", "Llamada reactivación", "Actualizar etapa o Lost", "Lección en nota"]),
    ("Renovación", ["Alerta 90 días", "QBR con métricas valor", "Playbook Renovación", "Propuesta", "Cierre o plan recuperación"]),
    ("Cliente en Riesgo", ["Señal en CS OS", "360 + health", "Playbook Recuperación", "War room si VIP", "Plan 30 días"]),
    ("Ticket Crítico", ["Clasificar P1", "Playbook Incidente", "Comunicar SLA", "Escalar si >2h", "Cerrar con confirmación"]),
    ("Escalación", ["Documentar en ticket/deal", "Notificar manager", "Executive si revenue impact", "Seguimiento cada 24h", "Post-mortem"]),
    ("Implementación", ["Kickoff", "Usuarios y roles", "Import datos", "Workflows", "Go-live + University"]),
    ("Onboarding Cliente", ["Welcome pack", "Tareas semana 1", "Health check día 14", "QBR día 30", "NPS día 45"]),
]

CERTS = [
    {"id": "sales-pro", "title": "AutonomusCRM Certified Sales Professional", "path": "commercial", "min_score": 80, "paths_required": ["foundations", "commercial"]},
    {"id": "cs-pro", "title": "AutonomusCRM Certified Customer Success Professional", "path": "cs", "min_score": 80, "paths_required": ["foundations", "cs"]},
    {"id": "admin", "title": "AutonomusCRM Certified Administrator", "path": "admin", "min_score": 85, "paths_required": ["foundations", "admin"]},
    {"id": "revops", "title": "AutonomusCRM Certified Revenue Operations Professional", "path": "commercial", "min_score": 80, "paths_required": ["foundations", "commercial", "executive"]},
    {"id": "exec", "title": "AutonomusCRM Certified Executive User", "path": "executive", "min_score": 75, "paths_required": ["foundations", "executive"]},
]

BADGES = [
    ("crm-explorer", "CRM Explorer", "Completa Fundamentos", 500),
    ("sales-expert", "Sales Expert", "Completa Ruta Comercial", 1200),
    ("cs-hero", "Customer Success Hero", "Completa Ruta CS", 1200),
    ("revenue-master", "Revenue Master", "Certificación RevOps", 2000),
    ("executive-analyst", "Executive Analyst", "Ruta Ejecutiva + cert", 1500),
    ("trust-specialist", "Trust Studio Specialist", "Trust Studio + Admin", 1000),
    ("ai-power", "AI Power User", "IA + Agents + Trust", 800),
    ("superadmin-elite", "SuperAdmin Elite", "Admin cert + auditoría", 2500),
]

def write(path: Path, content: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8")
    print(f"  OK {path.relative_to(ROOT)}")


def gen_questions(cert_id: str, cert_title: str, n: int = 50) -> list[dict]:
    rng = random.Random(cert_id)
    pool = [
        ("Un lead inbound debe contactarse en menos de:", ["24 horas", "1 semana", "Cuando haya tiempo", "Nunca"], 0, "Regla B2B de velocidad."),
        ("Customer 360 se usa principalmente para:", ["Contexto unificado", "Editar código", "Facturación legal", "Borrar datos"], 0, "Vista de negocio."),
        ("Trust Studio sirve para:", ["Aprobar decisiones IA", "Crear usuarios", "Import CSV", "Enviar email"], 0, "Human-in-the-loop."),
        ("Deal en etapa Discovery con 90% probabilidad es:", ["Error común", "Buena práctica", "Obligatorio", "Recomendado por IA"], 0, "Probabilidad debe alinear con etapa."),
        ("Al marcar deal Lost debes:", ["Documentar razón", "Borrar cliente", "Ocultar registro", "Nada"], 0, "Forecast honesto."),
        ("Renovación se anticipa típicamente a:", ["90 días", "1 día", "2 años", "No aplica"], 0, "Ventana CS estándar."),
        ("Ticket P1 requiere:", ["Respuesta urgente", "Cierre automático", "Solo email", "Ignorar SLA"], 0, "Severidad crítica."),
        ("Rol con escritura comercial UI:", ["Sales", "Viewer", "Solo Support", "Ninguno"], 0, "Admin/Manager/Sales."),
        ("Ctrl+K en AutonomusCRM:", ["Búsqueda global", "Cerrar sesión", "Dark mode", "Export"], 0, "Command palette."),
        ("Pipeline coverage saludable suele ser:", ["3x cuota", "0.5x", "Sin deals", "10x sin calidad"], 0, "Métrica RevOps."),
        ("Handoff ventas a CS debe incluir:", ["Nota de contexto", "Solo nombre", "Password", "Nada"], 0, "Continuidad."),
        ("Auditoría sirve para:", ["Trazabilidad compliance", "Marketing", "Diseño UI", "Precios"], 0, "Enterprise."),
        ("Revenue OS muestra:", ["Métricas de ingresos", "Código fuente", "Logs servidor", "Emails"], 0, "Dashboard."),
        ("Playbook se ejecuta en:", ["Customer Success", "Billing", "Landing", "Logout"], 0, "Módulo CS."),
        ("Usuario nuevo debe empezar en:", ["University", "Producción sin training", "API docs", "GitHub"], 0, "Adopción."),
    ]
    questions = []
    for i in range(n):
        q, opts, correct, explain = pool[i % len(pool)]
        questions.append({
            "id": f"{cert_id}-q{i+1}",
            "text": f"[{cert_title}] {q} (variante {i+1})",
            "options": opts,
            "correct": correct,
            "explanation": explain,
            "type": "multiple_choice",
        })
    # Add scenario questions
    for j in range(5):
        questions.append({
            "id": f"{cert_id}-scenario{j+1}",
            "text": f"Caso práctico {j+1}: Cliente VIP reporta caída. ¿Primer paso en AutonomusCRM?",
            "options": ["Abrir CS y playbook P1", "Borrar ticket", "Ignorar", "Cerrar deal"],
            "correct": 0,
            "explanation": "Severidad + playbook.",
            "type": "scenario",
        })
    return questions[:n]


def build_catalog(questions_by_cert: dict) -> dict:
    lessons = {}
    for path in PATHS:
        for unit in path["units"]:
            mod = unit.get("module")
            body = ""
            if mod and mod in MODULE_COPY:
                mc = MODULE_COPY[mod]
                body = f"**{unit['title']}** — {mc['what']}\n\n{mc['how']}"
            else:
                body = f"Unidad: {unit['title']}. Completa los objetivos y marca como terminada."
            lessons[unit["id"]] = {
                "id": unit["id"],
                "pathId": path["id"],
                "title": unit["title"],
                "durationMins": unit["mins"],
                "points": unit["points"],
                "route": MODULES.get(mod, {}).get("route") if mod else None,
                "content": body,
                "quiz": [{"q": "¿Completaste la práctica en el entorno?", "options": ["Sí", "No"], "correct": 0}],
            }
    return {
        "version": "1.0",
        "qaUrl": QA_URL,
        "paths": PATHS,
        "badges": [{"id": b[0], "title": b[1], "description": b[2], "points": b[3]} for b in BADGES],
        "certifications": CERTS,
        "exams": questions_by_cert,
        "lessons": lessons,
        "playbooks": [{"id": f"pb-{i}", "title": t, "steps": s} for i, (t, s) in enumerate(PLAYBOOKS)],
    }


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    print("Generating AutonomusCRM University...")

    # Master plan
    write(OUT / "AUTONOMUSCRM_UNIVERSITY_MASTER_PLAN.md", dedent(f"""
    # AUTONOMUSCRM UNIVERSITY — Master Plan

    > Trailhead interno · Productivo en 7 días · Certificado en 30 · Experto en 90

    ## Visión
    Plataforma de aprendizaje **micro-lecciones** integrada en `{QA_URL}/University` — no manuales largos.

    ## Principios (Salesforce Trailhead + HubSpot Academy)
    | Principio | Implementación |
    |-----------|----------------|
    | Bite-sized | Unidades 10-30 min |
    | Hands-on | Práctica en entorno QA |
    | Gamificación | Puntos, badges, ranking |
    | Certificación | 5 credenciales oficiales |
    | En app | Módulo University en Flow UI |

    ## Arquitectura de contenido
    ```mermaid
    flowchart TB
        MP[Master Plan] --> LP[Learning Paths]
        LP --> QS[Quick Starts]
        LP --> PB[Playbooks]
        LP --> EX[Exams]
        EX --> CERT[Certifications]
        CERT --> BADGE[Badges]
        UI[University UI] --> LP
    ```

    ## Objetivos de adopción
    | Hito | Meta |
    |------|------|
    | Día 7 | Operativo en módulos de su rol |
    | Día 30 | Certificación oficial |
    | Día 90 | Experto + mentor interno |
    | Día 30 adopción org | 90% usuarios activos University |
    | Día 60 | 95% |
    | Día 90 | 100% |

    ## Entregables
    Ver índice en `README.md` — 12 documentos + catálogo JSON + UI `/University`.

    ## Deprecación
    Manuales largos en `Documentation/Academy/_archive/` — **no usar para capacitación**.
    """).strip())

    # Learning paths
    lp = "# LEARNING PATHS\n\n> Rutas modulares — completa unidades para ganar puntos y badges.\n\n"
    for p in PATHS:
        lp += f"## {p['title']}\n\nBadge: **{p['badge']}**\n\n| Unidad | Duración | Puntos |\n|--------|----------|--------|\n"
        for u in p["units"]:
            lp += f"| {u['title']} | {u['mins']} min | {u['points']} |\n"
        lp += "\n```mermaid\nflowchart LR\n"
        for i, u in enumerate(p["units"]):
            lp += f"    U{i}[{u['title']}]\n"
        if len(p["units"]) > 1:
            lp += "    " + " --> ".join(f"U{i}" for i in range(len(p["units"]))) + "\n"
        lp += "```\n\n---\n\n"
    write(OUT / "LEARNING_PATHS.md", lp)

    # Quick starts (full mini-courses)
    _qs_script = Path(__file__).resolve().parent / "generate-quick-start-guides.py"
    _spec = importlib.util.spec_from_file_location("generate_quick_start_guides", _qs_script)
    _mod = importlib.util.module_from_spec(_spec)
    sys.modules["generate_quick_start_guides"] = _mod
    _spec.loader.exec_module(_mod)
    _mod.write_quick_start_guides(OUT / "QUICK_START_GUIDES.md")

    # Playbooks
    pb = "# PLAYBOOK LIBRARY\n\n> Procedimientos paso a paso — ejecutar en orden\n\n"
    for title, steps in PLAYBOOKS:
        pb += f"## {title}\n\n"
        for i, s in enumerate(steps, 1):
            pb += f"{i}. {s}\n"
        pb += "\n```mermaid\nflowchart TD\n    S[Inicio]\n"
        for i, s in enumerate(steps):
            pb += f"    S --> P{i}[{s[:30]}]\n"
            if i > 0:
                pb += f"    P{i-1} --> P{i}\n"
        pb += "```\n\n---\n\n"
    write(OUT / "PLAYBOOK_LIBRARY.md", pb)

    # Certifications
    cert_md = "# CERTIFICATION PROGRAM\n\n"
    for c in CERTS:
        cert_md += dedent(f"""
        ## {c['title']}

        | Campo | Valor |
        |-------|-------|
        | ID | `{c['id']}` |
        | Puntaje mínimo | {c['min_score']}% |
        | Rutas requeridas | {', '.join(c['paths_required'])} |

        ### Objetivos
        - Dominar módulos de la ruta en entorno real
        - Aprobar examen 50 preguntas + 5 casos
        - Completar checklist práctico con manager

        ### Temario
        Ver `LEARNING_PATHS.md` rutas: {', '.join(c['paths_required'])}

        ### Examen
        Ver `EXAM_LIBRARY.md` — sección `{c['id']}`

        ---
        """)
    write(OUT / "CERTIFICATION_PROGRAM.md", cert_md)

    # Exams
    questions_by_cert = {}
    exam_md = "# EXAM LIBRARY\n\n> 50 preguntas por certificación · MC + escenarios\n\n"
    for c in CERTS:
        qs_list = gen_questions(c["id"], c["title"], 50)
        questions_by_cert[c["id"]] = qs_list
        exam_md += f"## {c['title']} (`{c['id']}`)\n\n"
        for i, q in enumerate(qs_list[:10], 1):
            exam_md += f"**{i}.** {q['text']}\n"
            for j, o in enumerate(q["options"]):
                mark = " ✓" if j == q["correct"] else ""
                exam_md += f"   - {chr(65+j)}) {o}{mark}\n"
            exam_md += f"   *{q['explanation']}*\n\n"
        exam_md += f"*... +40 preguntas más en catálogo JSON y UI `/University/Exam/{c['id']}`*\n\n---\n\n"
    write(OUT / "EXAM_LIBRARY.md", exam_md)

    # Badges
    badge_md = "# BADGES SYSTEM\n\n| Badge | ID | Puntos | Cómo obtener |\n|-------|-----|--------|-------------|\n"
    for b in BADGES:
        badge_md += f"| 🏆 {b[1]} | `{b[0]}` | {b[3]} | {b[2]} |\n"
    badge_md += "\n## Progresión\n```mermaid\nflowchart LR\n    E[CRM Explorer] --> S[Sales/CS Hero]\n    S --> R[Revenue Master]\n    R --> X[Executive / Elite]\n```\n"
    write(OUT / "BADGES_SYSTEM.md", badge_md)

    # Onboarding 30
    o30 = "# ONBOARDING 30 DÍAS\n\n> Usuario operativo al día 30 · Certificación incluida\n\n| Día | Objetivo | Unidades University | Evidencia |\n|-----|----------|---------------------|----------|\n"
    days30 = [
        (1, "Primer acceso", "f-nav, f-crm", "Login + Command Center"),
        (2, "Fundamentos", "f-revops, f-cs", "Quiz Fundamentos"),
        (3, "IA", "f-ai", "Trust Studio visita"),
        (4, "Módulo rol #1", "Según rol", "Práctica QA"),
        (5, "Módulo rol #2", "Según rol", "Registro en CRM"),
        (7, "Semana 1 review", "Quick Start", "Manager sign-off"),
        (10, "Playbook", "1 playbook", "Simulación"),
        (15, "Ruta 50%", "Path del rol", "Puntos >500"),
        (20, "Examen blanco", "Exam practice", ">70%"),
        (25, "Casos prácticos", "4 escenarios", "3/4 OK"),
        (30, "Certificación", "Examen oficial", "Badge obtenido"),
    ]
    for d in range(1, 31):
        match = next((x for x in days30 if x[0] == d), None)
        if match:
            o30 += f"| {d} | {match[1]} | {match[2]} | {match[3]} |\n"
        else:
            o30 += f"| {d} | Práctica diaria | Unidad path rol | Actividad registrada |\n"
    write(OUT / "ONBOARDING_30_DAYS.md", o30)

    # Onboarding 90
    o90 = "# ONBOARDING 90 DÍAS — Programa Enterprise\n\n| Semana | Foco | Meta |\n|--------|------|------|\n"
    for w in range(1, 13):
        if w <= 4:
            o90 += f"| {w} | Fundamentos + ruta rol | {'Certificación' if w == 4 else 'Unidades + práctica'} |\n"
        elif w <= 8:
            o90 += f"| {w} | Profundización + playbooks | Experto módulos |\n"
        else:
            o90 += f"| {w} | Mentoring + optimización | Referente interno |\n"
    write(OUT / "ONBOARDING_90_DAYS.md", o90)

    # Videos
    vid = "# VIDEO ACADEMY — Catálogo y guiones\n\n"
    for name in MODULES:
        vid += f"## {name}\n\n| Video | Duración | Objetivo | Guion (resumen) | Resultado |\n|-------|----------|----------|-----------------|----------|\n"
        scripts = [
            (2, f"Qué es {name}", f"Mostrar {MODULES[name]['route']} — propósito de negocio", "Usuario explica el módulo"),
            (5, f"Flujo básico {name}", MODULE_COPY[name]["how"], "Usuario completa flujo"),
            (15, f"Profundización {name}", "Caso real TechSolutions", "Sin ayuda"),
            (30, f"Avanzado {name}", "Errores + buenas prácticas", "Listo para cert"),
            (45, f"Certificación {name}", "Simulación examen", "Aprobado"),
        ]
        for dur, obj, script, res in scripts:
            vid += f"| {name} {dur}min | {dur} min | {obj} | {script} | {res} |\n"
        vid += "\n"
    write(OUT / "VIDEO_ACADEMY.md", vid)

    # Adoption
    write(OUT / "ADOPTION_STRATEGY.md", dedent("""
    # ADOPTION STRATEGY — 90% · 95% · 100%

    ## KPIs de adopción
    | KPI | Fórmula | Meta D30 | Meta D60 | Meta D90 |
    |-----|---------|----------|----------|----------|
    | University DAU | Usuarios activos University / total | 70% | 85% | 95% |
    | Lesson completion | Lecciones completadas / asignadas | 80% | 90% | 100% |
    | Cert rate | Certificados / headcount | 50% | 80% | 100% |
    | CRM DAU | Login CRM + acción | 85% | 92% | 98% |
    | Data quality | Registros sin campos críticos vacíos | 75% | 88% | 95% |

    ## Dashboard de adopción
    - **Por usuario:** progreso %, puntos, badges, última lección (UI University + export Admin)
    - **Por departamento:** Ventas / CS / Admin — media de completitud
    - **Semanal:** reunión 15 min — ranking top 10 (gamificación)

    ## Tácticas
    1. University obligatorio día 1 antes de producción
    2. Manager no asigna pipeline hasta Quick Start completo
    3. Certificación ligada a evaluación de desempeño
    4. Champions por departamento
    5. Recordatorios en Command Center (banner adopción)
    """).strip())

    # Roadmap
    write(OUT / "UNIVERSITY_IMPLEMENTATION_ROADMAP.md", dedent("""
    # UNIVERSITY IMPLEMENTATION ROADMAP

    | Fase | Entregable | Estado |
    |------|------------|--------|
    | 1 | Learning Paths + docs | ✅ |
    | 2 | Quick Starts + Playbooks | ✅ |
    | 3 | Certifications + Exams | ✅ |
    | 4 | Badges + Onboarding | ✅ |
    | 5 | Video scripts | ✅ |
    | 6 | UI `/University` Flow | ✅ |
    | 7 | Progreso localStorage | ✅ |
    | 8 | Ranking + adoption KPIs | En UI |
    | 9 | Video producción | Pendiente grabación |
    | 10 | Persistencia DB progreso | Futuro (opcional) |

    ## Integración técnica
    - Catálogo: `wwwroot/data/university-catalog.json`
    - Páginas: `/University`, `/University/Lesson/{id}`, `/University/Exam/{id}`
    - Estilos: `flow-university.css` (Flow Design System)
    - Sin nuevos frameworks
    """).strip())

    # README
    write(OUT / "README.md", dedent(f"""
    # AutonomusCRM University

    > Plataforma Trailhead interna — **no leer manuales largos**

    ## Empezar
    1. Abrir **[University en la app]({QA_URL}/University)**
    2. Ruta **Fundamentos** (día 1)
    3. Quick Start de tu módulo: `QUICK_START_GUIDES.md`

    ## Documentos
    | # | Archivo |
    |---|---------|
    | 1 | AUTONOMUSCRM_UNIVERSITY_MASTER_PLAN.md |
    | 2 | LEARNING_PATHS.md |
    | 3 | QUICK_START_GUIDES.md |
    | 4 | PLAYBOOK_LIBRARY.md |
    | 5 | CERTIFICATION_PROGRAM.md |
    | 6 | EXAM_LIBRARY.md |
    | 7 | BADGES_SYSTEM.md |
    | 8 | ONBOARDING_30_DAYS.md |
    | 9 | ONBOARDING_90_DAYS.md |
    | 10 | VIDEO_ACADEMY.md |
    | 11 | ADOPTION_STRATEGY.md |
    | 12 | UNIVERSITY_IMPLEMENTATION_ROADMAP.md |

    ## Regenerar
    ```bash
    python scripts/generate-university-docs.py
    ```
    """).strip())

    catalog = build_catalog(questions_by_cert)
    CATALOG_PATH.parent.mkdir(parents=True, exist_ok=True)
    CATALOG_PATH.write_text(json.dumps(catalog, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"  OK {CATALOG_PATH.relative_to(ROOT)}")
    print("Done.")


if __name__ == "__main__":
    main()
