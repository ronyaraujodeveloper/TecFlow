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
- [x] **Anúncios ML pela API oficial (`MercadoLivreOfficialItems`):** `GET api.mercadolibre.com/items/{id}` com `Authorization: Bearer` do OAuth da conta conectada (renovado pelo `refresh_token` quando expira ou é recusado); links `MLB` são validados pela API antes da página pública; desafio anti-robô descarta o HTML e suspende os GETs nas páginas do ML por 15 min; logs claros quando a conta não está conectada.
- [x] **Resolvedor ML sem scraping (`MercadoLivreNoScraping`):** validação de ofertas, metadados do Gerador de Links e expansão de encurtadores deixaram de baixar a página pública do ML; o MLB vem da URL (redirects só por cabeçalho) e status/título/preço/imagem vêm de `/items/{id}` com Bearer e refresh automático; links de catálogo seguem `/products/{id}` até o anúncio ganhador.
- [x] **Buscar Ofertas ML só pela API oficial (`MercadoLivreSearchOAuthOnly`):** a opção `UseOfficialSearchApi` (padrão `false`) mantinha a busca anônima seguida da raspagem de `lista.mercadolivre.com.br`, que caía no `account-verification`; agora a busca sempre usa `Authorization: Bearer` (conta OAuth com refresh ➔ token fixo ➔ token do app), o token saiu da URL e a vitrine/`MercadoLivreHtmlParser` foram removidos.
- [x] **Callback público do OAuth ML (`MercadoLivreOAuthPublicCallback`):** `Integrations:MercadoLivre:RedirectUri` passou do túnel ngrok para `https://dev.ofertio.com.br/api/mercadolivre/callback` (servido pela WebUi via YARP até `MercadoLivreController`), usado na URL de autorização e na troca do `code`.
- [x] **Diagnóstico da troca do code ML (`MercadoLivreOAuthExchangeDiagnostics`):** o POST `oauth/token` ganhou envio próprio; HTTP 400 registra o corpo do ML (`error`/`message`) com `redirect_uri` e `client_id`, sem segredos, e a mensagem da tela traz o código do erro em vez de "Falha na API OAuth (400)".
- [x] **PKCE no OAuth ML (`MercadoLivreOAuthPkce`):** o diagnóstico revelou `code_verifier is a required parameter`; o `code_verifier` passou a ser gerado por autorização e guardado no state selado, a URL leva `code_challenge` S256 e a troca do `code` envia o verifier.
- [x] **Busca ML pelo catálogo (`MercadoLivreCatalogSearch`):** com a conta autorizada, `sites/MLB/search` seguia em 403 (endpoint descontinuado pelo ML para terceiros); a Buscar Ofertas passou a usar `products/search` + `products/{id}` (preço do `buy_box_winner`).
- [x] **Ofertas sem buy box + modal (`MercadoLivreCatalogOfferFallback`):** "dell i7" voltava 20 produtos de catálogo e 0 ofertas; o preço passou a cair para a faixa da buy box, `/products/{id}/items` e o primeiro produto filho, e a tela ganhou modal de carregamento durante a busca.
- [x] **Varredura e filtros na Buscar Ofertas (`MercadoLivreCatalogSearchScan`):** "Dell Inspiron" trazia só 3 ofertas de 20 produtos de catálogo; a busca passou a paginar até ~20 ofertas ou 100 produtos com "Carregar mais", aplicar preço e frete grátis durante a varredura, tentar termos mais curtos, ordenar, filtrar por categoria (`domain_id`) e mostrar quantos produtos foram analisados.
- [x] **Teto da varredura e filtros na tela (`MercadoLivreScanBudgetAndScreenFilters`):** "dell inspiron" + Notebooks parava em 40 produtos com 1 oferta; a varredura passou a ir até 100 quando já há oferta, produtos "pai" trazem até 4 variações, filtros alterados após a busca filtram só a tela e a página ganhou modal com as regras e as lojas do usuário.
- [x] **Métricas de afiliado (`AffiliateOfferMetrics`):** a Buscar Ofertas passou a servir para análise: comissão estimada (Shopee pela API; ML/Amazon pela taxa do perfil), vendidos, avaliações, ranking de mais vendidos, reputação do vendedor, notas de vantagem/segurança/oportunidade, painel de métricas e filtros de loja oficial, vendedor confiável e mais vendidos.
- [x] **Comissão do ML por categoria (`MercadoLivreCategoryCommission`):** o card dizia "informe sua taxa nas regras"; a comissão do ML passou a ser estimada pela categoria raiz do anúncio (`/categories/{id}`) com a tabela do afiliado generalista (`AffiliateCommissionRules`), a foto abre um modal com todas as imagens do produto e o card ganhou o botão "Abrir" ao lado de "Converter & Gerar Link".
- [x] **Tooltips nos selos (`OfferScoreTooltips`):** Oportunidade, Vantagem e Segurança ganharam o ícone (?) com tooltip do Bootstrap (hover e toque), inicializado no `OnAfterRenderAsync` via `ofertioTooltips.init`; o Bootstrap JS passou a ser carregado no WebUi e no Mobile.
- [x] **Relevância e anti-acessórios (`MercadoLivreRelevanceAndMainProducts`):** a ordenação padrão virou "Relevância (Padrão Mercado Livre)" (ordem exata da API; o score só reordena em "Melhor vantagem + segurança"), as categorias sugeridas viraram atalhos de 1 clique, entrou o filtro "Apenas produtos principais (ocultar acessórios)" e os tooltips passaram para os ícones (?) com `window.initTooltips` (`hover focus click`).
- [x] **Pílulas de categoria e selos clicáveis (`CategoryChipsAndScoreBadgeModal`):** buscar numa categoria não remove mais as outras pílulas (`MergeCategoryChips` preserva todas as categorias do termo); a selecionada fica azul, a troca é direta com 1 clique e "Outras categorias" (`bi-funnel`) alterna as excedentes sem nova consulta. Os selos de Oportunidade, Vantagem e Segurança viraram botões inteiros (tooltip no hover e modal de explicação no clique, com os números da oferta).
- [x] **Link de rastreio com a URL do ambiente (`DynamicTrackingLinkBaseUrl`):** o "Link de rastreio Ofertio" deixou de mostrar o `http://localhost:5001` interno e usa a URL base da tela (`NavigationManager.BaseUri`) no Gerador de Links, na página pública, na Buscar Ofertas e no seletor dos agendadores; a WebUi encaminha `/{storeSlug}/{code}` e `/r/{code}` à API para registrar o clique e redirecionar.
- [x] **Código MLB preservado (`MercadoLivreProductIdPreservation`):** o fallback de homologação que trocava links sem código MLB (ex.: `meli.la` para vitrine `/social/...`) pelo fictício `MLB1234567890` foi removido; vitrine sem produto agora é recusada com mensagem clara. A URL do produto (catálogo `/p/MLB...` ou anúncio `/MLB-...`) é mantida e só os `matt_*` são trocados, inclusive na reafiliação.
- [x] **Edição de lojas (`StoreEditModal`):** cada card de Minhas Lojas ganhou "Editar loja"; o modal de conexão abre como "Editar loja" com plataforma, apelido e ID de afiliado preenchidos e salva por UPDATE (`lojas/{id}/editar`) na mesma `ShopId`, sem duplicar a loja. "Conectar nova loja" volta sempre ao modo criação com o formulário limpo.
- [x] **Link de rastreio com slug numérico (`ShortLinkNumericSlug`):** `/97303724/xh8pmzh` dava 404 porque a rota do encurtador (API e proxy da WebUi) exigia slug começando com letra. A rota agora aceita qualquer slug alfanumérico ASCII de até 80 caracteres, igual ao que o `GenerateSlug` produz, e o redirect registra slug, código e host de destino no log.
- [x] **Integrações Globais no Painel Master (`GlobalIntegrationsMaster`):** a conexão com a API oficial do Mercado Livre saiu de Minhas Lojas e passou para Master ➔ Integrações Globais ➔ API Mercado Livre. O token do administrador fica em `GlobalIntegrationTokens` (criptografado), é renovado automaticamente via `refresh_token` quando expira ou é recusado, e as buscas idênticas são servidas por um cache global com tempo configurável (padrão 15 minutos). A matriz de menus ganhou a permissão "Integrações Globais", que oculta Buscar Ofertas, Radar de Ofertas e Perfil de mineração nos planos em que estiver desligada.
- [x] **Filtros compactos da Buscar Ofertas (`CompactSearchFilters`):** os checkboxes, os campos de preço/ordenação/categoria e as pílulas "Buscar só em" deram lugar a uma linha com três combos multisseleção (Plataformas, Filtros e Qualificadores, Categorias), que têm busca a partir de 3 letras e contador no botão, e ao botão "Filtros", que abre um modal com preço mínimo, preço máximo e ordenação. As categorias filtram só em memória, e as métricas ficam recolhidas para a vitrine aparecer sem rolagem.
- [x] **Padrão global de combos e dropdowns (`ComboDropdownStandard`):** o README ganhou a seção 2 das diretrizes de UI, com busca interna só acima de 6 itens, confirmação entre seleção múltipla e única, lista vertical, fechamento ao clicar fora, foco automático e teclado. O `MultiSelectDropdown` da Buscar Ofertas já segue o padrão.
- [x] **Combos centralizados e timeout do Mercado Livre (`CompactCombosAndMlTimeout`):** a barra de filtros da Buscar Ofertas ficou centralizada e responsiva; os combos fecham ao clicar ou focar fora, destacam o item por setas ou hover e marcam com Espaço ou Enter; os assets ganharam versão no link contra cache antigo. O `HttpClient` do Mercado Livre passou de 12 s fixos para 30 s configuráveis, e timeouts viram degradação amigável (produto ignorado, ofertas mantidas ou mensagem clara), sem `.Result` na busca.
- [x] **Categorias reais na Buscar Ofertas (`RealCategoryFilter`):** o combo "Categorias" deixou de usar nomes genéricos em inglês e passou a nomear cada domínio pela categoria real do anúncio (`/categories/{id}`, em português); a query string enviava a lista de categorias como texto (`System.Collections.Generic.List...`) e agora repete a chave, então "Buscar" consulta o Mercado Livre com `domain_id` em cada categoria marcada (até 3). Categoria marcada com 0 ofertas ganhou aviso e o botão "Buscar nesta categoria".
- [x] **Regras modulares e README enxuto (`DocsModularRules`):** as regras do README foram divididas em quatro arquivos em `docs/rules/` (backend, UI, marketplaces/OAuth e encoding), as pendências foram para `docs/CHECKLIST_ATIVO.md` e os blocos detalhados das entregas recentes vieram para o fim deste histórico. O README ficou com objetivo, stack, índice das regras, comandos e homologação, para o Cursor carregar menos contexto por tarefa. O `.cursorignore` exclui os docs analíticos (ANALISE, INDICE, RESUMO), mas não DIAGRAMAS nem HISTORICO, que o agente precisa editar.


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

---

# 📦 Checklist detalhado migrado do README (21/10/2026)

> Blocos que estavam no `README.md` até a modularização das regras em `docs/rules/`, mantidos sem edição como registro. Pendências abertas: [`CHECKLIST_ATIVO.md`](./CHECKLIST_ATIVO.md).

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
  - Entregue em `MLBOAuthSearchTokenFix` (conta do tenant + Bearer por requisição); desde `MercadoLivreNoScraping` é o único caminho da busca.
- [x] **Auto-Refresh de Token em Chamadas de Busca:**
  - Ao receber status HTTP 401 ou HTTP 403 da API oficial, o serviço de busca deve acionar o fluxo de renovação via `refresh_token` utilizando as credenciais do Vault Master antes de registrar falha.
  - Entregue em `MLBOAuthSearchTokenFix`; em produção o ML respondeu `403 PA_UNAUTHORIZED_RESULT_FROM_POLICIES` também ao token renovado (app aguardando homologação no portal).

## 🕸️ Vitrine Pública como Motor Primário da Busca ML (`MLBScraperPrimary`)

> **Substituído por `MercadoLivreNoScraping`:** a raspagem da vitrine, a busca anônima e a opção `UseOfficialSearchApi` foram removidas; o histórico abaixo fica só como registro.

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

> **Substituído por `MercadoLivreNoScraping`:** a busca anônima foi removida; toda busca usa `Authorization: Bearer`.

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

## 🛡️ Anúncios do Mercado Livre pela API oficial (`MercadoLivreOfficialItems`)

- [x] **`MercadoLivreApiService.GetItemAsync(userId, itemId)`:** `GET https://api.mercadolibre.com/items/{ITEM_ID}` sempre com `Authorization: Bearer`.
  - Ordem do token: OAuth da conta Mercado Livre conectada no painel (tenant do usuário) ➔ `Integrations:MercadoLivre:AccessToken` ➔ token do aplicativo (`client_credentials`).
  - Token expirado é renovado pelo `refresh_token` antes da consulta (e gravado na conta); se a API responder 401/403, renova e tenta mais uma vez.
  - Logs `[ML Item]` claros quando a conta não foi conectada/autorizada (no máximo um aviso a cada 30 min por usuário) e quando o refresh falha (pede para reconectar em Minhas Lojas).
- [x] **`LiveCheckSearchService` (Verificar anúncio e revalidação):** link direto do ML (`MLB...`) consulta a API oficial antes de qualquer GET na página pública; se a API responder, a página nem é baixada.
- [x] **Trava anti-robô (`MercadoLivreAntiBotGuard`, singleton):**
  - Ao receber o desafio (`account-verification`, `bot_challenge`...) na página do anúncio ou na vitrine `lista.mercadolivre.com.br`, o HTML é descartado e os GETs nas páginas públicas do ML ficam suspensos por 15 min.
  - Nesse período a oferta fica "Verificando" com o preço capturado (`Source = AntiBot`) e a busca não raspa a vitrine.

## 🚫 Resolvedor de links do Mercado Livre sem scraping (`MercadoLivreNoScraping`)

- [x] **Página pública do ML nunca é baixada** na validação de ofertas (captura dos grupos, pré-validação, saúde das ofertas, Verificar anúncio), nos metadados do Gerador de Links nem na expansão de encurtadores.
- [x] **Código do item:** extraído da URL (`produto.mercadolivre.com.br/MLB-12345678...` ➔ `MLB12345678`); encurtadores e vitrines seguem redirects só pelos cabeçalhos até achar o MLB.
- [x] **Dados pela API oficial:** `GET https://api.mercadolibre.com/items/{ITEM_ID}` com `Authorization: Bearer` (refresh automático do token da conta); links de catálogo `/p/MLB...` usam `/products/{id}` ➔ anúncio ganhador.
  - Gerador de Links usa o token OAuth da conta do usuário logado; rotinas em segundo plano (sem usuário) usam `Integrations:MercadoLivre:AccessToken` ou o token do aplicativo, nunca a conta de outro tenant.
  - Sem MLB ou sem resposta da API: oferta "Verificando" e nome pelo slug da URL.
- [x] **Busca por palavra-chave (Buscar Ofertas):** só API oficial com `Authorization: Bearer` (desde `MercadoLivreCatalogSearch` via `/products/search`; antes `GET /sites/MLB/search`) (conta OAuth do tenant com refresh ➔ `AccessToken` fixo ➔ token do app); o token não vai mais na URL (`access_token=` aparecia no log de requisições).
  - Removidos a busca anônima, a raspagem de `lista.mercadolivre.com.br`, o `MercadoLivreHtmlParser` e a opção `UseOfficialSearchApi`.
  - Recusa da API mostra "A API oficial do Mercado Livre recusou a busca (HTTP 403)..." pedindo para reconectar a conta, no lugar do aviso de verificação anti-robô.

## 🔁 Callback público do OAuth do Mercado Livre (`MercadoLivreOAuthPublicCallback`)

- [x] **`redirect_uri` fixo no domínio público:** `Integrations:MercadoLivre:RedirectUri` = `https://dev.ofertio.com.br/api/mercadolivre/callback` em `appsettings.json` e `appsettings.Homologacao.json` da API; o botão "Autorizar Mercado Livre (OAuth)" e a troca do `code` usam exatamente esse valor (prevalece sobre o host da requisição; o cofre do Master não tem a chave gravada).
- [ ] **App do ML:** a Redirect URI cadastrada em developers.mercadolivre.com.br precisa ser idêntica (`https://dev.ofertio.com.br/api/mercadolivre/callback`).
- [x] **Troca do code com diagnóstico (`MercadoLivreOAuthExchangeDiagnostics`):** `POST https://api.mercadolibre.com/oauth/token` com `grant_type=authorization_code`, `client_id`, `client_secret`, `code` e o `redirect_uri` acima; em HTTP 400 o log `[ML OAuth]` grava `error`, `message` e o corpo da resposta do ML (`invalid_grant`, `invalid_client`, `redirect_uri_mismatch`...) com `redirect_uri` e `client_id` enviados, sem `client_secret` nem `code`, e a tela mostra o código do erro.

## 🔐 PKCE no OAuth do Mercado Livre (`MercadoLivreOAuthPkce`)

- [x] **Causa:** o app do ML exige PKCE; a troca do `code` voltava `invalid_request — code_verifier is a required parameter`.
- [x] **`code_verifier` no state selado:** gerado por autorização (64 caracteres, `MercadoLivrePkce`) e guardado criptografado no `state` (AES-GCM), sem sessão nem banco.
- [x] **URL de autorização:** `code_challenge` (SHA-256 do verifier em base64url) + `code_challenge_method=S256`.
- [x] **Troca do code:** o callback abre o state e envia `code_verifier` no `POST oauth/token`; o verifier nunca vai para log nem para a URL.
- [x] **Reautorizar:** links de autorização gerados antes do deploy não têm verifier; clicar de novo em "Autorizar Mercado Livre (OAuth)" (conta 'AchadinhoCerto' reautorizada com sucesso).

## 🔎 Busca ML pelo catálogo oficial (`MercadoLivreCatalogSearch`)

- [x] **Causa do 403 com conta autorizada:** o Mercado Livre descontinuou `GET /sites/MLB/search` para apps de terceiros (abril/2025); ele responde `403 forbidden` mesmo com token OAuth válido e renovado.
- [x] **Nova busca:** `GET /products/search?status=active&site_id=MLB&q={termo}&limit={n}` (buscador de produtos de catálogo, documentado) com `Authorization: Bearer` da conta ➔ `GET /products/{id}` para cada resultado (até 4 em paralelo) ➔ preço, preço original e frete do `buy_box_winner`, link da página do produto (`/p/MLB...`) com `matt_tool`.
- [x] **Sem oferta ativa:** produto de catálogo sem `buy_box_winner` (ou recusado) é ignorado e registrado no log `[ML Busca]`.
- [x] **Limpeza:** removidos `MercadoLivreSearchDtos.cs` e `MercadoLivreItemParser.ParseSearch` (formato da busca descontinuada).
- [ ] **Limite conhecido:** só aparecem produtos de catálogo com oferta e preço; anúncios fora do catálogo não são listados pela API oficial.

## 🧭 Ofertas sem buy box + modal de carregamento (`MercadoLivreCatalogOfferFallback`)

- [x] **Causa da busca vazia:** "dell i7" trouxe 20 produtos de catálogo, nenhum com `buy_box_winner` (produtos "pai" sem configuração definida ou sem disputa de anúncios).
- [x] **Cadeia de preço (só API oficial):** `buy_box_winner` ➔ `buy_box_winner_price_range.min` ➔ menor preço em `/products/{id}/items` ➔ primeiro produto filho (`children_ids`); o log `[ML Busca]` registra por que cada produto ficou sem oferta.
- [x] **Modal de carregamento:** a Buscar Ofertas abre "Buscando ofertas..." com spinner ao clicar em Buscar e fecha quando o retorno chega (o `LoadingOverlay` do layout é estático e não aparecia nas páginas interativas).
- [x] **Validar com a conta real:** "Dell Inspiron" passou a trazer ofertas (3 de 20 produtos de catálogo); o volume baixo motivou a varredura abaixo.

## 🔎 Varredura, filtros e ordenação na Buscar Ofertas (`MercadoLivreCatalogSearchScan`)

- [x] **Paginação:** a busca do ML varre `products/search` em páginas de 20 até juntar ~20 ofertas ou analisar 100 produtos; botão "Carregar mais (Mercado Livre)" continua do próximo offset.
- [x] **Filtros durante a varredura:** preço mín./máx. e "Somente frete grátis" são aplicados antes de contar a meta, para não descartar tudo depois.
- [x] **Transparência:** selo do Mercado Livre mostra "N ofertas · M produtos analisados" (e "sem anúncio ativo" quando nenhum tinha oferta).
- [x] **Termo mais curto:** sem nenhuma oferta, tenta automaticamente removendo as últimas palavras ("Dell Inspiron i7 16GB" ➔ "Dell Inspiron i7" ➔ "Dell Inspiron") e avisa o termo usado.
- [x] **Ordenação:** relevância, menor preço ou maior desconto (reordena na tela sem nova consulta).
- [x] **Categoria:** domínios sugeridos pelo `domain_discovery` e pelos resultados; ao escolher, a busca envia `domain_id`.
- [x] **Validar com a conta real:** o log mostrou "dell inspiron" + Notebooks parando em 40 produtos analisados com 1 oferta, corrigido abaixo.

## 🧮 Teto da varredura, filtros na tela e regras (`MercadoLivreScanBudgetAndScreenFilters`)

- [x] **Teto de 100 de fato:** quando o termo já trouxe alguma oferta, a varredura segue até 100 produtos (o limite de 40 só reserva espaço para tentar termos mais curtos quando nada foi encontrado).
- [x] **Variações:** produto "pai" do catálogo (ex.: modelo com várias configurações) traz até 4 variações com oferta, cada uma em um card.
- [x] **Filtros na tela:** depois da busca, mudar lojas, preço, frete grátis, cupom, categoria ou ordenação filtra só os resultados carregados; Buscar consulta as plataformas de novo.
- [x] **Modal de regras:** "Como funciona a busca e as regras" mostra as lojas conectadas do usuário, o `matt_tool` usado e os limites/regras da busca.
- [ ] **Validar com a conta real:** repetir "dell inspiron" + Notebooks e conferir o selo "N ofertas · 100 produtos analisados".

## 📊 Métricas de afiliado na Buscar Ofertas (`AffiliateOfferMetrics`)

- [x] **Comissão:** Shopee pela API (`commissionRate`/`commission`); Mercado Livre e Amazon pela taxa do usuário (modal de regras ou Radar → Perfil), pois as APIs não expõem a comissão de afiliado.
- [x] **Vendidos, avaliações e mais vendidos:** ML via `/items` em lote, `/reviews/item` e `/highlights` da categoria; Shopee via `sales` e `ratingStar`. Sem dado, o card mostra "não informado".
- [x] **Vantagem e segurança:** notas 0–100 por oferta (desconto, frete, comissão, avaliação, ranking, vendas × loja oficial, reputação, MercadoLíder, Full, garantia, condição) e "Oportunidade" como ordenação padrão.
- [x] **Painel de métricas:** faixa/mediana de preço, % vendedores confiáveis, % loja oficial e frete grátis, nota média, maior comissão, melhor oportunidade, mais vendido e menor preço.
- [x] **Filtros de tela:** somente loja oficial, vendedores confiáveis e mais vendidos.
- [ ] **Informar sua taxa do ML:** salvar a taxa média do painel de afiliados no modal de regras para ver a comissão estimada.
- [ ] **Validar com a conta real:** conferir no log `[ML Busca] Métricas:` quantos vendedores, avaliações, ranking e vendas a API devolveu (o ML pode omitir `sold_quantity` para quem não é o vendedor).

## 💰 Comissão do Mercado Livre por categoria (`MercadoLivreCategoryCommission`)

- [x] **Categoria do anúncio:** `category_id` do `/items` ➔ `/categories/{id}` (`path_from_root`) ➔ categoria raiz, com cache de 12 h.
- [x] **Regras de comissão:** `AffiliateCommissionRules` com a tabela do afiliado generalista do ML (venda direta): 16% Beleza/Moda/Esportes, 12% Casa/Ferramentas/Games/Brinquedos e outras, 5% Eletrônicos/Informática/Celulares/Câmeras/Eletrodomésticos, 0% Alimentos e Bebidas; fora da tabela vale a taxa do perfil.
- [x] **Card:** "💰 Comissão estimada: 5% (Informática) · R$ …" no lugar de "informe sua taxa nas regras"; a tabela aparece no modal de regras.
- [x] **Imagem:** clique abre modal ampliado com anterior/próxima e miniaturas (ML: todas as fotos do produto; Amazon: variantes; Shopee: foto principal).
- [x] **Botões:** "Converter & Gerar Link" e "Abrir" lado a lado, com ícones no padrão dos Grupos monitorados.
- [ ] **Conferir as taxas:** comparar a tabela com o painel de afiliado do ML (o ML pode mudar as taxas; a venda indireta paga a metade).

## ❔ Tooltips nos selos da oferta (`OfferScoreTooltips`)

- [x] **Ícone (?):** `bi-question-circle` à direita de Oportunidade, Vantagem e Segurança nos cards da Buscar Ofertas.
- [x] **Bootstrap Tooltip:** `bootstrap.bundle.min.js` carregado no WebUi e no Mobile; `ofertioTooltips.init` no `OnAfterRenderAsync` (hover/foco e toque/clique).
- [x] **Conteúdo:** faixas da Oportunidade (0–39 baixa, 40–69 boa, 70–100 super oferta), composição da Vantagem (desconto real, frete, comissão, avaliação, ranking, vendas) e níveis de Segurança.

## 🎯 Relevância e precisão da busca ML (`MercadoLivreRelevanceAndMainProducts`)

- [x] **Relevância nativa:** "Relevância (Padrão Mercado Livre)" é a ordenação padrão e mantém a ordem exata da API; o score Ofertio só reordena em "Melhor vantagem + segurança". O `/products/search` não aceita `sort` (o `/sites/MLB/search`, que aceita, responde 403 para apps de terceiros) e já devolve na relevância do ML.
- [x] **Categorias em 1 clique:** atalhos "Buscar só em" com os domínios sugeridos (`domain_discovery` + `domain_id` dos resultados), pois o `/products/search` não devolve `available_filters`.
- [x] **Anti-acessórios:** "Apenas produtos principais (ocultar acessórios)" na tela e na varredura do ML.
- [x] **Tooltips:** ícone (?) com `data-bs-toggle="tooltip"`, `data-bs-placement="top"` e `title`; `window.initTooltips` (`hover focus click`) no primeiro render e após cada atualização da lista.
- [ ] **Validar com a conta real:** buscar "iphone 15" com e sem "Apenas produtos principais" e conferir os atalhos de categoria.

## 🏷️ Pílulas de categoria e selos clicáveis (`CategoryChipsAndScoreBadgeModal`)

- [x] **Categorias preservadas:** todas as categorias encontradas para o termo ficam na tela (`MergeCategoryChips`); a selecionada fica azul e as demais outline; troca direta com 1 clique, sem passar por "Todas".
- [x] **Outras categorias:** acima de 6, o botão `bi-funnel` alterna o restante sem nova chamada à API (a selecionada sempre aparece).
- [x] **Selos clicáveis:** o selo inteiro de Oportunidade, Vantagem e Segurança tem tooltip (hover/foco), `cursor: pointer; user-select: none` e abre o modal de explicação controlado pelo componente, com os números daquela oferta.
- [ ] **Validar no navegador:** buscar "chinelo", clicar em "Sandálias e tamancos", trocar direto para outra pílula e abrir o modal de cada selo (desktop e celular).

## 🔗 Link de rastreio com a URL do ambiente (`DynamicTrackingLinkBaseUrl`)

- [x] **URL dinâmica:** o "Link de rastreio Ofertio" (Gerador de Links, página pública, Buscar Ofertas e seletor de link dos agendadores) troca o `http://localhost:5001` por `NavigationManager.BaseUri` (`GetBaseUrl()`), ex.: `https://dev.ofertio.com.br/AchadinhoCerto/{code}`.
- [x] **Redirect pela WebUi:** `ShortLinkProxyExtensions` encaminha `/{storeSlug}/{code}` e `/r/{code}` para a API, que registra o clique e redireciona.
- [x] **Preservado:** domínio de encurtador configurado (ex.: `tflow.link`) não é trocado; botões Copiar, WhatsApp e Telegram mantêm os ícones `bi-*`.
- [ ] **Disparos automáticos:** mensagens montadas no servidor (grupos monitorados, robôs) ainda usam `Ofertio:ShortLinks:PublicBaseUrl`; definir o domínio público por ambiente.

## 🛡️ Código MLB preservado no link de afiliado (`MercadoLivreProductIdPreservation`)

- [x] **Sem ID fictício:** link do Mercado Livre sem código MLB (ex.: `meli.la` que abre uma vitrine `/social/...`) é recusado com mensagem clara, em vez de virar `MLB1234567890`.
- [x] **URL preservada:** `/p/MLB20621890` continua `/p/MLB20621890` e `/MLB-2062189012-item` continua anúncio; só os `matt_*` de terceiros são trocados pelos da loja.
- [x] **Extração:** catálogo e anúncio (com ou sem hífen, 6 a 14 dígitos), sem confundir `MLBU...` nem números maiores.
- [ ] **Homologar:** publicar com EXECUCAO_HOMOLOGACAO e converter de novo `https://www.mercadolivre.com.br/p/MLB20621890?matt_tool=97303724&matt_word=97303724`; links já gerados com `MLB1234567890` precisam ser refeitos.

## ✏️ Edição de lojas em Minhas Lojas / Integrações (`StoreEditModal`)

- [x] **Botão no card:** "Editar loja" (`bi-pencil-square`) antes de "Desconectar loja".
- [x] **Mesmo modal:** `ConnectStoreModal` abre como "Editar loja" com plataforma pré-selecionada (travada), apelido e ID de afiliado preenchidos; "Conectar nova loja" sempre abre limpo em modo criação.
- [x] **UPDATE de verdade:** `POST/PUT api/marketplace-auth/lojas/{id}` atualiza a loja existente (mesma `ShopId`), sem criar registro duplicado, mesmo quando o apelido muda.
- [x] **Validações:** dono da loja, ID de afiliado normalizado (aceita link), bloqueio de ID duplicado; campo de ID vazio mantém o atual.
- [ ] **Homologar:** editar o apelido e o ID de uma loja em `https://dev.ofertio.com.br/minhas-lojas` e conferir que continua um único card.

## 🔗 Link de rastreio com slug numérico (`ShortLinkNumericSlug`)

- [x] **Causa do 404:** a rota `/{storeSlug}/{code}` (API e proxy da WebUi) exigia slug começando com letra; loja com apelido numérico (ex.: `97303724`) gerava `/97303724/xh8pmzh`, que caía no "não encontrado" do Blazor.
- [x] **Rota alinhada ao gerador:** slug aceita `^[A-Za-z0-9]{1,80}$` nos dois lados; `GenerateSlug` só produz letras/dígitos ASCII. O formato do link continua `{base}/{slug}/{code}` (sem prefixo `/p/`, que é a página do Conversor Público).
- [x] **Tracing:** o redirect registra slug e código na entrada e o host de destino no 302 (ou "Código curto não encontrado").
- [ ] **Homologar:** abrir `https://dev.ofertio.com.br/97303724/xh8pmzh` e conferir o redirect para o Mercado Livre.

## 🌐 Integrações Globais no Painel Master (`GlobalIntegrationsMaster`)

- [x] **Menu Master:** "Integrações Globais" com submenu "API Mercado Livre" (espaço para TikTok Shop e Shopee como "Em breve").
- [x] **Tela API Mercado Livre:** status (`Ativo` / `Expirado` / `Desconectado`), data/hora de expiração do token, "Autorizar / Reautorizar Mercado Livre" (OAuth do administrador), desconectar e "Tempo de Cache de Buscas (minutos)" salvo nas configurações globais.
- [x] **Token Master:** tabela `GlobalIntegrationTokens` (criptografada); as buscas usam o token Master primeiro e mantêm o token legado do usuário como fallback.
- [x] **Refresh automático:** token expirado ou recusado (401/403) dispara o `refresh_token` no backend, grava o novo token e repete a busca de forma transparente.
- [x] **Cache global de buscas:** a mesma consulta (texto + filtros) dentro do tempo configurado (padrão 15 min, 0 desliga) não consome cota da API; cada cliente recebe o próprio `matt_tool`.
- [x] **Minhas Lojas:** removidos "API de busca: autorizada (OAuth)" e "Reautorizar Mercado Livre"; ficam ID de afiliado, Editar loja e Desconectar loja.
- [x] **Matriz de menus:** permissão "Integrações Globais" por plano; desligada, oculta Buscar Ofertas, Radar de Ofertas e Perfil de mineração (somente menu).
- [ ] **Homologar:** autorizar o Mercado Livre em Master ➔ Integrações Globais ➔ API Mercado Livre e fazer uma busca em `/busca` com um cliente sem OAuth próprio.

## 🎛️ Filtros compactos da Buscar Ofertas (`CompactSearchFilters`)

- [x] **Barra única:** `[Plataformas] [Filtros e Qualificadores] [Categorias] [Filtros]` logo abaixo da busca; cabeçalho em uma linha, alertas e status das lojas compactos e métricas recolhidas no botão "Métricas", para a vitrine aparecer sem rolagem.
- [x] **Combos multisseleção (`MultiSelectDropdown`):** checkboxes internos, campo de busca no topo que filtra a partir de 3 letras (sem diferenciar acentos) e resumo no botão, ex.: "Filtros e Qualificadores (2)".
- [x] **Categorias:** substituem o select "Categoria" e as pílulas "Buscar só em"; mostram quantas ofertas carregadas há em cada uma e filtram **só em memória**, sem nova chamada HTTP.
- [x] **Modal "Filtros" (`bi-sliders`):** preço mínimo, preço máximo e ordenação (Relevância como padrão), com "Aplicar Filtros" e "Limpar"; edita um rascunho (`OfferSearchAdvancedFilters`) que recebe novos campos sem mudar a tela.
- [ ] **Validar no navegador:** buscar "notebook", marcar duas categorias, abrir "Filtros" com preço mínimo maior que o máximo (deve recusar) e conferir no celular.
- [x] **Padrão de combos (seção 2 das diretrizes de UI):** `MultiSelectDropdown` mostra a busca só com mais de 6 opções (Plataformas e Filtros e Qualificadores ficam sem busca), foca a busca ou a lista ao abrir e navega por setas, Espaço, Enter e Esc.
- [ ] **Validar o teclado:** abrir "Categorias" com mais de 6 opções, descer com as setas, marcar com Espaço ou Enter e fechar com Esc sem disparar a busca.

## 🧭 Combos centralizados e timeout do Mercado Livre (`CompactCombosAndMlTimeout`)

- [x] **Barra de filtros centralizada:** `.busca-filter-bar` com `display:flex`, `flex-wrap:wrap`, `justify-content:center`, `align-items:center`, `gap:8px` e `width:100%`; no tablet e no celular o menu ancora na barra e nunca vaza na horizontal.
- [x] **Lista vertical blindada:** opções em coluna, uma por linha, com o checkbox à esquerda do texto; o `app.css` e o `app-interop.js` agora levam `?v=` (versão do build) para o navegador não reaproveitar o CSS antigo do cache.
- [x] **Fechar ao clicar fora:** `ofertioMultiSelect.watch/unwatch` observa `pointerdown` e `focusin` fora do combo aberto e chama `CloseFromOutside`; abrir outro combo fecha o anterior. O `dropdown-menu` do Bootstrap não foi usado porque o atalho de teclado nativo dele exige `data-bs-toggle` e disputaria o estado com o Blazor.
- [x] **Teclado e mouse:** foco automático na busca "Filtrar (mín. 3 letras)" (ou na lista, quando há até 6 opções); setas ou hover destacam o item; Espaço marca/desmarca; Enter marca/desmarca o item destacado ou fecha sem item destacado; Esc fecha e devolve o foco ao botão.
- [x] **Timeout do Mercado Livre:** `HttpClient` do ML com `Integrations:MercadoLivre:HttpTimeoutSeconds` (padrão 30 s, aceito de 15 a 120 s); Shopee e Amazon seguem em 12 s.
- [x] **Degradação amigável:** produto lento é ignorado e os demais aparecem; página seguinte lenta mantém as ofertas já achadas e o "Carregar mais" retoma dela; timeout na primeira página mostra "O Mercado Livre demorou para responder. Tente buscar de novo em instantes." no lugar da mensagem crua do .NET.
- [x] **Assíncrono ponta a ponta:** `.Result` removidos da busca unificada e do enriquecimento de métricas do ML (tudo com `await`).
- [ ] **Validar no navegador:** Ctrl+F5 na `/busca`, conferir os três combos centralizados, clicar fora para fechar e testar no celular.

## 🏷️ Categorias reais na Buscar Ofertas (`RealCategoryFilter`)

- [x] **Categoria real do item:** cada oferta do Mercado Livre usa o `domain_id` do produto de catálogo (ou da página do `products/search`, quando o produto não informa) e o `category_id` do anúncio, traduzido para português por `/categories/{id}` (cache de 12 h).
- [x] **Combo sem nomes em inglês:** opções = domínios das ofertas carregadas (nome da categoria real), sugestões do `domain_discovery` (nome em português, lembrado para as próximas buscas) e as marcadas; o nome derivado do id ("Flip flops") é só o último recurso, e domínios vistos sem nenhuma oferta não entram.
- [x] **Buscar com categoria:** as categorias marcadas vão para a API (a query string agora repete a chave `MercadoLivreCategories`, antes ia `System.Collections.Generic.List...`) e viram uma varredura do `products/search` com `domain_id` por categoria (até 3), intercalando as ofertas sem repetição; "Carregar mais" mantém as categorias.
- [x] **Contador e categoria zerada:** o contador continua em memória e em tempo real; categoria marcada com 0 ofertas mostra o aviso "Nenhuma oferta carregada em …" com o botão "Buscar nesta categoria" (`bi-search`) ou, se já foi consultada, "O Mercado Livre não retornou ofertas ativas em …".
- [ ] **Validar no navegador:** buscar "havaianas", conferir os nomes em português no combo, marcar uma categoria com 0 e clicar em Buscar.

## 📡 Radar híbrido e comparador multilojas (`RadarHybridAndCompare`)

- [x] **Índice da Busca:** cada busca em `/busca` grava as ofertas em `IndexedCatalogOffers` (termo, modelo, preço à vista, frete e comissão).
- [x] **Consulta híbrida do Radar:** a listagem e o worker olham primeiro essa base. O termo do perfil sem resultado dentro da janela de cache do Painel Master (0 desliga o cache) é consultado nas APIs oficiais pelo `MercadoLivreApiService` e pelos conectores de Shopee e Amazon. O cache do Mercado Livre continua valendo nessa chamada.
- [x] **Worker:** a cada ciclo usa os termos ativos do perfil (até 20). A tela do radar refresca no máximo 2 termos por abertura, para não travar a página; o restante fica com o worker.
- [x] **Comparar em outras lojas:** botão com `bi-diagram-3` no card da Busca, do Radar e das ofertas monitoradas. O modal identifica o modelo (`ExtractProductModel`), mostra o produto de referência, um spinner enquanto busca as outras lojas e a tabela Plataforma, Título, Preço à Vista, Frete, Comissão e Gerar Link. A loja de menor preço fica destacada.
- [ ] **Homologar:** a migration `AddIndexedCatalogOffers` já foi aplicada em `localhost\SQLEXPRESS` / `Ofertio`. Depois de publicar, buscar um produto com código de modelo em `/busca`, abrir "Comparar em outras lojas" e conferir o menor preço; no Radar, um termo do perfil sem oferta recente deve consultar as lojas.

