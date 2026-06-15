# PILOT SUPPORT GUIDE

**Para:** Equipo Autonomus (CS, implementación, soporte L2)  
**Cliente:** Admin/Manager sin acceso a código  
**Baseline:** `AUTONOMUSCRM_REALITY_CHECK_2026.md`

---

## Niveles de soporte

| Nivel | Canal | SLA piloto | Alcance |
|-------|-------|------------|---------|
| L1 | Email / ticket cliente | 4 h laborables | Uso UI, contraseñas, navegación |
| L2 | Videollamada | 1 sesión/semana incluida | Conexión BD, mappings, Operate |
| L3 | Ingeniería | 24 h crítico | Bugs producto, tenant isolation, rollback fallido |

**Regla:** Nunca pedir al cliente SQL, SSH al servidor app, ni acceso al repositorio.

---

## Preguntas frecuentes (cliente)

### “No puedo conectar mi PostgreSQL”

1. Verificar host/puerto desde AutonomusCRM (no desde laptop cliente).
2. Confirmar allowlist IP del entorno Autonomus.
3. Probar usuario solo SELECT.
4. Revisar mensaje en Connect paso 3 — copiar texto exacto al ticket.
5. Escalar L2 si timeout > 15 s repetido.

### “Discover no termina”

1. Confirmar RabbitMQ activo en entorno (L3 si caído).
2. Refrescar página — hub SignalR reconecta.
3. Tablas > 500: acordar ventana off-hours.
4. Revisar job en Explore — estado Failed muestra razón negocio.

### “Operate — Start session falla”

Causas más comunes:

| Mensaje / síntoma | Acción |
|-------------------|--------|
| Mappings not confirmed | Ir a Understand → confirmar entidades |
| Connection inactive | Connect → re-test → activar |
| Empty extract | Verificar permisos SELECT en tablas mapeadas |

### “Import no muestra datos en CRM”

1. Confirmar rol Admin/Owner para import.
2. Verificar Result Studio — filas importadas > 0.
3. Comprobar tenant correcto (no mezclar tenants en bookmark).
4. Audit: `/Audit` filtrado por acción import DIP.

### “¿Pueden arreglar datos con SQL?”

**Respuesta estándar:** No en piloto. Usar Clean/Merge/Exclude en Operate o acordar scope post-piloto.

---

## Escalación a L3 (ingeniería)

Incluir siempre:

- Tenant ID
- Connection profile ID (no password)
- Job ID Operate / Discovery / Health
- Timestamp UTC
- Captura UI del error
- ¿Rollback intentado? resultado

**No incluir:** connection strings completas, contraseñas, PII masiva.

---

## Límites de soporte piloto

| Sí hacemos | No hacemos |
|------------|------------|
| Guiar flujo UI paso a paso | Escribir SQL en BD cliente |
| Ajustar quotas demo en entorno piloto | Modificar código custom por cliente |
| Re-ejecutar rollback desde UI | Acceder a servidor BD del cliente |
| Segunda sesión kickoff si red falló | Migrar Oracle/SQL Server bajo presión |

---

## Contactos internos Autonomus

| Área | Responsabilidad |
|------|-----------------|
| Implementación | Kickoff, checklist, firewall |
| Producto DIP | Gaps UX Operate / Understand |
| Infra | RabbitMQ, Postgres app, secrets |
| Legal | Contrato alcance piloto |

*(Completar emails reales antes del primer piloto.)*

---

## Métricas de éxito soporte

- Tiempo a primera conexión BD < 1 sesión kickoff
- 0 tickets “necesito un desarrollador” resueltos con SQL
- 1 rollback documentado exitoso por piloto
- CSAT ≥ 4/5 post semana 4
