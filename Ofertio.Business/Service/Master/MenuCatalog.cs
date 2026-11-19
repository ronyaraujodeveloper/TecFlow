using Ofertio.Business.Dto;
using Ofertio.Core.Enums;

namespace Ofertio.Business.Service.Master;

public sealed record MenuItemDefinition(
    string Key,
    string Title,
    string? Route,
    string Icon,
    string? ParentKey,
    int Order,
    IReadOnlyList<string>? HiddenForPlans = null,
    string? Badge = null,
    bool HiddenForAllPlans = false,
    bool IsFeatureToggle = false,
    string? ComponentType = null)
{
    /// <summary>Parte de uma tela (gráfico, input, botão...); o pai é sempre uma Tela e nunca aparece na sidebar.</summary>
    public bool IsComponent => ComponentType is not null;

    /// <summary>Grupo: sem rota e não é chave de permissão nem componente.</summary>
    public bool IsFolder => Route is null && !IsFeatureToggle && !IsComponent;

    public MenuItemKind Kind => MenuHierarchyRules.KindOf(Route, IsFeatureToggle, ComponentType);
}

/// <summary>Tipos de componente exibidos como selo em /master/menu-permissions.</summary>
public static class ComponentTypes
{
    public const string Chart = "chart";
    public const string Card = "card";
    public const string Section = "section";
    public const string Input = "input";
    public const string Button = "button";

    /// <summary>Funcionalidade da tela sem um elemento visual único (ex.: slug personalizado).</summary>
    public const string Feature = "feature";

    public static string Label(string? type) => type switch
    {
        Chart => "gráfico",
        Card => "card",
        Section => "seção",
        Input => "campo",
        Button => "botão",
        Feature => "recurso",
        _ => "componente"
    };

    public static string Icon(string? type) => type switch
    {
        Chart => "bi-bar-chart-line",
        Card => "bi-card-text",
        Section => "bi-layout-text-window",
        Input => "bi-input-cursor-text",
        Button => "bi-hand-index",
        Feature => "bi-toggle-on",
        _ => "bi-puzzle"
    };
}

/// <summary>
/// Chaves dos componentes controláveis por plano, no formato {chaveDaTela}.{slug}.
/// Na tela use &lt;AccessGate Key="..."&gt;; se o componente expõe dados ou ações, cheque também no servidor.
/// </summary>
public static class ScreenComponents
{
    public static class Painel
    {
        public const string Kpis = "painel.indicadores";
        public const string ClicksChart = "painel.grafico-cliques";
        public const string Channels = "painel.origens";
        public const string Devices = "painel.dispositivos";
        public const string TopProducts = "painel.top-produtos";
        public const string PublicPageFunnel = "painel.funil-pagina-publica";
        public const string Regions = "painel.regioes";
        public const string Groups = "painel.grupos";
        public const string BestHours = "painel.melhores-horarios";
    }

    public static class PaginasPublicas
    {
        public const string CustomSlug = "integracoes.paginas.slug";
    }
}

/// <summary>
/// Árvore padrão da sidebar do cliente: Categoria (raiz sem rota) → Menu pai (link ou grupo) → Submenu.
/// O catálogo é a fonte da estrutura; MenuItems é sincronizado a partir dele automaticamente.
/// </summary>
public static class DefaultMenuCatalog
{
    public const string RadarOffers = "integracoes.radar";
    public const string RadarProfile = "integracoes.radar-perfil";
    public const string MarketplaceAccounts = "integracoes.lojas";
    public const string OfferSearch = "integracoes.busca";

    /// <summary>Permissão "Integrações Globais": libera as telas que consomem a API oficial conectada no Master.</summary>
    public const string GlobalIntegrations = "integracoes-globais";

    /// <summary>Permissão sob Minhas Páginas Públicas: libera trocar o slug aleatório por um endereço próprio.</summary>
    public const string CustomPublicSlug = ScreenComponents.PaginasPublicas.CustomSlug;

    /// <summary>Pasta "Minha conta": Segurança (chave legada "conta"), Meus dados e Meu plano.</summary>
    public const string MyAccount = "minha-conta";
    public const string MyData = "conta.dados";
    public const string MyPlan = "conta.plano";

    private static readonly IReadOnlyList<string> RadarPlans = [SaasPlanCodes.Starter];

    /// <summary>Telas ocultadas automaticamente quando a permissão (chave) está desligada para o plano.</summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> FeatureDependencies =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [GlobalIntegrations] = [OfferSearch, RadarOffers, RadarProfile]
        };

    public static readonly IReadOnlyList<MenuItemDefinition> All =
    [
        new("painel", "Painel", "/dashboard", "bi-speedometer2", null, 10),
        Component("painel", ScreenComponents.Painel.Kpis, "Indicadores (cliques, visitantes, links, melhor dia)", ComponentTypes.Card, 10),
        Component("painel", ScreenComponents.Painel.ClicksChart, "Gráfico: cliques ao longo do tempo", ComponentTypes.Chart, 20),
        Component("painel", ScreenComponents.Painel.Channels, "Gráfico: de onde vêm os cliques", ComponentTypes.Chart, 30),
        Component("painel", ScreenComponents.Painel.Devices, "Gráfico: dispositivos", ComponentTypes.Chart, 40),
        Component("painel", ScreenComponents.Painel.TopProducts, "Top 10 produtos mais clicados", ComponentTypes.Section, 50),
        Component("painel", ScreenComponents.Painel.PublicPageFunnel, "Funil da página pública", ComponentTypes.Chart, 60),
        Component("painel", ScreenComponents.Painel.Regions, "Gráfico: cliques por região", ComponentTypes.Chart, 70),
        Component("painel", ScreenComponents.Painel.Groups, "Grupos que mais trazem cliques", ComponentTypes.Section, 80),
        Component("painel", ScreenComponents.Painel.BestHours, "Gráfico: melhores dias e horários", ComponentTypes.Chart, 90),

        new("mensageria", "Mensageria & Bots", null, "bi-chat-dots", null, 20),
        new("mensageria.conexoes", "Conexões (WA & Telegram)", "/integracoes/conexoes", "bi-qr-code", "mensageria", 10),
        new("mensageria.agendador", "Agendador de Grupos", "/integracoes/agendador", "bi-calendar-week", "mensageria", 20),
        new("mensageria.bot", "Bot de Conversão", "/integracoes/bot-conversor", "bi-robot", "mensageria", 30),

        new(GlobalIntegrations, "Integrações Globais", null, "bi-globe-americas", null, 25, IsFeatureToggle: true),

        new("canais", "Canais", null, "bi-broadcast-pin", null, 30),
        new("whatsapp", "WhatsApp", null, "bi-whatsapp", "canais", 10),
        new("whatsapp.agendador", "Agendador WhatsApp", "/integracoes/whatsapp/agendador", "bi-send", "whatsapp", 10),
        new("whatsapp.monitorados", "Grupos monitorados", "/integracoes/whatsapp/grupos/monitorados", "bi-eye", "whatsapp", 20),
        new("telegram", "Telegram", null, "bi-telegram", "canais", 20),
        new("telegram.agendador", "Agendador Telegram", "/integracoes/telegram/agendador", "bi-send", "telegram", 10),
        new("telegram.monitorados", "Grupos monitorados", "/integracoes/telegram/grupos/monitorados", "bi-eye", "telegram", 20),

        new("integracoes", "Integrações", null, "bi-plug", null, 50),
        new(MarketplaceAccounts, "Minhas Lojas / Integrações", "/marketplace-accounts", "bi-shop", "integracoes", 10),
        new("integracoes.links", "Links & Ofertas", null, "bi-link-45deg", "integracoes", 20),
        new("integracoes.gerador", "Gerador de Links", "/gerador-links", "bi-link-45deg", "integracoes.links", 10),
        new(OfferSearch, "Buscar Ofertas", "/busca", "bi-search", "integracoes.links", 20),
        new(RadarOffers, "Radar de Ofertas", "/radar-ofertas", "bi-broadcast", "integracoes.links", 30, RadarPlans),
        new(RadarProfile, "Perfil de mineração", "/radar/perfil", "bi-sliders", "integracoes.links", 40, RadarPlans),
        new("integracoes.conteudo", "Conteúdo & Vitrines", null, "bi-collection", "integracoes", 30),
        new("integracoes.evergreen", "Biblioteca Evergreen", "/biblioteca-evergreen", "bi-recycle", "integracoes.conteudo", 10),
        new("integracoes.estudio", "Estúdio de mídia", "/estudio-midia", "bi-image", "integracoes.conteudo", 20),
        new("integracoes.achadinhos", "Achadinhos VIP", "/achadinhos", "bi-stars", "integracoes.conteudo", 30),
        new("integracoes.paginas", "Minhas Páginas Públicas", "/minhas-paginas-publicas", "bi-globe2", "integracoes.conteudo", 40),
        new(CustomPublicSlug, "Slug personalizado", null, "bi-pencil", "integracoes.paginas", 10, HiddenForAllPlans: true, ComponentType: ComponentTypes.Feature),
        new("integracoes.resultados", "Saúde & Resultados", null, "bi-activity", "integracoes", 40),
        new("integracoes.saude-ofertas", "Saúde de ofertas", "/saude/ofertas", "bi-heart-pulse", "integracoes.resultados", 10),
        new("integracoes.saude-agendamentos", "Saúde de agendamentos", "/saude/agendamentos", "bi-shield-check", "integracoes.resultados", 20),
        new("integracoes.lucro-grupo", "Lucro por grupo", "/atribuicao/grupos", "bi-pie-chart", "integracoes.resultados", 30),

        new("novidades", "Novidades", "/novidades", "bi-megaphone", null, 60),
        new(MyAccount, "Minha conta", null, "bi-person-circle", null, 70),
        new("conta", "Segurança", "/minha-conta", "bi-shield-lock", MyAccount, 10),
        new(MyData, "Meus dados", "/meus-dados", "bi-person-vcard", MyAccount, 20),
        new(MyPlan, "Meu plano", "/meu-plano", "bi-credit-card", MyAccount, 30),

        new(Roadmap, "Próximos Desenvolvimentos", null, "bi-rocket-takeoff", null, 80, HiddenForAllPlans: true),
        new("proximos.campanhas", "Campanhas", "/campanhas", "bi-megaphone", Roadmap, 10),
        new("proximos.metricas", "Métricas / Comissões", "/metricas", "bi-graph-up", Roadmap, 20),
        new("proximos.fila-engajamento", "Fila de engajamento", "/fila-engajamento", "bi-inbox", Roadmap, 30, Badge: "EM BREVE"),
        new("proximos.conciliacao", "Conciliação financeira", "/conciliacao-financeira", "bi-cash-stack", Roadmap, 40),
        new("proximos.propaganda", "Produtos propaganda", "/produtos-propaganda", "bi-tag", Roadmap, 50),
        new("proximos.saude-sistema", "Saúde do sistema", "/saude-sistema", "bi-heart-pulse", Roadmap, 60)
    ];

    public const string Roadmap = "proximos";

    /// <summary>Prefixo das chaves de pastas criadas no Master (fora do catálogo padrão).</summary>
    public const string CustomFolderPrefix = "pasta.";

    /// <summary>Endereços alternativos de uma tela: destacam o item na sidebar e herdam o bloqueio de rota da tela.</summary>
    public static readonly IReadOnlyDictionary<string, string[]> RouteAliases =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["/marketplace-accounts"] = ["/minhas-lojas", "/account/integrations", "/integracoes/lojas"],
            ["/gerador-links"] = ["/comissao/gerar"],
            ["/minha-conta"] = ["/seguranca", "/account/security"],
            ["/integracoes/conexoes"] = ["/integracoes/whatsapp", "/integracoes/telegram"],
            ["/integracoes/whatsapp/grupos/monitorados"] = ["/integracoes/grupos/monitorados"],
            ["/fila-engajamento"] = ["/engajamento/fila"],
            ["/conciliacao-financeira"] = ["/conciliacao/financeira"],
            ["/produtos-propaganda"] = ["/propaganda/produtos"],
            ["/saude-sistema"] = ["/saude/painel"]
        };

    /// <summary>A rota e seus endereços alternativos.</summary>
    public static IEnumerable<string> WithAliases(string route) =>
        RouteAliases.TryGetValue(route, out var aliases) ? aliases.Prepend(route) : [route];

    /// <summary>Componente de uma tela: chave {tela}.{slug}, sem rota, visível por padrão em todos os planos.</summary>
    private static MenuItemDefinition Component(string pageKey, string key, string title, string type, int order) =>
        new(key, title, null, ComponentTypes.Icon(type), pageKey, order, ComponentType: type);

    public static MenuItemDefinition? Find(string key) =>
        All.FirstOrDefault(item => string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase));

    /// <summary>Árvore do catálogo para clientes (fallback da UI quando a API não responde); omite o roadmap.</summary>
    public static List<ClientMenuNodeDto> BuildDefaultTree()
    {
        var ids = All
            .Select((item, index) => (item.Key, Id: index + 1))
            .ToDictionary(pair => pair.Key, pair => pair.Id, StringComparer.OrdinalIgnoreCase);
        var flat = All
            .Select(item => new MenuNodeSource(
                ids[item.Key],
                item.Key,
                item.Title,
                item.Route,
                item.Icon,
                item.ParentKey is null ? null : ids[item.ParentKey],
                item.Order,
                true,
                item.IsFolder,
                item.Badge,
                item.Kind))
            .ToList();
        var hidden = All.Where(item => item.HiddenForAllPlans).Select(item => ids[item.Key]).ToHashSet();
        return MenuVisibilityRules.BuildTree(flat, hidden);
    }
}

public sealed record MenuNodeSource(
    int Id,
    string Key,
    string Title,
    string? Route,
    string? Icon,
    int? ParentId,
    int Order,
    bool IsActive,
    bool IsFolder = false,
    string? Badge = null,
    MenuItemKind? Kind = null,
    string? ComponentType = null)
{
    /// <summary>Nível na hierarquia; sem valor explícito, deduzido de pasta/rota (dados legados e testes).</summary>
    public MenuItemKind EffectiveKind => Kind ?? (IsFolder ? MenuItemKind.Group : Route is null ? MenuItemKind.Feature : MenuItemKind.Page);
}

/// <summary>Regras da hierarquia Grupo → Tela → Componente e da resolução de acesso por chave e rota.</summary>
public static class MenuHierarchyRules
{
    public static MenuItemKind KindOf(string? route, bool isFeatureToggle, string? componentType) =>
        componentType is not null ? MenuItemKind.Component
        : isFeatureToggle ? MenuItemKind.Feature
        : route is null ? MenuItemKind.Group
        : MenuItemKind.Page;

    /// <summary>Mensagem de erro quando o item não pode ficar sob o pai informado (null = raiz); null quando é válido.</summary>
    public static string? ValidateParent(MenuItemKind kind, MenuItemKind? parentKind) => (kind, parentKind) switch
    {
        (MenuItemKind.Component, MenuItemKind.Page) => null,
        (MenuItemKind.Component, _) => "Componentes ficam sempre dentro da tela a que pertencem.",
        (_, MenuItemKind.Component) => "Nenhum item pode ficar dentro de um componente.",
        (_, MenuItemKind.Page) => "Dentro de uma tela só entram componentes dela.",
        (_, MenuItemKind.Feature) => "Chaves de permissão não recebem itens.",
        (MenuItemKind.Feature, MenuItemKind.Group) => "Chaves de permissão não podem ser movidas para dentro de pastas.",
        _ => null
    };

    /// <summary>
    /// Chaves negadas para o público: inativas, ocultas no plano, telas de chaves de permissão desligadas
    /// e, em cascata, tudo que está abaixo de um item negado (grupo → tela → componente).
    /// </summary>
    public static HashSet<string> ResolveDeniedKeys(IReadOnlyCollection<MenuNodeSource> items, IReadOnlySet<int> hiddenIds)
    {
        var hidden = MenuVisibilityRules.ApplyFeatureDependencies(items, hiddenIds);
        var byId = items.ToDictionary(item => item.Id);
        var denied = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var memo = new Dictionary<int, bool>();

        foreach (var item in items)
        {
            if (IsDenied(item.Id, []))
            {
                denied.Add(item.Key);
            }
        }

        return denied;

        bool IsDenied(int id, HashSet<int> path)
        {
            if (memo.TryGetValue(id, out var cached))
            {
                return cached;
            }

            var item = byId[id];
            var result = !item.IsActive
                || hidden.Contains(id)
                || (item.ParentId is int parentId && byId.ContainsKey(parentId) && path.Add(id) && IsDenied(parentId, path));
            memo[id] = result;
            return result;
        }
    }

    /// <summary>
    /// True quando a rota mais específica que casa com o caminho está negada.
    /// Rotas casam por igualdade ou prefixo seguido de "/" (ex.: /radar-ofertas/123).
    /// </summary>
    public static bool IsRouteDenied(string? path, IEnumerable<string> allowedRoutes, IEnumerable<string> deniedRoutes)
    {
        var normalized = Normalize(path);
        if (normalized.Length == 0)
        {
            return false;
        }

        var bestDenied = Longest(deniedRoutes);
        return bestDenied > 0 && bestDenied > Longest(allowedRoutes);

        int Longest(IEnumerable<string> routes) =>
            routes.Select(Normalize)
                .Where(route => route.Length > 1
                    && (normalized == route || normalized.StartsWith(route + "/", StringComparison.Ordinal)))
                .Select(route => route.Length)
                .DefaultIfEmpty(0)
                .Max();
    }

    private static string Normalize(string? route)
    {
        if (string.IsNullOrWhiteSpace(route))
        {
            return string.Empty;
        }

        var value = route.Trim();
        var cut = value.IndexOfAny(['?', '#']);
        if (cut >= 0)
        {
            value = value[..cut];
        }

        value = "/" + value.Trim('/');
        return value.ToLowerInvariant();
    }
}

/// <summary>Regras puras de edição da árvore pelo drag &amp; drop do Master.</summary>
public static class MenuTreeEditRules
{
    public const int OrderStep = 10;
    public const int FolderTitleMaxLength = 96;

    /// <summary>True quando <paramref name="candidateId"/> é o próprio item ou está abaixo dele (mover criaria ciclo).</summary>
    public static bool IsSelfOrDescendant(IReadOnlyDictionary<int, int?> parentById, int candidateId, int itemId)
    {
        var visited = new HashSet<int>();
        int? current = candidateId;
        while (current is int id && visited.Add(id))
        {
            if (id == itemId)
            {
                return true;
            }

            current = parentById.GetValueOrDefault(id);
        }

        return false;
    }

    /// <summary>Ordem final dos irmãos com o item inserido antes/depois do alvo (o item sai da posição antiga).</summary>
    public static List<int> InsertRelativeTo(IReadOnlyList<int> siblingIds, int itemId, int targetId, bool after)
    {
        var ordered = siblingIds.Where(id => id != itemId).ToList();
        var index = ordered.IndexOf(targetId);
        if (index < 0)
        {
            ordered.Add(itemId);
            return ordered;
        }

        ordered.Insert(after ? index + 1 : index, itemId);
        return ordered;
    }

    public static string? ValidateFolderTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return "Informe o título da pasta.";
        }

        return title.Trim().Length > FolderTitleMaxLength ? $"O título deve ter no máximo {FolderTitleMaxLength} caracteres." : null;
    }
}

/// <summary>Monta a árvore visível para um plano.</summary>
public static class MenuVisibilityRules
{
    /// <summary>
    /// Remove itens inativos/ocultos (e seus filhos) e grupos sem nenhum filho visível.
    /// </summary>
    public static List<ClientMenuNodeDto> BuildTree(IReadOnlyCollection<MenuNodeSource> items, IReadOnlySet<int> hiddenIds)
    {
        var byParent = items
            .Where(item => item.IsActive && !hiddenIds.Contains(item.Id) && item.EffectiveKind != MenuItemKind.Component)
            .GroupBy(item => item.ParentId ?? 0)
            .ToDictionary(group => group.Key, group => group.OrderBy(item => item.Order).ThenBy(item => item.Title).ToList());

        return Build(0);

        List<ClientMenuNodeDto> Build(int parentId)
        {
            if (!byParent.TryGetValue(parentId, out var children))
            {
                return [];
            }

            var nodes = new List<ClientMenuNodeDto>();
            foreach (var child in children)
            {
                var node = new ClientMenuNodeDto
                {
                    Key = child.Key,
                    Title = child.Title,
                    Route = child.Route,
                    Icon = child.Icon,
                    Badge = child.Badge,
                    IsFolder = child.IsFolder || string.IsNullOrWhiteSpace(child.Route),
                    Children = Build(child.Id)
                };

                if (string.IsNullOrWhiteSpace(node.Route) && node.Children.Count == 0)
                {
                    continue;
                }

                nodes.Add(node);
            }

            return nodes;
        }
    }

    /// <summary>
    /// Acrescenta aos ocultos as telas dependentes de cada chave de permissão desligada para o plano
    /// (chave inativa, ausente da lista ou marcada como oculta); religar a chave devolve as telas.
    /// </summary>
    public static HashSet<int> ApplyFeatureDependencies(IReadOnlyCollection<MenuNodeSource> items, IReadOnlySet<int> hiddenIds)
    {
        var result = hiddenIds.ToHashSet();
        var byKey = items
            .GroupBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        foreach (var (featureKey, dependents) in DefaultMenuCatalog.FeatureDependencies)
        {
            if (byKey.TryGetValue(featureKey, out var feature) && feature.IsActive && !hiddenIds.Contains(feature.Id))
            {
                continue;
            }

            foreach (var dependent in dependents)
            {
                if (byKey.TryGetValue(dependent, out var item))
                {
                    result.Add(item.Id);
                }
            }
        }

        return result;
    }

    /// <summary>Chave de permissão da qual a tela depende (null quando não depende de nenhuma).</summary>
    public static string? FeatureFor(string key) =>
        DefaultMenuCatalog.FeatureDependencies
            .FirstOrDefault(pair => pair.Value.Contains(key, StringComparer.OrdinalIgnoreCase))
            .Key;

    /// <summary>Ordena a árvore em lista plana (pai antes dos filhos) com a profundidade de cada item.</summary>
    public static List<(MenuNodeSource Item, int Depth)> Flatten(IReadOnlyCollection<MenuNodeSource> items)
    {
        var byParent = items
            .GroupBy(item => item.ParentId ?? 0)
            .ToDictionary(group => group.Key, group => group.OrderBy(item => item.Order).ThenBy(item => item.Title).ToList());
        var result = new List<(MenuNodeSource, int)>();
        Walk(0, 0);
        return result;

        void Walk(int parentId, int depth)
        {
            if (!byParent.TryGetValue(parentId, out var children))
            {
                return;
            }

            foreach (var child in children)
            {
                result.Add((child, depth));
                Walk(child.Id, depth + 1);
            }
        }
    }
}
