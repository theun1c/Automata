-- 001_seed_core.sql
-- Base dictionaries + demo data for Automata

BEGIN;

SET search_path TO automata, public;

-- =========================
-- Dictionaries
-- =========================

INSERT INTO roles (id, code, name) VALUES
    (1, 'admin', 'Администратор'),
    (2, 'operator', 'Оператор'),
    (3, 'engineer', 'Инженер'),
    (4, 'technician', 'Техник-оператор')
ON CONFLICT (id) DO NOTHING;

INSERT INTO machine_statuses (id, code, name) VALUES
    (1, 'working', 'Работает'),
    (2, 'stopped', 'Не работает'),
    (3, 'service', 'На обслуживании')
ON CONFLICT (id) DO NOTHING;

INSERT INTO payment_methods (id, code, name) VALUES
    (1, 'cash', 'Наличные'),
    (2, 'card', 'Карта'),
    (3, 'qr', 'QR')
ON CONFLICT (id) DO NOTHING;

INSERT INTO service_priorities (id, code, name) VALUES
    (1, 'low', 'Низкий'),
    (2, 'normal', 'Обычный'),
    (3, 'high', 'Высокий')
ON CONFLICT (id) DO NOTHING;

INSERT INTO providers (id, name) VALUES
    (1, 'MegaTel'),
    (2, 'CloudMobile')
ON CONFLICT (id) DO NOTHING;

INSERT INTO manufacturers (id, name) VALUES
    (1, 'Necta'),
    (2, 'Bianchi'),
    (3, 'Saeco')
ON CONFLICT (id) DO NOTHING;

INSERT INTO machine_models (id, manufacturer_id, name) VALUES
    (1, 1, 'Kikko Max'),
    (2, 2, 'Lei 400'),
    (3, 3, 'Cristallo Evo')
ON CONFLICT (id) DO NOTHING;

INSERT INTO timezones (id, name, utc_offset) VALUES
    (1, 'Europe/Moscow', '+03:00'),
    (2, 'Europe/Samara', '+04:00'),
    (3, 'Asia/Yekaterinburg', '+05:00')
ON CONFLICT (id) DO NOTHING;

INSERT INTO connection_types (id, code, name) VALUES
    (1, 'mdb', 'MDB'),
    (2, 'gprs', 'GPRS'),
    (3, 'ethernet', 'Ethernet'),
    (4, 'wifi', 'Wi-Fi')
ON CONFLICT (id) DO NOTHING;

INSERT INTO equipment_catalog (id, code, name) VALUES
    (1, 'coin_acceptor', 'Монетоприемник'),
    (2, 'bill_acceptor', 'Купюроприемник'),
    (3, 'cashless_module', 'Модуль безналичной оплаты'),
    (4, 'qr_module', 'QR-модуль'),
    (5, 'temperature_sensor', 'Датчик температуры')
ON CONFLICT (id) DO NOTHING;

INSERT INTO product_matrices (id, name, description) VALUES
    (1, 'Стандарт', 'Базовый ассортимент напитков и снеков'),
    (2, 'Расширенный', 'Расширенный ассортимент с премиальными позициями')
ON CONFLICT (id) DO NOTHING;

INSERT INTO critical_value_templates (id, name, settings_json) VALUES
    (1, 'Default critical', '{"minStockPercent":20,"maxTemp":35}'::jsonb)
ON CONFLICT (id) DO NOTHING;

INSERT INTO notification_templates (id, name, settings_json) VALUES
    (1, 'Default notifications', '{"email":true,"desktop":true}'::jsonb)
ON CONFLICT (id) DO NOTHING;

-- =========================
-- Companies and users
-- =========================

INSERT INTO companies (
    id, parent_company_id, name, address, contacts, notes, active_from, is_active, is_deleted, created_at, updated_at
) VALUES
    (1, NULL, 'ООО Автоматика', 'г. Москва, ул. Примерная, 1', '+7 (495) 000-00-01', 'Головная компания', '2024-01-01', TRUE, FALSE, NOW(), NOW()),
    (2, 1, 'Франчайзи Север', 'г. Москва, ул. Северная, 5', '+7 (495) 000-00-02', NULL, '2024-03-15', TRUE, FALSE, NOW(), NOW()),
    (3, 1, 'Франчайзи Центр', 'г. Москва, ул. Центральная, 8', '+7 (495) 000-00-03', NULL, '2024-05-10', TRUE, FALSE, NOW(), NOW())
ON CONFLICT (id) DO NOTHING;

-- Password hashes only (demo values)
INSERT INTO users (
    id, company_id, role_id, full_name, email, phone, password_hash, photo_path,
    is_email_confirmed, is_active, created_at, updated_at
) VALUES
    (1, 1, 1, 'Иванов Иван Иванович', 'admin@automata.local', '+7 (900) 000-00-01', '$2b$12$jS0q5S6d2QpQkF3mWcE.8e1QY7wzjKJ8LQ6s36D5aUZ3eE9x2A8aC', NULL, TRUE, TRUE, NOW(), NOW()),
    (2, 2, 2, 'Петров Петр Петрович', 'operator@automata.local', '+7 (900) 000-00-02', '$2b$12$M8JmF7xUdHhQe9mR2zQ9vO9kz1q4R8f6W2vW0y1XoC7nP4xT1jL7m', NULL, TRUE, TRUE, NOW(), NOW()),
    (3, 3, 3, 'Сидоров Сергей Сергеевич', 'engineer@automata.local', '+7 (900) 000-00-03', '$2b$12$kP2vN3mF4dH7qR9sT1uVwO7yX5zA1bC2dE3fG4hI5jK6lM7nN8oPq', NULL, TRUE, TRUE, NOW(), NOW())
ON CONFLICT (id) DO NOTHING;

INSERT INTO franchise_codes (
    id, company_id, code_hash, is_active, expires_at, created_by_user_id, created_at
) VALUES
    (1, 2, '$2b$12$franchiseCodeHash000000000000000000000000000000000000000', TRUE, NOW() + INTERVAL '365 days', 1, NOW()),
    (2, 3, '$2b$12$franchiseCodeHash111111111111111111111111111111111111111', TRUE, NOW() + INTERVAL '365 days', 1, NOW())
ON CONFLICT (id) DO NOTHING;

INSERT INTO email_verification_codes (
    id, email, code, expires_at, used_at, attempts, created_at
) VALUES
    (1, 'newuser@example.com', 'A7X92C', NOW() + INTERVAL '10 minutes', NULL, 0, NOW())
ON CONFLICT (id) DO NOTHING;

-- =========================
-- Vending domain data
-- =========================

INSERT INTO modems (
    id, provider_id, serial_number, connection_type_id, is_active, last_ping_at, created_at
) VALUES
    (1, 1, 'MDM-0001', 2, TRUE, NOW() - INTERVAL '3 minutes', NOW()),
    (2, 2, 'MDM-0002', 1, TRUE, NOW() - INTERVAL '7 minutes', NOW())
ON CONFLICT (id) DO NOTHING;

INSERT INTO rfid_cards (id, card_type, card_number) VALUES
    (1, 'service', 'RF-SRV-0001'),
    (2, 'collection', 'RF-COL-0001'),
    (3, 'loading', 'RF-LOD-0001')
ON CONFLICT (id) DO NOTHING;

INSERT INTO vending_machines (
    id, company_id, name, manufacturer_id, model_id, slave_manufacturer_id, slave_model_id, work_mode, status_id,
    address, place_description, latitude, longitude, machine_number, work_time_text, timezone_id, product_matrix_id,
    critical_value_template_id, notification_template_id, client_company_id, manager_user_id, engineer_user_id, technician_user_id,
    rfid_service_card_id, rfid_collection_card_id, rfid_loading_card_id, kit_online_cashbox_id, service_priority_id, modem_id, notes,
    installed_at, last_service_at, total_income, is_active, is_deleted, created_at, updated_at
) VALUES
    (1, 2, 'ТА Север-01', 1, 1, NULL, NULL, 'normal', 1,
     'г. Москва, ул. Северная, 10', 'ТЦ Север, 1 этаж', 55.801200, 37.545000, 'VM-0001', '08:00-22:00', 1, 1,
     1, 1, NULL, 2, 3, NULL, 1, 2, 3, 'KIT-1001', 2, 1, 'Основной автомат',
     NOW() - INTERVAL '300 days', NOW() - INTERVAL '15 days', 152340.50, TRUE, FALSE, NOW(), NOW()),
    (2, 3, 'ТА Центр-01', 2, 2, NULL, NULL, 'normal', 3,
     'г. Москва, ул. Центральная, 20', 'БЦ Центр, холл', 55.752200, 37.615600, 'VM-0002', '09:00-21:00', 1, 2,
     1, 1, NULL, 2, 3, NULL, 1, 2, 3, 'KIT-1002', 3, 2, 'Требует обслуживания',
     NOW() - INTERVAL '180 days', NOW() - INTERVAL '2 days', 94321.10, TRUE, FALSE, NOW(), NOW()),
    (3, 2, 'ТА Север-02', 3, 3, NULL, NULL, 'normal', 2,
     'г. Москва, ул. Северная, 15', 'БЦ Север, 2 этаж', 55.810000, 37.530000, 'VM-0003', '00:00-23:59', 1, 1,
     1, 1, NULL, 2, 3, NULL, NULL, NULL, NULL, NULL, 1, NULL, 'Модем отвязан',
     NOW() - INTERVAL '90 days', NOW() - INTERVAL '30 days', 50210.00, TRUE, FALSE, NOW(), NOW())
ON CONFLICT (id) DO NOTHING;

INSERT INTO machine_payment_systems (machine_id, payment_system_code) VALUES
    (1, 'coin_acceptor'),
    (1, 'bill_acceptor'),
    (1, 'cashless_module'),
    (1, 'qr_payment'),
    (2, 'coin_acceptor'),
    (2, 'bill_acceptor'),
    (2, 'cashless_module'),
    (3, 'coin_acceptor')
ON CONFLICT (machine_id, payment_system_code) DO NOTHING;

INSERT INTO products (id, name, description, price, is_active) VALUES
    (1, 'Эспрессо', 'Кофе эспрессо', 120.00, TRUE),
    (2, 'Капучино', 'Кофе капучино', 150.00, TRUE),
    (3, 'Вода 0.5', 'Питьевая вода 0.5л', 80.00, TRUE),
    (4, 'Сэндвич', 'Сэндвич куриный', 210.00, TRUE)
ON CONFLICT (id) DO NOTHING;

INSERT INTO machine_inventory (machine_id, product_id, quantity, min_stock, avg_daily_sales) VALUES
    (1, 1, 25, 8, 11.5),
    (1, 2, 14, 8, 8.2),
    (1, 3, 40, 10, 6.0),
    (2, 1, 12, 8, 7.0),
    (2, 2, 5, 8, 4.5),
    (2, 4, 7, 5, 2.1),
    (3, 3, 30, 12, 5.0)
ON CONFLICT (machine_id, product_id) DO NOTHING;

INSERT INTO sales (id, machine_id, product_id, quantity, sale_amount, sale_datetime, payment_method_id) VALUES
    (1, 1, 1, 2, 240.00, NOW() - INTERVAL '3 hours', 2),
    (2, 1, 2, 1, 150.00, NOW() - INTERVAL '2 hours', 2),
    (3, 1, 3, 1, 80.00, NOW() - INTERVAL '90 minutes', 1),
    (4, 2, 1, 1, 120.00, NOW() - INTERVAL '4 hours', 1),
    (5, 2, 4, 1, 210.00, NOW() - INTERVAL '1 day', 3),
    (6, 3, 3, 2, 160.00, NOW() - INTERVAL '6 hours', 1)
ON CONFLICT (id) DO NOTHING;

INSERT INTO maintenance_records (
    id, machine_id, service_datetime, work_description, issues, performer_user_id, performer_name_snapshot, created_at
) VALUES
    (1, 1, NOW() - INTERVAL '15 days', 'Плановое ТО, очистка узлов', NULL, 3, 'Сидоров С.С.', NOW()),
    (2, 2, NOW() - INTERVAL '2 days', 'Замена датчика температуры', 'Перегрев купюроприемника', 3, 'Сидоров С.С.', NOW())
ON CONFLICT (id) DO NOTHING;

INSERT INTO machine_events (id, machine_id, event_type, message, severity, occurred_at) VALUES
    (1, 1, 'info', 'Сеанс инкассации завершен', 'info', NOW() - INTERVAL '12 hours'),
    (2, 2, 'alarm', 'Ошибка купюроприемника', 'critical', NOW() - INTERVAL '3 hours'),
    (3, 3, 'warning', 'Низкий остаток товара', 'warning', NOW() - INTERVAL '2 hours')
ON CONFLICT (id) DO NOTHING;

INSERT INTO machine_equipment (machine_id, equipment_id, status_code, updated_at) VALUES
    (1, 1, 'ok', NOW()),
    (1, 2, 'ok', NOW()),
    (1, 3, 'ok', NOW()),
    (1, 4, 'ok', NOW()),
    (2, 1, 'ok', NOW()),
    (2, 2, 'fault', NOW()),
    (2, 3, 'ok', NOW()),
    (3, 1, 'ok', NOW()),
    (3, 5, 'ok', NOW())
ON CONFLICT (machine_id, equipment_id) DO NOTHING;

INSERT INTO news_items (id, title, body, published_at, is_active) VALUES
    (1, 'Плановое обновление ПО', 'В ночь на пятницу будет выполнено обновление мониторинга.', NOW() - INTERVAL '2 days', TRUE),
    (2, 'Новый поставщик стаканов', 'Добавлен новый поставщик расходных материалов.', NOW() - INTERVAL '1 day', TRUE)
ON CONFLICT (id) DO NOTHING;

INSERT INTO machine_monitor_snapshots (
    machine_id, connection_state, connection_type_id, total_load_percent, min_load_percent,
    cash_total, coin_sum, bill_sum, change_sum, last_ping_at, last_sale_at, last_collection_at, last_service_at,
    sales_today_amount, sales_since_service_amount, sales_since_service_count, additional_statuses_json, updated_at
) VALUES
    (1, 'online', 1, 87, 24, 2459.00, 260.00, 760.00, 1439.00, NOW() - INTERVAL '3 minutes', NOW() - INTERVAL '90 minutes', NOW() - INTERVAL '1 day', NOW() - INTERVAL '15 days', 3200.00, 5400.00, 48, '["paper_low"]'::jsonb, NOW()),
    (2, 'warning', 2, 52, 8, 1380.00, 220.00, 500.00, 660.00, NOW() - INTERVAL '7 minutes', NOW() - INTERVAL '4 hours', NOW() - INTERVAL '2 days', NOW() - INTERVAL '2 days', 980.00, 2100.00, 19, '["door_alarm","service_required"]'::jsonb, NOW()),
    (3, 'offline', NULL, 34, 12, 640.00, 120.00, 220.00, 300.00, NOW() - INTERVAL '2 hours', NOW() - INTERVAL '6 hours', NOW() - INTERVAL '4 days', NOW() - INTERVAL '30 days', 540.00, 800.00, 7, '[]'::jsonb, NOW())
ON CONFLICT (machine_id) DO NOTHING;

-- =========================
-- Sync sequences
-- =========================

SELECT setval(pg_get_serial_sequence('roles', 'id'), COALESCE((SELECT MAX(id) FROM roles), 1), TRUE);
SELECT setval(pg_get_serial_sequence('machine_statuses', 'id'), COALESCE((SELECT MAX(id) FROM machine_statuses), 1), TRUE);
SELECT setval(pg_get_serial_sequence('payment_methods', 'id'), COALESCE((SELECT MAX(id) FROM payment_methods), 1), TRUE);
SELECT setval(pg_get_serial_sequence('service_priorities', 'id'), COALESCE((SELECT MAX(id) FROM service_priorities), 1), TRUE);
SELECT setval(pg_get_serial_sequence('providers', 'id'), COALESCE((SELECT MAX(id) FROM providers), 1), TRUE);
SELECT setval(pg_get_serial_sequence('manufacturers', 'id'), COALESCE((SELECT MAX(id) FROM manufacturers), 1), TRUE);
SELECT setval(pg_get_serial_sequence('machine_models', 'id'), COALESCE((SELECT MAX(id) FROM machine_models), 1), TRUE);
SELECT setval(pg_get_serial_sequence('timezones', 'id'), COALESCE((SELECT MAX(id) FROM timezones), 1), TRUE);
SELECT setval(pg_get_serial_sequence('connection_types', 'id'), COALESCE((SELECT MAX(id) FROM connection_types), 1), TRUE);
SELECT setval(pg_get_serial_sequence('equipment_catalog', 'id'), COALESCE((SELECT MAX(id) FROM equipment_catalog), 1), TRUE);
SELECT setval(pg_get_serial_sequence('companies', 'id'), COALESCE((SELECT MAX(id) FROM companies), 1), TRUE);
SELECT setval(pg_get_serial_sequence('users', 'id'), COALESCE((SELECT MAX(id) FROM users), 1), TRUE);
SELECT setval(pg_get_serial_sequence('franchise_codes', 'id'), COALESCE((SELECT MAX(id) FROM franchise_codes), 1), TRUE);
SELECT setval(pg_get_serial_sequence('email_verification_codes', 'id'), COALESCE((SELECT MAX(id) FROM email_verification_codes), 1), TRUE);
SELECT setval(pg_get_serial_sequence('modems', 'id'), COALESCE((SELECT MAX(id) FROM modems), 1), TRUE);
SELECT setval(pg_get_serial_sequence('product_matrices', 'id'), COALESCE((SELECT MAX(id) FROM product_matrices), 1), TRUE);
SELECT setval(pg_get_serial_sequence('critical_value_templates', 'id'), COALESCE((SELECT MAX(id) FROM critical_value_templates), 1), TRUE);
SELECT setval(pg_get_serial_sequence('notification_templates', 'id'), COALESCE((SELECT MAX(id) FROM notification_templates), 1), TRUE);
SELECT setval(pg_get_serial_sequence('rfid_cards', 'id'), COALESCE((SELECT MAX(id) FROM rfid_cards), 1), TRUE);
SELECT setval(pg_get_serial_sequence('vending_machines', 'id'), COALESCE((SELECT MAX(id) FROM vending_machines), 1), TRUE);
SELECT setval(pg_get_serial_sequence('products', 'id'), COALESCE((SELECT MAX(id) FROM products), 1), TRUE);
SELECT setval(pg_get_serial_sequence('sales', 'id'), COALESCE((SELECT MAX(id) FROM sales), 1), TRUE);
SELECT setval(pg_get_serial_sequence('maintenance_records', 'id'), COALESCE((SELECT MAX(id) FROM maintenance_records), 1), TRUE);
SELECT setval(pg_get_serial_sequence('machine_events', 'id'), COALESCE((SELECT MAX(id) FROM machine_events), 1), TRUE);
SELECT setval(pg_get_serial_sequence('news_items', 'id'), COALESCE((SELECT MAX(id) FROM news_items), 1), TRUE);

COMMIT;
