#!/usr/bin/env python3
"""Generate localization-es-PA.json from localization-es.json with Panama overrides."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent / "AutonomusCRM.API" / "Resources"
ES = ROOT / "localization-es.json"
ES_PA = ROOT / "localization-es-PA.json"

# Panama Spanish overrides (formal usted, regional labels)
OVERRIDES = {
    "Lang_SpanishPanama": "Español (Panamá)",
    "Account_LoginSubtitle": "Inicie sesión en su espacio de trabajo.",
    "Account_InvalidCredentials": "Credenciales inválidas. Use el correo y la contraseña de demostración; el Tenant ID se completa automáticamente.",
    "Account_LoginFailed": "No se pudo iniciar sesión. Verifique sus datos e intente nuevamente.",
    "Account_EnterMfaCode": "Ingrese el código de su aplicación autenticadora.",
    "Account_InvalidMfaExpired": "Código MFA inválido o vencido.",
    "Integrations_OAuthNotConfigured": "OAuth no está configurado para {0}. Utilice conexión manual.",
    "Integrations_ProviderConnected": "{0} conectado correctamente.",
    "Cs_Message_PlaybookExecuted": "Playbook {0}: {1} tareas creadas.",
    "Customers_Create_PlaceholderPhone": "Ej: +507 6000-0000",
    "Leads_Create_PlaceholderPhone": "Ej: +507 6000-0000",
    "Settings_TenantLabel": "Inquilino",
    "Account_Tenant": "Inquilino:",
    "Common_Phone": "Teléfono celular",
    "Billing_PageTitle": "Facturación",
    "Deals_PageTitle": "Oportunidades",
    "Nav_Section_Commerce": "Comercial",
    "Form_SelectCustomer": "Seleccione un cliente",
    "Form_SelectSource": "Seleccione una fuente",
    "Toast_Ready": "Listo",
    "Operation_Retry": "Reintentar",
    "Shell_CommsConfigure": "Configurar",
    "Import_CsvFormatCustomers": "Formato CSV: Nombre,Correo,Celular,Empresa",
}

def main():
    with ES.open(encoding="utf-8-sig") as f:
        data = json.load(f)
    data.update(OVERRIDES)
    if "Lang_SpanishPanama" not in data:
        data["Lang_SpanishPanama"] = "Español (Panamá)"
    with ES_PA.open("w", encoding="utf-8") as f:
        json.dump(data, f, ensure_ascii=False, indent=4)
        f.write("\n")
    print(f"OK: {len(data)} keys -> {ES_PA.name}")

if __name__ == "__main__":
    main()
