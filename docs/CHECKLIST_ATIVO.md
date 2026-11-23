# ✅ Checklist Ativo do Ofertio (pendências)

> Só o que ainda falta fazer ou validar. Ao concluir, marque `[x]` e mova o item para [`HISTORICO_FASES_CONCLUIDAS.md`](./HISTORICO_FASES_CONCLUIDAS.md). O detalhamento de cada entrega (código entre parênteses) está no histórico.

## 🤖 Testes pela tela (`RpaE2E`)

Fases 1 a 9 concluídas no [histórico](./HISTORICO_FASES_CONCLUIDAS.md). O plano está em [`PLANO_RPA.md`](./PLANO_RPA.md).

## 🛒 Mercado Livre: app, OAuth e homologação

- [ ] **Cadastro no app do ML (`MercadoLivreEndpoints`, `MercadoLivreOAuthPublicCallback`):** Redirect URI = `https://dev.ofertio.com.br/api/mercadolivre/callback` (idêntica a `Integrations:MercadoLivre:RedirectUri`) e notificações = `https://dev.ofertio.com.br/api/mercadolivre/notifications`.
- [ ] **Token Master (`GlobalIntegrationsMaster`):** autorizar em Master ➔ Integrações Globais ➔ API Mercado Livre e buscar em `/busca` com um cliente sem OAuth próprio.
- [ ] **Link com código MLB (`MercadoLivreProductIdPreservation`):** converter de novo `https://www.mercadolivre.com.br/p/MLB20621890?matt_tool=97303724&matt_word=97303724`; refazer links antigos gerados com `MLB1234567890`.
- [ ] **Taxas de comissão (`MercadoLivreCategoryCommission`):** comparar `AffiliateCommissionRules` com o painel de afiliado do ML (a venda indireta paga a metade).
- [ ] **Sua taxa do ML (`AffiliateOfferMetrics`):** salvar a taxa média no modal de regras da Buscar Ofertas.

## 🎵 TikTok Shop: App de Criador e Integrações Globais (`TikTokShopGlobalIntegration`)

- [ ] **Migration:** aplicar `AddGlobalIntegrationExternalAccount` em homologação (runbook `EXECUCAO_HOMOLOGACAO.md`) e, depois, em produção.
- [x] **Partner Center (desenvolvimento):** app "Ofertio" Custom, categoria Creator collaborations; Redirect URL = `https://dev.ofertio.com.br/integracoes/tiktok/callback`.
- [ ] **Escopos:** em Partner Center ➔ Manage API, habilitar os escopos `creator.*` de busca de produtos em colaboração aberta e de link de promoção (o link de criador não leva escopos na URL).
- [ ] **Cofre:** preencher `Integrations:TikTokShop:AppKey`, `AppSecret`, `ServiceId` e `RedirectUri` em Master ➔ Cofre de chaves; testar em Master ➔ Contas de teste / dev ("TikTok Shop (Criador)").
- [ ] **Token Master:** autorizar em Master ➔ Integrações Globais ➔ API TikTok Shop com uma conta de criador (conta de vendedor deve ser recusada) e conferir o nome da conta e a expiração na tela.
- [ ] **Busca:** em `/busca`, marcar só "TikTok Shop" e conferir ofertas, comissão e o selo do canal. Confirmar no log que a resposta real bate com os campos lidos (`sales_price`, `commission.rate`, `detail_link`); se o TikTok mudar o path, ajustar `AffiliateProductSearchPath` sem novo código.
- [ ] **Conta de criador do cliente:** em Minhas Lojas, "Conectar conta de criador TikTok" no card TikTok; converter um link e conferir o link de promoção oficial. Validar o path `AffiliatePromotionLinkPath` e o campo do link na resposta real.
- [ ] **Sem conta de criador:** converter um link TikTok numa loja só com o ID e conferir o aviso "Conecte sua conta de criador TikTok em Minhas Lojas para garantir a comissão".
- [ ] **Legado:** avaliar a remoção de `TikTokShopApiService` e `TikTokAuthController` (fluxo de vendedor antigo, sem uso no App de Criador).
- [ ] **Questionário de segurança (`LegalPages`):** usar as URLs de produção `https://ofertio.com.br/legal/seguranca`, `/legal/privacidade` e `/legal/termos` (o dev depende do PC ligado); país dos dados = Alemanha (Hetzner) e Brasil.
- [ ] **Deploy em produção (`EXECUCAO_PRODUCAO.md`):** antes de subir os containers, fazer backup do banco da VPS e aplicar as migrations `AddUserStateCode`, `AddBillingProfileAndCheckout`, `AddGlobalIntegrationExternalAccount` e `AddPasswordResetTokens` (script idempotente gerado com `dotnet ef migrations script 20261022014444_AddIndexedCatalogOffers --idempotent`); depois conferir `/legal/*` e o Mercado Livre em produção.

## ⚖️ Documentos legais (`LegalPages`)

- [ ] **Revisão jurídica:** pedir a um advogado a revisão dos Termos de Uso, da Política de Privacidade e da Política de Segurança.
- [ ] **Backup de produção:** criar a rotina de cópia do banco e das chaves de criptografia prometida na Política de Segurança (item 9) e testar uma restauração.

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
- [ ] **Fase 2: cobrança real (ASAAS):** decidir entre formulário próprio com tokenização do cartão ou página de pagamento do ASAAS; trocar o `SimulatedPlanPaymentGateway` e deixar o webhook trocar plano e ciclo do inquilino. O checkout simulado não envia e-mail. O webhook que já confirma pagamento (`PAYMENT_RECEIVED` / `PAYMENT_CONFIRMED`) manda o aviso de assinatura; vencimento e estorno também.
- [ ] **Migration da senha (`AddPasswordResetTokens`):** aplicar `20261111003931_AddPasswordResetTokens` em homologação e em produção. No `localhost\SQLEXPRESS` / `Ofertio` ela já foi aplicada.
- [x] **Envio de verdade (`Email`):** funcionando na homologação pelo Gmail (`smtp.gmail.com`, conta `ofertiotecso@gmail.com`, senha de app no Cofre). **Decisão de 23/11/2026:** o remetente é a própria conta `ofertiotecso@gmail.com` (nome "Ofertio"), e o Responder para é `contato@ofertio.com.br`. O código força isso para conta @gmail.com, porque o domínio tem DMARC `p=reject` e o Gmail assina como gmail.com. **Não** verificar `contato@` em "Enviar e-mail como" do Gmail. O remetente `contato@ofertio.com.br` fica para quando houver um provedor com DKIM do domínio (ex.: Brevo); só mudam as chaves do Cofre.
- [x] **Primeiro deploy de produção com o compose do git (23/11/2026):** VPS no commit `1417b20`, `.env` criado, chave do Data Protection no volume, 8 migrations aplicadas (até `20261119225813`), home 200, API 401, sem `CryptographicException`. O `/appsettings.json` público, que expunha o segredo do Google, foi removido.
- [ ] **Trocar os segredos de produção expostos no chat de 23/11/2026:** senha de root da VPS (`passwd`), segredo do Google OAuth (gerar outro no Google Cloud e atualizar `GOOGLE_CLIENT_SECRET` no `.env` da VPS e o local), senha do SA do SQL Server e do RabbitMQ (`guest`). `ENCRYPTION_KEY` e `JWT_KEY` exigem plano próprio: trocar a chave de criptografia deixa ilegíveis os segredos já gravados no banco. Depois, apagar `/root/ofertio-config-backup`, que contém senhas.
- [ ] **E-mail em produção:** preencher a seção E-mail do Cofre na VPS (servidor `smtp.gmail.com`, usuário, senha de app) e clicar em "Enviar e-mail de teste".
- [x] **Senhas do Gmail expostas no chat:** senha de app nova gerada e colada no Cofre (23/11/2026), envio confirmado no log.
- [ ] **"Fale conosco" (Cloudflare):** desligar **Email Address Obfuscation** (Scrape Shield) e purgar o cache. Ligado, ele troca o `mailto:` por `/cdn-cgi/l/email-protection` e o clique cai na página de bloqueio.
- [ ] **CEP automático:** preencher rua, bairro, cidade e UF pelo ViaCEP em `/meus-dados`.
- [ ] **Gestão da assinatura:** troca de plano com pró-rata, cancelamento e histórico de pedidos em Meu plano.

## 🎁 Teste grátis e página inicial (`TrialPlan`, `LandingPage`)

- [ ] **Migration:** aplicar `AddTrialPlan` em homologação (runbook `EXECUCAO_HOMOLOGACAO.md`) e, depois, em produção. Cria o plano Trial (14 dias, sem cartão, cai em Starter, limites do Pro) e as colunas `TrialDays`, `RequireCard`, `PostTrialPlanCode` e `TrialEndingNoticeSentAt`. Incluir no script idempotente de produção.
- [ ] **Master ➔ Planos:** conferir o cartão "Teste grátis" (dias, plano ao fim do teste, exigir cartão), a linha do Trial com preços travados e os menus do Trial em Menus por plano.
- [ ] **Cadastro:** criar conta nova e conferir tenant próprio em Trial com `TrialEndsAt` correto (antes, todo cadastro caía no "Tenant Principal"). Conferir o aviso no cadastro, o banner no topo e o aviso em Meu plano.
- [ ] **Aviso de fim do teste:** com `TrialEndsAt` a menos de 3 dias, rodar o ciclo (`TenantBillingWorker`) e conferir o e-mail (um só por conta).
- [ ] **Cartão no cadastro (depende do ASAAS, fase 2):** a opção "Exigir cartão" só prepara a regra (conta com assinatura vira plano pago ao fim do teste). A coleta real do cartão e a cobrança ainda não existem; não ligar em produção antes disso.
- [ ] **Termos:** revisar `docs/Legal/RASCUNHO_CLAUSULA_TESTE_GRATIS.md` com o jurídico, publicar nova versão em Master ➔ Termos e atualizar a Política de Privacidade (gateway como operador).
- [ ] **Página inicial (`/conheca`):** revisar textos, trocar as imagens provisórias em `Ofertio.SharedUi/wwwroot/images/landing/` por capturas reais, validar em celular e depois trocar a rota `/` (login hoje) pela landing, redirecionando logados para `/dashboard`.
- [ ] **Links após cancelar (LinkAvailability):** conta cancelada, suspensa ou pausada mantém links curtos e página pública por Billing:LinkGraceDaysAfterCancel dias (padrão 30, em Master ➔ Cofre) e depois responde 410 (link) e "página indisponível" (conversão), sem gravar clique e sem trocar o ID de afiliado. Validar em homologação cancelando um tenant de teste; revisar a cláusula nova em docs/Legal/RASCUNHO_CLAUSULA_TESTE_GRATIS.md com o jurídico.
- [ ] **KPIs do Master:** comparação com o mês anterior quando houver histórico (`MasterDashboardDto`).

## 📊 Painel de cliques e origem do envio (`ClickChannel`, `PainelAnalytics`)

- [ ] **Migration `AddClickChannelAndBot`:** já aplicada em homologação (reclassificou o histórico). Aplicar em produção e incluir no script idempotente.
- [ ] **Segredo da chave de grupo:** definir `Ofertio:ShortLinks:GroupKeySecret` por ambiente, fora do Git, **antes de divulgar** e nunca trocar depois (as chaves `?g=` antigas deixariam de ser resolvidas). Sem valor, usa um segredo interno fixo.
- [ ] **Disparo real:** mandar uma mensagem para um grupo de WhatsApp e outro de Telegram e conferir que todos os links curtos saem como `/w/…?g=` e `/t/…?g=` (sem JID/ChatId) e que o nome do grupo aparece no painel.
- [ ] **Painel (`/dashboard`):** validar no navegador, desktop e celular: barra de período, indicadores com variação, gráfico por origem, Top 10, detalhe do produto (`/painel/produto/{id}`), grupos e mapa de calor. A antiga tela de campanhas e métricas passou para `/campanhas` e `/metricas`.
- [ ] **Gerador de Links:** conferir o menu "Copiar para" (WhatsApp, Telegram, E-mail, Instagram, Outro) e que os botões de WhatsApp/Telegram já usam `/w/` e `/t/`.
- [ ] **Página pública:** converter um link em `/p/{slug}` e conferir que o link devolvido sai como `/p/{loja}/{código}` e que o clique entra como "Página pública".
- [ ] **Limite conhecido:** a tag `{COMMISSION}` dos disparos usa a URL direta de afiliado (`AffiliateUrl`), não o link curto, então esse link nunca é rastreado.
- [ ] **Robôs de prévia:** abrir `/w/…` com User-Agent `WhatsApp/2.23` e conferir que redireciona, mas não entra nos cliques (aparece em "aberturas automáticas").
- [ ] **Pendentes do plano:** saúde dos grupos (tabela `GroupDispatchLog`: cliques por envio, tendência e alerta de grupo esfriando), visita à página pública no funil, limite de histórico por plano, "mais vendidos" quando houver importação de vendas.

## 🌐 Página pública: foto, links de convite, funil e região (`PublicPageProfile`)

- [ ] **Migration `AddPublicPageProfileJoinLinksAndRegion`:** aplicada em homologação (tabelas `PublicPageProfiles`, `PublicPageJoinLinks`, `PublicPageEvents`, `SaasPlans.MaxJoinLinks`, região em `LinkClickLog`). Aplicar em produção e incluir no script idempotente.
- [ ] **Master ➔ Planos:** conferir a coluna "Links de convite" (padrão: Trial 2, Starter 3, Pro 10, Agency 30, Enterprise ilimitado).
- [ ] **Minhas Páginas Públicas:** enviar e trocar a foto; criar um link de convite escolhendo um grupo do WhatsApp (conta administradora) e outro do Telegram (bot administrador); testar o botão "Testar"; colar um link manual e ver o aviso de "não verificável". **Confirmar em homologação que a versão da Evolution API responde `group/inviteCode`.**
- [ ] **Página pública (`/p/{slug}`):** conferir foto, aviso de afiliado, "Abrir na loja", "Copiar link" e, só depois de converter, os botões de grupo. Validar no celular.
- [ ] **Botão `/j/{id}`:** abre o convite, conta a entrada e não aceita endereço livre; convite desativado mostra aviso; conta cancelada fora da carência responde 410.
- [ ] **Conferência dos grupos (`JoinLinkHealthWorker`):** roda a cada 6 h (processa só os vencidos há mais de 20 h). Simular um grupo removido e conferir: 1ª falha mantém ativo, 2ª desativa, avisa no sistema e por e-mail; WhatsApp desconectado deixa "não verificado".
- [ ] **Região por IP:** baixar a base GeoLite2-City (`.mmdb`, MaxMind, cadastro gratuito, atualização mensal) e configurar `GeoIp:DatabasePath` por ambiente. Sem o arquivo, a região fica em branco e nada quebra. Conferir também que o IP do visitante chega à API (cabeçalhos `CF-Connecting-IP`/`X-Forwarded-For`): antes os cliques gravavam `***`.
- [ ] **Retenção do IP mascarado:** `Privacy:IpRetentionDays` (padrão 90). Confirmar o prazo com o jurídico (Marco Civil, art. 15).
- [ ] **Termos e Política de Privacidade:** revisar o novo rascunho em `docs/Legal/RASCUNHO_CLAUSULA_TESTE_GRATIS.md` (seção "página pública, foto, grupos e dados de acesso") e publicar.
- [ ] **Painel:** conferir o funil "Sua página pública" (visitas, conversões, entradas em grupo) e "De onde vêm os cliques (região estimada)".

## 📝 Registrado sem implementação

- **Autocomplete do ML como fallback (`MLBScraperPrimary`):** não gera ofertas (só termos) e responde 404/403 ao servidor; fora de escopo enquanto a busca for só pela API oficial.
