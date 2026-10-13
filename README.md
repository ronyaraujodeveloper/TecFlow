# 📝 TecFlow - Roadmap, Arquitetura & Contexto Geral

> **Painel principal do projeto** (antigo `TODO.md`). Regras de código, stack e checklist **ativo** (Fases 32 a 34). O histórico das Fases 1 a 31 está em [`docs/HISTORICO_FASES_CONCLUIDAS.md`](./docs/HISTORICO_FASES_CONCLUIDAS.md) (ver `.cursorrules`).

## 🎯 Objetivo do Projeto
Plataforma de **automação e inteligência para afiliados de alta escala**: orquestração de engajamento (comentários, mensagens e links), conciliação financeira de comissões, catálogo de produtos de divulgação e integrações com TikTok Shop e Shopee. O ecossistema combina backend robusto em C# (`TecFlow.API`, `TecFlow.Worker`, `TecFlow.Orquestrador`) com o frontend **`TecFlow.WebUi`** para controle, auditoria e monitoramento em produção.

## 🛠️ Stack Tecnológica Definida
- **Backend:** .NET 8.0 Web API (C#)
- **Frontend:** Blazor WebApp — projeto **`TecFlow.WebUi`** (ASP.NET Core .NET 8.0)
- **Banco de Dados:** SQL Server (`localhost\SQLEXPRESS` / `AutomacaoSociais`) via `Database:Provider=SqlServer` no Development e na Homologação IIS; PostgreSQL permanece apenas como assembly de migrations legado em `TecFlow.Infrastructure`.
- **ORM:** Entity Framework Core 8 — `UseSqlServer` (`TecFlow.Data`) ou `UseNpgsql` (`TecFlow.Infrastructure`) conforme o provider

---

### 🌐 Diretrizes de Codificação, Localização e Encoding (Padrão Obrigatório)

Para evitar que a acentuação em PT-BR fique quebrada (ex: exibir '??' ou caracteres corrompidos no portal ou banco de dados), todo desenvolvedor e IA parceira deve seguir rigorosamente estas regras:

1. **Encoding de Arquivos (UTF-8 com BOM):**
   - Todos os arquivos de código-fonte criados ou modificados (`.razor`, `.cs`, `.html`, `.css`, `.json`) **DEVEM** ser salvos explicitamente utilizando a codificação **UTF-8 com assinatura (BOM)**. Isso garante que o IIS e o compilador do .NET processem os acentos corretamente em ambientes Windows/Server.

2. **Cultura e Localização Nativa (PT-BR):**
   - O portal frontend `TecFlow.WebUi` opera sob a cultura `pt-BR`. O pipeline de inicialização configura globalmente as propriedades `DefaultThreadCurrentCulture` e `DefaultThreadCurrentUICulture` para garantir consistência em formatações de data, moeda e decodificação textual.

3. **Persistência de Dados:**
   - SQL Server (`Database:Provider=SqlServer`): `UseSqlServer` e migrations em `TecFlow.Data`.
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
   - **`[Nome]Filter`** (na pasta `TecFlow.Database/Filter/`): Contém as propriedades escalares da Entity como opcionais/nullable (`?`). Usado **EXCLUSIVAMENTE** para parâmetros de busca em listagens e consultas (**GET**). Não incluir objetos de navegação — apenas IDs (`CampaignId`, `OwnerId`, etc.).
   - **`[Nome]Dto`** (na pasta `TecFlow.Business/Dto/`): Contém as propriedades limpas que a tela envia para o formulário. Usado **EXCLUSIVAMENTE** para receber dados de escrita (**POST** e **PUT**). Não deve conter objetos complexos de navegação inteiros, apenas seus IDs correspondentes.
   - **`[Nome]ResponseDto`** (na pasta `TecFlow.Business/Dto/`): Envelope padrão de retorno que encapsula propriedades de controle (`Status` [bool], `Descricao` [string]) e a Entity real (`Data` [[Nome]?] e `DataList` [List<[Nome]>?]).

   > A **Entity** continua em `TecFlow.Core/Entities/` ou `TecFlow.Database/Entity/`. Exemplo `Estoque`: `EstoqueFilter`, `EstoqueDto`, `EstoqueResponseDto` + Entity.

3. **Padrão de Retorno de Métodos (Controllers e Services):** Métodos **GET** retornam `[Nome]ResponseDto` com `Data` ou `DataList`. Métodos **POST/PUT** recebem `[Nome]Dto` e retornam `[Nome]ResponseDto` quando aplicável.

4. **Filtros e Consultas:** Toda listagem (**GET**) deve aceitar `[FromQuery] [Nome]Filter filter`, aplicar o filtro na query (repositório ou extensão `ApplyFilter`) e paginar via `Pagin` (máximo 30 registros).

5. **Criptografia de credenciais:** Qualquer campo, propriedade ou dado que se refira a senhas ou tokens de acesso (como senhas de usuários, tokens do TikTok ou Shopee) DEVE ser criptografado antes de ser salvo no banco de dados e descriptografado apenas no momento do uso.

6. **Validações Obrigatórias:** Cadastros de e-mail, celular/WhatsApp, CPF, CNPJ alfanumérico e CEP devem passar estritamente pelos métodos do `ValidationHelper` em `TecFlow.Business/Service` antes de qualquer persistência.

7. **Design Responsivo & Mentalidade Mobile First:** Qualquer novo componente visual, página ou layout criado no projeto `TecFlow.WebUi` DEVE obrigatoriamente contemplar design responsivo (Mobile First). É proibido o uso de larguras fixas em pixels para containers principais e tabelas densas; utilize grids fluidos, flexbox e classes utilitárias responsivas (Bootstrap/Tailwind) para garantir que a interface se adapte nativamente a PCs, Celulares e Tablets.

8. **Auto-atualização do Roadmap:** Ao concluir uma tarefa das Fases **32 a 34** neste arquivo, marque `[ ]` para `[x]`. Quando a fase inteira estiver concluída, **mova o bloco** para `docs/HISTORICO_FASES_CONCLUIDAS.md`. Sincronize também `docs/LISTA_ARQUIVOS_MUDANCAS.md` e `docs/DIAGRAMAS_ARQUITETURA.md` conforme `.cursorrules`.

## 🧪 Suíte de Testes e Qualidade (TecFlow.Tests)

Diagnóstico das Fases **8** (auth/multi-loja), **10** (links backend), **11** (gerador UI) e **19** (homolog Shopee): a suíte cobre serialização JSON de DTOs, envio do formulário de vínculo manual, escopo `lojaId`, 401 sem JWT e envelopes 500 em vez de exceção não tratada. Telas Blazor (`MinhasLojas.razor`, `GeradorLinks.razor`) são validadas via serviços HTTP e o validador extraído do formulário (`ConnectStoreManualLinkForm`), sem bUnit. A lista detalhada de testes já cobertos está no [histórico de fases](./docs/HISTORICO_FASES_CONCLUIDAS.md).

### 💻 Comandos no terminal
- **Toda a suíte:** `dotnet test`
- **Projeto isolado:** `dotnet test .\TecFlow.Tests\TecFlow.Tests.csproj`
- **Logs detalhados:** `dotnet test --logger "console;verbosity=detailed"`
- **Filtro por módulo:** `dotnet test --filter "FullyQualifiedName~Shopee"`
- **Filtro auth/loja:** `dotnet test --filter "FullyQualifiedName~Integracoes|FullyQualifiedName~AuthController"`

## ⚠️ REGRAS ARQUITETURAIS INVIOLÁVEIS (NÃO ALTERAR)

1. **BANCO DE DADOS OFICIAL:**
   - O único banco de dados oficial do projeto é o **SQL Server**.
   - O nome do banco de dados na string de conexão deve ser estritamente **`AutomacaoSociais`**.
   - O provider do Entity Framework Core deve ser obrigatoriamente **`SqlServer`** (`Microsoft.EntityFrameworkCore.SqlServer`).
   - É **EXTREMAMENTE PROIBIDO** alterar a configuração do `appsettings.json` para `PostgreSQL` / `Npgsql` ou criar bancos com outros nomes (como `TecFlowDb`).

2. **COMPATIBILIDADE E ENCODING:**
   - Todos os arquivos editados ou criados devem ser salvos com a codificação **UTF-8 com BOM**.
   - As migrations do EF Core devem ser geradas e executadas exclusivamente para a sintaxe T-SQL / SQL Server direcionadas ao banco `AutomacaoSociais`.

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

O **TecFlow** foi projetado sob o padrão **Strategy Pattern** para permitir a adição plug-and-play de novas plataformas de afiliados e marketplaces.

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

Sempre que exibir o nome, tag ou selo de uma plataforma no TecFlow (listas, buscas, cards ou formulários), utilize a paleta de cores corporativas oficial com texto em alto contraste:

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

Para manter a consistência visual e a elegância da interface em toda a plataforma TecFlow, **é estritamente proibido o uso de diálogos nativos do navegador** (`window.confirm`, `window.alert` ou `window.prompt`).

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

O menu lateral do TecFlow deve seguir rigorosamente a seguinte estrutura hierárquica e agrupamento de módulos:

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

2. **Implementação da Estratégia (`TecFlow.Domain` / `TecFlow.Services`):**
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
     - `TecFlowTrackingUrl`: Link interno de telemetria `/{storeSlug}/{code}`.

---

## 🚀 Fases do Desenvolvimento (Checklist ativo)

As **Fases 1 a 32** (e entregas equivalentes já concluídas) foram movidas para o [Registro Histórico de Fases Concluídas](./docs/HISTORICO_FASES_CONCLUIDAS.md).

O checklist abaixo contém **apenas as fases ativas e pendentes**.

  ## 🛠️ Busca MLB autenticada (`MLBOauthSearch`)

- [x] **OAuth obrigatório:** `sites/MLB/search` com `Bearer` + `access_token`; ID de afiliado não autentica a busca.
- [x] **Sem scrap da vitrine:** `lista.mercadolivre.com.br` cai em `account-verification` no IIS e foi removido do fluxo.
- [x] **Badge:** `⚠️ Requer OAuth...` sem token; `❌ API MLB recusou a busca (HTTP 403)...` com token recusado.
- [x] **OAuth em Minhas Lojas:** botão **Autorizar Mercado Livre (OAuth)** no card e **Conectar via OAuth** na modal; `auth.mercadolivre.com.br/authorization` ➔ `/integracoes/oauth/callback` ➔ `authorization_code` grava `APP_USR-…` + refresh na loja ML existente (preserva o ID de afiliado).
- [x] **Placeholder ignorado:** `homolog-test-access-token` do Universal Link não é enviado à API; refresh rotacionado é persistido na conta.
- [ ] **Credenciais do app ML:** preencher `Integrations:MercadoLivre:AppId`/`SecretKey` e cadastrar a Redirect URI HTTPS (`…ngrok-free.dev/integracoes/oauth/callback`) em developers.mercadolivre.com.br.

## 👑 Módulo Backoffice & Gestão Master SaaS (Fases 35 a 41)

---

### 🛡️ Fase 35: Alternância Dinâmica de Perfil e Impersonation (`TenantImpersonationEngine`)

- [x] **35.1. Mecanismo de Alternância Admin / Usuário Comum (`SwitchProfileService`):**
  - Implementação do seletor global na barra superior para alternar instantaneamente da visão `SuperAdmin` para a visão `TenantUser` (Usuário Comum) sem perda de sessão.
- [x] **35.2. Emulação de Contexto Real de Cliente (`ImpersonateTenantContext`):**
  - Injeção temporária das Claims do tenant selecionado na memória do circuito Blazor (`ClaimsPrincipal`), permitindo testar a interface, limites de plano e buscas exatamente como o cliente enxerga.
- [x] **35.3. Barra Indicativa de Modo Emulação (Feedback Visual Fixado):**
  - Exibição de banner de alerta destacado no topo da tela: *"⚠️ Você está navegando como o tenant X. [Voltar ao Painel Master]"*.

---

### 🔐 Fase 36: Controle de Permissões e Gestão de Operadores Master (`MasterAdminRBAC`)

- [x] **36.1. Cadastro e Gestão de Operadores Internos (`MasterUsersManagement`):**
  - Tela dedicada para criação, edição e inativação de contas administradoras do TecFlow com controle de credenciais via ASP.NET Core Identity.
- [x] **36.2. Matriz de Permissões por Papel (`RBACMatrix`):**
  - Atribuição granular de papéis (`SuperAdmin`, `SuporteTecnico`, `FinanceiroSaaS`), restringindo quem pode alterar chaves globais, alterar planos ou alternar para o modo de emulação.
- [x] **36.3. Trilha de Auditoria do Master (`MasterAuditTrail`):**
  - Registro imutável em banco de todas as ações administrativas sensíveis (troca de chaves de API, alteração manual de saldo de créditos ou acesso via impersonation).

---

### ⚙️ Fase 37: Parametrização Global de APIs & Infraestrutura (`SystemSettingsEngine`)

- [x] **37.1. Gestão Centralizada de App Keys de Marketplaces (`GlobalMarketplaceKeys`):**
  - Painel para cadastro seguro do `AppId`/`ClientSecret` do Mercado Livre, `AppKey`/`AppSecret` da Shopee e `AccessKey`/`SecretKey`/`PartnerTag` da Amazon PA-API.
- [x] **37.2. Configuração de Provedores de Mensageria e Conexões (`GlobalMessagingSettings`):**
  - Gestão de endpoints da Evolution API (WhatsApp), tokens do Telegram e credenciais do MTProto UserBot.
- [x] **37.3. Central de Chaves de IA, Encurtador e Secrets (`InfrastructureVault`):**
  - Configuração de chaves globais da OpenAI e parametrização do domínio próprio do encurtador (`tflow.link`).
  - Persistência das chaves criptografadas no banco/environment variables sem dependência de edições manuais em arquivos `appsettings.json`.

---

### 💳 Fase 38: Gestão Financeira, Planos & Feature Toggles (`SubscriptionEngine`)

- [x] **38.1. Cadastro e Configuração de Planos SaaS (`SaaSPlansManagement`):**
  - Gestão dos planos de assinatura (*Starter, Pro, Agency, Enterprise*) definindo valores, ciclo de cobrança (mensal/anual) e limites de cada plano.
- [x] **38.2. Matriz de Recursos e Limites por Plano (`FeatureTogglesMatrix`):**
  - Trava de recursos por plano: quantidade de lojas conectadas, cotas diárias de "Achadinhos VIP", acesso ao radar de ofertas e limite de agendamentos diários.
- [x] **38.3. Integração com Gateway de Pagamentos e Gestão de Cotas (`PaymentGatewaySync`):**
  - Conexão com gateway de pagamento (ASAAS/Mercado Pago/Stripe) para gestão de cobranças recorrentes, faturamento automático por Pix/Boleto/Cartão e bloqueio automático de inadimplentes.
  - Painel de ajuste manual de créditos extras de Achadinhos por tenant.

---

### 👥 Fase 39: Gestão e Moderação de Inquilinos (`TenantManagement`)

- [x] **39.1. Central 360° do Inquilino (`Tenant360View`):**
  - Listagem completa dos clientes com filtros por status (*Ativo, Trial, Inadimplente, Suspenso, Cancelado*), plano contratado, consumo de disco e data de renovação.
- [x] **39.2. Ações Moderativas e Controle de Ciclo de Vida (`TenantLifecycleControl`):**
  - Botões de ação rápida para pausar, suspender, estender período de teste (trial) ou cancelar a conta de um tenant com motivo registrado.
- [x] **39.3. Monitor de Consumo de Mídia e Armazenamento (`TenantStorageMonitor`):**
  - Relatório detalhado da ocupação de disco por tenant (`wwwroot/uploads/products/{TenantId}`) com acionamento manual do expurgo de mídia para contas específicas.

---

### 📊 Fase 40: Dashboard Executivo do SaaS (`SaaSExecutiveMetrics`)

- [x] **40.1. Indicadores de Receita Recorrente (`MRRDashboard`):**
  - Painel gerencial exibindo métricas do SaaS: **MRR** (Receita Mensal Recorrente), **ARR** (Receita Anual Projetada), **ARPU** (Receita Média por Usuário) e faturamento líquido do mês.
- [x] **40.2. Análise de Saúde da Base (`ChurnAndRetentionMetrics`):**
  - Gráficos de crescimento de novos assinantes, conversão de contas Trial para Pagantes e taxa de **Churn** (cancelamentos).
- [x] **40.3. Métricas Globais de Operação e Disparos (`PlatformUsageMetrics`):**
  - Volume consolidado de links convertidos, disparos de mensagens enviados via WhatsApp/Telegram e total de requisições às APIs nas últimas 24h.

---

### 📢 Fase 41: Comunicação & Avisos em Massa (`GlobalBroadcastBanner Engine`)

- [x] **41.1. Banners de Avisos Globais na Interface (`GlobalSystemBanners`):**
  - Cadastro de avisos informativos, de manutenção ou comunicados importantes exibidos no topo do painel de todos os usuários (ex: *"A API da Shopee passará por manutenção hoje às 23h"*).
- [x] **41.2. Disparo de Notificações Internas por Segmento (`TargetedNotifications`):**
  - Envio de alertas direcionados dentro da plataforma para grupos específicos de tenants (ex.: apenas para inadimplentes ou apenas para assinantes do Plano Starter).
- [x] **41.3. Central de Changelog e Novidades do Produto (`ProductChangelog`):**
  - Módulo para publicação de atualizações do sistema (*What's New*), permitindo que os clientes leiam as melhorias recém-lançadas no TecFlow.

## 👑 Módulo Backoffice Master: Isolamento de Rotas, Contas de Desenvolvedor e Menu por Plano (Fases 42 a 44)

---

### 🌐 Fase 42: Segregação de Módulos e Rotas Master (`MasterRouteIsolation & Layout`)

- [x] **42.1. Padrão de Roteamento `/master/*` (`MasterRouteConvention`):**
  - Isolamento de todos os componentes e páginas de administração global sob o prefixo `/master/` (ex: `/master/marketplace-accounts`, `/master/planos`, `/master/usuarios`).
- [x] **42.2. Layout e Guard de Segurança Master (`MasterLayout` & `MasterAuthorize`):**
  - Criação do layout dedicado `MasterMainLayout.razor` com identidade visual executiva (distinta da visão do cliente).
  - Interceptador/Guard de autorização que bloqueia o acesso a qualquer rota `/master/*` para usuários que não possuem o papel `SuperAdmin` (redirecionando para `/` em tentativas de acesso não autorizado).
- [x] **42.3. Ocultação do Accordion "Próximos Desenvolvimentos" na Visão Cliente:**
  - Remoção/bloqueio definitivo do menu de roadmaps e protótipos na visão do cliente comum, tornando a Sidebar do tenant 100% focada em recursos produtivos e liberados.

---

### 🔑 Fase 43: Gerenciamento Master de Contas de Desenvolvedor e Testes (`MasterMarketplaceAccounts`)

- [x] **43.1. Tela Específica de Infraestrutura `/master/marketplace-accounts`:**
  - Desenvolvimento do painel Master para gerenciamento das **Contas de Desenvolvedor / App Credentials** (Mercado Livre AppId/SecretKey, Shopee AppKey/AppSecret, Amazon PA-API Keys) e contas de teste internas da empresa TecFlow.
- [x] **43.2. Segregação Total de Escopo (`DeveloperAccountIsolation`):**
  - Isolamento do serviço para garantir que as credenciais globais e contas de homologação cadastradas no `/master/marketplace-accounts` fiquem armazenadas em tabela/escopo de sistema (`SystemDeveloperAccounts`), sem interferir nas contas de afiliados do cliente (`/marketplace-accounts`).
- [x] **43.3. Teste de Conectividade e Saúde das APIs Globais:**
  - Painel com botões de diagnóstico direto na tela master para validar status de tokens de app, expiração de secrets e status das conexões com os servidores do Mercado Livre, Shopee e Amazon.

---

### 📜 Fase 44: Matriz Dinâmica de Menus por Plano e Banco de Dados (`DynamicPlanMenuPermissions`)

- [x] **44.1. Persistência de Menus no Banco de Dados (`MenuItems` & `PlanMenuPermissions`):**
  - Criação das tabelas `MenuItems` (cadastrando toda a árvore de menus com `Id`, `Title`, `Route`, `Icon`, `ParentId`, `Order`) e `PlanMenuPermissions` (mapeando a relação entre `PlanId` e `MenuItemId` com `IsVisible`).
- [x] **44.2. Interface Master de Controle de Árvore `/master/menu-permissions`:**
  - Desenvolvimento da tela Master exibindo a árvore hierárquica completa do menu do TecFlow em formato de matriz/tabela com colunas para cada plano (*Starter, Pro, Agency, Enterprise*).
  - Toggles/Checkboxes dinâmicos para você marcar ou desmarcar visualmente quais itens do menu cada plano enxergará na plataforma.
- [x] **44.3. Renderização Dinâmica da Sidebar do Cliente (`DynamicNavMenu`):**
  - Refatoração do `NavMenu.razor` para carregar a estrutura de navegação diretamente da tabela de permissões com base no plano ativo do tenant logado, omitindo automaticamente rotas não contratadas ou indisponíveis.

---

### 🎨 Fase 45: Navegação, Usabilidade e Paleta de Cores (Master × Cliente)

- [x] **45.1. Reset automático de scroll na navegação Blazor:**
  - Componente `NavigationScrollReset` (ouvinte de `NavigationManager.LocationChanged` + JSInterop `tecFlowAppInterop.resetScroll`) nos layouts `MainLayout` e `MasterMainLayout`, retornando ao topo da janela e do container `.main-content` a cada troca de rota (âncoras `#` preservadas).
- [x] **45.2. Hierarquia visual da Sidebar do cliente:**
  - Categorias (MENSAGERIA & BOTS, CANAIS, INTEGRAÇÕES) como cabeçalhos em caixa alta e cinza suave; Menus Pai com ícone destacado e texto em negrito; Submenus com recuo de `1.5rem`, linha guia e ícone menor.
  - "Minhas Lojas / Integrações" (`/marketplace-accounts`) como menu pai fixo da categoria Integrações, sempre visível conforme a matriz de permissão por plano.
- [x] **45.3. Harmonização de paletas:**
  - Master: fundo slate escuro (`#0F172A` / `#1E293B`) com detalhes laranja `#FF9900` e branco em header, sidebar, cards, tabelas, formulários e botões.
  - Cliente: paleta clara e neutra, foco em legibilidade e bordas suaves.
- [x] **45.4. Segregação Vault × Contas de teste:**
  - `/master/vault` isolado para as chaves globais (AppId/SecretKey, Evolution, Telegram, OpenAI, ASAAS) com diagnóstico das chaves em uso; `/master/marketplace-accounts` exclusivo para contas corporativas de teste/dev (sem publicação no cofre); `/marketplace-accounts` preservado para o afiliado cliente.

---

### 🧲 Fase 46: Menu Dinâmico com Drag & Drop e Roadmap no Banco (`MenuDragDrop`)

- [x] **46.1. Seeding de "Próximos Desenvolvimentos" em `MenuItems`:**
  - Pasta `Próximos Desenvolvimentos` (`IsFolder = true`, `Route = null`) com Campanhas (`/campanhas`), Métricas / Comissões (`/metricas`), Fila de engajamento (`/fila-engajamento`, selo "EM BREVE"), Conciliação financeira (`/conciliacao-financeira`), Produtos propaganda (`/produtos-propaganda`) e Saúde do sistema (`/saude-sistema`), oculta por padrão em todos os planos e listada na matriz `/master/menu-permissions`.
- [x] **46.2. Drag & Drop na matriz `/master/menu-permissions`:**
  - Arrasto entre linhas atualiza `OrderIndex`; arrasto para o centro de uma pasta atualiza `ParentId`; arrasto item sobre item abre modal para o título e cria uma nova pasta com os dois itens. Pastas criadas vazias podem ser excluídas.
- [x] **46.3. Sidebar do cliente sincronizada com `MenuItems` + `PlanMenuPermissions`:**
  - `NavMenu.razor` 100% dirigido pelo banco (sem accordion fixo), com pastas aninhadas, selos e recarga da árvore a cada navegação; master na visão cliente vê exatamente o que os clientes veem.

---

*Nota para a IA: Siga o checklist ativo passo a passo. Não pule etapas. Preserve o código de validação existente. Consulte o histórico em `docs/HISTORICO_FASES_CONCLUIDAS.md` para contexto das Fases 1 a 31.*
