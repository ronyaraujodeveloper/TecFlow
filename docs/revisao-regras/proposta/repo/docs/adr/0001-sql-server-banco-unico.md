# 0001. SQL Server como banco único e fim do suporte a PostgreSQL

- **Status:** Aceita (registro retroativo; fim do suporte a PostgreSQL decidido em 25/11/2026)
- **Data:** 2026-11-25

## Contexto

O projeto começou com PostgreSQL (migrations em `Ofertio.Infrastructure`) e migrou para SQL Server. Bancos com nomes diferentes (`OfertioDb`) causaram confusão entre ambientes. Mesmo depois da migração, o código continuou aceitando `UseNpgsql` (`Ofertio.Database/Data/RelationalDatabaseOptions.cs:35`), com interceptor de encoding próprio, e parte do SQL já é exclusivo de SQL Server (`DataPurgeService.cs:47-67`).

## Decisão

Usar SQL Server 2022, banco `Ofertio`, provider `SqlServer` e migrations só em `Ofertio.Data`, nos três ambientes. **O suporte a PostgreSQL acaba:** saem o ramo `UseNpgsql`, o `NpgsqlUtf8ClientEncodingInterceptor`, as migrations de `Ofertio.Infrastructure/Migrations`, o pacote Npgsql e as regras que citam conexão PostgreSQL.

## Alternativas descartadas

- **Manter os dois providers:** dobra o custo de migrations e testes, e o código já tem SQL específico de SQL Server.
- **Manter as migrations PostgreSQL só como histórico no código:** ocupam o build e confundem a IA e quem lê o código. O `git log` já guarda o histórico.

## Consequências

- Remoção em etapa própria (PLANO, etapa 17), com build e testes verdes e conferência de que nenhum ambiente usa `Database:Provider=PostgreSql`.
- Código novo pode usar recursos específicos de SQL Server sem condicional por provider.
