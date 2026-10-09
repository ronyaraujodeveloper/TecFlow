# Registro Histórico de Fases Concluídas do TecFlow

Este arquivo é o **Registro Histórico de Fases Concluídas** do TecFlow.

Ele preserva o checklist das **Fases 1 a 31** (e entregas equivalentes já marcadas como `[x]`), para reduzir o tamanho do `README.md` sem perder o rastro das implementações. O painel operacional atual — objetivo, stack, regras de código e **Fases 32 a 34** (pendentes) — permanece em [README.md](../README.md).

Quando uma fase ativa do README for concluída, mova o bloco correspondente para este arquivo e marque os itens como `[x]`.

---

## Menu lateral e suíte de testes (entregas equivalentes)
- [x] Sidebar (`NavMenu.razor`) reorganizada nesta árvore, sem duplicar Minhas Lojas, com ícones e destaque do grupo/página ativos.
- [x] Accordion “Próximos Desenvolvimentos”: `button` (não NavLink no cabeçalho), `isProximosOpen` + `ToggleProximosSubmenu()` com `StateHasChanged()`, subitens em `@if`.
- [x] `NavMenu` em Interactive Server para os grupos abrirem no clique (layout estático não processava `@onclick`).

### Cobertura por módulo
- [x] **Unidade — algoritmos:** `ValidationHelperTests`, `OrderStateMachineTests`
- [x] **Fase 8.1/8.2 — Auth / provedores:** `AuthControllerSecurityTests` (401, 400, 500 envelope, JSON `LinkProviderDto`, login `INVALID_CREDENTIALS`)
- [x] **Fase 8.3 — Integrações de loja:** `IntegracoesControllerTests`, `IntegracaoLojaServiceTests`, `ConnectStoreManualLinkFormTests`, `HttpServiceVincularManualTests`
- [x] **Fase 8.4 — Dashboard `lojaId`:** `DashboardControllerTests`, `MetricsControllerLojaScopeTests`
- [x] **Fase 10 — Strategy / telemetria:** `ShopeeLinkConversionTests`, `AffiliateLinkInfrastructureTests`, `LinkClickLogTests`, `ShortLinkRedirectControllerTests`
- [x] **Fase 10/19 — POST gerar link:** `AffiliateLinksControllerTests`, `GeradorLinksServiceTests` (JSON `GerarLinkAfiliadoDto`, 401/500)
- [x] **Fase 11 — UI HTTP:** `AffiliateShareLinkBuilderTests`, `HttpServiceAuthStatusTests` (401/500 no `HttpService`)
- [x] **Fase 9.2 / segurança de conta:** `AccountSecurityApiServiceTests`
- [x] **Integração — desserialização DTO / HttpService:** envelopes `ResponseDto` e ProblemDetails numérico não derrubam o Blazor
- [x] **UI — form vínculo (8.3 / 19.3):** bindings `ShopId` (`long`) e `AuthorizationCode` (`string`); payload invertido rejeitado na validação

---

## 🚀 Fases do Desenvolvimento e Reestruturação (Checklist)

### Fase 1: Transição de Arquitetura e Renomeação (Foco Atual) 📂
- [x] Mudar o nome da Solution e dos projetos de 'Tecso' para 'TecFlow'.
- [x] Estruturar o projeto **`TecFlow.Business`** com as pastas:
  - `Dto/` (Transições e as classes padrão de Response).
  - `Enum/` (Enumeradores separados por conceitos de domínio).
  - `Service/` (Serviços, regras de negócio e onde ficarão as classes herdadas de Criptografia e Validação).
- [x] Estruturar o projeto **`TecFlow.Database`** com as pastas:
  - `Entity/` (O coração do projeto).
  - `Filter/` (Objetos de busca baseados nas Entities).
  - `Interface/` (Contratos dos repositórios).
  - `Pagin/` (Classe de controle de paginação limitada a 30 registros).
  - `Repositorio/` (Implementação das consultas e queries que consomem os Filters).
- [x] Mover o `DbContext` existente para a raiz ou pasta adequada do `TecFlow.Database`.

### Fases Concluídas e Preservadas (A serem validadas na nova estrutura) ✅
- [x] Instalação e configuração do PostgreSQL local (Porta 5432, base: `automacaosociais`).
- [x] Criação do serviço de criptografia (agora alocado em `TecFlow.Business/Service`).
- [x] Implementação no `ValidationHelper` das validações de E-mail, CPF, CNPJ Alfanumérico, CEP (ViaCEP) e Força de Senhas.
- [x] Implementação da validação rigorosa de celular brasileiro (`IsValidBrazilianCellPhone`).
- [x] Estrutura base do Frontend criada (agora migrando para o projeto **`TecFlow.WebUi`**).
- [x] Telas iniciais de escolha (TikTok/Shopee) e cards de login mockados.
- [x] Implementar fluxo de captura e persistência de Telefone/WhatsApp (com aplicação da validação do helper) para alertas do Orquestrador.

### Fase 2: Implementação dos Componentes Base do Banco e Modelagem ⚙️
- [x] Criar a classe base de paginação na pasta `2.Database/Pagin/` travando os resultados em 30 registros.
- [x] Criar a primeira Entity oficial (`User`) na pasta `Entity/`.
- [x] Criar o objeto espelho `UserFilter` na pasta `Filter/`.
- [x] Criar o `UserDto` e `UserResponseDto` na pasta `1.Business/Dto/`.
- [x] Configurar a Connection String do PostgreSQL e aplicar a primeira Migration do ecossistema TecFlow.

### Fase 3: Regras de Negócio, Telas e APIs Externas 🔄
- [x] Refatorar Controllers para arquitetura de 3 objetos (Filter / Dto / ResponseDto) e remover Create/Update Dtos redundantes.
- [x] Migrar o projeto 'TecFlow.Portal' e adaptar o projeto `TecFlow.WebUi` para consumir os novos Services que retornam o padrão `ResponseDto`.
- [x] Descontinuar `TecFlow.Portal` e `TecFlow.Dashboard`; **`TecFlow.WebUi`** é o frontend canônico (Blazor).
- [x] Integrar os componentes de tela aos filtros de listagem compostos pela pasta `Filter`.
  - [ ] Integração real com as APIs de produção do TikTok Shop e Shopee.
  - [x] 3.1. Infraestrutura Core de Integração: Criar os HttpClient específicos, handlers de resiliência (Polly) e logs de requisições.
  - [x] 3.2. Fluxo de Autenticação & OAuth2: Implementar a geração de URL de autorização, captura do Authorization Code e armazenamento seguro/renovação automática do Access Token e Refresh Token para ambas as plataformas.
  - [x] 3.3. Sincronização de Catálogo (Produtos): Implementar mapeamento de payloads, busca de produtos das plataformas e conversão para o nosso padrão [Nome]ResponseDto.
  - [x] 3.4. Gestão de Pedidos & Estoque: Estruturar os endpoints/serviços para receber webhooks ou realizar polling de novos pedidos e atualizar estoque de forma bidirecional.

### Fase 4: Estratégia Mobile Híbrida (Android e iOS) 📱
- [x] 4.1. Fundação Mobile-First no TecFlow.WebUi e Contratos de API
  - [x] Auditar e refatorar layouts existentes do `TecFlow.WebUi` para conformidade Mobile First (breakpoints, tabelas responsivas, navegação touch-friendly).
  - [x] Padronizar contratos REST (`*Filter`, `*ResponseDto`) e autenticação para consumo estável por clientes móveis futuros.

- [x] 4.2. Shell Híbrido Nativo (.NET MAUI / Blazor Hybrid)
  - [x] Avaliar e estruturar projeto compartilhado (RCL) reutilizando componentes Blazor do `TecFlow.WebUi`.
  - [x] Configurar pipeline de build e publicação para Android e iOS (lojas ou distribuição interna).

- [x] 4.3. Engajamento Móvel em Tempo Real
  - [x] Integrar notificações push (FCM/APNs) para alertas de comentários, comissões e falhas de webhook.
  - [x] Implementar deep links para abrir diretamente painéis de conciliação e fila de engajamento no app.



### Fase 5: Visão de Negócio - Plataforma de Automação e Inteligência para Afiliados de Alta Escala 🚀
**[x] Revisado e validado (jun/2026)** — domínio base, enums globais e contratos de orquestração consolidados em `TecFlow.Core` e `TecFlow.Business` para API, Worker, Orquestrador e WebUi.

Orquestração de engajamento (comentários, mensagens e links), conciliação financeira de comissões, catálogo de produtos de divulgação e integrações com TikTok Shop e Shopee. O ecossistema combina backend robusto em C# (`TecFlow.API`, `TecFlow.Worker`, `TecFlow.Orquestrador`) com o frontend `TecFlow.WebUi` / `TecFlow.SharedUi` / `TecFlow.Mobile` para controle, auditoria e monitoramento em produção.

- [x] 5.0. Fucomatos (Core + Business)
  - [x] Enums: `SocialMediaType`, `EngagementStatus`, `CommissionStatus`.
  - [x] Entidade conceitual `AffiliateLink` (produto, link original, variações Shopee/TikTok Shop).
  - [x] Modelos de orquestração: `SocialEngagementEvent`, `EngagementOrchestrationResult`, `CommissionAuditLine`, `CommissionConciliationResult`.
  - [x] Contratos: `IEngagementOrchestrator`, `ICommissionConciliator` (implementação prática na Fase 6).
  - [x] DTOs: `AffiliateLinkDto`, `AffiliateLinkResponseDto`.

### Fase 6: Engenharia e Infraestrutura para Afiliados, Mensageria e Escala 🛠️
- [x] 6.1. Sistema de Filas e Mensageria (Foco em Automação de Engajamento)
  - [x] Implementar infraestrutura com RabbitMQ (MassTransit) compartilhada entre API, Worker e Orquestrador.
  - [x] Webhook `POST /api/webhooks/social-media/comments` publica `SocialMediaCommentReceivedEvent` e responde **202 Accepted**.
  - [x] Consumidor `SocialMediaCommentConsumer` no Worker com triagem por palavras-chave configuráveis e entrega simulada de link por `PostId`.
  - [x] Retry automático e fila de erro (DLQ) `social-media-comment-received-error`.

- [x] 6.2. Painel de Conciliação Financeira de Afiliado (TecFlow.WebUi / SharedUi)
  - [x] Página `ConciliacaoFinanceira.razor` mobile-first (KPIs em grid, tabela desktop / cards mobile com badges de divergência).
  - [x] `IAffiliateAnalyticsService` — importação de relatórios Shopee/TikTok (tokens válidos) + fallback pedidos locais.
  - [x] `ReconcileCommissionsAsync` — cruza rastreio local vs. repasse marketplace e classifica divergências (`CommissionDiscrepancyReportDto`).
  - [x] API Orquestrador: `GET /api/afiliados/analytics/conciliacao`.

- [x] 6.3. Mecanismo de Mapeamento de Produtos e Atributos Globais para Propaganda
  - [x] Entidades `GlobalAdvertisingProduct` + `MarketplaceAffiliateLink` (EF + migração PostgreSQL).
  - [x] `IAdvertisingProductService` — cadastro global, geração de links parametrizados e `GenerateOptimizedPayloadForPostAsync`.
  - [x] API `api/propaganda/produtos` e página `ProdutosPropaganda.razor` (mobile-first, copiar link Shopee/TikTok).

- [x] 6.4. Observabilidade e Telemetria (Monitoramento de Produção)
  - [x] Projeto `TecFlow.Observability` com `AddTecFlowTelemetry` (traces, métricas OTLP/Console/Prometheus, logs OpenTelemetry).
  - [x] Métricas de negócio: `comentarios_processados_total`, `links_enviados_sucesso`, `erros_conciliacao_contagem`.
  - [x] Instrumentação em API, Worker e Orquestrador (HTTP Shopee/TikTok, consumer de engajamento, conciliação).
  - [x] Painel `PainelSaude.razor` + `GET /api/saude/dashboard` (DB, RabbitMQ, APIs, erros recentes).

### Fase 7: Módulo de Vendas Diretas e Gestão de Estoque (Futuro) 📦
- [x] 7.1. Arquitetura Multi-Tenant / Multi-Conta por Marketplace (SaaS Ready)
  - [x] Ajustar a modelagem do banco de dados na camada 'Database' para suportar o conceito de inquilinos (Tenants) e vinculação de múltiplos 'ShopId' por usuário.
  - [x] Adaptar as queries e repositórios para isolar os dados de cada loja, permitindo que o lojista gerencie múltiplos CNPJs/Contas da Shopee e TikTok Shop no mesmo painel.

- [x] 7.2. Core de Vendas, Faturamento e ERP Local
  - [x] Criar entidades de 'Pedido de Venda' (Order), 'Cliente' (Customer) e 'Item do Pedido' para registrar vendas próprias.
  - [x] Estruturar o fluxo de estados do pedido (Pendente, Pago, Faturado, Enviado, Concluído) e preparar ganchos para futura integração com emissão de Notas Fiscais Eletrônicas (NF-e).

- [x] 7.3. Controle Avançado de Estoque Próprio (Estoque Físico)
  - [x] Implementar tabelas de movimentação de estoque (Entradas por compra, Saídas por venda, Ajustes manuais, Estoque Mínimo e Alertas).
  - [x] Desenvolver serviço de reserva de estoque para garantir que, no momento em que um pedido de venda direta for gerado, as unidades fiquem bloqueadas temporariamente até a confirmação do pagamento, evitando o Overbooking (vender o que não tem).
  
  ### Fase 8: Nova Arquitetura de Autenticação e Multi-Contas no Backend 🔐
- [x] 8.1. Esquema de Autenticação com Múltiplos Provedores (Identity Link)
  - Configurar a tabela de logins do ASP.NET Core Identity (`AspNetUserLogins`) para suportar múltiplos provedores sociais (Google, Facebook, Apple) vinculados ao mesmo ID de usuário.
  - Implementar lógica de *Auto-linking*: Se o login social autenticado usar um e-mail já existente no banco de dados, vincular o provedor social à conta existente em vez de gerar um usuário duplicado.

- [x] 8.2. Endpoints de Gestão de Provedores de Login e Segurança de Credenciais
  - Criar o endpoint `POST /api/auth/providers/vincular` para associar um novo método social com o usuário já logado no painel.
  - Criar o endpoint `DELETE /api/auth/providers/desvincular` para remover um método social, aplicando a validação de segurança que exige que reste ao menos um método de autenticação ativo (senha ou outro social).
  - Desenvolver o endpoint `PUT /api/auth/change-password` para troca de senha de e-mail e aplicar um bypass/script temporário para resetar a credencial do usuário de homologação `demo@tecso.local` (resolvendo o bloqueio de credenciais inválidas).

- [x] 8.3. Modelagem Relacional e Endpoints para Múltiplas Lojas (1 para Muitos)
  - Criar a entidade e migração PostgreSQL para a tabela `IntegracaoLoja` (`Id`, `IdUsuario`, `Plataforma` [TikTok/Shopee], `NomeAmigavel`, `AccessToken`, `RefreshToken`, `Status`), quebrando o acoplamento antigo de uma única conta por usuário.
  - Desenvolver o endpoint `GET /api/integracoes/lojas` para listar todas as contas de marketplaces conectadas ao usuário logado.
  - Desenvolver o endpoint `POST /api/integracoes/vincular` para capturar o fluxo de callback do OAuth do marketplace, solicitar o Nome Amigável/Apelido da loja e persistir o novo registro de forma isolada.
  - Desenvolver o endpoint `DELETE /api/integracoes/lojas/{id}` para desvincular e remover uma loja específica.

- [x] 8.4. Refatoração dos Endpoints de Métricas do Dashboard para Escopo de Loja
  - Ajustar os controladores e serviços que alimentam o Dashboard para exigir ou receber o parâmetro opcional `?lojaId=...`, garantindo que as queries apliquem o filtro de isolamento e retornem os dados da conta selecionada.


### Fase 9: Reformulação Visual e Componentes Multi-Contas no Frontend (TecFlow.WebUi) 🎨
- [x] 9.1. Reformulação Visual da Tela de Login Principal
  - Remover os botões iniciais de login direto por marketplace (TikTok/Shopee) da página de entrada.
  - Redesenhar a interface utilizando abordagem Mobile-First com botões de provedores sociais centrais (Google, Apple, Facebook) e o formulário tradicional de E-mail/Senha com link para recuperação de senha.
  - Integrar autorregistro de usuários via portal (`/cadastro`) com endpoint `POST /api/auth/register`, validação centralizada (`ValidationHelper`) e persistência no PostgreSQL.
  - Adicionar a opção "Cadastre-se" na tela de login e criar a nova página de registro conectada diretamente ao banco de dados PostgreSQL via API é um passo natural.

- [x] 9.2. Central de Contas e Segurança de Acesso do Usuário
  - Criar a página interna "Minha Conta / Segurança" (`/minha-conta`) com métodos de acesso (E-mail, Google, Facebook, Apple), vinculação/desvinculação OAuth e formulário de alteração de senha integrado à API (`GET/DELETE/PUT /api/auth/providers/*` e `change-password`).

- [x] 9.3. Painel de Gerenciamento Multi-Contas de Marketplaces
  - Desenvolver a interface "Minhas Lojas / Integrações" exibindo em cartões responsivos todas as contas integradas do TikTok/Shopee, Shop ID, Affiliate ID / Tracking ID, status de conexão (Verde/Vermelho) e o botão para disparar o OAuth de uma nova conta (permitindo gerenciar 10 ou mais lojas).

- [x] 9.4. Seletor Global de Escopo no Topbar do Dashboard
  - Desenvolver um componente de Dropdown persistente e fluido na barra superior do sistema carregando dinamicamente as lojas conectadas do usuário.
  - Persistir o estado da loja selecionada no escopo global do Blazor. Ao alternar a loja no topo, disparar o recarregamento dos componentes da página ativa injetando o novo `lojaId`.

### Fase 10: Mecanismo Omnichannel de Geração e Encurtamento de Links de Afiliado (Backend) 🔗
- [x] 10.1. Arquitetura Base e Padrão Strategy para Múltiplos Marketplaces
  - Criar a interface `IPlatformLinkStrategy` com métodos para validação de domínio e geração de Deep Links.
  - Implementar o `PlatformLinkResolver` para identificar dinamicamente qual provedor deve processar a URL com base no domínio (suportando nativamente TikTok e Shopee, e preparado para Mercado Livre, Amazon, Magalu, etc.).
  - Criar o DTO unificado `GerarLinkAfiliadoDto` recebendo a URL bruta e o escopo de identificação.

- [x] 10.2. Implementação dos Provedores e Integração com as APIs Core
  - Desenvolver as classes de estratégia iniciais consumindo os SDKs/APIs correspondentes de Afiliados.
  - Tratar payloads de links já encurtados pelas plataformas de origem (ex: links do tipo `s.shopee.com.br` ou encurtados de redes sociais), realizando o *unshorten* (rastreamento do redirecionamento HTTP) se necessário para extrair o ID real do produto antes de re-parametrizar.

- [x] 10.3. Encurtador Interno Multi-Plataforma e Telemetria de Cliques
  - Criar o mecanismo de redirecionamento dinâmico do TecFlow (ex: `tflow.link/xyz`).
  - Modelar a tabela `LinkClickLog` para registrar a telemetria de acessos (data, hora, IP, localização simulada, dispositivo e plataforma de origem do produto).

### Fase 11: Módulo Gerador de Links Omnichannel no Frontend (TecFlow.WebUi) 📱
- [x] 11.1. Tela Universal "Gerador de Links de Comissão" (Mobile-First)
  - Desenvolver a interface Blazor (`GeradorLinks.razor`) com um campo de captura inteligente de URLs.
  - Exibir visualmente os logos de todos os marketplaces suportados pelo sistema (com sinalização de quais estão ativos ou configurados para a conta do usuário).

- [x] 11.2. Painel Dinâmico de Resultados e Compartilhamento Nativo
  - Renderizar o link customizado gerado com feedback visual instantâneo e botão de cópia rápida.
  - Acoplar a Web Share API para permitir o envio direto do link gerado para canais como WhatsApp, Telegram e redes sociais em dispositivos móveis.

- [x] 11.3. Histórico Geral com Filtros por Plataforma e Métricas de Engajamento
  - Renderizar listagem responsiva contendo o histórico de links processados do tenant (independente da loja ativa no topo; abas Shopee/TikTok/Magalu etc. filtram explicitamente).
  - Adicionar badges dinâmicos para identificar visualmente a plataforma de destino (Shopee, TikTok, Amazon, etc.) e o contador agregador de cliques em tempo real baseado no log de telemetria.
  - Botão **Visualizar** no histórico recarrega o painel "Seus links de comissão" (OriginalUrl, AffiliateUrl, ShortenedUrl, plataforma) e o campo Link do produto, com scroll suave até o formulário.
  - Botão **Editar** abre modal para marcar/desmarcar contas da mesma plataforma (`IsActive = false`, sem delete).
  - Formulário lista checkboxes das contas ativas da plataforma detectada, com Selecionar todas; a geração converte todas as contas marcadas.

### 🔑 Fase 12: Autenticação Social e Identidade Omnichannel (Gmail, Apple, Facebook) 🌐

#### 12.1. Integração com Google OAuth 2.0 (Login pelo Gmail)
- [ ] **12.1.1. Configuração no Google Cloud Console:** Criar projeto, configurar a Tela de Consentimento OAuth (adicionando escopos `openid`, `profile`, `email`), registrar a URI de redirecionamento de homologação (`https://camcorder-bonding-sloppily.ngrok-free.dev/signin-google`) e gerar as chaves `ClientId` e `ClientSecret`.
- [ ] **12.1.2. Implementação no Backend (TecFlow.API):** Instalar e configurar o pacote `Microsoft.AspNetCore.Authentication.Google`, mapear as chaves no `appsettings.json` e criar o endpoint de callback para receber o token do Google, validar/criar o usuário no PostgreSQL e emitir o JWT do sistema.
- [ ] **12.1.3. Interface e Fluxo no Frontend (TecFlow.WebUi):** Desenvolver o botão "Entrar com Google" com design oficial, disparar o redirecionamento de segurança para o Google e tratar o retorno da sessão no Blazor Server.

#### 12.2. Integração com Apple Identity (Login pelo iCloud)
- [ ] **12.2.1. Configuração no Apple Developer Program:** Criar o *App ID* com a funcionalidade *Sign In with Apple* ativa, configurar o *Services ID* com a URL de redirecionamento correspondente e gerar a chave privada de assinatura (`.p8`).
- [ ] **12.2.2. Implementação no Backend (TecFlow.API):** Configurar a autenticação da Apple utilizando criptografia de chaves (`ClientSecret` gerado dinamicamente via JWT assinado com a `.p8`) e validar o ID Token enviado pela Apple.
- [ ] **12.2.3. Interface e Fluxo no Frontend (TecFlow.WebUi):** Acoplar o botão nativo "Sign in with Apple" respeitando as diretrizes estritas de UX da Apple e mapear o envio dos dados do usuário (nome/e-mail obtidos apenas no primeiro login).

#### 12.3. Integração com Meta for Developers (Login pelo Facebook)
- [ ] **12.3.1. Configuração no Meta Developers:** Criar um aplicativo do tipo "Consumidor", configurar o produto "Login do Facebook", adicionar as URIs de redirecionamento válidas e obter o *App ID* e *App Secret*.
- [ ] **12.3.2. Implementação no Backend (TecFlow.API):** Instalar o pacote `Microsoft.AspNetCore.Authentication.Facebook`, configurar o middleware no pipeline e mapear o mapeamento de claims (id, email, name).
- [ ] **12.3.3. Interface e Fluxo no Frontend (TecFlow.WebUi):** Inserir o botão "Entrar com Facebook" na página de login e ligar o fluxo de autenticação ao circuito Blazor.

#### 12.4. Mecanismo de Auto-Linking e Vínculo de Contas
- [ ] **12.4.1. Resolução de Conflito de E-mail Único:** Garantir no banco de dados que, se um usuário já cadastrado com `rony@...` via e-mail tentar clicar em "Entrar com Google" usando o mesmo e-mail, o sistema vincule com segurança a credencial do Google à conta existente em vez de gerar um registro duplicado ou estourar erro de constraint.

### Fase 13: Motor de Busca Cruzada e Comparador de Preços Multicloud (Backend) 🧠🔍
- [ ] 13.1. Evolução da Interface Strategy e Extração de Scraping/Meta-dados
  - Estender a interface `IPlatformLinkStrategy` para incluir o método `Task<ProductMetadataDto> ExtractProductMetadataAsync(string url)`.
  - Implementar um extrator de metadados básico (via API oficial ou crawler leve/HtmlAgilityPack) para identificar o Título Comercial, Imagem e Preço Atual do link original colado pelo usuário.

- [ ] 13.2. Implementação do Motor de Busca Cruzada em Paralelo (Cross-Search)
  - Estender a interface `IPlatformLinkStrategy` para incluir o método `Task<List<ProductSearchMatchDto>> SearchProductByTitleAsync(string title, Guid storeId)`.
  - Implementar nas classes especialistas (Shopee, TikTok, Amazon, Mercado Livre) a chamada de busca por palavra-chave nas respectivas APIs de afiliados.
  - Criar o serviço `ProductArbitrageService` que dispara as buscas em paralelo (`Task.WhenAll`) em todas as plataformas conectadas e ativas do usuário, ignorando falhas individuais de APIs externas para não travar o fluxo.

- [ ] 13.3. Algoritmo de Rankeamento, Filtragem por Menor Preço e Normalização
  - Desenvolver lógica de higienização de strings para comparar os títulos (removendo termos ruidosos como "Frete Grátis", "Original", "Promoção").
  - Filtrar os resultados para garantir que o preço encontrado nas plataformas concorrentes seja **menor** que o preço do link original.
  - Ordenar o resultado de forma ascendente pelo preço e limitar o retorno a no máximo 3 sugestões alternativas, já gerando o link de comissão convertido para cada uma delas.

### Fase 14: Painel de Otimização e Sugestões de Ofertas no Frontend (TecFlow.WebUi) 💸
- [ ] 14.1. Componente Reativo "Sugestões de Melhor Preço" (UI/UX)
  - Desenvolver uma seção dinâmica na página `GeradorLinks.razor` que exibe um *loader* de busca (ex: "Buscando preços melhores em outras plataformas...") logo após o link principal ser gerado.
  - Renderizar até 3 cards de sugestões alternativas utilizando abordagem Mobile-First.

- [ ] 14.2. Anatomia do Card de Sugestão e Ações Rápidas
  - Cada card de sugestão deve exibir de forma clara:
    * O logo do marketplace concorrente onde o produto mais barato foi encontrado.
    * O novo preço em destaque comparado ao preço original (ex: * De R$ 100,00 por R$ 85,00 na Amazon*).
    * Botões rápidos independentes de "Copiar Link Alternativo" e "Compartilhar".

- [ ] 14.3. Telemetria de Conversão de Arbitragem
  - Ajustar a tabela `LinkClickLog` para registrar quando um clique veio de um link de sugestão alternativa (arbitragem), permitindo que o usuário saiba no Dashboard se as sugestões de menor preço estão performando melhor que os links originais que ele cola.

### [x] Fase 15: Infraestrutura de Logs Globais e Telemetria 🚀

#### 15.1. Infraestrutura de Logs do Servidor (IIS)
- [x] Criar script PowerShell `Configurar-Logs-IIS.ps1` para gerar as pastas físicas de log e gerenciar permissões de escrita (FullControl) para o pool do IIS (`IIS_IUSRS` / `DefaultAppPool`).

- [x] Configurar o arquivo `web.config` do TecFlow.WebUi com a flag de controle dinâmico: `stdoutLogEnabled="true"` e diretório `.\logs\stdout`.

- [x] Configurar o arquivo `web.config` da TecFlow.API seguindo o mesmo padrão, mapeando os logs para `.\logs\stdout` orientado por flag de ativação.

#### 15.2. Padronização do Log de Aplicação (.NET Serilog/ILogger)
- [x] Inspecionar o arquivo `Program.cs` da TecFlow.API e garantir a injeção do provedor de Log (Console + Arquivo de Rolagem Diária `.txt`).

- [x] Inspecionar o arquivo `Program.cs` do TecFlow.WebUi (Blazor Server) para capturar o ciclo de vida das conexões de circuitos SignalR e requisições HTTP internas (`BlazorCircuitLoggingHandler`, `UseSerilogRequestLogging`).

- [x] Configurar os arquivos `appsettings.Homologacao.json` de ambos os projetos para definir o nível mínimo de log como `Information` (traces internos do framework em homolog).

#### 15.3. Blindagem e Captura de Exceções Ocultas
- [x] Implementar ou revisar um Middleware Global de Exceções (`ExceptionHandlingMiddleware`) na API para capturar erros 500, estendendo o log com detalhes contextuais seguros (LGPD) e retornando `ProblemDetails` JSON.

- [x] Validar que todos os blocos críticos de `try/catch` no fluxo de autenticação (OAuth, login tradicional, pontes de comunicação) invoquem explicitamente `_logger.LogError(ex, ...)` em vez de engolirem a exceção em silêncio.

### Fase 16: Identidade Digital e Infraestrutura de Produção (O Alicerce) 🗺️
- [ ] **16.1.** Registro e Apontamento do Domínio: Registrar tecflow.com.br no Registro.br e configurar os servidores de DNS na Cloudflare.

- [ ] **16.2.** Configuração do Servidor e SSL: Apontar o subdomínio homolog.tecflow.com.br para o servidor de homologação e instalar certificado SSL (Let's Encrypt).

- [ ] **16.3.** Blindagem de E-mail (Entregabilidade): Configurar apontamentos TXT de SPF, DKIM e DMARC na zona de DNS para evitar bloqueios no Gmail e iCloud.

- [ ] **16.4.** Publicação dos Termos Legais: Disponibilizar páginas institucionais em /privacidade e /termos para aprovação nas esteiras das Big Techs.

### 🔑 Fase 17: Consoles de Desenvolvedor e Credenciais de APIs
- [ ] **17.1.** Google Cloud Console: Configurar a tela de consentimento OAuth, gerar Client ID/Secret e ativar a API do Gmail.

- [ ] **17.2.** Apple Developer Program: Mapear Identifiers, Service IDs e chaves privadas (.p8) para o fluxo "Entrar com Apple".

- [ ] **17.3.** Meta for Developers: Criar app tipo Consumidor/Empresa e obter credenciais de Login do Facebook.

- [ ] **17.4.** Shopee Open Platform: Solicitar acesso à Affiliate API e à V2 Open API de gerenciamento de lojas.

- [ ] **17.5.** TikTok Developer / Shop Academy: Credenciamento empresarial para APIs de afiliados e login unificado.

- [ ] **17.6.** OpenAI Developer Platform: Gerar chaves secretas corporativas (sk-...) e travar limites de faturamento do motor de IA.

### 🔥 Fase 18: Homologação e Testes de Circuito Fechado (A Prova de Fogo)
- [ ] **18.1.** Teste de Autenticação Unificada e Auto-linking: Validar persistência e vínculo cruzado no PostgreSQL sem duplicar contas.

- [ ] **18.2.** Simulação de Vínculo Multi-Lojas: Executar fluxos em Sandbox (Shopee/TikTok) e checar tokens criptografados na tabela IntegracaoLoja.

- [ ] **18.3.** Teste de Estresse do Motor Strategy: Forçar expansão em lote de URLs encurtadas reais sem gargalo de processamento.

- [ ] **18.4.** Validação de Telemetria: Simular acessos móveis/desktop em links encurtados próprios (tflow.link/...) e verificar a integridade da tabela LinkClickLog.

### 🛍️ Fase 19: Homologação Prática do Gerador e Conversor de Links de Afiliado (Shopee) 🔗

#### 19.1. Parametrização e Configuração das Credenciais de Afiliado
- [X] **19.1.1. Inspecionar e Ajustar `appsettings.json`:** Mapear as chaves de Afiliado da Shopee (`PartnerId`, `PartnerKey` / `AppSecret` / `AppKey` / `AppSignature`) na `TecFlow.API` e no `TecFlow.WebUi`.
- [x] **19.1.2. Mapeamento de Fallback/Sandbox:** Implementar/validar o modo de simulação no `ShopeeIntegrationClient` para garantir que, caso as chaves reais de produção não estejam preenchidas, o sistema injete uma tag/sub_id de homologação sem estourar exceção.

#### 19.2. Teste do Motor Backend de Unshorten e Re-parametrizador (Strategy)
- [x] **19.2.1. Validação do `PlatformLinkResolver`:** Testar a resolução de domínios nativos da Shopee (`shopee.com.br`) e encurtados (`s.shopee.com.br`, `br.shp.ee`, `shp.ee`, `shope.ee`), com regex desktop `i.{shopId}.{itemId}` e geração de Universal Link (`/universal-link/product/{shopId}/{itemId}?sub_id=`) sem App Key / App Secret.
- [x] **19.2.2. Geração da URL Rastreada de Comissão:** Validar o Universal Link com `sub_id` (Tracking ID, apelido ou UserId), sem dependência impeditiva da Open API.
- [x] **19.2.3. Persistência de Telemetria:** Confirmar que a chamada grava corretamente um novo registro na tabela `LinkClickLog` com o `TenantId`/`ShopId` e metadados de acesso.

#### 19.3. Conexão End-to-End no Frontend (TecFlow.WebUi)
- [x] **19.3.1. Integração da Tela `GeradorLinks.razor`:** Ligar o evento do botão "Gerar Link" da interface Blazor ao endpoint `POST /api/afiliados/links/gerar` do backend, com `_isLoading`, alerta vermelho se a loja não estiver selecionada e `StateHasChanged()` após sucesso.
- [x] **19.3.2. Ações de Interface e Feedback Visual:** Renderizar `AffiliateUrl` (longa), `ShortenedShopeeUrl` (`br.shp.ee`) e `ShortenedUrl` (`http://localhost:5001/{storeSlug}/{code}`) em cards com cópia independente via `tecflow-clipboard.js`. Combo **Conta selecionada** quando o produto tem mais de uma conta ativa.
- [x] **19.3.4. Múltiplas contas:** `ShortAffiliateLinkAccounts.IsActive`; geração em lote; inativação lógica ao desmarcar.
- [x] **19.4. TikTok Shop:** `TikTokShopLinkStrategy` converte URLs oficiais/encurtadas com `sub_id` (Tracking ID ou nome amigável); cadastro na modal Conectar nova loja.
- [x] **19.5. Mercado Livre:** `MercadoLivreLinkStrategy` injeta `matt_tool` e `matt_word`; cadastro com Matt Tool ID.
- [x] **19.6. Amazon:** `AmazonLinkStrategy` extrai ASIN, expande `amzn.to`/`a.co` e injeta `tag` da conta; cadastro com Tag de Associado.
- [x] **19.7. Magazine Luiza:** `MagazineLuizaLinkStrategy` monta Magazine Você / `parceiro` e expande `magalu.me`; cadastro com nome da loja parceira.
- [x] **19.8. Kabum!:** `KabumLinkStrategy` injeta `sub_id` e `utm_source=afiliado`; expande `kb.um`; cadastro com Tracking ID.
- [x] **19.9. Casas Bahia:** `CasasBahiaLinkStrategy` injeta `parceiro` e `sub_id`; expande `cb.com.br` / app.link; cadastro com ID de Parceiro.
- [x] **19.10. Modal conectar loja:** grid compacto de plataformas (3–4 colunas) sem barra de rolagem.
- [x] **19.11. Cores institucionais:** badges/botões Shopee, TikTok, ML, Amazon, Magalu, Kabum e Casas Bahia com tokens CSS e estado ativo da marca.
- [x] **19.12. Como pegar meu ID?:** dica do formato por plataforma, painel oficial em nova aba e extração automática do ID ao colar URL (`tag`, `sub_id`, `an_id`, `matt_tool`/`matt_word`, `parceiro`).
- [x] **19.13. Validação de Tracking ID:** bloqueia URL residual no cadastro; expande encurtadores; saneia `MarketplaceAccounts` com `http` no TrackingId.
- [x] **19.14. Unicidade de ID:** um Tracking ID ativo por plataforma; exemplo padronizado `6512300000`.
- [x] **19.15. Desconectar loja:** `ConfirmarDesconexaoAsync` (`async Task`); `InativarContaAsync` grava `IsActive = false` via `SaveChangesAsync`; POST `api/marketplace-auth/lojas/{id}/desconectar`.
- [x] **19.16. Extração Shopee:** expande `s.shopee.com.br` / `br.shp.ee` / `shope.ee` e lê `mmp_pid=an_` / `utm_source=an_`.
- [x] **19.17. Encurtadores multiplataforma:** TikTok (`vt`/`vm`), Magalu (`onelink`/`magazinevoce`) e Mercado Livre (`meli.la`/`/sec/`).
- [x] **19.18. Auto-detecção ao colar URL:** o domínio define `SelectedPlatform`; unshorten devolve a URL destino (nunca `true`); extração TikTok/Magalu/ML após expandir.
- [x] **19.19. Magalu promoter_id:** prioriza `promoter_id=` (ex.: `5321952`) e ignora `utm_source=divulgador`/`magalu`.
- [x] **19.20. TikTok unique_id:** prioriza `unique_id=` (ex.: `amz.indica`), depois `user_id` e `sec_user_id` em `vt.tiktok.com` / `shop.tiktok.com`.
- [x] **19.21. TikTok login redirect:** decodifica `redirect_url` recursivamente; aceita `@username` / `amz.indica` no Tracking ID.
- [x] **19.22. Resolução de loja no gerador:** com o seletor em Todas (ou loja de outra plataforma), usa a primeira `MarketplaceAccounts` ativa da plataforma do link; só pede conexão se não houver nenhuma conta daquela plataforma.
- [x] **19.23. Metadados Shopee:** nome primário pelo slug da URL expandida (`HttpUtility.UrlDecode`); preço via `price_min` / `price` / OpenGraph; histórico `R$ 28,70`.
- [ ] **19.3.3. Teste do Circuito Fechado (Ponta a Ponta):** Efetuar login por e-mail no sistema, colar a URL real de uma cadeira/produto da Shopee, converter, copiar o link de comissão e validar o registro no SQL Server (`AutomacaoSociais` / `ShortAffiliateLinks`).

#### 19.4. Link Encurtado
- [ ] **19.4.1. Encurtar link Shopee

#### 19.5. Extração de Metadados de Produto (Nome e Preço) e Reformulação do Histórico de Links
- [x] 19.5.1. Migração do Modelo de Dados (EF Core & SQL Server): Atualizar a entidade ShortAffiliateLink no Entity Framework Core para incluir as propriedades ProductName (nvarchar(255)), ProductPrice (decimal(18,2)) e ProductImageUrl (nvarchar(500)). Executar dotnet ef migrations add AddProductMetadataToLinks e dotnet ef database update para sincronizar o banco de dados.
- [x] 19.5.2. Resolução e Expansão de Links Encurtados: a URL colada (inclui `promoby.me`, `ofertou.ai`, `bit.ly`, `t.me`) é expandida primeiro; o domínio só é validado na URL final. "Não reconhecemos este domínio" aparece apenas se o destino não for Amazon, Shopee, Magalu, ML, Kabum, TikTok Shop ou Casas Bahia.
- [x] 19.5.3. Desenvolvimento do Serviço Extrator (ProductMetadataService): Criar a rotina de busca de metadados utilizando HttpClient e parse de tags HTML/OpenGraph (og:title, og:price:amount, itemprop="price", JSON-LD) para extrair o Nome e o Preço a partir da URL completa dos marketplaces suportados (Shopee, TikTok Shop, Mercado Livre, Amazon, Magalu, Kabum! e Casas Bahia).
- [x] 19.5.4. Exibição de Card/Preview no Gerador de Links: Atualizar o componente Blazor (GeradorLinks.razor) para exibir o Card do Produto (contendo o Nome do Item, Preço em R$ e badge da plataforma) no painel "Seus links de comissão" imediatamente após o usuário clicar em "Gerar Link de Comissão".
- [x] 19.5.5. Reformulação Visual da Tabela "Histórico de Links": Modificar a estrutura da tabela do histórico de links no Blazor removendo as colunas brutas LINK ORIGINAL e LINK ENCURTADO, substituindo-as por PRODUTO (Nome do Produto) e PREÇO (Valor formatado em R$). Preservar as colunas PLATAFORMA, DATA, CLIQUES e as ações Visualizar e Editar.
- [x] 19.5.6. Tratamento de Exceções e Resiliência (Fallback): scraping bloqueado não interrompe a conversão; títulos genéricos (`Produto`/`Shopee`) gravam `NULL`; o afiliado informa nome/preço no preview e no histórico; fallback Shopee Open API (`get_item_base_info`) usa AppKey/AppSecret de `MarketplaceAccounts`.

### 🛡️ 20. Estratégia de Resiliência, Defesa Anti-Bot e Formatação na Interface (TecFlow)

- [ ] 20.1. **Proteção Contra Captchas e Bloqueios WAF (Cloudflare/Shopee/Marketplaces):**
   - O pipeline de extração descarta nomes genéricos e hashes de anti-bot (ex: `Opaanlp`, `Nsbo`, `Produto`, `Shopee Brasil`, `Captcha`).
   - Títulos suspeitos ou puramente numéricos são higienizados e convertidos para `NULL` no backend para evitar corrupção de dados na base SQL Server.

- [x] 20.2. **Fallback Manual Resiliente no Blazor (GeradorLinks.razor):**
   - Quando o scraping do marketplace for retido por mecanismos antirobô, o sistema não bloqueia a geração do link de comissão.
   - O painel exibe campos de edição direta (Nome do Produto e Preço em R$) para preenchimento opcional pelo afiliado, gravando as informações no banco sem travar a interface.
   - O modal do lápis pré-preenche `ProductName`/`ProductPrice`; o disquete persiste no SQL Server e atualiza o histórico.

- [x] 20.3. **Layout Compacto e Ações Rápidas por Ícones:**
   - Visualização do Card Preview em linha única flexível (`d-flex align-items-center gap-2`).
   - Substituição de botões textuais por ícones nativos do Bootstrap Icons:
     * **Salvar:** Ícone de Disquete (`bi bi-floppy`).
     * **Visualizar:** Ícone de Olho (`bi bi-eye`).
     * **Editar:** Ícone de Lápis (`bi bi-pencil`) — abre o modal de contas e os campos de nome/preço.
   - Disposição horizontal ultra-compacta para os cartões de compartilhamento (Copiar, WhatsApp e Telegram).

- [x] 20.4. **Resolvedor Universal de Links (Multi-hop Unshorten):**
   - Loop de até 5 iterações em `UrlUnshortenerService.ResolveToFinalSupportedMarketplaceAsync` (`301`/`302`/meta-refresh/`window.location`) para agregadores (`ofertou.ai`, `promoby.me`, `amzn.to`, `t.me`).
   - "Não reconhecemos este domínio" só após o loop se o destino final não for marketplace. Strip de `tag=`/`partner_id=`/`promoter_id=`/`utm_*` e re-injeção das credenciais do tenant.
---

## Arquitetura Mobile & Sincronização SQLite/SQL Server

O TecFlow.API (SQL Server `localhost\SQLEXPRESS` / `AutomacaoSociais`) é a fonte de verdade. O futuro app (`TecFlow.Mobile`) sincroniza via REST com os mesmos DTOs do Blazor; o SQLite local é cache offline, nunca substitui o servidor.

| Recurso no SQL Server | Endpoint REST | DTO | SQLite (rascunho) |
| --- | --- | --- | --- |
| `MarketplaceAccounts` | `GET/POST /api/marketplace-auth/lojas`, `POST /api/marketplace-auth/vincular-manual` | `MarketplaceAccountDto`, `IntegracaoLojaDto` | tabela `stores` (espelho do DTO + `SyncedAt`) |
| `ShortAffiliateLinks` | `POST /api/afiliados/links/gerar` (`/api/links/convert`), `GET /api/afiliados/links/historico` | `GerarLinkAfiliadoDto` / `GerarLinkAfiliadoResponseDto` | tabela `short_links` (`OriginalUrl`, `AffiliateUrl`, `Code`, `CreatedAt`, `Platform`, `MarketplaceAccountId`) |

Fluxo previsto:

1. Login JWT (`POST /api/auth/login`) — o token fica no secure storage do dispositivo.
2. Pull: `GET /api/marketplace-auth/lojas` materializa lojas no SQLite; a loja ativa replica o seletor do WebUi.
3. Push de conversão: o app envia a URL original; a API persiste `ShortAffiliateLink` com `SaveChangesAsync()` no SQL Server e devolve `AffiliateUrl` + `ShortenedUrl`.
4. Conflito: o servidor vence (`UpdatedAt` UTC). O SQLite só reenvia operações com `SyncStatus=Pending`.
5. Offline: gera-se um código local provisório; na reconexão o `POST /api/links/convert` grava o registro definitivo no SQL Server.

Contrato mínimo do registro de link (espelhado em `ShortAffiliateLink`): `OriginalUrl`, `AffiliateUrl`, `Code`/`ShortCode`, `CreatedAt`, `Platform`/`PlatformType`, `MarketplaceAccountId`.

### 🌐 21. Página pública de conversão
- [x] 21.1. Versionamento de slugs em `PublicConverterPages` (inativo + novo ativo com o mesmo `PublicCode`).
- [x] 21.2. Deduplicação visual `DistinctBy(PlatformType)` e `FirstOrDefault` da conta ativa na conversão.
- [x] 21.3. Conversões públicas no Histórico de Links (`Source=PublicPage`, badge Página pública).
- [x] 21.4. Layout público compacto: badges ~35% menores, Converter/Limpar 50/50, divisor e duas linhas (comissao direta + rastreio TecFlow) com Copiar/Abrir.

---

## 🚀 Roadmap de Expansão: Integrações e Bots de Distribuição

### 🤖 22. Módulo Telegram (Bot & Agendador de Grupos)
Permite que o usuário do TecFlow conecte seu próprio Bot do Telegram para escutar, converter e agendar ofertas em canais ou grupos.

- [x] 22.1. Arquitetura de Conexão (BotFather UX):**
  - Token, ApiKey e SessionData em `TelegramIntegrations` com AES-256; máscara `****************` em `/integracoes/telegram`.
  - `TelegramApiService.ValidateBotTokenAsync` (`GetMeAsync`) + `RegisterWebhookAsync` (`SetWebhookAsync` com secret do `X-Webhook-Secret`).
  - Status **Conectado ✅** após webhook `POST /api/v1/integrations/telegram/webhook/{userId}`.
  - `/integracoes/conexoes` (aba Telegram): valida Bot Token vazio/`*****`, spinner, toasts e modal **Saiba onde obter o código**.
  - Webhook tolerante: `SetWebhookAsync` em try/catch com log de localhost; credenciais persistidas; falha → `Conectado (Modo Disparo)` + aviso; sucesso → *Bot e Webhook conectados com sucesso!*.
- [x] 22.2. Conversão Automática em Chats:**
  - Webhook processa mensagens privadas, extrai URLs, converte com `PlatformLinkResolver` e loja ativa do `UserId`, responde em até 2s (`SendTextMessageAsync`) e grava `ShortAffiliateLinks.Source=TelegramBot`.
- [x] 22.3. Agendamento e Disparo Automático:**
  - Painel `/integracoes/telegram/agendador` e `TelegramBroadcastWorker` disparam campanhas em `TelegramBroadcastCampaigns` no ChatId do canal/grupo.

---

### 🟢 23. Módulo WhatsApp (Conexão via QR Code / Evolution API)
Oferece uma experiência fluida para afiliados iniciantes conectarem seu número pessoal ou de trabalho lendo um QR Code, sem burocracia ou custos por mensagem da Meta.

- [x] 23.1. Arquitetura de Sessão (Evolution API / Baileys):**
  - Painel unificado `/integracoes/conexoes` (abas WhatsApp / Telegram), QR em modal e status com foto, nome e número.
  - Menu **Mensageria & Bots**: Conexões, Bot de Conversão (`/integracoes/bot-conversor`) e Agendador de Grupos (`/integracoes/agendador`), sem remover WhatsApp/Telegram originais.
  - NavMenu: Mensageria & Bots (Conexões, Agendador de Grupos, Bot), WhatsApp/Telegram (agendadores), Integrações (Lojas única, Gerador, Páginas) e accordion Próximos Desenvolvimentos.
  - O TecFlow orquestra instâncias da **Evolution API** isoladas por `UserId`.
  - `EvolutionApiService` trata HTTP 40x/50x com log do body, ignora instância já criada e busca o QR; a UI exibe alerta amigável em vez de 500 genérico.
  - O usuário acessa a aba *Integrações > WhatsApp*, clica em "Conectar WhatsApp" e o Blazor exibe o QR Code dinâmico obtido via polling da API.
  - Ao escanear com o celular no aplicativo do WhatsApp, a sessão fica salva e ativa no servidor (`WhatsAppIntegrations`).
- [x] 23.2. Bot de Conversão Automática (Escuta e Resposta):**
  - Webhook `POST /api/v1/integrations/whatsapp/webhook` processa `MESSAGES_UPSERT` e ignora `fromMe`.
  - Conversão via `PlatformLinkResolver` + loja ativa do `UserId`; persistência `ShortAffiliateLinks.Source=WhatsAppBot`; resposta Evolution `sendText` em até 3s.
- [x] 23.3. Disparo e Agendamento para Grupos de Ofertas:**
  - Grupos sincronizados da Evolution (`WhatsAppGroups`) e campanhas em `WhatsAppBroadcastCampaigns`.
  - Worker com intervalo anti-bloqueio (15–180s) e tela `/integracoes/whatsapp/agendador`.
  - Nova campanha: Título, Mensagem (sem URL), seletor de links com badges coloridas e campo Link de Comissão com badge da plataforma; o disparo concatena copy + URL.
  - Grupos: MultiSelect com chips, atalhos frequentes persistidos e `IsAdmin` somente para o número conectado.
  - Lista de disparos: lápis preenche o formulário e chama `UpdateAgendamentoCommand`; data futura é preservada e data passada vai para +10 minutos; lixeira abre `ConfirmModal` no próprio componente e exclui o registro.
- [x] 23.4. Segurança defensiva (Telegram/WhatsApp):**
  - AES-256 em Token/ApiKey/SessionData; webhook `X-Webhook-Secret`; `UnauthorizedAccessException` se `integration.UserId != currentUserId`; tokens mascarados no Blazor.

### 🟢 24. quando for converter qualquer link, tentar pegar o link principal da imagem e deixar guardado, para apresentar na tela Gerador de Links de Comissão, se não encontrar possibilitar que o afiliado coloque manualmente.
- [x] Parser (`og:image`, `og:image:secure_url`, `twitter:image`, JSON-LD e `itemprop=image`) grava `ShortAffiliateLinks.ProductImageUrl`.
- [x] Gerador de Links exibe a miniatura e permite editar/salvar a URL da imagem.
- [x] Agendador WhatsApp preenche `model.ImageUrl` ao selecionar o produto no autocomplete.
- [x] `ProductImagePreview`: thumbnail 96×96, placeholder “Sem imagem” (`onerror`) e botão Remover Imagem.

### 🟢 25. Todo o botão salvar, deletar, pesquisar ter um modal load até terminar o processo

### 🟢 26. 

### 🟢 27. criar tela voltada a administração de grupos já existente de outras pessoas que vamos copiar as promoções lha elistente
- [x] Tela WhatsApp `/integracoes/whatsapp/grupos/monitorados` e Telegram `/integracoes/telegram/grupos/monitorados` (submenu de cada canal).
- [x] Webhooks WhatsApp/Telegram gravam `GroupCapturedMessages` (texto, mídia, URL, preço e data).
- [x] `OfferValidationService` confere HTTP + metadados (Ativo, Esgotado, Preço Alterado).
- [x] **Clonar para Minha Campanha** gera o link de comissão e abre `/integracoes/whatsapp/agendador` com mensagem e imagem.
- [x] Sync de grupos monitorados com try/catch, log do stack trace, UserId fallback 1 e falha isolada WhatsApp/Telegram.
- [x] `IUserContextProvider` registrado no DI da API (corrige 500 na tela de grupos monitorados).
- [x] HTTP 401 na listagem/sync não força `NavigateTo("/")`; o menu e a tela permanecem e o login sem JWT segue via `HttpService`.
- [x] Sincronizar Telegram aguarda o catch-up UserBot (48h / 1500 msgs) e avisa se a sessão MTProto estiver offline.
- [x] Clonar Oferta desencurta HEAD/GET, converte comissão, grava no histórico de `/gerador-links` (`SourceGroup`) e oferece Agendar Disparo.
- [x] Feed Telegram filtra lojas ativas + URL de produto; Sem interesse / Restaurar; ações do card em grid 2x2.

### 🧠 Fase 28: Motor de Inteligência, Mineração e Arbitragem de Ofertas (Radar & Mining Bot)
- [x] 28.1. Perfil e Parâmetros de Mineração do Afiliado:
  - Tela `/radar/perfil` com nichos, ticket médio, comissão mínima e restrição às lojas ativas (Shopee, Mercado Livre, Amazon, TikTok Shop).
- [x] 28.2. Arbitragem de Preços e Cupons em Tempo Real (Gerador):
  - Após converter URL, busca cruzada em segundo plano e cards com menor preço, cupom e link convertido.
- [x] 28.3. Garimpo Automático, Tendências e Social Listening (Worker 24/7):
  - `OfferMiningWorker` monitora quedas vs média histórica, movers e menções TikTok/Reels.
- [x] 28.4. Feed Recomendador e Piloto Automático (Auto-Agendamento):
  - Painel `/radar-ofertas` com score, envio ao agendador WhatsApp/Telegram e fila do piloto automático.

## 🚀 Fase 29: Validação Inteligente e Atribuição de Vendas (Diferenciais Exclusivos)

- [x] **29.1. Monitor de Saúde do Cupom e Estoque:** `OfferIntelligenceWorker` valida anúncios após o disparo e grava alertas em `/saude/ofertas`.
- [x] **29.2. Rastreamento por SubID (Mapeamento de Lucro por Grupo):** o disparo injeta `tf_src`/`tf_grp`/`sub_id`; o clique no encurtador alimenta `/atribuicao/grupos`.
- [x] **29.3. Biblioteca Evergreen & Reciclador de Ofertas Campeãs:** ranking por cliques e reciclagem automática em intervalos vazios (`/biblioteca-evergreen`).
- [x] **29.4. Moldura Dinâmica e Mídia Rica (Vídeos sem Marca d'Água):** estúdio em `/estudio-midia` aplica faixa promocional e baixa `og:video`.

## 🛡️ Fase 30: Validação Pré-Disparo e Saúde de Agendamentos (Pre-Flight Check)

- [x] **30.1. Validação de Integridade Pré-Envio (Pre-Flight Worker):**
  - `PreFlightWorker` inspeciona agendamentos nos 15 minutos finais; o disparo WhatsApp/Telegram só segue se `EnsureReadyAsync` passar.
  - Pausa automática (`Paused`) quando o preço sobe, o cupom some da página ou o produto esgota.

- [x] **30.2. Central de Notificações e Reagendamento:**
  - Painel `/saude/agendamentos` com o motivo da pausa (ex: "Cancelado: Preço alterado de R$ 49 para R$ 89").
  - Botão de substituição pelo menor preço nas lojas concorrentes, recolocando o disparo na fila.

## 💎 Fase 31: Módulo Inteligente de Achadinhos e Histórico de Preços (Deal Intelligence)

- [x] **31.1. Mecanismo de Menor Preço Histórico (`PriceHistoryTracker`):**
  - **Modelagem de Dados (`ProductPriceHistory`):** Tabela de histórico compacta indexada por `Platform` (Mercado Livre, Shopee, Amazon) + `PlatformProductId` (SKU numérico extraído da URL/API) e `Price`.
  - **Normalização de SKUs:** Extração do ID do produto independente do título anunciado (ex: `MLB1234567890` no Mercado Livre, `item_id` + `shop_id` na Shopee) para associar variações de postagens ao mesmo item.
  - **Cálculo de Desconto e Volatilidade:** Algoritmo que calcula o menor valor registrado nos últimos 30, 60 e 90 dias, além do preço médio do período.
  - **Interface Blazor (`MonitoredGroups.razor` / `Achadinhos.razor`):** Renderização de badges visuais em tempo real no Card de Oferta:
    - `<span class="badge bg-fire">🔥 Menor Preço dos Últimos 30 Dias</span>`
    - `<span class="badge bg-success">🏷️ Economia de R$ X vs. Média Histórica</span>`

- [x] **31.2. Gestão de Créditos de Ofertas Vips por Tenant (`TenantDealCredits`):**
  - **Tabela de Controle (`TenantCredits` & `CreditTransactions`):** Gestão de saldo por `TenantId`, registrando entradas (renovação do plano, compras de pacotes) e saídas (consumo por liberação de Achadinho).
  - **Regra de Cota Diária (Reset Atômico):** Renovação automática de 5 créditos diários para o Plano Básico todo dia à meia-noite (UTC). Os créditos da cota diária expiram em 24h (não acumulativos), incentivando o engajamento diário.
  - **Lógica de Desbloqueio Paywall na UI:**
    - Ofertas classificadas como "Super Achado" nascem com visual estético desfocado/bloqueado (`blur effect`) na vitrine VIP.
    - Botão no Card: `<button>🔒 Desbloquear esta oferta (1 Crédito)</button>`.
    - Modal de Top-Up: Quando o saldo zera, exibe opções de pacotes avulsos com desconto progressivo por volume (+10, +50, +100 créditos).

- [x] **31.3. Worker Global de Rastreamento de Tendências (`GlobalTrendingDealsWorker`):**
  - **Serviço em Segundo Plano Centralizado (`IHostedService`):** Worker executado no nível do sistema (sem vínculo com tenant específico) para varrer produtos em alta (*Trending Deals*).
  - **Algoritmo de Atratividade:** Cruzamento de alta taxa de engajamento nos grupos (mensagens duplicadas/encaminhadas com frequência) com redução súbita de preço (`PriceDropPercent >= 15%`).
  - **Vitrine Global ("Pool de Achadinhos"):** Ingestão na tabela `GlobalTrendingDeals` para disponibilização e consumo via créditos por todos os assinantes do TecFlow.


---

## Entregas equivalentes (pÃ³s Fase 31)

## 🤖 UX e Validação da Conexão Telegram (BotFather Modal)

1. **Validação e Feedbacks do Botão:**
   - [x] Ao clicar em "Validar e Conectar Bot", o sistema valida o formato da chave (Bot Token). Se estiver vazio ou preenchido com asteriscos, cancela o envio e exibe um alerta explicativo.
   - [x] Trata exceções da API da Telegram (como token inválido/inexistente ou erro de permissão no Webhook) exibindo toasts/alertas visuais sem travar o componente Blazor.
   - [x] Spinner `_isLoading` no botão; `GetMeAsync` 401 aborta com "Token do Telegram inválida".
   - [x] Falha no `SetWebhookAsync` (localhost) não impede salvar credenciais: status **Conectado (Modo Disparo)** + aviso informativo.

2. **Modal Auxiliar "Saiba onde obter o código":**
   - [x] Ao lado do campo Bot Token, incluir o botão/link **"Saiba onde obter o código"**.
   - [x] Ao clicar, exibe um modal estilizado do TecFlow com o passo a passo ilustrado de como criar o robô no `@BotFather` e copiar o token gerado.

## 🤖 Resiliência e Fallback na Conexão do Telegram

Para garantir o funcionamento contínuo em ambientes de Desenvolvimento (Localhost) e Produção:
1. **Fallback de Webhook em Localhost:** Caso a API do Telegram recuse o registro do Webhook (`SetWebhookAsync`) por ausência de uma URL pública HTTPS, o sistema **não bloqueia a conexão**.
2. **Disparo Garantido via Chat ID:** O registro é salvo na base de dados com status ativo, e o sistema utiliza o **Chat ID do Canal** informado manualmente (ex: `-100707440297`) para realizar o envio das campanhas agendadas.
3. **Feedback Amigável:** A interface exibe o aviso: *"Conexão salva! O webhook para escuta automática não pôde ser ativado em ambiente local, mas os disparos agendados para o Chat ID informado funcionarão normalmente."*

## 🏷️ Identificação Visual Dinâmica nas Telas de Agendamento

Nas páginas de agendamento (`/integracoes/whatsapp/agendador` e `/integracoes/telegram/agendador`):
- [x] **Cabeçalho Dinâmico:** O título principal "Disparo para grupos" deve ser acompanhado imediatamente por um badge/ícone oficial representando a rede ativa:
  - **WhatsApp:** Badge com fundo verde (`#25D366`), ícone do WhatsApp e texto "WhatsApp".
  - **Telegram:** Badge com fundo azul (`#0088cc`), ícone do Telegram e texto "Telegram".
- [x] **Identificação Visual Instantânea:** O componente deve alternar automaticamente as cores e ícones conforme a rota atual do Blazor.
- [x] Agendador Telegram alinhado ao WhatsApp: **Sincronizar Meus Canais do Telegram**, multi-select de canais e disparo com intervalo.

## 🤖 Sincronização de Grupos/Canais no Agendador Telegram

Para equiparar a experiência ao agendador do WhatsApp:
1. [x] **Botão de Sincronização:** Adicionar o botão "Sincronizar Meus Canais/Grupos do Telegram" no topo do agendador (`/integracoes/telegram/agendador`).
2. [x] **Consulta via API:** Ao clicar, o sistema deve invocar `GetUpdates` ou consultar os chats gerenciados pelo BotToken ativo e salvar em `TelegramGroups`.
3. [x] **Nome amigável:** `GetChatAsync` grava `chat.Title` (ex: *achadinhos*) em `TelegramGroups.Name`/`GroupName`.
4. [x] **Componente Multi-Select:** chips com nome + Chat ID, busca em tempo real, Selecionar Todos / Limpar Seleção e `SelectedChatIds`.

## 🕵️ Monitoramento de Canais de Terceiros como Membro Leitor (UserBot MTProto)

Para capturar ofertas em canais onde o usuário é apenas membro (sem privilégios de Admin):
1. [x] **Credenciais MTProto:** Configuração de `ApiId` e `ApiHash` no TecFlow para conectar uma conta de usuário do Telegram.
2. [x] **Escuta Passiva em Background:** O Worker (`TelegramUserMonitorWorker`) intercepta novas mensagens em todos os canais inscritos da conta.
3. [x] **Catch-up histórico paginado:** o botão *Sincronizar* **aguarda** `Messages_GetHistory` nos chats (`Dialogs` ou `DialogsSlice`, até **1500** msgs/canal ou **48h**) via `ToInputPeer`, persiste links (texto, entidade e botão) e só então recarrega a lista.
4. [x] **Fila desacoplada:** `Channel<UserBotCapturedPayload>` recebe o update MTProto sem bloquear o `WTelegramClient`; um leitor persiste no SQL. `HostOptions` + `requestTimeout="20:00:00"` no IIS evitam derrubar o `IHostedService`.
5. [x] **Extração de Ofertas:** regex HTTP ampla, Kabum/`shp.ee` e URLs de botão inline, salvando em `GroupCapturedMessages` com o canal de origem.
6. [x] **Grupos monitorados sem teto fixo:** a API pagina ofertas (`skip`/`take` 50) e a tela carrega mais 50 até exibir todas do período.

## 🛠️ Permissões de Sistema de Arquivos (Telegram Sessions)

O serviço de escuta do UserBot (`WTelegramClient`) requer acesso de leitura/escrita na pasta local de sessões:
- **Diretório:** `C:\ProgramData\TecFlow\telegram-sessions` (fora do publish IIS; o wipe de `inetpub` não apaga o `.session`).
- **Permissão do IIS:** `IIS_IUSRS` e o `TecFlowApiPool` com **FullControl** nessa pasta (`Configurar-Logs-IIS.ps1`).
- [x] **Fallback TEMP:** se a criação da pasta falhar, a sessão vai para `%TEMP%\TecFlow\telegram-sessions` e a sincronização de canais via Bot Token segue independente.

## 🤖 UX e Manual do UserBot MTProto (Escuta de Canais de Terceiros)

1. [x] **Modal de Instruções ("Como configurar o UserBot"):**
   - `UserBotHelpModal.razor` com 7 passos, alertas amarelo/azul, link `https://my.telegram.org` e botão Copiar exemplo (`28471934`).

2. [x] **Fluxo de Login em 2 Passos (Two-Step Phone Auth):**
   - **Passo 1:** O usuário informa o `api_id` numérico, o `api_hash` e o telefone E.164 e clica em "Solicitar Código". PIN e "Confirmar e Autenticar" só habilitam após HTTP 200. Helpers no formulário; ApiId rejeita Bot Token (`:`) e Chat ID (`-100`).
   - **Passo 2:** Após `SolicitarCodigo()` com sucesso, `isCodeInputDisabled` fica `false`. Em erro de formato, o usuário pode habilitar o PIN manualmente. **Confirmar e Autenticar** chama `MakeAuthAsync` e grava o `.session`.

## 🧬 Motor de Clonagem e Desencurtamento de Links (Clone & Convert Engine)

- [x] **Desencurtador e Follow Redirects em Background:**
  - `IUrlResolverService` resolve o link capturado com HEAD/GET (`AllowAutoRedirect`) até a URL canônica da loja.
- [x] **Conversão Dinâmica por Conta de Afiliado:**
  - `AffiliateLinkConverterService` troca a tag de terceiro pela comissão do usuário logado e extrai título, preço e imagem.
- [x] **Integração Grupos Monitorados -> Gerador de Links (`/gerador-links`):**
  - Clonar Oferta grava `ShortAffiliateLinks` (histórico) com `OriginalUrl`, `AffiliateUrl`, metadados e `SourceGroup`; toast de sucesso e atalho **Agendar Disparo**.

## 🧹 Filtros Inteligentes e Curadoria no Monitor de Grupos

- [x] **Filtragem Dinâmica por Integração Ativa:** Exibição exclusiva de ofertas das plataformas integradas na conta do usuário (Shopee, ML, Amazon, etc.).
- [x] **Expurgo Automático de Comunicados Sem Produto:** Algoritmo para descartar avisos informativos, banners de eventos e textos sem URLs diretas de checkout.
- [x] **Sistema de Uninterest / Feedback de Descarte:**
  - Botão "Sem interesse" para ocultar posts irrelevantes e treinar o filtro do usuário.
  - Aba de alternância "Ocultos / Descartados" para auditoria e restauração de mensagens.
- [x] **Redesign do Card (Grid 2x2):** Compactação dos botões de ação em duas linhas para melhor aproveitamento do espaço visual.

## 🛠️ Resiliência no Parser de Ofertas (Imagens, Validação e Scraping de Preço)

- [x] **Extração Dupla de Mídia (Telegram Media + OpenGraph Scraper):**
  - Download da foto direta da mensagem do Telegram ou fallback via extração da tag `og:image` no link final da loja.
- [x] **Filtro de Páginas Quebradas/Esgotadas (Validation Ping):**
  - Verificação de redirecionamento e expurgo automático de links que retornam erros conhecidos da Shopee/Mercado Livre (ex: "loja falhou ao carregar").
- [x] **Parser Inteligente de Título e Preço (Regex + Fallback HTML):**
  - Normalização de captura de valores (`R$`, `R$ `, `,00`) e fallback para as meta tags do e-commerce caso o texto do canal venha formatado de forma atípica.

  ## 🖼️ Gerenciamento e Ciclo de Vida de Imagens Capturadas (Media Pipeline)

- [x] **Armazenamento Segregado por Tenant/Data:**
  - Fotos do UserBot em `wwwroot/uploads/products/{TenantId}/{Ano}/{Mes}/` com nome `{messageId}_{guid8}.jpg` e caminho relativo em `GroupCapturedMessages.ProductImageUrl`.
- [x] **Rotina de Limpeza Automática (ProductImageCleanupWorker):**
  - Worker diário apaga `.jpg` com mais de 15 dias em `uploads/products/` e zera `ProductImageUrl`.
- [ ] **Disparo de Mídia Nativa nos Canais:**
  - Envio do arquivo de imagem físico armazenado em disco para as APIs de envio (WhatsApp/Telegram) ao agendar ou clonar ofertas.

  ## ⚡ Sincronização Assíncrona de Grupos (Timeout Prevention)

- [x] **Desacoplamento de Requisição HTTP:**
  - `POST .../sincronizar` devolve HTTP 202 e enfileira o catch-up no `TelegramUserMonitorWorker` (`Channel`), sem aguardar as 1500 mensagens/48h.
- [x] **Aumento de Timeout e Feedback Visual:**
  - `HttpClient` Orquestrador com timeout mínimo de 3 minutos e badge "Sincronizando novas ofertas em background..." em `/integracoes/telegram/grupos/monitorados`.
   
   ## 🛡️ Tratamento de Exceções e Resiliência no UserBot Background Worker

- [x] **Garantia de Criação de Diretórios na Inicialização:**
  - Verificação e criação proativa de `App_Data/telegram-sessions` e `wwwroot/uploads/products` na inicialização do serviço (`IHostedService.StartAsync`).
- [x] **Log Estruturado e Captura de Falhas no Job em Segundo Plano:**
  - Inclusão de blocos `try-catch` globais no loop do Worker para evitar crash silencioso da thread de background.
  - Exibição de alertas de erro de conexão/autenticação no feed do Blazor caso o Worker encontre exceções no WTelegramClient.

  ## 🧠 Engine Inteligente de Parsing e Structuring de Ofertas (Telegram Post Parser)

- [x] **Extração Estruturada por Expressões Regulares Avançadas / AI:**
  - **Título:** Normalização e remoção de emojis e prefixos visuais (`🟡`, `👌`, `🔥`).
  - **Preço:** Captura resiliente de valores numéricos (`✅ R$ 50,91`, `💲 Valor: R$479`).
  - **Cupom:** Mapeamento de códigos promocionais com palavras-chave (`CUPOM:`, `Cupom`, `Code:`).
  - **Link Principal:** Discriminação entre URL de checkout do produto e links institucionais/vitrines da campanha.

  ## 🧠 Parser de Ofertas Estruturadas & Renderização Reativa (Parsing & UI Pipeline)

- [x] **Engine Extrator de Ofertas (`StructuredOfferParserService`):**
  - Isolamento de Título, Preço (`decimal`), Código de Cupom e URL Canônica do Produto via Regex avançado.
- [x] **Sanitização de Caminho de Imagens (`ImageUrl` Web Path):**
  - Conversão obrigatória de separadores do SO (`\`) para barras de URL web (`/`) ao salvar caminhos estáticos em `wwwroot/uploads/`.
- [x] **Exibição Estruturada no Blazor (`/integracoes/telegram/grupos/monitorados`):**
  - Renderização de badges visuais para Cupons ativos, preço formatado em BRL (`C2`) e recarga reativa da grid via botão "Atualizar Feed".

## 📄 Paginação Dinâmica e Priorização Visual de Mídia (Paging & On-Demand Downloader)

- [x] **Seletor de Tamanho de Página (10, 25, 50, 100):**
  - Implementação de paginação de dados com controle dinâmico de `PageSize` no Blazor e no repositório LINQ (`Skip/Take`).
- [x] **Download Prioritário por Página Ativa:**
  - Otimização do Worker de imagens para priorizar o download das mídias referentes aos itens da página visível no momento.
- [x] **Indicador de Status do Lote e Skeleton Loader:**
  - Carregamento progressivo de mídia mantendo a interface leve e responsiva.

## 🖼️ Recuperação e Forçamento de Download de Mídia (Media Downloader Fix)

- [x] **Garantia de Stream em Disco no UserBot:**
  - Invocação explícita do `client.DownloadMediaAsync` para salvamento físico do arquivo `.jpg` na pasta `wwwroot/uploads/products/{TenantId}/{Ano}/{Mes}/`.
- [x] **Tratamento Fallback de `WebRootPath`:**
  - Resolução segura do caminho físico do servidor evitando exceções de diretório inexistente.
- [x] **Reprocessamento de Imagens Pendentes (Backfill Worker):**
  - Rotina para verificar mensagens no banco sem `ImageUrl` e refazer o download a partir do ID da mensagem no Telegram.

## ⚡ Atualização Cirúrgica e Assíncrona de Mídia (Non-Blocking UI & Fast SQL Update)

- [x] **Desconexão Total do Render da UI e do Download:**
  - A consulta Blazor utiliza queries otimizadas (`AsNoTracking` + `Skip/Take`) para resposta em sub-100ms.
- [x] **Gravador Direto de Caminho de Imagem (`UpdateImageUrlAsync`):**
  - Atualização direta da coluna `ImageUrl` no SQL Server via `MessageId` assim que o arquivo é gravado no disco, sem recarregar entidades inteiras na memória.
- [x] **Fallback de Interface Instantâneo:**
  - Renderização de texto/oferta imediata mesmo durante o processamento em lote de imagens pelo Worker.
  
  ## 🐞 Correção de Vínculo e Persistência de Mídia (Media Identity & DB Context Fix)

- [x] **Mapeamento Unificado por MessageId Real:**
  - Garantia de correspondência exata entre o ID da mensagem no Telegram (`msg.id`) e a chave primária/índice na tabela `GroupCapturedMessages`.
- [x] **Escopo Seguro de DbContext no Worker (`IServiceScopeFactory`):**
  - Criação explícita de escopo de banco de dados para a execução de `ExecuteSqlRawAsync` no Worker de background.
- [x] **Auditoria de URLs de Mídia:**
  - Sanitização com prefixo obrigatório `/` em todas as rotas de imagem estática.
  
  ## 🎯 Correção Crítica de Vínculo de Mídia e Priorização de Tela

- [x] **Mapeamento Rígido por `TelegramMessageId`:**
  - O nome do arquivo no disco DEVE iniciar rigorosamente com `{TelegramMessageId}.jpg` para permitir amarração direta no SQL.
- [x] **Priorização de Download para a Página Atual (Paging First):**
  - O Worker deve receber explicitamente a lista dos `TelegramMessageId` presentes nos 25 cards da página atual do Blazor e baixar ESSAS imagens em prioridade máxima.

  ## 🎯 Amarração Estrita de Nome de Arquivo e Injeção de Imagem (`MessageId Sync`)

- [x] **Padronização do Nome de Arquivo Físico:**
  - O arquivo físico no disco DEVE ser salvo estritamente com o nome `{TelegramMessageId}.jpg` para vincular automaticamente ao registro do banco de dados.
- [x] **Script de Vínculo Retroativo por Prefix/ID (`LinkDownloadedImages`):**
  - Mapeamento e associação direta dos arquivos já baixados em `wwwroot/uploads/products/` para os registros da página ativa no Blazor.

  ## ⛓️ Esteira de Processamento e Empacotamento Atômico de Ofertas (Atomic Pipeline)

- [x] **Arquitetura de Pacote Validado (Pipeline Pattern):**
  - Processamento em memória das mensagens capturadas do Telegram: Parse ➔ Download de Imagem ➔ Validação de Link ➔ Gravação Atômica no SQL.
- [x] **Garantia de Integridade na Tela:**
  - Exibição exclusiva de registros cujo pacote de dados (Título, Preço, Cupom e Caminho de Mídia em Disco) esteja 100% validado e persistido.

  ## 🧹 Purge e Reset de Dados de Monitoramento (Purge & Full Resync)

- [x] **Mecanismo de Reset Total de Captura:**
  - Expurgo completo dos registros na tabela `GroupCapturedMessages` (`DELETE WHERE UserId`) e exclusão física arquivo a arquivo em `wwwroot/uploads/products/{userId}` (ignora arquivos em uso no IIS).
- [x] **Disparo de Re-Sincronização Atômica:**
  - Botão **Limpar** (`HandleResetAllAsync`) só expurga SQL/disco; a recarga atômica fica no botão **Sincronizar grupos**.
- [x] **Limite temporário de 10 ofertas:**
  - Removido. Paginação voltou a 10/25/50/100 (padrão 25) e o catch-up UserBot usa o histórico completo.
- [x] **Deduplicação do feed monitorado:**
  - Nova oferta só entra se ainda não existir na tela/SQL pelo `ExternalMessageId` ou URL do produto (sem query string).
- [x] **IIS 500.30 (API):**
  - Seed de homologação não derruba mais a API; o pool `TecFlowApiPool` precisa de login Windows no SQL Server `AutomacaoSociais`.



## 🔍 Fase 32: Módulo de Busca Inteligente e Integração com APIs Oficiais (Live Search & API Integration)

- [x] **32.1. Buscador de Ofertas com Validação em Tempo Real (`LiveCheckSearchService`):**
  - **Barra de Busca com Indexação Full-Text:** Campo de busca rápida no topo da tela de grupos monitorados e achadinhos, filtrando por palavra-chave, faixa de preço, cupom e loja.
  - **Estratégia de Validação *Pre-Flight* (Live Check):**
    - Ao selecionar uma oferta para disparar ou clonar, o serviço executa um ping assíncrono para a API da loja correspondente via ID do produto.
    - **Checagem de Anúncio:** Retorna se a oferta/campanha permanece ativa ou se o anúncio foi pausado/encerrado na origem.
    - **Checagem de Variação de Preço/Cupom:** Se o preço na API for menor do que o capturado no banco ou houver um novo cupom ativo, atualiza a entidade e o card em tempo real antes de enviar ao canal do cliente.

- [x] **32.2. Ingestão Gratuita via APIs Oficiais de Afiliados:**
  - **Mercado Livre Developers API (`MercadoLivreApiService`):**
    - Consulta de endpoints públicos `/items/{Item_ID}` e `/offers` sem necessidade de token de usuário final.
    - Captura automática de `original_price`, `price`, `status`, estoque disponível e condições de frete.
  - **Shopee Affiliate Open API (`ShopeeApiService`):**
    - Integração via `AppKey` / `AppSecret` com assinatura HMAC-SHA256 no header da requisição.
    - Consulta ao endpoint `productOfferV2` para buscar detalhes do item, taxa de comissão e cupons de loja vigentes.
  - **Amazon Product Advertising API (`AmazonPaApiService` - PA-API 5.0):**
    - Integração HMAC via chave de associado Amazon para requisições `GetItems` e `SearchItems`.
    - Mapeamento de ofertas em tempo real com renovação de cota de chamadas atrelada às vendas da conta.    
- [x] **32.3. Filtro Local e Live Check no Workflow:**
  - Filtro em memória/LINQ nas telas de Grupos Monitorados e Achadinhos VIP.
  - Re-checagem ao vivo (Live Check) de status, preço e cupom por SKU no clone/desbloqueio.
- [x] **32.4. Motor de Descoberta por Palavra-Chave nas APIs Oficiais:**
  - Implementação de endpoints de busca textual nas APIs do Mercado Livre (`/sites/MLB/search`), Shopee (`productOfferV2`) e Amazon (`SearchItems`).
  - Mapeamento das credenciais corporativas da Amazon PA-API no `appsettings.json`.
- [x] **32.5. Interface Dedicada de Busca Omnichannel (`/busca`):**
  - Criação da página Blazor `/busca` integrada ao menu principal para exibição da vitrine unificada de resultados das lojas oficiais + base interna.
- [x] **Isolamento de DbContext na busca paralela:** `IDbContextFactory<AppDbContext>` scoped por task no `WhenAll`.
- [x] **SearchTelemetryUI:** badges por plataforma (encontrados / 0 / chave ausente) e log estruturado `ML/Shopee/Amazon/Local`.
- [x] **MercadoLivreSearchFix:** User-Agent Chrome/120, DTO `results`, Bearer OAuth da conta/`appsettings` e badge `⚠️ Requer conta conectada no painel`.
- [x] **Botão Conectar no badge ML:** redireciona para Minhas Lojas com modal de Mercado Livre.

    

## 🧹 Fase 33: Plano de Expurgo e Retenção de Dados (Data Retention & Purge Pipeline)

- [x] **33.1. Worker de Expurgo Automatizado (`DataPurgeWorker`):**
  - **Rotina Diária de Manutenção (Cron Job às 03:00 AM UTC):** Worker de segundo plano projetado para execução contínua com baixo impacto de I/O em disco e CPU.
  - **Exclusão de Mídia Física em Disco (`PurgeOldMediaFiles`):**
    - Varredura na pasta `wwwroot/uploads/products/{TenantId}/{yyyy/MM}/`.
    - Apagamento físico de todos os arquivos `.jpg`/`.png` cujos registros associados na tabela tenham sido criados há mais de 7 dias.
    - Atualização do campo `ImageUrl = NULL` ou caminho relativo para o placeholder padrão da aplicação.

- [x] **33.2. Condensação de Histórico de Preços (`ColdStorageArchiver`):**
  - **Migração Pré-Deleção (*Extract-Transform-Archive*):**
    - Antes da exclusão da mensagem capturada, o serviço extrai as métricas de precificação: `{ Platform, PlatformProductId, Price, CouponCode, CapturedAt }`.
    - Insere o registro condensado de poucas bytes na tabela `ProductPriceHistory`.
  - **Preservação da Memória Analítica:** Garante que o histórico de preços para os badges de "Menor Preço em 30 Dias" permaneça intacto mesmo após o descarte do texto longo e da imagem física.

- [x] **33.3. Manutenção de Alta Performance na Tabela Principal:**
  - **Hard Delete na Tabela `GroupCapturedMessages`:**
    - Execução da instrução SQL em lotes (*Batch Delete*) para prevenir *table lock* no SQL Server:
      `DELETE TOP (5000) FROM GroupCapturedMessages WHERE CreatedAt < DATEADD(day, -14, GETUTCDATE())`
  - **Manutenção de Índices:** Reorganização/reconstrução automática dos índices da tabela principal após o expurgo para garantir que as queries de paginação Blazor (`Skip`/`Take`) continuem respondendo em sub-100ms.

## 🖼️ Fase 34: Otimização, Padronização e Regra Estrita de Mídia (Media Pipeline & Compression)

- [x] **34.1. Regra Estrita de Ingestão de Imagens (`StrictImageIngestionPolicy`):**
  - **Interceptador Único de Salvamento:** Nenhuma imagem do Telegram, upload manual ou scraping pode ser salva na pasta `wwwroot/uploads/` sem passar obrigatoriamente pelo pipeline de validação e otimização.
  - **Rejeição/Ajuste de Formatos Inválidos:** Conversão forçada de arquivos pesados (PNGs sem transparência, BMPs, TIFFs ou JPEGs não otimizados) para o padrão leve da plataforma.

- [x] **34.2. Processamento e Normalização Técnica (`ImageOptimizationService`):**
  - **Redimensionamento Proporcional (Aspect Ratio Preserved):** Limite estrito de **1080px no maior lado** (largura ou altura), garantindo excelente definição sem distorção em telas mobile, notebooks e monitores ultrawide.
  - **Compressão Dinâmica e Formato WebP/JPEG:** Aplicação de qualidade adaptativa de 75% a 80%, reduzindo o peso final de cada foto para a faixa ideal de **80 KB a 250 KB** (economia de até 85% de espaço em disco e tráfego de rede).
  - **Remoção de Metadados (EXIF Stripping):** Expurgo automático de dados de geolocalização e perfis de cor para garantir segurança, privacidade e menor tamanho de payload.

- [x] **34.3. Adaptação Responsiva para Canais de Disparo (WhatsApp & Telegram Layouts):**
  - **Preview Perfeito Multi-Tela:** Mapeamento de proporção que evita cortes indesejados nas miniaturas do WhatsApp Web, WhatsApp Mobile e Telegram Desktop.
  - **Fallback com Canvas Neutro:** Aplicação automática de fundo neutro em imagens com proporções extremas (muito compridas ou verticais) para exibição elegante nos cards do Blazor e nos canais de mensagem.

## 💡 UX — Conexão simplificada do Mercado Livre (`MLAffiliateUX`)

- [x] **Campo de conexão:** rótulo `Link de Afiliado ou ID do Mercado Livre` e placeholder para colar o link de divulgação.
- [x] **Parser automático:** dígitos puros, `matt_tool=([0-9]+)` em links `http(s)` e expansão de `meli.la` / `/sec/` via `UrlUnshortenerService`.
- [x] **Como pegar meu ID?:** `HowToGetStoreIdModal.razor` com passos para Computador e Telemóvel (App), sem jargão técnico.

## 🐞 Leitura de loja ativa no Live Search (`PlatformEnumResolution`)

- [x] **Consulta de `MarketplaceAccounts`:** enum `MarketplaceType.MercadoLivre` ou nome sem espaços (`Mercado Livre` → `MercadoLivre`).
- [x] **Affiliate ID:** `TrackingId` / `AffiliateTrackingId` (ex.: `14343296`) considera a loja conectada e parametriza `matt_tool` nos permalinks da `/busca`.
---
*Nota para a IA: Sempre siga este roadmap passo a passo e use a nova estrutura de pastas estabelecida. Não pule etapas e preze pela preservação do código de validação já existente.*
