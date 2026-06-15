# PILOT RECOVERY GUIDE

**Audiencia:** Admin cliente + soporte Autonomus  
**Principio:** Recuperación vía UI y ops acordados — sin SQL ad hoc en piloto.

---

## 1. Conexión PostgreSQL fallida

| Síntoma | Causa probable | Recuperación |
|---------|----------------|--------------|
| Timeout | Firewall / red | Abrir puerto; allowlist IP Autonomus |
| Authentication failed | Usuario/contraseña | Regenerar credencial solo-lectura |
| Database does not exist | Nombre BD incorrecto | Corregir en Connect paso 2 |
| SSL required | PG exige SSL | Configurar SSL en perfil (Contact L2) |

**Pasos UI:**

1. `/DatabaseIntelligence/Connect` → editar conexión.
2. Test connection.
3. Si persiste → ticket L2 con hora exacta del intento.

---

## 2. Discovery atascado o Failed

1. Explore → ver estado del job.
2. Si **Failed**: leer mensaje (permiso denegado, tabla bloqueada).
3. Conceder SELECT al usuario en tablas necesarias (DBA cliente — script estándar GRANT, fuera de AutonomusCRM).
4. Re-lanzar Discover.

**No:** ejecutar DDL desde AutonomusCRM.

---

## 3. Understand — mappings incorrectos

1. Understand → desconfirmar entidad errónea.
2. Re-mapear columna → campo negocio.
3. Guardar → volver a Operate.

Sin mappings confirmados, **Start session** falla por diseño (protección).

---

## 4. Health scan lento o vacío

1. Verificar discovery previo completado.
2. Re-run health — modo Full vs Incremental.
3. Si 0 tablas: volver a Discover.

Hallazgos duplicados/huérfanos **no bloquean** Operate — son informativos.

---

## 5. Operate — preview inesperado

| Síntoma | Acción |
|---------|--------|
| 0 filas afectadas | Revisar Filter rules demasiado restrictivas |
| Demasiadas exclusiones | Desactivar Exclude studio temporalmente |
| Merge agresivo | Ajustar Merge rule (KeepNewest vs manual review) |

Siempre usar **Preview** antes de **Execute**.

---

## 6. Import fallido

1. Result Studio — leer errores por fila/entidad.
2. Corregir plan en studios → Preview → Execute (sin import).
3. Re-intentar Import.

Si quota 429 (raro en piloto dedicado): L3 ajusta `DataHub:Security` / DIP quotas en entorno piloto.

---

## 7. Rollback

**Cuándo:** import incorrecto, duplicados no deseados, prueba piloto.

**UI:**

1. `/DatabaseIntelligence/Operate?jobId={id}`
2. Botón **Rollback** (Admin/Owner).
3. Esperar SignalR / refresh.
4. Verificar CRM — registros importados por ese job eliminados.

**API (solo Autonomus L3, no cliente):**

```
POST /api/db-intelligence/operations/{jobId}/rollback?tenantId={tenantId}
```

**Verificación tests:** `DbOperationIntegrationTests` rollback PASS.

Si rollback falla → **congelar imports** → L3 inmediato → no re-importar hasta resolución.

---

## 8. Tenant isolation sospechado

Si el cliente ve datos de otra organización:

1. **Stop** — no continuar flujo.
2. L3 crítico con tenant ID + capturas.
3. Evidencia tests: `TenantIsolationIntegrationTests`, DIP isolation tests.

---

## 9. Recuperación infra Autonomus (L3)

| Componente | Síntoma | Acción |
|------------|---------|--------|
| RabbitMQ | Progress congelado | `docker compose restart rabbitmq` |
| PostgreSQL app | 503 / health fail | Verificar `/health/ready` |
| Redis | Session/cache | Reinicio servicio |

Cliente **no** ejecuta estos pasos.

---

## 10. Matriz escenario → recuperación

| Escenario datos | Health muestra | Operate acción | Recovery |
|-----------------|----------------|----------------|----------|
| Limpio | Score alto | Import directo | Rollback si prueba |
| Dañado | Validity findings | Clean studio | Preview antes execute |
| Duplicados | Duplicate findings | Merge studio | Rollback si merge mal |
| Huérfanos | Orphan findings | Exclude o import parcial | Documentar scope |

---

## Checklist post-incidente

- [ ] Causa raíz documentada (sin secretos)
- [ ] Rollback verificado o no aplicable
- [ ] Cliente puede continuar flujo UI
- [ ] Ticket cerrado con referencia a sección de esta guía
