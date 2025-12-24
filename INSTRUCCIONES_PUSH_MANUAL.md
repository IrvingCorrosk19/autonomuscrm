# 📋 Instrucciones para Push Manual a GitHub

## Paso 1: Crear Personal Access Token (PAT)

### 1.1. Ir a la página de tokens
- Abre tu navegador
- Ve a: **https://github.com/settings/tokens**
- O: GitHub → Tu perfil (arriba derecha) → **Settings** → **Developer settings** → **Personal access tokens** → **Tokens (classic)**

### 1.2. Generar nuevo token
- Click en el botón verde: **"Generate new token"**
- Selecciona: **"Generate new token (classic)"**

### 1.3. Configurar el token
- **Note (Nombre):** `AutonomusCRM-Push`
- **Expiration (Expiración):**
  - Para desarrollo: Selecciona "No expiration" o una fecha lejana (ej: 1 año)
  - Para producción: Fecha específica
- **Select scopes (Permisos):**
  - ✅ **Marca SOLO este checkbox:**
    - ✅ **`repo`** - Full control of private repositories
  - ❌ NO marques otros permisos (workflow, admin, etc.)

### 1.4. Generar y copiar
- Scroll hacia abajo
- Click en el botón verde: **"Generate token"**
- **⚠️ IMPORTANTE:** Copia el token INMEDIATAMENTE
  - El token empieza con `ghp_` (ejemplo: `ghp_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx`)
  - Solo se muestra UNA VEZ
  - Si lo pierdes, tendrás que crear uno nuevo

---

## Paso 2: Configurar Git con el Token

### Opción A: Usar el token en la URL (Recomendado para primera vez)

Abre PowerShell o Terminal en la carpeta del proyecto (`C:\Proyectos\CRM`) y ejecuta:

```powershell
# Reemplaza TU_TOKEN con el token que copiaste
git remote set-url origin https://TU_TOKEN@github.com/IrvingCorrosk19/autonomuscrm.git

# Verificar que se configuró correctamente
git remote -v
```

**Ejemplo:**
```powershell
git remote set-url origin https://ghp_abc123xyz456@github.com/IrvingCorrosk19/autonomuscrm.git
```

### Opción B: Usar el token directamente en el push

```powershell
git push https://TU_TOKEN@github.com/IrvingCorrosk19/autonomuscrm.git main
```

---

## Paso 3: Hacer el Push

Una vez configurado el remote con el token, ejecuta:

```powershell
git push -u origin main
```

Si todo está bien, verás algo como:
```
Enumerating objects: X, done.
Counting objects: 100% (X/X), done.
Delta compression using up to X threads
Compressing objects: 100% (X/X), done.
Writing objects: 100% (X/X), done.
To https://github.com/IrvingCorrosk19/autonomuscrm.git
 * [new branch]      main -> main
Branch 'main' set up to track remote branch 'main' from 'origin'.
```

---

## Paso 4: Verificar que funcionó

### 4.1. Verificar en Git
```powershell
git status
git branch -r
git log --oneline --all --graph -5
```

### 4.2. Verificar en GitHub
- Ve a: **https://github.com/IrvingCorrosk19/autonomuscrm**
- Deberías ver todos tus archivos y commits

---

## ⚠️ Problemas Comunes y Soluciones

### Error: "Invalid username or token"
- **Causa:** El token no tiene el scope `repo`
- **Solución:** Crea un nuevo token y asegúrate de marcar SOLO `repo`

### Error: "Permission denied"
- **Causa:** El token expiró o fue revocado
- **Solución:** Crea un nuevo token

### Error: "Repository not found"
- **Causa:** El token no tiene acceso al repositorio
- **Solución:** Verifica que el token tenga el scope `repo` y que el repositorio exista

### El token no funciona después de copiarlo
- **Causa:** Copiaste el token incompleto o con espacios
- **Solución:** Copia el token completo, sin espacios antes o después

---

## 🔒 Seguridad del Token

### ⚠️ IMPORTANTE:
- **NUNCA** subas el token al repositorio
- **NUNCA** compartas el token públicamente
- Si el token se expone, revócalo inmediatamente en: https://github.com/settings/tokens

### Para revocar un token:
1. Ve a: https://github.com/settings/tokens
2. Encuentra el token
3. Click en "Revoke"

---

## 📝 Resumen Rápido

1. ✅ Crear token en: https://github.com/settings/tokens
2. ✅ Marcar solo el scope `repo`
3. ✅ Copiar el token (empieza con `ghp_`)
4. ✅ Ejecutar: `git remote set-url origin https://TU_TOKEN@github.com/IrvingCorrosk19/autonomuscrm.git`
5. ✅ Ejecutar: `git push -u origin main`
6. ✅ Verificar en GitHub

---

## 🎯 Comandos Finales (Copia y pega)

Reemplaza `TU_TOKEN` con tu token real:

```powershell
# Configurar remote con token
git remote set-url origin https://TU_TOKEN@github.com/IrvingCorrosk19/autonomuscrm.git

# Hacer push
git push -u origin main

# Verificar
git status
```

---

¡Listo! Si tienes algún problema, revisa la sección "Problemas Comunes" arriba.

