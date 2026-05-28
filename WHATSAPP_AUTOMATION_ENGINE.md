# WHATSAPP AUTOMATION ENGINE

## Servicio
`IWhatsAppAutomationEngine` → `WhatsAppAutomationEngine`

## Eventos
Welcome, Reminder, Renewal, FollowUp, Recovery (Re-engagement).

## Plantillas
welcome, reminder, renewal, followup, recovery — variables configurables.

## Configuración
Sustituir `IWhatsAppDeliveryProvider` en DI (Twilio/Meta Business API).

## Tracking
Misma tabla `CustomerCommunicationLogs`, `Channel=WhatsApp`.

## Automatización
- Bienvenida: `CommunicationAgent`
- Recuperación: scan retention (cliente inactivo &gt; 45d)
