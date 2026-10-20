# 📝 Ofertio - Roadmap, Arquitetura & Contexto Geral

> **Painel principal do projeto** (antigo `TODO.md`). Regras de código, stack e checklist **ativo** (Fases 32 a 34). O histórico das Fases 1 a 31 está em [`docs/HISTORICO_FASES_CONCLUIDAS.md`](./docs/HISTORICO_FASES_CONCLUIDAS.md) (ver `.cursorrules`).

## 🎯 Objetivo do Projeto
Plataforma de **automação e inteligência para afiliados de alta escala**: orquestração de engajamento (comentários, mensagens e links), conciliação financeira de comissões, catálogo de produtos de divulgação e integrações com TikTok Shop e Shopee. O ecossistema combina backend robusto em C# (`Ofertio.API`, `Ofertio.Worker`, `Ofertio.Orquestrador`) com o frontend **`Ofertio.WebUi`** para controle, auditoria e monitoramento em produção.

## 🛠️ Stack Tecnológica Definida
- **Backend:** .NET 8.0 Web API (C#)
- **Frontend:** Blazor WebApp — projeto **`Ofertio.WebUi`** (ASP.NET Core .NET 8.0)
- **Banco de Dados:** SQL Server (`localhost\SQLEXPRESS` / `Ofertio`) via `Database:Provider=SqlServer` no Development e na Homologação IIS; PostgreSQL permanece apenas como assembly de migrations legado em `Ofertio.Infrastructure`.
- **ORM:** Entity Framework Core 8 — `UseSqlServer` (`Ofertio.Data`) ou `UseNpgsql` (`Ofertio.Infrastructure`) conforme o provider

---

### 🌐 Diretrizes de Codificação, Localização e Encoding (Padrão Obrigatório)

Para evitar que a acentuação em PT-BR fique quebrada (ex: exibir '??' ou caracteres corrompidos no portal ou banco de dados), todo desenvolvedor e IA parceira deve seguir rigorosamente estas regras:

1. **Encoding de Arquivos (UTF-8 com BOM):**
   - Todos os arquivos de código-fonte criados ou modificados (`.razor`, `.cs`, `.html`, `.css`, `.json`) **DEVEM** ser salvos explicitamente utilizando a codificação **UTF-8 com assinatura (BOM)**. Isso garante que o IIS e o compilador do .NET processem os acentos corretamente em ambientes Windows/Server.

2. **Cultura e Localização Nativa (PT-BR):**
   - O portal frontend `Ofertio.WebUi` opera sob a cultura `pt-BR`. O pipeline de inicialização configura globalmente as propriedades `DefaultThreadCurrentCulture` e `DefaultThreadCurrentUICulture` para garantir consistência em formatações de data, moeda e decodificação textual.

3. **Persistência de Dados:**
   - SQL Server (`Database:Provider=SqlServer`): `UseSqlServer` e migrations em `Ofertio.Data`.
   - PostgreSQL (Homologação IIS): driver `Npgsql` com `Client Encoding=UTF8;Encoding=UTF8;` na connection string.

---

## 📚 Documentação Complementar
* Veja a [Lista de Mudanças de Arquivos](./docs/LISTA_ARQUIVOS_MUDANCAS.md)
* Veja o [Registro Histórico de Fases Concluídas](./docs/HISTORICO_FASES_CONCLUIDAS.md)
* Veja a [ANALISE WORKSPACE COMPLETA](./docs/ANALISE_WORKSPACE_COMPLETA.md)
* Veja a [INDICE COMPLETO](./docs/INDICE_COMPLETO.md)
* Veja a [RESUMO EXECUTIVO](./docs/RESUMO_EXECUTIVO.md)
* Veja a [DIAGRAMAS ARQUITETURA](./docs/DIAGRAMAS_ARQUITETURA.md)
* Consulte os [Comandos Úteis do Git](./docs/ComandosGit.txt)

## 📐 Padrões de Código e Nomenclatura (Strict Rules)
Sempre que criar ou editar código neste projeto, você DEVE seguir estes padrões estritos:

1. **Idioma:** Código em inglês (classes, métodos, variáveis), comentários e documentação em português.

2. **Arquitetura Racional de Dados (Apenas 3 Objetos por Entidade):** Para evitar redundância de arquivos (como Create/Update Dtos), cada entidade deve possuir estritamente:
   - **`[Nome]Filter`** (na pasta `Ofertio.Database/Filter/`): Contém as propriedades escalares da Entity como opcionais/nullable (`?`). Usado **EXCLUSIVAMENTE** para parâmetros de busca em listagens e consultas (**GET**). Não incluir objetos de navegação — apenas IDs (`CampaignId`, `OwnerId`, etc.).
   - **`[Nome]Dto`** (na pasta `Ofertio.Business/Dto/`): Contém as propriedades limpas que a tela envia para o formulário. Usado **EXCLUSIVAMENTE** para receber dados de escrita (**POST** e **PUT**). Não deve conter objetos complexos de navegação inteiros, apenas seus IDs correspondentes.
   - **`[Nome]ResponseDto`** (na pasta `Ofertio.Business/Dto/`): Envelope padrão de retorno que encapsula propriedades de controle (`Status` [bool], `Descricao` [string]) e a Entity real (`Data` [[Nome]?] e `DataList` [List<[Nome]>?]).

   > A **Entity** continua em `Ofertio.Core/Entities/` ou `Ofertio.Database/Entity/`. Exemplo `Estoque`: `EstoqueFilter`, `EstoqueDto`, `EstoqueResponseDto` + Entity.

3. **Padrão de Retorno de Métodos (Controllers e Services):** Métodos **GET** retornam `[Nome]ResponseDto` com `Data` ou `DataList`. Métodos **POST/PUT** recebem `[Nome]Dto` e retornam `[Nome]ResponseDto` quando aplicável.

4. **Filtros e Consultas:** Toda listagem (**GET**) deve aceitar `[FromQuery] [Nome]Filter filter`, aplicar o filtro na query (repositório ou extensão `ApplyFilter`) e paginar via `Pagin` (máximo 30 registros).

5. **Criptografia de credenciais:** Qualquer campo, propriedade ou dado que se refira a senhas ou tokens de acesso (como senhas de usuários, tokens do TikTok ou Shopee) DEVE ser criptografado antes de ser salvo no banco de dados e descriptografado apenas no momento do uso.

6. **Validações Obrigatórias:** Cadastros de e-mail, celular/WhatsApp, CPF, CNPJ alfanumérico e CEP devem passar estritamente pelos métodos do `ValidationHelper` em `Ofertio.Business/Service` antes de qualquer persistência.

7. **Design Responsivo & Mentalidade Mobile First:** Qualquer novo componente visual, página ou layout criado no projeto `Ofertio.WebUi` DEVE obrigatoriamente contemplar design responsivo (Mobile First). É proibido o uso de larguras fixas em pixels para containers principais e tabelas densas; utilize grids fluidos, flexbox e classes utilitárias responsivas (Bootstrap/Tailwind) para garantir que a interface se adapte nativamente a PCs, Celulares e Tablets.

8. **Auto-atualização do Roadmap:** Ao concluir uma tarefa das Fases **32 a 34** neste arquivo, marque `[ ]` para `[x]`. Quando a fase inteira estiver concluída, **mova o bloco** para `docs/HISTORICO_FASES_CONCLUIDAS.md`. Sincronize também `docs/LISTA_ARQUIVOS_MUDANCAS.md` e `docs/DIAGRAMAS_ARQUITETURA.md` conforme `.cursorrules`.

## 🧪 Suíte de Testes e Qualidade (Ofertio.Tests)

Diagnóstico das Fases **8** (auth/multi-loja), **10** (links backend), **11** (gerador UI) e **19** (homolog Shopee): a suíte cobre serialização JSON de DTOs, envio do formulário de vínculo manual, escopo `lojaId`, 401 sem JWT e envelopes 500 em vez de exceção não tratada. Telas Blazor (`MinhasLojas.razor`, `GeradorLinks.razor`) são validadas via serviços HTTP e o validador extraído do formulário (`ConnectStoreManualLinkForm`), sem bUnit. A lista detalhada de testes já cobertos está no [histórico de fases](./docs/HISTORICO_FASES_CONCLUIDAS.md).

### 💻 Comandos no terminal
- **Toda a suíte:** `dotnet test`
- **Projeto isolado:** `dotnet test .\Ofertio.Tests\Ofertio.Tests.csproj`
- **Logs detalhados:** `dotnet test --logger "console;verbosity=detailed"`
- **Filtro por módulo:** `dotnet test --filter "FullyQualifiedName~Shopee"`
- **Filtro auth/loja:** `dotnet test --filter "FullyQualifiedName~Integracoes|FullyQualifiedName~AuthController"`

## ⚠️ REGRAS ARQUITETURAIS INVIOLÁVEIS (NÃO ALTERAR)

1. **BANCO DE DADOS OFICIAL:**
   - O único banco de dados oficial do projeto é o **SQL Server**.
   - O nome do banco de dados na string de conexão deve ser estritamente **`Ofertio`**.
   - O provider do Entity Framework Core deve ser obrigatoriamente **`SqlServer`** (`Microsoft.EntityFrameworkCore.SqlServer`).
   - É **EXTREMAMENTE PROIBIDO** alterar a configuração do `appsettings.json` para `PostgreSQL` / `Npgsql` ou criar bancos com outros nomes (como `OfertioDb`).

2. **COMPATIBILIDADE E ENCODING:**
   - Todos os arquivos editados ou criados devem ser salvos com a codificação **UTF-8 com BOM**.
   - As migrations do EF Core devem ser geradas e executadas exclusivamente para a sintaxe T-SQL / SQL Server direcionadas ao banco `Ofertio`.

PRINCIPIO DE PRESERVAÇÃO TOTAL (REGRA INVIOLÁVEL):
1. Altere APENAS o arquivo e o trecho de código explicitamente solicitados nesta instrução.
2. É EXTREMAMENTE PROIBIDO remover, sobrescrever ou simplificar componentes Blazor (.razor), métodos C#, DTOs, links do NavMenu.razor ou regras de negócio existentes que não façam parte do escopo desta alteração.
3. Parta sempre da versão mais recente salva e validada no Git.
4. Salve todos os arquivos na codificação UTF-8 com BOM (REGRA 1).
---

  ### 📜 Regra Estrita de Registro de Novas Telas e Árvore Dinâmica de Menus

1. **Obrigatoriedade de Registro no Banco de Dados (`MenuItems`):**
   - É **estritamente proibido** codificar links e submenus fixos no arquivo `NavMenu.razor`.
   - Toda nova página, rota ou componente do sistema DEVE obrigatoriamente possuir um registro correspondente na tabela `MenuItems` do SQL Server (incluindo `Title`, `Route`, `Icon`, `OrderIndex` e `ParentId`).
   - O seeding automático deve garimpar todas as rotas e garantir que o item apareça imediatamente no painel `/master/menu-permissions` para atribuição de permissões por plano.

2. **Hierarquia Dinâmica & Drag & Drop (`OrderIndex` & `ParentId`):**
   - O `NavMenu.razor` do cliente deve consultar as tabelas `MenuItems` e `PlanMenuPermissions` e renderizar os componentes estritamente respeitando a ordenação (`OrderIndex`) e os agrupamentos (`ParentId`) definidos pelo Master.
 ---  

## 🛠️ ROADMAP DE PLATAFORMAS (MULTI-MARKETPLACE)

O **Ofertio** foi projetado sob o padrão **Strategy Pattern** para permitir a adição plug-and-play de novas plataformas de afiliados e marketplaces.

### 📌 Status de Suporte por Plataforma

| Plataforma | Link Resolver / Deep Link | API de Afiliados | Status |
| :--- | :--- | :--- | :--- |
| **Shopee** | `ShopeeLinkStrategy.cs` | Suportado (br.shp.ee / app) | 🟢 **Concluído (Homologação)** |
| **TikTok Shop** | `TikTokShopLinkStrategy.cs` | Link de afiliado `shop.tiktok.com/view/product/{id}?sub_id=` | 🟢 **Concluído (Homologação)** |
| **Mercado Livre** | `MercadoLivreLinkStrategy.cs` | `matt_tool` + `matt_word` em `/p/{MLB}` e `/sec/` | 🟢 **Concluído** |
| **Amazon** | `AmazonLinkStrategy.cs` | `tag` de associado em `/dp/{ASIN}` (expande `amzn.to` / `a.co`) | 🟢 **Concluído** |
| **Kabum!** | `KabumLinkStrategy.cs` | `sub_id` + `utm_source=afiliado` em `/produto/{id}` (expande `kb.um`) | 🟢 **Concluído** |
| **Casas Bahia** | `CasasBahiaLinkStrategy.cs` | `parceiro` + `sub_id` em `/p/{id}` (expande `cb.com.br`) | 🟢 **Concluído** |
| **AliExpress** | `AliExpressLinkStrategy.cs` | Em planejamento | ⏳ **Backlog** |
| **Magazine Luiza** | `MagazineLuizaLinkStrategy.cs` | Magazine Você `/{loja}/p/{id}/` ou `?parceiro=` (expande `magalu.me`) | 🟢 **Concluído** |

## 🎨 Padronização Visual de Plataformas (Badges & Branding)

Sempre que exibir o nome, tag ou selo de uma plataforma no Ofertio (listas, buscas, cards ou formulários), utilize a paleta de cores corporativas oficial com texto em alto contraste:

- **Amazon:** `#FF9900` (Laranja) | Texto: `#FFFFFF`
- **Mercado Livre:** `#FFE600` (Amarelo) | Texto: `#000000` (ou `#2D3277`)
- **Shopee:** `#EE4D2D` (Laranja/Vermelho) | Texto: `#FFFFFF`
- **Magazine Luiza (Magalu):** `#0086FF` (Azul) | Texto: `#FFFFFF`
- **TikTok Shop:** `#000000` (Preto) | Texto: `#FFFFFF` (Com detalhe em Cyan `#00F2FE` / Rosa `#FE2C55` se aplicável)
- **Hotmart:** `#FF5200` (Laranja Queimado) | Texto: `#FFFFFF`
- **Braip:** `#12B76A` (Verde) | Texto: `#FFFFFF`
- **Outros / Genérico:** `#6B7280` (Cinza) | Texto: `#FFFFFF`

### Componente Reutilizável de Badge:
Deve seguir o padrão de cantos arredondados (`rounded-full` ou `rounded-md`), fonte em negrito (`font-semibold`) e tamanho compacto (`text-xs px-2.5 py-1`).

## 🔲 Padronização de Modais e Diálogos de Confirmação

Para manter a consistência visual e a elegância da interface em toda a plataforma Ofertio, **é estritamente proibido o uso de diálogos nativos do navegador** (`window.confirm`, `window.alert` ou `window.prompt`).

### Diretrizes de Modais:
1. **Ações Críticas (Exclusão / Edição / Desconexão):**
   - **Nunca** utilize `confirm()` nativo.
   - Utilize obrigatoriamente um componente de **Modal customizado** (ou biblioteca estilizada como SweetAlert2 / Blazor Bootstrap Modal).
   - O modal de confirmação de exclusão deve conter:
     - Título claro (ex: *Confirmar Exclusão*).
     - Ícone de alerta (ex: Lixeira vermelha ou Triângulo de Aviso).
     - Descrição com o nome do item a ser excluído.
     - Botão de ação destrutiva destacado em vermelho (ex: *Sim, excluir*).
     - Botão de cancelamento neutro (ex: *Cancelar*).

2. **Acessibilidade & UX:**
   - O modal deve fechar ao clicar na tecla `ESC` ou fora dele (backdrop click).
   - Deve ser utilizado de forma global e padronizada em todos os módulos da aplicação.

## ⏰ Regra de Validação de Horário na Edição de Agendamentos

Ao carregar um agendamento para edição na tela de disparo:
- **Verificação de Data Futura:** Se a `DataAgendada` for maior que o horário atual (`DataAgendada > DateTime.Now`), **mantenha exatamente a data e hora originais**.
- **Ajuste Apenas para Agendamentos Retroativos:** Caso a data/hora original já tenha passado (`DataAgendada <= DateTime.Now`), ajuste automaticamente o campo para **10 minutos à frente** do horário atual (`DateTime.Now.AddMinutes(10)`).
- **Feedback:** Exiba um aviso informativo apenas quando o ajuste de +10 minutos for aplicado.

## 🔐 Autenticação da Telegram Client API (UserBot MTProto)

1. **Obtenção de Credenciais:** As chaves `App API ID` e `App API Hash` devem ser geradas gratuitamente pelo usuário no portal oficial `https://my.telegram.org`.
2. **First-Time Auth (Login por Código):** Na primeira execução, o serviço solicita o código de autenticação via SMS/App do Telegram para gerar o arquivo de sessão local (`.session`).
3. **Persistência de Sessão:** Após o primeiro login efetuado com sucesso, a conexão permanece ativa por tempo indeterminado sem necessidade de novos logins.

## 🛠️ Diagnóstico e Boas Práticas para Ações de Tabela (Blazor Modais)

Para evitar que botões de ação em listas/tabelas fiquem inativos após refatorações:
- **Binding de Eventos:** Garanta sempre o uso de `@onclick="() => AbrirModalExclusao(item.Id)"` para evitar chamadas automáticas durante a renderização.
- **Renderização Reativa de Modais:** Ao disparar um modal customizado, altere o flag de exibição (ex: `isDeleteModalOpen = true`) e invoque `StateHasChanged()` explicitamente.
- **Isolamento de Componente:** Mantenha o componente de Modal fora da tag `<table>` ou do loop `@foreach`, posicionando-o no final do arquivo `.razor`.

## 🖼️ Gerenciamento e Auto-Preenchimento de Imagens de Produtos

1. **Captura na Conversão de Links (`/links`):**
   - Ao converter qualquer link de comissão, o crawler/parser do backend deve extrair a tag `og:image` ou a imagem principal da página do produto e armazená-la no campo `ProductImageUrl`.
   - Se a imagem não for localizada automaticamente, o afiliado poderá informar ou editar a URL da imagem manualmente.

2. **Preenchimento Automático no Agendador (`/integracoes/whatsapp/agendador`):**
   - Ao selecionar um link de comissão no filtro/autocomplete da tela de agendamento, o campo `URL da imagem (opcional)` é preenchido automaticamente com a imagem salva do produto.
   - O afiliado mantém total liberdade para alterar a URL ou limpá-la a qualquer momento antes de agendar.

3. **Preview da Imagem na Interface:**
   - O campo "URL da imagem" deve contar com um elemento de **Preview Visual (Thumbnail)** de 80x80px que exibe a foto em tempo real assim que uma URL válida estiver presente.

## 🗂️ Arquitetura do Menu Lateral (Sidebar Navigation)

O menu lateral do Ofertio deve seguir rigorosamente a seguinte estrutura hierárquica e agrupamento de módulos:

1. **Mensageria & Bots** (Core operacional diário):
   - Conexões (WA & Telegram)
   - Agendador de Grupos
   - Bot de Conversão

2. **Canais Específicos:**
   - **WhatsApp:** Agendador WhatsApp
   - **Telegram:** Agendador Telegram

3. **Integrações & Afiliados:**
   - Minhas Lojas / Integrações *(Única ocorrência do módulo)*
   - Gerador de Links
   - Minhas Páginas Públicas

4. **Próximos Desenvolvimentos** (Submenu retrátil/accordion para recursos em roadmap/gestão):
   - Dashboard
   - Campanhas
   - Métricas / Comissões
   - Fila de engajamento *(badge “Em breve”)*
   - Conciliação financeira
   - Produtos propaganda
   - Saúde do sistema
   - Minha conta / Segurança

## 📱 Comportamento de Submenus Retrateis (Accordion / Collapsible Nav)

Para todos os submenus interativos da Sidebar (como "Próximos Desenvolvimentos"):
- **Controle de Estado Local:** O componente da Sidebar deve gerenciar o estado de abertura (`isExpanded`) via código C# no Blazor Server/Wasm.
- **Isolamento de Manipulação do DOM:** Evite dependências de JavaScript externo (Bootstrap JS / Alpine) para alternar visibilidade de menus se o estado puder ser controlado diretamente via binding `@onclick` e classes condicionais do Blazor.
- **Acessibilidade e Transição:** Utilize transições CSS suaves (`transition-all duration-200`) e altere o ícone do indicador (chevron para baixo `v` / chevron para o lado `>`) conforme o menu expande ou recolhe.

---

## 🏛️ GUIA DE ARQUITETURA PARA NOVAS INTEGRAÇÕES (EXTENSIBILIDADE)

Instruções para o Cursor Agent / Desenvolvedores ao adicionar uma nova plataforma no backend:

1. **Camada de Domínio e Dados:**
   - Adicionar o Enum da plataforma em `PlatformType.cs`.
   - Garantir que a tabela `MarketplaceAccounts` no SQL Server armazene as credenciais (`AppKey`, `AppSecret`, `TrackingId`) associadas à nova plataforma.

2. **Implementação da Estratégia (`Ofertio.Domain` / `Ofertio.Services`):**
   - Criar a classe `NomePlataformaLinkStrategy.cs` implementando a interface `IPlatformLinkStrategy`.
   - Implementar o método `CanHandle(string url)` para reconhecer os domínios da plataforma via Regex.
   - Implementar a sanitização do link, injeção das tags de afiliado e a chamada ao SDK/API oficial para encurtamento.

3. **Injeção de Dependência:**
   - Registrar a nova estratégia no container de IoC em `Program.cs`:
     `builder.Services.AddScoped<IPlatformLinkStrategy, NomePlataformaLinkStrategy>();`

4. **Retorno Padronizado (DTO Único):**
   - Todas as estratégias DEVEM preencher obrigatoriamente o DTO `ConvertLinkResponseDto`:
     - `OriginalUrl`: Link bruto colado pelo usuário.
     - `AffiliateUrl`: Link direto da plataforma com tag de afiliado.
     - `ShortenedShopeeUrl` / `ShortenedPlatformUrl`: Link reduzido oficial da plataforma.
     - `OfertioTrackingUrl`: Link interno de telemetria `/{storeSlug}/{code}`.

## 🔒 Segurança de Dados, Criptografia e Conformidade (LGPD)

Para manter a conformidade com a **Lei Geral de Proteção de Dados (LGPD)** e garantir a integridade das integrações, este projeto utiliza **Criptografia em Repouso (At-Rest Encryption)** para todas as credenciais sensíveis gravadas na base de dados.

### ⚠️ Regras Obrigatórias de Desenvolvimento e Deploy

1. **Proteção de Tokens e Segredos (`DataProtection`):**
   - Todos os dados sensíveis (`AccessToken`, `RefreshToken`, `ClientSecret` e Chaves de API) **devem obrigatoriamente** passar pelo `ISecurityEncryptionService` / `ValueConverters` do Entity Framework Core.
   - **Nunca** grave tokens ou segredos em texto puro (*Plain Text*) na base de dados.

2. **Gestão do Diretório de Chaves de Criptografia:**
   - As chaves de proteção do `DataProtection` residem em `C:\inetpub\ofertio\api\App_Data\OfertioDataProtectionKeys` (`DataProtection:KeysPath = App_Data/OfertioDataProtectionKeys`, relativo à raiz da API); o Development aponta para o mesmo anel de chaves porque o banco `Ofertio` é compartilhado.
   - **Atenção no Deploy/Publish:** a limpeza da pasta da API **deve preservar `App_Data`**. A eliminação das chaves tornará impossível a descriptografia dos tokens salvos no banco, exigindo que todos os utilizadores reautorizem as suas contas de marketplace.
   - O nome de aplicação do Data Protection (`"TecFlow"`) e o purpose `TecFlow.MarketplaceTokens.v1` são identificadores dos payloads já gravados e **não podem ser renomeados**.

3. **Cadeia de Fallback Mantida (`Unprotect`):**
   - O serviço de segurança deve manter a cadeia de compatibilidade retroativa:
     $$\text{DataProtection} \longrightarrow \text{AES (Legado ENC1:)} \longrightarrow \text{Texto Puro}$$
   - Ao alterar qualquer entidade que contenha segredos, verifique se a conversão transparente no `OnModelCreating` do `ApplicationDbContext` permanece ativa.

4. **Credenciais e Variáveis de Ambiente:**
   - Ficheiros de configuração locais contendo segredos (ex: `appsettings.json`, `.env`) e a pasta de chaves do `DataProtection` estão incluídos no `.gitignore` e **nunca devem ser commitados no repositório**.

---

## 🚀 Fases do Desenvolvimento (Checklist ativo)

As **Fases 1 a 32** (e entregas equivalentes já concluídas) foram movidas para o [Registro Histórico de Fases Concluídas](./docs/HISTORICO_FASES_CONCLUIDAS.md).

O checklist abaixo contém **apenas as fases ativas e pendentes**.

 

  ## 🔐 Resolução OAuth e Fallback na Busca do Mercado Livre (`MLBSearchOAuthFallback`)

- [x] **Utilização do App Access Token / Client Credentials:**
  - Em buscas globais do Mercado Livre sem token de usuário individual, o `MercadoLivreApiService` deve utilizar o token de aplicação obtido via `grant_type=client_credentials` com o `AppId` e `SecretKey` do Vault Master.
  - Credenciais lidas via `IOptionsMonitor` (refletem o Vault sem reiniciar a API); token de aplicação em cache até 5 min antes de expirar e descartado se a API o recusar.
- [x] **Fallback de vitrine em HTTP 401/403 ou erro de escopo:**
  - Raspagem automática de `https://lista.mercadolivre.com.br/{query}` (`MercadoLivreHtmlParser`); página de verificação anti-bot é detectada e vira erro amigável no badge.
- [x] **Injeção Transparente do Affiliate ID (`matt_tool`):**
  - Garantia de que todos os produtos retornados na busca incorporem o `matt_tool` (`14343296`) do tenant nas URLs finais.
- [x] **Badge do Mercado Livre com contagem real:**
  - `OfficialCatalogSearchRules.CountVisibleOffers` conta apenas as ofertas exibidas (filtros + URLs únicas); a `/busca` limpa o alerta de erro assim que há produtos.
- [x] **RedirectUri limpa em túneis Ngrok:**
  - `OAuthRedirectUriNormalizer` (Business) aplicado em `MarketplaceAuthService` (authorize + troca do code), `IntegracoesEndpointExtensions` e `MinhasLojas.razor`: hosts `ngrok-free.dev` / `ngrok-free.app` / `ngrok.io` / `ngrok.app` saem sempre como `https://{host}/integracoes/oauth/callback`, sem porta local (`:7002` / `:5000`), query ou fragmento.
- [x] **Callback do Mercado Livre sem sessão no navegador:**
  - `/integracoes/oauth/callback` com `.AllowAnonymous()`: não redireciona mais para `?login=required` quando o `state` é selado.
  - `authorize-url` (usuário autenticado) sela `UserId` + `TenantId` + apelido + origem no `state` (`OAuthStateProtector`, AES-256-GCM com chave HKDF de `Encryption:Key`, validade de 15 min, uso único).
  - `POST api/marketplace-auth/mercadolivre/callback` (anônimo) abre o `state`, troca o code e grava `AccessToken` / `RefreshToken` em `MarketplaceAccounts` no tenant do usuário; retorno para `{origem}/marketplace-accounts?status=success` (ex.: `https://localhost:7002/marketplace-accounts?status=success`).


  ## 🔑 Resolução da Injeção de Token OAuth na Busca (`MLBOAuthSearchTokenFix`)

- [x] **Injeção Obrigatória do Access Token por Tenant:**
  - Garantir que o `MercadoLivreApiService` recupere o `AccessToken` válido da tabela `MarketplaceAccounts` correspondente ao `TenantId` logado e inclua o cabeçalho `Authorization: Bearer {AccessToken}` em todas as chamadas para a API `/sites/MLB/search`.
  - Conta ML ativa filtrada pelo `TenantId` do usuário (`Usuarios`), priorizando a do próprio usuário e com token OAuth real; Bearer aplicado por requisição (`HttpRequestMessage.Headers`), sem alterar `DefaultRequestHeaders` do `HttpClient` compartilhado entre tenants.
  - Log no console (`[ML Busca] ... token OAuth armazenado|renovado da conta '@Achadinhos de Aaz' (Id, Tenant)`), sem expor o token.
- [x] **Refresh Token Automático antes da Expiração:**
  - Caso o token esteja expirado ou receba HTTP 401/403, executar o fluxo automático de renovação (`refresh_token`) usando as credenciais do Vault Master antes de registrar falha de busca.
  - Em 401/403: `POST oauth/token` (`grant_type=refresh_token`, `AppId`/`SecretKey` do Vault), grava o novo `AccessToken`/`RefreshToken` em `MarketplaceAccounts` e repete a busca uma vez; só então cai na vitrine `lista.mercadolivre.com.br`.

## 🔑 Injeção Transparente do Token OAuth na API de Busca (`MLBOAuthSearchBinding`)

- [x] **Vinculação Obrigatória do Access Token por Tenant:**
  - Todo consumo do endpoint de busca do Mercado Livre (`/sites/MLB/search`) deve obrigatoriamente resgatar o `AccessToken` ativo da conta vinculada ao tenant atual e adicioná-lo ao cabeçalho HTTP `Authorization: Bearer {AccessToken}`.
  - Entregue em `MLBOAuthSearchTokenFix` (conta do tenant + Bearer por requisição); vale quando `UseOfficialSearchApi = true`.
- [x] **Auto-Refresh de Token em Chamadas de Busca:**
  - Ao receber status HTTP 401 ou HTTP 403 da API oficial, o serviço de busca deve acionar o fluxo de renovação via `refresh_token` utilizando as credenciais do Vault Master antes de registrar falha.
  - Entregue em `MLBOAuthSearchTokenFix`; em produção o ML respondeu `403 PA_UNAUTHORIZED_RESULT_FROM_POLICIES` também ao token renovado (app aguardando homologação no portal).

## 🕸️ Vitrine Pública como Motor Primário da Busca ML (`MLBScraperPrimary`)

- [x] **API oficial desligada até a homologação do app:**
  - `Integrations:MercadoLivre:UseOfficialSearchApi` (padrão `false`): `SearchProductsAsync` não chama `/sites/MLB/search`; com `true` volta o fluxo OAuth + refresh.
- [x] **Scraper `lista.mercadolivre.com.br/{query}`:**
  - Headers de Chrome 129 (`User-Agent`, `Accept`, `Accept-Language`, `sec-ch-ua` `"Google Chrome";v="129", "Not=A?Brand";v="8", "Chromium";v="129"`, `sec-ch-ua-mobile`, `sec-ch-ua-platform`, `Sec-Fetch-*`) e `HttpClientHandler` com `AutomaticDecompression = GZip | Deflate | Brotli`; `MercadoLivreHtmlParser` lê título, preço atual (com centavos), preço original (`andes-money-amount--previous`), imagem (`data-src` em lazy-load) e link de `.ui-search-layout__item` / `.poly-card`.
  - `matt_tool` do tenant injetado em cada oferta (conta ativa `AchadinhoCerto` com `TrackingId` `97303724`; a antiga `@Achadinhos de Aaz` / `14343296` está desconectada).
- [x] **Sem banner de 403 na `/busca`:**
  - Com ofertas, nenhum alerta; sem ofertas por desafio anti-robô (`bot_challenge` / `account-verification`), o badge mostra "vitrine pediu verificação anti-robô" em vez do erro HTTP 403 da API.
- [ ] **Fallback pelo autocomplete do ML (não implementado):**
  - `http2.mlstatic.com/resources/sites/MLB/suggest?q=` responde 404 e `.../autosuggest?q=` responde 403 a chamadas do servidor; além disso só devolve termos de busca (sem preço, imagem ou link), então não gera ofertas. O desafio anti-robô da vitrine não é contornado por código: a saída é a homologação do app (`UseOfficialSearchApi = true`) ou a busca executada no navegador do usuário.

## 🌐 Busca Pública Leve do Mercado Livre (`MLBPublicSearch`)

- [x] **API pública anônima como primeira tentativa:**
  - Com `UseOfficialSearchApi = false`, `SearchProductsAsync` chama `GET https://api.mercadolibre.com/sites/MLB/search?q={query}&limit=20` sem token nem escopos; mapeia `id`, `title`, `price`, `thumbnail` e `permalink`.
  - HTTP 403 (ou qualquer falha) cai na vitrine `lista.mercadolivre.com.br` e, se ela também estiver bloqueada, na mensagem amigável do badge — nenhuma exceção chega à `/busca`.
- [x] **Afiliado oficial `matt_tool=97303724`:**
  - Vem do `TrackingId` da conta ML ativa do tenant (`AchadinhoCerto`, Id 13), sem constante no código; injetado em todas as URLs da API pública e da vitrine.
- [x] **Banner da `/busca`:**
  - Com ofertas retornadas, o alerta de erro é limpo (comportamento de `BuscaGlobal.razor` mantido).
- [x] **Reautorização OAuth verificada:**
  - `14/10/2026 20:53:11`: `oauth/token` 200 e "Tokens ML gravados via callback anônimo" (UserId 1, tenant `a1000000-…`); conta 13 com `AccessToken`/`RefreshToken` criptografados no banco.

## 🔒 Criptografia em Repouso dos Tokens de Marketplace (`MarketplaceTokensDataProtection`)

- [x] **ASP.NET Core Data Protection:**
  - `AddOfertioDataProtection` com `SetApplicationName("TecFlow")` (nome legado mantido para os payloads existentes), chaves em `App_Data/OfertioDataProtectionKeys` da API (configurável em `DataProtection:KeysPath`) e protegidas por DPAPI da máquina; a pasta fica restrita a SYSTEM, Administradores, `IIS APPPOOL\OfertioApiPool` e ao usuário de desenvolvimento.
  - Registrado no `Program.cs` da API e em `AddOfertioInfrastructureData` (API, Worker e Orquestrador compartilham o mesmo anel de chaves).
- [x] **`ISecurityEncryptionService` / `SecurityEncryptionService` (Singleton):**
  - `Protect` grava `DP1:` + payload do `IDataProtector` (purpose `TecFlow.MarketplaceTokens.v1`), sem cifrar duas vezes.
  - `Unprotect` lê `DP1:`, depois o AES legado `ENC1:` e, por fim, devolve o texto puro; falha criptográfica é registrada sem o valor e retorna a string armazenada.
- [x] **`ValueConverter<string, string>` no `AppDbContext`:**
  - `EncryptedStringConverter.CreateProtected` aplicado a `MarketplaceAccount.AccessToken`, `RefreshToken` e `AppSecret` (o "ClientSecret" da entidade); contextos sem o serviço (design-time, testes) seguem no AES.
  - Sem migration: o tipo da coluna não muda; os tokens `ENC1:` existentes são lidos normalmente e regravados como `DP1:` na próxima atualização (ex.: refresh do token).

## 🖼️ Gravação Confiável das Fotos do Telegram (`TelegramMediaPersistenceFix`)

- [x] **Banco só depois do arquivo físico:**
  - `ImageOptimizationService` codifica o WebP em `.tmp`, faz `Flush(flushToDisk: true)` e só então move para o nome final; `OfferProductMediaStore` devolve a URL apenas com `File.Exists` verdadeiro, e `UpdateImageUrlAsync` / `OfferPipelineProcessor` recusam caminhos sem arquivo no disco.
  - O `UPDATE` cru por `ExternalMessageId` saiu de `TelegramUserMonitorHost.PersistTelegramPhotoAsync` (rodava antes de a mensagem existir); o reprocessamento grava filtrando `UserId` + `GroupKey`, porque o id da mensagem do Telegram se repete entre grupos.
  - Sem fire-and-forget: `OnUpdates`, captura histórica e fila de fotos aguardam cada download.
- [x] **Caminho físico via `IWebHostEnvironment.WebRootPath`:**
  - `wwwroot/uploads/products/{tenantId}/{yyyy}/{MM}/{arquivo}` com ano e mês em segmentos invariantes (`BuildRelativeDirectory`) e `Directory.CreateDirectory` automático com log.
- [x] **Falhas com log e URL nula:**
  - Download e conversão WebP em `try/catch` separados com `LogWarning` (tenant, chat, mensagem, bytes, caminho); sem arquivo confirmado a mensagem fica com `ProductImageUrl` nulo (placeholder "Processando mídia..." na tela) e `ClearMissingImageUrlAsync` limpa URLs que apontam para arquivos inexistentes quando o novo download falha.
- [x] **Causa raiz no deploy:**
  - A limpeza `Remove-Item C:\inetpub\ofertio\api\*` apagava `wwwroot\uploads` a cada publicação (198 de 300 mensagens recentes apontavam para arquivos removidos); a homologação passou a preservar `wwwroot\uploads`.

## 📁 Uploads em Pasta Externa ao Deploy (`ExternalUploadsFolder`)

- [x] **Gravação em `C:\inetpub\ofertio\uploads\products\{tenantId}\{year}\{month}`:**
  - `UploadStorage:RootPath` (`appsettings.json` da API e da WebUi) define a pasta servida em `/uploads`; `IUploadStorage` (Singleton) expõe `UploadsRoot` e `MediaRoot` para `ImageOptimizationService`, `OfferProductMediaStore`, `GroupCapturedMessagesService`, `ProductImageCleanupService`, `TenantAdminService`, `OfferMediaStudio` (vídeos/molduras) e `TelegramUserMonitorWorker`.
  - Sem configuração (testes, outra máquina) volta para `{WebRootPath}/uploads`; a pasta precisa se chamar `uploads`, porque as URLs no banco começam com `/uploads/`.
- [x] **Servimento estático:**
  - API: `app.UseStaticFiles` com `PhysicalFileProvider(C:\inetpub\ofertio\uploads)` e `RequestPath = "/uploads"` (antes do `wwwroot`); WebUi: `UseOfertioProductUploads` usa a mesma pasta configurada.
- [x] **Deploy sem perda de mídia:**
  - As 581 fotos existentes foram copiadas de `api\wwwroot\uploads` para `C:\inetpub\ofertio\uploads`; a limpeza literal `Remove-Item C:\inetpub\ofertio\api\*` voltou a ser segura.

## 🧹 Filtros de Integridade e Revalidação de Ofertas (`OfferIntegrityFilters`)

- [x] **Validação prévia na entrada (Telegram/WhatsApp → `OfferPipelineProcessor`):**
  - `OfferIntegrityRules.IsGenericStorefrontUrl` rejeita links cujo caminho contém `/social/`, `/lists/`, `/perfil/` ou `/perfil-vendedor/`, antes da validação HTTP e de novo depois de resolver a URL final (`OfferValidationService`), com o status `VitrineGenerica` e log `Ingestão rejeitada`.
- [x] **Trava de sanidade de preço:**
  - Preço abaixo de R$ 5,00 em produto de alto valor (TV, notebook, smartphone, monitor, console, geladeira etc., ignorando acessórios como capa, película ou cabo) grava `PrecoSuspeito` em vez de `Ativo`; a clonagem fica bloqueada com aviso.
- [x] **"Verificar anúncio" em tempo real:**
  - `LiveCheckSearchService.CheckUrlAsync` faz primeiro o GET da página (seguindo redirecionamentos); vitrine genérica ou marcas de anúncio encerrado/pausado (`Publicação finalizada`, `Anúncio pausado`...) gravam na hora `VitrineGenerica` / `Inativo` com `IsAvailable = false`, e a oferta sai do feed.
- [x] **Revalidação periódica (`OfferRevalidationWorker`):**
  - A cada `OfferRevalidation:IntervalHours` (padrão 2h, `appsettings.json` da API) revalida em lote (`BatchSize`, pausa `DelayBetweenChecksMs`) as ofertas `Ativo` / `PrecoAlterado` / `Verificando` dos últimos 7 dias ainda não verificadas no intervalo; encerradas, pausadas ou redirecionadas para vitrine viram inativas no banco, com resumo no log.

## 🔗 "Abrir Link Original" com Credencial de Afiliado (`AffiliateOpenLink`)

- [x] **`AffiliateUrl` calculado no servidor:**
  - `MonitoredGroupService.ListAsync` resolve uma vez por página o `matt_tool` via `IAffiliateUrlBuilderService` e preenche `GroupCapturedOfferDto.AffiliateUrl` em cada card.
- [x] **`matt_tool` do Mercado Livre:**
  - Vem do `TrackingId` / `AffiliateTrackingId` da conta Mercado Livre ativa do tenant (ex.: `97303724` da `AchadinhoCerto`); sem conta, usa `Integrations:MercadoLivre:DefaultAffiliateTrackingId` (`97303724`). `AffiliateUrlBuilderRules` anexa ou substitui `matt_tool`/`matt_word` mantendo os demais parâmetros, e troca link curto (`meli.la`, `/sec/`) pela URL direta do produto quando ela existe; outros marketplaces abrem a URL original.
- [x] **Nova aba:**
  - O botão continua um `<a target="_blank" rel="noopener noreferrer">` apontando para o link formatado, então o navegador não bloqueia como pop-up.

## 💲 Preço à Vista, Divergência e Esgotado na Shopee (`ShopeePriceIntegrity`)

- [x] **Preço do Telegram sem parcelamento:**
  - `OfferPriceTextRules.TryExtractCashPrice` ignora valores precedidos de multiplicador (`12x R$ 604,92`, `12 x de`, `parcelas de`) ou seguidos de `/mês`, descarta o "De R$ X" quando há "por R$ Y" e devolve o valor à vista (`R$ 6.678,28`); usado por `StructuredOfferParserService`, `GroupOfferCaptureRules.TryExtractTelegramPrice` (que também pula o `12` de `12x`) e pelo fallback `R$` do `ProductMetadataHtmlParser`, que agora descarta preços implausíveis (ex.: `price_min` bruto da Shopee sem escala).
- [x] **Preço real da página e correção no banco:**
  - `ShopeeProductPageRules.TryExtractActivePrice` lê `product:price:amount` / JSON-LD da página da Shopee no `OfferValidationService`; diferença de 30% ou mais para o preço salvo (`IsSignificantDivergence`, ex.: R$ 667,00 × R$ 6.678,28) atualiza `ExtractedPrice` no `LiveCheckSearchService` (botão e worker) e no `OfferPipelineProcessor` (entrada), com log `Divergência de preço corrigida`.
- [x] **Esgotado na Shopee:**
  - `ShopeeProductPageRules.IsSoldOut` detecta "Produto esgotado" e afins, `is_sold_out: true`, `item_status: sold_out` e classes `sold-out`; a oferta vira `Esgotado` com `IsAvailable = false`, a clonagem é recusada no servidor e o botão "Clonar" fica desabilitado no card.

## 🎟️ Aba de Cupons & Promoções (`CouponsTab`)

- [x] **Classificação `OfferType`:**
  - `GroupCapturedMessage.Type` (`Product`/`Coupon`, migration `AddGroupCapturedMessageOfferType`). `CouponMessageRules.Classify` marca `Coupon` quando a mensagem tem `CUPOM`, `CÓDIGO`, `USAR CUPOM`, `R$ N OFF` ou `N% OFF` e não traz link de produto, ou quando nenhum preço à vista sobra após remover os valores do próprio cupom; oferta de produto com cupom continua `Product`.
  - O `OfferPipelineProcessor` grava cupons sem validação de página nem pacote atômico (nome, preço, foto), com plataforma pela URL ou pelo texto; o Feed de Ofertas lista só `Product` e a revalidação periódica ignora cupons.
- [x] **Abas da tela:**
  - `GruposMonitorados.razor` ganhou `[Cupons & Promoções]` ao lado de `[Feed de Ofertas]` e `[Ocultos / Descartados]`, com pills `Todos`, `Mercado Livre`, `Shopee`, `Amazon` e `Outros` (`couponPlatform` na API).
- [x] **`CouponCard.razor`:**
  - Código em destaque com botão copiar (`ofertioClipboard`), desconto, validade e regras extraídas do texto (`CouponMessageRules`), e "Ir para a loja" em nova aba com o `AffiliateUrl` (`matt_tool` no Mercado Livre).

## 🧩 Layout Compacto de Grupos Monitorados (`CompactOffersUi`)

- [x] **Densidade visual:**
  - Cabeçalho com título `h5`, botões `btn-sm` e sem o texto explicativo; o selo "Sincronizando" virou um pill ao lado do título.
- [x] **Toast de status:**
  - Mensagens como "Status atualizado: Ativo." viram toast flutuante com fechar e auto-dismiss em 5 segundos (`ShowToastAsync` versionado, sem um toast antigo apagar o novo); erros continuam em alerta compacto.
- [x] **Busca compacta + Filtros Avançados ⚙️:**
  - Na tela ficam só a busca rápida e o seletor de grupo; Período, Preço mín./máx., Loja e Ofertas por página foram para `AdvancedOfferFiltersModal` (contador de filtros ativos no botão, "Limpar filtros"); o checkbox "Só com cupom" saiu.
- [x] **Auto-scroll ao paginar:**
  - `SetPageAsync` chama `ofertioAppInterop.scrollIntoViewById("ofertas-container", "start")` com rolagem suave.
- [x] **`OfferCard.razor`:**
  - Card extraído da página, com botões em grade 2x2 de linha única (`padding: 4px 8px; font-size: 0.82rem`, ícones, sem quebra de texto) e badges de marketplace/status em pill sutil (`badge-sm`, cores `*-subtle`); `CouponCard` segue o mesmo padrão.

## 🔔 Notificações Toast Globais (`GlobalToasts`)

- [x] **`IToastService` / `ToastService`:**
  - `ShowSuccess`, `ShowError`, `ShowWarning`, `ShowInfo`; cada `ToastMessage` tem ID único (`Guid`), mensagem, tipo e duração (padrão 10 segundos); no máximo 5 visíveis, mensagem repetida do mesmo tipo substitui a anterior. Registrado como Scoped (um por circuito Blazor).
- [x] **`ToastContainer.razor`:**
  - No `MainLayout` e no `MasterMainLayout`, fixo no canto superior direito (`top: 20px; right: 20px; z-index: 9999`), cores #198754 / #dc3545 / #ffc107 / #0d6efd, botão fechar "X", barra de progresso e fechamento automático. Usa `InteractiveServer` próprio porque o layout é SSR estático.
- [x] **Grupos Monitorados migrado:**
  - O banner/toast local de status ("Status atualizado: Ativo.") e os alertas fixos de erro da página viraram `ToastService.ShowSuccess` / `ShowError` / `ShowInfo`; o `CouponCard` avisa a cópia do código pelo serviço global.

## 🎟️ Cupons Fora do Feed de Produtos (`CouponFeedCleanup`)

- [x] **Classificador mais rigoroso (`CouponMessageRules`):**
  - Vira cupom: título de anúncio ("NOVO CUPOM ...", "NOVOS CUPONS ...", "CUPONS ...", "CUPOM SHOPEE/MERCADO LIVRE/AMAZON ...") ou mensagem com 2+ códigos; "Limite de R$ X" e "OFF em R$ X" deixam de contar como preço à vista.
  - Produto com um único cupom ("R$ 391,42 no Pix — Use o cupom OFFHOJE") continua no feed; tokens dentro de URLs (ASIN da Amazon) e pedaços de palavras acentuadas ("RELÂMPAGO") não contam como código; "a partir de R$ X" volta a ser preço do produto.
- [x] **Reclassificação do legado (`CouponReclassificationService` + `CouponReclassificationWorker`):**
  - Roda uma vez a cada inicialização da API (15 s após subir), em lotes de 500, só sobre itens `Product` com sinal de cupom; itens sem link direto só viram cupom se forem anúncio de cupons. Medição no banco atual: ~607 de 9.634 itens migram para a aba Cupons.
- [x] **Filtro explícito `Type == Product`:**
  - Feed (já existia em `MonitoredGroupService.ListAsync`) e agora também mineração, arbitragem, tendências globais e busca local de catálogo.

## 👤 Cabeçalho com Menu de Perfil (`UserProfileMenu`)

- [x] **Elementos obsoletos removidos:**
  - Dropdown "Todas as lojas" (`ActiveStoreScopeSelector`), botão azul "Painel" e badge "Sessão ativa (plataforma)" (`SessionStatusBadge`).
- [x] **Menu de perfil:**
  - Pílula com ícone + nome do usuário; dropdown `dropdown-menu-end` só com Minha Conta (`/minha-conta`), Super Admin (só master) e Sair (`/auth/logout`). Usa `<details>` nativo porque o layout é SSR estático (sem bootstrap.js); fecha ao clicar fora ou com Esc.
- [x] **Header compacto (`HeaderRestore`):**
  - Grupo [Painel Master] [Visão Cliente] e badge de papel do usuário (`MasterProfileSwitcher`) restaurados no formato original, seguidos do menu de perfil, com `d-flex align-items-center gap-2`.
  - `.portal-header` / `.master-header` com `position: relative; z-index: 1050`: o `backdrop-filter` do cabeçalho criava um contexto de empilhamento e o dropdown ficava sob o conteúdo da página.

## 🔗 Vitrines de Terceiros e Afiliado Forçado no Mercado Livre (`ShowcaseBypass`)

- [x] **Bypass de vitrine (`MercadoLivreShowcaseRules`):**
  - URLs com `/social/`, `affiliate-profile` ou `reco_client=home_affiliate-profile` viram a página direta do produto focado (`item_id` ➔ `produto.mercadolivre.com.br/MLB-…`, `product_id` / `/p/MLB…` ➔ `www.mercadolivre.com.br/p/MLB…`); vitrine sem produto focado continua `VitrineGenerica`.
  - Aplicado na validação (link inicial, canônico e redirect final), na ingestão e no "Verificar anúncio", que grava a página direta em `PrimaryProductUrl` (inclusive de itens antigos com `meli.la`).
- [x] **Sanitização do afiliado (`AffiliateUrlBuilderRules`):**
  - Remove `matt_tool_id`, `matt_tool`, `matt_d2id`, `matt_tracing_id` e `matt_word` de terceiros e aplica sempre o `matt_tool` do tenant ou o oficial `97303724` (`MercadoLivreIntegrationOptions.OfficialAffiliateTrackingId`).
- [x] **Botões do `OfferCard`:**
  - "Abrir Link Original" usa o `AffiliateUrl` higienizado; "Clonar Oferta" do Mercado Livre usa a mesma URL (via conversor da loja ML ou direto, quando não houver loja ML conectada).

## 📄 100 Ofertas por Página (`OffersPageSize100`)

- [x] **Padrão de paginação:**
  - `GroupOfferCaptureRules.OffersPageSize = 100` alimenta a tela de Grupos Monitorados, o Modal de Filtros Avançados, a API (`take`) e o cliente HTTP; opções 10 / 25 / 50 / 100 mantidas.
- [x] **Seleção pré-marcada:**
  - "Ofertas por página" abre com `100` selecionado; valor fora da lista volta para 100.
- [x] **Consulta sem teto oculto:**
  - A pré-busca para deduplicação deixou de parar em 80 linhas (agora `take + 20`, até 200), então a página de 100 vem completa.

## 🧭 Vitrines Meli Social, Card Compacto e Reset Limpo (`MeliSocialCardReset`)

- [x] **Bypass obrigatório de `/social/` (ex.: Master Tech Jr):**
  - Links do Telegram/WhatsApp com `mercadolivre.com.br/social/` seguem os redirects HTTP (`UrlResolverService` + GET da validação) até `/p/MLB...` ou `produto.mercadolivre.com.br`.
  - Se a URL final continuar no perfil social, o ID dos parâmetros (`item_id`, `product_id`, `wid`, inclusive no fragmento) vira `https://www.mercadolivre.com.br/p/{ITEM_ID}?matt_tool=97303724`; sem ID, a oferta segue como `VitrineGenerica`.
  - Qualquer `matt_tool` de terceiros é trocado pelo `97303724` (ou pelo TrackingId da conta ML do tenant).
- [x] **Data e hora da captura no cabeçalho do card:**
  - Pílula `🕒 18/10/2026 09:20` (`ReceivedAt` em horário local) no topo do `OfferCard`, com o nome do grupo ao lado.
- [x] **Botões enxutos:**
  - "Abrir", "Verificar", "Clonar" e "Ocultar" (ícones originais, rótulos completos no tooltip) em uma única linha com padding reduzido.
- [x] **Reset limpo do ambiente (`ResetarOfertasLegadas.ps1`):**
  - Zera `GroupCapturedMessages` e `uploads\products` (todos os usuários ou `-UserId`), preservando conexões, grupos e lojas; `-WhatIf` para simular, `-PararIis` para pausar os pools e `-IncluirHistoricoPrecos` para zerar `ProductPriceHistory`.

## 🕒 Feed em Ordem Cronológica da Mensagem (`FeedChronologicalOrder`)

- [x] **Ordenação decrescente pela hora da mensagem:**
  - `OrderByCapturedAtDescending()` (`ReceivedAt` desc, desempate por `Id` desc) no Feed, nos Cupons, nos Ignorados, na busca local de catálogo e no backfill de mídia; antes o feed usava `CreatedAt` (hora da gravação), e a sincronização de histórico jogava mensagens antigas (ex.: 00:23) para o topo.
- [x] **Fuso horário padronizado:**
  - `ReceivedAt` é `datetime2` gravado sempre em UTC com o horário de envio da mensagem (UserBot `message.Date`, Bot API `message.date`, Evolution `messageTimestamp`); a interface converte para o horário de Brasília.
- [x] **Verificação:**
  - Teste com as ofertas reais do banco garante a ordem 09:44 ➔ 09:20 ➔ 09:14 ➔ 08:44 ➔ 08:14 ➔ 00:23.

## 🚫 Remoção Imediata de Produtos Esgotados (`SoldOutFeedRemoval`)

- [x] **Detecção de anúncio encerrado no Mercado Livre (`MercadoLivreAvailabilityRules`):**
  - "Anúncio pausado" / "Publicação finalizada" ➔ `Inativo`; "Sem estoque", `schema.org/OutOfStock`, página só com "Produtos relacionados" (sem "Comprar agora" / "Adicionar ao carrinho") ou produto redirecionado para `lista.mercadolivre.com.br` ➔ `Esgotado`; em ambos `IsAvailable = false`.
  - API `/items/{id}`: `paused`/`closed` ➔ `Inativo` e `available_quantity = 0` ➔ `Esgotado`.
  - Página de verificação anti-robô (`/gz/account-verification`) é inconclusiva e não derruba a oferta.
- [x] **Atualização em tempo real no botão Verificar:**
  - A API devolve a própria oferta revalidada (`ValidatedOffer` + `OfferRemoved`) em vez de recarregar o feed do grupo; o card esgotado sai da lista em memória, o contador é ajustado e aparece o toast "Produto esgotado. Oferta removida do feed.".
- [x] **Filtro estrito no feed de Ofertas:**
  - Produtos só aparecem com `IsAvailable` e status na lista branca `Ativo` / `PrecoAlterado` / `PrecoSuspeito` / `Verificando` (`GroupOfferStatuses.FeedVisibleStatuses`); cupons seguem filtrados só por `IsAvailable`. O Worker de lote (`OfferRevalidationService`) usa a mesma regra, então o que ele desativa some do feed na próxima carga.

## 🏷️ Filtro por Características de Produtos (`ProductTagsFilter`)

- [x] **Estrutura no banco:**
  - Entidade `ProductTag` (`Id`, `Name`, `Slug`, `IsActive`) e N:N com `GroupCapturedMessage` pela tabela `OfferTags` (cascata nos dois lados); migração `AddProductTags` com seed Gamer, Eletrônicos, Informática, Casa & Cozinha, Smartphones e Moda/Beleza.
  - "Todas as Características" é a opção sem filtro do combo, não uma linha do banco.
- [x] **Combo na interface:**
  - `form-select-sm` à direita de "Ocultos / Descartados", carregado de `ProductTags` ativas pela resposta do feed; destacado quando há filtro e desabilitado na aba de Cupons.
- [x] **Filtro LINQ:**
  - `MonitoredGroupService.ListAsync` aplica `.Where(o => o.Tags.Any(t => t.Id == selectedTagId))` nos produtos quando `tagId` > 0.
- [x] **Classificador automático:**
  - `ProductTagClassifierRules` (palavras-chave sem acento, por palavra inteira, ignorando URLs) vincula as tags na gravação de cada oferta nova do Telegram/WhatsApp; `ProductTagBackfillWorker` classifica uma vez as ofertas já capturadas na subida da API.

## 💰 Preço Total da Amazon sem Valor por Unidade (`AmazonUnitPriceFix`)

- [x] **Parser ignora preço unitário:**
  - `OfferPriceTextRules.IsUnitPriceContext` descarta valores como "R$ 16,60 / unidade", "por frasco" e "unidade R$ X" tanto no texto da mensagem quanto no HTML genérico (`ProductMetadataHtmlParser`); kits com "R$ 159,90 por 3 unidades" mantêm o total.
- [x] **Caixa de compra da Amazon:**
  - `AmazonProductPageRules` lê estritamente o total da buy box (`.apexPriceToPay` / `.priceToPay` / `#corePrice_feature_div` com `a-offscreen` ou `a-price-whole` + `a-price-fraction`), pulando preço riscado e unitário; sem buy box usa `og:price:amount`. `OfferValidationService` aplica essa leitura nas páginas da Amazon.
- [x] **Verificar atualiza o preço na tela:**
  - Quando o preço real difere do salvo, `MonitoredGroupService.ValidateAsync` devolve `PriceUpdated`; o card é substituído em memória, `StateHasChanged()` redesenha e aparece o toast "Preço atualizado para R$ 55,00".

## 🚀 Migração de Marca TecFlow → Ofertio (`RebrandOfertio`)

- [x] **Solução, projetos e namespaces:**
  - `Ofertio.sln` e projetos `Ofertio.*` (API, WebUi, SharedUi, Core, Business, Data, Database, Infrastructure, Infrastructure.Services, Util, Observability, Orquestrador, Worker, Mobile, Application, Tests); namespaces, assemblies (`Ofertio.API.dll` no `web.config`) e classes `Ofertio*` renomeados.
- [x] **Interface Blazor:**
  - Marca "Ofertio" no cabeçalho, no Master Console e nos títulos das páginas; título padrão "Ofertio — Inteligência em Ofertas" nos layouts, na tela inicial e no `index.html` do Mobile.
- [x] **Banco de dados (SQL Server):**
  - Banco `AutomacaoSociais` renomeado para `Ofertio` (dados preservados) e connection strings atualizadas; migração `RebrandOfertioSettings` renomeia a chave `TecFlow:ShortLinks:PublicBaseUrl` do cofre para `Ofertio:ShortLinks:PublicBaseUrl`.
- [x] **Pastas, uploads e Data Protection:**
  - Uploads em `C:\inetpub\ofertio\uploads` (produção) e `C:\inetpub\ofertio_dev\uploads` (Development); chaves em `App_Data/OfertioDataProtectionKeys`, preservado pela limpeza do deploy; sessão do Telegram em `C:\ProgramData\Ofertio\telegram-sessions`.
- [x] **IIS:**
  - Sites `Ofertio.API` / `Ofertio.WebUi`, pools `OfertioApiPool` / `OfertioWebUiPool` com login no SQL Server e pastas `C:\inetpub\ofertio\{api,webui,uploads}`.
- [x] **Identificadores legados mantidos:**
  - Nome de aplicação/purpose do Data Protection, ID da migração `InitialTecFlow`, prefixo `tecflow-u` das instâncias do WhatsApp, `demo@TecFlow.local` e IDs de rastreio/afiliado (`tecflow_kabum`, `tecflow_cb`, `tecflow_sandbox_subid`, `tecflow_test`, `utm_source=tecflow`), além das credenciais sandbox; credenciais do Mercado Livre (`matt_tool=97303724`, conta `ronyfaria`) intactas.

## 🎨 Identidade Visual Ofertio (`OfertioVisualIdentity`)

- [x] **Ativos de imagem:**
  - Logotipo recortado e redimensionado (490x160, fundo azul-marinho original) em `wwwroot/images/logo-ofertio.png` do WebUi e do Mobile.
  - Ícone das setas laranja com fundo transparente em `wwwroot/favicon.png` (192x192) e `wwwroot/favicon.ico` (16/32/48).
- [x] **Layouts Blazor:**
  - Cabeçalho do portal (`MainLayout`) e do Master Console (`MasterMainLayout`) exibem o logotipo (`height: 40px; width: auto; object-fit: contain;`) com alt "Ofertio — Inteligência em Ofertas"; a tela de login (`Home`) usa o logotipo em 72px no lugar do selo de texto.
  - O `NavMenu` não tem marca (só itens de menu), por isso não mudou.
- [x] **Tags do `<head>`:**
  - `App.razor` (WebUi), `AppShell.razor` e `index.html` (Mobile) apontam o favicon para `favicon.png`/`favicon.ico`.
  - Título padrão "Ofertio - Inteligência em Ofertas" via `<PageTitle>` nos layouts e na tela de login (um `<title>` fixo no `App.razor` sobrescreveria os títulos de cada página) e via `<title>` no `index.html` do Mobile.

## 🔘 Botões de Grupos Monitorados (`GruposMonitoradosButtonsFix`)

- [x] **Botão "Limpar" (Telegram):**
  - Corrigido: o modal de confirmação nunca abria porque o `ConfirmDialogHost` era renderizado de forma estática no `MainLayout`; agora usa `InteractiveServer` (como o `ToastContainer`), e o clique deixa de ficar travado esperando.
  - Após confirmar, apaga ofertas capturadas e imagens (como antes), redefine todos os filtros (busca, grupo "Todos os grupos", característica "Todas", aba Feed de Ofertas, plataforma de cupom, filtros avançados e página 1) e recarrega a lista padrão, com o toast "Filtros redefinidos com sucesso.".
- [x] **Estados de carregamento:**
  - `_isRefreshing`, `_isClearing` e `_isSyncing` com spinner Bootstrap e botão desabilitado ("Atualizando...", "Limpando...", "Sincronizando..."), sempre redefinidos no `finally`.
  - Sincronização em segundo plano do Telegram mantém o badge e o botão travados até o status da execução atual concluir (status antigo é ignorado; falha não repete o toast a cada 20s).
- [x] **Toasts de conclusão:** "Sincronização concluída! X novos grupos e Y novas mensagens atualizadas." (`MonitoredGroupSyncRules.BuildCompletionMessage`).
- [x] **Cards (Verificar, Clonar, Ocultar, Restaurar, Sem interesse):** só o card clicado mostra o mini-spinner no próprio botão (`OfferCardAction` por oferta); os demais cards seguem utilizáveis.

## 🏬 Filtro por Plataforma em Grupos Monitorados (`MonitoredPlatformFilter`)

- [x] **Select "Todas as Plataformas":**
  - Ao lado direito do select de características; ao trocar a plataforma, volta para a página 1 e recarrega o feed.
  - Filtro único para as três abas (Feed de Ofertas, Cupons & Promoções e Ocultos / Descartados); a plataforma escolhida é mantida ao trocar de aba. As pílulas de marketplace da aba de cupons foram removidas.
- [x] **Lista dinâmica de plataformas (`MonitoredGroupsResponseDto.Platforms`):**
  - Mostra só as plataformas com conta de marketplace ativa do usuário (`MarketplaceAccounts`, as mesmas que liberam produtos no feed).
  - Sem conta conectada, usa as plataformas detectadas nas ofertas capturadas no período; sem nenhuma, exibe Mercado Livre e Shopee.
  - Se a plataforma selecionada sair da lista (conta desativada), ela continua visível no select até o usuário trocar ou clicar em "Limpar".
- [x] **Filtro no backend:**
  - `GET api/integracoes/grupos/monitorados?platform=` ➔ `LiveSearchFilterDto.Platform` ➔ `OfferPlatformFilterRules.Apply` (`PlatformType` no SQL), no feed de ofertas e nos ocultos; aceita apelidos como `Magalu` e `Ponto`.
- [x] **Botão "Limpar":** também volta a plataforma para "Todas as Plataformas".

## 🔐 Certificado HTTPS no Kestrel (`KestrelDevCertificate`)

- [x] **Endpoint `https://localhost:7002` do WebUi em Development:**
  - `Ofertio.WebUi/appsettings.Development.json` carrega `C:/Programacao/certificado/dev.ofertio.com.br.pfx` (gerado no win-acme) em `Kestrel:Endpoints:Https`, com `"Protocols": "Http1AndHttp2"` (HTTP/2 via ALPN e fallback HTTP/1.1; equivale a `listenOptions.Protocols` sem duplicar o bind da porta no `Program.cs`).
  - A senha do `.pfx` fica fora do repositório, em User Secrets do WebUi: `dotnet user-secrets set "Kestrel:Endpoints:Https:Certificate:Password" "<senha>" --project .\Ofertio.WebUi\Ofertio.WebUi.csproj` (ou variável de ambiente `Kestrel__Endpoints__Https__Certificate__Password`).
  - Vale só para `dotnet run`/F5: no IIS o WebUi roda `inprocess` e o HTTPS da porta 7002 é o binding do site `Ofertio.WebUi`. Com o IIS ativo na 7002, pare o site antes de subir o Kestrel nessa porta.
  - `.gitignore` ignora `*.pfx`.

## 🛒 Rotas do app Mercado Livre (`MercadoLivreEndpoints`)

- [x] **`MercadoLivreController` (`api/mercadolivre`, anônimo):**
  - `GET api/mercadolivre/callback?code=&state=`: Redirect URI do OAuth 2.0. Abre o `state` selado, bloqueia reuso, grava os tokens em `MarketplaceAccounts` e redireciona para `/marketplace-accounts?status=success|error` da origem onde o OAuth começou (sem origem válida, responde JSON).
  - `POST api/mercadolivre/notifications`: lê o JSON (`topic`, `resource`, `user_id`, `application_id`, `attempts`) e responde `200 OK` imediatamente, inclusive com payload vazio ou inválido, para o ML não reenviar nem desativar o tópico.
- [x] **Fluxo compartilhado:** `MercadoLivreOAuthCallbackFlow` atende tanto o novo `GET` quanto o `POST api/marketplace-auth/mercadolivre/callback` usado pela WebUi.
- [x] **`Program.cs` da API:** `builder.Services.AddControllers()` e `app.MapControllers()` já presentes.
- [x] **Host público pela WebUi (`MercadoLivreWebUiProxy`, YARP):** `MercadoLivreProxyExtensions` (`AddHttpForwarder` + `MapForwarder`) repassa `/api/mercadolivre/callback` e `/api/mercadolivre/notifications` para `OrquestradorApi:BaseUrl` (`http://localhost:5001` no IIS), preservando método, query, corpo e o redirect 302 da API.
  - O `EXECUCAO_HOMOLOGACAO.md` remove a subaplicação `/api` do site `Ofertio.WebUi` se ela existir.
  - ⚠️ Não criar aplicação `/api` dentro do site `Ofertio.WebUi` no IIS: no mesmo `OfertioApiPool` ela derruba a API com **HTTP 500.35** (dois apps in-process no mesmo pool) e, com pool próprio, duplica os workers da API.
- [ ] **Cadastro no app do ML:** Redirect URI do app e `Integrations:MercadoLivre:RedirectUri` = `https://<host público da WebUi>/api/mercadolivre/callback`; URL de notificações = `https://<host público da WebUi>/api/mercadolivre/notifications`.

---

*Nota para a IA: Siga o checklist ativo passo a passo. Não pule etapas. Preserve o código de validação existente. Consulte o histórico em `docs/HISTORICO_FASES_CONCLUIDAS.md` para contexto das Fases 1 a 31.*
