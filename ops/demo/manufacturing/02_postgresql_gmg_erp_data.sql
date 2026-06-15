-- Global Manufacturing Group — ERP demo data (PostgreSQL full volume)
-- Target: 50k customers, 500k invoices, 2M payments (+ products, contacts, activities, deals, tasks)
-- Runtime: ~2-8 minutes depending on hardware. Run after 01_postgresql_gmg_erp_schema.sql

SET client_min_messages TO WARNING;

INSERT INTO gmg_erp.empresas (company_id, company_name, country_code, industry)
SELECT i, 'GMG Division ' || i, CASE i % 4 WHEN 0 THEN 'US' WHEN 1 THEN 'DE' WHEN 2 THEN 'JP' ELSE 'MX' END, 'Manufacturing'
FROM generate_series(1, 200) i;

INSERT INTO gmg_erp.products (product_id, sku, product_name, category, unit_price)
SELECT i,
       'SKU-' || lpad(i::text, 5, '0'),
       'Industrial Component ' || i,
       CASE i % 5 WHEN 0 THEN 'Heavy Machinery' WHEN 1 THEN 'Electronics' WHEN 2 THEN 'Hydraulics' WHEN 3 THEN 'Safety' ELSE 'Services' END,
       (500 + (i % 120) * 75)::numeric
FROM generate_series(1, 320) i;

INSERT INTO gmg_erp.cust_master (customer_id, company_id, customer_name, email, phone, segment, modified_at)
SELECT i,
       (i % 200) + 1,
       'GMG Customer ' || i,
       'erp.customer' || i || '@gmg-erp.demo',
       '+1800' || lpad(i::text, 7, '0'),
       CASE WHEN i % 10 = 0 THEN 'Enterprise' WHEN i % 3 = 0 THEN 'Mid-Market' ELSE 'SMB' END,
       NOW() - ((i % 400) || ' days')::interval
FROM generate_series(1, 50000) i;

INSERT INTO gmg_erp.customer_contacts (contact_id, customer_id, full_name, email, phone, title)
SELECT i,
       ((i - 1) % 50000) + 1,
       'Contact ' || i,
       'contact' || i || '@gmg-erp.demo',
       '+1811' || lpad(i::text, 7, '0'),
       CASE i % 4 WHEN 0 THEN 'Buyer' WHEN 1 THEN 'Plant Manager' WHEN 2 THEN 'CFO' ELSE 'Engineer' END
FROM generate_series(1, 75000) i;

INSERT INTO gmg_erp.facturacion (invoice_id, customer_id, invoice_number, invoice_date, due_date, total_amount, status)
SELECT i,
       ((i - 1) % 50000) + 1,
       'INV-' || lpad(i::text, 8, '0'),
       DATE '2023-01-01' + ((i % 900) || ' days')::interval,
       DATE '2023-01-01' + ((i % 900 + 30) || ' days')::interval,
       (1200 + (i % 250) * 35)::numeric,
       CASE WHEN i % 17 = 0 THEN 'Overdue' WHEN i % 5 = 0 THEN 'Paid' ELSE 'Open' END
FROM generate_series(1, 500000) i;

INSERT INTO gmg_erp.pagos (payment_id, invoice_id, payment_ref, payment_date, amount, method)
SELECT i,
       ((i - 1) % 500000) + 1,
       'PAY-' || lpad(i::text, 9, '0'),
       DATE '2023-01-01' + ((i % 950) || ' days')::interval,
       (200 + (i % 80) * 12)::numeric,
       CASE i % 3 WHEN 0 THEN 'Wire' WHEN 1 THEN 'ACH' ELSE 'Card' END
FROM generate_series(1, 2000000) i;

INSERT INTO gmg_erp.tbl_ventas (sale_id, customer_id, product_id, sale_date, quantity, amount)
SELECT i,
       ((i - 1) % 50000) + 1,
       ((i - 1) % 320) + 1,
       DATE '2024-01-01' + ((i % 500) || ' days')::interval,
       1 + (i % 12),
       (900 + (i % 60) * 40)::numeric
FROM generate_series(1, 120000) i;

INSERT INTO gmg_erp.activities (activity_id, contact_id, activity_type, subject, activity_date)
SELECT i,
       ((i - 1) % 75000) + 1,
       CASE i % 4 WHEN 0 THEN 'Call' WHEN 1 THEN 'Visit' WHEN 2 THEN 'Email' ELSE 'Demo' END,
       'Activity ' || i,
       NOW() - ((i % 365) || ' days')::interval
FROM generate_series(1, 15000) i;

INSERT INTO gmg_erp.crm_deals (deal_id, customer_id, title, stage, amount, expected_close)
SELECT i,
       ((i - 1) % 50000) + 1,
       'Deal ' || i,
       CASE i % 5 WHEN 0 THEN 'Closed Won' WHEN 1 THEN 'Proposal' WHEN 2 THEN 'Negotiation' WHEN 3 THEN 'Qualification' ELSE 'Prospecting' END,
       (20000 + (i % 30) * 5000)::numeric,
       CURRENT_DATE + ((i % 90) || ' days')::interval
FROM generate_series(1, 2500) i;

INSERT INTO gmg_erp.crm_tasks (task_id, deal_id, title, status, due_date)
SELECT i,
       CASE WHEN i <= 2500 THEN i ELSE NULL END,
       CASE i % 3 WHEN 0 THEN 'Send proposal' WHEN 1 THEN 'Plant audit' ELSE 'Contract review' END,
       CASE WHEN i % 4 = 0 THEN 'Done' ELSE 'Open' END,
       CURRENT_DATE + ((i % 21) || ' days')::interval
FROM generate_series(1, 1000) i;

ANALYZE gmg_erp.empresas;
ANALYZE gmg_erp.cust_master;
ANALYZE gmg_erp.customer_contacts;
ANALYZE gmg_erp.products;
ANALYZE gmg_erp.facturacion;
ANALYZE gmg_erp.pagos;
ANALYZE gmg_erp.tbl_ventas;
ANALYZE gmg_erp.activities;
ANALYZE gmg_erp.crm_deals;
ANALYZE gmg_erp.crm_tasks;

SELECT 'gmg_erp seeded' AS status,
       (SELECT COUNT(*) FROM gmg_erp.cust_master) AS customers,
       (SELECT COUNT(*) FROM gmg_erp.facturacion) AS invoices,
       (SELECT COUNT(*) FROM gmg_erp.pagos) AS payments;
