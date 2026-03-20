-- 001_initial_schema.sql
-- Упрощённая схема Automata (PostgreSQL) по ТЗ «ЗАдание УП01 2026»

BEGIN;

CREATE SCHEMA IF NOT EXISTS automata;
SET search_path TO automata, public;

-- =========================
-- Справочники
-- =========================

CREATE TABLE IF NOT EXISTS roles (
    id BIGSERIAL PRIMARY KEY,
    code VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(100) NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS machine_statuses (
    id BIGSERIAL PRIMARY KEY,
    code VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(100) NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS payment_methods (
    id BIGSERIAL PRIMARY KEY,
    code VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(100) NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS machine_types (
    id BIGSERIAL PRIMARY KEY,
    code VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(100) NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS connection_types (
    id BIGSERIAL PRIMARY KEY,
    code VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(100) NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS equipment_types (
    id BIGSERIAL PRIMARY KEY,
    code VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(100) NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS providers (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(200) NOT NULL UNIQUE
);

-- =========================
-- Компании и пользователи
-- =========================

CREATE TABLE IF NOT EXISTS companies (
    id BIGSERIAL PRIMARY KEY,
    parent_company_id BIGINT NULL REFERENCES companies(id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    name VARCHAR(255) NOT NULL,
    address TEXT NOT NULL,
    phone VARCHAR(32) NULL,
    email VARCHAR(320) NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_companies_name UNIQUE (name),
    CONSTRAINT chk_companies_not_self_parent CHECK (parent_company_id IS NULL OR parent_company_id <> id),
    CONSTRAINT chk_companies_email_format CHECK (
        email IS NULL OR email ~* '^[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}$'
    )
);

CREATE TABLE IF NOT EXISTS users (
    id BIGSERIAL PRIMARY KEY,
    company_id BIGINT NULL REFERENCES companies(id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    role_id BIGINT NOT NULL REFERENCES roles(id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    last_name VARCHAR(100) NOT NULL,
    first_name VARCHAR(100) NOT NULL,
    middle_name VARCHAR(100) NULL,
    email VARCHAR(320) NOT NULL,
    phone VARCHAR(32) NULL,
    password_hash VARCHAR(255) NOT NULL,
    photo_path TEXT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT chk_users_email_format CHECK (email ~* '^[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}$')
);

CREATE UNIQUE INDEX IF NOT EXISTS uq_users_email_lower ON users (LOWER(email));

CREATE TABLE IF NOT EXISTS franchise_codes (
    id BIGSERIAL PRIMARY KEY,
    company_id BIGINT NOT NULL REFERENCES companies(id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    code_hash VARCHAR(255) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    expires_at TIMESTAMPTZ NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS email_verification_codes (
    id BIGSERIAL PRIMARY KEY,
    email VARCHAR(320) NOT NULL,
    code VARCHAR(12) NOT NULL,
    expires_at TIMESTAMPTZ NOT NULL,
    used_at TIMESTAMPTZ NULL,
    attempts INT NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT chk_email_verification_codes_email_format CHECK (
        email ~* '^[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}$'
    ),
    CONSTRAINT chk_email_verification_codes_attempts_nonnegative CHECK (attempts >= 0),
    CONSTRAINT chk_email_verification_codes_format CHECK (code ~ '^[0-9A-Za-z]{4,12}$')
);

CREATE INDEX IF NOT EXISTS ix_email_verification_codes_email_created_at
    ON email_verification_codes (LOWER(email), created_at DESC);

-- =========================
-- Торговые автоматы
-- =========================

CREATE TABLE IF NOT EXISTS machine_models (
    id BIGSERIAL PRIMARY KEY,
    machine_type_id BIGINT NOT NULL REFERENCES machine_types(id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    brand VARCHAR(100) NOT NULL,
    model_name VARCHAR(100) NOT NULL,
    CONSTRAINT uq_machine_models UNIQUE (machine_type_id, brand, model_name)
);

CREATE TABLE IF NOT EXISTS modems (
    id BIGSERIAL PRIMARY KEY,
    provider_id BIGINT NOT NULL REFERENCES providers(id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    connection_type_id BIGINT NOT NULL REFERENCES connection_types(id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    serial_number VARCHAR(100) NOT NULL UNIQUE,
    phone_number VARCHAR(32) NULL,
    installed_at DATE NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS vending_machines (
    id BIGSERIAL PRIMARY KEY,
    company_id BIGINT NOT NULL REFERENCES companies(id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    machine_model_id BIGINT NOT NULL REFERENCES machine_models(id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    status_id BIGINT NOT NULL REFERENCES machine_statuses(id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    name VARCHAR(255) NOT NULL,
    location_text TEXT NOT NULL,
    installed_at DATE NOT NULL,
    last_service_at DATE NULL,
    total_income NUMERIC(14, 2) NOT NULL DEFAULT 0,
    modem_id BIGINT NULL REFERENCES modems(id) ON UPDATE RESTRICT ON DELETE SET NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_vending_machines_name UNIQUE (name),
    CONSTRAINT chk_vending_machines_total_income_nonnegative CHECK (total_income >= 0)
);

CREATE UNIQUE INDEX IF NOT EXISTS uq_vending_machines_modem_id_not_null
    ON vending_machines (modem_id)
    WHERE modem_id IS NOT NULL;

CREATE INDEX IF NOT EXISTS ix_vending_machines_company_id ON vending_machines (company_id);
CREATE INDEX IF NOT EXISTS ix_vending_machines_status_id ON vending_machines (status_id);
CREATE INDEX IF NOT EXISTS ix_vending_machines_name ON vending_machines (name);

CREATE TABLE IF NOT EXISTS machine_payment_methods (
    machine_id BIGINT NOT NULL REFERENCES vending_machines(id) ON UPDATE RESTRICT ON DELETE CASCADE,
    payment_method_id BIGINT NOT NULL REFERENCES payment_methods(id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    PRIMARY KEY (machine_id, payment_method_id)
);

-- =========================
-- Товары, остатки, продажи
-- =========================

CREATE TABLE IF NOT EXISTS products (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(200) NOT NULL UNIQUE,
    description TEXT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS machine_products (
    machine_id BIGINT NOT NULL REFERENCES vending_machines(id) ON UPDATE RESTRICT ON DELETE CASCADE,
    product_id BIGINT NOT NULL REFERENCES products(id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    price NUMERIC(10, 2) NOT NULL,
    quantity INT NOT NULL DEFAULT 0,
    min_stock INT NOT NULL DEFAULT 0,
    avg_daily_sales NUMERIC(10, 2) NOT NULL DEFAULT 0,
    PRIMARY KEY (machine_id, product_id),
    CONSTRAINT chk_machine_products_price_positive CHECK (price > 0),
    CONSTRAINT chk_machine_products_quantity_nonnegative CHECK (quantity >= 0),
    CONSTRAINT chk_machine_products_min_stock_nonnegative CHECK (min_stock >= 0),
    CONSTRAINT chk_machine_products_avg_daily_sales_nonnegative CHECK (avg_daily_sales >= 0)
);

CREATE TABLE IF NOT EXISTS sales (
    id BIGSERIAL PRIMARY KEY,
    machine_id BIGINT NOT NULL REFERENCES vending_machines(id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    product_id BIGINT NOT NULL REFERENCES products(id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    payment_method_id BIGINT NOT NULL REFERENCES payment_methods(id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    quantity INT NOT NULL,
    total_amount NUMERIC(12, 2) NOT NULL,
    sold_at TIMESTAMPTZ NOT NULL,
    CONSTRAINT chk_sales_quantity_positive CHECK (quantity > 0),
    CONSTRAINT chk_sales_total_amount_nonnegative CHECK (total_amount >= 0)
);

CREATE INDEX IF NOT EXISTS ix_sales_machine_id_sold_at ON sales (machine_id, sold_at DESC);
CREATE INDEX IF NOT EXISTS ix_sales_product_id ON sales (product_id);

-- =========================
-- Обслуживание и мониторинг
-- =========================

CREATE TABLE IF NOT EXISTS maintenance_records (
    id BIGSERIAL PRIMARY KEY,
    machine_id BIGINT NOT NULL REFERENCES vending_machines(id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    performer_user_id BIGINT NULL REFERENCES users(id) ON UPDATE RESTRICT ON DELETE SET NULL,
    performer_name VARCHAR(255) NOT NULL,
    serviced_at TIMESTAMPTZ NOT NULL,
    work_description TEXT NOT NULL,
    issues TEXT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_maintenance_records_machine_id_serviced_at
    ON maintenance_records (machine_id, serviced_at DESC);

CREATE TABLE IF NOT EXISTS machine_events (
    id BIGSERIAL PRIMARY KEY,
    machine_id BIGINT NOT NULL REFERENCES vending_machines(id) ON UPDATE RESTRICT ON DELETE CASCADE,
    event_type VARCHAR(50) NOT NULL,
    message TEXT NOT NULL,
    occurred_at TIMESTAMPTZ NOT NULL,
    CONSTRAINT chk_machine_events_type CHECK (event_type IN ('info', 'warning', 'error', 'service'))
);

CREATE INDEX IF NOT EXISTS ix_machine_events_machine_id_occurred_at
    ON machine_events (machine_id, occurred_at DESC);

CREATE TABLE IF NOT EXISTS machine_equipment (
    machine_id BIGINT NOT NULL REFERENCES vending_machines(id) ON UPDATE RESTRICT ON DELETE CASCADE,
    equipment_type_id BIGINT NOT NULL REFERENCES equipment_types(id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    status_code VARCHAR(20) NOT NULL,
    notes TEXT NULL,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    PRIMARY KEY (machine_id, equipment_type_id),
    CONSTRAINT chk_machine_equipment_status_code CHECK (status_code IN ('ok', 'warning', 'fault'))
);

CREATE TABLE IF NOT EXISTS news_items (
    id BIGSERIAL PRIMARY KEY,
    title VARCHAR(255) NOT NULL,
    body TEXT NOT NULL,
    published_at TIMESTAMPTZ NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);

COMMIT;
