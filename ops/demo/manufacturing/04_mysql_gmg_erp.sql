-- Global Manufacturing Group — sample ERP (MySQL 8+)
CREATE DATABASE IF NOT EXISTS gmg_erp_demo CHARACTER SET utf8mb4;
USE gmg_erp_demo;

DROP TABLE IF EXISTS pagos;
DROP TABLE IF EXISTS facturacion;
DROP TABLE IF EXISTS cust_master;
DROP TABLE IF EXISTS empresas;

CREATE TABLE empresas (
    company_id BIGINT PRIMARY KEY,
    company_name VARCHAR(200) NOT NULL,
    country_code CHAR(2) NOT NULL,
    industry VARCHAR(80) NOT NULL DEFAULT 'Manufacturing'
) ENGINE=InnoDB;

CREATE TABLE cust_master (
    customer_id BIGINT PRIMARY KEY,
    company_id BIGINT NOT NULL,
    customer_name VARCHAR(200) NOT NULL,
    email VARCHAR(255),
    phone VARCHAR(50),
    segment VARCHAR(40),
    CONSTRAINT fk_cust_company FOREIGN KEY (company_id) REFERENCES empresas(company_id)
) ENGINE=InnoDB;

CREATE TABLE facturacion (
    invoice_id BIGINT PRIMARY KEY,
    customer_id BIGINT NOT NULL,
    invoice_number VARCHAR(40) NOT NULL,
    invoice_date DATE NOT NULL,
    total_amount DECIMAL(18,2) NOT NULL,
    CONSTRAINT fk_inv_customer FOREIGN KEY (customer_id) REFERENCES cust_master(customer_id)
) ENGINE=InnoDB;

CREATE TABLE pagos (
    payment_id BIGINT PRIMARY KEY,
    invoice_id BIGINT NOT NULL,
    payment_ref VARCHAR(60) NOT NULL,
    payment_date DATE NOT NULL,
    amount DECIMAL(18,2) NOT NULL,
    CONSTRAINT fk_pay_invoice FOREIGN KEY (invoice_id) REFERENCES facturacion(invoice_id)
) ENGINE=InnoDB;

INSERT INTO empresas
SELECT i, CONCAT('GMG Division ', i), 'US', 'Manufacturing'
FROM (WITH RECURSIVE seq AS (SELECT 1 n UNION ALL SELECT n+1 FROM seq WHERE n < 200) SELECT n i FROM seq) s;

INSERT INTO cust_master
SELECT i, (i MOD 200) + 1, CONCAT('GMG Customer ', i), CONCAT('cust', i, '@gmg.demo'), CONCAT('+1', i), 'SMB'
FROM (WITH RECURSIVE seq AS (SELECT 1 n UNION ALL SELECT n+1 FROM seq WHERE n < 5000) SELECT n i FROM seq) s;

INSERT INTO facturacion
SELECT i, (i MOD 5000) + 1, CONCAT('INV-', i), DATE_ADD('2024-01-01', INTERVAL (i MOD 365) DAY), 1000 + (i MOD 100) * 10
FROM (WITH RECURSIVE seq AS (SELECT 1 n UNION ALL SELECT n+1 FROM seq WHERE n < 50000) SELECT n i FROM seq) s;

INSERT INTO pagos
SELECT i, (i MOD 50000) + 1, CONCAT('PAY-', i), DATE_ADD('2024-01-01', INTERVAL (i MOD 365) DAY), 250 + (i MOD 50)
FROM (WITH RECURSIVE seq AS (SELECT 1 n UNION ALL SELECT n+1 FROM seq WHERE n < 200000) SELECT n i FROM seq) s;
