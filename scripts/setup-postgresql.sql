/*
  Cria role e base PostgreSQL (executar como superuser postgres)
  psql -U postgres -f scripts/setup-postgresql.sql
*/

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'Us_Automacao') THEN
        CREATE ROLE "Us_Automacao" WITH LOGIN PASSWORD 'tecban321@';
        RAISE NOTICE 'Role Us_Automacao criada.';
    ELSE
        RAISE NOTICE 'Role Us_Automacao já existe.';
    END IF;
END
$$;

SELECT 'CREATE DATABASE automacaosociais OWNER "Us_Automacao"'
WHERE NOT EXISTS (SELECT 1 FROM pg_database WHERE datname = 'automacaosociais')\gexec

GRANT ALL PRIVILEGES ON DATABASE automacaosociais TO "Us_Automacao";
