-- 001_initial_schema.sql
-- Automata DB schema (PostgreSQL), based on docs/CODEX_SPEC.md

BEGIN;

CREATE SCHEMA IF NOT EXISTS automata;
SET search_path TO automata, public;

-- =========================
-- Dictionaries
-- =========================

CREATE TABLE IF NOT EXISTS roles (
    id BIGSERIAL PRIMARY KEY,
    code VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(100) NOT NULL
);

CREATE TABLE IF NOT EXISTS machine_statuses (
    id BIGSERIAL PRIMARY KEY,
    code VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(100) NOT NULL
);

CREATE TABLE IF NOT EXISTS payment_methods (
    id BIGSERIAL PRIMARY KEY,
    code VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(100) NOT NULL
);

CREATE TABLE IF NOT EXISTS service_priorities (
    id BIGSERIAL PRIMARY KEY,
    code VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(100) NOT NULL
);

CREATE TABLE IF NOT EXISTS providers (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(200) NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS manufacturers (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(200) NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS machine_models (
    id BIGSERIAL PRIMARY KEY,
    manufacturer_id BIGINT NOT NULL REFERENCES manufacturers(id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    name VARCHAR(200) NOT NULL,
    CONSTRAINT uq_machine_models UNIQUE (manufacturer_id, name)
);

CREATE TABLE IF NOT EXISTS timezones (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL UNIQUE,
    utc_offset VARCHAR(6) NOT NULL,
    CONSTRAINT chk_timezones_utc_offset_format CHECK (utc_offset ~ '^[+-][0-9]{2}:[0-9]{2}$')
);

CREATE TABLE IF NOT EXISTS connection_types (
    id BIGSERIAL PRIMARY KEY,
    code VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(100) NOT NULL
);

CREATE TABLE IF NOT EXISTS equipment_catalog (
    id BIGSERIAL PRIMARY KEY,
    code VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(200) NOT NULL
);

-- =========================
-- Companies and users
-- =========================

CREATE TABLE IF NOT EXISTS companies (
    id BIGSERIAL PRIMARY KEY,
    parent_company_id BIGINT NULL REFERENCES companies(id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    name VARCHAR(255) NOT NULL,
    address TEXT NOT NULL,
    contacts TEXT NOT NULL,
    notes TEXT NULL,
    active_from DATE NOT NULL DEFAULT CURRENT_DATE,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT chk_company_not_self_parent CHECK (parent_company_id IS NULL OR parent_company_id <> id)
);

CREATE TABLE IF NOT EXISTS users (
    id BIGSERIAL PRIMARY KEY,
    company_id BIGINT NULL REFERENCES companies(id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    role_id BIGINT NOT NULL REFERENCES roles(id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    full_name VARCHAR(255) NOT NULL,
    email VARCHAR(320) NOT NULL,
    phone VARCHAR(32) NULL,
    password_hash VARCHAR(255) NOT NULL,
    photo_path TEXT NULL,
    is_email_confirmed BOOLEAN NOT NULL DEFAULT FALSE,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT chk_users_email_format CHECK (email ~* '^[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}$')
);

CREATE UNIQUE INDEX IF NOT EXISTS uq_users_email_lower ON users (LOWER(email));

CREATE TABLE IF NOT EXISTS franchise_codes (
    id BIGSERIAL PRIMARY KEY,
    company_id BIGINT NULL REFERENCES companies(id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    code_hash VARCHAR(255) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    expires_at TIMESTAMPTZ NULL,
    created_by_user_id BIGINT NULL REFERENCES users(id) ON UPDATE RESTRICT ON DELETE SET NULL,
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
    CONSTRAINT chk_email_verification_attempts_nonnegative CHECK (attempts >= 0),
    CONSTRAINT chk_email_verification_code_format CHECK (code ~ '^[0-9A-Za-z]{4,12}$'),
    CONSTRAINT chk_email_verification_email_format CHECK (email ~* '^[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}$')
);

CREATE INDEX IF NOT EXISTS ix_email_verification_email_created_at
    ON email_verification_codes (LOWER(email), created_at DESC);

-- =========================
-- Vending domain
-- =========================

CREATE TABLE IF NOT EXISTS modems (
    id BIGSERIAL PRIMARY KEY,
    provider_id BIGINT NOT NULL REFERENCES providers(id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    serial_number VARCHAR(100) NOT NULL UNIQUE,
    connection_type_id BIGINT NULL REFERENCES connection_types(id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    last_ping_at TIMESTAMPTZ NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS product_matrices (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(200) NOT NULL UNIQUE,
    description TEXT NULL
);

CREATE TABLE IF NOT EXISTS critical_value_templates (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(200) NOT NULL UNIQUE,
    settings_json JSONB NOT NULL DEFAULT '{}'::jsonb
);

CREATE TABLE IF NOT EXISTS notification_templates (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(200) NOT NULL UNIQUE,
    settings_json JSONB NOT NULL DEFAULT '{}'::jsonb
);

CREATE TABLE IF NOT EXISTS rfid_cards (
    id BIGSERIAL PRIMARY KEY,
    card_type VARCHAR(20) NOT NULL,
    card_number VARCHAR(100) NOT NULL UNIQUE,
    CONSTRAINT chk_rfid_card_type CHECK (card_type IN ('service', 'collection', 'loading'))
);

CREATE TABLE IF NOT EXISTS vending_machines (
    id BIGSERIAL PRIMARY KEY,
    company_id BIGINT NOT NULL REFERENCES companies(id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    name VARCHAR(255) NOT NULL,
    manufacturer_id BIGINT NOT NULL REFERENCES manufacturers(id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    model_id BIGINT NOT NULL REFERENCES machine_models(id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    slave_manufacturer_id BIGINT NULL REFERENCES manufacturers(id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    slave_model_id BIGINT NULL REFERENCES machine_models(id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    work_mode VARCHAR(100) NOT NULL,
    status_id BIGINT NOT NULL REFERENCES machine_statuses(id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    address TEXT NOT NULL,
    place_description TEXT NOT NULL,
    latitude NUMERIC(9, 6) NULL,
    longitude NUMERIC(9, 6) NULL,
    machine_number VARCHAR(100) NOT NULL UNIQUE,
    work_time_text VARCHAR(32) NOT NULL,
    timezone_id BIGINT NOT NULL REFERENCES timezones(id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    product_matrix_id BIGINT NOT NULL REFERENCES product_matrices(id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    critical_value_template_id BIGINT NOT NULL REFERENCES critical_value_templates(id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    notification_template_id BIGINT NOT NULL REFERENCES notification_templates(id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    client_company_id BIGINT NULL REFERENCES companies(id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    manager_user_id BIGINT NULL REFERENCES users(id) ON UPDATE RESTRICT ON DELETE SET NULL,
    engineer_user_id BIGINT NULL REFERENCES users(id) ON UPDATE RESTRICT ON DELETE SET NULL,
    technician_user_id BIGINT NULL REFERENCES users(id) ON UPDATE RESTRICT ON DELETE SET NULL,
    rfid_service_card_id BIGINT NULL REFERENCES rfid_cards(id) ON UPDATE RESTRICT ON DELETE SET NULL,
    rfid_collection_card_id BIGINT NULL REFERENCES rfid_cards(id) ON UPDATE RESTRICT ON DELETE SET NULL,
    rfid_loading_card_id BIGINT NULL REFERENCES rfid_cards(id) ON UPDATE RESTRICT ON DELETE SET NULL,
    kit_online_cashbox_id VARCHAR(100) NULL,
    service_priority_id BIGINT NOT NULL REFERENCES service_priorities(id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    modem_id BIGINT NULL REFERENCES modems(id) ON UPDATE RESTRICT ON DELETE SET NULL,
    notes TEXT NULL,
    installed_at TIMESTAMPTZ NOT NULL,
    last_service_at TIMESTAMPTZ NULL,
    total_income NUMERIC(14, 2) NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT chk_vending_machine_latitude CHECK (latitude IS NULL OR (latitude >= -90 AND latitude <= 90)),
    CONSTRAINT chk_vending_machine_longitude CHECK (longitude IS NULL OR (longitude >= -180 AND longitude <= 180)),
    CONSTRAINT chk_vending_machine_total_income_nonnegative CHECK (total_income >= 0),
    CONSTRAINT chk_vending_machine_work_time_format CHECK (work_time_text ~ '^[0-2][0-9]:[0-5][0-9]-[0-2][0-9]:[0-5][0-9]$')
);

CREATE UNIQUE INDEX IF NOT EXISTS uq_vending_machine_modem_id_not_null
    ON vending_machines (modem_id)
    WHERE modem_id IS NOT NULL;

CREATE INDEX IF NOT EXISTS ix_vending_machines_name ON vending_machines (name);
CREATE INDEX IF NOT EXISTS ix_vending_machines_company_id ON vending_machines (company_id);
CREATE INDEX IF NOT EXISTS ix_vending_machines_status_id ON vending_machines (status_id);

CREATE TABLE IF NOT EXISTS machine_payment_systems (
    machine_id BIGINT NOT NULL REFERENCES vending_machines(id) ON UPDATE RESTRICT ON DELETE CASCADE,
    payment_system_code VARCHAR(50) NOT NULL,
    PRIMARY KEY (machine_id, payment_system_code),
    CONSTRAINT chk_machine_payment_system_code CHECK (
        payment_system_code IN ('coin_acceptor', 'bill_acceptor', 'cashless_module', 'qr_payment')
    )
);

CREATE TABLE IF NOT EXISTS products (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(200) NOT NULL UNIQUE,
    description TEXT NULL,
    price NUMERIC(10, 2) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT chk_products_price_positive CHECK (price > 0)
);

CREATE TABLE IF NOT EXISTS machine_inventory (
    machine_id BIGINT NOT NULL REFERENCES vending_machines(id) ON UPDATE RESTRICT ON DELETE CASCADE,
    product_id BIGINT NOT NULL REFERENCES products(id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    quantity INT NOT NULL DEFAULT 0,
    min_stock INT NOT NULL DEFAULT 0,
    avg_daily_sales NUMERIC(10, 2) NOT NULL DEFAULT 0,
    PRIMARY KEY (machine_id, product_id),
    CONSTRAINT chk_machine_inventory_quantity_nonnegative CHECK (quantity >= 0),
    CONSTRAINT chk_machine_inventory_min_stock_nonnegative CHECK (min_stock >= 0),
    CONSTRAINT chk_machine_inventory_avg_daily_sales_nonnegative CHECK (avg_daily_sales >= 0)
);

CREATE TABLE IF NOT EXISTS sales (
    id BIGSERIAL PRIMARY KEY,
    machine_id BIGINT NOT NULL REFERENCES vending_machines(id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    product_id BIGINT NOT NULL REFERENCES products(id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    quantity INT NOT NULL,
    sale_amount NUMERIC(12, 2) NOT NULL,
    sale_datetime TIMESTAMPTZ NOT NULL,
    payment_method_id BIGINT NOT NULL REFERENCES payment_methods(id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    CONSTRAINT chk_sales_quantity_positive CHECK (quantity > 0),
    CONSTRAINT chk_sales_amount_positive CHECK (sale_amount > 0)
);

CREATE INDEX IF NOT EXISTS ix_sales_machine_datetime ON sales (machine_id, sale_datetime DESC);
CREATE INDEX IF NOT EXISTS ix_sales_product_id ON sales (product_id);

CREATE TABLE IF NOT EXISTS maintenance_records (
    id BIGSERIAL PRIMARY KEY,
    machine_id BIGINT NOT NULL REFERENCES vending_machines(id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    service_datetime TIMESTAMPTZ NOT NULL,
    work_description TEXT NOT NULL,
    issues TEXT NULL,
    performer_user_id BIGINT NULL REFERENCES users(id) ON UPDATE RESTRICT ON DELETE SET NULL,
    performer_name_snapshot VARCHAR(255) NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_maintenance_machine_datetime
    ON maintenance_records (machine_id, service_datetime DESC);

CREATE TABLE IF NOT EXISTS machine_events (
    id BIGSERIAL PRIMARY KEY,
    machine_id BIGINT NOT NULL REFERENCES vending_machines(id) ON UPDATE RESTRICT ON DELETE CASCADE,
    event_type VARCHAR(100) NOT NULL,
    message TEXT NOT NULL,
    severity VARCHAR(20) NOT NULL,
    occurred_at TIMESTAMPTZ NOT NULL,
    CONSTRAINT chk_machine_events_severity CHECK (severity IN ('info', 'warning', 'critical'))
);

CREATE INDEX IF NOT EXISTS ix_machine_events_machine_time
    ON machine_events (machine_id, occurred_at DESC);

CREATE TABLE IF NOT EXISTS machine_equipment (
    machine_id BIGINT NOT NULL REFERENCES vending_machines(id) ON UPDATE RESTRICT ON DELETE CASCADE,
    equipment_id BIGINT NOT NULL REFERENCES equipment_catalog(id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    status_code VARCHAR(50) NOT NULL,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    PRIMARY KEY (machine_id, equipment_id)
);

CREATE TABLE IF NOT EXISTS news_items (
    id BIGSERIAL PRIMARY KEY,
    title VARCHAR(255) NOT NULL,
    body TEXT NOT NULL,
    published_at TIMESTAMPTZ NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);

-- =========================
-- Monitor snapshot (API layer support)
-- =========================

CREATE TABLE IF NOT EXISTS machine_monitor_snapshots (
    machine_id BIGINT PRIMARY KEY REFERENCES vending_machines(id) ON UPDATE RESTRICT ON DELETE CASCADE,
    connection_state VARCHAR(20) NOT NULL,
    connection_type_id BIGINT NULL REFERENCES connection_types(id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    total_load_percent INT NOT NULL,
    min_load_percent INT NOT NULL,
    cash_total NUMERIC(14, 2) NOT NULL DEFAULT 0,
    coin_sum NUMERIC(14, 2) NOT NULL DEFAULT 0,
    bill_sum NUMERIC(14, 2) NOT NULL DEFAULT 0,
    change_sum NUMERIC(14, 2) NOT NULL DEFAULT 0,
    last_ping_at TIMESTAMPTZ NULL,
    last_sale_at TIMESTAMPTZ NULL,
    last_collection_at TIMESTAMPTZ NULL,
    last_service_at TIMESTAMPTZ NULL,
    sales_today_amount NUMERIC(14, 2) NOT NULL DEFAULT 0,
    sales_since_service_amount NUMERIC(14, 2) NOT NULL DEFAULT 0,
    sales_since_service_count INT NOT NULL DEFAULT 0,
    additional_statuses_json JSONB NOT NULL DEFAULT '[]'::jsonb,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT chk_monitor_connection_state CHECK (
        connection_state IN ('online', 'offline', 'warning', 'service')
    ),
    CONSTRAINT chk_monitor_total_load_range CHECK (total_load_percent BETWEEN 0 AND 100),
    CONSTRAINT chk_monitor_min_load_range CHECK (min_load_percent BETWEEN 0 AND 100),
    CONSTRAINT chk_monitor_amounts_nonnegative CHECK (
        cash_total >= 0 AND coin_sum >= 0 AND bill_sum >= 0 AND change_sum >= 0
    ),
    CONSTRAINT chk_monitor_sales_nonnegative CHECK (
        sales_today_amount >= 0 AND sales_since_service_amount >= 0 AND sales_since_service_count >= 0
    )
);

CREATE INDEX IF NOT EXISTS ix_monitor_connection_state
    ON machine_monitor_snapshots (connection_state);
CREATE INDEX IF NOT EXISTS ix_monitor_updated_at
    ON machine_monitor_snapshots (updated_at DESC);

COMMIT;
