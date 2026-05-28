# TABLE_RUNTIME_EXPERIENCE

## Capacidades runtime
| Feature | Estado |
|---------|--------|
| Navegación ↑/↓ en filas | Activo (`site.js`) |
| `data-label` móvil desde headers | Activo |
| `table-responsive` auto-wrap | Activo |
| Densidad compact/comfortable | **Persistida** (`crm_table_density`) |
| Body class `crm-density-compact` | CSS reducido padding/font |

## Uso intensivo
Operadores pueden fijar modo compact una vez y mantenerlo en Users, Workflows, Audit, Settings y tablas del dashboard.

## Bulk / selección
Sin cambios en Fase 11 (evitar sobreingeniería). Patrones `crm-*` en módulos ya migrados.

## Accesibilidad tabla
- Filas `tabindex="0"` para foco.
- Headers propagados a celdas en viewport estrecho.

## Validación
- [ ] Toggle densidad en Index persiste en /Users
- [ ] Arrow keys no rompen inputs dentro de fila editable
