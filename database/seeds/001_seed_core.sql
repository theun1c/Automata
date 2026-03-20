-- 001_seed_core.sql
-- Стартовые данные для упрощённой схемы Automata

BEGIN;

SET search_path TO automata, public;

-- =========================
-- Справочники
-- =========================

INSERT INTO roles (id, code, name) VALUES
    (1, 'admin', 'Администратор'),
    (2, 'operator', 'Оператор')
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

INSERT INTO machine_types (id, code, name) VALUES
    (1, 'coffee', 'Кофейный автомат'),
    (2, 'snack', 'Снековый автомат')
ON CONFLICT (id) DO NOTHING;

INSERT INTO connection_types (id, code, name) VALUES
    (1, 'gsm', 'GSM'),
    (2, 'ethernet', 'Ethernet'),
    (3, 'wifi', 'Wi‑Fi')
ON CONFLICT (id) DO NOTHING;

INSERT INTO equipment_types (id, code, name) VALUES
    (1, 'bill_acceptor', 'Купюроприемник'),
    (2, 'card_reader', 'Терминал оплаты картой'),
    (3, 'coffee_unit', 'Кофейный модуль'),
    (4, 'cooling_unit', 'Холодильный модуль')
ON CONFLICT (id) DO NOTHING;

INSERT INTO providers (id, name) VALUES
    (1, 'MegaTel'),
    (2, 'CityNet')
ON CONFLICT (id) DO NOTHING;

-- =========================
-- Компании и пользователи
-- =========================

INSERT INTO companies (id, parent_company_id, name, address, phone, email, created_at, updated_at) VALUES
    (1, NULL, 'ООО Автомата', 'г. Москва, ул. Центральная, 1', '+7 (495) 100-10-10', 'info@automata.local', NOW(), NOW()),
    (2, 1, 'ООО Франчайзи Север', 'г. Москва, ул. Северная, 15', '+7 (495) 100-10-11', 'north@automata.local', NOW(), NOW()),
    (3, 1, 'ООО Франчайзи Центр', 'г. Москва, ул. Тверская, 7', '+7 (495) 100-10-12', 'center@automata.local', NOW(), NOW())
ON CONFLICT (id) DO NOTHING;

INSERT INTO users (
    id, company_id, role_id, last_name, first_name, middle_name, email, phone, password_hash, photo_path, is_active, created_at, updated_at
) VALUES
    (1, 1, 1, 'Иванов', 'Иван', 'Иванович', 'admin@automata.local', '+7 (900) 000-00-01', '$2b$12$jS0q5S6d2QpQkF3mWcE.8e1QY7wzjKJ8LQ6s36D5aUZ3eE9x2A8aC', NULL, TRUE, NOW(), NOW()),
    (2, 2, 2, 'Петров', 'Пётр', 'Петрович', 'operator@automata.local', '+7 (900) 000-00-02', '$2b$12$M8JmF7xUdHhQe9mR2zQ9vO9kz1q4R8f6W2vW0y1XoC7nP4xT1jL7m', NULL, TRUE, NOW(), NOW())
ON CONFLICT (id) DO NOTHING;

INSERT INTO franchise_codes (id, company_id, code_hash, is_active, expires_at, created_at) VALUES
    (1, 2, '$2b$12$northFranchiseCodeHash000000000000000000000000000000000', TRUE, NOW() + INTERVAL '365 days', NOW()),
    (2, 3, '$2b$12$centerFranchiseCodeHash11111111111111111111111111111111', TRUE, NOW() + INTERVAL '365 days', NOW())
ON CONFLICT (id) DO NOTHING;

INSERT INTO email_verification_codes (id, email, code, expires_at, used_at, attempts, created_at) VALUES
    (1, 'newuser@example.com', 'A7X92C', NOW() + INTERVAL '10 minutes', NULL, 0, NOW())
ON CONFLICT (id) DO NOTHING;

-- =========================
-- Торговые автоматы
-- =========================

INSERT INTO machine_models (id, machine_type_id, brand, model_name) VALUES
    (1, 1, 'Necta', 'Kikko Max'),
    (2, 2, 'Bianchi', 'Lei 400')
ON CONFLICT (id) DO NOTHING;

INSERT INTO modems (id, provider_id, connection_type_id, serial_number, phone_number, installed_at, created_at) VALUES
    (1, 1, 1, 'MDM-0001', '+79990000001', '2025-02-01', NOW()),
    (2, 2, 2, 'MDM-0002', '+79990000002', '2025-02-10', NOW())
ON CONFLICT (id) DO NOTHING;

INSERT INTO vending_machines (
    id, company_id, machine_model_id, status_id, name, location_text, installed_at, last_service_at, total_income, modem_id, created_at, updated_at
) VALUES
    (1, 2, 1, 1, 'ТА Север-01', 'г. Москва, ТЦ Север, 1 этаж', '2025-01-15', '2026-03-10', 152340.50, 1, NOW(), NOW()),
    (2, 3, 2, 3, 'ТА Центр-01', 'г. Москва, БЦ Центр, холл', '2025-02-10', '2026-03-18', 94321.10, 2, NOW(), NOW()),
    (3, 2, 1, 2, 'ТА Север-02', 'г. Москва, БЦ Север, 2 этаж', '2025-03-05', '2026-02-25', 50210.00, NULL, NOW(), NOW())
ON CONFLICT (id) DO NOTHING;

INSERT INTO machine_payment_methods (machine_id, payment_method_id) VALUES
    (1, 1), (1, 2), (1, 3),
    (2, 1), (2, 2),
    (3, 1)
ON CONFLICT (machine_id, payment_method_id) DO NOTHING;

-- =========================
-- Товары и продажи
-- =========================

INSERT INTO products (id, name, description, created_at) VALUES
    (1, 'Эспрессо', 'Кофе эспрессо', NOW()),
    (2, 'Капучино', 'Кофе капучино', NOW()),
    (3, 'Вода 0.5', 'Питьевая вода 0.5л', NOW()),
    (4, 'Сэндвич', 'Сэндвич куриный', NOW())
ON CONFLICT (id) DO NOTHING;

INSERT INTO machine_products (machine_id, product_id, price, quantity, min_stock, avg_daily_sales) VALUES
    (1, 1, 120.00, 25, 8, 11.5),
    (1, 2, 150.00, 14, 8, 8.2),
    (1, 3, 80.00, 40, 10, 6.0),
    (2, 1, 120.00, 12, 8, 7.0),
    (2, 4, 210.00, 7, 5, 2.1),
    (3, 3, 80.00, 30, 12, 5.0)
ON CONFLICT (machine_id, product_id) DO NOTHING;

INSERT INTO sales (id, machine_id, product_id, payment_method_id, quantity, total_amount, sold_at) VALUES
    (1, 1, 1, 2, 2, 240.00, NOW() - INTERVAL '3 hours'),
    (2, 1, 2, 2, 1, 150.00, NOW() - INTERVAL '2 hours'),
    (3, 1, 3, 1, 1, 80.00, NOW() - INTERVAL '90 minutes'),
    (4, 2, 1, 1, 1, 120.00, NOW() - INTERVAL '4 hours'),
    (5, 2, 4, 3, 1, 210.00, NOW() - INTERVAL '1 day'),
    (6, 3, 3, 1, 2, 160.00, NOW() - INTERVAL '6 hours')
ON CONFLICT (id) DO NOTHING;

-- =========================
-- Обслуживание и мониторинг
-- =========================

INSERT INTO maintenance_records (
    id, machine_id, performer_user_id, performer_name, serviced_at, work_description, issues, created_at
) VALUES
    (1, 1, 2, 'Петров П.П.', NOW() - INTERVAL '10 days', 'Плановое обслуживание и пополнение товара', NULL, NOW()),
    (2, 2, 2, 'Петров П.П.', NOW() - INTERVAL '2 days', 'Замена купюроприемника', 'Не принимал купюры', NOW())
ON CONFLICT (id) DO NOTHING;

INSERT INTO machine_events (id, machine_id, event_type, message, occurred_at) VALUES
    (1, 1, 'info', 'Проведена инкассация', NOW() - INTERVAL '12 hours'),
    (2, 2, 'service', 'Требуется обслуживание', NOW() - INTERVAL '3 hours'),
    (3, 3, 'warning', 'Низкий остаток воды', NOW() - INTERVAL '2 hours')
ON CONFLICT (id) DO NOTHING;

INSERT INTO machine_equipment (machine_id, equipment_type_id, status_code, notes, updated_at) VALUES
    (1, 1, 'ok', NULL, NOW()),
    (1, 2, 'ok', NULL, NOW()),
    (1, 3, 'ok', NULL, NOW()),
    (2, 1, 'fault', 'Требуется замена', NOW()),
    (2, 2, 'ok', NULL, NOW()),
    (2, 4, 'ok', NULL, NOW()),
    (3, 3, 'warning', 'Нужно провести очистку', NOW())
ON CONFLICT (machine_id, equipment_type_id) DO NOTHING;

INSERT INTO news_items (id, title, body, published_at, is_active) VALUES
    (1, 'Обновление графика обслуживания', 'На этой неделе обновлён маршрут сервисной группы.', NOW() - INTERVAL '2 days', TRUE),
    (2, 'Новый автомат в центре города', 'В сеть добавлен новый автомат в бизнес-центре.', NOW() - INTERVAL '1 day', TRUE)
ON CONFLICT (id) DO NOTHING;

-- =========================
-- Синхронизация последовательностей
-- =========================

SELECT setval(pg_get_serial_sequence('roles', 'id'), COALESCE((SELECT MAX(id) FROM roles), 1), TRUE);
SELECT setval(pg_get_serial_sequence('machine_statuses', 'id'), COALESCE((SELECT MAX(id) FROM machine_statuses), 1), TRUE);
SELECT setval(pg_get_serial_sequence('payment_methods', 'id'), COALESCE((SELECT MAX(id) FROM payment_methods), 1), TRUE);
SELECT setval(pg_get_serial_sequence('machine_types', 'id'), COALESCE((SELECT MAX(id) FROM machine_types), 1), TRUE);
SELECT setval(pg_get_serial_sequence('connection_types', 'id'), COALESCE((SELECT MAX(id) FROM connection_types), 1), TRUE);
SELECT setval(pg_get_serial_sequence('equipment_types', 'id'), COALESCE((SELECT MAX(id) FROM equipment_types), 1), TRUE);
SELECT setval(pg_get_serial_sequence('providers', 'id'), COALESCE((SELECT MAX(id) FROM providers), 1), TRUE);
SELECT setval(pg_get_serial_sequence('companies', 'id'), COALESCE((SELECT MAX(id) FROM companies), 1), TRUE);
SELECT setval(pg_get_serial_sequence('users', 'id'), COALESCE((SELECT MAX(id) FROM users), 1), TRUE);
SELECT setval(pg_get_serial_sequence('franchise_codes', 'id'), COALESCE((SELECT MAX(id) FROM franchise_codes), 1), TRUE);
SELECT setval(pg_get_serial_sequence('email_verification_codes', 'id'), COALESCE((SELECT MAX(id) FROM email_verification_codes), 1), TRUE);
SELECT setval(pg_get_serial_sequence('machine_models', 'id'), COALESCE((SELECT MAX(id) FROM machine_models), 1), TRUE);
SELECT setval(pg_get_serial_sequence('modems', 'id'), COALESCE((SELECT MAX(id) FROM modems), 1), TRUE);
SELECT setval(pg_get_serial_sequence('vending_machines', 'id'), COALESCE((SELECT MAX(id) FROM vending_machines), 1), TRUE);
SELECT setval(pg_get_serial_sequence('products', 'id'), COALESCE((SELECT MAX(id) FROM products), 1), TRUE);
SELECT setval(pg_get_serial_sequence('sales', 'id'), COALESCE((SELECT MAX(id) FROM sales), 1), TRUE);
SELECT setval(pg_get_serial_sequence('maintenance_records', 'id'), COALESCE((SELECT MAX(id) FROM maintenance_records), 1), TRUE);
SELECT setval(pg_get_serial_sequence('machine_events', 'id'), COALESCE((SELECT MAX(id) FROM machine_events), 1), TRUE);
SELECT setval(pg_get_serial_sequence('news_items', 'id'), COALESCE((SELECT MAX(id) FROM news_items), 1), TRUE);

COMMIT;
