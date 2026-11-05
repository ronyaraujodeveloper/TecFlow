/*
  Carga de homologação do robô de tela (SQL Server).

  Não execute na produção. No SSMS, selecione o banco de homologação
  e troque o USE abaixo se o catálogo tiver outro nome.

  1. Preencha @OwnerEmail com um usuário que já existe nesse banco.
  2. @InserirCarga = 1 grava 200 campanhas e 200 produtos com prefixo CARGA-.
     O robô não apaga esse prefixo.
  3. @ExecutarLimpeza = 1 apaga só o que o robô criou (prefixo E2E-)
     daquele dono e desativa operadores E2E-, sem excluir o usuário dono.
     A listagem da tela não tem botão de excluir; esta limpeza é o script.

  Os dois interruptores podem ficar ligados no mesmo lote.
  Com os dois em 0 o script para antes de gravar.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

USE [Ofertio];

BEGIN TRANSACTION;

DECLARE @OwnerEmail NVARCHAR(320) = N'';
DECLARE @InserirCarga BIT = 1;
DECLARE @ExecutarLimpeza BIT = 0;
DECLARE @OwnerId INT;
DECLARE @TenantId UNIQUEIDENTIFIER;
DECLARE @Now DATETIME2 = SYSUTCDATETIME();
DECLARE @CampanhasRemovidas INT = 0;
DECLARE @ProdutosRemovidos INT = 0;
DECLARE @OperadoresInativados INT = 0;

IF @OwnerEmail IS NULL OR LEN(LTRIM(RTRIM(@OwnerEmail))) = 0
    THROW 50000, N'Preencha @OwnerEmail com um usuário da homologação antes de executar.', 1;

IF @InserirCarga = 0 AND @ExecutarLimpeza = 0
    THROW 50000, N'Ligue @InserirCarga, @ExecutarLimpeza ou os dois.', 1;

SELECT @OwnerId = Id, @TenantId = TenantId
FROM dbo.Usuarios
WHERE Email = @OwnerEmail;

IF @OwnerId IS NULL
    THROW 50000, N'Nenhum usuário com esse e-mail. Confira o banco de homologação.', 1;

IF @ExecutarLimpeza = 1
BEGIN
    IF OBJECT_ID(N'dbo.OfferHealthAlerts', N'U') IS NOT NULL
        DELETE o
        FROM dbo.OfferHealthAlerts AS o
        INNER JOIN dbo.Campanhas AS c ON c.Id = o.CampaignId
        WHERE c.OwnerId = @OwnerId
          AND c.Nome LIKE N'E2E-%';

    IF OBJECT_ID(N'dbo.PreFlightNotifications', N'U') IS NOT NULL
        DELETE n
        FROM dbo.PreFlightNotifications AS n
        INNER JOIN dbo.Campanhas AS c ON c.Id = n.CampaignId
        WHERE c.OwnerId = @OwnerId
          AND c.Nome LIKE N'E2E-%';

    IF OBJECT_ID(N'dbo.Metricas', N'U') IS NOT NULL
    BEGIN
        UPDATE m
        SET m.MetricaId = NULL
        FROM dbo.Metricas AS m
        INNER JOIN dbo.Campanhas AS c ON c.Id = m.CampanhaId
        WHERE c.OwnerId = @OwnerId
          AND c.Nome LIKE N'E2E-%';

        DELETE m
        FROM dbo.Metricas AS m
        INNER JOIN dbo.Campanhas AS c ON c.Id = m.CampanhaId
        WHERE c.OwnerId = @OwnerId
          AND c.Nome LIKE N'E2E-%';
    END;

    IF OBJECT_ID(N'dbo.Afiliados', N'U') IS NOT NULL
        DELETE a
        FROM dbo.Afiliados AS a
        INNER JOIN dbo.Campanhas AS c ON c.Id = a.CampanhaId
        WHERE c.OwnerId = @OwnerId
          AND c.Nome LIKE N'E2E-%';

    DELETE FROM dbo.Campanhas
    WHERE OwnerId = @OwnerId
      AND Nome LIKE N'E2E-%';
    SET @CampanhasRemovidas = @@ROWCOUNT;

    IF OBJECT_ID(N'dbo.MarketplaceAffiliateLinks', N'U') IS NOT NULL
        DELETE l
        FROM dbo.MarketplaceAffiliateLinks AS l
        INNER JOIN dbo.ProdutosPropagandaGlobal AS p ON p.Id = l.ProdutoGlobalId
        WHERE p.OwnerId = @OwnerId
          AND p.NomeAmigavel LIKE N'E2E-%';

    DELETE FROM dbo.ProdutosPropagandaGlobal
    WHERE OwnerId = @OwnerId
      AND NomeAmigavel LIKE N'E2E-%';
    SET @ProdutosRemovidos = @@ROWCOUNT;

    UPDATE dbo.Usuarios
    SET IsActive = 0,
        UpdatedAt = @Now
    WHERE Id <> @OwnerId
      AND IsActive = 1
      AND (Nome LIKE N'E2E-%' OR Email LIKE N'e2e-%@example.com');
    SET @OperadoresInativados = @@ROWCOUNT;
END;

IF @InserirCarga = 1
BEGIN
    ;WITH Numeros AS (
        SELECT TOP (200) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
        FROM sys.all_objects AS a
        CROSS JOIN sys.all_objects AS b
    )
    INSERT INTO dbo.Campanhas (Nome, Descricao, DataInicio, DataFim, Orcamento, OwnerId, TenantId, CreatedAt)
    SELECT
        N'CARGA-campanha-' + RIGHT(N'000' + CAST(n AS NVARCHAR(3)), 3),
        N'Carga de homologação. O robô não apaga o prefixo CARGA-.',
        @Now,
        DATEADD(DAY, 30, @Now),
        10.00,
        @OwnerId,
        @TenantId,
        @Now
    FROM Numeros
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.Campanhas AS c
        WHERE c.OwnerId = @OwnerId
          AND c.Nome = N'CARGA-campanha-' + RIGHT(N'000' + CAST(Numeros.n AS NVARCHAR(3)), 3)
    );

    ;WITH Numeros AS (
        SELECT TOP (200) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
        FROM sys.all_objects AS a
        CROSS JOIN sys.all_objects AS b
    )
    INSERT INTO dbo.ProdutosPropagandaGlobal
        (TenantId, GlobalProductUid, NomeAmigavel, Descricao, CategoriaGlobal, PrecoMedio, OwnerId, CreatedAt)
    SELECT
        @TenantId,
        NEWID(),
        N'CARGA-produto-' + RIGHT(N'000' + CAST(n AS NVARCHAR(3)), 3),
        N'Carga de homologação. O robô não apaga o prefixo CARGA-.',
        N'Homologacao',
        89.90,
        @OwnerId,
        @Now
    FROM Numeros
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.ProdutosPropagandaGlobal AS p
        WHERE p.OwnerId = @OwnerId
          AND p.NomeAmigavel = N'CARGA-produto-' + RIGHT(N'000' + CAST(Numeros.n AS NVARCHAR(3)), 3)
    );
END;

COMMIT TRANSACTION;

PRINT N'Limpeza: ' + CAST(@CampanhasRemovidas AS NVARCHAR(10)) + N' campanhas E2E-, '
    + CAST(@ProdutosRemovidos AS NVARCHAR(10)) + N' produtos E2E-, '
    + CAST(@OperadoresInativados AS NVARCHAR(10)) + N' operadores inativados.';
PRINT N'Carga CARGA- conferida para ' + @OwnerEmail + N'.';
