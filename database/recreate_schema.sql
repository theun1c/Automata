\set ON_ERROR_STOP on

-- Полное пересоздание схемы automata
DROP SCHEMA IF EXISTS automata CASCADE;

\i database/migrations/001_initial_schema.sql
\i database/seeds/001_seed_core.sql

SET search_path TO automata, public;

SELECT 'critical_value_templates' AS table_name, COUNT(*) AS total FROM critical_value_templates
UNION ALL
SELECT 'companies', COUNT(*) FROM companies
UNION ALL
SELECT 'machine_models', COUNT(*) FROM machine_models
UNION ALL
SELECT 'machine_statuses', COUNT(*) FROM machine_statuses
UNION ALL
SELECT 'maintenance_records', COUNT(*) FROM maintenance_records
UNION ALL
SELECT 'modems', COUNT(*) FROM modems
UNION ALL
SELECT 'notification_templates', COUNT(*) FROM notification_templates
UNION ALL
SELECT 'product_matrices', COUNT(*) FROM product_matrices
UNION ALL
SELECT 'products', COUNT(*) FROM products
UNION ALL
SELECT 'roles', COUNT(*) FROM roles
UNION ALL
SELECT 'sales', COUNT(*) FROM sales
UNION ALL
SELECT 'users', COUNT(*) FROM users
UNION ALL
SELECT 'vending_machines', COUNT(*) FROM vending_machines
ORDER BY table_name;
