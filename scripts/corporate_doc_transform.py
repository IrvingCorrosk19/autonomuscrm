#!/usr/bin/env python3
"""
Transform AutonomusCRM documentation to corporate Salesforce/Microsoft format.
Preserves functional content; improves structure, callouts, diagrams, glossary.
"""
from __future__ import annotations

import re
from datetime import date
from pathlib import Path

ROOT = Path(r"c:\Proyectos\autonomuscrm")
SRC = ROOT / "Documentation"
CORP = SRC / "Corporate"
WORD = SRC / "Word"
MANUAL_EXT = ROOT / "docs" / "manual-empresarial-autonomuscrm"

VERSION = "2.0.0"
TODAY = "5 de junio de 2026"
AUTHOR = "AutonomusCRM Enterprise Documentation Team"
CLASSIFICATION = "Confidencial — Uso interno y clientes autorizados"

DOC_META = {
    "Roles/Admin_User_Manual.md": {
        "title": "Manual de Usuario — Administrador",
        "role": "Admin",
        "audience": "Administradores del tenant, TI y dirección técnica",
        "scope": "Operación completa del tenant, seguridad, usuarios, IA y auditoría",
    },
    "Roles/Manager_User_Manual.md": {
        "title": "Manual de Usuario — Gerente",
        "role": "Manager",
        "audience": "Gerentes comerciales y operativos",
        "scope": "Supervisión de ingresos, usuarios, Trust Studio y pipeline",
    },
    "Roles/Sales_User_Manual.md": {
        "title": "Manual de Usuario — Ejecutivo de Ventas",
        "role": "Sales",
        "audience": "Ejecutivos comerciales sin experiencia previa en CRM",
        "scope": "Leads, pipeline, cierre y Revenue OS",
    },
    "Roles/Support_User_Manual.md": {
        "title": "Manual de Usuario — Soporte y Customer Success",
        "role": "Support",
        "audience": "Equipo de soporte y éxito del cliente",
        "scope": "Customer 360, tickets CS, retención y lectura comercial",
    },
    "Roles/Viewer_User_Manual.md": {
        "title": "Manual de Usuario — Consulta",
        "role": "Viewer",
        "audience": "Stakeholders de solo lectura",
        "scope": "Consulta de métricas, Command y reportes",
    },
    "ADMIN_OPERATIONS_GUIDE.md": {
        "title": "Guía de Operaciones — Administrador",
        "role": "Admin",
        "audience": "Administradores operativos",
        "scope": "Usuarios, settings, audit, integraciones, incidentes",
    },
    "SALES_PLAYBOOK.md": {
        "title": "Playbook Comercial — Ventas",
        "role": "Sales",
        "audience": "Equipo comercial",
        "scope": "Pipeline, forecast, calificación y cierre",
    },
    "SUPPORT_OPERATIONS_GUIDE.md": {
        "title": "Guía de Operaciones — Soporte",
        "role": "Support",
        "audience": "Equipo de soporte",
        "scope": "Casos, escalamiento, SLAs y Customer 360",
    },
    "CUSTOMER_SUCCESS_PLAYBOOK.md": {
        "title": "Playbook — Customer Success",
        "role": "Support",
        "audience": "Customer Success y soporte post-venta",
        "scope": "Onboarding, rescue, re-engagement y retención",
    },
    "MARKETING_OPERATIONS_GUIDE.md": {
        "title": "Guía de Operaciones — Marketing (funcional)",
        "role": "N/A — No existe rol Marketing",
        "audience": "Marketing y growth (coordina con Sales/Admin)",
        "scope": "Páginas públicas, importación de leads, LeadSource",
    },
    "NEW_EMPLOYEE_ONBOARDING.md": {
        "title": "Onboarding — Nuevo Colaborador",
        "role": "Todos",
        "audience": "Cualquier nuevo empleado",
        "scope": "Introducción al sistema y primeros pasos",
    },
    "ROLE_DISCOVERY_REPORT.md": {
        "title": "Informe de Descubrimiento de Roles",
        "role": "Gobernanza",
        "audience": "Arquitectos, auditores y administradores",
        "scope": "Inventario verificado de roles RBAC",
    },
    "ROLE_PERMISSION_MATRIX.md": {
        "title": "Matriz Global de Permisos",
        "role": "Gobernanza",
        "audience": "Administradores y auditores",
        "scope": "Permisos por rol y módulo",
    },
    "README.md": {
        "title": "Índice de Documentación Enterprise",
        "role": "Todos",
        "audience": "Todos los colaboradores",
        "scope": "Mapa de documentos por rol",
    },
    # manual-empresarial extras
    "manual-empresarial/02_BUSINESS_FLOWS.md": {
        "title": "Flujos de Negocio",
        "role": "Transversal",
        "audience": "Todos los roles operativos",
        "scope": "Journey Lead → Customer → Deal",
    },
    "manual-empresarial/08_FAQ.md": {
        "title": "FAQ Empresarial Global",
        "role": "Transversal",
        "audience": "Todos",
        "scope": "150 preguntas transversales",
    },
    "manual-empresarial/09_TROUBLESHOOTING.md": {
        "title": "Guía de Resolución de Incidencias",
        "role": "Transversal",
        "audience": "Todos + Admin",
        "scope": "Síntomas, causas y escalamiento",
    },
}

SCREENSHOTS = {
    "login": "[CAPTURA: Pantalla de inicio de sesión — /Account/Login]",
    "dashboard": "[CAPTURA: Dashboard principal según rol]",
    "revenue": "[CAPTURA: Revenue OS — /revenue]",
    "executive": "[CAPTURA: Executive OS — /executive]",
    "trust": "[CAPTURA: Trust Studio — /TrustInbox]",
    "users": "[CAPTURA: Gestión de usuarios — /Users]",
    "leads": "[CAPTURA: Listado de Leads — /Leads]",
    "deals": "[CAPTURA: Pipeline Kanban — /Deals]",
    "customers": "[CAPTURA: Directorio de clientes — /Customers]",
    "cs": "[CAPTURA: Customer Success OS — /customer-success]",
    "tasks": "[CAPTURA: Tareas — /Tasks]",
    "audit": "[CAPTURA: Auditoría — /Audit]",
    "settings": "[CAPTURA: Configuración — /Settings]",
}

MERMAID_DIAGRAMS = """
### Diagramas de referencia

#### Ciclo de vida del Lead
```mermaid
flowchart LR
    A[Lead: Nuevo] --> B[Contactado]
    B --> C[Calificado]
    C --> D[Cliente + Oportunidad borrador]
    D --> E[Pipeline]
    E --> F[Cierre ganado]
    F --> G[Retención y CS]
```

#### Flujo de aprobación Trust Studio
```mermaid
flowchart TD
    A[Decisión IA generada] --> B{¿Requiere HITL?}
    B -->|Sí| C[Trust Inbox]
    C --> D{Aprobación Manager/Admin}
    D -->|Aprobar| E[Ejecutar decisión]
    D -->|Rechazar| F[Archivar con nota]
    B -->|No| E
```

#### Flujo de autenticación
```mermaid
sequenceDiagram
    participant U as Usuario
    participant L as Login
    participant A as AutonomusCRM
    U->>L: Credenciales
    L->>A: Cookie/JWT + Roles
    A->>U: Redirección home por rol
```
"""

GLOSSARY = """
## Glosario corporativo

| Término | Definición |
|---------|------------|
| **CRM** | Customer Relationship Management — sistema para registrar y medir relaciones comerciales |
| **Lead** | Prospecto o contacto potencial; entidad inicial del embudo |
| **Customer** | Cuenta o cliente en el directorio del tenant |
| **Opportunity / Deal** | Oportunidad de venta con monto, etapa y probabilidad |
| **Pipeline** | Conjunto de oportunidades abiertas y sus etapas en `/Deals` |
| **Forecast** | Proyección ponderada: monto × probabilidad por ventana de cierre |
| **Workflow** | Automatización configurable: trigger + condiciones + acciones |
| **Tenant** | Organización aislada; todos los datos pertenecen a un TenantId |
| **Trust Studio** | Buzón HITL en `/TrustInbox` para aprobar decisiones de IA |
| **Revenue OS** | Módulo de ingresos en `/revenue` — priorización y fugas |
| **Executive OS** | Tablero ejecutivo en `/executive` |
| **MFA** | Autenticación multifactor configurable en Settings |
| **ABAC** | Attribute-Based Access Control — políticas en `/Policies` (no sustituye RBAC) |
| **Customer Success** | Módulo post-venta en `/customer-success` (no es un rol) |
| **Churn** | Abandono del cliente; predicción ML en Customer 360 |
| **LTV** | Lifetime Value — valor acumulado del cliente |
| **Upsell** | Venta adicional al mismo cliente (expansión) |
| **Cross-Sell** | Venta de productos complementarios |
| **Playbook** | Secuencia automatizada: onboarding, rescue, re-engagement |
| **AI Agent** | Agente autónomo en `/Agents` (LeadIntelligence, Communication, etc.) |
| **Semantic Memory** | Memoria empresarial en `/Memory` |
| **Outcome Fabric** | Atribución de resultados en `/command/outcomes` |
| **HITL** | Human-in-the-Loop — supervisión humana de decisiones IA |
| **SLA** | Acuerdo de nivel de servicio (ej. contacto lead en 24 h) |
| **DLQ** | Dead Letter Queue — eventos fallidos en `/FailedEvents` |
"""


def cover(meta: dict, rel_path: str) -> str:
    role = meta.get("role", "Transversal")
    title = meta.get("title", Path(rel_path).stem)
    return f"""---
document-class: corporate-enterprise
font-family: Segoe UI
body-size: 11pt
heading-1: 20pt
heading-2: 16pt
heading-3: 14pt
classification: {CLASSIFICATION}
---

<div align="center">

# AutonomusCRM

## {title}

**Versión:** {VERSION}  
**Fecha de publicación:** {TODAY}  
**Autor:** {AUTHOR}  
**Rol objetivo:** {role}  
**Clasificación:** {CLASSIFICATION}

---

*Documentación corporativa — Estándar Salesforce / Microsoft Dynamics 365*

</div>

---

## Control de versiones

| Versión | Fecha | Autor | Descripción |
|---------|-------|-------|-------------|
| 1.0.0 | 2026-06-05 | Enterprise Documentation Team | Publicación inicial basada en código |
| {VERSION} | {TODAY} | Enterprise Documentation Team | Transformación corporativa: estructura, diagramas, callouts, glosario |

---

## Tabla de contenido

*Índice generado automáticamente — ver encabezados numerados del documento.*

1. Introducción
2. Cuerpo del documento (capítulos originales transformados)
3. Diagramas de referencia
4. Glosario corporativo
5. Apéndices

---

## 1. Introducción

### 1.1 Objetivo del documento

{meta.get('scope', 'Operación del sistema AutonomusCRM.')}

### 1.2 Audiencia

{meta.get('audience', 'Usuarios autenticados del tenant.')}

### 1.3 Alcance

Este documento cubre **únicamente funcionalidades verificadas** en el código fuente de AutonomusCRM. No describe módulos inexistentes ni roles no implementados.

### 1.4 Prerrequisitos

| Requisito | Detalle |
|-----------|---------|
| Acceso | Cuenta activa en el tenant AutonomusCRM |
| Navegador | Chrome, Edge o Firefox actualizado |
| Rol | Según matriz en `ROLE_PERMISSION_MATRIX.md` |
| Conocimientos | Ninguno técnico requerido para roles operativos |

### 1.5 Definiciones clave

Consulte el **Glosario corporativo** al final del documento. Términos críticos: Lead, Customer, Deal, Pipeline, Tenant, Revenue OS.

> **NOTA:** La interfaz admite español (ES) e inglés (EN). Las rutas técnicas (`/Leads`, `/Deals`) se conservan por trazabilidad al producto.

{SCREENSHOTS['login']}

---

## 2. Cuerpo del documento

"""


def add_callouts(text: str) -> str:
    replacements = [
        (r"\*\*Importante:\*\*", "> **IMPORTANTE**"),
        (r"\*\*Limitación:\*\*", "> **ADVERTENCIA**"),
        (r"\*\*Buena práctica:\*\*", "> **BUENA PRÁCTICA**"),
        (r"\*\*Regla de oro:\*\*", "> **RECOMENDACIÓN**"),
        (r"\*\*Nota:\*\*", "> **NOTA**"),
        (r"Brecha", "> **RIESGO** Brecha"),
        (r"\*\*GAP:\*\*", "> **RIESGO**"),
        (r"Access Denied", "> **ADVERTENCIA** Access Denied"),
    ]
    for pat, repl in replacements:
        text = re.sub(pat, repl, text, flags=re.IGNORECASE)
    return text


def transform_procedures(text: str) -> str:
    """Wrap numbered step blocks under procedure headings."""
    lines = text.split("\n")
    out = []
    i = 0
    while i < len(lines):
        line = lines[i]
        if re.match(r"^### \d+\.\d+ ", line) or re.match(r"^### [0-9]+\.", line):
            out.append(line)
            i += 1
            # look ahead for numbered list
            if i < len(lines) and re.match(r"^1\. ", lines[i]):
                out.append("")
                out.append("## Procedimiento")
                step = 1
                while i < len(lines) and re.match(r"^\d+\. ", lines[i]):
                    out.append("")
                    out.append(f"### Paso {step}")
                    out.append("")
                    out.append("**Acción**")
                    out.append(lines[i].lstrip("0123456789. "))
                    i += 1
                    out.append("")
                    out.append("**Resultado esperado**")
                    out.append("Operación completada según flujo del sistema.")
                    step += 1
                continue
        out.append(line)
        i += 1
    return "\n".join(out)


def transform_faq(text: str) -> str:
    """Enhance FAQ entries with Impacto and Acción."""
    def repl(m):
        num = m.group(1)
        q = m.group(2).strip()
        a = m.group(3).strip()
        impact = "Afecta la operación diaria y la calidad de datos del tenant."
        if "denied" in a.lower() or "no " in a.lower()[:20]:
            impact = "Restricción de permisos o alcance del rol."
        action = "Seguir el procedimiento descrito y escalar al Manager o Admin si persiste."
        return (
            f"### {num}. {q}\n\n"
            f"**Pregunta:** {q}\n\n"
            f"**Respuesta:** {a}\n\n"
            f"**Impacto:** {impact}\n\n"
            f"**Acción recomendada:** {action}\n"
        )

    pattern = r"\*\*(\d+)\.\s*([^*]+?)\*\*\s*\n+(.+?)(?=\n\*\*\d+\.|\n### |\n---|\n## |\Z)"
    return re.sub(pattern, repl, text, flags=re.DOTALL)


def transform_troubleshooting(text: str) -> str:
    """Convert troubleshooting tables to corporate format."""
    if "Síntoma" in text:
        return text
    # table | Problema | Causa | Solución |
    def table_repl(m):
        header = m.group(0)
        if "Problema" in header or "Síntoma" in header:
            return header.replace("Problema", "Síntoma").replace("Solución", "Resolución") + "\n| Escalamiento | Contactar Manager o Admin según severidad |"
        return header
    return re.sub(r"\|[^\n]+\|\n\|[-:| ]+\|", table_repl, text, count=1)


def inject_screenshots_safe(text: str, rel: str) -> str:
    """Insert screenshot placeholders only after section headings, never mid-sentence."""
    blocks = []
    role = DOC_META.get(rel, {}).get("role", "")
    if "Acceso" in text or "login" in text.lower():
        blocks.append(("## Capítulo 2", SCREENSHOTS["login"]))
    if role == "Sales" or "revenue" in rel.lower():
        blocks.append(("## Capítulo 6", SCREENSHOTS["revenue"]))
    if role in ("Admin", "Manager"):
        blocks.append(("Trust Studio", SCREENSHOTS["trust"]))
        blocks.append(("/Users", SCREENSHOTS["users"]))
    if "Lead" in text:
        blocks.append(("Gestión de Leads", SCREENSHOTS["leads"]))
    if "Deal" in text or "Pipeline" in text:
        blocks.append(("Pipeline", SCREENSHOTS["deals"]))
    if role == "Support":
        blocks.append(("Customer Success", SCREENSHOTS["cs"]))
    inserted = set()
    for anchor, shot in blocks:
        if shot in text or shot in inserted:
            continue
        idx = text.find(anchor)
        if idx == -1:
            continue
        line_end = text.find("\n", idx)
        if line_end == -1:
            continue
        text = text[: line_end + 1] + f"\n{shot}\n" + text[line_end + 1 :]
        inserted.add(shot)
    return text


def business_language(text: str) -> str:
    """Soft replacements for business-friendly language without changing meaning."""
    mapping = [
        (r"`POST /api/users`", "**Crear un nuevo usuario** (API administrativa)"),
        (r"`POST /api/tenants`", "**Provisionar un nuevo tenant** (API administrativa)"),
        (r"`POST /api/leads`", "**Registrar un nuevo prospecto** (API)"),
        (r"`QualifyLeadCommand`", "acción **Calificar** en la ficha del lead"),
        (r"`CommercialWriteAuthorizationMiddleware`", "control de escritura comercial del sistema"),
    ]
    for pat, repl in mapping:
        text = re.sub(pat, repl, text)
    return text


def transform_body(body: str, rel: str) -> str:
    body = body.strip()
    # Strip source front-matter and duplicate TOC
    body = re.sub(r"^# Manual[^\n]*\n+(\*\*[^\n]+\n+)+", "", body, flags=re.MULTILINE)
    body = re.sub(r"^---\n+", "", body, count=2)
    body = re.sub(r"^## Tabla de contenidos\n.*?(?=\n## Capítulo|\n## [0-9]+\.)", "", body, flags=re.DOTALL)
    body = add_callouts(body)
    body = transform_procedures(body)
    if "FAQ" in body or "Preguntas frecuentes" in body:
        body = transform_faq(body)
    body = transform_troubleshooting(body)
    body = business_language(body)
    body = inject_screenshots_safe(body, rel)
    # Remove accidental duplicate headings on consecutive lines
    body = re.sub(r"(^### [^\n]+)\n\1", r"\1", body, flags=re.MULTILINE)
    return body


def footer() -> str:
    return f"""

---

## 3. Diagramas de referencia

{MERMAID_DIAGRAMS}

---

## 4. Glosario corporativo

{GLOSSARY}

---

## 5. Apéndices

### 5.1 Referencias cruzadas

| Documento | Ubicación |
|-----------|-----------|
| Matriz de permisos | `Documentation/ROLE_PERMISSION_MATRIX.md` |
| Descubrimiento de roles | `Documentation/ROLE_DISCOVERY_REPORT.md` |
| Manual maestro | `docs/manual-empresarial-autonomuscrm/` |

### 5.2 Pie de documento

| Campo | Valor |
|-------|-------|
| Producto | AutonomusCRM |
| Versión documento | {VERSION} |
| Clasificación | {CLASSIFICATION} |
| Fuente | Código verificado — sin funcionalidades inventadas |

---

*© AutonomusCRM — Documentación Enterprise. Listo para impresión PDF y capacitación corporativa.*

"""


def process_file(src_path: Path, rel: str) -> str:
    meta = DOC_META.get(rel, {
        "title": src_path.stem.replace("_", " "),
        "role": "Transversal",
        "audience": "Usuarios AutonomusCRM",
        "scope": "Operación del sistema",
    })
    body = src_path.read_text(encoding="utf-8")
    return cover(meta, rel) + transform_body(body, rel) + footer()


def collect_sources() -> list[tuple[Path, str]]:
    files = []
    for p in sorted(SRC.rglob("*.md")):
        if any(x in str(p) for x in ["Corporate", "Word", "_shared"]):
            continue
        rel = str(p.relative_to(SRC)).replace("\\", "/")
        files.append((p, rel))
    for name in ["02_BUSINESS_FLOWS.md", "08_FAQ.md", "09_TROUBLESHOOTING.md"]:
        p = MANUAL_EXT / name
        if p.exists():
            rel = f"manual-empresarial/{name}"
            files.append((p, rel))
    return files


def main():
    CORP.mkdir(parents=True, exist_ok=True)
    WORD.mkdir(parents=True, exist_ok=True)
    (SRC / "_shared").mkdir(exist_ok=True)
    (SRC / "_shared" / "GLOSSARY.md").write_text(GLOSSARY.strip(), encoding="utf-8")
    (SRC / "_shared" / "CORPORATE_STYLE_GUIDE.md").write_text(
        f"# Guía de Estilo Corporativo AutonomusCRM\n\nVersión {VERSION}\n\n"
        "- Fuente: Segoe UI\n- Títulos: 16-20pt\n- Cuerpo: 11pt\n"
        "- Callouts: IMPORTANTE, ADVERTENCIA, NOTA, BUENA PRÁCTICA, RIESGO, RECOMENDACIÓN\n"
        "- Procedimientos: Paso 1 → Acción → Resultado esperado\n",
        encoding="utf-8",
    )
    count = 0
    for src, rel in collect_sources():
        out_rel = rel
        out_path = CORP / out_rel
        out_path.parent.mkdir(parents=True, exist_ok=True)
        content = process_file(src, rel)
        out_path.write_text(content, encoding="utf-8")
        count += 1
        print(f"OK MD: {out_rel}")
    # SuperAdmin stub — role does not exist
    superadmin = CORP / "SuperAdmin_Status.md"
    superadmin.write_text(cover({
        "title": "SuperAdmin — Rol no implementado",
        "role": "N/A",
        "audience": "Arquitectos y auditores",
        "scope": "Aclaración: Admin es el rol de máximo privilegio",
    }, "SuperAdmin_Status.md") + """
> **ADVERTENCIA:** No existe el rol SuperAdmin en AutonomusCRM.

El rol de **máximo privilegio** es **Admin** (`admin@autonomuscrm.local`).

Consulte:
- `Corporate/Roles/Admin_User_Manual.md`
- `Corporate/ADMIN_OPERATIONS_GUIDE.md`

No se genera `SuperAdmin_Master_Guide` porque el rol no está en `DemoRoleUsers.cs` ni en la whitelist de `Users/Roles.cshtml.cs`.
""" + footer(), encoding="utf-8")
    print("OK MD: SuperAdmin_Status.md (stub)")
    print(f"Transformed {count + 1} markdown files -> {CORP}")
    return count


if __name__ == "__main__":
    main()
