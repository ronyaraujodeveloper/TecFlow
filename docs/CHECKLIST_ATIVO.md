# ✅ Checklist Ativo do Ofertio (pendências)

> Só o que ainda falta fazer ou validar. Ao concluir, marque `[x]` e mova o item para [`HISTORICO_FASES_CONCLUIDAS.md`](./HISTORICO_FASES_CONCLUIDAS.md). O detalhamento de cada entrega (código entre parênteses) está no histórico.

## 🛒 Mercado Livre: app, OAuth e homologação

- [ ] **Cadastro no app do ML (`MercadoLivreEndpoints`, `MercadoLivreOAuthPublicCallback`):** Redirect URI = `https://dev.ofertio.com.br/api/mercadolivre/callback` (idêntica a `Integrations:MercadoLivre:RedirectUri`) e notificações = `https://dev.ofertio.com.br/api/mercadolivre/notifications`.
- [ ] **Token Master (`GlobalIntegrationsMaster`):** autorizar em Master ➔ Integrações Globais ➔ API Mercado Livre e buscar em `/busca` com um cliente sem OAuth próprio.
- [ ] **Link com código MLB (`MercadoLivreProductIdPreservation`):** converter de novo `https://www.mercadolivre.com.br/p/MLB20621890?matt_tool=97303724&matt_word=97303724`; refazer links antigos gerados com `MLB1234567890`.
- [ ] **Taxas de comissão (`MercadoLivreCategoryCommission`):** comparar `AffiliateCommissionRules` com o painel de afiliado do ML (a venda indireta paga a metade).
- [ ] **Sua taxa do ML (`AffiliateOfferMetrics`):** salvar a taxa média no modal de regras da Buscar Ofertas.

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

## 📝 Registrado sem implementação

- **Autocomplete do ML como fallback (`MLBScraperPrimary`):** não gera ofertas (só termos) e responde 404/403 ao servidor; fora de escopo enquanto a busca for só pela API oficial.
