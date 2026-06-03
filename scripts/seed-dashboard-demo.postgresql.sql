/*
  TecFlow — seed PostgreSQL (Portal + Orquestrador)
  Login: demo@TecFlow.local / Test@123

  psql -U Us_Automacao -d automacaosociais -f scripts/seed-dashboard-demo.postgresql.sql
*/

BEGIN;

DO $$
DECLARE
    v_owner_id INTEGER;
    v_now TIMESTAMPTZ := NOW() AT TIME ZONE 'UTC';
    v_campanha_tiktok_id INTEGER;
    v_campanha_shopee_id INTEGER;
    v_campanha_encerrada_id INTEGER;
    v_total_campanhas INTEGER;
    v_total_metricas INTEGER;
BEGIN
    IF NOT EXISTS (SELECT 1 FROM "Usuarios" WHERE "Email" = 'demo@TecFlow.local') THEN
        INSERT INTO "Usuarios" ("Nome", "Email", "PasswordHash", "Plano", "CreatedAt")
        VALUES (
            'Utilizador Demo',
            'demo@TecFlow.local',
            '$2a$11$/vyYZV8TriN/IBQ8vlTbyeaoJJfTHkWy9T./mfHyefLAqo6FYmTmm',
            'Pro',
            v_now
        );
        RAISE NOTICE 'Utilizador demo@TecFlow.local criado.';
    ELSE
        RAISE NOTICE 'Utilizador demo@TecFlow.local já existe.';
    END IF;

    SELECT "Id" INTO v_owner_id FROM "Usuarios" WHERE "Email" = 'demo@TecFlow.local';

    IF NOT EXISTS (
        SELECT 1 FROM "Campanhas"
        WHERE "OwnerId" = v_owner_id AND "Nome" = 'TikTok Shop — Verão 2026')
    THEN
        INSERT INTO "Campanhas" ("Nome", "Descricao", "DataInicio", "DataFim", "Orcamento", "OwnerId", "CreatedAt")
        VALUES (
            'TikTok Shop — Verão 2026',
            'Campanha sazonal de afiliados TikTok Shop com foco em eletrónicos.',
            v_now - INTERVAL '30 days',
            v_now + INTERVAL '60 days',
            15000.00,
            v_owner_id,
            v_now
        );
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM "Campanhas"
        WHERE "OwnerId" = v_owner_id AND "Nome" = 'Shopee — Flash Sale Maio')
    THEN
        INSERT INTO "Campanhas" ("Nome", "Descricao", "DataInicio", "DataFim", "Orcamento", "OwnerId", "CreatedAt")
        VALUES (
            'Shopee — Flash Sale Maio',
            'Promoções relâmpago e cupons para afiliados Shopee.',
            v_now - INTERVAL '7 days',
            v_now + INTERVAL '23 days',
            8500.00,
            v_owner_id,
            v_now
        );
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM "Campanhas"
        WHERE "OwnerId" = v_owner_id AND "Nome" = 'TikTok Ads — Remarketing Q1')
    THEN
        INSERT INTO "Campanhas" ("Nome", "Descricao", "DataInicio", "DataFim", "Orcamento", "OwnerId", "CreatedAt")
        VALUES (
            'TikTok Ads — Remarketing Q1',
            'Campanha encerrada de remarketing (dados históricos no painel).',
            v_now - INTERVAL '120 days',
            v_now - INTERVAL '30 days',
            5200.00,
            v_owner_id,
            v_now
        );
    END IF;

    SELECT "Id" INTO v_campanha_tiktok_id FROM "Campanhas"
    WHERE "OwnerId" = v_owner_id AND "Nome" = 'TikTok Shop — Verão 2026';

    SELECT "Id" INTO v_campanha_shopee_id FROM "Campanhas"
    WHERE "OwnerId" = v_owner_id AND "Nome" = 'Shopee — Flash Sale Maio';

    SELECT "Id" INTO v_campanha_encerrada_id FROM "Campanhas"
    WHERE "OwnerId" = v_owner_id AND "Nome" = 'TikTok Ads — Remarketing Q1';

    IF v_campanha_tiktok_id IS NOT NULL
       AND NOT EXISTS (
           SELECT 1 FROM "Metricas"
           WHERE "CampanhaId" = v_campanha_tiktok_id
             AND "OwnerId" = v_owner_id
             AND "Visualizacoes" = 125000)
    THEN
        INSERT INTO "Metricas" ("CampanhaId", "Visualizacoes", "Cliques", "Vendas", "Investimento", "Receita", "OwnerId", "CreatedAt")
        VALUES (v_campanha_tiktok_id, 125000, 8750, 420, 4500.00, 18900.00, v_owner_id, v_now);

        INSERT INTO "Metricas" ("CampanhaId", "Visualizacoes", "Cliques", "Vendas", "Investimento", "Receita", "OwnerId", "CreatedAt")
        VALUES (v_campanha_tiktok_id, 48000, 3200, 155, 1800.00, 6200.00, v_owner_id, v_now - INTERVAL '14 days');
    END IF;

    IF v_campanha_shopee_id IS NOT NULL
       AND NOT EXISTS (
           SELECT 1 FROM "Metricas"
           WHERE "CampanhaId" = v_campanha_shopee_id
             AND "OwnerId" = v_owner_id
             AND "Visualizacoes" = 89000)
    THEN
        INSERT INTO "Metricas" ("CampanhaId", "Visualizacoes", "Cliques", "Vendas", "Investimento", "Receita", "OwnerId", "CreatedAt")
        VALUES (v_campanha_shopee_id, 89000, 6100, 310, 2800.00, 11200.00, v_owner_id, v_now);
    END IF;

    IF v_campanha_encerrada_id IS NOT NULL
       AND NOT EXISTS (
           SELECT 1 FROM "Metricas"
           WHERE "CampanhaId" = v_campanha_encerrada_id AND "OwnerId" = v_owner_id)
    THEN
        INSERT INTO "Metricas" ("CampanhaId", "Visualizacoes", "Cliques", "Vendas", "Investimento", "Receita", "OwnerId", "CreatedAt")
        VALUES (v_campanha_encerrada_id, 210000, 9200, 180, 5200.00, 9800.00, v_owner_id, v_now - INTERVAL '90 days');
    END IF;

    SELECT COUNT(*) INTO v_total_campanhas FROM "Campanhas" WHERE "OwnerId" = v_owner_id;
    SELECT COUNT(*) INTO v_total_metricas FROM "Metricas" WHERE "OwnerId" = v_owner_id;

    RAISE NOTICE '========== Seed concluído ==========';
    RAISE NOTICE 'OwnerId: %', v_owner_id;
    RAISE NOTICE 'Login: demo@TecFlow.local / Test@123';
    RAISE NOTICE 'Campanhas: %', v_total_campanhas;
    RAISE NOTICE 'Métricas: %', v_total_metricas;
END
$$;

COMMIT;
