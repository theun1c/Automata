-- 001_initial_schema.sql
-- Минимальная схема Automata (PostgreSQL)

BEGIN;

CREATE SCHEMA IF NOT EXISTS automata;
SET search_path TO automata, public;

CREATE TABLE IF NOT EXISTS roles (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS machine_statuses (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS machine_models (
    id BIGSERIAL PRIMARY KEY,
    brand VARCHAR(100) NOT NULL,
    model_name VARCHAR(100) NOT NULL,
    CONSTRAINT uq_machine_models UNIQUE (brand, model_name)
);

CREATE TABLE IF NOT EXISTS users (
    id BIGSERIAL PRIMARY KEY,
    last_name VARCHAR(100) NOT NULL,
    first_name VARCHAR(100) NOT NULL,
    middle_name VARCHAR(100) NULL,
    email VARCHAR(320) NOT NULL,
    phone VARCHAR(32) NULL,
    password_hash VARCHAR(255) NOT NULL,
    role_id BIGINT NOT NULL REFERENCES roles(id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_users_email UNIQUE (email),
    CONSTRAINT chk_users_email_format CHECK (email ~* '^[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}$')
);

CREATE TABLE IF NOT EXISTS vending_machines (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL UNIQUE,
    location TEXT NOT NULL,
    machine_model_id BIGINT NOT NULL REFERENCES machine_models(id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    status_id BIGINT NOT NULL REFERENCES machine_statuses(id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    installed_at DATE NOT NULL,
    last_service_at DATE NULL,
    total_income NUMERIC(14, 2) NOT NULL DEFAULT 0,
    CONSTRAINT chk_vending_machines_total_income_nonnegative CHECK (total_income >= 0)
);

CREATE TABLE IF NOT EXISTS products (
    id BIGSERIAL PRIMARY KEY,
    machine_id BIGINT NOT NULL REFERENCES vending_machines(id) ON UPDATE RESTRICT ON DELETE CASCADE,
    name VARCHAR(200) NOT NULL,
    description TEXT NULL,
    price NUMERIC(10, 2) NOT NULL,
    quantity INT NOT NULL DEFAULT 0,
    min_stock INT NOT NULL DEFAULT 0,
    avg_daily_sales NUMERIC(10, 2) NOT NULL DEFAULT 0,
    CONSTRAINT chk_products_price_positive CHECK (price > 0),
    CONSTRAINT chk_products_quantity_nonnegative CHECK (quantity >= 0),
    CONSTRAINT chk_products_min_stock_nonnegative CHECK (min_stock >= 0),
    CONSTRAINT chk_products_avg_daily_sales_nonnegative CHECK (avg_daily_sales >= 0)
);

CREATE TABLE IF NOT EXISTS sales (
    id BIGSERIAL PRIMARY KEY,
    machine_id BIGINT NOT NULL REFERENCES vending_machines(id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    product_id BIGINT NOT NULL REFERENCES products(id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    quantity INT NOT NULL,
    sale_amount NUMERIC(12, 2) NOT NULL,
    sale_datetime TIMESTAMPTZ NOT NULL,
    payment_method VARCHAR(50) NOT NULL,
    CONSTRAINT chk_sales_quantity_positive CHECK (quantity > 0),
    CONSTRAINT chk_sales_sale_amount_nonnegative CHECK (sale_amount >= 0)
);

CREATE TABLE IF NOT EXISTS maintenance_records (
    id BIGSERIAL PRIMARY KEY,
    machine_id BIGINT NOT NULL REFERENCES vending_machines(id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    user_id BIGINT NOT NULL REFERENCES users(id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    service_date TIMESTAMPTZ NOT NULL,
    work_description TEXT NOT NULL,
    issues TEXT NULL
);

COMMIT;
