-- Global Manufacturing Group — sample ERP schema + data (SQL Server)
-- Adjust database name and run in SSMS / sqlcmd against a demo SQL Server instance.

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'gmg_erp') EXEC('CREATE SCHEMA gmg_erp');

IF OBJECT_ID('gmg_erp.pagos','U') IS NOT NULL DROP TABLE gmg_erp.pagos;
IF OBJECT_ID('gmg_erp.facturacion','U') IS NOT NULL DROP TABLE gmg_erp.facturacion;
IF OBJECT_ID('gmg_erp.cust_master','U') IS NOT NULL DROP TABLE gmg_erp.cust_master;
IF OBJECT_ID('gmg_erp.empresas','U') IS NOT NULL DROP TABLE gmg_erp.empresas;

CREATE TABLE gmg_erp.empresas (
    company_id BIGINT PRIMARY KEY,
    company_name NVARCHAR(200) NOT NULL,
    country_code CHAR(2) NOT NULL,
    industry NVARCHAR(80) NOT NULL DEFAULT 'Manufacturing');

CREATE TABLE gmg_erp.cust_master (
    customer_id BIGINT PRIMARY KEY,
    company_id BIGINT NOT NULL REFERENCES gmg_erp.empresas(company_id),
    customer_name NVARCHAR(200) NOT NULL,
    email NVARCHAR(255),
    phone NVARCHAR(50),
    segment NVARCHAR(40));

CREATE TABLE gmg_erp.facturacion (
    invoice_id BIGINT PRIMARY KEY,
    customer_id BIGINT NOT NULL REFERENCES gmg_erp.cust_master(customer_id),
    invoice_number NVARCHAR(40) NOT NULL,
    invoice_date DATE NOT NULL,
    total_amount DECIMAL(18,2) NOT NULL);

CREATE TABLE gmg_erp.pagos (
    payment_id BIGINT PRIMARY KEY,
    invoice_id BIGINT NOT NULL REFERENCES gmg_erp.facturacion(invoice_id),
    payment_ref NVARCHAR(60) NOT NULL,
    payment_date DATE NOT NULL,
    amount DECIMAL(18,2) NOT NULL);

;WITH n AS (SELECT TOP (5000) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) i FROM sys.all_objects a CROSS JOIN sys.all_objects b)
INSERT INTO gmg_erp.empresas SELECT (i % 200) + 1, CONCAT('GMG Division ', (i % 200) + 1), 'US', 'Manufacturing' FROM n GROUP BY (i % 200) + 1;

;WITH n AS (SELECT TOP (5000) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) i FROM sys.all_objects a CROSS JOIN sys.all_objects b)
INSERT INTO gmg_erp.cust_master SELECT i, (i % 200) + 1, CONCAT('GMG Customer ', i), CONCAT('cust', i, '@gmg.demo'), CONCAT('+1', i), 'SMB' FROM n;

;WITH n AS (SELECT TOP (50000) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) i FROM sys.all_objects a CROSS JOIN sys.all_objects b CROSS JOIN sys.all_objects c)
INSERT INTO gmg_erp.facturacion SELECT i, (i % 5000) + 1, CONCAT('INV-', i), DATEADD(day, i % 365, '20240101'), 1000 + (i % 100) * 10 FROM n;

;WITH n AS (SELECT TOP (200000) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) i FROM sys.all_objects a CROSS JOIN sys.all_objects b CROSS JOIN sys.all_objects c CROSS JOIN sys.all_objects d)
INSERT INTO gmg_erp.pagos SELECT i, (i % 50000) + 1, CONCAT('PAY-', i), DATEADD(day, i % 365, '20240101'), 250 + (i % 50) FROM n;
