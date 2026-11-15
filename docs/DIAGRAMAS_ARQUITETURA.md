# 🗺️ Diagramas de Arquitetura

> Visão em Mermaid de como o Ofertio se conecta. As regras que sustentam cada fluxo estão em [backend-architecture.md](./rules/backend-architecture.md) e [marketplaces-oauth.md](./rules/marketplaces-oauth.md); o roteiro de produção em [CONFIGURACAO_PRODUCAO_DOCKER.md](./CONFIGURACAO_PRODUCAO_DOCKER.md). A versão anterior está em [arquivo/](./arquivo/DIAGRAMAS_ARQUITETURA_2026.md).

## 1. Dependências entre projetos

Gerado dos `<ProjectReference>` dos `.csproj` por [scripts/gerar-diagrama-dependencias.ps1](../scripts/gerar-diagrama-dependencias.ps1). Quando uma referência mudar, rode `powershell -File scripts\gerar-diagrama-dependencias.ps1` e cole a saída no lugar do bloco abaixo (seta = "depende de"; `InfrastructureServices` = `Ofertio.Infrastructure.Services`).

```mermaid
graph LR
    API --> Application
    API --> Business
    API --> Data
    API --> Database
    API --> Core
    API --> InfrastructureServices
    API --> Infrastructure
    API --> Observability
    Application --> Business
    Application --> Core
    Application --> Util
    Business --> Core
    Business --> Database
    Data --> Database
    Data --> Util
    Database --> Core
    Database --> Util
    E2E --> Business
    Infrastructure --> Business
    Infrastructure --> Core
    Infrastructure --> Database
    Infrastructure --> Util
    InfrastructureServices --> Business
    InfrastructureServices --> Observability
    InfrastructureServices --> Core
    InfrastructureServices --> Infrastructure
    InfrastructureServices --> Util
    Mobile --> SharedUi
    Observability --> Business
    Orquestrador --> Application
    Orquestrador --> Business
    Orquestrador --> Data
    Orquestrador --> Database
    Orquestrador --> Core
    Orquestrador --> InfrastructureServices
    Orquestrador --> Infrastructure
    Orquestrador --> Observability
    SharedUi --> Business
    SharedUi --> Core
    SharedUi --> Database
    SharedUi --> Util
    Tests --> API
    Tests --> Application
    Tests --> Business
    Tests --> Core
    Tests --> Database
    Tests --> InfrastructureServices
    Tests --> Infrastructure
    Tests --> Orquestrador
    Tests --> SharedUi
    Tests --> Util
    WebUi --> SharedUi
    Worker --> Application
    Worker --> Core
    Worker --> Data
    Worker --> Infrastructure
    Worker --> InfrastructureServices
    Worker --> Observability
```

## 2. Requisição WebUi → YARP → API → SQL Server

O host público é o da WebUi; a API só escuta internamente (`OrquestradorApi:BaseUrl`). As rotas cadastradas nos apps de terceiros e o link de rastreio passam pelo YARP (`MercadoLivreProxyExtensions`, `AllowAnonymous`). Não existe subaplicação `/api` no site da WebUi.

```mermaid
sequenceDiagram
    autonumber
    participant B as Navegador
    participant W as WebUi (Blazor)
    participant Y as YARP (MapForwarder)
    participant A as API (controllers)
    participant S as Services (Business + Infrastructure.Services)
    participant D as SQL Server (Ofertio)

    B->>W: página / ação da tela
    W->>A: serviço HTTP da SharedUi (OrquestradorApi:BaseUrl)
    B->>W: /api/mercadolivre/callback, /notifications ou /r/{code}
    W->>Y: rota repassada
    Y->>A: encaminha sem alterar o caminho
    A->>S: DTO de escrita ou [Nome]Filter (GET)
    S->>D: EF Core (AppDbContext, segredos via ValueConverter)
    D-->>S: entidades
    S-->>A: [Nome]ResponseDto (Status, Descricao, Data/DataList)
    A-->>W: JSON (ou 302 no link de rastreio)
    W-->>B: HTML / Blazor
    Note over A,D: clique do link de rastreio entra numa fila limitada e um serviço em segundo plano grava no SQL Server
```

## 3. Busca do Mercado Livre só pela API oficial (ordem dos tokens)

Sempre `Authorization: Bearer`, nunca token na URL; sem raspagem, sem busca anônima e sem contornar captcha (`MercadoLivreAntiBotGuard` suspende os GETs públicos por 15 min).

```mermaid
flowchart TD
    Q([Busca por palavra-chave]) --> C{Cache global<br/>texto + filtros + categoria<br/>padrão 15 min}
    C -- acerto --> R([Resultado: cada cliente recebe o próprio matt_tool])
    C -- falha --> T1[1. Token Master<br/>GlobalIntegrationTokens]
    T1 -- ok --> P
    T1 -- "401/403" --> RF[refresh_token e<br/>nova tentativa]
    RF -- ok --> P
    RF -- recusado --> T2[2. Token OAuth da conta do tenant]
    T2 -- ausente ou recusado --> T3[3. Integrations:MercadoLivre:AccessToken]
    T3 -- ausente ou recusado --> T4[4. Token do app<br/>client_credentials AppId/SecretKey]
    T2 -- ok --> P
    T3 -- ok --> P
    T4 --> P
    P[GET /products/search<br/>status=active, site_id=MLB, q] --> PR[GET /products/id<br/>buy_box_winner → faixa → /items → variações]
    PR --> R
    G[Anúncio GetItemAsync e<br/>Gerador de Links] --> G1[conta OAuth do usuário logado] --> G2[AccessToken fixo] --> G3[token do app]
```

Cada degrau só é usado se o anterior não existir ou for recusado. Rotinas em segundo plano nunca usam a conta de outro tenant.

## 4. OAuth com state selado (Mercado Livre e TikTok de Criador)

O `state` é selado por `OAuthStateProtector` (AES-256-GCM, chave HKDF de `Encryption:Key`) e leva `UserId`, `TenantId`, apelido, origem e, no ML, o `code_verifier` do PKCE. Validade de 15 min e uso único; por isso o callback é anônimo.

```mermaid
sequenceDiagram
    autonumber
    participant U as Usuário
    participant W as WebUi
    participant A as API
    participant P as Plataforma (ML ou TikTok)
    participant D as SQL Server

    U->>W: Conectar conta
    W->>A: pede URL de autorização
    A->>A: OAuthStateProtector.Protect(UserId, TenantId, origem, verifier)
    A-->>U: redireciona para a autorização (ML: PKCE S256 · TikTok: shop.tiktok.com/alliance/creator/auth)
    U->>P: consente
    alt Mercado Livre
        P-->>U: redirect para /api/mercadolivre/callback?code&state
        U->>W: GET /api/mercadolivre/callback
        W->>A: YARP → MercadoLivreController.callback (anônimo)
        A->>A: TryUnprotect(state): válido, não expirado, uso único
        A->>P: POST oauth/token (code + code_verifier)
    else TikTok de Criador
        P-->>U: volta para a Redirect URL do app (/integracoes/tiktok/callback)
        U->>W: callback da WebUi
        W->>A: POST api/marketplace-auth/oauth/sealed-callback
        A->>A: TryUnprotect(state) e despacha pela plataforma do state
        A->>P: GET auth.tiktok-shops.com/api/v2/token/get
        Note over A,P: só user_type = 1 (criador) é aceito
    end
    P-->>A: AccessToken + RefreshToken
    A->>D: grava criptografado em MarketplaceAccounts
    A-->>U: redireciona para /marketplace-accounts?status=success|error
```

## 5. Checkout e cobrança do plano

Hoje são dois fluxos separados. O checkout do cliente usa o gateway simulado (aprova sem cobrar) e só registra o pedido. A cobrança real nasce no Master, e o webhook do ASAAS só ativa o tenant e renova. Quem grava `PlanCode`/`BillingCycle` é o Master (`TenantAdminService`). Número, CVV e validade do cartão só trafegam até o gateway: o pedido guarda bandeira e 4 últimos dígitos.

```mermaid
sequenceDiagram
    autonumber
    participant U as Usuário
    participant M as MyPlanService.CheckoutAsync
    participant G as SimulatedPlanPaymentGateway
    participant D as SQL Server
    participant MS as Master (TenantAdminService)
    participant AS as Gateway ASAAS
    participant WH as AsaasWebhookController
    participant WS as AsaasWebhookService

    rect rgb(240,240,240)
    Note over U,D: Fluxo A: checkout do cliente (fase de telas)
    U->>M: plano + ciclo + CreditCardDto
    M->>M: valida conta, ciclo, dados cadastrais e cartão
    M->>G: ChargeAsync (aprova sem cobrar)
    G-->>M: status Simulated
    M->>D: PlanCheckouts (bandeira, last4, status); não altera o Tenant
    M-->>U: PlanCheckoutResponseDto
    end
    rect rgb(235,245,255)
    Note over MS,D: Fluxo B: cobrança iniciada pelo Master
    MS->>D: grava Tenant.PlanCode e BillingCycle
    MS->>AS: CreateSubscriptionAsync
    AS-)WH: POST api/webhooks/asaas (header asaas-access-token)
    WH->>WS: ValidateTokenAsync (401 se inválido) e HandleAsync
    WS->>D: SaasPayments por GatewayPaymentId
    alt PAYMENT_CONFIRMED / RECEIVED
        WS->>D: Tenant Ativo e RenewalAt +1 mês (anual: +1 ano)
    else PAYMENT_OVERDUE
        WS->>D: Tenant Inadimplente
    end
    WS-)U: e-mail de aviso do plano
    end
```

## 6. Produção Docker

Só o Caddy publica portas no host. Entre containers o tráfego é HTTP e o TLS termina no Caddy (rede `ofertio-net`, VPS em `/var/www/ofertio`). Os Dockerfiles, o `Caddyfile` e o `docker-compose.yml` da aplicação ainda não estão no git.

```mermaid
flowchart LR
    I([Internet]) -- "80 / 443" --> C[caddy<br/>TLS Let's Encrypt]
    subgraph NET [rede ofertio-net]
        C -- "/api/*" --> A[api :8080]
        C -- "demais rotas" --> W[webui :8080]
        W -- "OrquestradorApi__BaseUrl<br/>http://api:8080/" --> A
        A -- "SQL_CONNECTION :1433" --> S[(sqlserver<br/>volume sqlserver_data)]
        A -- "RabbitMq__Host :5672" --> R[rabbitmq]
        A --- U[volume uploads<br/>/app/uploads]
    end
```
