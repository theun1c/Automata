\set ON_ERROR_STOP on

DROP SCHEMA IF EXISTS automata CASCADE;

\i database/migrations/001_initial_schema.sql
\i database/seeds/001_seed_core.sql

SET search_path TO automata, public;

SELECT 'companies' AS table_name, COUNT(*) AS total FROM companies
UNION ALL
SELECT 'users' AS table_name, COUNT(*) AS total FROM users
UNION ALL
SELECT 'vending_machines' AS table_name, COUNT(*) AS total FROM vending_machines
UNION ALL
SELECT 'machine_products' AS table_name, COUNT(*) AS total FROM machine_products
UNION ALL
SELECT 'sales' AS table_name, COUNT(*) AS total FROM sales
ORDER BY table_name;
