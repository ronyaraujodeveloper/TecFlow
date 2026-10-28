using Ofertio.Business.Dto;

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
    bool IsFeatureToggle = false)
{
    /// <summary>Chave de permissão (sem rota) que libera um conjunto de telas; nunca aparece na sidebar.</summary>
    public bool IsFolder => Route is null && !IsFeatureToggle;
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
    public const string CustomPublicSlug = "integracoes.paginas.slug";

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
        new(CustomPublicSlug, "Slug personalizado", null, "bi-pencil", "integracoes.paginas", 10, HiddenForAllPlans: true, IsFeatureToggle: true),
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
                item.Badge))
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
    string? Badge = null);

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
            .Where(item => item.IsActive && !hiddenIds.Contains(item.Id))
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
