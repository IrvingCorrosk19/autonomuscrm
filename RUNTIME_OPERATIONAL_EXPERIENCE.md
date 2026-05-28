# RUNTIME_OPERATIONAL_EXPERIENCE

## Objetivo
Optimizar la experiencia runtime diaria: cambio de contexto, multitarea y continuidad operacional.

## Implementado
1. **Barra runtime sticky** — accesos a Dashboard, Leads, Pipeline, Clientes, Workflows, Agents, Auditoría desde cualquier módulo autenticado.
2. **Etiqueta de contexto** — derivada del módulo actual en `_Layout.cshtml` (`ViewData["RuntimeContextLabel"]`).
3. **Continuar** — enlace “Continuar: {módulo}” cuando el usuario cambia de módulo (ruta exacta anterior en `sessionStorage`).
4. **Page enter** — affordance visual breve al cargar contenido (no bloqueante).

## Velocidad percibida
- Sticky bars reducen scroll para acciones repetidas.
- Sin peticiones extra al servidor para continuidad (client-side only).

## Pendiente incremental (no bloqueante)
- Extender sticky ops bar a Leads/Deals list pages cuando migren completamente a `crm-*`.
- Atajos de teclado globales opcionales (evaluar adopción real antes de activar).

## Métricas operativas sugeridas
- Tiempo medio entre módulos (telemetría futura).
- Uso del enlace “Continuar” vs sidebar.
