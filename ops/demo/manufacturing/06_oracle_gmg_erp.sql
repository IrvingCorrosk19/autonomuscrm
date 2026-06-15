-- Global Manufacturing Group — sample ERP (Oracle 19c+)
-- Run as demo user with CREATE TABLE privileges

BEGIN
   EXECUTE IMMEDIATE 'CREATE USER gmg_erp IDENTIFIED BY "GmgDemo2026!"';
EXCEPTION WHEN OTHERS THEN IF SQLCODE != -01920 THEN RAISE; END IF;
END;
/

ALTER USER gmg_erp QUOTA UNLIMITED ON USERS;

CREATE TABLE gmg_erp.empresas (
    company_id NUMBER(19) PRIMARY KEY,
    company_name VARCHAR2(200) NOT NULL,
    country_code CHAR(2) NOT NULL,
    industry VARCHAR2(80) DEFAULT 'Manufacturing' NOT NULL
);

CREATE TABLE gmg_erp.cust_master (
    customer_id NUMBER(19) PRIMARY KEY,
    company_id NUMBER(19) NOT NULL REFERENCES gmg_erp.empresas(company_id),
    customer_name VARCHAR2(200) NOT NULL,
    email VARCHAR2(255),
    phone VARCHAR2(50),
    segment VARCHAR2(40)
);

CREATE TABLE gmg_erp.facturacion (
    invoice_id NUMBER(19) PRIMARY KEY,
    customer_id NUMBER(19) NOT NULL REFERENCES gmg_erp.cust_master(customer_id),
    invoice_number VARCHAR2(40) NOT NULL,
    invoice_date DATE NOT NULL,
    total_amount NUMBER(18,2) NOT NULL
);

CREATE TABLE gmg_erp.pagos (
    payment_id NUMBER(19) PRIMARY KEY,
    invoice_id NUMBER(19) NOT NULL REFERENCES gmg_erp.facturacion(invoice_id),
    payment_ref VARCHAR2(60) NOT NULL,
    payment_date DATE NOT NULL,
    amount NUMBER(18,2) NOT NULL
);

INSERT INTO gmg_erp.empresas SELECT level, 'GMG Division ' || level, 'US', 'Manufacturing' FROM dual CONNECT BY level <= 200;
INSERT INTO gmg_erp.cust_master SELECT level, MOD(level-1,200)+1, 'GMG Customer ' || level, 'cust'||level||'@gmg.demo', '+1'||level, 'SMB' FROM dual CONNECT BY level <= 5000;
INSERT INTO gmg_erp.facturacion SELECT level, MOD(level-1,5000)+1, 'INV-'||level, DATE '2024-01-01' + MOD(level,365), 1000 + MOD(level,100)*10 FROM dual CONNECT BY level <= 50000;
INSERT INTO gmg_erp.pagos SELECT level, MOD(level-1,50000)+1, 'PAY-'||level, DATE '2024-01-01' + MOD(level,365), 250 + MOD(level,50) FROM dual CONNECT BY level <= 200000;
COMMIT;
