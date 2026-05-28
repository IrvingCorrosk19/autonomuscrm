# EMAIL AUTOMATION ENGINE

## Servicio
`IEmailAutomationEngine` → `EmailAutomationEngine`

## Eventos
Welcome, Onboarding, FollowUp, Renewal, Risk, ReEngagement, LeadWelcome.

## Plantillas (variables `{{name}}`, `{{renewal_date}}`, etc.)
welcome, onboarding, followup, renewal, risk, reengagement

## Tracking
Registro en `CustomerCommunicationLogs`: `TrackingId`, `Status` (Queued/Sent/Failed), `Variables` jsonb.

## Provider
`IEmailDeliveryProvider` — implementación default `LogEmailDeliveryProvider` (producción: SMTP/SendGrid vía DI).

## Integración
`CommunicationAgent` — bienvenida cliente y follow-up lead.
