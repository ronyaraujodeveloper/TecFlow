# 🔑 Integrações, OAuth & Marketplaces

> Fluxo OAuth do Mercado Livre, token Master, busca pela API oficial, sanitização de links de afiliado e novas plataformas. Segurança dos tokens em repouso: [backend-architecture.md](./backend-architecture.md#6-segurança-criptografia-e-lgpd).

## 1. Limites inegociáveis

- **Só API oficial do Mercado Livre** com `Authorization: Bearer` (token no cabeçalho, nunca na URL). Proibido raspar `lista.mercadolivre.com.br` ou páginas de anúncio e proibido busca anônima.
- **Desafios anti-robô e captchas** (Mercado Livre `account-verification` / `bot_challenge`, captcha da Amazon) **não são contornados** por código; a resposta é tratada como inconclusiva e os GETs públicos ficam suspensos por 15 min (`MercadoLivreAntiBotGuard`).
- **Logs** nunca registram `client_secret`, `code`, `code_verifier`, `AccessToken` ou `RefreshToken`.
- **Segredos** (Client Secret, AppSecret, senhas) ficam no cofre Master/User Secrets, nunca em `appsettings` versionado, código ou documentação.

## 2. OAuth do Mercado Livre

- **Redirect URI fixa:** `Integrations:MercadoLivre:RedirectUri` = `https://dev.ofertio.com.br/api/mercadolivre/callback` (API, `appsettings.json` e `appsettings.Homologacao.json`); a URL de autorização e a troca do `code` usam exatamente esse valor, idêntico ao cadastrado no app do ML.
- **PKCE obrigatório:** `code_verifier` de 64 caracteres (`MercadoLivrePkce`) guardado criptografado no `state`; a URL de autorização leva `code_challenge` (SHA-256 base64url) com `code_challenge_method=S256`, e o callback envia o verifier no `POST oauth/token`.
- **State selado:** `OAuthStateProtector` (AES-256-GCM, chave HKDF de `Encryption:Key`) guarda `UserId`, `TenantId`, apelido, origem e verifier; validade de 15 min e uso único. Por isso o callback é anônimo.
- **Rotas (`MercadoLivreController`, `api/mercadolivre`, anônimo):**
  - `GET callback?code=&state=`: abre o state, troca o code, grava os tokens em `MarketplaceAccounts` e redireciona para `/marketplace-accounts?status=success|error` da origem.
  - `POST notifications`: responde `200 OK` imediatamente, mesmo com payload inválido.
  - `POST api/marketplace-auth/mercadolivre/callback` (usado pela WebUi) compartilha o fluxo `MercadoLivreOAuthCallbackFlow`.
- **Host público:** a WebUi repassa `/api/mercadolivre/callback` e `/api/mercadolivre/notifications` para a API via YARP (`MercadoLivreProxyExtensions`). Não criar subaplicação `/api` no site `Ofertio.WebUi`.
- **Diagnóstico:** erro HTTP 400 na troca do code registra `error`, `message` e corpo do ML (`invalid_grant`, `redirect_uri_mismatch`...) com `redirect_uri` e `client_id`, sem segredos.
- **Ngrok:** `OAuthRedirectUriNormalizer` gera `https://{host}/integracoes/oauth/callback` sem porta, query ou fragmento.

## 3. Token Master e ordem dos tokens

- **Token Master (Integrações Globais):** autorizado pelo administrador em Master ➔ Integrações Globais ➔ API Mercado Livre (`/master/integracoes-globais/mercado-livre`) e salvo criptografado em `GlobalIntegrationTokens`.
- **Cliente final sem OAuth próprio:** basta o ID de afiliado/Tracking ID no card da loja (vira `matt_tool`/`matt_word` nos links); a busca usa o token Master.
- **Busca por palavra-chave:** token Master ➔ (recusa 401/403: `refresh_token` + nova tentativa) ➔ token OAuth da conta do tenant ➔ `Integrations:MercadoLivre:AccessToken` ➔ token do app (`client_credentials` com `AppId`/`SecretKey` do cofre).
- **Anúncio (`GetItemAsync`) e Gerador de Links:** conta OAuth do usuário logado ➔ `AccessToken` fixo ➔ token do app. Rotinas em segundo plano nunca usam a conta de outro tenant.
- **Refresh automático:** token expirado ou recusado dispara `grant_type=refresh_token`, grava o novo par na conta e repete a chamada uma vez.
- **Cache global de buscas:** consulta idêntica (texto + filtros + categoria) dentro do tempo configurado no Master (padrão 15 min, 0 desliga) não consome cota; cada cliente recebe o próprio `matt_tool`.

## 4. Busca de ofertas no Mercado Livre

- **Endpoint:** `GET /products/search?status=active&site_id=MLB&q=...` (o `/sites/MLB/search` responde 403 para apps de terceiros) ➔ `/products/{id}` para o preço: `buy_box_winner` ➔ faixa de preço ➔ `/products/{id}/items` ➔ variações (`children_ids`).
- **Varredura:** páginas de 20 até juntar a meta de ofertas ou 100 produtos analisados; sem ofertas, tenta termos mais curtos; "Carregar mais" continua do próximo offset.
- **Relevância:** a ordem da API é preservada; `/products/search` não aceita `sort` nem devolve `available_filters`.
- **Categorias:** chave = `domain_id` do produto de catálogo; nome em português pela categoria real do anúncio (`category_id` ➔ `/categories/{id}`, cache de 12 h) ou pelo `domain_discovery`. Categorias marcadas viram `domain_id` no `products/search` (até 3 por busca).
- **Timeout:** `Integrations:MercadoLivre:HttpTimeoutSeconds` (padrão 30 s, 15 a 120); produto lento é ignorado e página lenta mantém as ofertas já achadas.
- **Limite conhecido:** só aparecem produtos de catálogo com oferta ativa; anúncios fora do catálogo não são listados pela API oficial.

## 5. Sanitização de links e afiliado

- **`matt_tool` do Mercado Livre:** sempre o `TrackingId` / `AffiliateTrackingId` da conta ML ativa do tenant ou, sem conta, o oficial `97303724` (`MercadoLivreIntegrationOptions.OfficialAffiliateTrackingId`). `matt_tool`, `matt_tool_id`, `matt_word`, `matt_d2id` e `matt_tracing_id` de terceiros são removidos (`AffiliateUrlBuilderRules`), mantendo os demais parâmetros.
- **Vitrines de terceiros:** URLs com `/social/`, `affiliate-profile` ou `reco_client=home_affiliate-profile` viram a página direta do produto focado (`item_id` / `product_id` / `wid`); sem produto focado, a oferta fica `VitrineGenerica`.
- **Código MLB preservado:** catálogo (`/p/MLB...`, com ou sem o nome do produto) continua em `https://www.mercadolivre.com.br/.../p/MLB{id}?matt_tool=&matt_word=`. Anúncio que já está em `produto.mercadolivre.com.br/MLB-{id}-slug` permanece nesse endereço. Trocar o catálogo por `produto.mercadolivre.com.br/MLB-{id}` abre “página não existe”. Link sem código MLB é recusado com mensagem clara (nunca um ID fictício).
- **Encurtadores** (`meli.la`, `/sec/`, `amzn.to`, `a.co`, `kb.um`, `magalu.me`...) são expandidos só pelos cabeçalhos de redirect.
- **Link de rastreio Ofertio:** `{base}/{storeSlug}/{code}` (slug `^[A-Za-z0-9]{1,80}$`) ou `/r/{code}`, com base `NavigationManager.BaseUri`; a WebUi repassa para a API. A API consulta o código com `await` e devolve o 302 na hora; o clique entra numa fila limitada e um serviço em segundo plano grava no SQL Server.

## 6. Plataformas suportadas (Strategy Pattern)

| Plataforma | Estratégia | Parâmetro de afiliado | Status |
| :--- | :--- | :--- | :--- |
| Shopee | `ShopeeLinkStrategy` | br.shp.ee / app | 🟢 Homologação |
| TikTok Shop | `TikTokShopLinkStrategy` | `shop.tiktok.com/view/product/{id}?sub_id=` | 🟢 Homologação |
| Mercado Livre | `MercadoLivreLinkStrategy` | `matt_tool` + `matt_word` em `/MLB-{id}` e `/sec/` | 🟢 Concluído |
| Amazon | `AmazonLinkStrategy` | `tag` em `/dp/{ASIN}` | 🟢 Concluído |
| Kabum! | `KabumLinkStrategy` | `sub_id` + `utm_source=afiliado` | 🟢 Concluído |
| Casas Bahia | `CasasBahiaLinkStrategy` | `parceiro` + `sub_id` em `/p/{id}` | 🟢 Concluído |
| Magazine Luiza | `MagazineLuizaLinkStrategy` | Magazine Você `/{loja}/p/{id}/` ou `?parceiro=` | 🟢 Concluído |
| AliExpress | `AliExpressLinkStrategy` | em planejamento | ⏳ Backlog |

### Nova plataforma

1. Enum em `PlatformType.cs`; credenciais (`AppKey`, `AppSecret`, `TrackingId`) em `MarketplaceAccounts`.
2. Classe `NomePlataformaLinkStrategy` implementando `IPlatformLinkStrategy`: `CanHandle(string url)` por Regex, sanitização, injeção das tags de afiliado e encurtamento oficial.
3. Registro: `builder.Services.AddScoped<IPlatformLinkStrategy, NomePlataformaLinkStrategy>();`
4. Retorno no `ConvertLinkResponseDto`: `OriginalUrl`, `AffiliateUrl`, `ShortenedShopeeUrl` / `ShortenedPlatformUrl` e `OfertioTrackingUrl` (`/{storeSlug}/{code}`).

## 7. Telegram Client API (UserBot MTProto)

1. `App API ID` e `App API Hash` gerados pelo usuário em `https://my.telegram.org`.
2. Primeiro login por código (SMS/app do Telegram) gera a sessão em `C:\ProgramData\Ofertio\telegram-sessions`.
3. Depois disso a sessão permanece ativa sem novos logins.
