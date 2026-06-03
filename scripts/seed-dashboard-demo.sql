/*
  TecFlow — dados de demonstração (SQL Server — legado)
  Para PostgreSQL use: scripts/seed-dashboard-demo.postgresql.sql
  Ou deixe o Orquestrador inserir automaticamente em Development (DemoDataSeeder).

  Login: demo@TecFlow.local / Test@123
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

USE [AutomacaoSociais];
GO

BEGIN TRANSACTION;

DECLARE @OwnerId INT;
DECLARE @Now DATETIME2 = SYSUTCDATETIME();
DECLARE @CampanhaTikTokId INT;
DECLARE @CampanhaShopeeId INT;
DECLARE @CampanhaEncerradaId INT;

-- ---------------------------------------------------------------------------
-- Utilizador de demonstração
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE Email = N'demo@TecFlow.local')
BEGIN
    INSERT INTO dbo.Usuarios (Nome, Email, PasswordHash, Plano, CreatedAt)
    VALUES (
        N'Utilizador Demo',
        N'demo@TecFlow.local',
        N'$2a$11$/vyYZV8TriN/IBQ8vlTbyeaoJJfTHkWy9T./mfHyefLAqo6FYmTmm', -- Test@123
        N'Pro',
        @Now
    );
    PRINT N'Utilizador demo@TecFlow.local criado.';
END
ELSE
    PRINT N'Utilizador demo@TecFlow.local já existe.';

SELECT @OwnerId = Id FROM dbo.Usuarios WHERE Email = N'demo@TecFlow.local';

-- ---------------------------------------------------------------------------
-- Campanhas
-- ---------------------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM dbo.Campanhas
    WHERE OwnerId = @OwnerId AND Nome = N'TikTok Shop — Verão 2026')
BEGIN
    INSERT INTO dbo.Campanhas (Nome, Descricao, DataInicio, DataFim, Orcamento, OwnerId, CreatedAt)
    VALUES (
        N'TikTok Shop — Verão 2026',
        N'Campanha sazonal de afiliados TikTok Shop com foco em eletrónicos.',
        DATEADD(DAY, -30, @Now),
        DATEADD(DAY, 60, @Now),
        15000.00,
        @OwnerId,
        @Now
    );
END

IF NOT EXISTS (
    SELECT 1 FROM dbo.Campanhas
    WHERE OwnerId = @OwnerId AND Nome = N'Shopee — Flash Sale Maio')
BEGIN
    INSERT INTO dbo.Campanhas (Nome, Descricao, DataInicio, DataFim, Orcamento, OwnerId, CreatedAt)
    VALUES (
        N'Shopee — Flash Sale Maio',
        N'Promoções relâmpago e cupons para afiliados Shopee.',
        DATEADD(DAY, -7, @Now),
        DATEADD(DAY, 23, @Now),
        8500.00,
        @OwnerId,
        @Now
    );
END

IF NOT EXISTS (
    SELECT 1 FROM dbo.Campanhas
    WHERE OwnerId = @OwnerId AND Nome = N'TikTok Ads — Remarketing Q1')
BEGIN
    INSERT INTO dbo.Campanhas (Nome, Descricao, DataInicio, DataFim, Orcamento, OwnerId, CreatedAt)
    VALUES (
        N'TikTok Ads — Remarketing Q1',
        N'Campanha encerrada de remarketing (dados históricos no painel).',
        DATEADD(DAY, -120, @Now),
        DATEADD(DAY, -30, @Now),
        5200.00,
        @OwnerId,
        @Now
    );
END

SELECT @CampanhaTikTokId = Id FROM dbo.Campanhas WHERE OwnerId = @OwnerId AND Nome = N'TikTok Shop — Verão 2026';
SELECT @CampanhaShopeeId = Id FROM dbo.Campanhas WHERE OwnerId = @OwnerId AND Nome = N'Shopee — Flash Sale Maio';
SELECT @CampanhaEncerradaId = Id FROM dbo.Campanhas WHERE OwnerId = @OwnerId AND Nome = N'TikTok Ads — Remarketing Q1';

-- ---------------------------------------------------------------------------
-- Métricas (uma linha por campanha + período extra na campanha TikTok)
-- ---------------------------------------------------------------------------
IF @CampanhaTikTokId IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.Metricas WHERE CampanhaId = @CampanhaTikTokId AND OwnerId = @OwnerId AND Visualizacoes = 125000)
BEGIN
    INSERT INTO dbo.Metricas (CampanhaId, Visualizacoes, Cliques, Vendas, Investimento, Receita, OwnerId, CreatedAt)
    VALUES (@CampanhaTikTokId, 125000, 8750, 420, 4500.00, 18900.00, @OwnerId, @Now);

    INSERT INTO dbo.Metricas (CampanhaId, Visualizacoes, Cliques, Vendas, Investimento, Receita, OwnerId, CreatedAt)
    VALUES (@CampanhaTikTokId, 48000, 3200, 155, 1800.00, 6200.00, @OwnerId, DATEADD(DAY, -14, @Now));
END

IF @CampanhaShopeeId IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.Metricas WHERE CampanhaId = @CampanhaShopeeId AND OwnerId = @OwnerId AND Visualizacoes = 89000)
BEGIN
    INSERT INTO dbo.Metricas (CampanhaId, Visualizacoes, Cliques, Vendas, Investimento, Receita, OwnerId, CreatedAt)
    VALUES (@CampanhaShopeeId, 89000, 6100, 310, 2800.00, 11200.00, @OwnerId, @Now);
END

IF @CampanhaEncerradaId IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.Metricas WHERE CampanhaId = @CampanhaEncerradaId AND OwnerId = @OwnerId)
BEGIN
    INSERT INTO dbo.Metricas (CampanhaId, Visualizacoes, Cliques, Vendas, Investimento, Receita, OwnerId, CreatedAt)
    VALUES (@CampanhaEncerradaId, 210000, 9200, 180, 5200.00, 9800.00, @OwnerId, DATEADD(DAY, -90, @Now));
END

DECLARE @TotalCampanhas INT = (SELECT COUNT(*) FROM dbo.Campanhas WHERE OwnerId = @OwnerId);
DECLARE @TotalMetricas INT = (SELECT COUNT(*) FROM dbo.Metricas WHERE OwnerId = @OwnerId);

COMMIT TRANSACTION;

PRINT N'';
PRINT N'========== Seed concluído ==========';
PRINT N'OwnerId: ' + CAST(@OwnerId AS NVARCHAR(20));
PRINT N'Login Portal: demo@TecFlow.local / Test@123';
PRINT N'Campanhas: ' + CAST(@TotalCampanhas AS NVARCHAR(10));
PRINT N'Métricas:  ' + CAST(@TotalMetricas AS NVARCHAR(10));
GO
