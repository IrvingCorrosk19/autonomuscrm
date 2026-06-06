-- AutonomusCRM VPS Test — Datos funcionales minimos
-- Requiere: 02_CLEAN_TEST_DATABASE_SCRIPT.sql ejecutado primero
-- Tenant: b1000000-0000-4000-8000-000000000001 (TechSolutions Panama)

\set ON_ERROR_STOP on

BEGIN;

-- Limpiar datos funcionales previos del tenant
DELETE FROM "WorkflowTasks" WHERE "TenantId" = 'b1000000-0000-4000-8000-000000000001';
DELETE FROM "AiDecisionAudits" WHERE "TenantId" = 'b1000000-0000-4000-8000-000000000001';
DELETE FROM "DomainEvents" WHERE "TenantId" = 'b1000000-0000-4000-8000-000000000001';
DELETE FROM "ProductUsageEvents" WHERE "TenantId" = 'b1000000-0000-4000-8000-000000000001';
DELETE FROM "Deals" WHERE "TenantId" = 'b1000000-0000-4000-8000-000000000001';
DELETE FROM "Leads" WHERE "TenantId" = 'b1000000-0000-4000-8000-000000000001';
DELETE FROM "Customers" WHERE "TenantId" = 'b1000000-0000-4000-8000-000000000001';

-- 5 Clientes
INSERT INTO "Customers" (
    "Id", "TenantId", "Name", "Email", "Phone", "Company", "Status", "Metadata",
    "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy"
) VALUES
('c1000001-0000-4000-8000-000000000001', 'b1000000-0000-4000-8000-000000000001', 'Banco Regional PA', 'contacto@bancoregional.pa', '+50760010001', 'Banco Regional', 3, '{}'::jsonb, NOW() AT TIME ZONE 'UTC', NULL, 'vps-test-seed', NULL),
('c1000001-0000-4000-8000-000000000002', 'b1000000-0000-4000-8000-000000000001', 'Logistica Express SA', 'ventas@logisticaexpress.pa', '+50760010002', 'Logistica Express', 3, '{}'::jsonb, NOW() AT TIME ZONE 'UTC', NULL, 'vps-test-seed', NULL),
('c1000001-0000-4000-8000-000000000003', 'b1000000-0000-4000-8000-000000000001', 'RetailMax Panama', 'info@retailmax.pa', '+50760010003', 'RetailMax', 2, '{}'::jsonb, NOW() AT TIME ZONE 'UTC', NULL, 'vps-test-seed', NULL),
('c1000001-0000-4000-8000-000000000004', 'b1000000-0000-4000-8000-000000000001', 'Clinica Salud Integral', 'admin@saludintegral.pa', '+50760010004', 'Salud Integral', 3, '{}'::jsonb, NOW() AT TIME ZONE 'UTC', NULL, 'vps-test-seed', NULL),
('c1000001-0000-4000-8000-000000000005', 'b1000000-0000-4000-8000-000000000001', 'Constructora del Canal', 'proyectos@cdc.pa', '+50760010005', 'CDC', 4, '{}'::jsonb, NOW() AT TIME ZONE 'UTC', NULL, 'vps-test-seed', NULL);

-- 10 Leads (Status: 0=New, 1=Contacted, 2=Qualified; Source: 1=Website, 2=Referral, etc.)
INSERT INTO "Leads" (
    "Id", "TenantId", "Name", "Email", "Phone", "Company", "Status", "Source", "Metadata",
    "AssignedToUserId", "Score", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy"
) VALUES
('f1000001-0000-4000-8000-000000000001', 'b1000000-0000-4000-8000-000000000001', 'Lead Web Fintech', 'lead1@fintech-test.pa', NULL, 'FinTech PA', 0, 1, '{}'::jsonb, 'b1000001-0000-4000-8000-000000000004', 72, NOW() AT TIME ZONE 'UTC', NULL, 'vps-test-seed', NULL),
('f1000001-0000-4000-8000-000000000002', 'b1000000-0000-4000-8000-000000000001', 'Referido Seguros', 'lead2@seguros-test.pa', NULL, 'Seguros Atlas', 1, 2, '{}'::jsonb, 'b1000001-0000-4000-8000-000000000005', 65, NOW() AT TIME ZONE 'UTC', NULL, 'vps-test-seed', NULL),
('f1000001-0000-4000-8000-000000000003', 'b1000000-0000-4000-8000-000000000001', 'Campana Email Q2', 'lead3@email-test.pa', NULL, 'Marketing Co', 0, 4, '{}'::jsonb, NULL, 55, NOW() AT TIME ZONE 'UTC', NULL, 'vps-test-seed', NULL),
('f1000001-0000-4000-8000-000000000004', 'b1000000-0000-4000-8000-000000000001', 'Partner Telecom', 'lead4@telecom-test.pa', '+50760020004', 'Telecom Sur', 2, 6, '{}'::jsonb, 'b1000001-0000-4000-8000-000000000004', 80, NOW() AT TIME ZONE 'UTC', NULL, 'vps-test-seed', NULL),
('f1000001-0000-4000-8000-000000000005', 'b1000000-0000-4000-8000-000000000001', 'Evento SaaS Summit', 'lead5@summit-test.pa', NULL, 'Summit Corp', 1, 7, '{}'::jsonb, 'b1000001-0000-4000-8000-000000000005', 60, NOW() AT TIME ZONE 'UTC', NULL, 'vps-test-seed', NULL),
('f1000001-0000-4000-8000-000000000006', 'b1000000-0000-4000-8000-000000000001', 'Cold Call Industria', 'lead6@industria-test.pa', NULL, 'Industria Norte', 0, 5, '{}'::jsonb, NULL, 45, NOW() AT TIME ZONE 'UTC', NULL, 'vps-test-seed', NULL),
('f1000001-0000-4000-8000-000000000007', 'b1000000-0000-4000-8000-000000000001', 'Social Media Ads', 'lead7@social-test.pa', NULL, 'Social Brands', 0, 3, '{}'::jsonb, 'b1000001-0000-4000-8000-000000000004', 58, NOW() AT TIME ZONE 'UTC', NULL, 'vps-test-seed', NULL),
('f1000001-0000-4000-8000-000000000008', 'b1000000-0000-4000-8000-000000000001', 'Lead Perdido Demo', 'lead8@lost-test.pa', NULL, 'Lost Co', 4, 1, '{}'::jsonb, NULL, 30, NOW() AT TIME ZONE 'UTC', NULL, 'vps-test-seed', NULL),
('f1000001-0000-4000-8000-000000000009', 'b1000000-0000-4000-8000-000000000001', 'Lead Calificado VIP', 'lead9@vip-test.pa', NULL, 'VIP Prospects', 2, 2, '{}'::jsonb, 'b1000001-0000-4000-8000-000000000003', 88, NOW() AT TIME ZONE 'UTC', NULL, 'vps-test-seed', NULL),
('f1000001-0000-4000-8000-000000000010', 'b1000000-0000-4000-8000-000000000001', 'Lead Nuevo Hoy', 'lead10@hoy-test.pa', NULL, 'Nuevo Cliente SA', 0, 1, '{}'::jsonb, 'b1000001-0000-4000-8000-000000000005', 50, NOW() AT TIME ZONE 'UTC', NULL, 'vps-test-seed', NULL);

-- 5 Oportunidades (Deals)
INSERT INTO "Deals" (
    "Id", "TenantId", "CustomerId", "Title", "Description", "Amount", "ExpectedAmount",
    "Status", "Stage", "Probability", "AssignedToUserId", "ExpectedCloseDate", "ClosedAt",
    "Metadata", "Version", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy"
) VALUES
('d1000001-0000-4000-8000-000000000001', 'b1000000-0000-4000-8000-000000000001', 'c1000001-0000-4000-8000-000000000001', 'CRM Enterprise Banco Regional', 'Implementacion full suite', 85000.00, 90000.00, 0, 3, 75, 'b1000001-0000-4000-8000-000000000004', NOW() AT TIME ZONE 'UTC' + INTERVAL '15 days', NULL, '{}'::jsonb, 0, NOW() AT TIME ZONE 'UTC', NULL, 'vps-test-seed', NULL),
('d1000001-0000-4000-8000-000000000002', 'b1000000-0000-4000-8000-000000000001', 'c1000001-0000-4000-8000-000000000002', 'Automatizacion Logistica', 'Workflows + integraciones', 42000.00, 45000.00, 0, 2, 50, 'b1000001-0000-4000-8000-000000000005', NOW() AT TIME ZONE 'UTC' + INTERVAL '30 days', NULL, '{}'::jsonb, 0, NOW() AT TIME ZONE 'UTC', NULL, 'vps-test-seed', NULL),
('d1000001-0000-4000-8000-000000000003', 'b1000000-0000-4000-8000-000000000001', 'c1000001-0000-4000-8000-000000000003', 'Retail Analytics Pilot', 'Piloto 90 dias', 18000.00, 20000.00, 0, 1, 35, 'b1000001-0000-4000-8000-000000000004', NOW() AT TIME ZONE 'UTC' + INTERVAL '45 days', NULL, '{}'::jsonb, 0, NOW() AT TIME ZONE 'UTC', NULL, 'vps-test-seed', NULL),
('d1000001-0000-4000-8000-000000000004', 'b1000000-0000-4000-8000-000000000001', 'c1000001-0000-4000-8000-000000000004', 'Salud Integral — Cerrada Ganada', 'Contrato anual', 55000.00, 55000.00, 1, 4, 100, 'b1000001-0000-4000-8000-000000000004', NOW() AT TIME ZONE 'UTC' - INTERVAL '5 days', NOW() AT TIME ZONE 'UTC' - INTERVAL '3 days', '{}'::jsonb, 0, NOW() AT TIME ZONE 'UTC' - INTERVAL '60 days', NULL, 'vps-test-seed', NULL),
('d1000001-0000-4000-8000-000000000005', 'b1000000-0000-4000-8000-000000000001', 'c1000001-0000-4000-8000-000000000005', 'CDC — Cerrada Perdida', 'Competidor seleccionado', 120000.00, 120000.00, 1, 5, 0, 'b1000001-0000-4000-8000-000000000005', NOW() AT TIME ZONE 'UTC' - INTERVAL '10 days', NOW() AT TIME ZONE 'UTC' - INTERVAL '8 days', '{}'::jsonb, 0, NOW() AT TIME ZONE 'UTC' - INTERVAL '90 days', NULL, 'vps-test-seed', NULL);

-- 5 Tareas operativas
INSERT INTO "WorkflowTasks" (
    "Id", "TenantId", "WorkflowId", "Title", "Description", "Status",
    "RelatedEntityId", "RelatedEntityType", "AssignedToUserId", "DueDate",
    "Priority", "TaskType", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy"
) VALUES
('e2000001-0000-4000-8000-000000000001', 'b1000000-0000-4000-8000-000000000001', 'b1000004-0000-4000-8000-000000000001', 'Llamar lead Fintech', 'Primer contacto', 'Open', 'f1000001-0000-4000-8000-000000000001', 'Lead', 'b1000001-0000-4000-8000-000000000004', NOW() AT TIME ZONE 'UTC' + INTERVAL '2 days', 'High', NULL, NOW() AT TIME ZONE 'UTC', NULL, 'vps-test-seed', NULL),
('e2000001-0000-4000-8000-000000000002', 'b1000000-0000-4000-8000-000000000001', 'b1000004-0000-4000-8000-000000000002', 'Enviar propuesta Banco Regional', 'Deal en negociacion', 'Open', 'd1000001-0000-4000-8000-000000000001', 'Deal', 'b1000001-0000-4000-8000-000000000004', NOW() AT TIME ZONE 'UTC' + INTERVAL '5 days', 'High', NULL, NOW() AT TIME ZONE 'UTC', NULL, 'vps-test-seed', NULL),
('e2000001-0000-4000-8000-000000000003', 'b1000000-0000-4000-8000-000000000001', 'b1000004-0000-4000-8000-000000000002', 'Revision pipeline semanal', 'Manager review', 'Completed', 'd1000001-0000-4000-8000-000000000002', 'Deal', 'b1000001-0000-4000-8000-000000000003', NOW() AT TIME ZONE 'UTC' - INTERVAL '1 day', 'Normal', NULL, NOW() AT TIME ZONE 'UTC' - INTERVAL '7 days', NULL, 'vps-test-seed', NULL),
('e2000001-0000-4000-8000-000000000004', 'b1000000-0000-4000-8000-000000000001', 'b1000004-0000-4000-8000-000000000001', 'Ticket CS — onboarding', 'Cliente nuevo requiere capacitacion', 'Open', 'c1000001-0000-4000-8000-000000000002', 'Customer', 'b1000001-0000-4000-8000-000000000006', NOW() AT TIME ZONE 'UTC' + INTERVAL '3 days', 'Normal', 'CS_Ticket', NOW() AT TIME ZONE 'UTC', NULL, 'vps-test-seed', NULL),
('e2000001-0000-4000-8000-000000000005', 'b1000000-0000-4000-8000-000000000001', 'b1000004-0000-4000-8000-000000000001', 'Ticket CS — incidencia API', 'Error integracion webhook', 'Open', 'c1000001-0000-4000-8000-000000000001', 'Customer', 'b1000001-0000-4000-8000-000000000006', NOW() AT TIME ZONE 'UTC' + INTERVAL '1 day', 'High', 'CS_Ticket', NOW() AT TIME ZONE 'UTC', NULL, 'vps-test-seed', NULL);

-- 3 casos Customer Success (tickets adicionales)
INSERT INTO "WorkflowTasks" (
    "Id", "TenantId", "WorkflowId", "Title", "Description", "Status",
    "RelatedEntityId", "RelatedEntityType", "AssignedToUserId", "DueDate",
    "Priority", "TaskType", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy"
) VALUES
('e2000002-0000-4000-8000-000000000001', 'b1000000-0000-4000-8000-000000000001', 'b1000004-0000-4000-8000-000000000001', 'CS — Renovacion contrato', 'Cliente VIP proximo a vencer', 'Open', 'c1000001-0000-4000-8000-000000000005', 'Customer', 'b1000001-0000-4000-8000-000000000006', NOW() AT TIME ZONE 'UTC' + INTERVAL '14 days', 'Normal', 'CS_Ticket', NOW() AT TIME ZONE 'UTC', NULL, 'vps-test-seed', NULL),
('e2000002-0000-4000-8000-000000000002', 'b1000000-0000-4000-8000-000000000001', 'b1000004-0000-4000-8000-000000000001', 'CS — Salud NPS bajo', 'Seguimiento satisfaccion', 'Open', 'c1000001-0000-4000-8000-000000000004', 'Customer', 'b1000001-0000-4000-8000-000000000006', NOW() AT TIME ZONE 'UTC' + INTERVAL '7 days', 'High', 'CS_Ticket', NOW() AT TIME ZONE 'UTC', NULL, 'vps-test-seed', NULL),
('e2000002-0000-4000-8000-000000000003', 'b1000000-0000-4000-8000-000000000001', 'b1000004-0000-4000-8000-000000000001', 'CS — Capacitacion usuarios', 'Sesion training Admin', 'Completed', 'c1000001-0000-4000-8000-000000000003', 'Customer', 'b1000001-0000-4000-8000-000000000006', NOW() AT TIME ZONE 'UTC' - INTERVAL '2 days', 'Normal', 'CS_Ticket', NOW() AT TIME ZONE 'UTC' - INTERVAL '10 days', NULL, 'vps-test-seed', NULL);

-- 2 registros billing-related (usage events)
INSERT INTO "ProductUsageEvents" (
    "Id", "TenantId", "CustomerId", "Module", "EventType", "SessionId", "Industry",
    "DurationMinutes", "RecordedAt", "UserId",
    "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy"
) VALUES
('b3000001-0000-4000-8000-000000000001', 'b1000000-0000-4000-8000-000000000001', 'c1000001-0000-4000-8000-000000000001', 'CRM', 'login', 'sess-001', 'Finance', 15, NOW() AT TIME ZONE 'UTC', 'b1000001-0000-4000-8000-000000000004', NOW() AT TIME ZONE 'UTC', NULL, 'vps-test-seed', NULL),
('b3000001-0000-4000-8000-000000000002', 'b1000000-0000-4000-8000-000000000001', 'c1000001-0000-4000-8000-000000000004', 'RevenueOS', 'dashboard_view', 'sess-002', 'Healthcare', 8, NOW() AT TIME ZONE 'UTC', 'b1000001-0000-4000-8000-000000000003', NOW() AT TIME ZONE 'UTC', NULL, 'vps-test-seed', NULL);

-- 3 eventos de auditoria (DomainEvents)
INSERT INTO "DomainEvents" (
    "Id", "TenantId", "AggregateId", "EventType", "EventData", "OccurredOn", "CorrelationId", "CreatedAt"
) VALUES
('e1000001-0000-4000-8000-000000000001', 'b1000000-0000-4000-8000-000000000001', 'd1000001-0000-4000-8000-000000000004', 'DealClosedWonEvent', '{"dealId":"d1000001-0000-4000-8000-000000000004","amount":55000}'::jsonb, NOW() AT TIME ZONE 'UTC' - INTERVAL '3 days', NULL, NOW() AT TIME ZONE 'UTC'),
('e1000001-0000-4000-8000-000000000002', 'b1000000-0000-4000-8000-000000000001', 'f1000001-0000-4000-8000-000000000001', 'LeadCreatedEvent', '{"leadId":"f1000001-0000-4000-8000-000000000001","source":"Website"}'::jsonb, NOW() AT TIME ZONE 'UTC' - INTERVAL '1 day', NULL, NOW() AT TIME ZONE 'UTC'),
('e1000001-0000-4000-8000-000000000003', 'b1000000-0000-4000-8000-000000000001', 'c1000001-0000-4000-8000-000000000001', 'CustomerCreatedEvent', '{"customerId":"c1000001-0000-4000-8000-000000000001"}'::jsonb, NOW() AT TIME ZONE 'UTC' - INTERVAL '30 days', NULL, NOW() AT TIME ZONE 'UTC');

-- 2 escenarios Trust Studio (1 pending HITL score>=70, 1 executed)
INSERT INTO "AiDecisionAudits" (
    "Id", "TenantId", "DecisionType", "Action", "DecisionScore", "Reason", "Evidence",
    "Status", "Outcome", "BusinessSucceeded", "BusinessRecordedAt", "BusinessOutcomeDetail",
    "CustomerId", "DealId", "UserId", "AgentName", "ExecutedAt",
    "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy"
) VALUES
(
    'a1000001-0000-4000-8000-000000000001',
    'b1000000-0000-4000-8000-000000000001',
    'DealStrategy', 'RecommendDiscount', 78,
    'Deal en negociacion — descuento 5% recomendado por agente',
    '{"confidence":0.78,"factor":"pipeline_velocity"}'::jsonb,
    'Pending', NULL, NULL, NULL, NULL,
    'c1000001-0000-4000-8000-000000000001', 'd1000001-0000-4000-8000-000000000001', 'b1000001-0000-4000-8000-000000000004',
    'DealStrategyAgent', NULL,
    NOW() AT TIME ZONE 'UTC', NULL, 'vps-test-seed', NULL
),
(
    'a1000001-0000-4000-8000-000000000002',
    'b1000000-0000-4000-8000-000000000001',
    'CustomerRisk', 'FlagChurnRisk', 82,
    'Cliente con senales de churn — revision support',
    '{"riskScore":0.82,"signals":["low_usage","ticket_open"]}'::jsonb,
    'Executed', 'Review scheduled', true, NOW() AT TIME ZONE 'UTC' - INTERVAL '1 day', 'Support ticket created',
    'c1000001-0000-4000-8000-000000000002', NULL, 'b1000001-0000-4000-8000-000000000006',
    'ChurnRiskAgent', NOW() AT TIME ZONE 'UTC' - INTERVAL '1 day',
    NOW() AT TIME ZONE 'UTC' - INTERVAL '2 days', NULL, 'vps-test-seed', NULL
);

COMMIT;

SELECT 'Customers' AS entity, COUNT(*)::text FROM "Customers" WHERE "TenantId" = 'b1000000-0000-4000-8000-000000000001'
UNION ALL SELECT 'Leads', COUNT(*)::text FROM "Leads" WHERE "TenantId" = 'b1000000-0000-4000-8000-000000000001'
UNION ALL SELECT 'Deals', COUNT(*)::text FROM "Deals" WHERE "TenantId" = 'b1000000-0000-4000-8000-000000000001'
UNION ALL SELECT 'WorkflowTasks', COUNT(*)::text FROM "WorkflowTasks" WHERE "TenantId" = 'b1000000-0000-4000-8000-000000000001'
UNION ALL SELECT 'AiDecisionAudits', COUNT(*)::text FROM "AiDecisionAudits" WHERE "TenantId" = 'b1000000-0000-4000-8000-000000000001'
UNION ALL SELECT 'DomainEvents', COUNT(*)::text FROM "DomainEvents" WHERE "TenantId" = 'b1000000-0000-4000-8000-000000000001';
