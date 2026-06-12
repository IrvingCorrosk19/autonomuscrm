# AutonomusCRM — Documentación Enterprise

Paquete de habilitación corporativa nivel Salesforce / Microsoft Dynamics 365.

---

## Estructura del paquete

```
Documentation/
├── README.md                              ← Este índice
├── CORPORATE_TRANSFORMATION_REPORT.md     ← Informe de transformación v2.0
├── ROLE_DISCOVERY_REPORT.md               ← Fuente: inventario de roles
├── ROLE_PERMISSION_MATRIX.md              ← Fuente: matriz permisos
├── Roles/                                 ← Manuales fuente (v1.0)
├── Corporate/                             ← ✨ Versión corporativa (v2.0)
│   ├── Roles/
│   ├── manual-empresarial/
│   └── SuperAdmin_Status.md
├── Word/                                  ← ✨ Documentos Word (.docx)
├── _shared/                               ← Glosario y guía de estilo
└── [Playbooks y guías fuente]
```

---

## ¿Qué versión usar?

| Necesidad | Usar |
|-----------|------|
| Capacitación / clientes / auditoría | `Corporate/` o `Word/` |
| Edición técnica del contenido | `Documentation/` (fuente) |
| Manual maestro 18 capítulos | `docs/manual-empresarial-autonomuscrm/` |

---

## Documentos Word (`Word/`)

| Archivo | Audiencia |
|---------|-----------|
| `Admin_User_Manual.docx` | Administradores |
| `Manager_User_Manual.docx` | Gerentes |
| `Sales_User_Manual.docx` | Ejecutivos de ventas |
| `Support_User_Manual.docx` | Soporte / CS |
| `Viewer_User_Manual.docx` | Consulta |
| `Admin_Operations_Guide.docx` | Operaciones Admin |
| `Sales_Playbook.docx` | Equipo comercial |
| `Support_Operations_Guide.docx` | Equipo soporte |
| `CustomerSuccess_Playbook.docx` | Customer Success |
| `Marketing_Operations_Guide.docx` | Marketing (no es rol) |
| `New_Employee_Onboarding.docx` | Nuevos colaboradores |
| `FAQ.docx` | 150 preguntas globales |
| `Troubleshooting.docx` | Resolución de incidencias |
| `Business_Flows.docx` | Flujos de negocio |
| `Role_Permission_Matrix.docx` | Matriz de permisos |
| `SuperAdmin_Status.docx` | Aclaración: rol no existe |

---

## Roles del sistema (5 verificados)

| Rol | Email demo | Manual corporativo | Word |
|-----|------------|-------------------|------|
| Admin | admin@autonomuscrm.local | `Corporate/Roles/Admin_User_Manual.md` | `Admin_User_Manual.docx` |
| Manager | manager@autonomuscrm.local | `Corporate/Roles/Manager_User_Manual.md` | `Manager_User_Manual.docx` |
| Sales | sales@autonomuscrm.local | `Corporate/Roles/Sales_User_Manual.md` | `Sales_User_Manual.docx` |
| Support | support@autonomuscrm.local | `Corporate/Roles/Support_User_Manual.md` | `Support_User_Manual.docx` |
| Viewer | viewer@autonomuscrm.local | `Corporate/Roles/Viewer_User_Manual.md` | `Viewer_User_Manual.docx` |

**No existe:** SuperAdmin, Marketing, Customer Success (como rol), Analyst.

---

## Regenerar documentación corporativa

```powershell
python scripts/corporate_doc_transform.py
python scripts/generate_word_docs.py
```

---

## Credenciales demo

Contraseña: `{Rol}123!` (ej. `Sales123!`)

---

*Versión documental 2.0.0 — Confidencial — Uso interno y clientes autorizados*
