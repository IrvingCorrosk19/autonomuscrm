-- Global Manufacturing Group — ERP demo schema (PostgreSQL primary volume database)
-- Run against the same database as AutonomusCRM (default: autonomuscrm)
-- psql -U postgres -d autonomuscrm -f 01_postgresql_gmg_erp_schema.sql
-- psql -U postgres -d autonomuscrm -f 02_postgresql_gmg_erp_data.sql

CREATE SCHEMA IF NOT EXISTS gmg_erp;

DROP TABLE IF EXISTS gmg_erp.pagos CASCADE;
DROP TABLE IF EXISTS gmg_erp.facturacion CASCADE;
DROP TABLE IF EXISTS gmg_erp.tbl_ventas CASCADE;
DROP TABLE IF EXISTS gmg_erp.activities CASCADE;
DROP TABLE IF EXISTS gmg_erp.crm_tasks CASCADE;
DROP TABLE IF EXISTS gmg_erp.crm_deals CASCADE;
DROP TABLE IF EXISTS gmg_erp.customer_contacts CASCADE;
DROP TABLE IF EXISTS gmg_erp.cust_master CASCADE;
DROP TABLE IF EXISTS gmg_erp.products CASCADE;
DROP TABLE IF EXISTS gmg_erp.empresas CASCADE;

CREATE TABLE gmg_erp.empresas (
    company_id   BIGINT PRIMARY KEY,
    company_name VARCHAR(200) NOT NULL,
    country_code CHAR(2) NOT NULL,
    industry     VARCHAR(80) NOT NULL DEFAULT 'Manufacturing'
);

CREATE TABLE gmg_erp.cust_master (
    customer_id   BIGINT PRIMARY KEY,
    company_id    BIGINT NOT NULL REFERENCES gmg_erp.empresas(company_id),
    customer_name VARCHAR(200) NOT NULL,
    email         VARCHAR(255),
    phone         VARCHAR(50),
    segment       VARCHAR(40),
    modified_at   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE gmg_erp.customer_contacts (
    contact_id   BIGINT PRIMARY KEY,
    customer_id  BIGINT NOT NULL REFERENCES gmg_erp.cust_master(customer_id),
    full_name    VARCHAR(200) NOT NULL,
    email        VARCHAR(255),
    phone        VARCHAR(50),
    title        VARCHAR(120)
);

CREATE TABLE gmg_erp.products (
    product_id   BIGINT PRIMARY KEY,
    sku          VARCHAR(40) NOT NULL,
    product_name VARCHAR(200) NOT NULL,
    category     VARCHAR(80) NOT NULL,
    unit_price   NUMERIC(18,2) NOT NULL
);

CREATE TABLE gmg_erp.tbl_ventas (
    sale_id      BIGINT PRIMARY KEY,
    customer_id  BIGINT NOT NULL REFERENCES gmg_erp.cust_master(customer_id),
    product_id   BIGINT NOT NULL REFERENCES gmg_erp.products(product_id),
    sale_date    DATE NOT NULL,
    quantity     INT NOT NULL,
    amount       NUMERIC(18,2) NOT NULL
);

CREATE TABLE gmg_erp.facturacion (
    invoice_id     BIGINT PRIMARY KEY,
    customer_id    BIGINT NOT NULL REFERENCES gmg_erp.cust_master(customer_id),
    invoice_number VARCHAR(40) NOT NULL,
    invoice_date   DATE NOT NULL,
    due_date       DATE NOT NULL,
    total_amount   NUMERIC(18,2) NOT NULL,
    status         VARCHAR(20) NOT NULL DEFAULT 'Open'
);

CREATE TABLE gmg_erp.pagos (
    payment_id     BIGINT PRIMARY KEY,
    invoice_id     BIGINT NOT NULL REFERENCES gmg_erp.facturacion(invoice_id),
    payment_ref    VARCHAR(60) NOT NULL,
    payment_date   DATE NOT NULL,
    amount         NUMERIC(18,2) NOT NULL,
    method         VARCHAR(30) NOT NULL DEFAULT 'Wire'
);

CREATE TABLE gmg_erp.activities (
    activity_id   BIGINT PRIMARY KEY,
    contact_id    BIGINT NOT NULL REFERENCES gmg_erp.customer_contacts(contact_id),
    activity_type VARCHAR(40) NOT NULL,
    subject       VARCHAR(200) NOT NULL,
    activity_date TIMESTAMPTZ NOT NULL
);

CREATE TABLE gmg_erp.crm_deals (
    deal_id       BIGINT PRIMARY KEY,
    customer_id   BIGINT NOT NULL REFERENCES gmg_erp.cust_master(customer_id),
    title         VARCHAR(200) NOT NULL,
    stage         VARCHAR(40) NOT NULL,
    amount        NUMERIC(18,2) NOT NULL,
    expected_close DATE
);

CREATE TABLE gmg_erp.crm_tasks (
    task_id       BIGINT PRIMARY KEY,
    deal_id       BIGINT REFERENCES gmg_erp.crm_deals(deal_id),
    title         VARCHAR(200) NOT NULL,
    status        VARCHAR(30) NOT NULL,
    due_date      DATE
);

CREATE INDEX ix_cust_master_company ON gmg_erp.cust_master(company_id);
CREATE INDEX ix_contacts_customer ON gmg_erp.customer_contacts(customer_id);
CREATE INDEX ix_facturacion_customer ON gmg_erp.facturacion(customer_id);
CREATE INDEX ix_pagos_invoice ON gmg_erp.pagos(invoice_id);
CREATE INDEX ix_ventas_customer ON gmg_erp.tbl_ventas(customer_id);

COMMENT ON SCHEMA gmg_erp IS 'Global Manufacturing Group ERP demo — DIP discovery target';
