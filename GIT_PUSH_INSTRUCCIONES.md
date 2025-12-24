# Instrucciones para subir el código a GitHub

## Opción 1: Usar Personal Access Token (Recomendado)

1. **Crear un Personal Access Token en GitHub:**
   - Ve a: https://github.com/settings/tokens
   - Click en "Generate new token" → "Generate new token (classic)"
   - Nombre: "AutonomusCRM"
   - Selecciona el scope: `repo` (acceso completo a repositorios)
   - Click en "Generate token"
   - **Copia el token** (solo se muestra una vez)

2. **Configurar el remote con el token:**
   ```bash
   git remote set-url origin https://TU_TOKEN@github.com/IrvingCorrosk19/autonomuscrm.git
   ```

3. **Hacer push:**
   ```bash
   git push -u origin main
   ```

## Opción 2: Usar GitHub CLI

```bash
gh auth login
git push -u origin main
```

## Opción 3: Usar SSH (Recomendado para producción)

1. **Generar clave SSH:**
   ```bash
   ssh-keygen -t ed25519 -C "irvingcorrosk19@gmil.com"
   ```

2. **Agregar la clave pública a GitHub:**
   - Copia el contenido de `~/.ssh/id_ed25519.pub`
   - Ve a: https://github.com/settings/keys
   - Click en "New SSH key"
   - Pega la clave y guarda

3. **Cambiar el remote a SSH:**
   ```bash
   git remote set-url origin git@github.com:IrvingCorrosk19/autonomuscrm.git
   ```

4. **Hacer push:**
   ```bash
   git push -u origin main
   ```

## Estado Actual

✅ Repositorio Git inicializado
✅ Archivos agregados al staging
✅ Commit realizado: "Initial commit: AUTONOMUS CRM - Sistema completo al 100%"
✅ Branch renombrado a 'main'
⏳ Pendiente: Push al repositorio remoto

## Verificar estado

```bash
git status
git remote -v
git log --oneline
```


