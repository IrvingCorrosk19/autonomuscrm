# CROSS_MODULE_EXPERIENCE

## Módulos enlazados
Dashboard · Leads · Customers · Deals · Workflows · Agents · Audit · Settings · Users · Policies · Support

## Continuidad
- **Global:** `_CrmRuntimeBar` en `_Layout.cshtml` (solo autenticado).
- **Contexto:** label coherente con módulo activo.
- **Historial ligero:** `crmUi.moduleFromPath()` + `crm_runtime_last`.
- **Sidebar:** `ModuleActive(path)` marca activo en rutas hijas (`/Leads/Create`, `/Deals/Details`).

## Consistencia visual
Misma familia `crm-runtime-*`, tokens de borde/fondo alineados con ops bar y onboarding existentes.

## Flujo objetivo
El usuario percibe una sola aplicación SaaS, no páginas aisladas.

## Gap conocido
Páginas Leads/Customers/Deals con estilos legacy inline: migración gradual sin romper operación (Fase 11 no rehace frontend).
