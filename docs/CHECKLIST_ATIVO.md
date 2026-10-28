# ✅ Checklist Ativo do Ofertio (pendências)

> Só o que ainda falta fazer ou validar. Ao concluir, marque `[x]` e mova o item para [`HISTORICO_FASES_CONCLUIDAS.md`](./HISTORICO_FASES_CONCLUIDAS.md). O detalhamento de cada entrega (código entre parênteses) está no histórico.

## 🛒 Mercado Livre: app, OAuth e homologação

- [ ] **Cadastro no app do ML (`MercadoLivreEndpoints`, `MercadoLivreOAuthPublicCallback`):** Redirect URI = `https://dev.ofertio.com.br/api/mercadolivre/callback` (idêntica a `Integrations:MercadoLivre:RedirectUri`) e notificações = `https://dev.ofertio.com.br/api/mercadolivre/notifications`.
- [ ] **Token Master (`GlobalIntegrationsMaster`):** autorizar em Master ➔ Integrações Globais ➔ API Mercado Livre e buscar em `/busca` com um cliente sem OAuth próprio.
- [ ] **Link com código MLB (`MercadoLivreProductIdPreservation`):** converter de novo `https://www.mercadolivre.com.br/p/MLB20621890?matt_tool=97303724&matt_word=97303724`; refazer links antigos gerados com `MLB1234567890`.
- [ ] **Taxas de comissão (`MercadoLivreCategoryCommission`):** comparar `AffiliateCommissionRules` com o painel de afiliado do ML (a venda indireta paga a metade).
- [ ] **Sua taxa do ML (`AffiliateOfferMetrics`):** salvar a taxa média no modal de regras da Buscar Ofertas.

## 🎵 TikTok Shop: App de Criador e Integrações Globais (`TikTokShopGlobalIntegration`)

- [ ] **Migration:** aplicar `AddGlobalIntegrationExternalAccount` em homologação (runbook `EXECUCAO_HOMOLOGACAO.md`) e, depois, em produção.
- [ ] **Partner Center:** criar o app do tipo Criador (Affiliate Creator) com os escopos de busca de produtos em colaboração aberta e de link de promoção; Redirect URL = `https://dev.ofertio.com.br/integracoes/oauth/callback`.
- [ ] **Cofre:** preencher `Integrations:TikTokShop:AppKey`, `AppSecret`, `ServiceId` e `RedirectUri` em Master ➔ Cofre de chaves; testar em Master ➔ Contas de teste / dev ("TikTok Shop (Criador)").
- [ ] **Token Master:** autorizar em Master ➔ Integrações Globais ➔ API TikTok Shop com uma conta de criador (conta de vendedor deve ser recusada) e conferir o nome da conta e a expiração na tela.
- [ ] **Busca:** em `/busca`, marcar só "TikTok Shop" e conferir ofertas, comissão e o selo do canal. Confirmar no log que a resposta real bate com os campos lidos (`sales_price`, `commission.rate`, `detail_link`); se o TikTok mudar o path, ajustar `AffiliateProductSearchPath` sem novo código.
- [ ] **Conta de criador do cliente:** em Minhas Lojas, "Conectar conta de criador TikTok" no card TikTok; converter um link e conferir o link de promoção oficial. Validar o path `AffiliatePromotionLinkPath` e o campo do link na resposta real.
- [ ] **Sem conta de criador:** converter um link TikTok numa loja só com o ID e conferir o aviso "Conecte sua conta de criador TikTok em Minhas Lojas para garantir a comissão".
- [ ] **Legado:** avaliar a remoção de `TikTokShopApiService` e `TikTokAuthController` (fluxo de vendedor antigo, sem uso no App de Criador).

## 🔎 Buscar Ofertas: validação no navegador

- [ ] **Varredura (`MercadoLivreScanBudgetAndScreenFilters`):** "dell inspiron" + Notebooks deve mostrar "N ofertas · 100 produtos analisados".
- [ ] **Métricas (`AffiliateOfferMetrics`):** conferir no log `[ML Busca] Métricas:` vendedores, avaliações, ranking e vendas devolvidos.
- [ ] **Produtos principais (`MercadoLivreRelevanceAndMainProducts`):** "iphone 15" com e sem "Apenas produtos principais".
- [ ] **Modal dos selos (`CategoryChipsAndScoreBadgeModal`):** abrir Oportunidade, Vantagem e Segurança no desktop e no celular (as pílulas de categoria foram substituídas pelo combo).
- [ ] **Filtros compactos (`CompactSearchFilters`):** "notebook" com duas categorias; preço mínimo maior que o máximo deve ser recusado; conferir no celular.
- [ ] **Teclado dos combos (`CompactSearchFilters`):** "Categorias" com mais de 6 opções, setas, Espaço/Enter e Esc sem disparar a busca.
- [ ] **Combos centralizados (`CompactCombosAndMlTimeout`):** Ctrl+F5 na `/busca`, clicar fora para fechar e testar no celular.
- [ ] **Categorias reais (`RealCategoryFilter`):** "havaianas" com nomes em português; marcar uma categoria com 0 e clicar em Buscar.

## 🔐 Google OAuth em homologação

- [ ] **Console do Google (`GoogleOAuthHomologacao`):** aplicativo Web; origens `https://dev.ofertio.com.br` e `https://localhost:7002`; URIs de redirecionamento `https://dev.ofertio.com.br/signin-google` e `https://localhost:7002/signin-google`; escopos `openid`, `email` e `profile`. Depois de publicado, entrar em `https://dev.ofertio.com.br/` com "Entrar com Google".
- [ ] **Vincular na conta (`AccountSocialLink`):** em `/minha-conta`, "Vincular conta" no Google deve voltar com o badge Conectado. "Desvincular conta" sem senha e sem outro provedor deve mostrar o alerta e manter o vínculo.

## 📡 Radar híbrido e comparador multilojas

- [ ] **Homologar (`RadarHybridAndCompare`):** a migration `AddIndexedCatalogOffers` já foi aplicada em `localhost\SQLEXPRESS` / `Ofertio`. Depois de publicar, em `/busca` abrir "Comparar em outras lojas" num produto com código de modelo e conferir a linha de menor preço. No Radar, um termo do perfil sem oferta recente deve consultar as lojas oficiais.


## 🔗 Links, lojas e disparos

- [ ] **Slug numérico (`ShortLinkNumericSlug`):** abrir `https://dev.ofertio.com.br/97303724/xh8pmzh` e conferir o redirect para o Mercado Livre.
- [ ] **Editar loja (`StoreEditModal`):** editar apelido e ID em `https://dev.ofertio.com.br/minhas-lojas` e conferir que continua um único card.
- [ ] **Disparos automáticos (`DynamicTrackingLinkBaseUrl`):** mensagens montadas no servidor ainda usam `Ofertio:ShortLinks:PublicBaseUrl`; definir o domínio público por ambiente.

## 💳 Meus dados e Meu plano (`MyPlanCheckout`)

- [ ] **Migration:** aplicar `AddBillingProfileAndCheckout` em homologação (runbook `EXECUCAO_HOMOLOGACAO.md`) e, depois, em produção pelo fluxo de produção.
- [ ] **Fluxo no navegador:** menu Minha conta ➔ Meu plano; olho de um plano; Escolher com cadastro incompleto deve abrir o modal "Completar meus dados" e levar a `/meus-dados?plano=…`; salvar completo deve seguir para o pagamento; Anual com `4111 1111 1111 1111` deve terminar em "Pedido registrado em modo de teste". Conferir no celular.
- [ ] **Teste com a conta Master (`BillingOptions`):** `rony.araujo@gmail.com` está liberado só em homologação (`Billing:MasterCheckoutTestEmails` no `appsettings.Homologacao.json` da API). Remover a liberação ao terminar os testes; nunca preencher em produção.
- [ ] **Vantagens:** preencher "Vantagens (uma por linha)" em `/master/planos` para Starter, Pro, Agency e Enterprise.
- [ ] **Fase 2: cobrança real (ASAAS):** decidir entre formulário próprio com tokenização do cartão ou página de pagamento do ASAAS; trocar o `SimulatedPlanPaymentGateway` e deixar o webhook trocar plano e ciclo do inquilino.
- [ ] **CEP automático:** preencher rua, bairro, cidade e UF pelo ViaCEP em `/meus-dados`.
- [ ] **Gestão da assinatura:** troca de plano com pró-rata, cancelamento e histórico de pedidos em Meu plano.

## 📝 Registrado sem implementação

- **Autocomplete do ML como fallback (`MLBScraperPrimary`):** não gera ofertas (só termos) e responde 404/403 ao servidor; fora de escopo enquanto a busca for só pela API oficial.
