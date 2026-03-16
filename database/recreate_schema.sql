\set ON_ERROR_STOP on

DROP SCHEMA IF EXISTS automata CASCADE;

\i database/migrations/001_initial_schema.sql
\i database/seeds/001_seed_core.sql

SET search_path TO automata, public;

-- Quick verification
SELECT 'companies' AS table_name, COUNT(*) AS total FROM companies
UNION ALL
SELECT 'users' AS table_name, COUNT(*) AS total FROM users
UNION ALL
SELECT 'vending_machines' AS table_name, COUNT(*) AS total FROM vending_machines
UNION ALL
SELECT 'machine_monitor_snapshots' AS table_name, COUNT(*) AS total FROM machine_monitor_snapshots
ORDER BY table_name;
