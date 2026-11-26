# Auditoria técnica: segurança e performance (Ofertio)

- **Data:** 25/11/2026
- **Escopo:** repositório inteiro no commit `2f04597` (branch `main`).
- **Método:** somente leitura. Foram feitas leitura de código, `git log -p` com regex (o `gitleaks` não está instalado e não foi instalado), `dotnet list package --vulnerable --include-transitive` e `dotnet list package --outdated`.
- **Limites:** nada foi executado contra produção nem contra serviços externos.
- **Segredos:** sempre mascarados, com os 4 primeiros caracteres seguidos de `****`.

---

## 1. Resumo executivo

**Notas:** Segurança **3/10**. Performance **6/10**.

**Apto para produção hoje: NÃO.** O sistema já está publicado (commit `d3fd7f2`), e os itens do bloco "Corrigir agora" (seção 6) precisam ser resolvidos antes de qualquer novo deploy, idealmente em caráter emergencial.

**Os 5 riscos mais graves:**
1. **`/api/Usuarios` é anônimo.** Sem login, qualquer pessoa lista os usuários de todos os inquilinos, com `PasswordHash` e tokens TikTok. Também altera, apaga e cria usuários. O usuário criado cai dentro do inquilino ativo mais antigo e passa a ler os dados dele.
2. **Tomada de qualquer conta com login Apple forjado.** O id_token não tem a assinatura validada, e o sistema vincula a conta pelo e-mail automaticamente.
3. **Segredos de produção versionados no git.** Chave JWT (permite forjar tokens Master), chave de criptografia das credenciais, segredo dos webhooks, ApiKey da Evolution, chave OpenAI e client secret do Mercado Livre.
4. **Créditos sem pagamento.** `POST /api/achadinhos/creditos/pacote` soma créditos sem cobrar nada.
5. **Webhook do Asaas estende a assinatura duas vezes.** Como o Asaas envia `PAYMENT_CONFIRMED` e depois `PAYMENT_RECEIVED`, um mês pago vira dois, e cada reenvio soma mais um.

**Pontos fortes:**
- Senhas com BCrypt.
- Reset de senha bem feito.
- SQL sempre parametrizado.
- Nenhum XSS encontrado.
- Upload de avatar seguro.
- `IHttpClientFactory` com Polly.
- Preço do plano calculado a partir do banco.
- Nenhum `.Result`/`.Wait()` em produção.

---

## 2. Arquitetura identificada (Fase 0)

### 2.1 Stack

| Camada | Tecnologia / versão | Evidência |
|---|---|---|
| Backend | **.NET 8 (net8.0)** em API, Worker, Orquestrador, bibliotecas, WebUi | `Ofertio.API/Ofertio.API.csproj:4` |
| Mobile | .NET MAUI Blazor Hybrid **net9.0** | `Ofertio.Mobile/Ofertio.Mobile.csproj:4-5,43` |
| Web | Blazor Web App (Interactive Server) + YARP 2.3.0 | `Ofertio.WebUi/Program.cs:26-27`, `Ofertio.WebUi.csproj:25` |
| ORM | EF Core 8.0.8 (SQL Server em produção; Npgsql suportado) | `Ofertio.Database/Data/RelationalDatabaseOptions.cs:28,35`; `docker-compose.yml:31,68` |
| Auth | JWT HMAC (JwtBearer 8.0.8), Identity Core (só `AddIdentityCore`), BCrypt.Net-Next 4.2.0; WebUi com cookie + Google/Facebook/Apple | `Ofertio.API/Program.cs:108-163`; `Ofertio.WebUi/Extensions/AuthenticationExtensions.cs:32-122` |
| Filas | MassTransit.RabbitMQ 8.3.4 (só engajamento) + 14 `BackgroundService` | `Ofertio.Infrastructure.Services.csproj:41-42`; `Ofertio.API/Program.cs:93-106` |
| Cache | `IMemoryCache`. Não há Redis | `MasterBackofficeRegistrationExtensions.cs:12` |
| Logs/telemetria | Serilog 4.3.1, OpenTelemetry 1.11.x, Prometheus | `Ofertio.Observability.csproj:21-28` |
| Resiliência | Polly 8.2.0 / Microsoft.Extensions.Http.Polly 8.0.8 | `IntegrationHttpClientRegistrationExtensions.cs:42-74` |
| Frontend JS | Não há `package.json`. Só JS interop estático | `Ofertio.SharedUi/wwwroot/app-interop.js` |

> **Achado de plataforma:** o **.NET 8 e o .NET 9 saíram de suporte em 10/11/2026**. Os pacotes ASP.NET/EF estão em 8.0.8/8.0.11, com patches defasados mesmo dentro da linha 8.

### 2.2 Projetos e camadas

- **Core**: entidades. Não referencia nenhum outro projeto.
- **Util**: não referencia nenhum outro projeto.
- **Database**: `AppDbContext`. Referencia Core e Util.
- **Business** (DTOs, interfaces, regras): referencia Core e **Database**. É uma inversão de dependência: a camada de negócio conhece o DbContext.
- **Data**: migrations SQL Server.
- **Infrastructure**: JWT, segurança e migrations Postgres.
- **Infrastructure.Services**: serviços e integrações.
- **Application**.
- **Observability**.
- **Hosts:** API, Worker (sem HTTP), Orquestrador (não está no compose de produção) e WebUi.
- **SharedUi** (RCL), usada pela WebUi e pelo Mobile. Também referencia **Database**, outra inversão.
- **Tests**: xUnit com 176 arquivos. **E2E**: Playwright.

### 2.3 Pontos de entrada

- **HTTP:** cerca de 70 controllers em `Ofertio.API/Controllers` (inventário em 2.7).
- **Minimal APIs:** Orquestrador `/health` (`Ofertio.Orquestrador/Program.cs:263`); WebUi `/auth/*`, `/integracoes/oauth/*`, `/master/emular/*`.
- **Webhooks:** Asaas, Shopee, TikTok Shop, Telegram, WhatsApp (Evolution), redes sociais, Mercado Livre.
- **Jobs:** 14 workers em `Ofertio.API/Workers` (billing, purge, mineração de ofertas, broadcasts Telegram/WhatsApp, monitor do userbot etc.).
- **Uploads:** avatar (`PublicPageProfileController.cs:28-40`) e mídia gerada no servidor a partir de URL (`OfferMediaStudio.cs`).

### 2.4 Autenticação e autorização

**API**
- JWT Bearer HMAC com validade de 8h e 1h para emulação (`Ofertio.Infrastructure/Security/JwtTokenService.cs:13-14`).
- Não há refresh token, revogação nem security stamp.
- **Não existe FallbackPolicy:** endpoint sem atributo é público.
- Papéis Master usam as policies `master:*` (`Security/MasterAuthorizationExtensions.cs:10-17`).
- O plano é controlado por `[RequiresAccess]` e `[RadarPlanGate]`.

**Isolamento de inquilino**
- A claim `Ofertio:tenant_id` (`CurrentTenantService.cs:17`) alimenta um `HasQueryFilter` global (`AppDbContext.cs:961-1016`).
- O filtro é **fail-open**: quando o tenant é nulo, a consulta enxerga todos os inquilinos (`:997-1000`).

**WebUi**
- Cookie de autenticação com o JWT da API e o refresh token guardados como claims (`AuthCookieService.cs:109,116`).
- Usa antiforgery (`Ofertio.WebUi/Program.cs:63`).

### 2.5 Configuração e segredos

- **Arquivos versionados:** `appsettings*.json` (API, WebUi, Orquestrador, Worker, Mobile), `docker-compose*.yml`, `Caddyfile*`, `.env.example` e `comandos.txt`.
- **Produção:** o `.env` na VPS (não versionado: `.gitignore:14`) entra no compose via `${...}` (`docker-compose.yml:69-91`).
- **Lacunas do compose:** ele **não** sobrescreve `Integrations__Webhook__Secret` nem `EvolutionApi__ApiKey`, e nada da WebUi relacionado ao JWT. Detalhes na seção 5.

### 2.6 Integrações externas e deploy

**Integrações**
- **Pagamento:** Asaas.
- **Marketplaces:** Shopee, TikTok Shop, Mercado Livre, Amazon PA-API.
- **Mensageria:** Telegram (Bot + WTelegramClient), WhatsApp (Evolution API).
- **IA:** Gemini e OpenAI.
- **Outros:** e-mail SMTP Gmail, Firebase FCM, MaxMind GeoIP, CEP, Microsoft Graph (Orquestrador).
- Todas usam `IHttpClientFactory`; não há `new HttpClient(` fora dos testes.

**Deploy**
- `docker-compose.yml` com api, webui, mssql, rabbitmq e caddy. Só o Caddy publica as portas 80/443 (`docker-compose.yml:129-131`).
- Dockerfiles multi-stage **rodando como root** e com verificação de assinatura NuGet desligada (`Ofertio.API/Dockerfile:2,17`; `nuget.config:9`).
- **Não há CI/CD:** `.github/workflows` está vazio.
- Homologação usa túnel Cloudflare com `Caddyfile.hml`.

### 2.7 Inventário de endpoints (Ofertio.API)

Legenda:
- **Anon** = sem autenticação.
- **JWT** = `[Authorize]` (qualquer usuário logado).
- **T** = isolado só pelo filtro de inquilino.
- **U** = o serviço filtra por UserId.

| Rota | Método | Auth | Política | Quem acessa | Dados sensíveis |
|---|---|---|---|---|---|
| **api/Usuarios**, `{id}` (`UserAccountsController.cs:12-113`) | GET/POST/PUT/DELETE | **Anon (sem atributo)** | nenhuma | **qualquer pessoa** | **entidade inteira: PasswordHash, tokens TikTok, e-mail, telefone** |
| api/Usuarios/login (`:115-116`) | POST | Anon | – | público | credenciais / JWT |
| **WeatherForecast** (`WeatherForecastController.cs:5-21`) | GET | **Anon** | – | público | nada (template) |
| api/Test (`TestController.cs:7-14`) | GET | JWT | – | logado | nada |
| api/auth: tiktok/login, shopee/login, password-reset/request e /confirm, register (`AuthController.cs:31-145`) | POST | Anon | – | público | credenciais |
| api/auth: providers/vincular e /desvincular, change-password, state, status | vários | JWT | – | logado | conta |
| api/marketplace-auth: authorize-url, `{plataforma}`/authorize-url, mercadolivre/callback, oauth/sealed-callback | GET/POST | Anon | – | público | OAuth code |
| **api/marketplace-auth/vincular-manual** (`MarketplaceAuthController.cs:428-442`) | POST | **Anon** | – | **qualquer pessoa** | grava loja no **UserId de fallback** |
| api/marketplace-auth: lojas (CRUD), expand-affiliate-url, callback, **token** (`:516-524`) | vários | JWT | – | U/T | `token` devolve o **access token do marketplace** |
| api/mercadolivre: callback, notifications (`MercadoLivreController.cs:15,82-99`) | GET/POST | Anon | – | público | só grava log |
| api/public-converter/`{slug}` e /convert | GET/POST | Anon | – | público | gera link de afiliado sem rate limit |
| api/public/plans, api/legal/active | GET | Anon | – | público | preços / termos |
| api/legal: acceptance(s) | GET/POST | JWT | – | logado | IP de aceite |
| /j/`{id}`, /r/`{code}`, /`{slug}`/`{code}`, /`{ch}`/`{slug}`/`{code}` | GET | Anon | – | público | redirect (destino salvo no banco) |
| api/webhooks/asaas | POST | Anon + token no header | – | Asaas | pagamentos |
| api/webhooks/shopee, /tiktokshop | POST | Anon + HMAC | – | marketplace | pedidos |
| **api/webhooks/social-media/comments** (`SocialMediaWebhookController.cs:10-56`) | POST | **Anon, sem validação** | – | **qualquer pessoa** | enfileira evento com `OwnerId` vindo do cliente |
| api/v1/integrations/telegram/webhook(/`{userId}`), /whatsapp/webhook | POST | Anon + `[WebhookSecurity]` | segredo global | Telegram/Evolution | mensagens |
| api/Afiliados, api/Campanhas, api/Conteudos (CRUD `{id:int}`) | CRUD | JWT | – | T (dono checado só no PUT) | – |
| api/Produtos (CRUD, `{id}`/otimizar), api/estoque/*, api/Publicacao/`{produtoId}`/publicar-tiktok | vários | JWT | – | T | – |
| api/vendas/clientes, api/vendas/pedidos (+status, faturar) | vários | JWT | – | T | CPF/CNPJ, endereço |
| api/integracoes (lojas, whatsapp/*, telegram/*, grupos/monitorados/*) | vários | JWT | – | U; `midia/vincular-disco` é global (`MonitoredGroupsController.cs:137`) | bot token, sessão do userbot |
| api/radar/* | vários | JWT | `[RadarPlanGate]` + `RequiresAccess` (arbitragem e comparar-precos sem `RequiresAccess`) | plano | – |
| api/busca | GET | JWT | `RequiresAccess(OfferSearch)` | plano | – |
| api/achadinhos: GET, `{id}`/desbloquear, **creditos/pacote** | GET/POST | JWT | – | U | **créditos sem pagamento** |
| api/affiliate-links, api/afiliados/links, api/links/convert, api/afiliados/pagina-publica/*, api/afiliados/paginas-publicas | vários | JWT | – | U | – |
| api/inteligencia/* (inclui midia/moldura e midia/video) | vários | JWT | – | logado | **SSRF** |
| api/conta: dados, plano, plano/pagamento(/`{id}`) | vários | JWT (isento do guard) | – | U | dados de cartão (só last4 é gravado) |
| api/painel, api/sistema/*, api/Dashboard, api/Metricas, api/Orquestrador/status-pipeline, api/pre-flight, api/localidades, api/devices, api/propaganda/produtos, api/marketplace-orders/poll, api/marketplace-products/sync, api/TikTokAuth/callback | vários | JWT | – | T/U | – |
| api/master/* (`MasterControllerBase.cs:10-12`) | vários | JWT + papel Master | `master:*` por ação (`me`, `permissoes`, `emular/encerrar` e `GET planos` exigem só o papel) | operadores Master | todos os inquilinos, chaves globais |
| /swagger, /metrics, /uploads | GET | Anon | – | público na API; o Caddy só repassa `/api/*` (`Caddyfile:3-5`) | documentação, métricas, arquivos |

**Outros hosts**
- **Orquestrador:** `/health` e `api/saude/dashboard` anônimos; `api/Usuarios` POST com `[AllowAnonymous]` (`Ofertio.Orquestrador/Controllers/UserAccountsController.cs:26-27`). Não está no compose de produção.
- **Worker:** não expõe HTTP.

---

## 3. Matriz do checklist

### 3.1 Fase 1: Segurança

| ID | Item | Status | Evidência (arquivo:linha) / como verifiquei | Sev. | Esf. |
|---|---|---|---|---|---|
| S1 | Autenticação sem endpoints sensíveis anônimos | **ERRADO** | Não há FallbackPolicy (`MasterAuthorizationExtensions.cs:10-17`). `UserAccountsController.cs:12-14` está sem `[Authorize]`, e `TenantAccessGuardMiddleware.cs:37` deixa anônimos passarem. `vincular-manual` usa `[AllowAnonymous]` (`MarketplaceAuthController.cs:429`). Verificado lendo os controllers e o pipeline (`Program.cs:197-203`). | Crítica | P |
| S2 | Hash forte de senha | OK | BCrypt (`OfertioPasswordHasher.cs:10-23`). Busquei MD5/SHA aplicados a senha e não há. | – | – |
| S3 | Política de senha, lockout e força bruta | PARCIAL | A política existe (`IdentityRegistrationExtensions.cs:22-27`). **Não há lockout:** `PlatformAuthService.cs:386` usa `CheckPasswordAsync` sem `AccessFailedAsync`. **Não há rate limit:** a busca por `AddRateLimiter` não encontrou nada. Usuário inexistente retorna antes do BCrypt (`:381-383`), o que permite enumerar e-mails pelo tempo de resposta. | Alta | M |
| S4 | JWT e cookies | PARCIAL | Issuer, audience, assinatura e lifetime são validados (`Program.cs:108-133`), mas `ValidAudiences` sempre aceita também `OfertioClient` (`:129`). **A chave está versionada** (S21). Validade de 8h, **sem refresh e sem revogação** (`JwtTokenService.cs:13`; `PlatformAuthService.cs:463-472`). O cookie da WebUi usa o padrão (HttpOnly/Lax/SameAsRequest; `AuthenticationExtensions.cs:41-45`). A WebUi não tem DataProtection persistente. | Alta | M |
| S5 | Links de reset e confirmação | OK | O token tem 32 bytes, é guardado só como hash SHA256, expira em 30 min e é de uso único; a resposta não revela se o e-mail existe (`PasswordResetService.cs:49-124`; `TransactionalEmailRules.cs:12-31`). **Porém o cadastro revela** o e-mail: `PlatformAuthService.cs:279`. | Baixa | P |
| S6 | Rotas de admin restritas no servidor | OK | `MasterControllerBase.cs:10-12` usa `[Authorize(Roles=AnyMaster)]` mais policies por ação. A WebUi também tem `MasterRouteGuard`. Ressalva: 4 ações exigem só o papel (`MasterAccessController.cs:27,41,107`; `MasterSettingsController.cs:77`). | Baixa | P |
| S7 | IDOR / acesso por objeto | **ERRADO** | `api/Usuarios/{id}` é anônimo e sem filtro. Em Afiliados, Campanhas, Conteúdos e Produtos, o GET/DELETE `{id}` filtra só pelo inquilino; o dono é checado apenas no PUT (`AffiliatesController.cs:47-57,127-138` vs `:80`). Um inquilino pode ter vários usuários (`TenantProvisioningService.cs:109-117`). `marketplace-auth/token` devolve o access token (`MarketplaceAuthController.cs:516-524`). Verifiquei uma amostra de 16 endpoints (tabela no item A7). | Crítica | M |
| S8 | Mass assignment / overposting | PARCIAL | Nenhuma entidade é recebida via `[FromBody]`; todos os tipos são DTOs. Mas `UserAccountDto.Plan` é gravado vindo do cliente (`UserAccountsController.cs:75,100`) e as **respostas** devolvem entidades inteiras com o hash (`UserAccountResponseDto.cs:9-10`). | Alta | P |
| S9 | Regras de negócio críticas no servidor | **ERRADO** | O preço do plano vem do banco (`MyPlanService.cs:112-119`), o que está OK. Mas **créditos são somados sem pagamento** (`DealCreditsService.cs:152-174`). | Crítica | P |
| S10 | Validação de entrada no servidor | PARCIAL | Há `ValidationHelper` e DataAnnotations nos DTOs, mas sem padrão único. URLs são validadas só pelo esquema (ver S14). | Média | M |
| S11 | Consultas parametrizadas | OK | Todos os `ExecuteSqlRaw`/`SqlQuery` são literais ou parametrizados (`GroupCapturedMessagesService.cs:195-497`; `DataPurgeService.cs:47-67`; `LegacyCredentialReEncryptService.cs:32,88-97`). Não há Dapper nem `FromSqlRaw`. | – | – |
| S12 | XSS | OK | Não há `Html.Raw` nem `eval`. O único `MarkupString` com conteúdo dinâmico passa por sanitizador de allowlist (`TermsDocumentRules.cs:127-224`). Esse sanitizador é por regex; vale migrar para `HtmlSanitizer`. | Baixa | P |
| S13 | Upload de arquivos | PARCIAL | Avatar OK: magic bytes, limite de 2MB, nome por GUID, `CreateNew` (`AvatarImageRules.cs:15-56`). Porém o storage padrão fica **dentro do wwwroot** (`UploadStorageRules.cs:10-25`), e o vídeo baixado de URL externa é gravado **sem validar tipo nem tamanho** (`OfferMediaStudio.cs:110-114`). | Média | M |
| S14 | Path traversal, SSRF, desserialização, comando, open redirect | **ERRADO** (SSRF) | **SSRF:** `OfferMediaStudio.cs:44,55,103,110,129-139` (sem bloqueio de IP interno/metadata e sem limite de tamanho). Resolvedores de URL seguem redirects sem bloqueio de IP privado (`UrlResolverService.cs:32-98`; `UrlExpansionService.cs:30-190`; `ProductMetadataService.cs:365-386`). Path traversal OK (`ProductImageStorageRules.cs:299-306`). Não há `Process.Start` nem `BinaryFormatter`. Redirects OK (`JoinLinkRules.cs:43-94`; `PlatformLinkResolver.cs:63-73`). | Alta | M |
| S15 | Rate limiting | **AUSENTE** | Busquei `AddRateLimiter`, `EnableRateLimiting` e `UseRateLimiter` na solução: nada. | Alta | P |
| S16 | CORS restrito | **ERRADO** | `AllowAnyOrigin/AnyMethod/AnyHeader` (`Program.cs:65,198`). Não usa credenciais (bearer), o que reduz o impacto. | Média | P |
| S17 | CSRF onde há cookie | OK | A WebUi usa `UseAntiforgery` (`Ofertio.WebUi/Program.cs:63`). A API usa bearer, então não se aplica. | – | – |
| S18 | Cabeçalhos de segurança | **AUSENTE** (API/Caddy) / PARCIAL (WebUi) | API: sem HSTS, CSP, nosniff ou X-Frame-Options; HTTPS redirect só em dev (`Program.cs:169-172`). `Caddyfile:2-10` não tem diretiva `header`. WebUi: `UseHsts` (`Program.cs:51-55`), sem CSP. | Média | P |
| S19 | Webhooks | **ERRADO** | Asaas: token comparado em tempo constante (`AsaasWebhookService.cs:47-58`), mas a **renovação não é idempotente** (`:105-115`) e o `Amount` vem do payload (`:82`). SocialMedia **sem autenticação** (`SocialMediaWebhookController.cs:10`). Telegram/WhatsApp usam segredo global versionado, sem assinatura do corpo (`WebhookSecurityAttribute.cs:24-55`), e o `{userId}` da rota é aceito como verdade (`TelegramMessageProcessor.cs:61-78`). Shopee/TikTok com HMAC OK (`MarketplaceWebhookSignatureVerifier.cs:22-101`). | Alta | M |
| S20 | Erros sem detalhes em produção | OK (ressalva) | ProblemDetails e middleware próprio (`ExceptionHandlingMiddleware.cs`), sem DeveloperExceptionPage. `exception.Message` é exposto em Homologacao (`Program.cs:149-152`). | Baixa | P |
| S21 | Segredos fora do código | **ERRADO** | Jwt:Key, Encryption:Key, EvolutionApi:ApiKey, segredo de webhook, chave OpenAI e senha do Postgres estão versionados (seção 5). Os valores do `.env` local são **iguais** aos versionados (comparação booleana, sem exibir valores). | Crítica | M |
| S22 | Segredos no histórico do git | **ERRADO** | `git log -p --all` com regex (sem gitleaks): commits `31c658f` e `26f62ba` contêm as mesmas chaves, mais a senha do Postgres e a PartnerKey da Shopee (sandbox). Lista na seção 5. | Crítica | M |
| S23 | `.gitignore` | PARCIAL | Cobre `bin/`, `obj/`, `.env` e `.env.local` (`.gitignore:1-16`). **Não cobre** `secrets.json`, `appsettings.*.local.json` (fora do E2E), `.env.*`, `*.pem`/`*.key` nem `test_*.txt` (que estão versionados). | Baixa | P |
| S24 | Dependências vulneráveis/desatualizadas | **ERRADO** | `dotnet list package --vulnerable`: **SixLabors.ImageSharp 3.1.11** (direto, 4 High e 2 Moderate) em `Ofertio.Infrastructure.Services`. Transitivos: Microsoft.Extensions.Caching.Memory 8.0.0 (High), System.Formats.Asn1 5.0.0 (High), Microsoft.OpenApi 2.4.1 (High), Azure.Identity 1.10.3, Microsoft.AspNetCore.WebUtilities 2.3.9, OpenTelemetry 1.11.2, Microsoft.Identity.Client 4.56.0 (Moderate/Low). **.NET 8 e 9 fora de suporte desde 10/11/2026.** `--outdated`: ASP.NET/EF 8.0.8 (último 8.0.x mais novo); Telegram.Bot 19 vs 22; MassTransit 8.3 vs 9.3. `npm audit`: N/A (não há package.json). | Alta | M |
| S25 | Pacotes sem uso | PARCIAL | `HttpClientFactory` 1.0.5 (pacote de terceiros; `Ofertio.API.csproj:19`) e `Microsoft.AspNetCore.Http` 2.3.10 (`Ofertio.Core.csproj:20`): busquei usos nos `.cs` e não há. `WeatherForecastController` e `TestController` são código morto. | Baixa | P |
| S26 | Log de eventos de segurança | PARCIAL | Existe auditoria do Master (`MasterAuditService.cs`; `MasterAuditLog.cs`). **Falhas de login não são registradas** (`PlatformAuthService.cs:381-389`), nem acesso negado. **Vazam segredos e dados pessoais nos logs:** URL completa com `access_token` e `sign` da Shopee (`ExternalApiLoggingHandler.cs:23-51`, ligado por padrão em `appsettings.json:109,146`); corpos de OAuth/refresh (`MercadoLivreApiService.cs:1481`; `MarketplaceAuthService.cs:211,221,777`); e-mails (`PlatformAuthService.cs:425`; `GlobalIntegrationService.cs:182,327`). | Média | P |
| S27 | Alertas / detecção | **AUSENTE** | Há Prometheus/OTel e `SeqServerUrl` local (`appsettings.json:66`), mas não encontrei regras de alerta, Alertmanager nem notificação de atividade suspeita. | Média | M |
| S28 | LGPD básico | PARCIAL | Tokens, `PasswordHash` e `Document` são cifrados em repouso (`AppDbContext.cs:1041-1060`). Existe purga de dados operacionais (`DataPurgeService`). **Não encontrei fluxo de exclusão/anonimização de conta** a pedido do titular (busquei `DeleteAccount`, `ExcluirConta`, `Anonymi`). O vazamento do item 1 do resumo é incidente de dados pessoais. | Média | M |

### 3.2 Fase 2: Performance e escalabilidade

| ID | Item | Status | Evidência (arquivo:linha) | Sev. | Esf. |
|---|---|---|---|---|---|
| P1 | N+1 | PARCIAL | `ExecuteSqlRaw` por linha em `GroupCapturedMessagesService.cs:226-232,285-305,328-363`; `AnyAsync` dentro de loop em `OfferMiningEngine.cs:122,147` e `ProductArbitrageService.cs:242-244`; `FirstOrDefaultAsync` por item em `SalesOrderService.cs:71-73`; `SaveChanges` por inquilino em `TenantAdminService.cs:398-432`. Não há lazy loading. | Média | M |
| P2 | Projeção para DTO | PARCIAL | Vários endpoints devolvem entidades inteiras (Usuarios, Products, Affiliates, SalesOrders, Customers). | Média | M |
| P3 | `AsNoTracking` em leituras | PARCIAL | 193 usos em 61 arquivos, mas 11 dos 16 repositórios em `Infrastructure.Services/Repositories` não usam nenhum (ex.: `ProductRepository`, `CampaignRepository`, `SalesOrderRepository`). | Baixa | P |
| P4 | Índices | PARCIAL | Cerca de 20 índices compostos começando por `TenantId` (`AppDbContext.cs:118-947`). **Faltam** índices em `Tenants.GatewaySubscriptionId` e `GatewayCustomerId`, usados no webhook (`AsaasWebhookService.cs:222-232`; `AppDbContext.cs:120-121`), e em `GroupCapturedMessages.CreatedAt`, usado no purge (`DataPurgeService.cs:50`). | Baixa | P |
| P5 | Paginação | PARCIAL | Sem paginação: `GET api/Usuarios` (`UserAccountsController.cs:28-30`), `ProductRepository.cs:31,89-100`, `TenantAdminService.cs:79-86`, `MasterMetricsService.cs:30-36`, `GlobalTrendingDealsEngine.cs:99`. Exemplo positivo: `LinkAnalyticsService.cs:157,237`. | Média | M |
| P6 | async/await | OK | Nenhum `.Result`, `.Wait()` ou `GetAwaiter().GetResult()` em produção. `async void` só no Mobile (`AndroidPushInitializer.cs:8`; `MobilePushCoordinator.cs:34`). `CancellationToken` propagado nos controllers. | – | – |
| P7 | DbContext e HttpClient | OK | DbContext scoped. Sem `new HttpClient(` fora dos testes; tudo via `IHttpClientFactory`. Não há `EnableRetryOnFailure` (`RelationalDatabaseOptions.cs:28-37`). | Baixa | P |
| P8 | Cache | OK (proporcional) | `IMemoryCache` em `TenantAccessReader`, `MenuPermissionService`, `LinkAvailabilityService` e na busca do ML. Redis não se justifica no porte atual (N/A). | – | – |
| P9 | Timeout, retry e circuit breaker | PARCIAL | Polly em Shopee/TikTok (`IntegrationHttpClientRegistrationExtensions.cs:42-74`) e IA (`ExternalServiceRegistrationExtensions.cs:46-55`). Asaas, Evolution, ML e resolvedores de URL ficam sem política explícita (`WhatsAppEvolutionRegistrationExtensions.cs:83-126`; `MasterBackofficeRegistrationExtensions.cs:28`). | Média | P |
| P10 | Chamadas em paralelo | NÃO VERIFICÁVEL | Não identifiquei um gargalo sequencial evidente sem medir; não há métricas de latência por endpoint no repositório. | – | – |
| P11 | Chamadas duplicadas | NÃO VERIFICÁVEL | Exigiria rastrear em tempo de execução as telas Blazor; não há telemetria de cliente versionada. | – | – |
| P12 | Tarefas lentas em segundo plano | PARCIAL | 14 workers existem. Mas o **e-mail é enviado dentro da requisição** (`PasswordResetService.cs:77`; `TenantAdminService.cs:199`) e `TelegramUserMonitorHost.cs:21,28` usa canais **ilimitados** (risco de memória). | Média | M |
| P13 | Compressão | **AUSENTE** | Não há `AddResponseCompression` na API nem na WebUi. O Caddy não tem `encode` (`Caddyfile:2-10`). | Baixa | P |
| P14 | Streaming de IA | N/A | A IA (Gemini/OpenAI) roda em backend e workers para gerar conteúdo, não em chat interativo. | – | – |
| P15 | Loading/skeleton e atualização otimista | NÃO VERIFICÁVEL | Exigiria inspecionar todas as telas em execução; fora do escopo de leitura estática deste ciclo. | – | – |
| P16 | Debounce em busca | NÃO VERIFICÁVEL | Mesmo motivo do P15. | – | – |
| P17 | Imagens otimizadas e cache de estáticos | PARCIAL | Existe `IImageOptimizationService` (ImageSharp, vulnerável, ver S24). Não há cabeçalho de cache para `/uploads` (`Program.cs:186-192`) nem CDN. | Baixa | M |
| P18 | Divisão de JS | N/A | Blazor Server, sem bundler JS próprio. | – | – |
| P19 | Renderizações desnecessárias | NÃO VERIFICÁVEL | Exigiria profiling das telas Blazor. | – | – |
| P20 | Observabilidade | PARCIAL | Serilog + OpenTelemetry + Prometheus (`TelemetryRegistrationExtensions.cs:39`). **Não há health checks** na API nem na WebUi (só o Orquestrador tem `/health`). `/metrics` anônimo (mitigado pelo Caddy). | Média | P |
| P21 | Stateless / várias instâncias | PARCIAL (baixa prioridade) | Hoje roda 1 instância, o que é adequado. Impedimentos se for escalar: uploads em disco local, nonce OAuth em `IMemoryCache` (`MercadoLivreOAuthCallbackFlow.cs:147`), WebUi sem DataProtection compartilhada, billing sem lock distribuído (`TenantAdminService.cs:394-432`). **A falta de DataProtection persistente na WebUi já derruba as sessões a cada restart do container**, então esse ponto vale mesmo com 1 instância. | Média | P |

---

### 3.3 Fase 3: Riscos além dos checklists

| # | Risco | Evidência | Sev. |
|---|---|---|---|
| F1 | **Filtro de inquilino fail-open.** Qualquer caminho sem claim de inquilino (anônimo, worker, bug) enxerga todos os inquilinos. O teste trata isso como intencional. | `AppDbContext.cs:997-1000`; `TenantQueryFilterTests.cs:13` | Alta |
| F2 | **Usuário sem inquilino entra no inquilino ativo mais antigo.** Combinado com o cadastro anônimo, dá acesso aos dados de outro cliente. | `TenantProvisioningService.cs:109-117`, chamado por `UserAccountRepository.AddAsync` (`:60-64`) | Crítica |
| F3 | **Nenhum teste de integração do pipeline de autorização.** `Mvc.Testing` está referenciado, mas não há `WebApplicationFactory`. Os 176 arquivos de teste são unitários. Não há teste de `/api/Usuarios`, de SSRF, da corrida de créditos nem da idempotência do Asaas (o teste existente confere o e-mail, não o `RenewalAt`: `PlanPaymentEmailTests.cs:57-62`). | `Ofertio.Tests` | Alta |
| F4 | **Concorrência em saldo.** `UnlockAsync` lê e grava o saldo sem lock e sem token de concorrência; não há nenhum `RowVersion` no modelo. Estoque e pedidos usam `Serializable` com retry, o que está OK. | `DealCreditsService.cs:117-144`; `InventoryService.cs:362-406` | Média |
| F5 | **Backup de produção inexistente.** A própria checklist do projeto registra como pendente a rotina de backup do banco e das chaves de criptografia. | `docs/CHECKLIST_ATIVO.md:34` | Alta |
| F6 | **Rotação de chaves sem plano.** A checklist reconhece que trocar `ENCRYPTION_KEY` deixa ilegíveis os segredos gravados, e que `JWT_KEY`/`ENCRYPTION_KEY` precisam de plano próprio. Confirma que as chaves versionadas provavelmente estão em uso. | `docs/CHECKLIST_ATIVO.md:73`; `LegacyCredentialReEncryptService.cs` | Alta |
| F7 | **Plataforma fora de suporte.** .NET 8/9 sem patches de segurança desde 10/11/2026. | `*.csproj` (net8.0/net9.0) | Alta |
| F8 | **Containers como root e verificação de assinatura NuGet desligada.** Somam-se às imagens `:latest` sem versão fixa (mssql, rabbitmq, caddy). | `Ofertio.API/Dockerfile:2,17`; `nuget.config:9`; `docker-compose.yml` | Média |
| F9 | **Sem CI/CD.** Nenhuma barreira automática (build, testes, scan de segredos e dependências) antes do deploy. | `.github/workflows` vazio | Média |
| F10 | **Acoplamento de camadas.** Business e SharedUi referenciam Database (DbContext), o que dificulta testar as regras de negócio isoladamente. | `Ofertio.Business.csproj:11-12`; `Ofertio.SharedUi.csproj` | Baixa |
| F11 | **`DataPurgeService` usa T-SQL puro** (`DELETE TOP`, `DATEADD`, `ALTER INDEX`) e quebra no provider PostgreSQL, que também é suportado. | `DataPurgeService.cs:47-67` | Baixa |
| F12 | **`BackgroundServiceExceptionBehavior.Ignore`.** Um worker que falha morre em silêncio, e não há health check para perceber. | `Ofertio.API/Program.cs:90` | Média |
| F13 | **`UseForwardedHeaders` com `KnownNetworks`/`KnownProxies` limpos na WebUi.** Aceita X-Forwarded-* de qualquer origem; só não é explorável enquanto a WebUi estiver alcançável apenas pelo Caddy. | `Ofertio.WebUi/Program.cs:38-44` | Baixa |
| F14 | **Arquivos de lixo versionados.** `comandos.txt` (com segredos), `test_*.txt` e `temp_replace_orquestrador.ps1` estão no repositório. | `git ls-files` | Média |

---

## 4. Achados detalhados (por severidade)

### CRÍTICOS

#### A1. CRUD de usuários anônimo (`/api/Usuarios`)

**Descrição**
- `UserAccountsController` não tem `[Authorize]` e não existe FallbackPolicy.
- O `TenantAccessGuardMiddleware` deixa anônimos passarem (`:37`).
- O filtro de inquilino libera tudo quando o tenant é nulo (`AppDbContext.cs:997-1000`).
- A resposta é a entidade `UserAccount` inteira (`UserAccountResponseDto.cs:9-10`).

**Cenário de exploração**
1. `curl https://<dominio>/api/Usuarios` devolve todos os usuários de todos os inquilinos, com `PasswordHash` (BCrypt, sujeito a quebra offline), `TikTokShopAccessToken`, `TikTokRefreshToken`, e-mail e telefone.
2. `DELETE /api/Usuarios/{id}` apaga qualquer conta, e `PUT` troca e-mail e senha de qualquer conta, ou seja, tomada de conta.
3. `POST /api/Usuarios` cria um usuário (com o `Plan` escolhido pelo cliente), que cai no inquilino ativo mais antigo (`TenantProvisioningService.cs:109-117`). Depois, `POST /api/Usuarios/login` entrega um JWT válido para ler os dados desse inquilino.

**Evidência:** `Ofertio.API/Controllers/UserAccountsController.cs:12-113`; `Ofertio.Core/Entities/UserAccount.cs:22,51-52`.

**Correção recomendada**
1. FallbackPolicy global em `Program.cs`, tornando anônimo só o que tem `[AllowAnonymous]` explícito:
   ```csharp
   builder.Services.AddAuthorization(options =>
   {
       options.FallbackPolicy = new AuthorizationPolicyBuilder()
           .RequireAuthenticatedUser()
           .Build();
   });
   builder.Services.AddOfertioMasterAuthorization();
   ```
2. No `UserAccountsController`, usar `[Authorize(Roles = MasterRoles.AnyMaster)]` na classe (ou remover o controller: o cadastro e o login legítimos já existem em `api/auth`) e manter `[AllowAnonymous]` só no login, se ainda for usado.
3. Responder com DTO de saída sem `PasswordHash` nem tokens, e marcar esses campos com `[JsonIgnore]` na entidade como defesa extra.
4. Não aceitar `Plan` vindo do cliente.
5. Depois da correção, tratar como incidente: forçar a troca de senha de todos os usuários e revogar os tokens TikTok expostos.

**Como testar**
- Teste de integração com `WebApplicationFactory<Program>`: `GET /api/Usuarios` sem token deve retornar 401.
- Com token de usuário comum: 403.
- Com Master: 200, e o JSON não pode conter `passwordHash`.
- Teste de regressão que percorre todos os endpoints (`EndpointDataSource`) e falha se algum não tiver `[Authorize]` ou `[AllowAnonymous]` explícito.

#### A2. Tomada de conta via id_token Apple sem validação

**Descrição**
- `ValidateAppleIdToken` usa `ReadJwtToken`, que só decodifica. Não confere assinatura, `iss`, `aud` nem `exp`.
- O login social vincula automaticamente a conta que tem o mesmo e-mail (`PlatformAuthService.cs:421-433`).

**Cenário de exploração:** o atacante monta um JWT não assinado com `sub` qualquer e `email` da vítima, envia no login social Apple e recebe o JWT da vítima. Também funciona contra contas Master.

**Evidência:** `Ofertio.Infrastructure.Services/Security/SocialAuthTokenValidator.cs:189-215`.

**Correção recomendada**
- Validar com as chaves públicas da Apple:
  ```csharp
  private static readonly ConfigurationManager<OpenIdConnectConfiguration> AppleConfig = new(
      "https://appleid.apple.com/.well-known/openid-configuration",
      new OpenIdConnectConfigurationRetriever(),
      new HttpDocumentRetriever { RequireHttps = true });

  var config = await AppleConfig.GetConfigurationAsync(cancellationToken);
  var result = await new JsonWebTokenHandler().ValidateTokenAsync(idToken, new TokenValidationParameters
  {
      ValidIssuer = "https://appleid.apple.com",
      ValidAudiences = _options.AppleClientIds,   // Services ID / bundle id
      IssuerSigningKeys = config.SigningKeys,
      ValidateLifetime = true,
      ClockSkew = TimeSpan.FromMinutes(2)
  });
  if (!result.IsValid) return null;
  ```
- Exigir `email_verified == true` antes de qualquer vínculo automático por e-mail, em todos os provedores.
- **Facebook:** validar com `GET /debug_token` que o `app_id` é o do Ofertio (`SocialAuthTokenValidator.cs:157`).

**Como testar:** unitário com token forjado (assinado com chave própria ou com `alg: none`) deve retornar `null`; token com `aud` de outro app deve retornar `null`; token real de teste deve passar.

#### A3. Segredos de produção versionados

**Descrição:** chave JWT, chave de criptografia, segredo de webhook e ApiKey da Evolution estão em `appsettings*.json` versionados; chave OpenAI e client secret em `comandos.txt`. O compose de produção não sobrescreve o segredo de webhook nem a ApiKey da Evolution. O `.env` local usa os mesmos JWT/Encryption versionados.

**Cenário de exploração**
- Com a `Jwt:Key`, qualquer pessoa com acesso ao repositório (ou a um clone ou fork vazado) **forja um JWT com papel Master** e `tenant_id` arbitrário.
- Com a `Encryption:Key` e uma cópia do banco, decifra todos os tokens de marketplace e bot.
- Com o segredo de webhook, injeta mensagens falsas de Telegram/WhatsApp para qualquer usuário.

**Evidência:** seção 5.

**Correção recomendada**
1. Revogar e gerar novos valores (seção 5).
2. Remover todos os valores dos `appsettings*.json`, deixando `""`, e falhar no startup quando faltarem em produção (o padrão já existe para o JWT em `Program.cs:110`).
3. Acrescentar ao `docker-compose.yml`: `Integrations__Webhook__Secret: ${WEBHOOK_SECRET}`, `EvolutionApi__ApiKey: ${EVOLUTION_API_KEY}`, e na WebUi `Jwt__Key`.
4. Usar User Secrets em desenvolvimento.
5. Para a `Encryption:Key`, criar uma ferramenta de recifragem (chave antiga → nova, numa transação, com dry-run) cobrindo as ~25 colunas `ENC1:`. *Correção posterior:* o `LegacyCredentialReEncryptService` existente só cifra texto puro e não troca de chave. Passo a passo em [ROTEIRO_BACKUP_E_SEGREDOS.md](./ROTEIRO_BACKUP_E_SEGREDOS.md), item C5.
6. Só depois, limpar o histórico (`git filter-repo`) e apagar `comandos.txt`.

**Como testar:** `git grep -nE '"(Key|Secret|ApiKey)"\s*:\s*"[^"]{8,}"' -- '*.json'` não retorna nada; a API sobe em produção só com o `.env`; um JWT assinado com a chave antiga é recusado (401).

#### A4. Créditos sem pagamento

**Descrição:** `POST api/achadinhos/creditos/pacote` chama `TopUpAsync`, que faz `PurchasedBalance += packSize` sem nenhuma cobrança.

**Cenário de exploração:** um usuário logado repete a chamada e obtém créditos ilimitados.

**Evidência:** `DealIntelligenceController.cs:51-55`; `DealCreditsService.cs:152-174`.

**Correção recomendada:** o endpoint deve **criar uma cobrança** (mesmo fluxo de `MyPlanService.CheckoutAsync`, preço do pacote vindo do banco), e o crédito só deve entrar no webhook de pagamento confirmado. Até lá, restringir a Master (`[MasterAuthorize(AdjustCredits)]`) ou desativar.

**Como testar:** integração: usuário comum chamando `creditos/pacote` não altera `PurchasedBalance`; webhook `PAYMENT_CONFIRMED` do pacote credita exatamente uma vez.

#### A5. Renovação dupla no webhook Asaas

**Descrição:** o bloco que estende `RenewalAt` roda a cada `PAYMENT_CONFIRMED` ou `PAYMENT_RECEIVED`, mesmo quando `alreadyPaid` é verdadeiro.

**Cenário de exploração:** em pagamento com cartão, o Asaas envia os dois eventos, então 1 mês pago vira 2. Cada reenvio manual ou automático do Asaas soma mais um período. Isso é perda de receita direta.

**Evidência:** `AsaasWebhookService.cs:85,97-116`.

**Correção recomendada**
```csharp
case "PAYMENT_CONFIRMED":
case "PAYMENT_RECEIVED":
    if (alreadyPaid)
    {
        message = $"Pagamento {payment.Id} já processado.";
        break;               // idempotente: não estende de novo
    }
    noticeKind = "confirmed";
    row.PaidAt = ParseDate(payment.PaymentDate) ?? now;
    // ... ApplyTransition + extensão de RenewalAt (uma única vez)
```
Também convém conferir `payment.Value` contra o preço do plano antes de ativar.

**Como testar:** chamar o handler com `PAYMENT_CONFIRMED` e depois `PAYMENT_RECEIVED` do mesmo `payment.Id`: `RenewalAt` avança exatamente 1 ciclo. Repetir o mesmo evento 3 vezes também deve avançar 1 ciclo só.

### ALTOS

#### A6. `vincular-manual` anônimo e webhook de redes sociais sem autenticação

**Descrição**
- `POST api/marketplace-auth/vincular-manual` é `[AllowAnonymous]` e, sem claim, grava a loja no `HomologMarketplaceAuth.FallbackUserId` (`MarketplaceAuthController.cs:428-442`).
- `POST api/webhooks/social-media/comments` aceita qualquer payload e enfileira no RabbitMQ com o `OwnerId` vindo do cliente (`SocialMediaWebhookController.cs:10,50`).

**Impacto:** injeção de credenciais de loja na conta de fallback (que pode ser um usuário real em produção); disparo de respostas automáticas em nome de qualquer dono.

**Correção recomendada:** remover o `[AllowAnonymous]` e o fallback do `vincular-manual` (exigir JWT e nunca usar um UserId padrão em produção); no webhook social, exigir assinatura HMAC do provedor (ou `[WebhookSecurity]` com segredo próprio) e derivar o `OwnerId` do lado do servidor.

**Como testar:** 401 sem credencial nos dois endpoints.

#### A7. IDOR dentro do inquilino

**Descrição:** GET/DELETE `{id}` de Afiliados, Campanhas, Conteúdos e Produtos filtram só por inquilino; só o PUT confere o dono. `marketplace-auth/token` devolve o access token do marketplace.

**Amostra verificada (16):** Usuarios (sem filtro); Afiliados, Campanhas, Conteúdos, Produtos, Publicacao, Clientes, Pedidos, marketplace-auth/token (só inquilino); whatsapp/campanhas, pagina-publica/links, notificações, pre-flight, integracoes/lojas, affiliate-links/metadata, painel/produtos, conta/pagamento (filtram por usuário ou inquilino explícito, OK).

**Correção recomendada:** definir a regra de negócio (os usuários do mesmo inquilino podem ver os itens uns dos outros?). Se não puderem, aplicar o mesmo `item.UserId == userId` do PUT em GET e DELETE. O endpoint `token` não deve devolver o token ao cliente; as chamadas ao marketplace devem ser feitas pelo servidor.

**Como testar:** dois usuários no mesmo inquilino; o B tenta `GET/DELETE /api/Afiliados/{id do A}` e recebe 404.

#### A8. Força bruta: sem rate limit e sem lockout; JWT não revogável

**Correção recomendada**
```csharp
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("auth", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
});
app.UseRateLimiter();   // depois de UseRouting
// [EnableRateLimiting("auth")] em AuthController, UserAccounts/login, PublicConverter
```
- Habilitar o lockout do Identity (`AccessFailedAsync` / `IsLockedOutAsync`) no `PlatformAuthService`.
- Executar um hash fictício quando o usuário não existe, para igualar o tempo de resposta.
- Incluir uma claim `security_stamp` no JWT e validá-la em `OnTokenValidated` (com cache curto), para que troca ou reset de senha invalide os tokens antigos.
- Como o TLS termina no Caddy, configurar `ForwardedHeaders` com `KnownProxies` para o IP do cliente ser o real.

**Como testar:** 11 logins em 1 minuto: o 11º retorna 429; N senhas erradas bloqueiam a conta; um token emitido antes do reset de senha retorna 401.

#### A9. SSRF

**Descrição:** `OfferMediaStudio` e os resolvedores de URL fazem GET em URLs do usuário, seguindo redirects, sem bloquear IPs internos (127.0.0.0/8, 10/8, 172.16/12, 192.168/16, 169.254/16, ::1) e sem limite de tamanho.

**Impacto:** varredura da rede interna do Docker (mssql:1433, rabbitmq:15672 com guest), leitura de painéis internos, metadata de nuvem, e disco cheio via `/uploads/videos`.

**Correção recomendada:** um `SocketsHttpHandler.ConnectCallback` compartilhado que resolve o DNS e recusa IPs privados (isso protege inclusive contra redirects e DNS rebinding), registrado nos clientes nomeados usados por `OfferMediaStudio`, `UrlResolverService`, `UrlExpansionService` e `ProductMetadataService`. Somar `MaxResponseContentBufferSize`, `Timeout` curto, allowlist de hosts quando possível (CDNs dos marketplaces) e validação do content-type real do vídeo.

**Como testar:** unitário com `http://127.0.0.1`, `http://169.254.169.254` e `http://mssql:1433` deve falhar; um redirect 302 para IP interno também deve falhar.

#### A10. Dependências vulneráveis e plataforma fora de suporte

**Correção recomendada**
- Atualizar SixLabors.ImageSharp para a última 3.1.x corrigida (ou 4.x).
- Fixar versões corrigidas dos transitivos (Caching.Memory, System.Formats.Asn1, Microsoft.OpenApi) via `PackageReference` direto ou `Directory.Packages.props`.
- Planejar a migração para o **.NET 10 (LTS)**. O SDK 10.0.303 já está instalado na máquina.

**Como testar:** `dotnet list package --vulnerable --include-transitive` sem resultados; suíte de testes verde.

#### A11. Ausência de backup e de testes de integração de autorização

Ver F3 e F5. **Correção:** rotina diária de `BACKUP DATABASE` do SQL Server para fora da VPS, incluindo as chaves de DataProtection e o `.env`, com restauração testada; projeto de testes com `WebApplicationFactory` cobrindo autenticação, inquilino e webhooks.

### MÉDIOS (resumo, correção curta)

- **CORS:** `WithOrigins("https://ofertio.com.br", ...)` vindo de configuração, substituindo o `AllowAnyOrigin` (`Program.cs:65`).
- **Cabeçalhos:** no `Caddyfile`, um bloco `header { Strict-Transport-Security "max-age=31536000; includeSubDomains"; X-Content-Type-Options nosniff; X-Frame-Options DENY; Referrer-Policy strict-origin-when-cross-origin; -Server }`, mais uma CSP testada na WebUi.
- **Filtro de inquilino fail-closed:** trocar `CurrentTenantId == null` por bypass explícito (`TenantFilterBypass`) só para login e workers.
- **Logs:** mascarar a query string no `ExternalApiLoggingHandler` (`access_token`, `sign`) e remover os `Body={Body}` de OAuth.
- **Uploads:** configurar `UploadStorage:RootPath` fora do wwwroot e servir com `Content-Disposition`/`nosniff`.
- **Corrida no saldo:** `RowVersion` em `CreditWallet` ou `UPDATE ... WHERE Balance >= @custo` atômico.
- **WebUi:** `AddDataProtection().PersistKeysToFileSystem(volume)` com o mesmo `ApplicationName`.
- **Health checks:** `AddHealthChecks().AddDbContextCheck<AppDbContext>()` e `/health` na API e na WebUi, usados pelo compose (`healthcheck`).
- **E-mail:** enfileirar o envio (Channel limitado + `BackgroundService`).
- **Exclusão de conta (LGPD):** endpoint de exclusão e anonimização a pedido do titular.
- **Containers não-root** (`USER app` nas imagens aspnet:8.0+), tags fixas e verificação de assinatura NuGet ligada.
- **CI mínimo:** build, testes, `dotnet list package --vulnerable` e gitleaks no GitHub Actions.

### BAIXOS

- Remover `WeatherForecastController`, `TestController`, `HttpClientFactory` e `Microsoft.AspNetCore.Http` 2.x.
- Unificar a mensagem do cadastro para não revelar e-mail existente.
- Completar o `.gitignore`.
- Índices em `Tenants.GatewaySubscriptionId`/`GatewayCustomerId` e `GroupCapturedMessages.CreatedAt`.
- `AsNoTracking` nos repositórios de leitura; paginação nas listagens do Master e de Produtos.
- Compressão (`encode zstd gzip` no Caddy).
- Trocar o sanitizador por regex por `HtmlSanitizer`.

---

## 5. Segredos a revogar

Todos devem ser considerados **comprometidos**: estão no repositório remoto `github.com/.../TecFlow` (visibilidade NÃO VERIFICÁVEL sem consultar a API do GitHub) e no histórico.

| # | Segredo | Onde está (HEAD) | Histórico | Ação |
|---|---|---|---|---|
| 1 | Chave de API **OpenAI** `sk-p****` | `comandos.txt:155` | desde `31c658f` | **Revogar já** no painel da OpenAI e conferir o uso/faturamento |
| 2 | **Client secret do app TikTok for Developers** (client key `awmv****`) `nvPN****` | `comandos.txt:160` | desde `31c658f` (também em `TecFlow.API/appsettings.json`) | Gerar outro no portal do provedor |
| 3 | **Jwt:Key/Secret** `4F7A****` | `Ofertio.API/appsettings.json:77-78`, `.Development.json:27-28`, `.Homologacao.json:85-86`; `Ofertio.WebUi/appsettings*.json`; `Ofertio.Orquestrador/appsettings*.json` (inclui `.Production.json:13-14`) | desde `31c658f` | Gerar outra chave (64+ bytes aleatórios) e pôr só no `.env`. Derruba as sessões atuais, o que é aceitável |
| 4 | **Encryption:Key** `z4wT****` | `Ofertio.API/appsettings.json:22`, `.Homologacao.json:23`; `Ofertio.Orquestrador/appsettings.json:12`; `Ofertio.Worker/appsettings.json:12` | desde `31c658f` | Rotacionar com ferramenta nova de recifragem (o `LegacyCredentialReEncryptService` não troca de chave), com backup antes. Ver roteiro, item C5 |
| 5 | **Integrations:Webhook:Secret** `tecf****` | `Ofertio.API/appsettings.json:149`, `.Homologacao.json:75` | `26f62ba` | Novo segredo no `.env`, no compose e no `setWebhook` do Telegram e da Evolution |
| 6 | **EvolutionApi:ApiKey** `4233****` | `Ofertio.API/appsettings.json:174`, `.Development.json:38`, `.Homologacao.json:92` | desde `31c658f` | Trocar na Evolution e no `.env` |
| 7 | Senha **PostgreSQL** `tecb****` | `Ofertio.Orquestrador/appsettings.json:3`, `.Development.json:3`; `Ofertio.Worker/appsettings.json:3`; `scripts/setup-postgresql.ps1:31,41` | desde `31c658f` | Trocar se esse banco existir fora da máquina local |
| 8 | **Shopee PartnerKey** (sandbox) `tecf****` | `Ofertio.API/appsettings.json:129`, `.Homologacao.json:65` | `26f62ba` | Gerar outra se a chave for real |
| 9 | RabbitMQ `gues****` (padrão) | `Ofertio.API/appsettings.json:52` | – | Produção já sobrescreve, mas a checklist (`docs/CHECKLIST_ATIVO.md:73`) diz que ainda é `guest`: trocar |

**Depois de revogar:** limpar o histórico com `git filter-repo` (é preciso combinar com quem tiver clones), apagar `comandos.txt` e adicionar gitleaks ao CI.

---

## 6. Plano de ação priorizado

### Corrigir agora (crítico, antes de qualquer deploy)

| Bloco | O quê | Esf. |
|---|---|---|
| **A** | FallbackPolicy global + `/api/Usuarios` restrito a Master (ou removido) + DTO de saída sem hash/tokens + sem `Plan` vindo do cliente. Em seguida: forçar troca de senha e revogar tokens TikTok (incidente) | P |
| **B** | Validar id_token Apple (JWKS/iss/aud/exp) e token Facebook (`debug_token`/app_id); exigir `email_verified` no auto-vínculo | M |
| **C** | Revogar e rotacionar segredos (seção 5), limpar os `appsettings`, completar o `docker-compose.yml` (webhook, Evolution, JWT da WebUi) | M |
| **D** | `creditos/pacote`: exigir pagamento ou restringir a Master | P |
| **E** | Idempotência da renovação no webhook Asaas, conferindo o valor contra o plano | P |
| **F** | `vincular-manual` autenticado e sem fallback; webhook de redes sociais com assinatura | P |
| **G** | Backup do banco e das chaves antes da rotação do bloco C | P |

### Próximas 2 semanas

1. Rate limiter (auth, cadastro, reset, conversor público) + lockout + igualar tempo do login.
2. Revogação de JWT (security stamp) e DataProtection persistente na WebUi.
3. Guarda anti-SSRF nos clientes HTTP de URL do usuário + limite de tamanho do vídeo.
4. Atualizar ImageSharp e transitivos vulneráveis; iniciar a migração para .NET 10 LTS.
5. IDOR dentro do inquilino (GET/DELETE) e endpoint `marketplace-auth/token`.
6. Filtro de inquilino fail-closed.
7. CORS restrito + cabeçalhos de segurança no Caddy.
8. Tirar tokens e e-mails dos logs; registrar falhas de login e acessos negados.
9. Testes de integração com `WebApplicationFactory`: autorização de todos os endpoints, inquilino, webhooks e idempotência do Asaas.

### Backlog

- Health checks + alertas (Prometheus/Alertmanager ou Seq) + trocar `BackgroundServiceExceptionBehavior.Ignore`.
- Paginação, `AsNoTracking`, índices faltantes e N+1 apontados.
- Fila para e-mail; canais limitados no monitor do Telegram.
- `RowVersion`/update atômico nos créditos.
- CI (build, testes, scan de dependências e gitleaks); containers não-root; tags fixas.
- Exclusão/anonimização de conta (LGPD).
- Compressão; cache de estáticos; uploads fora do wwwroot.
- Limpeza de código morto, pacotes sem uso, `.gitignore`, arquivos de lixo; reduzir o acoplamento Business/SharedUi → Database.
- `git filter-repo` depois da rotação.

---

## 7. Itens N/A e não verificáveis

| ID | Status | Justificativa |
|---|---|---|
| S17 (API) | N/A | A API autentica por bearer token, não por cookie. A WebUi tem antiforgery. |
| S24 (npm audit) | N/A | Não há `package.json`; o JS é estático, sem dependências npm. |
| P8 (Redis) | N/A | Uma instância só, `IMemoryCache` basta no porte atual. |
| P14 | N/A | Não há chat de IA interativo; a IA roda no backend e nos workers. |
| P18 | N/A | Blazor Server, sem bundler JS. |
| P21 (multi-instância) | Baixa prioridade | Uma instância é adequada hoje; só o DataProtection da WebUi vale mesmo assim. |
| P10, P11 | NÃO VERIFICÁVEL | Exigem medição em tempo de execução (tracing por endpoint e por tela), e não houve execução nesta auditoria. |
| P15, P16, P19 | NÃO VERIFICÁVEL | Exigem inspeção das telas em execução ou profiling do Blazor; fora do escopo estático. |
| Valores em produção | NÃO VERIFICÁVEL | Não acessei a VPS. Não dá para afirmar que o `.env` de produção usa as chaves versionadas, mas o `.env` local usa, e `docs/CHECKLIST_ATIVO.md:73` indica que `JWT_KEY` e `ENCRYPTION_KEY` nunca foram rotacionadas. O segredo de webhook e a ApiKey da Evolution **certamente** vêm do `appsettings.json` versionado, porque o compose não os sobrescreve. |
| Visibilidade do repositório | NÃO VERIFICÁVEL | Exigiria consultar a API do GitHub (serviço externo). Mesmo que seja privado, os segredos estão em todos os clones. |
| Exposição de `/swagger` e `/metrics` | NÃO VERIFICÁVEL em produção | Pelo `Caddyfile:3-5`, só `/api/*` chega à API, então não ficariam públicos; não testei contra o domínio real. |
