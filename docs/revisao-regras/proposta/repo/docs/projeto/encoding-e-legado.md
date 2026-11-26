# Encoding, cultura e identificadores legados

> Substitui `docs/rules/encoding-standards.md`. Idioma e nomenclatura geral de C# foram para `docs/padroes/codigo-csharp.md` §1; nomes específicos do Ofertio ficam aqui.

## 1. Encoding

Decisão de 25/11/2026: o BOM obrigatório vale só para `.ps1`. A exigência ampla nasceu para o IIS, que saiu.

- DEVE: todo arquivo de texto em UTF-8.
- DEVE: scripts `.ps1` em UTF-8 **com BOM**. Motivo: o Windows PowerShell 5.1 lê arquivo sem BOM como ANSI e quebra acentos e strings.
- Os demais arquivos (`.cs`, `.razor`, `.md`, `.json`...) podem ter ou não BOM. NÃO DEVE: remover em massa o BOM dos arquivos existentes; o diff não traz benefício.
- NÃO DEVE: BOM em `.json` de configuração do Claude Code, `.yml` do GitHub e `.sh` (ferramentas Linux podem rejeitar).
- Garantias automáticas: `.editorconfig` (`[*.ps1] charset = utf-8-bom`) e o hook `PostToolUse` do Claude Code (`.claude/hooks/garantir-bom.ps1`, só em `.ps1`). O `NormalizarEncodingPrecoce.ps1` passa a ter `*.ps1` como padrão e deixa de ser passo obrigatório antes do build.

## 2. Cultura

- DEVE: `Ofertio.WebUi` em `pt-BR` (`DefaultThreadCurrentCulture` e `DefaultThreadCurrentUICulture`): datas, moeda e textos.
- DEVE: datas gravadas em UTC (`datetime2`) e exibidas no fuso do usuário (`IUserClock`) ou de Brasília no Master. Detalhe em [frontend-ui.md §8](./frontend-ui.md).

## 3. Nomes específicos do Ofertio

| Tipo | Padrão | Exemplo |
|---|---|---|
| Objetos por entidade | `[Nome]Filter`, `[Nome]Dto`, `[Nome]ReadDto`, `[Nome]ResponseDto` | `EstoqueFilter` |
| Rotas novas | inglês, plural, kebab-case | `api/sales-orders/{id:int}` |
| Código de erro | `UPPER_SNAKE_CASE` em inglês, em `ErrorCodes` | `INSUFFICIENT_CREDITS` |
| Serviço | `I[Nome]Service` / `[Nome]Service` | `IMyPlanService` |
| Regras puras | `[Assunto]Rules` (estática) | `MenuHierarchyRules` |
| Opções | `[Nome]Options` com `const string SectionName` | `Integrations:MercadoLivre` |
| Estratégia de link | `[Plataforma]LinkStrategy` | `ShopeeLinkStrategy` |
| Páginas Blazor | PascalCase, pasta por módulo | `Pages/Busca/BuscaGlobal.razor` |
| CSS | kebab-case com prefixo da tela/componente | `busca-*`, `multiselect-*` |
| JavaScript | objeto global `window.ofertio*` | `ofertioMultiSelect` |
| Id de campo | `{tela}-{campo}` | `propaganda-preco` |

## 4. Identificadores legados (não renomear)

Herdados da marca TecFlow e gravados em dados, URLs ou credenciais. Renomear quebra dados existentes (ADR 0004).

- Data Protection: `ApplicationName="TecFlow"` e purpose `TecFlow.MarketplaceTokens.v1`.
- Migration `InitialTecFlow`.
- Prefixo `tecflow-u` das instâncias do WhatsApp e o usuário `demo@TecFlow.local`.
- IDs de afiliado/rastreio `tecflow_*`, `utm_source=tecflow` e `matt_word` "tecflow".
- Credenciais sandbox `tecflow-homolog-*`.
- Propriedade `Descricao` do envelope `ResponseDto` (contrato com a WebUi e o Mobile).
- Rotas existentes em português (`api/Afiliados`, `api/Campanhas`, `api/conta`, `api/vendas/*`, `api/integracoes/*` etc.): não são renomeadas. Rota nova é em inglês, mesmo dentro de um controller antigo.
