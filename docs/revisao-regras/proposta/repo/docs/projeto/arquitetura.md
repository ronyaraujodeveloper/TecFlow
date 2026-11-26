# Arquitetura do Ofertio

> Substitui `docs/rules/backend-architecture.md` §1 a §5, §8 e §9. Segurança específica: [seguranca-ofertio.md](./seguranca-ofertio.md). Menu e permissões: [menu-e-permissoes.md](./menu-e-permissoes.md). Regras gerais de C#, API e banco: `docs/padroes/`.

## 1. Preservação

- NÃO DEVE: remover ou simplificar validação, regra de negócio, método, DTO, componente Blazor ou item de menu que não faça parte do pedido. Motivo: refatorações anteriores apagaram regras sem querer.
- DEVE: o escopo de uma tarefa = o código pedido + os testes dele + a documentação afetada. Motivo: teste e documentação no mesmo passo fazem parte da entrega.
- NÃO DEVE: refatorar, renomear ou "melhorar" código fora desse escopo. A melhoria vira sugestão no relatório final ou item no `CHECKLIST_ATIVO.md`.
- Exceção: correção mínima indispensável para o código pedido compilar ou o teste passar (ex.: ajustar uma assinatura chamada). Ela é citada no relatório da tarefa.
- (Substitui "altere apenas o arquivo e o trecho solicitados", `backend-architecture.md:7`, que conflitava com a regra de teste e documentação no mesmo passo.)

## 2. Camadas e dependências

O mapa de projetos e as referências permitidas estão no `CLAUDE.md` (seção "Mapa da solução"). Regras:

- DEVE: lógica de decisão testável em classe estática `[Assunto]Rules` no `Ofertio.Business` (`Service/**`), sem IO. Serviço de `Ofertio.Infrastructure.Services` só orquestra: lê, chama a regra, grava, chama API externa.
- DEVE: controller fino: recebe DTO, chama um serviço, devolve o envelope. NÃO DEVE: acessar `AppDbContext` direto, montar consulta, chamar `HttpClient` ou conter regra de negócio. Dívida conhecida: `InventoryController`, `MarketplaceAuthController`.
- NÃO DEVE: nova referência de `Business` ou `SharedUi` para `Database` (ver ADR 0002). Tipo que a tela precisa e hoje mora no `Database` é movido para `Business` quando for tocado.
- NÃO DEVE: nova referência entre projetos sem ADR e sem atualizar o diagrama 1 de `docs/DIAGRAMAS_ARQUITETURA.md` (gerado por `scripts/gerar-diagrama-dependencias.ps1`).

## 3. Objetos por entidade

| Objeto | Pasta | Uso | Contém |
|---|---|---|---|
| `[Nome]Filter` | `Ofertio.Database/Filter/` | só leitura (GET), via `[FromQuery]` | propriedades escalares opcionais e IDs; implementa `IPagedFilter` |
| `[Nome]Dto` | `Ofertio.Business/Dto/` | só escrita (POST/PUT), via `[FromBody]` | campos que o usuário pode alterar; IDs em vez de navegação |
| `[Nome]ReadDto` | `Ofertio.Business/Dto/` | dados devolvidos (tipo de `Data`/`DataList`) | só os campos que a tela precisa; IDs e nomes de relacionamentos já resolvidos; **nunca** segredo, hash, token ou dado de outro inquilino |
| `[Nome]ResponseDto` | `Ofertio.Business/Dto/` | envelope de sucesso | `Status` (bool), `Descricao` (string), `Data` (`[Nome]ReadDto?`), `DataList` (`List<[Nome]ReadDto>?`), `Paging` |

- DEVE: só esses quatro por entidade. Motivo: evita `CreateXDto`/`UpdateXDto` duplicados e separa o que entra do que sai (ADR 0009).
- DEVE: o `ReadDto` ser montado por projeção (`Select`) na consulta ou por um método de mapeamento único, nunca devolvendo a entidade "por enquanto".
- NÃO DEVE: `ResponseDto` com propriedade do tipo de entidade (`Ofertio.Core.Entities`/`Ofertio.Database.Entity`). Verificar: teste de arquitetura que reflete os tipos `*ResponseDto` e falha se achar entidade.
- Quando a entidade é simples e não tem nada sensível, o `ReadDto` ainda é criado (pode ser um `record` curto). Motivo: campo sensível acrescentado à entidade no futuro não vaza sozinho.
- Migração: `UserAccountResponseDto` primeiro (vazamento de `PasswordHash`); os demais quando o controller for tocado.
- NÃO DEVE: o `[Nome]Dto` conter campo que o cliente não pode definir (dono, inquilino, plano, saldo, papel).
- Exceções permitidas (não contam como objeto extra): payloads de API externa (`*Payload`, `*Request` de integração em `Business/Integrations/`), webhooks recebidos (`*WebhookRequest`) e comandos de fila. Ficam junto da integração, não em `Dto/`.

## 4. Controllers, consultas e listagens

- DEVE: listagem aceita `[FromQuery] [Nome]Filter filter`, aplica o filtro (`ApplyFilter` ou repositório) e pagina com `PagedListHelper` (padrão 20, máximo 30, `Ofertio.Database/Pagin/PagedListHelper.cs:7-8`).
- Exceção documentada: combos de localidade (`api/localidades/cidades?StateCode=SP`) devolvem a UF inteira, sem paginação; vêm do `ILocationCatalog` em memória.
- DEVE: lista na query string repete a chave (`Categorias=a&Categorias=b`), formato do `FilterQueryStringExtensions.ToQueryString`.
- DEVE: e-mail, celular/WhatsApp, CPF, CNPJ alfanumérico e CEP validados pelo `ValidationHelper` (`Ofertio.Util/Validation`) antes de persistir.
- DEVE: localização (país/estado/cidade) guardada só como ID das tabelas `Countries`/`States`/`Cities`; a UF sai da cidade.
- DEVE: `HttpClient` tipado com timeout de opções (ex.: `Integrations:MercadoLivre:HttpTimeoutSeconds`, padrão 30 s). Timeout vira mensagem amigável ou resultado parcial; cancelamento do chamador não é timeout (`OfficialCatalogSearchRules.IsHttpTimeout`).

### Códigos HTTP e formato de erro

Decisão de 25/11/2026 (ADR 0010): **código HTTP correto em tudo**. Substitui os três formatos que conviviam: envelope com `Status=false` (`backend-architecture.md:32`), `ProblemDetails` só no middleware (`ExceptionHandlingMiddleware.cs:80`) e `ACCESS_DENIED` com HTTP 200 (`backend-architecture.md:77`).

| Situação | HTTP | Corpo | `errorCode` |
|---|---|---|---|
| Sucesso com dados | 200 | `[Nome]ResponseDto` (`Status=true`) | — |
| Criação | 201 | `[Nome]ResponseDto` com o item criado | — |
| Sucesso sem corpo (ex.: DELETE) | 204 | — | — |
| Entrada inválida (formato, tamanho, campo obrigatório) | 400 | `ValidationProblemDetails` (`errors` por campo) | `VALIDATION` |
| Sem autenticação ou sessão expirada | 401 | `ProblemDetails` | `UNAUTHENTICATED` |
| Autenticado sem papel ou sem a chave do plano | 403 | `ProblemDetails` | `ACCESS_DENIED` |
| Recurso inexistente **ou de outro dono/inquilino** | 404 | `ProblemDetails` | `NOT_FOUND` |
| Duplicidade ou conflito de concorrência | 409 | `ProblemDetails` | `CONFLICT` (ou específico, ex. `EMAIL_IN_USE`) |
| Regra de negócio violada com entrada válida (saldo insuficiente, plano sem cota, estado não permite) | 422 | `ProblemDetails` | específico, ex. `INSUFFICIENT_CREDITS` |
| Limite de taxa | 429 | `ProblemDetails` + `Retry-After` | `RATE_LIMITED` |
| Integração externa fora ou lenta | 502 / 504 | `ProblemDetails` com mensagem amigável | `UPSTREAM_ERROR` / `UPSTREAM_TIMEOUT` |
| Erro inesperado | 500 | `ProblemDetails` com `traceId`, sem `exception.Message` | `INTERNAL` |

Regras:

- DEVE: todo erro sair como `ProblemDetails` (RFC 9457) com as extensões `errorCode` (constante em inglês, `UPPER_SNAKE_CASE`, lista em `Ofertio.Business/Errors/ErrorCodes.cs`) e `traceId`. O campo `detail` traz a mensagem em português que a tela pode mostrar.
- DEVE: o serviço devolver um resultado tipado (`ServiceResult`/`Result<T>` com `ErrorCode`) e o controller traduzir para o HTTP num único lugar (método base ou extensão `ToActionResult()`), não com `if` em cada action.
- NÃO DEVE: HTTP 200 com `Status=false`. Em resposta de sucesso, `Status` é sempre `true`. A propriedade fica por compatibilidade e `Descricao` passa a ser só a mensagem de sucesso opcional.
- DEVE: o cliente (serviços HTTP da `SharedUi` e o Mobile) tratar **401** como sessão expirada (vai para o login) e **403** como "sem acesso" (toast ou `/sem-acesso`), lendo `detail` do `ProblemDetails` para a mensagem.
- Transição: os endpoints existentes migram por controller, depois que o cliente HTTP da `SharedUi` aceitar os dois formatos (PLANO, etapa 15). Endpoint novo já nasce no formato novo.

## 5. Banco de dados

- DEVE: banco único SQL Server `Ofertio`, container `ofertio-db`, provider `SqlServer`. NÃO DEVE: trocar para PostgreSQL ou criar banco com outro nome (ADR 0001).
- DEVE: migrations em `Ofertio.Data` com `--context AppDbContext` (comandos no `CLAUDE.md`). O suporte a PostgreSQL acabou (ADR 0001): `Ofertio.Infrastructure/Migrations`, `UseNpgsql` e o pacote Npgsql são removidos (PLANO, etapa 17). NÃO DEVE: código novo com SQL ou provider que não seja SQL Server.
- DEVE: entidade nova herdar `BaseEntity` (`Id`, `CreatedAt`, `UpdatedAt`) salvo motivo registrado. Hoje 58 de 75 herdam.
- DEVE: em produção, migration aplicada por script idempotente depois do backup, seguindo `docs/EXECUCAO_PRODUCAO.md`.
- Regras gerais (migration aplicada imutável, destrutiva em duas etapas, concorrência em saldo): `docs/padroes/banco-de-dados.md`.

## 6. Arquivos e uploads

- DEVE: uploads fora da pasta do deploy, em `UploadStorage:RootPath` (`/app/uploads`, volume `uploads`), servidos em `/uploads`. A pasta se chama `uploads` porque as URLs no banco começam com `/uploads/`.
- DEVE: o banco só recebe a URL depois que o arquivo existe (`.tmp` → flush → move); sem arquivo confirmado, URL nula.

## 7. Nova feature de ponta a ponta

Ordem sugerida (o comando `/nova-feature` segue estes passos):

1. **Entidade** em `Ofertio.Core/Entities/` (herda `BaseEntity`) + configuração no `AppDbContext` (índices, `HasMaxLength`, conversor criptografado se houver segredo, filtro de inquilino).
2. **Migration** com `/nova-migration`.
3. **Filter** em `Ofertio.Database/Filter/` + extensão `ApplyFilter`.
4. **Dto**, **ReadDto** e **ResponseDto** em `Ofertio.Business/Dto/`; códigos de erro novos em `ErrorCodes`.
5. **Regras** em `Ofertio.Business/Service/<Área>/<Assunto>Rules.cs` + testes unitários.
6. **Interface** do serviço em `Ofertio.Business` e **implementação** em `Ofertio.Infrastructure.Services/<Área>/`, registrada no método de extensão da área.
7. **Controller** em `Ofertio.API/Controllers/`: rota em inglês e kebab-case, `[Authorize]`/política/`[RequiresAccess]`, `CancellationToken`, filtro por dono, `[ProducesResponseType]` para os códigos da tabela do §4.
8. **Testes de autorização** (sem token, outro dono) e de serviço.
9. **Tela** em `Ofertio.SharedUi/Components/Pages/<Módulo>/`: registro em `MenuItems`/`DefaultMenuCatalog`, regras de `frontend-ui.md`, ids estáveis.
10. **E2E** em `Ofertio.E2E` para o que a pessoa vê ou clica.
11. **Documentação:** diagrama (se mudou fluxo), checklist, `.env.example` (se há configuração nova).

## 8. Testes do projeto

- DEVE: xUnit + Moq em `Ofertio.Tests`; EF Core InMemory só para serviço sem transação ou SQL específico.
- DEVE: testes novos em `Unit/<Área>/` ou `Integration/<Área>/`. As pastas `UnitTests/`, `IntegrationTests/`, `APITests/`, `Services/` e `ServicesTests/` são legado e não recebem arquivo novo.
- DEVE: telas Blazor testadas pelos serviços HTTP e helpers extraídos (sem bUnit) e pelo `Ofertio.E2E`.
- Regras gerais: `docs/padroes/testes.md`. Regras do robô de tela: [frontend-ui.md §10](./frontend-ui.md).
