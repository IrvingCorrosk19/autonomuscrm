# Informe — Internacionalización (es / en)

**Fecha:** 2026-06-04  
**Alcance:** AutonomusCRM.API (Razor Pages + JS shell)  
**Build:** `dotnet build AutonomusCRM.API.csproj -c Release` — **OK**

---

## 1. Resumen

Se implementó internacionalización completa para **español (por defecto)** e **inglés**, sin cambios de lógica de negocio ni de base de datos.

- **245 claves** en recursos compartidos (`SharedResource`)
- **4 claves** de validación (`ValidationMessages`)
- Selector **Español | English** en topbar (app autenticada) y login
- Preferencia persistida en cookie `.AspNetCore.Culture` (1 año)
- Textos JS (paleta, toasts, tema) vía `window.__flowI18n`

---

## 2. Estructura de recursos

```
AutonomusCRM.API/Resources/
├── SharedResource.cs
├── SharedResource.es.resx      ← fuente generada desde localization-es.json
├── SharedResource.en.resx
├── ValidationMessages.cs
├── ValidationMessages.es.resx
├── ValidationMessages.en.resx
├── localization-es.json        ← mantenimiento de traducciones (fuente)
├── localization-en.json
├── Views/
│   └── ModuleViewResources.cs   ← marker para recursos por vista (fase 2)
└── Controllers/
    └── ControllerResources.cs   ← marker para mensajes API (fase 2)

scripts/generate-localization-resx.ps1   ← regenera .resx desde JSON
```

### Convención de claves

| Prefijo | Uso |
|---------|-----|
| `Shell_` | Layout, topbar, paleta |
| `Nav_` | Sidebar y secciones |
| `Account_` | Login, MFA, errores auth |
| `Btn_` / `Common_` | Acciones reutilizables |
| `Command_`, `Executive_`, `Revenue_`, … | Módulos de producto |
| `Route_` / `Palette_` / `Toast_` | Cliente JS |

---

## 3. Configuración (`Program.cs`)

- `AddAppLocalization()` — `ResourcesPath = "Resources"`
- `AddViewLocalization(Suffix)` en Razor Pages
- `AddDataAnnotationsLocalization` → `ValidationMessages`
- `UseAppLocalization()` antes de `UseRouting()`
- Culturas: **es** (default), **en**
- Proveedores: cookie → `Accept-Language`

---

## 4. Cambio de idioma

### UI

En la barra superior (y en login): **Español | English**

### Endpoint

```
GET /culture/set?culture=en&returnUrl=/ruta-actual
GET /culture/set?culture=es&returnUrl=/ruta-actual
```

- Controlador: `Controllers/CultureController.cs` (`[AllowAnonymous]`)
- Cookie: estándar ASP.NET Core `CookieRequestCultureProvider`

---

## 5. Uso en código

### Vistas Razor

```cshtml
@inject IStringLocalizer<SharedResource> L   <!-- en _ViewImports -->

@L["Nav_Leads"]
@L["Shell_CommsActive", emailMode, whatsAppMode]
```

### PageModel (servidor)

```csharp
private readonly IStringLocalizer<SharedResource> _localizer;
ErrorMessage = _localizer["Account_InvalidCredentials"];
```

### JavaScript

```javascript
flowI18n('toastSuccess', 'Éxito');
// Rutas de paleta: window.__flowI18n.routes (inyectado en _Layout)
```

---

## 6. Archivos modificados (principales)

| Área | Archivos |
|------|----------|
| Infra | `Extensions/LocalizationExtensions.cs`, `Program.cs`, `Controllers/CultureController.cs` |
| Recursos | `Resources/*`, `scripts/generate-localization-resx.ps1` |
| Layout / nav | `_Layout.cshtml`, `Flow/_FlowSidebar.cshtml`, `_LanguageSelector.cshtml`, `_FlowI18nScript.cshtml` |
| Account | `Account/Login.cshtml`, `Login.cshtml.cs`, `AccessDenied.cshtml`, `Error.cshtml` |
| Partials | `_CrmEmptyState`, `_FlowEmptyState`, `_FlowTrustActions` |
| Páginas core | `Index`, `Executive`, `Revenue`, `TrustInbox`, `Leads`, `Deals`, `Customers`, `Customer360`, `Settings`, `Users`, `Agents`, `Memory`, `Integrations`, `Audit`, `Policies`, `Billing`, `Tasks`, `Workflows`, `VoiceCalls`, `CustomerSuccess` |
| JS/CSS | `wwwroot/js/flow-shell.js`, `flow-worldclass.js`, `site.js`, `wwwroot/css/site.css` |
| Global | `Pages/_ViewImports.cshtml` |

---

## 7. Textos migrados (cobertura)

| Capa | Estado |
|------|--------|
| Shell (layout, menú, topbar, footer) | ✅ Completo |
| Login / AccessDenied / Error | ✅ Completo |
| Títulos de páginas principales | ✅ Completo |
| Trust actions + mensajes servidor TrustInbox | ✅ Completo |
| Command (Index) — bloques vacíos y hero principal | ✅ Parcial |
| Leads / Customer360 — headers, filtros, vacíos | ✅ Parcial |
| JS paleta, toasts, tema oscuro | ✅ Completo |
| Formularios Create/Edit (Leads, Deals, Users, …) | ⏳ Pendiente |
| Marketing (`Landing`, `Demo`, `Pricing`, `Roi`) | ⏳ Pendiente |
| `_FlowInsightActions`, `_FlowExplainability` | ⏳ Pendiente |
| Enums en tablas (LeadStatus, DealStage) | ⏳ Pendiente |
| Emails / plantillas HTML | ⏳ No existen plantillas transaccionales en API |
| DataAnnotations en ViewModels | ⏳ Infra lista; atributos aún en inglés por defecto EF |

---

## 8. Validaciones realizadas

| Prueba | Resultado |
|--------|-----------|
| `dotnet build -c Release` | ✅ PASS |
| Análisis estático (sin errores nuevos) | ✅ Solo warnings preexistentes |
| VPS `http://164.68.99.83:8091` | ⚠️ Aún despliega build **anterior** (`/culture/set` → 404). Requiere `deploy-vps.ps1` |

### Prueba local recomendada

1. `dotnet run --project AutonomusCRM.API`
2. Abrir `/Account/Login` → textos en español
3. Clic **English** → "Sign in", "Secure access", sidebar en inglés tras login
4. Navegar `/`, `/Leads`, `/Customer360` — títulos y menú coherentes
5. `Ctrl+K` — paleta en idioma activo

---

## 9. Pendientes (fase 2)

1. Migrar **~40 vistas** secundarias (Create/Edit/Import/Bulk, marketing, Command subpages)
2. Localizar `_FlowInsightActions` y contenido dinámico Executive/Revenue/TrustInbox body
3. Recursos `ModuleViewResources.es/en.resx` por módulo si el JSON central crece demasiado
4. Localizar mensajes `TempData` en PageModels (Leads bulk, Users, etc.)
5. Enum display names (`IStringLocalizer` + helper)
6. Desplegar al VPS y validar cookie cross-request en HTTPS
7. Tests automatizados: `Culture=es` vs `Culture=en` en `WebApplicationFactory`

---

## 10. Mantenimiento de traducciones

1. Editar `Resources/localization-es.json` y `localization-en.json`
2. Ejecutar: `powershell -File scripts/generate-localization-resx.ps1`
3. Rebuild y verificar claves faltantes (muestran el nombre de la clave en UI)

---

## 11. Reglas respetadas

- ✅ Sin cambios de lógica de negocio
- ✅ Sin cambios de BD
- ✅ Sin eliminación de funcionalidades
- ✅ Estilos/layout preservados (+ estilos mínimos del selector de idioma)
- ✅ Preparado para más idiomas (`fr`, `pt`, …) añadiendo JSON + cultura en `LocalizationExtensions`
