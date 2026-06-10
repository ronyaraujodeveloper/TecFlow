# 📝 TecFlow - Roadmap, Arquitetura & Contexto Geral

> **Painel principal do projeto** (antigo `TODO.md`). Tarefas, regras de código e links para `docs/`. A IA deve marcar checkboxes aqui ao concluir implementações (ver `.cursorrules`).

## 🎯 Objetivo do Projeto
Plataforma de **automação e inteligência para afiliados de alta escala**: orquestração de engajamento (comentários, mensagens e links), conciliação financeira de comissões, catálogo de produtos de divulgação e integrações com TikTok Shop e Shopee. O ecossistema combina backend robusto em C# (`TecFlow.API`, `TecFlow.Worker`, `TecFlow.Orquestrador`) com o frontend **`TecFlow.WebUi`** para controle, auditoria e monitoramento em produção.

## 🛠️ Stack Tecnológica Definida
- **Backend:** .NET 8.0 Web API (C#)
- **Frontend:** Blazor WebApp — projeto **`TecFlow.WebUi`** (ASP.NET Core .NET 8.0)
- **Banco de Dados:** PostgreSQL (Instalado localmente via EDB, Porta 5432)
- **ORM:** Entity Framework Core (EF Core) com Npgsql

---

### 🌐 Diretrizes de Codificação, Localização e Encoding (Padrão Obrigatório)

Para evitar que a acentuação em PT-BR fique quebrada (ex: exibir '??' ou caracteres corrompidos no portal ou banco de dados), todo desenvolvedor e IA parceira deve seguir rigorosamente estas regras:

1. **Encoding de Arquivos (UTF-8 com BOM):**
   - Todos os arquivos de código-fonte criados ou modificados (`.razor`, `.cs`, `.html`, `.css`, `.json`) **DEVEM** ser salvos explicitamente utilizando a codificação **UTF-8 com assinatura (BOM)**. Isso garante que o IIS e o compilador do .NET processem os acentos corretamente em ambientes Windows/Server.

2. **Cultura e Localização Nativa (PT-BR):**
   - O portal frontend `TecFlow.WebUi` opera sob a cultura `pt-BR`. O pipeline de inicialização configura globalmente as propriedades `DefaultThreadCurrentCulture` e `DefaultThreadCurrentUICulture` para garantir consistência em formatações de data, moeda e decodificação textual.

3. **Persistência de Dados (PostgreSQL):**
   - Todas as comunicações com o banco de dados via driver `Npgsql` devem explicitar os parâmetros de encoding na Connection String (`Client Encoding=UTF8;Encoding=UTF8;`), garantindo que strings enviadas via Entity Framework Core preservem a acentuação original no armazenamento físico.

---

## 📚 Documentação Complementar
* Veja a [Lista de Mudanças de Arquivos](./docs/LISTA_ARQUIVOS_MUDANCAS.md)
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

8. **Auto-atualização do Roadmap:** Toda vez que eu te pedir para executar uma tarefa descrita neste arquivo, assim que você concluir a implementação do código com sucesso e sem erros de compilação, você DEVE marcar automaticamente a respectiva tarefa como concluída mudando de `[ ]` para `[x]` **neste `README.md`**, sem que eu precise te pedir explicitamente. Sincronize também `docs/LISTA_ARQUIVOS_MUDANCAS.md` e `docs/DIAGRAMAS_ARQUITETURA.md` conforme `.cursorrules`.

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

- [x] 5.0. Fundação de domínio e contratos (Core + Business)
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
  - Desenvolver a interface "Minhas Lojas / Integrações" exibindo em cartões responsivos todas as contas integradas do TikTok/Shopee, seus respectivos status de conexão (Verde/Vermelho) e o botão para disparar o OAuth de uma nova conta (permitindo gerenciar 10 ou mais lojas).

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
  - Renderizar listagem responsiva contendo o histórico de links processados.
  - Adicionar badges dinâmicos para identificar visualmente a plataforma de destino (Shopee, TikTok, Amazon, etc.) e o contador agregador de cliques em tempo real baseado no log de telemetria.

  ### Fase 12: Motor de Busca Cruzada e Comparador de Preços Multicloud (Backend) 🧠🔍
- [ ] 12.1. Evolução da Interface Strategy e Extração de Scraping/Meta-dados
  - Estender a interface `IPlatformLinkStrategy` para incluir o método `Task<ProductMetadataDto> ExtractProductMetadataAsync(string url)`.
  - Implementar um extrator de metadados básico (via API oficial ou crawler leve/HtmlAgilityPack) para identificar o Título Comercial, Imagem e Preço Atual do link original colado pelo usuário.

- [ ] 12.2. Implementação do Motor de Busca Cruzada em Paralelo (Cross-Search)
  - Estender a interface `IPlatformLinkStrategy` para incluir o método `Task<List<ProductSearchMatchDto>> SearchProductByTitleAsync(string title, Guid storeId)`.
  - Implementar nas classes especialistas (Shopee, TikTok, Amazon, Mercado Livre) a chamada de busca por palavra-chave nas respectivas APIs de afiliados.
  - Criar o serviço `ProductArbitrageService` que dispara as buscas em paralelo (`Task.WhenAll`) em todas as plataformas conectadas e ativas do usuário, ignorando falhas individuais de APIs externas para não travar o fluxo.

- [ ] 12.3. Algoritmo de Rankeamento, Filtragem por Menor Preço e Normalização
  - Desenvolver lógica de higienização de strings para comparar os títulos (removendo termos ruidosos como "Frete Grátis", "Original", "Promoção").
  - Filtrar os resultados para garantir que o preço encontrado nas plataformas concorrentes seja **menor** que o preço do link original.
  - Ordenar o resultado de forma ascendente pelo preço e limitar o retorno a no máximo 3 sugestões alternativas, já gerando o link de comissão convertido para cada uma delas.

### Fase 13: Painel de Otimização e Sugestões de Ofertas no Frontend (TecFlow.WebUi) 💸
- [ ] 13.1. Componente Reativo "Sugestões de Melhor Preço" (UI/UX)
  - Desenvolver uma seção dinâmica na página `GeradorLinks.razor` que exibe um *loader* de busca (ex: "Buscando preços melhores em outras plataformas...") logo após o link principal ser gerado.
  - Renderizar até 3 cards de sugestões alternativas utilizando abordagem Mobile-First.

- [ ] 13.2. Anatomia do Card de Sugestão e Ações Rápidas
  - Cada card de sugestão deve exibir de forma clara:
    * O logo do marketplace concorrente onde o produto mais barato foi encontrado.
    * O novo preço em destaque comparado ao preço original (ex: * De R$ 100,00 por R$ 85,00 na Amazon*).
    * Botões rápidos independentes de "Copiar Link Alternativo" e "Compartilhar".

- [ ] 13.3. Telemetria de Conversão de Arbitragem
  - Ajustar a tabela `LinkClickLog` para registrar quando um clique veio de um link de sugestão alternativa (arbitragem), permitindo que o usuário saiba no Dashboard se as sugestões de menor preço estão performando melhor que os links originais que ele cola.


  Como Analista de Sistemas Sênior, vejo que chegamos no momento mais crítico da transição de um projeto de software: a Infraestrutura de Produção e Homologação Física. Sair do ambiente local e conectar com gigantes de tecnologia exige conformidade com segurança, DNS, políticas de privacidade e fluxos de consentimento (OAuth).

Seu roteiro está excelente, mas para torná-lo um Guia de Produção Infalível, incluí 4 pontos cruciais que estavam faltando e que poderiam travar suas aprovações nas plataformas:

1. Configuração de Segurança e Entregabilidade de E-mail (SPF, DKIM, DMARC): Essencial para o Gmail e iCloud não bloquearem as notificações e ativações de conta do TecFlow.

2. Página de Política de Privacidade e Termos de Uso: O Facebook, TikTok e Google rejeitam aplicativos de desenvolvedor que não possuem esses links públicos no domínio oficial.

3. SSL/TLS Rígido (HTTPS): Nenhuma API de marketplace aceita callbacks em HTTP.

4. Criação de Usuários de Teste (SandBox): Antes de ir para a API pública da Shopee/TikTok, precisamos testar no ambiente de simulação deles.

Aqui está o seu Roteiro de Infraestrutura e Homologação Omnichannel completo e revisado. Salve-o, pois vamos executá-lo item por item.

🗺️ Roteiro de Infraestrutura, Credenciais e Homologação TecFlow
Módulo A: Identidade Digital e Infraestrutura (O Alicerce)
[ ] A.1. Registro e Apontamento do Domínio: * Registrar tecflow.com.br no Registro.br.

Configurar os servidores de DNS (Cloudflare recomendada para proteção contra ataques e gerenciamento rápido).

[ ] A.2. Configuração do Servidor e SSL:

Configurar o servidor IIS/Linux de homologação apontando para o subdomínio homolog.tecflow.com.br.

Instalar certificado SSL válido (Let's Encrypt) para garantir HTTPS obrigatório.

[ ] A.3. Blindagem de E-mail (Entregabilidade):

Configurar os apontamentos TXT de SPF, DKIM e DMARC no DNS do domínio para que os e-mails do sistema cheguem na caixa de entrada do Gmail e iCloud.

[ ] A.4. Publicação dos Termos Legais (Obrigatório para APIs):

Colocar no ar uma página simples em tecflow.com.br/privacidade e tecflow.com.br/termos (essencial para aprovação nos consoles de desenvolvedor).

Módulo B: Consoles de Desenvolvedor e Credenciais (As Chaves)
[ ] B.1. Google Cloud Console (Gmail e Login Social):

Criar projeto no Google Cloud, configurar a tela de consentimento OAuth (adicionando os links de privacidade) e gerar o Client ID e Client Secret. Ativar a API do Gmail.

[ ] B.2. Apple Developer Program (iCloud/Apple Login):

Configurar o Identifiers, Service IDs e chaves privadas (.p8) para o botão "Entrar com Apple".

[ ] B.3. Meta for Developers (Facebook Login):

Criar aplicativo do tipo "Consumidor/Empresa", configurar o escopo de login e obter ID e Chave Secreta do App.

[ ] B.4. Shopee Open Platform (Afiliados e Lojas):

Criar conta de desenvolvedor na Shopee Open Platform. Solicitar acesso à Affiliate API e à V2 Open API (gerenciamento de lojas).

[ ] B.5. TikTok Developer / TikTok Shop Academy:

Cadastrar a empresa no console de desenvolvedor do TikTok para obter as credenciais do programa de afiliados e login de parceiro.

[ ] B.6. OpenAI Developer Platform:

Criar conta organizacional na OpenAI, configurar limites de faturamento e gerar as chaves de API secretas (sk-...) para o motor de IA.

Módulo C: Homologação e Testes de Circuito Fechado (A Prova de Fogo)
[ ] C.1. Teste de Autenticação Unificada e Auto-linking:

Validar em ambiente de homologação se o login e registro via Google, Facebook e Apple associam o usuário corretamente no banco PostgreSQL sem duplicar contas.

[ ] C.2. Simulação de Vínculo Multi-Lojas (OAuth Sandbox):

Utilizar as ferramentas de teste (Console Sandbox) da Shopee e TikTok para simular o clique em "Conectar Loja", autorizar o acesso e validar se o token entra criptografado na tabela IntegracaoLoja.

[ ] C.3. Teste de Estresse do Motor Strategy e Unshorten:

Submeter links encurtados de teste (reais) para garantir que o backend expande a URL, descobre a plataforma correta e gera o novo link comissionável sem gargalos.

[ ] C.4. Validação da Telemetria e Redirecionamento:

Clicar nos links encurtados próprios gerados (tflow.link/...) através de um celular e de um computador. Validar se a tabela LinkClickLog captura os dados de dispositivo e se o redirecionamento joga o usuário na tela do produto com sucesso.

---
*Nota para a IA: Sempre siga este roadmap passo a passo e use a nova estrutura de pastas estabelecida. Não pule etapas e preze pela preservação do código de validação já existente.*