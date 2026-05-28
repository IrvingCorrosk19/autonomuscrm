# Ejecutar AutonomusCRM con Visual Studio

## Requisitos

1. **Visual Studio 2022** (17.12 o superior recomendado) con carga de trabajo **ASP.NET y desarrollo web**.
2. **SDK .NET 9** — en terminal: `dotnet --version` debe mostrar `9.0.x`.
3. **PostgreSQL** en `localhost:5432`, base `autonomuscrm` (credenciales en `appsettings.Development.json`).

Si falta el SDK, instala desde [https://dotnet.microsoft.com/download/dotnet/9.0](https://dotnet.microsoft.com/download/dotnet/9.0) o abre el instalador de VS y marca **.NET 9.0 Runtime**.

## Pasos en Visual Studio

1. Abre `AutonomusCRM.sln`.
2. Clic derecho en **AutonomusCRM.API** → **Establecer como proyecto de inicio**.
3. En la barra de herramientas, perfil de inicio: **AutonomusCRM.API** o **http** (no uses **AutonomusCRM.Workers** para la web).
4. Si usas el perfil **https**, ejecuta una vez en terminal:
   ```powershell
   dotnet dev-certs https --trust
   ```
5. Pulsa **F5**. Debe abrirse: [http://localhost:5154/Account/Login](http://localhost:5154/Account/Login)

Login demo: `admin@autonomuscrm.local` / `Admin123!`

## Errores frecuentes

| Mensaje | Solución |
|--------|----------|
| `address already in use` / puerto 5154 | Otra instancia sigue activa. Ejecuta `.\scripts\stop-dev-api.ps1` y vuelve a F5. |
| `JWT Key not configured` | Asegúrate de que el perfil tenga `ASPNETCORE_ENVIRONMENT=Development` (ya está en `launchSettings.json`). |
| No abre el navegador | Abre manualmente `http://localhost:5154/Account/Login`. |
| Proyecto Workers al F5 | Establece **AutonomusCRM.API** como proyecto de inicio (no Workers). |
| Error de base de datos | Inicia PostgreSQL y revisa la cadena en `appsettings.Development.json`. |

## Perfil de solución (VS 17.12+)

En el desplegable junto al botón de inicio, elige **CRM Web (AutonomusCRM.API)** si aparece (archivo `AutonomusCRM.slnLaunch`).

## Sin Visual Studio

```powershell
cd AutonomusCRM.API
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --launch-profile AutonomusCRM.API
```
