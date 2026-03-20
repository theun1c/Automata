\set ON_ERROR_STOP on

DROP SCHEMA IF EXISTS automata CASCADE;

\i database/migrations/001_initial_schema.sql
\i database/seeds/001_seed_core.sql

SET search_path TO automata, public;

SELECT 'roles' AS table_name, COUNT(*) AS total FROM roles
UNION ALL
SELECT 'machine_statuses' AS table_name, COUNT(*) AS total FROM machine_statuses
UNION ALL
SELECT 'machine_models' AS table_name, COUNT(*) AS total FROM machine_models
UNION ALL
SELECT 'users' AS table_name, COUNT(*) AS total FROM users
UNION ALL
SELECT 'vending_machines' AS table_name, COUNT(*) AS total FROM vending_machines
UNION ALL
SELECT 'products' AS table_name, COUNT(*) AS total FROM products
UNION ALL
SELECT 'sales' AS table_name, COUNT(*) AS total FROM sales
UNION ALL
SELECT 'maintenance_records' AS table_name, COUNT(*) AS total FROM maintenance_records
ORDER BY table_name;
