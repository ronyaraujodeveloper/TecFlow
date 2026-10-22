# 🎨 Diretrizes de Interface & UI/UX

> Regras para Blazor (`Ofertio.SharedUi` / `Ofertio.WebUi`), componentes, combos, modais e responsividade. Registro de telas no menu: [backend-architecture.md](./backend-architecture.md#7-registro-de-telas-e-árvore-dinâmica-de-menus).

## 1. Ícones obrigatórios em ações e botões

Todo botão (`<button>`, `<a>` estilizado como botão ou elemento clicável de ação) **deve** ter um ícone do **Bootstrap Icons** (`bi bi-*`) antes do rótulo, com espaçamento `me-1` (ou `gap-1` no contêiner flex).

| Ação | Ícone |
| :--- | :--- |
| Atualizar / Sincronizar | `bi-arrow-repeat` |
| Limpar / Excluir | `bi-trash` (limpar filtros: `bi-x-circle`) |
| Filtros / Configurações | `bi-gear` ou `bi-sliders` |
| Buscar | `bi-search` |
| Abrir / Link externo | `bi-box-arrow-up-right` |
| Verificar / Validar | `bi-check-circle` |
| Clonar / Copiar | `bi-copy` |
| Ocultar / Descartar | `bi-eye-slash` |
| Autorizar / Conectar OAuth | `bi-key` ou `bi-shield-check` |

```html
<button class="btn btn-outline-secondary">
    <i class="bi bi-arrow-repeat me-1"></i> Atualizar
</button>
```

## 2. Padrão global para combos e dropdowns

1. **Busca interna condicional:** só com **mais de 6 itens** (`MultiSelectRules.ShowSearch`); com 6 ou menos, sem input de busca. A busca filtra a partir de 3 letras, sem diferenciar acentos.
2. **Seleção múltipla vs. única:** confirme com o usuário, para cada combo novo, se é multiseleção (checkboxes) ou seleção única.
3. **Lista estritamente vertical:** um item por linha, checkbox à esquerda do texto.
4. **Fechar ao clicar fora:** clique ou foco fora fecha o combo (`ofertioMultiSelect.watch/unwatch` no `app-interop.js`); abrir outro combo fecha o anterior.
5. **Foco automático:** ao abrir, foco na busca; sem busca (≤ 6 itens), foco na lista.
6. **Teclado:** setas (ou hover) movem o item ativo, Espaço marca/desmarca, Enter marca/desmarca o item ativo ou fecha, Esc fecha e devolve o foco ao botão.
7. **Gatilho com ícone** `bi bi-*` e resumo da seleção (ex.: "Categorias (2)").

Referência de implementação: `Ofertio.SharedUi/Components/Shared/MultiSelectDropdown.razor`. Não use o `dropdown-menu` do Bootstrap nesses combos: o atalho de teclado nativo dele exige `data-bs-toggle` e disputa o estado com o Blazor.

## 3. Responsividade (Mobile First)

- Todo componente, página ou layout novo contempla celular, tablet e desktop.
- Proibido largura fixa em pixels para contêineres principais e tabelas densas; use grids fluidos, flexbox e utilitários responsivos do Bootstrap.
- Barras de filtros: `display:flex; flex-wrap:wrap; gap` e menus ancorados para não vazar na horizontal.

## 4. Modais e diálogos de confirmação

- **Proibido** `window.confirm`, `window.alert` ou `window.prompt`.
- Ações críticas (excluir, editar, desconectar) usam modal customizado com título claro, ícone de alerta, nome do item, botão destrutivo vermelho ("Sim, excluir") e botão neutro ("Cancelar").
- O modal fecha com `Esc` e clique no backdrop, e é padronizado em todos os módulos.
- **Ações em tabelas:** use `@onclick="() => AbrirModalExclusao(item.Id)"` (lambda, para não disparar na renderização); ao abrir, altere a flag (ex.: `isDeleteModalOpen = true`) e chame `StateHasChanged()`; o modal fica fora da `<table>` e do `@foreach`, no fim do `.razor`.
- Componentes globais renderizados no layout SSR estático (ex.: `ConfirmDialogHost`, `ToastContainer`) precisam de `InteractiveServer` próprio.

## 5. Feedback ao usuário

- Mensagens de sucesso, erro, aviso e informação usam o `IToastService` (`ShowSuccess`, `ShowError`, `ShowWarning`, `ShowInfo`), exibido pelo `ToastContainer` no canto superior direito.
- Botões com operação demorada mostram spinner, ficam desabilitados ("Atualizando...") e voltam ao normal no `finally`.

## 6. Badges e cores de plataforma

Selos com cantos arredondados, `font-semibold` e tamanho compacto (`text-xs px-2.5 py-1`):

- **Amazon:** `#FF9900`, texto `#FFFFFF`
- **Mercado Livre:** `#FFE600`, texto `#000000` (ou `#2D3277`)
- **Shopee:** `#EE4D2D`, texto `#FFFFFF`
- **Magazine Luiza:** `#0086FF`, texto `#FFFFFF`
- **TikTok Shop:** `#000000`, texto `#FFFFFF` (detalhes `#00F2FE` / `#FE2C55`)
- **Hotmart:** `#FF5200`; **Braip:** `#12B76A`; **Outros:** `#6B7280` (texto `#FFFFFF`)

## 7. Menu lateral (Sidebar)

Agrupamento: **Mensageria & Bots** (Conexões, Agendador de Grupos, Bot de Conversão) ➔ **Canais** (Agendador WhatsApp, Agendador Telegram) ➔ **Integrações & Afiliados** (Minhas Lojas, Gerador de Links, Páginas Públicas) ➔ **Próximos Desenvolvimentos** (accordion com Dashboard, Campanhas, Métricas, Fila de engajamento "Em breve", Conciliação, Produtos propaganda, Saúde do sistema, Minha conta).

- Submenus retráteis controlam `isExpanded` em C# (`@onclick` + classes condicionais), sem JavaScript externo; transição suave e chevron que gira ao expandir.

## 8. Regras de tela específicas

- **Edição de agendamentos:** se `DataAgendada > DateTime.Now`, mantenha data e hora originais; se já passou, ajuste para `DateTime.Now.AddMinutes(10)` e mostre aviso apenas nesse caso.
- **Imagens de produto:** a conversão de links grava `og:image` em `ProductImageUrl` (editável); o agendador preenche "URL da imagem (opcional)" ao escolher o link, e o campo tem preview de 80x80 px.

## 9. Assets estáticos e localização

- `app.css` e `app-interop.js` do SharedUi são servidos em `_content/Ofertio.SharedUi/` com `?v=` (versão do build) no `App.razor`, para o navegador não usar cópia antiga do cache.
- Interface em `pt-BR` (textos, datas e moeda); regras de cultura e encoding em [encoding-standards.md](./encoding-standards.md).
