# Registro Histórico de Fases Concluídas do Ofertio

Este arquivo é o **Registro Histórico de Fases Concluídas** do Ofertio.

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
- [x] Mudar o nome da Solution e dos projetos de 'Tecso' para 'Ofertio'.
- [x] Estruturar o projeto **`Ofertio.Business`** com as pastas:
  - `Dto/` (Transições e as classes padrão de Response).
  - `Enum/` (Enumeradores separados por conceitos de domínio).
  - `Service/` (Serviços, regras de negócio e onde ficarão as classes herdadas de Criptografia e Validação).
- [x] Estruturar o projeto **`Ofertio.Database`** com as pastas:
  - `Entity/` (O coração do projeto).
  - `Filter/` (Objetos de busca baseados nas Entities).
  - `Interface/` (Contratos dos repositórios).
  - `Pagin/` (Classe de controle de paginação limitada a 30 registros).
  - `Repositorio/` (Implementação das consultas e queries que consomem os Filters).
- [x] Mover o `DbContext` existente para a raiz ou pasta adequada do `Ofertio.Database`.

### Fases Concluídas e Preservadas (A serem validadas na nova estrutura) ✅
- [x] Instalação e configuração do PostgreSQL local (Porta 5432, base: `automacaosociais`).
- [x] Criação do serviço de criptografia (agora alocado em `Ofertio.Business/Service`).
- [x] Implementação no `ValidationHelper` das validações de E-mail, CPF, CNPJ Alfanumérico, CEP (ViaCEP) e Força de Senhas.
- [x] Implementação da validação rigorosa de celular brasileiro (`IsValidBrazilianCellPhone`).
- [x] Estrutura base do Frontend criada (agora migrando para o projeto **`Ofertio.WebUi`**).
- [x] Telas iniciais de escolha (TikTok/Shopee) e cards de login mockados.
- [x] Implementar fluxo de captura e persistência de Telefone/WhatsApp (com aplicação da validação do helper) para alertas do Orquestrador.

### Fase 2: Implementação dos Componentes Base do Banco e Modelagem ⚙️
- [x] Criar a classe base de paginação na pasta `2.Database/Pagin/` travando os resultados em 30 registros.
- [x] Criar a primeira Entity oficial (`User`) na pasta `Entity/`.
- [x] Criar o objeto espelho `UserFilter` na pasta `Filter/`.
- [x] Criar o `UserDto` e `UserResponseDto` na pasta `1.Business/Dto/`.
- [x] Configurar a Connection String do PostgreSQL e aplicar a primeira Migration do ecossistema Ofertio.

### Fase 3: Regras de Negócio, Telas e APIs Externas 🔄
- [x] Refatorar Controllers para arquitetura de 3 objetos (Filter / Dto / ResponseDto) e remover Create/Update Dtos redundantes.
- [x] Migrar o projeto 'Ofertio.Portal' e adaptar o projeto `Ofertio.WebUi` para consumir os novos Services que retornam o padrão `ResponseDto`.
- [x] Descontinuar `Ofertio.Portal` e `Ofertio.Dashboard`; **`Ofertio.WebUi`** é o frontend canônico (Blazor).
- [x] Integrar os componentes de tela aos filtros de listagem compostos pela pasta `Filter`.
  - [ ] Integração real com as APIs de produção do TikTok Shop e Shopee.
  - [x] 3.1. Infraestrutura Core de Integração: Criar os HttpClient específicos, handlers de resiliência (Polly) e logs de requisições.
  - [x] 3.2. Fluxo de Autenticação & OAuth2: Implementar a geração de URL de autorização, captura do Authorization Code e armazenamento seguro/renovação automática do Access Token e Refresh Token para ambas as plataformas.
  - [x] 3.3. Sincronização de Catálogo (Produtos): Implementar mapeamento de payloads, busca de produtos das plataformas e conversão para o nosso padrão [Nome]ResponseDto.
  - [x] 3.4. Gestão de Pedidos & Estoque: Estruturar os endpoints/serviços para receber webhooks ou realizar polling de novos pedidos e atualizar estoque de forma bidirecional.

### Fase 4: Estratégia Mobile Híbrida (Android e iOS) 📱
- [x] 4.1. Fundação Mobile-First no Ofertio.WebUi e Contratos de API
  - [x] Auditar e refatorar layouts existentes do `Ofertio.WebUi` para conformidade Mobile First (breakpoints, tabelas responsivas, navegação touch-friendly).
  - [x] Padronizar contratos REST (`*Filter`, `*ResponseDto`) e autenticação para consumo estável por clientes móveis futuros.

- [x] 4.2. Shell Híbrido Nativo (.NET MAUI / Blazor Hybrid)
  - [x] Avaliar e estruturar projeto compartilhado (RCL) reutilizando componentes Blazor do `Ofertio.WebUi`.
  - [x] Configurar pipeline de build e publicação para Android e iOS (lojas ou distribuição interna).

- [x] 4.3. Engajamento Móvel em Tempo Real
  - [x] Integrar notificações push (FCM/APNs) para alertas de comentários, comissões e falhas de webhook.
  - [x] Implementar deep links para abrir diretamente painéis de conciliação e fila de engajamento no app.



### Fase 5: Visão de Negócio - Plataforma de Automação e Inteligência para Afiliados de Alta Escala 🚀
**[x] Revisado e validado (jun/2026)** — domínio base, enums globais e contratos de orquestração consolidados em `Ofertio.Core` e `Ofertio.Business` para API, Worker, Orquestrador e WebUi.

Orquestração de engajamento (comentários, mensagens e links), conciliação financeira de comissões, catálogo de produtos de divulgação e integrações com TikTok Shop e Shopee. O ecossistema combina backend robusto em C# (`Ofertio.API`, `Ofertio.Worker`, `Ofertio.Orquestrador`) com o frontend `Ofertio.WebUi` / `Ofertio.SharedUi` / `Ofertio.Mobile` para controle, auditoria e monitoramento em produção.

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

- [x] 6.2. Painel de Conciliação Financeira de Afiliado (Ofertio.WebUi / SharedUi)
  - [x] Página `ConciliacaoFinanceira.razor` mobile-first (KPIs em grid, tabela desktop / cards mobile com badges de divergência).
  - [x] `IAffiliateAnalyticsService` — importação de relatórios Shopee/TikTok (tokens válidos) + fallback pedidos locais.
  - [x] `ReconcileCommissionsAsync` — cruza rastreio local vs. repasse marketplace e classifica divergências (`CommissionDiscrepancyReportDto`).
  - [x] API Orquestrador: `GET /api/afiliados/analytics/conciliacao`.

- [x] 6.3. Mecanismo de Mapeamento de Produtos e Atributos Globais para Propaganda
  - [x] Entidades `GlobalAdvertisingProduct` + `MarketplaceAffiliateLink` (EF + migração PostgreSQL).
  - [x] `IAdvertisingProductService` — cadastro global, geração de links parametrizados e `GenerateOptimizedPayloadForPostAsync`.
  - [x] API `api/propaganda/produtos` e página `ProdutosPropaganda.razor` (mobile-first, copiar link Shopee/TikTok).

- [x] 6.4. Observabilidade e Telemetria (Monitoramento de Produção)
  - [x] Projeto `Ofertio.Observability` com `AddOfertioTelemetry` (traces, métricas OTLP/Console/Prometheus, logs OpenTelemetry).
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


### Fase 9: Reformulação Visual e Componentes Multi-Contas no Frontend (Ofertio.WebUi) 🎨
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
  - Criar o mecanismo de redirecionamento dinâmico do Ofertio (ex: `tflow.link/xyz`).
  - Modelar a tabela `LinkClickLog` para registrar a telemetria de acessos (data, hora, IP, localização simulada, dispositivo e plataforma de origem do produto).

### Fase 11: Módulo Gerador de Links Omnichannel no Frontend (Ofertio.WebUi) 📱
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
- [ ] **12.1.2. Implementação no Backend (Ofertio.API):** Instalar e configurar o pacote `Microsoft.AspNetCore.Authentication.Google`, mapear as chaves no `appsettings.json` e criar o endpoint de callback para receber o token do Google, validar/criar o usuário no PostgreSQL e emitir o JWT do sistema.
- [ ] **12.1.3. Interface e Fluxo no Frontend (Ofertio.WebUi):** Desenvolver o botão "Entrar com Google" com design oficial, disparar o redirecionamento de segurança para o Google e tratar o retorno da sessão no Blazor Server.

#### 12.2. Integração com Apple Identity (Login pelo iCloud)
- [ ] **12.2.1. Configuração no Apple Developer Program:** Criar o *App ID* com a funcionalidade *Sign In with Apple* ativa, configurar o *Services ID* com a URL de redirecionamento correspondente e gerar a chave privada de assinatura (`.p8`).
- [ ] **12.2.2. Implementação no Backend (Ofertio.API):** Configurar a autenticação da Apple utilizando criptografia de chaves (`ClientSecret` gerado dinamicamente via JWT assinado com a `.p8`) e validar o ID Token enviado pela Apple.
- [ ] **12.2.3. Interface e Fluxo no Frontend (Ofertio.WebUi):** Acoplar o botão nativo "Sign in with Apple" respeitando as diretrizes estritas de UX da Apple e mapear o envio dos dados do usuário (nome/e-mail obtidos apenas no primeiro login).

#### 12.3. Integração com Meta for Developers (Login pelo Facebook)
- [ ] **12.3.1. Configuração no Meta Developers:** Criar um aplicativo do tipo "Consumidor", configurar o produto "Login do Facebook", adicionar as URIs de redirecionamento válidas e obter o *App ID* e *App Secret*.
- [ ] **12.3.2. Implementação no Backend (Ofertio.API):** Instalar o pacote `Microsoft.AspNetCore.Authentication.Facebook`, configurar o middleware no pipeline e mapear o mapeamento de claims (id, email, name).
- [ ] **12.3.3. Interface e Fluxo no Frontend (Ofertio.WebUi):** Inserir o botão "Entrar com Facebook" na página de login e ligar o fluxo de autenticação ao circuito Blazor.

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

### Fase 14: Painel de Otimização e Sugestões de Ofertas no Frontend (Ofertio.WebUi) 💸
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

- [x] Configurar o arquivo `web.config` do Ofertio.WebUi com a flag de controle dinâmico: `stdoutLogEnabled="true"` e diretório `.\logs\stdout`.

- [x] Configurar o arquivo `web.config` da Ofertio.API seguindo o mesmo padrão, mapeando os logs para `.\logs\stdout` orientado por flag de ativação.

#### 15.2. Padronização do Log de Aplicação (.NET Serilog/ILogger)
- [x] Inspecionar o arquivo `Program.cs` da Ofertio.API e garantir a injeção do provedor de Log (Console + Arquivo de Rolagem Diária `.txt`).

- [x] Inspecionar o arquivo `Program.cs` do Ofertio.WebUi (Blazor Server) para capturar o ciclo de vida das conexões de circuitos SignalR e requisições HTTP internas (`BlazorCircuitLoggingHandler`, `UseSerilogRequestLogging`).

- [x] Configurar os arquivos `appsettings.Homologacao.json` de ambos os projetos para definir o nível mínimo de log como `Information` (traces internos do framework em homolog).

#### 15.3. Blindagem e Captura de Exceções Ocultas
- [x] Implementar ou revisar um Middleware Global de Exceções (`ExceptionHandlingMiddleware`) na API para capturar erros 500, estendendo o log com detalhes contextuais seguros (LGPD) e retornando `ProblemDetails` JSON.

- [x] Validar que todos os blocos críticos de `try/catch` no fluxo de autenticação (OAuth, login tradicional, pontes de comunicação) invoquem explicitamente `_logger.LogError(ex, ...)` em vez de engolirem a exceção em silêncio.

### Fase 16: Identidade Digital e Infraestrutura de Produção (O Alicerce) 🗺️
- [ ] **16.1.** Registro e Apontamento do Domínio: Registrar ofertio.com.br no Registro.br e configurar os servidores de DNS na Cloudflare.

- [ ] **16.2.** Configuração do Servidor e SSL: Apontar o subdomínio homolog.ofertio.com.br para o servidor de homologação e instalar certificado SSL (Let's Encrypt).

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
- [X] **19.1.1. Inspecionar e Ajustar `appsettings.json`:** Mapear as chaves de Afiliado da Shopee (`PartnerId`, `PartnerKey` / `AppSecret` / `AppKey` / `AppSignature`) na `Ofertio.API` e no `Ofertio.WebUi`.
- [x] **19.1.2. Mapeamento de Fallback/Sandbox:** Implementar/validar o modo de simulação no `ShopeeIntegrationClient` para garantir que, caso as chaves reais de produção não estejam preenchidas, o sistema injete uma tag/sub_id de homologação sem estourar exceção.

#### 19.2. Teste do Motor Backend de Unshorten e Re-parametrizador (Strategy)
- [x] **19.2.1. Validação do `PlatformLinkResolver`:** Testar a resolução de domínios nativos da Shopee (`shopee.com.br`) e encurtados (`s.shopee.com.br`, `br.shp.ee`, `shp.ee`, `shope.ee`), com regex desktop `i.{shopId}.{itemId}` e geração de Universal Link (`/universal-link/product/{shopId}/{itemId}?sub_id=`) sem App Key / App Secret.
- [x] **19.2.2. Geração da URL Rastreada de Comissão:** Validar o Universal Link com `sub_id` (Tracking ID, apelido ou UserId), sem dependência impeditiva da Open API.
- [x] **19.2.3. Persistência de Telemetria:** Confirmar que a chamada grava corretamente um novo registro na tabela `LinkClickLog` com o `TenantId`/`ShopId` e metadados de acesso.

#### 19.3. Conexão End-to-End no Frontend (Ofertio.WebUi)
- [x] **19.3.1. Integração da Tela `GeradorLinks.razor`:** Ligar o evento do botão "Gerar Link" da interface Blazor ao endpoint `POST /api/afiliados/links/gerar` do backend, com `_isLoading`, alerta vermelho se a loja não estiver selecionada e `StateHasChanged()` após sucesso.
- [x] **19.3.2. Ações de Interface e Feedback Visual:** Renderizar `AffiliateUrl` (longa), `ShortenedShopeeUrl` (`br.shp.ee`) e `ShortenedUrl` (`http://localhost:5001/{storeSlug}/{code}`) em cards com cópia independente via `ofertio-clipboard.js`. Combo **Conta selecionada** quando o produto tem mais de uma conta ativa.
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
- [ ] **19.3.3. Teste do Circuito Fechado (Ponta a Ponta):** Efetuar login por e-mail no sistema, colar a URL real de uma cadeira/produto da Shopee, converter, copiar o link de comissão e validar o registro no SQL Server (`Ofertio` / `ShortAffiliateLinks`).

#### 19.4. Link Encurtado
- [ ] **19.4.1. Encurtar link Shopee

#### 19.5. Extração de Metadados de Produto (Nome e Preço) e Reformulação do Histórico de Links
- [x] 19.5.1. Migração do Modelo de Dados (EF Core & SQL Server): Atualizar a entidade ShortAffiliateLink no Entity Framework Core para incluir as propriedades ProductName (nvarchar(255)), ProductPrice (decimal(18,2)) e ProductImageUrl (nvarchar(500)). Executar dotnet ef migrations add AddProductMetadataToLinks e dotnet ef database update para sincronizar o banco de dados.
- [x] 19.5.2. Resolução e Expansão de Links Encurtados: a URL colada (inclui `promoby.me`, `ofertou.ai`, `bit.ly`, `t.me`) é expandida primeiro; o domínio só é validado na URL final. "Não reconhecemos este domínio" aparece apenas se o destino não for Amazon, Shopee, Magalu, ML, Kabum, TikTok Shop ou Casas Bahia.
- [x] 19.5.3. Desenvolvimento do Serviço Extrator (ProductMetadataService): Criar a rotina de busca de metadados utilizando HttpClient e parse de tags HTML/OpenGraph (og:title, og:price:amount, itemprop="price", JSON-LD) para extrair o Nome e o Preço a partir da URL completa dos marketplaces suportados (Shopee, TikTok Shop, Mercado Livre, Amazon, Magalu, Kabum! e Casas Bahia).
- [x] 19.5.4. Exibição de Card/Preview no Gerador de Links: Atualizar o componente Blazor (GeradorLinks.razor) para exibir o Card do Produto (contendo o Nome do Item, Preço em R$ e badge da plataforma) no painel "Seus links de comissão" imediatamente após o usuário clicar em "Gerar Link de Comissão".
- [x] 19.5.5. Reformulação Visual da Tabela "Histórico de Links": Modificar a estrutura da tabela do histórico de links no Blazor removendo as colunas brutas LINK ORIGINAL e LINK ENCURTADO, substituindo-as por PRODUTO (Nome do Produto) e PREÇO (Valor formatado em R$). Preservar as colunas PLATAFORMA, DATA, CLIQUES e as ações Visualizar e Editar.
- [x] 19.5.6. Tratamento de Exceções e Resiliência (Fallback): scraping bloqueado não interrompe a conversão; títulos genéricos (`Produto`/`Shopee`) gravam `NULL`; o afiliado informa nome/preço no preview e no histórico; fallback Shopee Open API (`get_item_base_info`) usa AppKey/AppSecret de `MarketplaceAccounts`.

### 🛡️ 20. Estratégia de Resiliência, Defesa Anti-Bot e Formatação na Interface (Ofertio)

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

O Ofertio.API (SQL Server `localhost\SQLEXPRESS` / `Ofertio`) é a fonte de verdade. O futuro app (`Ofertio.Mobile`) sincroniza via REST com os mesmos DTOs do Blazor; o SQLite local é cache offline, nunca substitui o servidor.

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
- [x] 21.4. Layout público compacto: badges ~35% menores, Converter/Limpar 50/50, divisor e duas linhas (comissao direta + rastreio Ofertio) com Copiar/Abrir.

---

## 🚀 Roadmap de Expansão: Integrações e Bots de Distribuição

### 🤖 22. Módulo Telegram (Bot & Agendador de Grupos)
Permite que o usuário do Ofertio conecte seu próprio Bot do Telegram para escutar, converter e agendar ofertas em canais ou grupos.

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
  - O Ofertio orquestra instâncias da **Evolution API** isoladas por `UserId`.
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
  - **Vitrine Global ("Pool de Achadinhos"):** Ingestão na tabela `GlobalTrendingDeals` para disponibilização e consumo via créditos por todos os assinantes do Ofertio.


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
   - [x] Ao clicar, exibe um modal estilizado do Ofertio com o passo a passo ilustrado de como criar o robô no `@BotFather` e copiar o token gerado.

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
1. [x] **Credenciais MTProto:** Configuração de `ApiId` e `ApiHash` no Ofertio para conectar uma conta de usuário do Telegram.
2. [x] **Escuta Passiva em Background:** O Worker (`TelegramUserMonitorWorker`) intercepta novas mensagens em todos os canais inscritos da conta.
3. [x] **Catch-up histórico paginado:** o botão *Sincronizar* **aguarda** `Messages_GetHistory` nos chats (`Dialogs` ou `DialogsSlice`, até **1500** msgs/canal ou **48h**) via `ToInputPeer`, persiste links (texto, entidade e botão) e só então recarrega a lista.
4. [x] **Fila desacoplada:** `Channel<UserBotCapturedPayload>` recebe o update MTProto sem bloquear o `WTelegramClient`; um leitor persiste no SQL. `HostOptions` + `requestTimeout="20:00:00"` no IIS evitam derrubar o `IHostedService`.
5. [x] **Extração de Ofertas:** regex HTTP ampla, Kabum/`shp.ee` e URLs de botão inline, salvando em `GroupCapturedMessages` com o canal de origem.
6. [x] **Grupos monitorados sem teto fixo:** a API pagina ofertas (`skip`/`take` 50) e a tela carrega mais 50 até exibir todas do período.

## 🛠️ Permissões de Sistema de Arquivos (Telegram Sessions)

O serviço de escuta do UserBot (`WTelegramClient`) requer acesso de leitura/escrita na pasta local de sessões:
- **Diretório:** `C:\ProgramData\Ofertio\telegram-sessions` (fora do publish IIS; o wipe de `inetpub` não apaga o `.session`).
- **Permissão do IIS:** `IIS_IUSRS` e o `OfertioApiPool` com **FullControl** nessa pasta (`Configurar-Logs-IIS.ps1`).
- [x] **Fallback TEMP:** se a criação da pasta falhar, a sessão vai para `%TEMP%\Ofertio\telegram-sessions` e a sincronização de canais via Bot Token segue independente.

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
  - Seed de homologação não derruba mais a API; o pool `OfertioApiPool` precisa de login Windows no SQL Server `Ofertio`.



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
    - Busca `sites/MLB/search` com token OAuth obtido em **Minhas Lojas** (`authorization_code` ➔ `APP_USR-…` + refresh persistido na loja ML; placeholder do Universal Link ignorado).
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

## 🛠️ Fallback HTML da busca MLB (`MLBScraperSearchFallback`)

- [x] **HTTP 403:** se `sites/MLB/search` falhar, consulta `https://lista.mercadolivre.com.br/{query}` com User-Agent Chrome.
- [x] **TrackingId:** `matt_tool` da loja ativa injetado nos links da API e do scraper; badge 🟢 com a contagem de produtos.
- [x] **Cards completos:** parser percorre todos os nós da listagem; a `/busca` renderiza a `DataList` unificada com `StateHasChanged`.
- [x] **Erro rastreável + HAP:** HTTP ≠ 200 não devolve lista vazia; `LogError`/`Failed("ML HTTP …")` no badge vermelho; parser HtmlAgilityPack com Chrome/122.
- [x] **Anti-403 da vitrine:** `HttpClientHandler` nativo (cookies + Brotli) e headers `sec-ch-ua`/`Sec-Fetch-*`; badge `{n} produtos` quando o HTML mapeia ofertas.
- [x] **Busca só via OAuth:** a vitrine HTML é classificada como bot (`account-verification`); `/busca` exige token OAuth e trata 403 CloudFront sem scrap.

  ## 🔍 Transparência de Status e Logs no Live Search (`SearchTelemetryUI`)

- [x] **Indicador de Status por Plataforma na Tela de Busca:**
  - Exibição de badges de progresso/sucesso (ex: Mercado Livre: 12 itens | Shopee: Sem credencial | Amazon: 0 itens) em vez de apenas uma mensagem genérica de lista vazia.
- [x] **Logs de Auditoria de Busca:**
  - Registro no `ILogger` dos parâmetros de busca e contagem de itens retornados por cada canal de integração.

  ## 🛠️ Ajuste no Consumo da API Pública do Mercado Livre (`MercadoLivreSearchFix`)

- [x] **Configuração do Header `User-Agent`:**
  - Adição do header `User-Agent` obrigatório no `HttpClient` de integração com a API pública do Mercado Livre.
- [x] **Desserialização Correta do JSON (`MLB Search DTO`):**
  - Mapeamento explícito da propriedade `results` e tratamento de exceção HTTP para log detalhado no C#.
- [x] **Headers Chrome + diagnóstico no badge:**
  - `User-Agent` Chrome/122, `Accept: application/json`, log `Erro API ML [{StatusCode}]: {Body}` e badge `❌` com `ML HTTP {status}`.
- [x] **Bearer OAuth na busca MLB:**
  - Token da conta conectada, `Integrations:MercadoLivre` (AppId/SecretKey) e badge `⚠️ Requer conta conectada no painel`.
- [x] **Botão Conectar no badge do ML:**
  - Redireciona para `/minhas-lojas?conectar=mercadolivre` e abre o modal com Mercado Livre pré-selecionado.

  ## 💡 Descomplicação da Conexão de Lojas (UX - Mercado Livre Onboarding)

- [x] **Simplificação de Onboarding do Mercado Livre (`MLAffiliateUX`):**
  - Permissão para colar diretamente qualquer link de afiliado do Mercado Livre no formulário de conexão.
  - Extração automática do `matt_tool` no backend via Regex/URL Parser sem exigir digitação de IDs técnicos pelo utilizador.
- [x] **Modal Didático e Passo a Passo Responsivo:**
  - Instruções ilustradas separadas para computador e aplicação móvel.

## 🐞 Correção de Leitura de Loja Ativa no Live Search (`PlatformEnumResolution`)

- [x] **Mapeamento Unificado de Plataformas:**
  - Padronização da consulta de contas ativas (`MarketplaceAccounts`) utilizando estritamente o `PlatformType` enum para evitar falha por divergência de espaço ou string ("Mercado Livre" vs "MercadoLivre").

  ## 🛠️ Resiliência no Live Search do Mercado Livre (`MLBScraperSearchFallback`)

- [x] **Bypass do Bloqueio HTTP 403 da API de Busca:**
  - Implementação de fallback resiliênte para busca HTML/OpenGraph na vitrine do Mercado Livre quando a chamada da API sem Bearer Token retornar HTTP 403.
- [x] **Preservação do TrackingId:**
  - Garantia de que todos os produtos encontrados na busca (seja via API ou Fallback Scraper) tenham o `matt_tool` do tenant (`14343296`) injetado no link final de comissão.
- [x] **Parser de todos os cards da vitrine:**
  - Percorre cada `ui-search-layout__item` / `ui-search-result__content` (título, preço, link e imagem).
- [x] **Grid Blazor reativa:**
  - Unifica `DataList` e chama `InvokeAsync(StateHasChanged)` sem filtro de palavra-chave que ocultava os itens.
- [x] **Erro rastreável (sem lista vazia silenciosa):**
  - HTTP ≠ 200 e exceções geram `LogError` + `OfficialCatalogChannelResult.Failed`; `/busca` mostra a mensagem no badge vermelho e no alerta.
- [x] **Parser HtmlAgilityPack da vitrine:**
  - Seletores `ui-search-layout__item` / `ui-search-result`, headers de browser e `matt_tool` no permalink.

## 👑 Backoffice & Gestão Master SaaS (Fases 35 a 41)

- [x] **Fase 35 — Impersonation:** alternância de visão Master/Cliente (`/master/visao/{modo}`), emulação via JWT de 1h do dono do tenant (`ImpersonationService`, `/master/emular/{id}`) e banner fixo "Você está navegando como o tenant X" com retorno ao painel.
- [x] **Fase 36 — RBAC:** papéis SuperAdmin / SuporteTecnico / FinanceiroSaaS (`MasterPermissionMatrix` + policies `master:*`), CRUD de operadores (`/master/operadores`) e trilha de auditoria imutável (`MasterAuditLogs`, bloqueio de UPDATE/DELETE no `AppDbContext`).
- [x] **Fase 37 — Configurações globais:** chaves de marketplaces, mensageria, OpenAI, domínio `tflow.link` e ASAAS no cofre `SystemSettings` (criptografia `ENC1:`), lidas pela API via `DatabaseSettingsConfigurationProvider`; tela `/master/configuracoes` com "Aplicar e reiniciar API".
- [x] **Fase 38 — Assinaturas:** planos Starter/Pro/Agency/Enterprise (`SaasPlans`, `/master/planos`), limites aplicados em lojas, agendamentos, Radar e cota VIP; gateway ASAAS (assinatura recorrente, webhook `/api/webhooks/asaas`, bloqueio automático por inadimplência via `TenantBillingWorker`) e créditos manuais.
- [x] **Fase 39 — Inquilinos:** Tenant 360° com filtros (status, plano, renovação, disco), pausar/suspender/reativar/cancelar/estender trial com motivo obrigatório, `TenantAccessGuardMiddleware` e monitor de armazenamento com purge.
- [x] **Fase 40 — Dashboard executivo:** MRR, ARR, ARPU, receita líquida, conversão de trial, churn 30d, novos assinantes por mês e uso 24h (links, cliques, disparos WhatsApp/Telegram, requisições à API).
- [x] **Fase 41 — Comunicação:** banners globais agendados, notificações segmentadas (todos / status / plano / tenant) e changelog "O que há de novo" (`/novidades`).

## 🧭 Console Master, Contas de Desenvolvedor e Menu por Plano (Fases 42 a 44)

- [x] **Fase 42 — Rotas e layout master:** convenção `/master/*` (`MasterRoutes`; `/master/usuarios`, `/master/planos`, `/master/marketplace-accounts`), `MasterMainLayout` com identidade executiva própria, guard `UseMasterRouteGuard` que redireciona não-masters para `/` e accordion "Próximos Desenvolvimentos" oculto na visão cliente.
- [x] **Fase 43 — Contas de desenvolvedor:** `/master/marketplace-accounts` com App Credentials (ML AppId/Secret, Shopee AppKey/Secret, Amazon PA-API) e contas de teste internas em `SystemDeveloperAccounts` (segredos criptografados), teste de conectividade real por plataforma, status de validade do segredo e publicação como credencial global no cofre.
- [x] **Fase 44 — Menu por plano:** tabelas `MenuItems` / `PlanMenuPermissions`, matriz `/master/menu-permissions` (planos × itens, ativo/inativo) e `NavMenu` dinâmico via `GET api/sistema/menu` conforme o plano do tenant (Radar oculto no Starter por padrão).

## 🎨 Navegação, Usabilidade e Paletas (Fase 45)

- [x] **Fase 45 — UX Master × Cliente:** reset de scroll ao trocar de rota (`NavigationScrollReset` + `ofertioAppInterop.resetScroll`), sidebar do cliente em três níveis (Categoria → Menu pai → Submenu, com "Minhas Lojas / Integrações" fixo em Integrações), console Master em slate escuro com laranja `#FF9900`, paleta clara do cliente e segregação `/master/vault` (chaves globais + diagnóstico) × `/master/marketplace-accounts` (somente contas corporativas de teste/dev).

## 🧲 Menu Dinâmico com Drag & Drop (Fase 46)

- [x] **Fase 46 — Árvore de menus editável:** pasta "Próximos Desenvolvimentos" e seus 6 subitens semeados em `MenuItems` (ocultos por padrão em todos os planos, selo "EM BREVE" na fila de engajamento), drag & drop na matriz `/master/menu-permissions` (reordenar via `OrderIndex`, mover para pasta via `ParentId`, item sobre item cria pasta com modal) e `NavMenu` lendo apenas o banco, com pastas aninhadas e recarga a cada navegação.

## 🔐 Cofre de Chaves: Alterações em Segredos Mascarados (Fase 47)

- [x] **Fase 47 - Detecção de alterações no Vault:** inputs de `/master/vault` com `@bind-value:event="oninput"` e flag `_hasChanges` imediata (selo "Alterações não salvas", campo alterado destacado, máscara selecionada ao focar, ícone de olho para mostrar/ocultar segredos); `SystemSettingsService` ignora apenas o valor idêntico à máscara atual, rejeita valores que ainda contêm "•" e grava os demais com AES, recarregando a configuração da API.

## 🔐 Resolução OAuth e Fallback na Busca do Mercado Livre (`MLBSearchOAuthFallback`)

- [x] **App Access Token:** sem OAuth individual da loja, `MercadoLivreApiService` obtém token `client_credentials` com `AppId`/`SecretKey` do Vault Master (lidos ao vivo via `IOptionsMonitor`, token em cache) e envia `Authorization: Bearer` na busca `sites/MLB/search`.
- [x] **Fallback de vitrine:** HTTP 401/403 ou erro de escopo aciona a raspagem de `lista.mercadolivre.com.br/{query}` (reativada; a página `account-verification` é detectada e exibida como erro), com `matt_tool` (`14343296`) injetado em cada oferta.
- [x] **Badge real na `/busca`:** contagem do Mercado Livre baseada nas ofertas efetivamente exibidas (`CountVisibleOffers`); alerta de erro limpo quando há produtos.
- [x] **RedirectUri limpa no Ngrok:** `OAuthRedirectUriNormalizer` remove a porta local (`:7002` / `:5000`), força https e descarta query/fragmento para hosts Ngrok, no authorize, na troca do code e no callback pendente da WebUi (ex.: `https://camcorder-bonding-sloppily.ngrok-free.dev/integracoes/oauth/callback`).
- [x] **Callback ML sem sessão:** `state` selado com AES-256-GCM (`OAuthStateProtector`, chave HKDF de `Encryption:Key`) contendo `UserId`/`TenantId`/apelido/origem; `/integracoes/oauth/callback` anônimo repassa a `POST api/marketplace-auth/mercadolivre/callback`, que grava os tokens em `MarketplaceAccounts` (nonce de uso único, 15 min) e devolve `{origem}/marketplace-accounts?status=success`.
- [x] **Token OAuth da conta na busca (`MLBOAuthSearchTokenFix`):** `MercadoLivreApiService` usa o `AccessToken` da conta ML ativa do tenant do usuário (Bearer por requisição); em HTTP 401/403 renova via `refresh_token` com `AppId`/`SecretKey` do Vault, grava o novo token em `MarketplaceAccounts` e repete a busca antes da vitrine; log `[ML Busca]` identifica a conta usada (ex.: `@Achadinhos de Aaz`).
- [x] **Vitrine como motor primário (`MLBScraperPrimary`):** com `UseOfficialSearchApi=false` (padrão, enquanto o app aguarda homologação — a API respondia `403 PA_UNAUTHORIZED_RESULT_FROM_POLICIES` em todos os endpoints de leitura), a busca vai direto a `lista.mercadolivre.com.br` com headers de Chrome 129 e `HttpClientHandler` GZip/Deflate/Brotli (o ML segue redirecionando para `account-verification`; o fallback via `suggest`/`autosuggest` foi descartado por responder 404/403 e não trazer ofertas); o parser extrai título, preço atual/original, imagem (`data-src`) e link com `matt_tool`; o desafio anti-robô é detectado e exibido como mensagem própria, sem banner de 403.
- [x] **Busca pública leve (`MLBPublicSearch`):** antes da vitrine, `sites/MLB/search` é chamado sem token (mapeia `id`/`title`/`price`/`thumbnail`/`permalink`); 403 cai na vitrine e depois na mensagem amigável. `matt_tool=97303724` vem da conta ativa `AchadinhoCerto`; reautorização OAuth de 14/10 20:53 confirmada no log (`oauth/token` 200, tokens gravados). Em 17/10 a API anônima ainda respondia 403 e o `suggest` 404.
- [x] **Tokens de marketplace com Data Protection (`MarketplaceTokensDataProtection`):** `AccessToken`, `RefreshToken` e `AppSecret` de `MarketplaceAccounts` passam por `ValueConverter` com `ISecurityEncryptionService` (Singleton, `IDataProtector` da aplicação `TecFlow`, prefixo `DP1:`); chaves em `C:\ProgramData\TecFlow\DataProtectionKeys` (hoje `C:\inetpub\ofertio\api\App_Data\OfertioDataProtectionKeys`) com DPAPI e ACL restrita; leitura tolerante a `ENC1:` (AES legado) e texto puro, com migração preguiçosa a cada regravação.
- [x] **Fotos do Telegram gravadas com confirmação em disco (`TelegramMediaPersistenceFix`):** WebP gravado via `.tmp` + flush físico + move em `WebRootPath/uploads/products/{tenant}/{yyyy}/{MM}` (segmentos invariantes, pasta criada automaticamente); URL só vai ao banco com `File.Exists`, filtrada por `UserId` + `GroupKey` (o `UPDATE` só por `ExternalMessageId` podia atingir outro grupo); falhas de download/conversão em `LogWarning` deixam `ProductImageUrl` nulo. Causa raiz das imagens quebradas: a limpeza do deploy apagava `wwwroot\uploads` (198 de 300 mensagens recentes sem arquivo); a homologação passou a preservar a pasta.
- [x] **Uploads fora da pasta publicada (`ExternalUploadsFolder`):** mídias gravadas em `C:\inetpub\ofertio\uploads\products\{tenant}\{yyyy}\{MM}` via `UploadStorage:RootPath` / `IUploadStorage` (fallback `{WebRootPath}/uploads`), servidas em `/uploads` por `PhysicalFileProvider` na API e na WebUi; as 581 fotos existentes foram copiadas para a pasta externa e a limpeza literal da pasta da API no deploy voltou a ser segura.
- [x] **Filtros de integridade e revalidação de ofertas (`OfferIntegrityFilters`):** links de vitrine (`/social/`, `/lists/`, `/perfil/`, `/perfil-vendedor/`) rejeitados na entrada e marcados `VitrineGenerica` na verificação; preço abaixo de R$ 5,00 em produtos de alto valor vira `PrecoSuspeito` (sem clonagem); "Verificar anúncio" faz o GET da página antes das APIs e inativa na hora anúncios encerrados/pausados (`Inativo`); `OfferRevalidationWorker` revalida a cada 2h (`OfferRevalidation:IntervalHours`) as ofertas ativas dos últimos 7 dias.
- [x] **"Abrir Link Original" com afiliado (`AffiliateOpenLink`):** o card de Grupos Monitorados abre em nova aba o `AffiliateUrl` calculado na API por `AffiliateUrlBuilderService`; no Mercado Livre o `matt_tool` (ex.: `97303724`) vem da conta ML ativa do tenant ou de `Integrations:MercadoLivre:DefaultAffiliateTrackingId`, substituindo o de terceiros e preservando os demais parâmetros.
- [x] **Preço à vista, divergência e esgotado na Shopee (`ShopeePriceIntegrity`):** o preço das mensagens ignora parcelas (`12x R$ 604,92`) e o "De" de "De/Por", ficando com o valor à vista (`R$ 6.678,28`); o preço ativo da página da Shopee (meta/JSON-LD) corrige no banco divergências de 30% ou mais com log; "Produto esgotado", `is_sold_out` ou `item_status: sold_out` deixam a oferta `Esgotado` e sem clonagem.
- [x] **Aba de Cupons & Promoções (`CouponsTab`):** mensagens de cupom (`CUPOM`, `CÓDIGO`, `USAR CUPOM`, `R$ OFF`) sem preço à vista viram `OfferType.Coupon`, saem do Feed de Ofertas e aparecem na nova aba com pills Todos / Mercado Livre / Shopee / Amazon / Outros, em `CouponCard` com copiar código, desconto, validade, regras e "Ir para a loja" com link de afiliado.
- [x] **Layout compacto de Grupos Monitorados (`CompactOffersUi`):** cabeçalho e controles `sm` sem texto explicativo, toast de status com auto-dismiss em 5s, só busca rápida + grupo na tela e o restante em "Filtros Avançados ⚙️" (sem "Só com cupom"), scroll suave até `#ofertas-container` ao paginar e `OfferCard` com ações 2x2 compactas e badges pill sutis.
- [x] **Notificações Toast globais (`GlobalToasts`):** `IToastService`/`ToastService` (Success/Error/Warning/Info, ID único, 10s padrão) e `ToastContainer` fixo no canto superior direito do `MainLayout`; Grupos Monitorados e `CouponCard` passaram a usar o serviço no lugar dos alertas fixos e do toast local.
- [x] **Cupons fora do feed de produtos (`CouponFeedCleanup`):** classificador reconhece anúncios de cupom (título "NOVO CUPOM..." ou 2+ códigos, "Limite de R$" não é preço), `CouponReclassificationWorker` migra o legado no startup (~607 itens) e os motores de radar filtram `Type == Product`.
- [x] **Cabeçalho com menu de perfil (`UserProfileMenu`):** removidos "Todas as lojas", botão "Painel" e "Sessão ativa"; pílula com o nome do usuário abre dropdown nativo (Minha Conta, Super Admin, Modo de Visão, Sair) no `MainLayout` e no `MasterMainLayout`.
- [x] **Cabeçalho restaurado (`HeaderRestore`):** grupo [Painel Master] [Visão Cliente] e badge de papel de volta ao topo, menu de perfil só com Minha Conta / Super Admin / Sair e cabeçalho com `z-index: 1050` para o dropdown não ficar sob a página.
- [x] **Vitrines de terceiros e afiliado forçado (`ShowcaseBypass`):** links do Mercado Livre com `/social/` / `affiliate-profile` viram a página direta do produto focado (`item_id` ou `/p/MLB...`), os parâmetros `matt_tool_id` / `matt_tool` / `matt_d2id` / `matt_tracing_id` de terceiros são removidos e o `matt_tool=97303724` é sempre aplicado em "Abrir Link Original" e "Clonar Oferta".
- [x] **100 ofertas por página (`OffersPageSize100`):** padrão de paginação do feed, do Modal de Filtros Avançados, da API e do cliente passou de 25 para 100 (pré-selecionado), e a pré-busca do serviço perdeu o teto de 80 linhas que cortava a página.
- [x] **Vitrines Meli Social, card compacto e reset (`MeliSocialCardReset`):** links `/social/` do Mercado Livre seguem os redirects até o produto e, se ficarem no perfil, viram `/p/{ITEM_ID}?matt_tool=97303724`; o `OfferCard` mostra a data/hora da captura no cabeçalho e os botões "Abrir / Verificar / Clonar / Ocultar" em uma linha; `ResetarOfertasLegadas.ps1` zera ofertas e `uploads\products` para recapturar do zero.
- [x] **Feed em ordem cronológica (`FeedChronologicalOrder`):** consultas de ofertas capturadas ordenam por `ReceivedAt` (hora da mensagem, UTC) em vez de `CreatedAt`; webhooks do WhatsApp/Telegram gravam o horário de envio e os cards exibem o horário de Brasília.
- [x] **Remoção imediata de esgotados (`SoldOutFeedRemoval`):** a verificação do Mercado Livre detecta anúncio pausado, sem estoque, reduzido a "Produtos relacionados" ou redirecionado para a busca e marca `IsAvailable = false` (`Inativo`/`Esgotado`); o botão Verificar tira o card da tela na hora com o toast "Produto esgotado. Oferta removida do feed." e o feed só lista produtos com status da lista branca.
- [x] **Filtro por características (`ProductTagsFilter`):** tabela `ProductTags` (seed Gamer, Eletrônicos, Informática, Casa & Cozinha, Smartphones, Moda/Beleza) ligada às ofertas por `OfferTags`; combo "Todas as Características" ao lado de "Ocultos / Descartados" filtra com `Tags.Any(t => t.Id == selectedTagId)`, e as ofertas novas e legadas são classificadas por palavras-chave.
- [x] **Preço total da Amazon (`AmazonUnitPriceFix`):** o parser ignora valores por unidade ("R$ 16,60 / unidade", "por frasco") e, nas páginas da Amazon, lê o total da buy box (`apexPriceToPay` / `priceToPay` / `corePrice_feature_div`, fallback `og:price:amount`); o Verificar atualiza o card na hora com o toast "Preço atualizado para R$ X".
- [x] **Migração de marca (`RebrandOfertio`):** solução, projetos, namespaces, interface e documentação passam de TecFlow para Ofertio; banco SQL Server renomeado para `Ofertio`, IIS em `C:\inetpub\ofertio` com pools/sites `Ofertio*`, uploads de produção e dev separados e chaves do Data Protection em `App_Data` preservado no deploy; identificadores gravados (Data Protection, migração inicial, instâncias do WhatsApp, IDs de afiliado) mantidos para não perder dados nem tokens.
- [x] **Identidade visual Ofertio (`OfertioVisualIdentity`):** logotipo em `wwwroot/images/logo-ofertio.png` no cabeçalho do portal, no Master Console e na tela de login; favicon das setas laranja (`favicon.png`/`favicon.ico`) no WebUi e no Mobile; título padrão "Ofertio - Inteligência em Ofertas".
- [x] **Botões de Grupos Monitorados (`GruposMonitoradosButtonsFix`):** o "Limpar" volta a funcionar (o `ConfirmDialogHost` estático nunca abria o modal), apaga ofertas/imagens e redefine todos os filtros; Atualizar, Limpar e Sincronizar ganham spinner e bloqueio individuais; toasts "Filtros redefinidos com sucesso." e "Sincronização concluída! X novos grupos e Y novas mensagens atualizadas."; mini-spinner só no card clicado.
- [x] **Filtro por plataforma (`MonitoredPlatformFilter`):** select "Todas as Plataformas" ao lado das características em Grupos Monitorados (Mercado Livre, Shopee, Amazon, Magazine Luiza, AliExpress, Casas Bahia / Ponto, Outros), filtrado no SQL por `PlatformType`, volta à página 1 ao trocar e é redefinido pelo "Limpar".
- [x] **Plataformas dinâmicas no filtro (`MonitoredPlatformFilterDynamic`):** o select lista só as plataformas com conta de marketplace ativa do usuário; sem conta, as detectadas nas ofertas capturadas; sem nenhuma, Mercado Livre e Shopee.
- [x] **Filtro de plataforma único (`UnifiedPlatformFilter`):** as pílulas de marketplace da aba de cupons saíram; "Todas as Plataformas" filtra Feed de Ofertas, Cupons & Promoções e Ocultos / Descartados e é mantido ao trocar de aba.
- [x] **Certificado no Kestrel (`KestrelDevCertificate`):** o WebUi em Development sobe `https://localhost:7002` com o `.pfx` do win-acme (`dev.ofertio.com.br`); senha em User Secrets e `*.pfx` fora do Git; endpoint com `Protocols: Http1AndHttp2`.
- [x] **Rotas do app Mercado Livre (`MercadoLivreEndpoints`):** `MercadoLivreController` em `api/mercadolivre` com `GET callback` (OAuth 2.0 com state selado, grava tokens e volta para `/marketplace-accounts`) e `POST notifications` (responde 200 assim que lê o payload); fluxo do state selado compartilhado com `api/marketplace-auth/mercadolivre/callback`.
- [x] **Rotas do ML pela WebUi (`MercadoLivreWebUiProxy`):** a aplicação `/api` criada no site `Ofertio.WebUi` dividia o `OfertioApiPool` e derrubava a API com HTTP 500.35; foi removida e a WebUi passou a atender `/api/mercadolivre/callback` e `/api/mercadolivre/notifications` no host público, repassando à API em `localhost:5001`.
- [x] **Reverse proxy YARP (`MercadoLivreWebUiProxy`):** o repasse manual virou `MapForwarder` (pacote `Yarp.ReverseProxy`) em `MercadoLivreProxyExtensions`, encaminhando as duas rotas do ML para `OrquestradorApi:BaseUrl`; o deploy remove a subaplicação `/api` do site WebUi se ela reaparecer.


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
  - Tela dedicada para criação, edição e inativação de contas administradoras do Ofertio com controle de credenciais via ASP.NET Core Identity.
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
  - Módulo para publicação de atualizações do sistema (*What's New*), permitindo que os clientes leiam as melhorias recém-lançadas no Ofertio.

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
  - Desenvolvimento do painel Master para gerenciamento das **Contas de Desenvolvedor / App Credentials** (Mercado Livre AppId/SecretKey, Shopee AppKey/AppSecret, Amazon PA-API Keys) e contas de teste internas da empresa Ofertio.
- [x] **43.2. Segregação Total de Escopo (`DeveloperAccountIsolation`):**
  - Isolamento do serviço para garantir que as credenciais globais e contas de homologação cadastradas no `/master/marketplace-accounts` fiquem armazenadas em tabela/escopo de sistema (`SystemDeveloperAccounts`), sem interferir nas contas de afiliados do cliente (`/marketplace-accounts`).
- [x] **43.3. Teste de Conectividade e Saúde das APIs Globais:**
  - Painel com botões de diagnóstico direto na tela master para validar status de tokens de app, expiração de secrets e status das conexões com os servidores do Mercado Livre, Shopee e Amazon.

---

### 📜 Fase 44: Matriz Dinâmica de Menus por Plano e Banco de Dados (`DynamicPlanMenuPermissions`)

- [x] **44.1. Persistência de Menus no Banco de Dados (`MenuItems` & `PlanMenuPermissions`):**
  - Criação das tabelas `MenuItems` (cadastrando toda a árvore de menus com `Id`, `Title`, `Route`, `Icon`, `ParentId`, `Order`) e `PlanMenuPermissions` (mapeando a relação entre `PlanId` e `MenuItemId` com `IsVisible`).
- [x] **44.2. Interface Master de Controle de Árvore `/master/menu-permissions`:**
  - Desenvolvimento da tela Master exibindo a árvore hierárquica completa do menu do Ofertio em formato de matriz/tabela com colunas para cada plano (*Starter, Pro, Agency, Enterprise*).
  - Toggles/Checkboxes dinâmicos para você marcar ou desmarcar visualmente quais itens do menu cada plano enxergará na plataforma.
- [x] **44.3. Renderização Dinâmica da Sidebar do Cliente (`DynamicNavMenu`):**
  - Refatoração do `NavMenu.razor` para carregar a estrutura de navegação diretamente da tabela de permissões com base no plano ativo do tenant logado, omitindo automaticamente rotas não contratadas ou indisponíveis.

---

### 🎨 Fase 45: Navegação, Usabilidade e Paleta de Cores (Master × Cliente)

- [x] **45.1. Reset automático de scroll na navegação Blazor:**
  - Componente `NavigationScrollReset` (ouvinte de `NavigationManager.LocationChanged` + JSInterop `ofertioAppInterop.resetScroll`) nos layouts `MainLayout` e `MasterMainLayout`, retornando ao topo da janela e do container `.main-content` a cada troca de rota (âncoras `#` preservadas).
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

### 🔐 Fase 47: Cofre de Chaves — Alterações em Segredos Mascarados (`VaultDirtyTracking`)

- [x] **47.1. Binding instantâneo em `/master/vault`:**
  - Inputs de AppId/SecretKey com `@bind-value:event="oninput"`; flag `_hasChanges` ligada na digitação (selo "Alterações não salvas", botão Salvar habilitado e campo alterado destacado); a máscara é selecionada ao focar para ser substituída.
- [x] **47.2. Tratamento da máscara no salvamento:**
  - Valor idêntico à máscara (`••••1234`) é ignorado e o segredo do banco é mantido; valor diferente é gravado com AES e a configuração da API é recarregada; máscara editada (contém "•") é rejeitada sem gravar nada.
- [x] **47.3. Ícone de olho nos segredos do Vault:**
  - Botão `bi-eye` / `bi-eye-slash` por campo secreto (`_showSecretMap`, `ToggleVisibility`, `IsSecretVisible`) alterna `password` ↔ `text`; o servidor continua enviando apenas a máscara, então só a chave recém-digitada fica legível; tudo volta a ficar oculto após salvar/recarregar.
