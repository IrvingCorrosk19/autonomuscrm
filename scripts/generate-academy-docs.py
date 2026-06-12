#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""AutonomusCRM Enterprise Academy — Document Generator (UTF-8)."""

from __future__ import annotations

import sys
from pathlib import Path
from typing import Any

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "Documentation" / "Academy"
GUIDES = OUT / "Guides"

QA_URL = "http://164.68.99.83:8091"
PASSWORD = "AutonomusTest123!"
TENANT = "TechSolutions Panamá"

ROLES: dict[str, dict[str, Any]] = {
    "SUPERADMIN": {
        "file": "SUPERADMIN_GUIDE",
        "title": "SuperAdmin",
        "persona": "Líder de plataforma y gobierno del tenant",
        "email": "superadmin@autonomuscrm.local",
        "impact": "Garantiza continuidad operativa, seguridad, cumplimiento y escalabilidad del CRM para toda la organización.",
        "revenue": "Habilita equipos comerciales y de éxito del cliente; reduce fricción operativa que destruye pipeline.",
        "satisfaction": "Asegura datos confiables, permisos correctos y respuestas rápidas a incidentes.",
        "growth": "Prepara la empresa para nuevos mercados, integraciones y automatización con IA.",
        "write_access": True,
        "admin_access": True,
        "focus": ["Gobierno del tenant", "Usuarios y roles", "Políticas ABAC", "Auditoría", "Integraciones", "Trust Studio", "Facturación"],
        "day_focus": ["Revisar salud del sistema", "Aprobar decisiones críticas de IA", "Auditar accesos", "Resolver incidentes de permisos"],
        "kpis": ["Uptime operativo del CRM", "Tiempo medio de resolución de incidentes", "% usuarios activos semanales", "Decisiones IA pendientes >24h", "Eventos fallidos sin resolver"],
    },
    "ADMIN": {
        "file": "ADMIN_GUIDE",
        "title": "Administrador",
        "persona": "Administrador operativo del CRM",
        "email": "admin@autonomuscrm.local",
        "impact": "Configura el CRM para que cada equipo trabaje con datos limpios, procesos claros y automatización confiable.",
        "revenue": "Elimina cuellos de botella (permisos, workflows, datos duplicados) que frenan cierres.",
        "satisfaction": "Estructura Customer Success, tickets y playbooks para respuestas consistentes.",
        "growth": "Escala usuarios, integraciones y políticas sin perder control.",
        "write_access": True,
        "admin_access": True,
        "focus": ["Usuarios", "Workflows", "Políticas", "Configuración", "Leads/Deals operativos", "Auditoría"],
        "day_focus": ["Onboarding de nuevos usuarios", "Revisar workflows", "Validar políticas", "Apoyar escalamientos comerciales"],
        "kpis": ["Tiempo de alta de usuario", "Workflows activos sin errores", "Tickets CS sin asignar", "Duplicados en directorio", "Cumplimiento de políticas"],
    },
    "MANAGER": {
        "file": "MANAGER_GUIDE",
        "title": "Gerente Comercial",
        "persona": "Líder de equipo de ingresos y operaciones comerciales",
        "email": "manager@autonomuscrm.local",
        "impact": "Convierte actividad individual en resultados de equipo predecibles.",
        "revenue": "Supervisa pipeline, forecast y coaching diario; desbloquea deals atascados.",
        "satisfaction": "Coordina handoff ventas a soporte y renovación.",
        "growth": "Identifica oportunidades de expansión y capacita al equipo.",
        "write_access": True,
        "admin_access": True,
        "focus": ["Pipeline", "Revenue OS", "Executive OS", "Equipo", "Tareas", "Trust Studio (aprobaciones)"],
        "day_focus": ["Stand-up de pipeline", "Revisar deals en riesgo", "Coaching a vendedores", "Forecast semanal"],
        "kpis": ["Pipeline coverage (3x cuota)", "Win rate", "Ciclo de venta medio", "Deals estancados >14 días", "Forecast accuracy"],
    },
    "SALES": {
        "file": "SALES_GUIDE",
        "title": "Ejecutivo Comercial",
        "persona": "Responsable de generar y cerrar oportunidades",
        "email": "sales1@autonomuscrm.local",
        "impact": "Transforma interés en ingresos firmes y relaciones de largo plazo.",
        "revenue": "Califica leads, avanza deals, cierra contratos y registra cada interacción.",
        "satisfaction": "Entrega clientes bien informados al equipo de éxito post-venta.",
        "growth": "Detecta upsell y cross-sell en cuentas existentes.",
        "write_access": True,
        "admin_access": False,
        "focus": ["Leads", "Pipeline", "Clientes", "Customer 360", "Tareas", "Llamadas de voz", "IA comercial"],
        "day_focus": ["Prospección", "Seguimiento", "Demos", "Propuestas", "Cierre y handoff"],
        "kpis": ["Leads contactados por día", "Tasa de conversión lead a oportunidad", "Valor pipeline personal", "Deals cerrados por mes", "Actividad registrada al 100 por ciento"],
    },
    "SUPPORT": {
        "file": "SUPPORT_GUIDE",
        "title": "Especialista de Soporte y Éxito del Cliente",
        "persona": "Guardián de la experiencia post-venta",
        "email": "support@autonomuscrm.local",
        "impact": "Protege ingresos recurrentes, reduce churn y eleva NPS.",
        "revenue": "Identifica señales de renovación, expansión y riesgo antes de que impacten MRR.",
        "satisfaction": "Resuelve incidentes, ejecuta playbooks y documenta cada contacto.",
        "growth": "Convierte clientes satisfechos en referidos y casos de expansión.",
        "write_access": False,
        "admin_access": False,
        "focus": ["Customer Success OS", "Customer 360", "Tickets", "Playbooks", "Tareas", "Trust Studio (lectura)"],
        "day_focus": ["Cola de tickets", "Clientes en riesgo", "Renovaciones próximas", "Escalamientos"],
        "kpis": ["CSAT", "Tiempo primera respuesta", "Tiempo resolución", "Churn rate", "Renovaciones a tiempo", "NPS"],
    },
    "VIEWER": {
        "file": "VIEWER_GUIDE",
        "title": "Analista / Observador",
        "persona": "Stakeholder que consume información sin modificar registros",
        "email": "viewer@autonomuscrm.local",
        "impact": "Toma decisiones informadas con datos en tiempo real sin riesgo operativo.",
        "revenue": "Identifica tendencias y anomalías para alertar a líderes.",
        "satisfaction": "Monitorea SLAs y salud de cartera desde dashboards.",
        "growth": "Apoya planificación estratégica con lectura de pipeline y outcomes.",
        "write_access": False,
        "admin_access": False,
        "focus": ["Command Center", "Revenue OS", "Executive OS", "Lectura de pipeline", "Auditoría (lectura)"],
        "day_focus": ["Revisar dashboards", "Exportar insights", "Reportar anomalías a gerencia"],
        "kpis": ["Informes entregados a tiempo", "Alertas válidas generadas", "Precisión de lectura de KPIs", "Adopción de rutina de revisión diaria"],
    },
}

SCREENS = [
    ("Command Center", "/", "Pulso del negocio: ingresos generados, protegidos, decisiones IA 24h.", "Siempre"),
    ("Trust Studio", "/TrustInbox", "Bandeja de aprobación humana para decisiones de IA (Human-in-the-Loop).", "role_admin"),
    ("Workforce", "/Agents", "Agentes de IA que automatizan tareas repetitivas.", "Día 2"),
    ("Revenue OS", "/revenue", "Dashboard de ingresos, pipeline y forecast.", "role_revenue"),
    ("Executive OS", "/executive", "Vista ejecutiva para juntas y board.", "role_exec"),
    ("Pipeline", "/Deals", "Oportunidades comerciales por etapa.", "role_pipeline"),
    ("Directorio", "/Customers", "Base de clientes activos.", "Día 1"),
    ("Customer 360", "/Customer360", "Vista unificada: historial, deals, tickets, interacciones.", "Día 1"),
    ("Customer Success", "/customer-success", "Tickets, playbooks y salud de cartera.", "role_cs"),
    ("Leads", "/Leads", "Prospectos no calificados o en calificación.", "role_leads"),
    ("Tareas", "/Tasks", "Compromisos con fecha y responsable.", "Día 1"),
    ("Usuarios", "/Users", "Gestión de accesos (Admin/Manager).", "role_users"),
    ("Políticas", "/Policies", "Reglas de negocio y acceso ABAC.", "role_policies"),
    ("Auditoría", "/Audit", "Trazabilidad de acciones para cumplimiento.", "role_audit"),
    ("Configuración", "/Settings", "Preferencias de tenant y cuenta.", "role_settings"),
]

BASE_ERRORS = [
    "No registrar llamadas ni emails en el CRM",
    "Dejar leads sin contactar más de 24 horas",
    "Crear cliente duplicado en lugar de buscar en directorio",
    "Avanzar etapa de deal sin criterio de la etapa",
    "Cerrar deal Won sin fecha de inicio de contrato",
    "Ignorar alertas de deals en riesgo del Command Center",
    "No usar Customer 360 antes de reuniones importantes",
    "Dejar tareas vencidas sin reprogramar",
    "Aprobar decisiones de IA sin leer contexto en Trust Studio",
    "Rechazar todas las sugerencias de IA por costumbre",
    "No documentar razón al marcar deal como Lost",
    "Mezclar idiomas en notas sin etiqueta clara",
    "Compartir credenciales entre usuarios",
    "Exportar datos sensibles sin autorización",
    "Saltarse calificación BANT en leads enterprise",
    "No convertir lead calificado a cliente antes del cierre",
    "Handoff ventas a soporte sin nota de contexto",
    "Prometer funcionalidades no confirmadas con producto",
    "Usar probabilidad 90% en etapa Discovery",
    "No actualizar forecast después de cambio de etapa",
    "Ignorar políticas ABAC configuradas por Admin",
    "Crear workflows duplicados para el mismo trigger",
    "No revisar eventos fallidos en plataforma",
    "Dejar usuarios inactivos con acceso activo",
    "No cerrar tickets resueltos en Customer Success",
    "Escalar todo en lugar de usar playbooks",
    "No ejecutar playbook correcto para el tipo de incidente",
    "Perder SLA de primera respuesta en tickets P1",
    "No identificar sponsor económico en deals B2B",
    "Trabajar deals fuera del pipeline oficial",
    "No segmentar leads por fuente/campaña",
    "Importar CSV sin validar formato",
    "Borrar registros en lugar de marcar Lost/Inactivo",
    "No usar filtros guardados en listas grandes",
    "Ignorar señales de churn en Customer Success OS",
    "No preparar QBR con datos de Revenue OS",
    "Confundir MRR con valor total del deal",
    "No alinear fecha cierre con ciclo de compra del cliente",
    "Dejar campos obligatorios vacíos para ir rápido",
    "No sincronizar actividades con calendario del equipo",
    "Revisar solo email y olvidar Command Center",
    "No usar búsqueda global Ctrl+K",
    "Crear tareas genéricas sin vínculo a registro",
    "No nombrar deals con convención del equipo",
    "Ignorar capacitación de certificación operativa",
    "Operar en producción sin haber pasado examen del rol",
    "No reportar bugs o fricción UX a Admin",
    "Asumir permisos de otro rol sin verificar",
    "No leer Quick Start impreso del primer día",
    "Mezclar datos de prueba con producción en demos",
]

ROLE_SPECIFIC_ERRORS: dict[str, list[str]] = {
    "SUPERADMIN": [
        "Cambiar políticas sin change log",
        "Otorgar rol Admin por conveniencia",
        "No revisar auditoría semanal",
        "Desactivar MFA conceptualmente",
        "Ignorar billing y límites de tenant",
    ],
    "ADMIN": [
        "Crear usuarios sin rol mínimo necesario",
        "No desactivar usuarios que salen de la empresa",
        "Configurar workflow sin probar en sandbox",
        "No comunicar mantenimientos al equipo",
    ],
    "MANAGER": [
        "Microgestionar cada campo del vendedor",
        "Pipeline review sin datos actualizados",
        "Aprobar descuentos sin margen",
        "No hacer coaching 1:1 semanal",
    ],
    "SALES": [
        "Prospección sin investigación previa",
        "Enviar propuesta genérica",
        "No pedir introducción al decisor",
        "Perseguir deals no calificados por ego",
    ],
    "SUPPORT": [
        "Cerrar ticket sin confirmación del cliente",
        "No etiquetar causa raíz",
        "Prometer SLA no acordado",
        "No escalar a tiempo",
    ],
    "VIEWER": [
        "Intentar editar registros (frustración)",
        "Sacar conclusiones sin validar con dueño del dato",
        "Distribuir exports sin clasificación de confidencialidad",
    ],
}

WORKDAY_BLOCKS_TEMPLATE = [
    ("08:00", "Arranque y priorización", "Revisar Command Center y bandeja de prioridades del día.", "Las primeras 60 minutos definen si reaccionas o lideras la jornada.", "Lista top 5 acciones con dueño y hora límite."),
    ("08:30", "Revisión de alertas", "Trust Studio, deals en riesgo, tickets críticos según tu rol.", "La IA y los workflows señalan lo urgente antes que el ruido.", "Cero sorpresas a mediodía."),
    ("09:00", "Bloque de ejecución #1", None, "Proteger tiempo profundo para trabajo de alto impacto.", "Avance medible en métrica clave del rol."),
    ("09:30", "Seguimiento a interesados", "Actualizar registros: leads, clientes o tickets con notas de contacto.", "Sin registro no hay forecast, handoff ni auditoría.", "CRM refleja la realidad del negocio."),
    ("10:00", "Coordinación interna", "Sync con ventas, soporte o admin según handoffs pendientes.", "El 40% de deals se pierden por falta de coordinación.", "Handoffs cerrados con próximo paso definido."),
    ("10:30", "Bloque de ejecución #2", None, "Mantener momentum en pipeline o cola de servicio.", "Etapa avanzada o ticket resuelto."),
    ("11:00", "Customer 360 / contexto", "Antes de cada llamada importante, abrir vista 360 del cliente.", "Personalización aumenta conversión y confianza.", "Conversaciones con contexto completo."),
    ("11:30", "Tareas y compromisos", "Completar o reprogramar tareas vencidas en `/Tasks`.", "Los compromisos olvidados erosionan credibilidad.", "Bandeja de tareas al día."),
    ("12:00", "Cierre de mañana", "Verificar que métricas del día AM están registradas.", "Gerencia toma decisiones con datos de hoy, no de ayer.", "Dashboard actualizado."),
    ("13:00", "Revisión post-almuerzo", "Command palette (Ctrl+K) para saltar a registros pendientes.", "Recuperar foco rápido tras pausa.", "Retoma en <5 minutos."),
    ("14:00", "Bloque de ejecución #3", None, "Ventana típica de reuniones con clientes.", "Propuesta enviada o caso resuelto."),
    ("15:00", "IA y automatización", "Revisar sugerencias de Workforce/Agents y aprobar o rechazar en Trust Studio si aplica.", "La IA amplifica tu capacidad; tú mantienes el criterio.", "Decisiones IA alineadas a política comercial."),
    ("16:00", "Pipeline / cola de servicio", None, "Última oportunidad del día para desbloquear lo crítico.", "Ningún deal/ticket crítico sin próximo paso."),
    ("16:30", "Documentación y calidad de datos", "Corregir campos vacíos, etapas incorrectas, duplicados evidentes.", "Datos sucios = forecast falso.", "Registros listos para reportes."),
    ("17:00", "Planificación del día siguiente", "Crear tareas para mañana con recordatorios.", "Arrancar con plan vence arrancar con ansiedad.", "Agenda del día +1 definida."),
    ("17:30", "Cierre de jornada", "Revisar KPIs personales vs meta diaria.", "Lo que no se mide no mejora.", "Autoevaluación honesta + 1 mejora para mañana."),
]

SCENARIOS_BY_ROLE: dict[str, list[dict[str, Any]]] = {
    "SUPERADMIN": [
        {"name": "Auditoría de acceso sospechoso", "story": "Alerta: usuario Viewer intentó acceder a edición de deal (denegado).", "steps": ["Revisar `/Audit`", "Confirmar política ABAC en `/Policies`", "Coaching al usuario o ajuste de rol", "Documentar incidente", "Comunicar a compliance"], "outcome": "Cumplimiento demostrable para cliente enterprise."},
        {"name": "Incidente de permisos masivo", "story": "Tras cambio de política, 12 usuarios reportan acceso denegado a Deals.", "steps": ["Command Center — alcance", "Revisar `/Policies` y diff", "Rollback o ajuste granular", "Validar con usuario piloto", "Comunicar resolución"], "outcome": "Acceso restaurado en <4h; post-mortem documentado."},
        {"name": "Decisión IA crítica de descuento", "story": "IA propone 35% descuento en deal enterprise $200K.", "steps": ["Abrir Trust Studio", "Leer contexto y margen", "Consultar política comercial", "Aprobar/rechazar con nota", "Notificar a Manager"], "outcome": "Decisión auditada; margen protegido."},
        {"name": "Onboarding tenant filial", "story": "TechSolutions abre operación en Costa Rica; requiere segmentación.", "steps": ["Settings — preferencias", "Crear políticas territorio", "Usuarios con rol mínimo", "Workflows de asignación", "Validar Command Center"], "outcome": "Filial operativa en 5 días hábiles."},
        {"name": "Eventos fallidos en integración", "story": "Webhook de facturación falla 47 veces en 24h.", "steps": ["Revisar eventos fallidos", "Escalar a proveedor", "Pausar workflow si necesario", "Comunicar a finanzas", "Reactivar tras fix"], "outcome": "Integración estable; cero pérdida de datos."},
        {"name": "Revisión semanal de gobierno", "story": "Ritual viernes: salud de plataforma y cumplimiento.", "steps": ["Executive OS export", "Audit muestra semanal", "Trust Studio backlog", "Usuarios inactivos", "Plan semana siguiente"], "outcome": "Informe ejecutivo de 1 página entregado."},
        {"name": "Crisis de seguridad credenciales", "story": "Empleado comparte password en chat interno.", "steps": ["Reset forzado en `/Users`", "Auditoría de acciones recientes", "Política de contraseñas", "Capacitación express", "Cierre de ticket interno"], "outcome": "Riesgo contenido; política reforzada."},
        {"name": "Escalamiento billing límite tenant", "story": "Tenant alcanza 95% de licencias contratadas.", "steps": ["Revisar Settings/billing", "Proyección con Revenue OS", "Negociar upgrade con CFO", "Ajustar usuarios si temporal", "Documentar decisión"], "outcome": "Continuidad sin bloqueo de altas."},
        {"name": "Migración de datos legacy", "story": "Importación 500 clientes desde CRM anterior.", "steps": ["Plantilla CSV validada", "Import en sandbox", "Deduplicación Customer 360", "Go-live supervisado", "Auditoría post-migración"], "outcome": "Migración sin duplicados críticos."},
        {"name": "War room churn múltiple", "story": "Tres cuentas enterprise en riesgo simultáneo.", "steps": ["Executive OS concentración", "Roles claros en Tasks", "Trust Studio priorizado", "Comunicación CEO", "Seguimiento diario"], "outcome": "2 de 3 recuperadas; lecciones en playbook."},
    ],
    "ADMIN": [
        {"name": "Nuevo empleado comercial", "story": "Contratas SDR sin experiencia en CRM.", "steps": ["Crear usuario Sales en `/Users`", "Asignar mentor y tareas onboarding", "Enviar Quick Start", "Validar login día 1", "Revisión KPI día 15"], "outcome": "Productivo en 7 días; primera oportunidad día 10."},
        {"name": "Importación masiva post-evento", "story": "Feria IT Expo: 200 tarjetas escaneadas.", "steps": ["Preparar CSV plantilla", "Import en `/Leads`", "Deduplicar Customer 360", "Asignar por territorio", "Reportar a Marketing"], "outcome": "200 leads en <2h sin duplicar clientes activos."},
        {"name": "Workflow de asignación leads", "story": "Leads inbound sin dueño generan retraso de 48h.", "steps": ["Mapear trigger en workflow", "Regla round-robin por territorio", "Probar con lead de prueba", "Activar en producción", "Monitorear 72h"], "outcome": "Tiempo asignación <15 min."},
        {"name": "Política ABAC descuentos", "story": "Managers aprueban descuentos sin trazabilidad.", "steps": ["Definir umbral en `/Policies`", "Trust Studio para >15%", "Capacitar managers", "Auditar primera semana", "Ajustar si fricción"], "outcome": "100% descuentos mayores auditados."},
        {"name": "Limpieza duplicados directorio", "story": "Auditoría encuentra 23 clientes duplicados.", "steps": ["Export lista duplicados", "Fusionar en Customer 360", "Actualizar deals vinculados", "Notificar a dueños de cuenta", "Checklist prevención"], "outcome": "Directorio unificado; forecast confiable."},
        {"name": "Ticket CS sin asignar", "story": "Cola de soporte con 8 tickets >4h sin dueño.", "steps": ["Customer Success OS cola", "Regla auto-asignación", "Escalar a Support lead", "SLA comunicado a clientes", "Post-mortem proceso"], "outcome": "Cola cero en 24h."},
        {"name": "Configuración playbooks CS", "story": "Nuevo playbook Renovación 90 días.", "steps": ["Documentar pasos negocio", "Crear en Customer Success", "Vincular tareas tipo", "Capacitar Support", "Piloto con 3 cuentas"], "outcome": "Playbook adoptado al 100% en renovaciones."},
        {"name": "Mantenimiento planificado", "story": "Actualización de módulo Revenue OS sábado 02:00.", "steps": ["Ventana comunicada 72h antes", "Backup verificado", "Ejecutar deploy", "Smoke test post-deploy", "All-clear al equipo"], "outcome": "Cero tickets por mantenimiento no comunicado."},
        {"name": "Usuario saliente desactivación", "story": "Vendedor senior deja la empresa viernes.", "steps": ["Desactivar en `/Users` inmediato", "Reasignar deals en `/Deals`", "Reasignar tareas", "Auditoría accesos últimos 7 días", "Handoff cartera a manager"], "outcome": "Cero acceso post-salida; cartera continua."},
        {"name": "Escalamiento comercial urgente", "story": "Sales bloqueado por permiso en propuesta.", "steps": ["Reproducir con usuario", "Revisar Policies/rol", "Fix mínimo necesario", "Comunicar a solicitante", "Documentar en Audit"], "outcome": "Desbloqueo <1h; sin sobre-permisos."},
    ],
    "MANAGER": [
        {"name": "Deal estancado 21 días", "story": "Oportunidad Infraestructura Cloud sin actividad 3 semanas.", "steps": ["Abrir deal en `/Deals`", "Customer 360 última interacción", "Llamada reactivación con vendedor", "Actualizar probabilidad o Lost", "Coaching en stand-up"], "outcome": "Deal reactivado o cerrado limpio para forecast."},
        {"name": "Upsell detectado por IA", "story": "Agente sugiere módulo Workforce a cliente 50+ usuarios.", "steps": ["Trust Studio contexto", "Validar Customer 360", "Sales crea deal expansión", "Aprobar descuento si aplica", "Seguimiento semanal"], "outcome": "+$18K ARR expansión."},
        {"name": "Crisis de churn múltiple", "story": "Tres cuentas mid-market señal roja misma semana.", "steps": ["Executive OS riesgo", "War room CS+Sales", "Playbook Recuperación", "CEO informado vía board", "Review diario 14 días"], "outcome": "2 de 3 salvadas; 1 churn con entrevista salida."},
        {"name": "Pipeline review lunes", "story": "Ritual semanal con equipo de 6 vendedores.", "steps": ["Revenue OS cobertura", "Deals >14 días sin actividad", "Coaching por vendedor", "Forecast commit actualizado", "Tareas de seguimiento"], "outcome": "Forecast accuracy +8% en trimestre."},
        {"name": "Descuento fuera de política", "story": "Cliente exige 25%; política máxima 15% sin VP.", "steps": ["Trust Studio si IA involucrada", "Calcular margen en deal", "Negociar valor no precio", "Escalar a CRO si necesario", "Documentar decisión"], "outcome": "Cierre con margen o Lost documentado."},
        {"name": "Nuevo vendedor ramping", "story": "SDR promovido a AE; primera cartera propia.", "steps": ["Asignar deals nurture", "Shadowing 2 semanas", "KPIs progresivos", "Revisión 1:1 semanal", "Certificación Sales"], "outcome": "Primer cierre mes 2."},
        {"name": "Conflicto territorio cuenta", "story": "Dos AEs reclaman mismo cliente enterprise.", "steps": ["Customer 360 historial", "Política territorio", "Decisión documentada", "Handoff único dueño", "Comunicación a ambos"], "outcome": "Un dueño; relación cliente intacta."},
        {"name": "Forecast fin de trimestre", "story": "Q4: commit $1.2M con riesgo en 3 deals grandes.", "steps": ["Deals por probabilidad", "Validar con cada AE", "Plan B para at-risk", "Executive OS presentación", "Daily stand-up últimas 2 semanas"], "outcome": "Commit cumplido 94%."},
        {"name": "Handoff ventas a CS fallido", "story": "Cliente nuevo sin contexto en onboarding.", "steps": ["Revisar nota handoff", "Reunión tripartita Sales-CS-Cliente", "Actualizar Customer 360", "Playbook Onboarding", "NPS a 30 días"], "outcome": "NPS onboarding >8."},
        {"name": "Coaching vendedor bajo KPI", "story": "AE con 40% actividad registrada vs equipo.", "steps": ["Datos Revenue OS actividad", "1:1 diagnóstico", "Plan 30 días acciones", "Check-in semanal", "Re-certificación si persiste"], "outcome": "Actividad al 90% en 4 semanas."},
    ],
    "SALES": [
        {"name": "Lead inbound LinkedIn", "story": "María, directora TI RetailMax, descarga whitepaper.", "steps": ["`/Leads` filtrar hoy", "Llamar <15 min", "Nota descubrimiento", "Calificar BANT", "Crear deal Discovery si califica"], "outcome": "Oportunidad $45K con fecha cierre estimada."},
        {"name": "Demo enterprise multi-stakeholder", "story": "Cuenta bancaria: 5 asistentes en demo Command Center.", "steps": ["Customer 360 investigación", "Agenda con pain points", "Demo Revenue OS + 360", "Siguiente paso propuesta", "Tarea follow-up 48h"], "outcome": "Propuesta solicitada; deal en Proposal."},
        {"name": "Objeción precio mid-market", "story": "Cliente compara con competidor 30% más barato.", "steps": ["No bajar precio inmediato", "ROI con Revenue OS datos", "Trust Studio si descuento IA", "Propuesta valor", "Involucrar manager si >15%"], "outcome": "Cierre con valor o Lost competitivo documentado."},
        {"name": "Reactivación lead frío", "story": "Lead de hace 6 meses vuelve a abrir email.", "steps": ["Historial en Customer 360", "Email personalizado contexto", "Llamada si abre", "Actualizar etapa lead", "Nurture o descarte"], "outcome": "15% reactivación a oportunidad."},
        {"name": "Cierre Won y handoff CS", "story": "Contrato $80K firmado; inicio en 15 días.", "steps": ["Deal Won con fecha contrato", "Nota handoff completa", "Intro email CS+cliente", "Tarea CS onboarding", "Celebrar y siguiente cuenta"], "outcome": "Onboarding CS sin sorpresas."},
        {"name": "Lost con aprendizaje", "story": "Cliente elige competidor por integración.", "steps": ["Marcar Lost razón específica", "Nota para producto", "Mantener relación nurture", "Tarea revisión 6 meses", "Compartir en stand-up"], "outcome": "Inteligencia competitiva para equipo."},
        {"name": "Expansión cuenta existente", "story": "Cliente actual necesita 20 licencias adicionales.", "steps": ["Customer 360 salud", "Deal expansión vinculado", "Propuesta rápida", "Manager si descuento", "Cierre y actualización MRR"], "outcome": "+$24K ARR sin nuevo logo."},
        {"name": "Prospección outbound día", "story": "Meta: 30 contactos, 5 conversaciones.", "steps": ["Lista ICP en Leads", "Bloque 09:00-11:00 llamadas", "Registrar cada intento", "3 tareas follow-up", "Revisar KPI fin día"], "outcome": "Pipeline +$120K ponderado."},
        {"name": "Negociación final legal", "story": "Legal cliente pide cambios contrato estándar.", "steps": ["No prometer sin legal interno", "Tarea a admin/legal", "Mantener deal caliente", "Fecha límite clara", "Documentar en deal"], "outcome": "Firma en 10 días sin perder relación."},
        {"name": "Multithreading decisor", "story": "Solo contactas usuario; sin acceso a CFO.", "steps": ["Mapa stakeholders en 360", "Pedir intro al sponsor", "Valor económico para CFO", "Reunión ejecutiva", "Actualizar probabilidad"], "outcome": "Acceso decisor; deal avanza a Negotiation."},
    ],
    "SUPPORT": [
        {"name": "Cliente VIP ticket P1", "story": "Banco Nacional: caída integración API.", "steps": ["`/customer-success` playbook P1", "Notificar Admin integración", "Touchpoint cada 30 min", "Escalar manager si SLA <2h", "Cierre con confirmación cliente"], "outcome": "Incidente resuelto; NPS preservado."},
        {"name": "Renovación anual en riesgo", "story": "Contrato $120K vence 45 días; uso bajó 30%.", "steps": ["Customer 360 señales salud", "Playbook Renovación", "QBR con sponsor", "Plan valor documentado", "Deal o nota renovación"], "outcome": "Renovación firmada o churn con lecciones."},
        {"name": "Onboarding día 1 cliente nuevo", "story": "Handoff Sales llegó incompleto.", "steps": ["Leer deal Won y notas", "Llamada kickoff 60 min", "Playbook Onboarding", "Tareas 30-60-90", "Health score baseline"], "outcome": "Cliente operativo semana 2."},
        {"name": "Ticket repetido mismo cliente", "story": "Tercer ticket mismo mes por mismo síntoma.", "steps": ["Etiquetar causa raíz", "Escalar técnico L2", "Comunicar progreso proactivo", "QBR adelantado si VIP", "Actualizar playbook"], "outcome": "Causa raíz resuelta; confianza restaurada."},
        {"name": "NPS detractor recuperación", "story": "Cliente puntúa 4/10 post-incidente.", "steps": ["Llamada empatía 24h", "Plan acción concreto", "Seguimiento 7 y 30 días", "Nota en Customer 360", "Alerta manager si VIP"], "outcome": "NPS recuperado a 7+ en 60 días."},
        {"name": "Expansión desde soporte", "story": "Cliente pregunta por módulo adicional en ticket.", "steps": ["Documentar interés en 360", "Notificar Sales", "Mantener ticket resuelto", "No vender agresivo", "Seguimiento expansión"], "outcome": "Deal expansión creado; CSAT intacto."},
        {"name": "SLA P2 en cola alta", "story": "15 tickets P2; capacidad equipo 8/día.", "steps": ["Priorizar VIP y renovaciones", "Comunicar tiempos honestos", "Pedir refuerzo manager", "Templates respuesta", "Post-mortem capacidad"], "outcome": "SLA cumplido 92%; plan contratación."},
        {"name": "Churn inevitable documentado", "story": "Cliente confirma no renovará por presupuesto.", "steps": ["Entrevista salida estructurada", "Documentar razones", "Oferta win-back si aplica", "Cierre limpio accesos", "Lecciones a producto"], "outcome": "Churn limpio; inteligencia para retención."},
        {"name": "Playbook adopción baja", "story": "Clientes no usan feature clave; riesgo valor.", "steps": ["Identificar cohorte", "Webinar adopción", "Tareas guiadas en CS", "Métricas uso 30 días", "Celebrar quick wins"], "outcome": "Adopción +40%; health score verde."},
        {"name": "Escalamiento a ingeniería", "story": "Bug confirmado bloquea operación cliente.", "steps": ["Reproducir y documentar", "Ticket interno prioridad", "Comunicación cliente cada 4h", "Workaround si existe", "Cierre con confirmación"], "outcome": "Resolución 18h; cliente informado siempre."},
    ],
    "VIEWER": [
        {"name": "Reporte ejecutivo viernes", "story": "CEO pide estado ingresos y riesgos para junta.", "steps": ["`/executive` export board", "Validar Revenue OS", "Listar 3 riesgos top", "Narrativa 3 bullets", "Enviar antes 17:00"], "outcome": "Junta informada en 15 min preparación."},
        {"name": "Anomalía pipeline detectada", "story": "Deal $500K saltó etapa sin actividad registrada.", "steps": ["Verificar en `/Deals`", "Alertar manager dueño", "No editar registro", "Documentar en informe", "Seguimiento resolución"], "outcome": "Dato corregido; calidad reporte preservada."},
        {"name": "Dashboard semanal RevOps", "story": "Ritual lunes: KPIs para CRO.", "steps": ["Command Center métricas", "Revenue OS tendencias", "Comparar vs semana anterior", "Destacar desviaciones", "Distribuir PDF interno"], "outcome": "CRO recibe informe 08:30 puntual."},
        {"name": "Concentración riesgo cartera", "story": "40% MRR en 2 clientes; alerta estratégica.", "steps": ["Executive OS concentración", "Customer 360 por cuenta", "Validar con CS", "Memo para CEO", "Seguimiento trimestral"], "outcome": "Plan diversificación iniciado."},
        {"name": "Lectura auditoría compliance", "story": "Auditor externo pide muestra accesos Q3.", "steps": ["`/Audit` filtros período", "Export sin datos sensibles extra", "Validar con Admin", "Entregar paquete", "Archivar solicitud"], "outcome": "Auditoría sin hallazgos críticos."},
        {"name": "Benchmark interno equipos", "story": "Comparar win rate región PA vs CR.", "steps": ["Revenue OS segmentar", "Deals por región lectura", "Tabla comparativa", "Hipótesis con manager", "No compartir fuera sin aprobación"], "outcome": "Insight accionable para CRO."},
        {"name": "Preparación board trimestral", "story": "Material Q4 para directorio.", "steps": ["Executive OS + Revenue OS", "Gráficos tendencia 4 trimestres", "Riesgos y oportunidades", "Revisión con CFO", "Versión final export"], "outcome": "Board deck listo 5 días antes."},
        {"name": "Monitoreo SLA Customer Success", "story": "SLA primera respuesta cayó 8% este mes.", "steps": ["Customer Success OS métricas", "Identificar cola y causas", "Informe a COO", "No asignar tickets", "Seguimiento mes siguiente"], "outcome": "COO activa plan capacidad."},
        {"name": "Validación forecast antes commit", "story": "Manager pide segunda opinión datos.", "steps": ["Revenue OS forecast", "Deals at-risk lista", "Cruzar con actividad", "Memo independiente", "Presentar en pipeline review"], "outcome": "Forecast ajustado -5%; más realista."},
        {"name": "Tendencia churn 6 meses", "story": "Churn subió de 2% a 4.5% en mid-market.", "steps": ["Customer Success datos", "Cohortes por tamaño", "Razones Lost documentadas", "Visualización tendencia", "Recomendación a CSM"], "outcome": "Iniciativa retención mid-market lanzada."},
    ],
}

CASE_STUDIES: dict[str, list[dict[str, str]]] = {
    "SUPERADMIN": [
        {"title": "TechSolutions Panamá — Primer año de gobierno", "context": "Empresa 120 usuarios, 3 integraciones, auditoría SOC2 pendiente.", "challenge": "Políticas inconsistentes y backlog Trust Studio >50.", "actions": "Ritual semanal gobierno; políticas ABAC simplificadas; SLA interno IA 4h.", "result": "Auditoría SOC2 aprobada; decisiones IA <24h promedio."},
        {"title": "Fusión de dos unidades de negocio", "context": "Adquisición regional requiere unificar CRM en 60 días.", "challenge": "Duplicados, roles conflictivos, resistencia al cambio.", "actions": "Migración por fases; superusuarios por área; comunicación semanal CEO.", "result": "Un solo tenant; pipeline unificado día 58."},
    ],
    "ADMIN": [
        {"title": "Reducción tiempo alta usuario 5 días a 4 horas", "context": "RRHH enviaba tickets manuales; errores de rol frecuentes.", "challenge": "Plantilla y workflow inexistentes.", "actions": "Workflow onboarding; checklist rol mínimo; video 5 min.", "result": "Alta mismo día; cero tickets rol incorrecto en Q2."},
        {"title": "Limpieza post-campaña marketing", "context": "3.000 leads importados; 12% duplicados con clientes.", "challenge": "Marketing presiona velocidad vs calidad.", "actions": "Reglas dedup; asignación automática; reporte calidad semanal.", "result": "SQL rate +22%; quejas ventas -80%."},
    ],
    "MANAGER": [
        {"title": "Turnaround equipo bajo cuota", "context": "Equipo 6 AEs al 62% de cuota a mitad de año.", "challenge": "Pipeline inflado; actividad baja.", "actions": "Pipeline hygiene; coaching diario 15 min; forecast honesto.", "result": "Cierre año 98% cuota colectiva."},
        {"title": "Lanzamiento producto nuevo", "context": "Nuevo módulo IA; equipo sin experiencia vendiendo.", "challenge": "Propuestas genéricas; ciclo largo.", "actions": "Playbook venta; demos estándar; Trust Studio en proceso.", "result": "$400K pipeline nuevo en 90 días."},
    ],
    "SALES": [
        {"title": "De cero a President's Club", "context": "AE nuevo en TechSolutions; sin cartera asignada.", "challenge": "Construir pipeline desde leads fríos.", "actions": "50 llamadas/día; Customer 360 obsesivo; mentor manager.", "result": "Top 3 empresa año 1; $1.1M cerrado."},
        {"title": "Salvar deal enterprise en riesgo", "context": "Deal $300K estancado; sponsor cambió.", "challenge": "Sin acceso nuevo decisor.", "actions": "Multithreading; Executive OS para exec sponsor interno; valor ROI.", "result": "Won 45 días después; referencia pública."},
    ],
    "SUPPORT": [
        {"title": "De soporte reactivo a CS proactivo", "context": "NPS 6.2; churn 8% anual.", "challenge": "Solo tickets; sin playbooks renovación.", "actions": "Playbooks 90/60/30; QBR trimestral; health score.", "result": "NPS 8.1; churn 4.2% en 12 meses."},
        {"title": "Recuperación cuenta VIP post-P1", "context": "Banco Nacional incidente 6h; amenaza cancelación.", "challenge": "Confianza destruida.", "actions": "War room; comunicación CEO; crédito servicio; plan prevención.", "result": "Renovación + expansión $50K."},
    ],
    "VIEWER": [
        {"title": "Insight que evitó sorpresa en board", "context": "Forecast manager optimista +15%.", "challenge": "CEO necesitaba segunda opinión.", "actions": "Análisis deals at-risk; actividad cruzada; memo independiente.", "result": "Guidance ajustado; confianza inversores preservada."},
        {"title": "Programa de reporting self-service", "context": "CRO inundado de pedidos ad-hoc.", "challenge": "Viewer único con acceso; cuello de botella.", "actions": "Plantillas semanales; catálogo informes; SLA 24h.", "result": "Tiempo CRO en datos -60%."},
    ],
}

EXERCISES_BY_CHAPTER: dict[str, list[str]] = {
    "ch1": [
        "Redacta tu elevator pitch del rol en 30 segundos usando solo lenguaje de negocio.",
        "Identifica 3 stakeholders que dependen de tu trabajo diario y qué les entregas.",
        "Enumera 5 resultados medibles que tu manager espera este trimestre.",
    ],
    "ch2": [
        "Completa login en entorno QA y captura Command Center (anotación, no screenshot obligatorio).",
        "Visita cada pantalla marcada Día 1 y escribe una frase de valor de negocio.",
        "Practica Ctrl+K: encuentra un cliente, un deal y una tarea en <2 min cada uno.",
    ],
    "ch3": [
        "Simula una jornada completa en papel: asigna tus tareas reales a cada bloque horario.",
        "Identifica qué bloque sueles saltarte y diseña un recordatorio.",
        "Pairing 1h con colega certificado: observa su rutina 08:00-10:00.",
    ],
    "ch4": [
        "Ejecuta 2 escenarios en entorno QA con mentor validando cada paso.",
        "Para cada escenario, completa el cuadro de decisión antes de actuar.",
        "Escribe qué harías diferente si el cliente fuera VIP.",
    ],
    "ch5": [
        "Auto-evaluación: ¿cuántos de los 50 errores cometiste la semana pasada?",
        "Crea tu checklist personal de 10 errores a evitar.",
        "Role-play: compañero simula error; tú aplicas prevención y corrección.",
    ],
    "ch6": [
        "Define meta numérica para tu KPI #1 con tu manager.",
        "Identifica en qué pantalla medirás cada KPI esta semana.",
        "Redacta plan de mejora 30 días para tu KPI más débil.",
    ],
    "ch7": [
        "Lista 3 tareas delegables a IA y 3 que nunca delegarías.",
        "Procesa 5 decisiones en Trust Studio (entorno QA) con nota de criterio.",
        "Discute con manager: ¿dónde está el límite de automatización en tu rol?",
    ],
    "ch8": [
        "Completa checklist competencias al 100% ítems críticos.",
        "Simula examen: responde 10 preguntas muestra sin consultar guía.",
        "Solicita observación manager en operación real 2h.",
    ],
}

AI_SCENARIOS_BY_ROLE: dict[str, list[dict[str, str]]] = {
    "SUPERADMIN": [
        {"title": "Política IA vs automatización", "desc": "IA sugiere auto-aprobar descuentos <5%. Tú defines que todo pase por Trust Studio en enterprise."},
        {"title": "Agente de limpieza de datos", "desc": "Workforce detecta 200 registros incompletos. Apruebas batch con límite 50/día."},
    ],
    "ADMIN": [
        {"title": "Workflow sugerido por IA", "desc": "Agente propone workflow asignación leads. Pruebas en sandbox antes de aprobar."},
        {"title": "Duplicados detectados", "desc": "IA lista 15 posibles duplicados. Validas manualmente antes de fusión."},
    ],
    "MANAGER": [
        {"title": "Deal at-risk scoring", "desc": "Command Center marca 4 deals. Priorizas coaching con datos de actividad."},
        {"title": "Forecast assist", "desc": "IA sugiere ajuste -10% forecast. Validas con cada AE antes de commit."},
    ],
    "SALES": [
        {"title": "Siguiente mejor acción", "desc": "IA sugiere llamar a lead X. Verificas contexto 360 antes de marcar."},
        {"title": "Borrador email propuesta", "desc": "Workforce genera borrador. Personalizas antes de enviar; nunca auto-envío."},
    ],
    "SUPPORT": [
        {"title": "Clasificación ticket", "desc": "IA sugiere severidad P2 vs P1. Validas síntomas con playbook."},
        {"title": "Riesgo churn", "desc": "Alerta health score rojo. Ejecutas playbook antes de escalar."},
    ],
    "VIEWER": [
        {"title": "Anomalía en dashboard", "desc": "IA señala pico inusual pipeline. Investigas lectura; alertas manager con evidencia."},
        {"title": "Resumen ejecutivo auto", "desc": "Borrador narrativa semanal. Validas cifras en Revenue OS antes de distribuir."},
    ],
}


def write_file(path: Path, content: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8")
    rel = path.relative_to(ROOT)
    print(f"  OK {rel}")


def screen_learn_first(role_key: str, r: dict, learn_tag: str) -> str | None:
    admin_roles = {"SUPERADMIN", "ADMIN", "MANAGER"}
    if learn_tag == "role_admin":
        return "Día 1" if role_key in admin_roles else "Día 3"
    if learn_tag == "role_revenue":
        return "Día 1" if role_key in {"MANAGER", "SALES", "VIEWER"} else "Semana 1"
    if learn_tag == "role_exec":
        return "Día 2" if role_key in {"MANAGER", "VIEWER"} else "Semana 2"
    if learn_tag == "role_pipeline":
        return "Día 1" if r["write_access"] else "Día 2"
    if learn_tag == "role_cs":
        return "Día 1" if role_key == "SUPPORT" else "Semana 1"
    if learn_tag == "role_leads":
        return "Día 1" if role_key == "SALES" else "Semana 1"
    if learn_tag == "role_users":
        return "Día 2" if r["admin_access"] else "N/A"
    if learn_tag == "role_policies":
        return "Semana 1" if r["admin_access"] else "N/A"
    if learn_tag == "role_audit":
        return "Día 3" if role_key in {"SUPERADMIN", "ADMIN"} else "Opcional"
    if learn_tag == "role_settings":
        return "Día 2" if r["admin_access"] else "N/A"
    return learn_tag


def get_workday_blocks(r: dict) -> list[tuple[str, str, str, str, str]]:
    blocks = []
    day_idx = 0
    for time, title, what, why, result in WORKDAY_BLOCKS_TEMPLATE:
        if what is None:
            what = r["day_focus"][day_idx % len(r["day_focus"])]
            day_idx += 1
        blocks.append((time, title, what, why, result))
    return blocks


def get_errors(role_key: str, r: dict) -> list[str]:
    combined = BASE_ERRORS + ROLE_SPECIFIC_ERRORS.get(role_key, [])
    while len(combined) < 50:
        combined.append(
            f"Error recurrente #{len(combined) + 1}: no aplicar checklist de cierre diario del rol {r['title']}"
        )
    return combined[:50]


def scenario_mermaid(steps: list[str]) -> str:
    lines = ["flowchart TD", "    S[Inicio]"]
    prev = "S"
    for i, step in enumerate(steps[:5], 1):
        node = f"A{i}"
        label = step.replace("`", "").replace('"', "'")[:40]
        lines.append(f"    {prev} --> {node}[{label}]")
        prev = node
    lines.append(f"    {prev} --> O[Resultado]")
    return "\n".join(lines)


def expand_scenario_section(si: int, sc: dict, r: dict) -> list[str]:
    lines = [
        f"## Escenario 4.{si} — {sc['name']}",
        "",
        f"**Historia:** {sc['story']}",
        "",
        "**Stakeholders:** Cliente · Tu equipo · Manager · (si aplica) Admin/SuperAdmin",
        "",
        "**Precondiciones:** Acceso al entorno QA; Customer 360 disponible; mentor asignado si es primera vez.",
        "",
        "**Tiempo estimado:** 45-90 minutos (incluye documentación en CRM).",
        "",
        "**Flujo:**",
        "",
        "```mermaid",
        scenario_mermaid(sc["steps"]),
        "```",
        "",
        "**Pasos detallados:**",
        "",
    ]
    for n, st in enumerate(sc["steps"], 1):
        lines.append(f"{n}. {st}")
        lines.append(f"   - *Verificación:* Registro actualizado y próximo paso con fecha.")
        lines.append("")
    lines.extend([
        f"**Resultado de negocio:** {sc['outcome']}",
        "",
        "**Cuadro de decisión:**",
        "",
        "| Pregunta | Sí | No |",
        "|----------|----|----|",
        "| ¿Tengo contexto 360? | Continuar | Abrir Customer 360 primero |",
        "| ¿Próximo paso definido? | Ejecutar | Crear tarea con fecha |",
        "| ¿Requiere aprobación? | Trust Studio | Proceder |",
        "| ¿Impacta forecast? | Notificar manager | Registrar y continuar |",
        "",
        "**Errores comunes en este escenario:**",
        "",
        "- Omitir registro de actividad antes de cambiar etapa.",
        "- No definir dueño del siguiente paso.",
        "- Ignorar alertas del Command Center relacionadas.",
        "",
        "**Preguntas de reflexión:**",
        "",
        "1. ¿Qué KPI de tu rol mejora si ejecutas bien este escenario?",
        "2. ¿Qué habrías hecho diferente con un cliente VIP?",
        "3. ¿Qué documentación dejó el siguiente rol listo para actuar?",
        "",
        "**Práctica en entorno QA:**",
        "",
        f"- URL: {QA_URL}",
        f"- Usuario: `{r['email']}`",
        "- Busca registros seed o crea datos de práctica con prefijo ACADEMY-",
        "",
        "---",
        "",
    ])
    return lines


def build_role_guide(role_key: str, r: dict) -> str:
    lines: list[str] = [
        f"# {r['title']} — Guía Operativa AutonomusCRM Academy",
        "",
        "> **Programa:** AutonomusCRM Enterprise Academy  ",
        f"> **Rol:** {r['title']}  ",
        f"> **Usuario de práctica:** `{r['email']}`  ",
        f"> **Entorno:** {QA_URL}  ",
        "",
        "---",
        "",
        "## Tabla de contenido",
        "",
        "1. [Capítulo 1 — Bienvenida e impacto](#capítulo-1--bienvenida-e-impacto)",
        "2. [Capítulo 2 — Mi primer día](#capítulo-2--mi-primer-día)",
        "3. [Capítulo 3 — Mi jornada laboral completa](#capítulo-3--mi-jornada-laboral-completa)",
        "4. [Capítulo 4 — Procesos y escenarios reales](#capítulo-4--procesos-y-escenarios-reales)",
        "5. [Capítulo 5 — Errores más comunes](#capítulo-5--errores-más-comunes)",
        "6. [Capítulo 6 — Indicadores de desempeño](#capítulo-6--indicadores-de-desempeño)",
        "7. [Capítulo 7 — Uso de IA en tu rol](#capítulo-7--uso-de-ia-en-tu-rol)",
        "8. [Capítulo 8 — Certificación operativa](#capítulo-8--certificación-operativa)",
        "",
        "---",
        "",
        "# Capítulo 1 — Bienvenida e impacto",
        "",
        f"## Quién eres como {r['title']}",
        "",
        r["persona"],
        "",
        r["impact"],
        "",
        "### Tu impacto en la empresa",
        "",
        "```mermaid",
        "flowchart LR",
        f"    A[Tú: {r['title']}] --> B[Ingresos]",
        "    A --> C[Satisfacción cliente]",
        "    A --> D[Crecimiento sostenible]",
        "    B --> E[Empresa rentable]",
        "    C --> E",
        "    D --> E",
        "```",
        "",
        "| Dimensión | Tu contribución |",
        "|-----------|-----------------|",
        f"| **Ingresos** | {r['revenue']} |",
        f"| **Satisfacción** | {r['satisfaction']} |",
        f"| **Crecimiento** | {r['growth']} |",
        "",
        f"### Historia real: primer mes en {TENANT}",
        "",
        f'Imagina tu primer lunes en **{TENANT}**, empresa B2B de servicios tecnológicos. El CEO te dice: *"No necesito que aprendas software; necesito que protejas nuestro pipeline y nuestros clientes."* Esta guía te lleva de cero a productivo usando AutonomusCRM como sistema nervioso del negocio — no como formulario digital.',
        "",
        "### Áreas de enfoque de tu rol",
        "",
    ]
    for f in r["focus"]:
        lines.append(f"- {f}")
    lines.extend([
        "",
        "### Mapa mental de tu rol",
        "",
        "```mermaid",
        "mindmap",
        f"  root(({r['title']}))",
    ])
    for f in r["focus"][:6]:
        lines.append(f"    {f.replace(' ', '_')}")
    lines.extend([
        "```",
        "",
        "### Ejercicio 1.1 — Autodiagnóstico",
        "",
        "Responde por escrito (15 min):",
        "",
        "1. ¿Qué resultado medible debe lograr tu rol este trimestre?",
        "2. ¿Quién depende de tu trabajo diario?",
        "3. ¿Qué pasa en la empresa si no inicias sesión durante una semana?",
        "",
        "### Ejercicios adicionales Capítulo 1",
        "",
    ])
    for ex in EXERCISES_BY_CHAPTER["ch1"]:
        lines.append(f"- {ex}")
    lines.extend(["", "### Estudios de caso", ""])
    for cs in CASE_STUDIES.get(role_key, []):
        lines.extend([
            f"#### {cs['title']}",
            "",
            f"**Contexto:** {cs['context']}",
            "",
            f"**Desafío:** {cs['challenge']}",
            "",
            f"**Acciones:** {cs['actions']}",
            "",
            f"**Resultado:** {cs['result']}",
            "",
            "**Lecciones aplicables hoy:**",
            "",
            "1. ¿Qué elemento replicarías mañana en tu jornada?",
            "2. ¿Qué riesgo similar existe en tu cartera actual?",
            "",
        ])
    lines.extend([
        "---",
        "",
        "# Capítulo 2 — Mi primer día",
        "",
        "## 2.1 Acceso al sistema",
        "",
        "| Paso | Acción | Por qué |",
        "|------|--------|---------|",
        f"| 1 | Ir a {QA_URL}/Account/Login | Punto de entrada seguro |",
        f"| 2 | Email: `{r['email']}` | Identidad única auditada |",
        "| 3 | Contraseña: (proporcionada por Admin) | Nunca compartir |",
        "| 4 | TenantId: dejar vacío o cero | Búsqueda por email |",
        "| 5 | Tras login → Command Center `/` | Tu centro de mando |",
        "",
        "```mermaid",
        "sequenceDiagram",
        "    participant U as Tú",
        "    participant CRM as AutonomusCRM",
        "    participant CC as Command Center",
        "    U->>CRM: Login email + password",
        "    CRM->>CC: Redirección post-auth",
        "    CC->>U: Métricas + prioridades del día",
        "```",
        "",
        "## 2.2 Recorrido guiado — Qué significa cada pantalla",
        "",
        "| Pantalla | Ruta | Qué significa para el negocio | Aprender |",
        "|----------|------|--------------------------------|----------|",
    ])
    for nav, path, meaning, tag in SCREENS:
        learn = screen_learn_first(role_key, r, tag)
        if learn == "N/A" and not r["admin_access"]:
            continue
        lines.append(f"| {nav} | `{path}` | {meaning} | {learn} |")
    lines.extend([
        "",
        "## 2.3 Configuración personal esencial",
        "",
        "1. **Idioma** — Selector en barra superior (es/en).",
        "2. **Tema** — Modo claro/oscuro según preferencia.",
        "3. **Búsqueda global** — `Ctrl+K` para saltar a cualquier registro.",
        "4. **Marcadores mentales** — Memoriza 3 rutas de tu rol para la semana 1.",
        "",
        "### Simulación 2.A — Primer login (30 min)",
        "",
        "1. Inicia sesión con tu usuario de práctica.",
        "2. Permanece 5 min en Command Center sin hacer clic: solo lee métricas.",
        "3. Abre cada pantalla de la tabla anterior marcada Día 1.",
        "4. Escribe una frase de negocio (no técnica) describiendo cada pantalla.",
        "",
        "### Ejercicios Capítulo 2",
        "",
    ])
    for ex in EXERCISES_BY_CHAPTER["ch2"]:
        lines.append(f"- {ex}")
    lines.extend([
        "",
        "### Tabla de decisión — ¿Qué pantalla abro primero?",
        "",
        "| Situación | Pantalla | Ruta |",
        "|-----------|----------|------|",
        "| Inicio del día | Command Center | `/` |",
        "| Antes de llamada cliente | Customer 360 | `/Customer360` |",
        "| Revisar pipeline | Deals | `/Deals` |",
        "| Aprobar IA | Trust Studio | `/TrustInbox` |",
        "| Ticket abierto | Customer Success | `/customer-success` |",
        "",
        "---",
        "",
        "# Capítulo 3 — Mi jornada laboral completa",
        "",
        "> Jornada tipo B2B — adapta horarios a tu zona. La lógica es universal.",
        "",
    ])
    for time, title, what, why, result in get_workday_blocks(r):
        lines.extend([
            f"## {time} — {title}",
            "",
            "| | |",
            "|---|---|",
            f"| **Qué hacer** | {what} |",
            f"| **Por qué** | {why} |",
            f"| **Resultado esperado** | {result} |",
            "",
            "**Tips para tu rol:**",
            "",
            f"- Prioriza `{r['focus'][0]}` si el tiempo se agota.",
            "- Usa Ctrl+K para no perder minutos navegando.",
            "- Registra actividad antes de pasar al siguiente bloque.",
            "",
            "**Micro-ejercicio (5 min):** Anota qué métrica de negocio validarías al terminar este bloque.",
            "",
        ])
    lines.extend([
        "```mermaid",
        "gantt",
        "    title Jornada tipo AutonomusCRM",
        "    dateFormat HH:mm",
        "    axisFormat %H:%M",
        "    section Mañana",
        "    Priorización     :08:00, 30m",
        "    Ejecución        :09:00, 120m",
        "    Coordinación     :11:00, 60m",
        "    section Tarde",
        "    Clientes         :14:00, 120m",
        "    IA y cierre      :16:00, 90m",
        "```",
        "",
        "### Ejercicios Capítulo 3",
        "",
    ])
    for ex in EXERCISES_BY_CHAPTER["ch3"]:
        lines.append(f"- {ex}")
    lines.extend([
        "",
        "---",
        "",
        "# Capítulo 4 — Procesos y escenarios reales",
        "",
    ])
    scenarios = SCENARIOS_BY_ROLE.get(role_key, [])
    for si, sc in enumerate(scenarios, 1):
        lines.extend(expand_scenario_section(si, sc, r))
    lines.extend([
        "## Proceso maestro — Ciclo de vida del cliente",
        "",
        "```mermaid",
        "flowchart LR",
        "    L[Lead] --> Q{Calificado?}",
        "    Q -->|Sí| C[Cliente]",
        "    Q -->|No| N[Nurture]",
        "    C --> D[Deal]",
        "    D --> W{Won?}",
        "    W -->|Sí| ON[Onboarding CS]",
        "    W -->|No| ARCH[Archivo + razón]",
        "    ON --> R[Renovación / Expansión]",
        "```",
        "",
        "### Ejercicios Capítulo 4",
        "",
    ])
    for ex in EXERCISES_BY_CHAPTER["ch4"]:
        lines.append(f"- {ex}")
    lines.extend([
        "",
        "---",
        "",
        "# Capítulo 5 — Errores más comunes",
        "",
        "> Top 50 errores observados en adopción CRM enterprise — y cómo evitarlos.",
        "",
    ])
    for ei, err in enumerate(get_errors(role_key, r), 1):
        lines.extend([
            f"### Error {ei} — {err}",
            "",
            "| | |",
            "|---|---|",
            "| **Consecuencia** | Pérdida de tiempo, forecast falso, riesgo de churn o incumplimiento. |",
            "| **Prevención** | Checklist diario, mentoría día 1-7, usar plantillas del rol. |",
            "| **Corrección** | Actualizar registro hoy; documentar lección en nota del cliente/deal. |",
            "",
            f"**Ejemplo en {TENANT}:** Un colega cometió este error; el impacto fue retraso de forecast 1 semana. La corrección inmediata fue actualizar Customer 360 y notificar al manager.",
            "",
        ])
    lines.extend([
        "### Ejercicios Capítulo 5",
        "",
    ])
    for ex in EXERCISES_BY_CHAPTER["ch5"]:
        lines.append(f"- {ex}")
    lines.extend([
        "",
        "---",
        "",
        "# Capítulo 6 — Indicadores de desempeño",
        "",
        "## KPIs de tu rol",
        "",
        "| KPI | Meta sugerida | Dónde medir | Alerta si... |",
        "|-----|---------------|-------------|--------------|",
    ])
    for k in r["kpis"]:
        lines.append(f"| {k} | Definir con manager | Command / Revenue / CS | Tendencia 2 semanas negativa |")
    lines.extend([
        "",
        "## Interpretación ejecutiva",
        "",
        "```mermaid",
        "flowchart TD",
        "    K[KPI en rojo] --> D{Diagnóstico}",
        "    D -->|Datos| FIX[Calidad de registro]",
        "    D -->|Proceso| TRN[Capacitación]",
        "    D -->|Mercado| STR[Estrategia]",
        "    FIX --> P[Plan 30 días]",
        "    TRN --> P",
        "    STR --> P",
        "```",
        "",
        "### Profundización por KPI",
        "",
    ])
    for k in r["kpis"]:
        lines.extend([
            f"#### {k}",
            "",
            "**Definición de negocio:** Métrica acordada con tu manager que refleja contribución del rol.",
            "",
            "**Ritual de medición:** Revisar al cierre de jornada (17:30) y en stand-up semanal.",
            "",
            "**Acción si está en rojo:** Diagnóstico en 24h; plan de mejora documentado en tarea vinculada.",
            "",
            "**Pregunta para tu manager:** ¿Cuál es la meta numérica este trimestre?",
            "",
        ])
    lines.extend([
        "### Plan de mejora personal (plantilla)",
        "",
        "1. KPI más débil esta semana: ___________",
        "2. Causa raíz probable: ___________",
        "3. Acción concreta mañana: ___________",
        "4. Fecha de revisión con manager: ___________",
        "",
        "### Ejercicios Capítulo 6",
        "",
    ])
    for ex in EXERCISES_BY_CHAPTER["ch6"]:
        lines.append(f"- {ex}")
    lines.extend([
        "",
        "---",
        "",
        "# Capítulo 7 — Uso de IA en tu rol",
        "",
        "## IA para usuarios de negocio (sin tecnicismos)",
        "",
        "AutonomusCRM integra IA como **copiloto operativo**: detecta riesgos, sugiere acciones y automatiza tareas repetitivas. **Tú** conservas la decisión final en Trust Studio.",
        "",
        "| Capacidad IA | Beneficio | Riesgo si se ignora | Riesgo si se abusa |",
        "|--------------|-----------|---------------------|---------------------|",
        "| Detección deals en riesgo | Salvar ingresos | Churn de pipeline | Falsos positivos sin revisión |",
        "| Sugerencias Workforce | Ahorro de tiempo | Burnout manual | Automatizar sin política |",
        "| Aprobación HITL | Control y cumplimiento | Decisiones no auditadas | Cuello de botella si no revisas |",
        "",
    ])
    for ai in AI_SCENARIOS_BY_ROLE.get(role_key, []):
        lines.extend([
            f"### Escenario IA — {ai['title']}",
            "",
            ai["desc"],
            "",
            "**Tu decisión:** Aprobar · Rechazar · Escalar — siempre con nota de negocio.",
            "",
        ])
    lines.extend([
        "### Escenario IA 7.1 — Command Center",
        "",
        "El Command Center muestra *3 decisiones pendientes*. Abres Trust Studio, lees el contexto de cada una, apruebas 2 y rechazas 1 porque contradice política comercial. **Eso es operación enterprise con IA responsable.**",
        "",
        "```mermaid",
        "sequenceDiagram",
        "    participant CC as Command Center",
        "    participant TS as Trust Studio",
        "    participant U as Tú",
        "    CC->>U: Alerta decisión pendiente",
        "    U->>TS: Revisar contexto",
        "    U->>TS: Aprobar o Rechazar",
        "    TS->>CC: Estado actualizado",
        "```",
        "",
        "### Ejercicios Capítulo 7",
        "",
    ])
    for ex in EXERCISES_BY_CHAPTER["ch7"]:
        lines.append(f"- {ex}")
    lines.extend([
        "",
        "---",
        "",
        "# Capítulo 8 — Certificación operativa",
        "",
        "## Checklist de competencias",
        "",
        "- [ ] Inicio de sesión y navegación sin ayuda",
        "- [ ] Explicar Command Center en lenguaje de negocio",
    ])
    if r["write_access"]:
        lines.extend([
            "- [ ] Crear y actualizar registro comercial con calidad",
            "- [ ] Cerrar o perder deal con documentación",
        ])
    if role_key == "SUPPORT":
        lines.extend([
            "- [ ] Resolver ticket usando playbook correcto",
            "- [ ] Escalar incidente P1 en <30 min",
        ])
    if r["admin_access"]:
        lines.extend([
            "- [ ] Crear usuario con rol mínimo necesario",
            "- [ ] Explicar diferencia entre rol y permiso comercial",
        ])
    lines.extend([
        "- [ ] Usar Customer 360 antes de reunión simulada",
        "- [ ] Completar jornada tipo Capítulo 3 en entorno de práctica",
        "- [ ] Aprobar/rechazar decisión IA con criterio",
        "- [ ] Identificar 10 errores del Capítulo 5 en simulación",
        "",
        "## Criterios de aprobación",
        "",
        "| Componente | Peso | Mínimo |",
        "|------------|------|--------|",
        "| Examen teórico | 30% | 80% |",
        "| Casos prácticos | 40% | 3/4 aprobados |",
        "| Checklist operativo | 20% | 100% ítems críticos |",
        "| Observación manager | 10% | Satisfactorio |",
        "",
        f"**Examen completo:** ver `ROLE_CERTIFICATION_EXAMS.md` — sección **{r['title']}**.",
        "",
        "### Ejercicios Capítulo 8",
        "",
    ])
    for ex in EXERCISES_BY_CHAPTER["ch8"]:
        lines.append(f"- {ex}")
    lines.extend([
        "",
        "### FAQ de certificación",
        "",
        "**¿Puedo reintentar el examen?** Sí, tras 5 días y revisión con mentor.",
        "",
        "**¿El entorno QA es el mismo que producción?** Misma interfaz; datos de práctica.",
        "",
        f"**¿Contraseña práctica?** `{PASSWORD}`",
        "",
        "---",
        "",
        f"*AutonomusCRM Enterprise Academy — {r['title']} — Documento generado para capacitación operativa.*",
    ])
    return "\n".join(lines) + "\n"


def build_master_guide() -> str:
    return f"""# AUTONOMUSCRM ACADEMY — Master Guide

> **Visión:** Convertir a cualquier colaborador sin experiencia previa en un operador productivo de AutonomusCRM en menos de 90 días, con certificación por rol.

---

## 1. ¿Qué es un CRM?

Un **Customer Relationship Management (CRM)** es el sistema donde vive la relación completa entre tu empresa y sus clientes potenciales y activos.

| Sin CRM | Con AutonomusCRM |
|---------|------------------|
| Información en emails y hojas sueltas | Una fuente de verdad compartida |
| Forecast "a ojo" | Pipeline medible y auditable |
| Clientes olvidados post-venta | Customer Success proactivo |
| Decisiones reactivas | IA + humanos en bucle controlado |

```mermaid
mindmap
  root((CRM))
    Ventas
      Leads
      Pipeline
      Cierre
    Clientes
      Directorio
      Customer 360
    Éxito
      Tickets
      Renovación
    Inteligencia
      Command Center
      IA
      Trust Studio
```

---

## 2. Revenue Operations (RevOps)

**RevOps** alinea ventas, marketing y éxito del cliente bajo métricas comunes de ingresos.

```mermaid
flowchart TB
    M[Marketing] --> L[Leads]
    S[Ventas] --> D[Deals]
    D --> C[Clientes]
    C --> CS[Customer Success]
    CS --> R[Renovación y expansión]
    R --> D
```

En AutonomusCRM: **Leads → Deals → Customers → Customer Success → Revenue OS** forman este ciclo.

---

## 3. Customer Success

Customer Success no es "soporte técnico". Es **garantizar que el cliente logre valor** y renueve.

| Fase | Objetivo | Módulo AutonomusCRM |
|------|----------|---------------------|
| Onboarding | Primer valor en 30 días | Customer 360 + Tasks |
| Adopción | Uso constante | Playbooks |
| Renovación | Proteger MRR | Customer Success OS |
| Expansión | Crecer cuenta | Deals + IA |

---

## 4. IA aplicada a negocios

La IA en AutonomusCRM **no reemplaza** tu criterio. **Amplifica** detección y ejecución:

1. **Detecta** — deals en riesgo, anomalías, oportunidades.
2. **Sugiere** — próximos pasos vía Workforce/Agents.
3. **Ejecuta con supervisión** — Trust Studio (Human-in-the-Loop).

```mermaid
sequenceDiagram
    participant IA as IA Autonomus
    participant TS as Trust Studio
    participant U as Usuario
    IA->>TS: Propone decisión
    U->>TS: Aprueba / Rechaza
    TS->>IA: Aprendizaje y auditoría
```

**Riesgos a gestionar:** falsos positivos, automatización sin política, apruebas masivas sin leer.

---

## 5. Cómo funciona AutonomusCRM — Flujo del negocio

### 5.1 Arquitectura de negocio (no técnica)

```mermaid
flowchart LR
    subgraph Entrada
        WEB[Landing / Campañas]
        REF[Referidos]
        EVENT[Eventos]
    end
    subgraph CRM
        CMD[Command Center]
        LEAD[Leads]
        DEAL[Pipeline]
        CUST[Customers]
        CS[Customer Success]
    end
    subgraph Salida
        REV[Ingresos]
        NPS[Satisfacción]
    end
    WEB --> LEAD
    REF --> LEAD
    EVENT --> LEAD
    LEAD --> DEAL
    DEAL --> CUST
    CUST --> CS
    CMD --> LEAD
    CMD --> DEAL
    CMD --> CS
    DEAL --> REV
    CS --> NPS
    CS --> REV
```

### 5.2 Módulos por propósito de negocio

| Área | Pantallas | Para qué sirve |
|------|-----------|----------------|
| **Mando** | Command, Trust Studio, Workforce | Priorizar, supervisar IA, automatizar |
| **Ingresos** | Revenue OS, Executive OS, Deals | Medir, proyectar, cerrar |
| **Relación** | Customers, Customer 360, Leads | Conocer y captar |
| **Post-venta** | Customer Success, Tasks | Retener y expandir |
| **Plataforma** | Users, Policies, Audit, Settings | Gobierno y cumplimiento |

### 5.3 Roles y responsabilidad

| Rol | Mandato principal |
|-----|-------------------|
| SuperAdmin / Admin | Gobierno, usuarios, políticas |
| Manager | Pipeline del equipo, forecast |
| Sales | Prospección, calificación, cierre |
| Support | Tickets, renovación, churn |
| Viewer | Inteligencia sin modificar datos |

---

## 6. Ruta de aprendizaje recomendada

```mermaid
flowchart TD
    START[Master Guide] --> QS[Quick Start 2 páginas]
    QS --> RG[Guía de rol 8 capítulos]
    RG --> ONB[Onboarding Día 1-90]
    RG --> EX[Examen certificación]
    EX --> CERT[Badge operativo]
    CERT --> ADV[Training avanzado]
```

| Semana | Entregable | Resultado |
|--------|------------|-----------|
| 1 | Master + Quick Start + Cap 1-2 | Autonomía de navegación |
| 2 | Cap 3-4 + escenarios | Primera operación real supervisada |
| 3 | Cap 5-7 + shadowing | Calidad de datos y uso de IA |
| 4 | Cap 8 + examen | Certificación operativa |

---

## 7. Principios de adopción enterprise

1. **Un registro, una verdad** — Si no está en el CRM, no ocurrió.
2. **Contexto antes de acción** — Customer 360 antes de cada interacción importante.
3. **IA con responsabilidad** — Trust Studio no es opcional para roles de aprobación.
4. **Medir para mejorar** — KPIs del Capítulo 6 de tu guía de rol.
5. **Certificar antes de escalar** — Examen + casos prácticos obligatorios.

---

## 8. Entorno de práctica oficial

| Campo | Valor |
|-------|-------|
| URL | {QA_URL} |
| Tenant | {TENANT} |
| Contraseña práctica | `{PASSWORD}` |

Usuarios por rol: ver `README.md` de esta carpeta.

---

## Validación world-class

| Pregunta | Respuesta esperada |
|----------|-------------------|
| ¿Persona sin experiencia puede usar el sistema? | Sí, con guía de rol + onboarding |
| ¿Puede trabajar sin ayuda? | Sí, tras certificación semana 4 |
| ¿Puede generar resultados? | Sí, KPIs definidos por rol |
| ¿Aprende en <1 semana navegación? | Sí, Capítulos 1-2 |
| ¿Nivel Academy Salesforce/HubSpot? | Estructura equivalente por rol |
| ¿Listo enterprise? | Sí, con gobierno + auditoría |

---

*AutonomusCRM Enterprise Academy — Master Guide*
"""


def build_quick_start() -> str:
    lines = [
        "# ROLE QUICK START GUIDES — AutonomusCRM Academy",
        "",
        "> **Impresión:** 2 páginas por rol · orientación operativa",
        "",
        "---",
        "",
    ]
    for r in ROLES.values():
        impact_short = r["impact"][:120] + "..." if len(r["impact"]) > 120 else r["impact"]
        lines.extend([
            f"## {r['title']} — Quick Start",
            "",
            "| | |",
            "|---|---|",
            f"| **Login** | {QA_URL}/Account/Login |",
            f"| **Usuario** | `{r['email']}` |",
            f"| **Tu misión** | {impact_short} |",
            "",
            "### Día 1 — 5 acciones",
            "1. Login → Command Center — lee ingresos y alertas 5 min",
            f"2. Abre: {r['focus'][0]}, {r['focus'][1]}",
            f"3. Completa simulación Capítulo 2 de `{r['file']}.md`",
            "4. Crea 1 tarea propia en `/Tasks` con fecha mañana",
            "5. Lee cheat sheet de tu rol en `ROLE_CHEAT_SHEETS.md`",
            "",
            "### Rutas esenciales",
        ])
        for f in r["focus"]:
            lines.append(f"- {f}")
        lines.extend([
            "",
            "### KPI #1 esta semana",
            f"- {r['kpis'][0]}",
            "",
            "### Si te bloqueas",
            f"- Manager o Admin · Documentación: `Guides/{r['file']}.md`",
            "",
            "---",
            "",
        ])
    return "\n".join(lines)


def build_cheat_sheets() -> str:
    lines = ["# ROLE CHEAT SHEETS — AutonomusCRM Academy", ""]
    for r in ROLES.values():
        lines.extend([
            f"## {r['title']}",
            "",
            "| Atajo / Proceso | Acción |",
            "|-----------------|--------|",
            "| `Ctrl+K` | Búsqueda global |",
            "| Command | Pulso diario — `/` |",
            "| Customer 360 | Contexto pre-llamada |",
            "| Trust Studio | Aprobar IA — `/TrustInbox` |",
            "",
            f"**KPIs:** {' · '.join(r['kpis'])}",
            "",
            "**Alertas:** Deals en riesgo · Tickets SLA · Decisiones IA >24h",
            "",
            "**Errores top 5:** Sin registrar actividad · Duplicar cliente · Etapa incorrecta · Ignorar 360 · No cerrar tareas",
            "",
            "---",
            "",
        ])
    return "\n".join(lines)


def build_onboarding() -> str:
    lines = [
        "# ROLE ONBOARDING PROGRAM — AutonomusCRM Academy",
        "",
        "Programa estructurado Día 1 → Día 90 por rol.",
        "",
        "```mermaid",
        "gantt",
        "    title Onboarding 90 días",
        "    dateFormat YYYY-MM-DD",
        "    section Fundamentos",
        "    Día 1-7    :a1, 2025-01-01, 7d",
        "    section Operación",
        "    Día 8-30   :a2, after a1, 23d",
        "    section Maestría",
        "    Día 31-90  :a3, after a2, 60d",
        "```",
        "",
    ]
    for r in ROLES.values():
        lines.extend([
            f"## {r['title']}",
            "",
            "| Día | Objetivo | Actividades | Evidencia |",
            "|-----|----------|-------------|-----------|",
            "| **1** | Primer acceso | Master Guide + Quick Start + login | Screenshot Command Center |",
            "| **3** | Navegación | Cap 2 guía de rol + 5 pantallas | Lista pantallas visitadas |",
            "| **7** | Primera operación | 1 escenario Cap 4 con mentor | Registro en CRM |",
            f"| **15** | Autonomía parcial | Jornada tipo Cap 3 media | KPI {r['kpis'][0]} |",
            "| **30** | Certificación básica | Examen teórico 80% | Badge Básico |",
            "| **60** | Intermedio | 4 escenarios sin ayuda | Revisión manager |",
            "| **90** | Experto operativo | Examen avanzado + proyecto real | Certificación completa |",
            "",
        ])
    return "\n".join(lines)


def build_training() -> str:
    lines = ["# ROLE TRAINING PROGRAM — AutonomusCRM Academy", ""]
    for r in ROLES.values():
        lines.extend([
            f"## {r['title']}",
            "",
            "| Nivel | Duración | Objetivos | Resultado |",
            "|-------|----------|-----------|-----------|",
            "| **Básico** | 8 h (semana 1) | Login, navegación, conceptos CRM | Navega sin ayuda |",
            "| **Intermedio** | 16 h (semanas 2-3) | Procesos Cap 4, calidad de datos | Opera con supervisión ligera |",
            "| **Avanzado** | 24 h (mes 2) | IA, KPIs, escenarios complejos | Autonomía completa |",
            "| **Experto** | 40 h (mes 3) | Optimización, mentoring, playbooks | Referente del rol |",
            "",
        ])
    return "\n".join(lines)


EXAM_QUESTIONS = [
    "¿Cuál es el primer paso al iniciar tu jornada en AutonomusCRM?",
    "¿Qué módulo usarías para ver historial unificado de un cliente?",
    "¿Dónde apruebas o rechazas decisiones de IA?",
    "Menciona 3 KPIs de tu rol.",
    "¿Qué hacer si un deal lleva 14 días sin actividad?",
    "¿Cuál es la diferencia entre Lead y Customer?",
    "¿Por qué documentar la razón al perder un deal?",
    "¿Qué es Trust Studio en lenguaje de negocio?",
    "¿Cuándo escalar a tu manager?",
    "¿Qué información necesitas antes de una llamada a cliente enterprise?",
]


def build_certification_exams() -> str:
    lines = [
        "# ROLE CERTIFICATION EXAMS — AutonomusCRM Academy",
        "",
        "> Evaluación por competencias — teórica + práctica + escenarios",
        "",
        "## Estructura global",
        "",
        "| Parte | Preguntas / Casos | Tiempo | Aprobación |",
        "|-------|-------------------|--------|------------|",
        "| A — Teoría CRM | 20 | 30 min | 80% |",
        "| B — AutonomusCRM | 20 | 30 min | 80% |",
        "| C — Rol específico | 15 | 25 min | 80% |",
        "| D — Casos prácticos | 4 escenarios | 60 min | 3/4 |",
        "| E — Competencias | Observación | — | Satisfactorio |",
        "",
    ]
    for r in ROLES.values():
        lines.extend([
            "---",
            "",
            f"## Examen — {r['title']}",
            "",
            "### Parte C — Preguntas de rol (muestra)",
            "",
        ])
        for qi, q in enumerate(EXAM_QUESTIONS, 1):
            lines.append(f"{qi}. **{q}**")
            lines.append("   - [ ] Respuesta modelo en guía Capítulo correspondiente")
            lines.append("")
        lines.extend([
            f"### Parte D — Caso práctico {r['title']}",
            "",
            "**Escenario:** Cliente existente solicita ampliación de licencias. Describe paso a paso qué pantallas usas, qué registras y qué KPI impactas.",
            "",
            "**Rúbrica:** Contexto 360 (25%) · Registro completo (25%) · Próximo paso (25%) · Comunicación handoff (25%)",
            "",
        ])
    return "\n".join(lines)


def build_business_playbooks() -> str:
    return """# BUSINESS PROCESS PLAYBOOKS — AutonomusCRM Academy

Playbooks operativos de negocio — no documentación técnica.

---

## 1. Ventas — Lead a Cierre

```mermaid
flowchart TD
    A[Lead entra] --> B[Contacto <24h]
    B --> C{Califica?}
    C -->|No| N[Nurture]
    C -->|Sí| D[Crear Deal]
    D --> E[Discovery]
    E --> F[Propuesta]
    F --> G{Decisión}
    G -->|Won| H[Handoff CS]
    G -->|Lost| I[Razón documentada]
```

| Etapa | Criterio de salida | Pantalla |
|-------|-------------------|----------|
| Nuevo | Datos completos | Leads |
| Contactado | Nota de llamada | Leads Details |
| Calificado | BANT completo | Qualify |
| Oportunidad | Deal creado | Deals |
| Cierre | Won + contrato | Deals Details |

---

## 2. Atención al cliente

| Paso | Acción | SLA sugerido |
|------|--------|--------------|
| 1 | Crear/recibir ticket | Customer Success |
| 2 | Clasificar severidad | P1 <15 min respuesta |
| 3 | Ejecutar playbook | Según tipo |
| 4 | Resolver y confirmar | Cierre con cliente |
| 5 | Registrar en 360 | Nota permanente |

---

## 3. Renovación

1. Alerta 90 días antes — Customer Success OS
2. QBR con métricas de valor — Customer 360
3. Propuesta renovación — Deal o nota
4. Cierre o plan de recuperación

---

## 4. Cobranza (coordinación con finanzas)

| Señal | Acción CRM |
|-------|------------|
| Pago atrasado | Tarea + nota en Customer 360 |
| Sin respuesta | Escalamiento Manager |
| Disputa | Ticket + auditoría |

---

## 5. Recuperación de churn

```mermaid
flowchart LR
    R[Riesgo detectado] --> C[Llamada sponsor]
    C --> P[Plan valor]
    P --> O{Recuperado?}
    O -->|Sí| RN[Renovación]
    O -->|No| EXIT[Entrevista salida]
```

---

## 6. Expansión (Upsell / Cross-sell)

- IA o CS identifica oportunidad → Trust Studio si aplica
- Sales crea deal expansión
- Manager valida margen

---

## 7. Escalamiento

| Nivel | Cuándo | A quién |
|-------|--------|---------|
| L1 | SLA incumplido | Support lead |
| L2 | Cliente VIP | Manager |
| L3 | Riesgo revenue | CRO / Executive OS |

---

## 8. Crisis operativa

1. Command Center — alcance
2. War room — Tasks + roles claros
3. Comunicación — plantilla ejecutiva
4. Post-mortem — Audit + lecciones

---

## 9. Clientes VIP

- Etiqueta en Customer 360
- SLA reducido
- Manager en copia de tickets P1/P2
- QBR trimestral obligatorio

---

## 10. Clientes en riesgo

| Señal | Playbook |
|-------|----------|
| Uso bajo | Adopción |
| NPS <7 | Recuperación |
| Ticket repetido | Escalamiento técnico |
| Sponsor cambió | Re-mapeo cuenta |

---

*AutonomusCRM Enterprise Academy — Business Process Playbooks*
"""


def build_executive_playbook() -> str:
    return """# EXECUTIVE PLAYBOOK — AutonomusCRM Academy

Guía para liderazgo — decisiones con datos, no con intuición.

---

## CEO — Visión y accountability

**Pregunta semanal:** ¿Protegimos y generamos ingresos esta semana?

| Uso AutonomusCRM | Frecuencia |
|------------------|------------|
| Executive OS + export board | Semanal (junta) |
| Command Center — revenue protected | Diario (5 min) |
| Trust Studio — decisiones críticas IA | Según alerta |

```mermaid
flowchart TD
    CEO[CEO] --> E[Executive OS]
    E --> D{Decisiones}
    D --> I[Inversión]
    D --> P[Prioridades equipo]
    D --> R[Riesgo concentrado]
```

---

## COO — Operación y eficiencia

- Tasks + workflows sin cuellos de botella
- Auditoría mensual de calidad de datos
- SLA Customer Success vs capacidad

---

## CRO — Ingresos

| KPI | Fuente |
|-----|--------|
| Pipeline coverage | Revenue OS |
| Win rate | Deals |
| Forecast accuracy | Revenue OS |
| Churn | Customer Success |

**Ritual:** Pipeline review lunes — Manager + Sales en `/Deals`

---

## Director Comercial

- Coaching basado en actividad registrada
- Política de descuentos en Policies
- Alineación marketing → Leads por fuente

---

## Gerente de Operaciones

- Usuarios y permisos con Admin
- Integraciones estables
- Eventos fallidos = cero tolerancia >48h

---

## Customer Success Manager

- Health score y renovaciones 90/60/30 días
- Playbooks actualizados trimestralmente
- NPS por cohorte

---

*AutonomusCRM Enterprise Academy — Executive Playbook*
"""


def build_university() -> str:
    return f"""# AUTONOMUSCRM UNIVERSITY — Programa Oficial de Capacitación

> La academia corporativa oficial de AutonomusCRM

---

## Misión

Formar operadores de clase mundial que generen ingresos, satisfacción y crecimiento usando AutonomusCRM como sistema nervioso del negocio.

---

## Rutas de aprendizaje

```mermaid
flowchart TB
    subgraph Fundamentos
        MG[Master Guide]
        BP[Business Playbooks]
    end
    subgraph Por Rol
        G[Guía 8 capítulos]
        QS[Quick Start]
        CS[Cheat Sheet]
    end
    subgraph Certificación
        ONB[Onboarding 90d]
        TRN[Training 4 niveles]
        EX[Examen]
        CERT[Badge]
    end
    MG --> G
    G --> ONB
    ONB --> EX
    EX --> CERT
    CERT --> TRN
```

---

## Catálogo de cursos

| Código | Nombre | Audiencia | Duración |
|--------|--------|-----------|----------|
| ACAD-101 | Fundamentos CRM + RevOps | Todos | 4 h |
| ACAD-201 | AutonomusCRM Operación | Todos | 8 h |
| ACAD-301 | Rol — Básico | Por rol | 8 h |
| ACAD-302 | Rol — Intermedio | Por rol | 16 h |
| ACAD-303 | Rol — Avanzado | Por rol | 24 h |
| ACAD-401 | IA Responsable | Manager+ | 4 h |
| ACAD-501 | Executive Intelligence | Liderazgo | 2 h |
| ACAD-601 | Certificación Operativa | Por rol | Examen |

---

## Ruta de certificación

1. Completar Master Guide
2. Quick Start + Guía de rol (Cap 1-4)
3. Onboarding Día 1-15
4. Examen teórico 80%
5. 4 casos prácticos
6. Sign-off manager
7. Badge **AutonomusCRM Certified — [Rol]**

---

## Ruta de crecimiento profesional

| Badge | Requisito | Siguiente paso |
|-------|-----------|----------------|
| Certified Basic | Examen + 15 días | Operación supervisada |
| Certified Pro | 30 días + KPIs | Mentor de nuevos |
| Certified Expert | 90 días + proyecto | Líder de práctica |
| Certified Master | Train-the-trainer | Academia interna |

---

## Documentos del programa

| # | Documento |
|---|-----------|
| 1 | AUTONOMUSCRM_ACADEMY_MASTER_GUIDE.md |
| 2 | Guides/*_GUIDE.md (×6) |
| 3 | ROLE_QUICK_START_GUIDES.md |
| 4 | ROLE_CHEAT_SHEETS.md |
| 5 | ROLE_ONBOARDING_PROGRAM.md |
| 6 | ROLE_TRAINING_PROGRAM.md |
| 7 | ROLE_CERTIFICATION_EXAMS.md |
| 8 | BUSINESS_PROCESS_PLAYBOOKS.md |
| 9 | EXECUTIVE_PLAYBOOK.md |
| 10 | AUTONOMUSCRM_UNIVERSITY.md |

---

## Entorno oficial

URL: {QA_URL} | Tenant: {TENANT}

---

*AutonomusCRM University — World-class CRM adoption*
"""


def build_readme() -> str:
    rows = "\n".join(
        f"| {r['title']} | {r['email']} | [Guides/{r['file']}.md](Guides/{r['file']}.md) |"
        for r in ROLES.values()
    )
    return f"""# AutonomusCRM Enterprise Academy

Academia corporativa completa — capacitación operativa world-class.

**No es documentación técnica.** Es programa de adopción empresarial.

## Inicio rápido

1. Leer [AUTONOMUSCRM_ACADEMY_MASTER_GUIDE.md](AUTONOMUSCRM_ACADEMY_MASTER_GUIDE.md)
2. Imprimir tu rol en [ROLE_QUICK_START_GUIDES.md](ROLE_QUICK_START_GUIDES.md)
3. Estudiar tu [Guía de rol](Guides/)
4. Seguir [ROLE_ONBOARDING_PROGRAM.md](ROLE_ONBOARDING_PROGRAM.md)
5. Certificarte con [ROLE_CERTIFICATION_EXAMS.md](ROLE_CERTIFICATION_EXAMS.md)

## Programa completo

Ver [AUTONOMUSCRM_UNIVERSITY.md](AUTONOMUSCRM_UNIVERSITY.md)

## Entorno de práctica

| Rol | Email | Guía |
|-----|-------|------|
{rows}

Password: `{PASSWORD}` | URL: {QA_URL}

## QA técnico (separado)

Pruebas automatizadas y casos QA: [../QA/README.md](../QA/README.md)
"""


def main() -> int:
    print("Generating AutonomusCRM Enterprise Academy...")
    GUIDES.mkdir(parents=True, exist_ok=True)

    write_file(OUT / "AUTONOMUSCRM_ACADEMY_MASTER_GUIDE.md", build_master_guide())

    guide_paths: list[Path] = []
    for role_key, r in ROLES.items():
        path = GUIDES / f"{r['file']}.md"
        write_file(path, build_role_guide(role_key, r))
        guide_paths.append(path)

    write_file(OUT / "ROLE_QUICK_START_GUIDES.md", build_quick_start())
    write_file(OUT / "ROLE_CHEAT_SHEETS.md", build_cheat_sheets())
    write_file(OUT / "ROLE_ONBOARDING_PROGRAM.md", build_onboarding())
    write_file(OUT / "ROLE_TRAINING_PROGRAM.md", build_training())
    write_file(OUT / "ROLE_CERTIFICATION_EXAMS.md", build_certification_exams())
    write_file(OUT / "BUSINESS_PROCESS_PLAYBOOKS.md", build_business_playbooks())
    write_file(OUT / "EXECUTIVE_PLAYBOOK.md", build_executive_playbook())
    write_file(OUT / "AUTONOMUSCRM_UNIVERSITY.md", build_university())
    write_file(OUT / "README.md", build_readme())

    print("\nLine counts — role guides:")
    for p in guide_paths:
        count = len(p.read_text(encoding="utf-8").splitlines())
        print(f"  {p.name}: {count} lines")

    print(f"\nDone. Academy at {OUT.relative_to(ROOT)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
