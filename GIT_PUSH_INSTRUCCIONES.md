# Instrucciones para hacer Push a GitHub

## ⚠️ IMPORTANTE: GitHub requiere Personal Access Token

GitHub ya no acepta contraseñas para autenticación. Necesitas crear un **Personal Access Token (PAT)**.

## Pasos para crear el token:

1. **Ve a GitHub Settings:**
   - https://github.com/settings/tokens
   - O: GitHub → Tu perfil → Settings → Developer settings → Personal access tokens → Tokens (classic)

2. **Genera un nuevo token:**
   - Click en "Generate new token" → "Generate new token (classic)"
   - **Nombre:** `AutonomusCRM-Development`
   - **Expiración:** Elige una fecha (o "No expiration" para desarrollo)
   - **Scopes:** Selecciona `repo` (acceso completo a repositorios)
   - Click en "Generate token"

3. **Copia el token inmediatamente** (solo se muestra una vez)

## Opción 1: Usar el token en la URL (temporal)

```bash
git remote set-url origin https://TU_TOKEN@github.com/IrvingCorrosk19/autonomuscrm.git
git push -u origin main
```

## Opción 2: Usar GitHub CLI (recomendado)

```bash
# Instalar GitHub CLI si no lo tienes
# Windows: winget install GitHub.cli

# Autenticarse
gh auth login

# Hacer push
git push -u origin main
```

## Opción 3: Usar SSH (más seguro para producción)

1. **Generar clave SSH:**
   ```bash
   ssh-keygen -t ed25519 -C "irvingcorrosk19@gmil.com"
   ```

2. **Agregar clave pública a GitHub:**
   - Copia el contenido de `~/.ssh/id_ed25519.pub`
   - Ve a: https://github.com/settings/keys
   - Click en "New SSH key"
   - Pega la clave y guarda

3. **Cambiar remote a SSH:**
   ```bash
   git remote set-url origin git@github.com:IrvingCorrosk19/autonomuscrm.git
   git push -u origin main
   ```

## Estado actual

✅ `.gitignore` actualizado y completo
✅ Todas las vistas creadas y actualizadas
✅ Commit realizado: "feat: Actualizar .gitignore y completar todas las vistas con diseño unificado"
⏳ Pendiente: Push al repositorio remoto (requiere token)

## Verificar estado

```bash
git status
git log --oneline -5
git remote -v
```
