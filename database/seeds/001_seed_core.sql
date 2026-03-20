-- 001_seed_core.sql
-- Стартовые данные для минимальной схемы Automata

BEGIN;

SET search_path TO automata, public;

INSERT INTO roles (id, name) VALUES
    (1, 'Администратор'),
    (2, 'Оператор')
ON CONFLICT (id) DO NOTHING;

INSERT INTO machine_statuses (id, name) VALUES
    (1, 'Рабочий'),
    (2, 'Не рабочий')
ON CONFLICT (id) DO NOTHING;

INSERT INTO machine_models (id, brand, model_name) VALUES
    (1, 'Necta', 'Kikko Max'),
    (2, 'Bianchi', 'Lei 400')
ON CONFLICT (id) DO NOTHING;

INSERT INTO users (
    id, last_name, first_name, middle_name, email, phone, password_hash, role_id, created_at
) VALUES
    (1, 'Иванов', 'Иван', 'Иванович', 'admin@automata.local', '+7 (900) 000-00-01', '$2b$12$jS0q5S6d2QpQkF3mWcE.8e1QY7wzjKJ8LQ6s36D5aUZ3eE9x2A8aC', 1, NOW()),
    (2, 'Петров', 'Пётр', 'Петрович', 'operator@automata.local', '+7 (900) 000-00-02', '$2b$12$M8JmF7xUdHhQe9mR2zQ9vO9kz1q4R8f6W2vW0y1XoC7nP4xT1jL7m', 2, NOW())
ON CONFLICT (id) DO NOTHING;

INSERT INTO vending_machines (
    id, name, location, machine_model_id, status_id, installed_at, last_service_at, total_income
) VALUES
    (1, 'ТА-001', 'г. Москва, ТЦ Север, 1 этаж', 1, 1, '2025-01-15', '2026-03-10', 152340.50),
    (2, 'ТА-002', 'г. Москва, БЦ Центр, холл', 2, 2, '2025-02-10', '2026-03-18', 94321.10)
ON CONFLICT (id) DO NOTHING;

INSERT INTO products (
    id, machine_id, name, description, price, quantity, min_stock, avg_daily_sales
) VALUES
    (1, 1, 'Эспрессо', 'Кофе эспрессо', 120.00, 25, 8, 11.5),
    (2, 1, 'Капучино', 'Кофе капучино', 150.00, 14, 8, 8.2),
    (3, 2, 'Вода 0.5', 'Питьевая вода 0.5л', 80.00, 30, 12, 5.0),
    (4, 2, 'Сэндвич', 'Сэндвич куриный', 210.00, 7, 5, 2.1)
ON CONFLICT (id) DO NOTHING;

INSERT INTO sales (
    id, machine_id, product_id, quantity, sale_amount, sale_datetime, payment_method
) VALUES
    (1, 1, 1, 2, 240.00, NOW() - INTERVAL '3 hours', 'Карта'),
    (2, 1, 2, 1, 150.00, NOW() - INTERVAL '2 hours', 'Карта'),
    (3, 2, 3, 2, 160.00, NOW() - INTERVAL '6 hours', 'Наличные'),
    (4, 2, 4, 1, 210.00, NOW() - INTERVAL '1 day', 'QR')
ON CONFLICT (id) DO NOTHING;

INSERT INTO maintenance_records (
    id, machine_id, user_id, service_date, work_description, issues
) VALUES
    (1, 1, 2, NOW() - INTERVAL '10 days', 'Плановое обслуживание и пополнение товара', NULL),
    (2, 2, 2, NOW() - INTERVAL '2 days', 'Замена купюроприемника', 'Не принимал купюры')
ON CONFLICT (id) DO NOTHING;

SELECT setval(pg_get_serial_sequence('roles', 'id'), COALESCE((SELECT MAX(id) FROM roles), 1), TRUE);
SELECT setval(pg_get_serial_sequence('machine_statuses', 'id'), COALESCE((SELECT MAX(id) FROM machine_statuses), 1), TRUE);
SELECT setval(pg_get_serial_sequence('machine_models', 'id'), COALESCE((SELECT MAX(id) FROM machine_models), 1), TRUE);
SELECT setval(pg_get_serial_sequence('users', 'id'), COALESCE((SELECT MAX(id) FROM users), 1), TRUE);
SELECT setval(pg_get_serial_sequence('vending_machines', 'id'), COALESCE((SELECT MAX(id) FROM vending_machines), 1), TRUE);
SELECT setval(pg_get_serial_sequence('products', 'id'), COALESCE((SELECT MAX(id) FROM products), 1), TRUE);
SELECT setval(pg_get_serial_sequence('sales', 'id'), COALESCE((SELECT MAX(id) FROM sales), 1), TRUE);
SELECT setval(pg_get_serial_sequence('maintenance_records', 'id'), COALESCE((SELECT MAX(id) FROM maintenance_records), 1), TRUE);

COMMIT;
