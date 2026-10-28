# 🏗️ Arquitetura Backend & Banco de Dados

> Regras obrigatórias para C# (.NET 8), API, serviços, EF Core e SQL Server. Encoding, idioma e nomenclatura: [encoding-standards.md](./encoding-standards.md). Integrações e OAuth: [marketplaces-oauth.md](./marketplaces-oauth.md).

## 1. Princípio de preservação total (inviolável)

1. Altere **apenas** o arquivo e o trecho de código explicitamente solicitados.
2. É **proibido** remover, sobrescrever ou simplificar componentes Blazor (`.razor`), métodos C#, DTOs, links do `NavMenu.razor` ou regras de negócio que não façam parte do escopo da alteração.
3. Parta sempre da versão mais recente salva e validada no Git.
4. Salve todos os arquivos em UTF-8 com BOM ([encoding-standards.md](./encoding-standards.md)).

## 2. Camadas da solução

- **`Ofertio.Core`:** entidades (`Entities/`) e enums de domínio.
- **`Ofertio.Database`:** `AppDbContext`, entidades de banco (`Entity/`) e filtros de consulta (`Filter/`).
- **`Ofertio.Data`:** migrations do SQL Server (projeto usado no `dotnet ef`).
- **`Ofertio.Business`:** DTOs (`Dto/`), interfaces, opções de integração (`Integrations/`) e regras puras (`Service/**/*Rules.cs`).
- **`Ofertio.Infrastructure.Services`:** serviços com HTTP/IO (Mercado Livre, Shopee, Amazon, Telegram, WhatsApp, mídia).
- **`Ofertio.API`:** controllers e endpoints; **`Ofertio.Worker`** / **`Ofertio.Orquestrador`:** processos em segundo plano.
- **`Ofertio.Util`:** utilitários transversais (`Validation/ValidationHelper`, criptografia).
- **`Ofertio.SharedUi`:** páginas, componentes, helpers e `wwwroot` compartilhados; **`Ofertio.WebUi`:** host Blazor WebApp; **`Ofertio.Mobile`:** app .NET MAUI Blazor Hybrid.
- **`Ofertio.Tests`:** xUnit + Moq (unitários e de integração).

Lógica de decisão testável fica em classes estáticas `*Rules` no Business; serviços de infraestrutura só orquestram chamadas e persistência.

## 3. Arquitetura racional de dados (apenas 3 objetos por entidade)

Para evitar redundância (como Create/Update DTOs), cada entidade possui estritamente:

- **`[Nome]Filter`** (`Ofertio.Database/Filter/`): propriedades escalares da entidade como opcionais (`?`). Usado **exclusivamente** em buscas e listagens (**GET**). Sem objetos de navegação, apenas IDs (`CampaignId`, `OwnerId`...).
- **`[Nome]Dto`** (`Ofertio.Business/Dto/`): propriedades limpas enviadas pelo formulário. Usado **exclusivamente** na escrita (**POST** e **PUT**). Sem objetos de navegação inteiros, apenas IDs.
- **`[Nome]ResponseDto`** (`Ofertio.Business/Dto/`): envelope de retorno com `Status` (bool), `Descricao` (string), `Data` (`[Nome]?`) e `DataList` (`List<[Nome]>?`).

> A entidade continua em `Ofertio.Core/Entities/` ou `Ofertio.Database/Entity/`. Exemplo `Estoque`: `EstoqueFilter`, `EstoqueDto`, `EstoqueResponseDto` + entidade.

## 4. Controllers, serviços e consultas

- **Retorno:** métodos **GET** retornam `[Nome]ResponseDto` com `Data` ou `DataList`; **POST/PUT** recebem `[Nome]Dto` e retornam `[Nome]ResponseDto` quando aplicável.
- **Filtros:** toda listagem aceita `[FromQuery] [Nome]Filter filter`, aplica o filtro na query (repositório ou extensão `ApplyFilter`) e pagina via `Pagin` (máximo 30 registros).
- **Listas na query string:** o cliente (`FilterQueryStringExtensions.ToQueryString`) repete a chave para cada item (`Categorias=a&Categorias=b`), formato que o `[FromQuery]` entende.
- **Validações:** e-mail, celular/WhatsApp, CPF, CNPJ alfanumérico e CEP passam pelo `ValidationHelper` (`Ofertio.Util/Validation`) antes de persistir.
- **Assíncrono ponta a ponta:** sem `.Result` / `.Wait()`; use `await` (inclusive depois de `Task.WhenAll`).
- **HTTP externo:** `HttpClient` tipado com timeout vindo de opções (ex.: `Integrations:MercadoLivre:HttpTimeoutSeconds`, padrão 30 s); timeout do cliente vira mensagem amigável ou resultado parcial, nunca a exceção crua na tela. Cancelamento pelo chamador não é tratado como timeout (`OfficialCatalogSearchRules.IsHttpTimeout`).

## 5. Banco de dados oficial (inviolável)

- O único banco oficial é o **SQL Server**, banco **`Ofertio`** (`localhost\SQLEXPRESS` no Development e na Homologação IIS).
- Provider obrigatório: **`SqlServer`** (`Microsoft.EntityFrameworkCore.SqlServer`, `Database:Provider=SqlServer`).
- É **proibido** trocar o `appsettings.json` para `PostgreSQL` / `Npgsql` ou criar bancos com outros nomes (ex.: `OfertioDb`). O PostgreSQL existe só como assembly de migrations legado em `Ofertio.Infrastructure`.
- Migrations geradas e aplicadas apenas em T-SQL, em `Ofertio.Data` com `--context AppDbContext` (comandos no [README](../../README.md#-comandos-principais)).

## 6. Segurança, criptografia e LGPD

- **Credenciais sempre criptografadas em repouso:** senhas, `AccessToken`, `RefreshToken`, `ClientSecret`/`AppSecret` e chaves de API passam pelo `ISecurityEncryptionService` / `ValueConverter` do EF Core (`EncryptedStringConverter.CreateProtected`); nunca em texto puro no banco.
- **Data Protection:** `AddOfertioDataProtection` com chaves em `App_Data/OfertioDataProtectionKeys` da API (`DataProtection:KeysPath`), protegidas por DPAPI; API, Worker e Orquestrador compartilham o mesmo anel.
- **Cadeia de leitura (`Unprotect`):** `DP1:` (Data Protection) ➔ `ENC1:` (AES legado) ➔ texto puro. Ao mexer em entidades com segredos, confira se a conversão no `OnModelCreating` continua ativa.
- **Deploy:** a limpeza da pasta da API **preserva `App_Data`**; sem as chaves, os tokens gravados ficam ilegíveis e todas as contas precisam ser reautorizadas.
- **Identificadores legados:** `SetApplicationName("TecFlow")` e o purpose `TecFlow.MarketplaceTokens.v1` **não podem ser renomeados** (ver [encoding-standards.md](./encoding-standards.md#5-identificadores-legados-não-renomear)).
- **Segredos fora do repositório:** `appsettings` locais com segredos, `.env`, `*.pfx` e a pasta de chaves ficam no `.gitignore`. Senha do certificado do Kestrel só em User Secrets (`Kestrel:Endpoints:Https:Certificate:Password`) ou variável de ambiente.
- **Logs:** nunca registrar tokens, senhas, `client_secret`, `code` do OAuth ou `code_verifier`.
- **Cartão de crédito:** número, CVV e validade só trafegam na requisição (`CreditCardDto`) até o `IPlanPaymentGateway`; nunca são gravados, logados nem devolvidos. O pedido (`PedidosAssinatura`) guarda apenas bandeira e 4 últimos dígitos. CPF/CNPJ do cadastro (`PerfisCadastrais.Document`) fica criptografado.
- **Troca de plano:** o checkout (`MyPlanService`) registra o pedido e nunca altera `Tenant.PlanCode`/`BillingCycle`; quem troca o plano é o webhook do gateway ao confirmar o pagamento.

## 7. Registro de telas e árvore dinâmica de menus

1. **Toda tela nova tem registro em `MenuItems`** (SQL Server) com `Title`, `Route`, `Icon`, `OrderIndex` e `ParentId`; é proibido fixar links ou submenus no `NavMenu.razor`.
2. O seeding garimpa as rotas e o item aparece em `/master/menu-permissions` para permissão por plano (`PlanMenuPermissions`).
3. O `NavMenu.razor` renderiza respeitando `OrderIndex` e os agrupamentos por `ParentId` definidos pelo Master (drag & drop).

## 8. Arquivos, uploads e IIS

- **Uploads fora do deploy:** `UploadStorage:RootPath` (`C:\inetpub\ofertio\uploads` em produção, `C:\inetpub\ofertio_dev\uploads` no Development), servido em `/uploads`; a pasta precisa se chamar `uploads` porque as URLs no banco começam com `/uploads/`.
- **Mídia:** o banco só recebe a URL depois que o arquivo físico existe (`.tmp` ➔ flush ➔ move); sem arquivo confirmado, a URL fica nula.
- **IIS:** sites `Ofertio.API` / `Ofertio.WebUi`, pools `OfertioApiPool` / `OfertioWebUiPool`. **Não** criar subaplicação `/api` dentro de `Ofertio.WebUi` (HTTP 500.35 no mesmo pool); a WebUi repassa as rotas públicas para a API via YARP.
- **HTTPS no Kestrel (só `dotnet run`):** `Ofertio.WebUi/appsettings.Development.json` aponta o `.pfx` em `Kestrel:Endpoints:Https` (`Http1AndHttp2`); no IIS o HTTPS da porta 7002 é o binding do site.

## 9. Testes

- Todo comportamento novo ou corrigido ganha teste em `Ofertio.Tests` (xUnit + Moq; EF Core InMemory para serviços com banco).
- Telas Blazor são validadas pelos serviços HTTP e helpers extraídos (sem bUnit).
- Comandos de execução e filtros: [README](../../README.md#-comandos-principais).
