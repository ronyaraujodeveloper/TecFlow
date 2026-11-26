# Ajustes nos arquivos que só mudam de lugar

> Três documentos atuais estão bons e só são **movidos** de `docs/rules/` para `docs/projeto/`. Em vez de copiar centenas de linhas iguais, esta lista diz exatamente o que muda em cada um. Linhas citadas referem-se à versão atual.

## `docs/rules/frontend-ui.md` → `docs/projeto/frontend-ui.md`

| Linha atual | Ajuste | Motivo |
|---|---|---|
| 3 | Trocar o link de registro de telas para `./menu-e-permissoes.md`. | §7 do backend virou documento próprio. |
| 41 | "Todo componente... contempla celular, tablet e desktop" → "DEVE: funcionar em 360 px, 768 px e 1280 px de largura sem rolagem horizontal. Verificar: E2E com viewport 360 ou inspeção." | Torna verificável. |
| 73 | Remover "transição suave". Manter "submenus retráteis controlam `isExpanded` em C#, sem JavaScript externo". | "Suave" não é verificável e não é regra. |
| 75–80 (§7.1) | Remover e apontar para `./menu-e-permissoes.md` §4. | Duplicava o backend §7. |
| 71 (§7, agrupamento da sidebar) | Remover a lista fixa de grupos. Escrever: "A ordem e os grupos da sidebar vêm de `MenuItems` e são definidos pelo Master." | A lista fixa desatualiza a cada tela nova e contradiz o menu dinâmico (`backend-architecture.md:68-70`). |
| 109 | Manter "Esta seção é a fonte única", trocando a menção a `backend-architecture.md` por `arquitetura.md`. | Nome novo. |
| novo §5 | Acrescentar: "DEVE: busca que dispara requisição usa debounce de ~300 ms." | Regra de performance P15 aplicada à tela. |
| todo o arquivo | Prefixar as regras com DEVE / NÃO DEVE / RECOMENDADO, sem mudar o conteúdo. | Padrão de escrita. |

## `docs/rules/marketplaces-oauth.md` → `docs/projeto/integracoes-marketplaces.md`

| Linha atual | Ajuste | Motivo |
|---|---|---|
| 3 | Link de segurança → `./seguranca-ofertio.md`. | Nome novo. |
| 9 e 10 | Remover (logs e segredos) e apontar para `docs/padroes/observabilidade.md` §2 e `seguranca-ofertio.md` §3. | Duplicados em `backend-architecture.md:60-61`. |
| 14 | Separar a Redirect URI por ambiente: hoje o texto e o `Ofertio.API/appsettings.json:98,124` usam `https://dev.ofertio.com.br/...` como valor base. ❓ Confirmar qual URI a produção usa (pergunta 11). | O `appsettings.json` base vale para produção. |
| 21 e `backend-architecture.md:84` | Remover "Não criar subaplicação `/api` no site `Ofertio.WebUi`" (regra do IIS). Manter só "a WebUi repassa `/api/mercadolivre/*` para a API via YARP". | IIS saiu; o roteiro IIS está arquivado (`README.md:84`). |
| 85–87 (§8 Telegram) | Trocar `C:\ProgramData\Ofertio\telegram-sessions` por "volume `api_appdata`, pasta `App_Data/telegram-sessions` (`docker-compose.yml:85`)". | Desatualizado desde a migração para Docker. |
| 125–136 (§7 tabela) | Mover a coluna "Status" para o `CHECKLIST_ATIVO.md` ou removê-la. | Status de entrega não é regra e desatualiza. |
| novo §1 | Acrescentar: "DEVE: webhook de marketplace valida a assinatura HMAC (`MarketplaceWebhookSignatureVerifier`) e é idempotente pelo id do evento." | Regra existia só no código. |

## `docs/rules/ambientes-docker.md` → `docs/projeto/ambientes-docker.md`

| Linha atual | Ajuste | Motivo |
|---|---|---|
| 41–44 (§4 Segredos) | Remover e apontar para `seguranca-ofertio.md` §3 (tabela de segredos por ambiente). | Duplicado em 4 lugares. |
| 55 | Manter (é a fonte única de "nunca `down -v`"). Remover a mesma frase de `README.md:82` e `EXECUCAO_DESENVOLVIMENTO.md:182`, que passam a apontar para cá. | Duplicação. |
| novo §6 | "DEVE: imagens de terceiros no compose com versão fixa, ao menos `maior.menor` (ex.: `mcr.microsoft.com/mssql/server:2022-CU16-ubuntu-22.04`, `rabbitmq:3.13-management`, `caddy:2.8`); NÃO DEVE `:latest`. Atualizar a versão é uma tarefa explícita, testada antes em homologação." | Auditoria F8. Com `:latest`, um `docker compose pull` na VPS pode trazer uma versão maior sem teste. Recomendação adotada (pergunta 12). |
| novo §6 | "DEVE: containers da API e da WebUi rodam com usuário não root (`USER $APP_UID`, já disponível nas imagens .NET 8). Volumes gravados pela aplicação (`uploads`, `api_appdata`) recebem dono `1654` antes da troca." | Auditoria F8. Se a aplicação for invadida, o atacante não ganha root no container. SQL Server já roda como `mssql`. Recomendação adotada (pergunta 12). |
| novo §7 | "DEVE: backup diário automático dos volumes `sqlserver_data` e `api_appdata` fora da VPS, com restauração testada por trimestre." | Política de Segurança promete; `CHECKLIST_ATIVO.md:34` registra como pendente. |

## Arquivos que deixam de existir

| Arquivo | Destino do conteúdo |
|---|---|
| `docs/rules/backend-architecture.md` | `arquitetura.md`, `seguranca-ofertio.md`, `menu-e-permissoes.md` |
| `docs/rules/encoding-standards.md` | `encoding-e-legado.md` e `docs/padroes/codigo-csharp.md` §1 |
| `.cursorrules` e `.cursorignore` | removidos (o Cursor não é mais usado, decisão de 25/11); o fluxo de documentação foi para o `CLAUDE.md` |
| `NormalizarEncodingPrecoce.ps1` no README e em `EXECUCAO_DESENVOLVIMENTO.md` | deixa de ser passo obrigatório antes do build; o script passa a ter `*.ps1` como padrão |
| `README.md` §"Regras de ouro" e "Nota para a IA" | `CLAUDE.md` |
| memórias do Claude `controle-itens-hierarquia-menu` e `ambientes-espelham-producao` | já estão nos documentos versionados; podem ser apagadas da memória pessoal |
