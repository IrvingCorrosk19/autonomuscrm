# Permisos Exactos para el Token de GitHub

## Permisos Requeridos para Push

Para hacer push al repositorio `autonomuscrm`, el token necesita estos permisos:

### ✅ Permisos OBLIGATORIOS:

1. **`repo`** (Full control of private repositories)
   - Este es el permiso MÁS IMPORTANTE
   - Incluye automáticamente:
     - `repo:status` - Acceso a estado de repositorios
     - `repo_deployment` - Acceso a deployments
     - `public_repo` - Acceso a repositorios públicos
     - `repo:invite` - Invitar colaboradores
     - `security_events` - Eventos de seguridad

### 📋 Pasos Detallados para Crear el Token:

1. **Ve a la página de tokens:**
   ```
   https://github.com/settings/tokens
   ```

2. **Click en "Generate new token" → "Generate new token (classic)"**

3. **Configuración del token:**
   - **Note:** `AutonomusCRM-Push-Access`
   - **Expiration:** 
     - Para desarrollo: "No expiration" o una fecha lejana
     - Para producción: Fecha específica
   - **Scopes (Permisos):**
     - ✅ **Marca SOLO este:**
       - ✅ `repo` ← **ESTE ES EL ÚNICO QUE NECESITAS**
   
   **NO marques otros permisos a menos que los necesites específicamente**

4. **Click en "Generate token" (abajo de la página)**

5. **Copia el token inmediatamente** (solo se muestra una vez)
   - El token empezará con `ghp_` (classic token)
   - O con `github_pat_` (fine-grained token)

## ⚠️ IMPORTANTE:

- **Solo necesitas el scope `repo`** para hacer push
- No necesitas `workflow`, `admin`, `delete_repo`, etc.
- El scope `repo` es suficiente para:
  - Push
  - Pull
  - Crear branches
  - Crear tags
  - Todo lo relacionado con el repositorio

## 🔍 Verificar Permisos del Token Actual:

Si ya tienes un token, puedes verificar sus permisos en:
```
https://github.com/settings/tokens
```

Si el token no tiene el scope `repo` marcado, necesitas crear uno nuevo.

## 📝 Nota sobre Fine-Grained Tokens:

Si estás usando un **Fine-Grained Token** (empieza con `github_pat_`):
- Necesitas darle permisos específicos al repositorio
- Ve a: Settings → Developer settings → Personal access tokens → Fine-grained tokens
- Asegúrate de que tenga acceso al repositorio `autonomuscrm`
- Permisos necesarios: `Contents: Read and write`

## 🚀 Después de Crear el Token:

Una vez que tengas el token con el scope `repo`, úsalo así:

```bash
git remote set-url origin https://TU_TOKEN@github.com/IrvingCorrosk19/autonomuscrm.git
git push -u origin main
```

O simplemente:

```bash
git push https://TU_TOKEN@github.com/IrvingCorrosk19/autonomuscrm.git main
```

