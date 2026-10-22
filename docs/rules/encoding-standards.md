# 🔤 Padrões de Encoding & Nomenclatura

> Evita acentuação quebrada (`??`, caracteres corrompidos) no portal, no IIS e no banco, e mantém nomes consistentes entre camadas.

## 1. UTF-8 com BOM (obrigatório)

- Todo arquivo criado ou modificado (`.cs`, `.razor`, `.css`, `.js`, `.json`, `.html`, `.md`, `.ps1`) é salvo em **UTF-8 com assinatura (BOM)**, para o IIS e o compilador lerem os acentos corretamente no Windows.
- Antes de publicar, normalize:

```powershell
$WarningPreference='SilentlyContinue'; .\NormalizarEncodingPrecoce.ps1 *> $null
```

- O script cobre `*.cs`, `*.razor`, `*.json`, `*.css` e `*.html` por padrão; para documentação use `-Extensions '*.md'` (com `-SolutionRoot` apontando para a pasta desejada).

## 2. Cultura e localização (pt-BR)

- O `Ofertio.WebUi` roda em `pt-BR`: a inicialização define `DefaultThreadCurrentCulture` e `DefaultThreadCurrentUICulture`, garantindo datas, moeda e textos consistentes.
- Datas gravadas em UTC (`datetime2`) e exibidas no horário de Brasília.
- Conexões PostgreSQL legadas usam `Client Encoding=UTF8;Encoding=UTF8;` (o banco oficial é o SQL Server).

## 3. Idioma

- **Código em inglês:** classes, métodos, variáveis, propriedades, rotas de API e chaves de configuração.
- **Português:** comentários, documentação, mensagens de log e todo texto exibido ao usuário.

## 4. Nomenclatura

- **Dados:** `[Nome]Filter`, `[Nome]Dto`, `[Nome]ResponseDto` + entidade ([backend-architecture.md](./backend-architecture.md#3-arquitetura-racional-de-dados-apenas-3-objetos-por-entidade)).
- **Serviços:** `I[Nome]Service` / `[Nome]Service`; regras puras e testáveis em classes estáticas `[Assunto]Rules`.
- **Opções:** `[Nome]Options` com `const string SectionName` (ex.: `Integrations:MercadoLivre`).
- **Estratégias de link:** `[Plataforma]LinkStrategy`.
- **Blazor:** componentes e páginas em PascalCase; páginas em pastas por módulo (ex.: `Pages/Busca/BuscaGlobal.razor`).
- **CSS:** classes kebab-case com prefixo da tela ou do componente (`busca-*`, `multiselect-*`).
- **JavaScript:** objetos globais `window.ofertio*` (ex.: `ofertioMultiSelect`, `ofertioClipboard`).
- **Migrations:** PascalCase descritivo (ex.: `AddProductTags`).
- **Commits:** Conventional Commits (`fix(busca): ...`), registrados em `docs/GIT_VERSAO.md`.

## 5. Identificadores legados (não renomear)

Herdados da marca TecFlow e gravados em dados, URLs ou credenciais:

- Data Protection: `ApplicationName="TecFlow"` e purpose `TecFlow.MarketplaceTokens.v1`.
- Migration `InitialTecFlow`.
- Prefixo `tecflow-u` das instâncias do WhatsApp e o usuário `demo@TecFlow.local`.
- IDs de afiliado/rastreio `tecflow_*` (ex.: `tecflow_kabum`, `tecflow_cb`), `utm_source=tecflow` e `matt_word` "tecflow".
- Credenciais sandbox `tecflow-homolog-*` e o histórico de `docs/GIT_VERSAO.md`.
