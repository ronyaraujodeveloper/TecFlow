[2026-09-29 21:43:13]fix(homolog): preenche preco e nome no modal de edicao do link
[2026-09-29 22:37:27]feat(paginas): versiona slugs da pagina publica e deduplica plataformas
[2026-09-29 22:57:36]feat(paginas): compacta badges e resultados da pagina publica
[2026-09-30 21:03:02]feat(whatsapp): orquestra sessao Evolution API por usuario
[2026-09-30 21:22:34]feat(whatsapp): converte links do webhook Evolution em ate 3s
[2026-09-30 21:43:37]feat(whatsapp): agenda disparos para grupos com intervalo anti-bloqueio
[2026-09-30 22:06:41]feat(security): criptografa tokens de Telegram e WhatsApp e exige X-Webhook-Secret
[2026-09-30 22:26:46]feat(telegram): conecta bot, converte links no privado e agenda disparos
[2026-09-30 22:38:52]feat(ui): unifica conexoes WhatsApp e Telegram em abas com QR modal
[2026-09-30 22:57:34]feat(ui): unifica menu de mensageria sem perder rotas
[2026-09-30 23:20:51]fix(ui): restaura lojas no menu e evita tela em branco nas paginas publicas
[2026-09-30 23:42:24]fix(whatsapp): trata falhas da Evolution API sem 500 generico
[2026-10-01 21:44:49]feat(ui): seletor de links e reset no agendador de grupos
[2026-10-01 22:18:46]feat(whatsapp): separa copy e link no disparo para grupos
[2026-10-01 22:41:05]feat(whatsapp): MultiSelect de grupos com admin real e atalhos
[2026-10-01 23:06:30]feat(whatsapp): edita e pagina lista de disparos
[2026-10-03 18:47:15]feat(whatsapp): confirma e atualiza agendamentos
[2026-10-03 19:04:02]fix(whatsapp): habilita clique nas acoes do agendador
[2026-10-03 19:29:03]feat(ui): padroniza badges coloridas de plataforma
[2026-10-03 19:36:30]feat(ui): substitui confirm nativo por modal global
[2026-10-03 19:47:37]fix(whatsapp): preserva data futura na edicao
[2026-10-03 19:54:51]fix(whatsapp): abre modal de exclusao na propria tela
[2026-10-03 20:16:33]feat(links): captura e preview da imagem do produto
[2026-10-03 20:33:05]feat(ui): reorganiza grupos do menu lateral
[2026-10-03 20:47:45]fix(ui): corrige toggle do accordion Proximos Desenvolvimentos
[2026-10-03 20:59:53]fix(ui): accordion Proximos Desenvolvimentos via botao e StateHasChanged
[2026-10-03 21:17:08]fix(ui): torna NavMenu InteractiveServer para abrir submenus
[2026-10-03 21:56:11]feat(grupos): monitora e clona ofertas de grupos
[2026-10-03 22:38:45]feat(telegram): valida token e modal BotFather
[2026-10-03 23:03:12]feat(telegram): salva conexao quando webhook local falha
[2026-10-03 23:18:32]fix(telegram): captura falha do SetWebhook sem abortar conexao
[2026-10-03 23:32:25]feat(ui): badge de plataforma nos agendadores
[2026-10-03 23:45:14]feat(telegram): sincroniza canais e dispara em multi-select
[2026-10-04 00:02:43]feat(telegram): chips com nome amigavel do canal
[2026-10-04 00:19:08]fix(grupos): captura erro ao sincronizar grupos monitorados
[2026-10-04 10:30:14]fix(grupos): registra IUserContextProvider e evita falso sucesso no sync
[2026-10-04 10:52:41]feat(grupos): separa grupos monitorados no menu WhatsApp e Telegram
[2026-10-04 11:35:31]feat(telegram): escuta userbot mtproto de canais de terceiros
[2026-10-04 11:51:44]fix(telegram): fallback TEMP para sessoes UserBot no IIS
[2026-10-04 12:25:08]feat(telegram): login UserBot em duas etapas com modal de ajuda
[2026-10-04 14:15:52]fix(telegram): valida api_id numerico e habilita PIN apos codigo 200
[2026-10-04 14:45:05]fix(telegram): helpers UserBot, PIN manual e MakeAuth da sessao
[2026-10-04 15:15:54]feat(telegram): catch-up e fila de captura userbot
[2026-10-04 16:01:56]feat(telegram): catch-up paginado e lista de ofertas em 50
[2026-10-04 18:12:59]fix(telegram): varre historico real dos canais no catch-up
[2026-10-04 18:32:39]fix(auth): trata 401 da API sem mascarar como CORS
[2026-10-04 18:49:09]fix(ui): restaura menu e login apos 401
[2026-10-04 19:22:02]fix(telegram): sync aguarda historico UserBot e preserva sessao
[2026-10-04 21:22:59]feat(links): clona oferta com unshorten e historico
[2026-10-04 21:55:28]feat(ui): filtra feed monitorado e descarta ofertas
