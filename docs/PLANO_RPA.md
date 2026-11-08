# Plano RPA — testes pela tela (`RpaE2E`)

> Robô Playwright para .NET em homologação: abre a página, entra com e-mail e senha e exercita os campos. A massa de casos (vírgula, letra, negativo, limite) fica no `Ofertio.Tests`. OAuth, sessão real de WhatsApp/Telegram e cobrança ASAAS ficam fora do robô.
>
> Pendência aberta: [`CHECKLIST_ATIVO.md`](./CHECKLIST_ATIVO.md). Ao concluir uma fase, o bloco vai para [`HISTORICO_FASES_CONCLUIDAS.md`](./HISTORICO_FASES_CONCLUIDAS.md).

### Regra permanente

As fases 1 a 9 estão fechadas. O que for novo continua entrando em `Ofertio.E2E` no mesmo passo do `Ofertio.Tests`, sem abrir fase nova:

- Tela nova no menu do cliente: visita em `MenuSmokeTests`, conferindo o `h1`. Tela nova no menu Master: a mesma visita, quando a sessão permanece em `/master`.
- Campo preenchível novo, inclusive checkbox: `id` estável `{tela}-{campo}` e `<label for>` igual, como em [`frontend-ui.md`](./rules/frontend-ui.md#10-ids-estáveis-para-testes-pela-tela). O robô preenche esse `id`, recusa o valor inválido e só grava o caminho válido com prefixo `E2E-`.
- Botão novo, inclusive o de aviso e o de modal: o robô clica e confere o que a tela mostra. Se o texto se repete, o botão ganha `id`. O confirmar de um aceite fica desabilitado até a caixa ser marcada.
- Aviso, modal e checkbox que travam o próximo passo entram no mesmo teste. O login dispensa cookies e reaceite quando eles aparecem.
- Publicação que vale para todo mundo grava um trecho `E2E-` e devolve o texto anterior antes de encerrar.
- OAuth, sessão real de WhatsApp/Telegram, QR, webhook e cobrança ASAAS continuam fora. O `RobotBoundaryTests` barra esses cliques.

### Fase 1: Ids estáveis para o robô (`RpaTestIds`)

- [x] **1.1. Convenção:**
  - Regra permanente na seção "Ids estáveis para testes pela tela" de [`docs/rules/frontend-ui.md`](./rules/frontend-ui.md). Todo campo preenchível ganha `id` kebab-case com prefixo da tela (`propaganda-preco`, `campaign-budget`). Linha repetida inclui a chave (`plan-monthly-starter`, `vault-{chave}`). `<label for>` usa o mesmo id. Botão de texto único permanece no nome acessível; botão repetido ganha `id`. Ids do item 1.2 não são renomeados.
- [x] **1.2. Inventário que já serve (não renomear):**
  - Conferidos e mantidos: `portal-email` / `portal-password`, `register-*`, `recovery-email`, `profile-*`, `current-password` / `new-password` / `confirm-password`, `card-*`, `busca-min` / `busca-max` / `taxa-ml` / `taxa-amazon`, `live-search-q` / `monitored-*`, `advanced-filter-*`, `ach-*`, `wa-*`, `tg-*`, `product-url`, `friendly-name` / `tracking-id`.
- [x] **1.3. Onda A — piloto de cadastro:**
  - `ProdutosPropaganda.razor`: `propaganda-nome`, `propaganda-categoria`, `propaganda-preco`, `propaganda-imagem`, `propaganda-descricao`, `propaganda-shopee-url`, `propaganda-shopee-id`, `propaganda-tiktok-url`, `propaganda-tiktok-id`; botões `propaganda-toggle-form` e `propaganda-salvar`.
  - `CampaignCreateForm.razor`: `campaign-name`, `campaign-description`, `campaign-budget`, `campaign-start`, `campaign-end`, `campaign-save`.
- [x] **1.4. Onda B — Master:**
  - `MasterOperators.razor`: `operator-name`, `operator-email`, `operator-role`, `operator-password`.
  - `MasterPlans.razor`: `plan-name-{code}`, `plan-monthly-{code}`, `plan-annual-{code}`, `plan-stores-{code}`, `plan-vip-{code}`, `plan-schedules-{code}`, `plan-radar-{code}`, `plan-active-{code}`, `plan-save-{code}` (highlights já têm id).
  - `MasterTenants.razor`: `tenant-search`, `tenant-status`, `tenant-plan`, `tenant-renewal`, `tenant-disk`, `tenant-action`, `tenant-trial-days`, `tenant-reason`, `tenant-plan-code`, `tenant-cycle`, `tenant-credits`, `tenant-credits-reason`.
  - `MasterAudit.razor`: `audit-action`, `audit-actor`, `audit-from`, `audit-to`.
  - `MasterDeveloperAccounts.razor`: `dev-platform`, `dev-kind`, `dev-environment`, `dev-label`, `dev-client-id`, `dev-secret`, `dev-extra`, `dev-test-user`, `dev-test-password`, `dev-secret-expires`, `dev-notes`; `dev-active` mantido.
  - `MasterCommunication.razor`: `banner-title`, `banner-severity`, `banner-message`, `banner-start`, `banner-end`, `comm-segment-kind`, `comm-segment-value`, `comm-title`, `comm-message`, `changelog-version`, `changelog-title`, `changelog-body`; `banner-active` e `changelog-published` mantidos.
  - `MasterSettings.razor`: input `vault-{chave}` e olho `vault-show-{chave}`, com `:` e demais símbolos da chave trocados por `-`.
- [x] **1.5. Onda C — restante interno:**
  - `EstudioMidia.razor`: `studio-image`, `studio-page`.
  - `HistoricoLinks.razor`: `history-name-card-{id}`, `history-price-card-{id}`, `history-name-table-{id}`, `history-price-table-{id}` (cartão e tabela ficam os dois no HTML).
  - `LinkGeneratorResultPanel.razor`: `generated-product-name`, `generated-product-price` (`selected-commission-account` mantido).
  - `BotConversor.razor` e `WhatsAppConexao.razor`: checkboxes já tinham `bot-wa-*` e `wa-bot-*`; nada foi renomeado.
  - `MasterMenuPermissions.razor`: `menu-active-{MenuItemId}` e `menu-plan-{MenuItemId}-{planId}` (`menu-folder-title` mantido).

### Fase 2: Regra do campo no servidor (`RpaFieldRules`)

- [x] **2.1. Número e texto onde a tela só confia no navegador:**
  - `MoneyAmountRules` (0 a 1.000.000,00, pt-BR `89,90` e ponto `89.90`). `AdvertisingProductService` recusa preço fora da faixa. `CampaignsController` recusa orçamento fora da faixa no criar e no alterar (`CampaignDto`, que é o payload da tela). `NewCampaignDto.Budget` ganhou `[Range]` e o nome continua com `[Required]` e `[StringLength(100)]`.
- [x] **2.2. Matriz no `Ofertio.Tests`:**
  - `MoneyAmountRules.Samples`: vazio, letra, negativo, zero, vírgula, ponto e acima do máximo. `MoneyAmountRulesTests`, `AdvertisingProductServiceTests` e `CampaignsControllerBudgetTests`. A fase 5 reutiliza `Samples`.

### Fase 3: Projeto Playwright (`RpaPlaywright`)

- [x] **3.1. `Ofertio.E2E`:**
  - Projeto xUnit + `Microsoft.Playwright`, fora do `Ofertio.Tests`. URL, e-mail e senha em User Secrets (`E2E:BaseUrl`, `E2E:Email`, `E2E:Password`) ou nas variáveis `OFERTIO_E2E_BASE_URL`, `OFERTIO_E2E_EMAIL` e `OFERTIO_E2E_PASSWORD`. O exemplo `appsettings.example.json` não leva senha; `appsettings.json` local está no `.gitignore`.
- [x] **3.2. Login único:**
  - `PortalSession.LoginAsync` abre `/`, preenche `#portal-email` e `#portal-password`, clica em Entrar e espera `/dashboard`. Locale `pt-BR` e fuso `America/Sao_Paulo`. Registros usam o prefixo `E2E-`. Sem segredo configurado, o teste de login fica ignorado e não abre o Chrome.

### Fase 4: Fumaça de navegação (`RpaSmoke`)

- [x] **4.1. Menu autenticado:**
  - `MenuSmokeTests` entra uma vez e abre `/dashboard`, `/meus-dados`, `/propaganda/produtos`, `/busca`, `/integracoes/grupos/monitorados` e `/minhas-lojas`, conferindo o `h1`. `/master` só exige "Dashboard executivo SaaS" se a rota permanecer; quem não é master volta para a entrada e a fumaça segue. Nenhum campo é preenchido.

### Fase 5: Piloto numérico (`RpaPilotPrice`)

- [x] **5.1. Produtos de propaganda:**
  - `propaganda-preco` aceita texto. `PricePilotTests` usa os casos `89,90`, `abc` e `-1` de `MoneyAmountRules.Samples`. A vírgula grava um produto `E2E-` e o card mostra `89,90` (`C2` pt-BR). Letra e negativo exibem "Preço médio inválido..." e não criam card. A página passou a ser interativa. Não há editar nem excluir.
- [x] **5.2. Nova campanha:**
  - `campaign-budget` aceita texto com a mesma matriz. `89,90` cria a campanha `E2E-` e a linha, filtrada pelo nome, mostra `89,90`. Letra e negativo exibem "Orçamento inválido..." e não entram na lista. A listagem não tem botão de alterar nem de excluir, então o robô não clica nessas ações.

### Fase 6: Conta (`RpaAccount`)

- [x] **6.1. Telas que já têm id:**
  - `AccountFlowTests` abre `/cadastro` (e-mail inválido e senhas diferentes, sem criar conta) e `/recuperar-senha` (não envia senha nova). Em `/meus-dados`, CPF, CNPJ e CEP inválidos não gravam; a UF `SP` é escolhida e devolvida. Em `/minha-conta`, senha diferente fica bloqueada e a senha atual vazia não troca a senha. O cartão `4111 1111 1111 1111` só conclui o pedido quando a tela avisa que nenhuma cobrança é feita.

### Fase 7: Master (`RpaMaster`)

- [x] **7.1. Depois da onda B:**
  - `MasterFlowTests` cria um operador `E2E-` e desativa (a tela não exclui). Nas vantagens do primeiro plano com botão salvar, grava um texto e devolve o anterior. Auditoria e inquilinos só filtram. Créditos, suspensão e "Aplicar ação" não são clicados. Quem não é master não falha o teste.

### Fase 8: Busca, grupos e links (`RpaCatalog`)

- [x] **8.1. Filtros e um fluxo de link:**
  - `CatalogFlowTests` abre os filtros de `/busca` com mínimo `500` e máximo `100` e espera "O preço mínimo não pode ser maior que o preço máximo", sem consultar as lojas. Em Grupos Monitorados, a busca rápida envia `q=e2e-nao-existe` e não sincroniza o canal. O gerador preenche `#product-url` com um link da Shopee, confere o aviso ou o painel "Seus links de comissão" e não abre OAuth nem Minhas Lojas.

### Fase 9: Fora do robô de tela (`RpaForaDeEscopo`)

- [x] **9.1. Não automatizar clique:**
  - `RobotBoundary` lista os cliques e os endereços de autorização, QR, código de bot, webhook e cobrança ASAAS. `RobotBoundaryTests` lê os `.cs` do robô e falha se algum deles aparecer. "Assinar" só permanece no fluxo que também exige o aviso "nenhuma cobrança é feita".
- [x] **9.2. Carga de dados:**
  - `scripts/carga-homolog-rpa.sql` grava 200 campanhas e 200 produtos com prefixo `CARGA-` para o e-mail informado. A limpeza, desligada por padrão, apaga só `E2E-` desse dono e desativa operadores `E2E-`. O script não foi executado.
