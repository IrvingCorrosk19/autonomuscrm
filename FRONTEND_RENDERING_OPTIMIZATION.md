# FRONTEND_RENDERING_OPTIMIZATION

- Se reduce complejidad de render en `Agents` al eliminar generación de HTML string masiva.
- Menor costo de parseo y menor riesgo de reflows impredecibles en apertura de modal.
- Reutilización de estructura DOM estable para formularios por agente.
- JS operativo simplificado para binding y serialización de configuración.
