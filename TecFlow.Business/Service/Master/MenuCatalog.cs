using TecFlow.Business.Dto;

namespace TecFlow.Business.Service.Master;

public sealed record MenuItemDefinition(
    string Key,
    string Title,
    string? Route,
    string Icon,
    string? ParentKey,
    int Order,
    IReadOnlyList<string>? HiddenForPlans = null);

/// <summary>Árvore padrão da sidebar do cliente. Novos itens daqui são inseridos em MenuItems automaticamente.</summary>
public static class DefaultMenuCatalog
{
    public const string RadarOffers = "integracoes.radar";
    public const string RadarProfile = "integracoes.radar-perfil";

    private static readonly IReadOnlyList<string> RadarPlans = [SaasPlanCodes.Starter];

    public static readonly IReadOnlyList<MenuItemDefinition> All =
    [
        new("painel", "Painel", "/dashboard", "bi-speedometer2", null, 10),

        new("mensageria", "Mensageria & Bots", null, "bi-chat-dots", null, 20),
        new("mensageria.conexoes", "Conexões (WA & Telegram)", "/integracoes/conexoes", "bi-qr-code", "mensageria", 10),
        new("mensageria.agendador", "Agendador de Grupos", "/integracoes/agendador", "bi-calendar-week", "mensageria", 20),
        new("mensageria.bot", "Bot de Conversão", "/integracoes/bot-conversor", "bi-robot", "mensageria", 30),

        new("whatsapp", "WhatsApp", null, "bi-whatsapp", null, 30),
        new("whatsapp.agendador", "Agendador WhatsApp", "/integracoes/whatsapp/agendador", "bi-send", "whatsapp", 10),
        new("whatsapp.monitorados", "Grupos monitorados", "/integracoes/whatsapp/grupos/monitorados", "bi-eye", "whatsapp", 20),

        new("telegram", "Telegram", null, "bi-telegram", null, 40),
        new("telegram.agendador", "Agendador Telegram", "/integracoes/telegram/agendador", "bi-send", "telegram", 10),
        new("telegram.monitorados", "Grupos monitorados", "/integracoes/telegram/grupos/monitorados", "bi-eye", "telegram", 20),

        new("integracoes", "Integrações", null, "bi-plug", null, 50),
        new("integracoes.lojas", "Minhas Lojas / Integrações", "/marketplace-accounts", "bi-shop", "integracoes", 10),
        new("integracoes.gerador", "Gerador de Links", "/gerador-links", "bi-link-45deg", "integracoes", 20),
        new("integracoes.busca", "Buscar Ofertas", "/busca", "bi-search", "integracoes", 30),
        new(RadarOffers, "Radar de Ofertas", "/radar-ofertas", "bi-broadcast", "integracoes", 40, RadarPlans),
        new(RadarProfile, "Perfil de mineração", "/radar/perfil", "bi-sliders", "integracoes", 50, RadarPlans),
        new("integracoes.saude-ofertas", "Saúde de ofertas", "/saude/ofertas", "bi-heart-pulse", "integracoes", 60),
        new("integracoes.lucro-grupo", "Lucro por grupo", "/atribuicao/grupos", "bi-pie-chart", "integracoes", 70),
        new("integracoes.evergreen", "Biblioteca Evergreen", "/biblioteca-evergreen", "bi-recycle", "integracoes", 80),
        new("integracoes.estudio", "Estúdio de mídia", "/estudio-midia", "bi-image", "integracoes", 90),
        new("integracoes.saude-agendamentos", "Saúde de agendamentos", "/saude/agendamentos", "bi-shield-check", "integracoes", 100),
        new("integracoes.achadinhos", "Achadinhos VIP", "/achadinhos", "bi-stars", "integracoes", 110),
        new("integracoes.paginas", "Minhas Páginas Públicas", "/minhas-paginas-publicas", "bi-globe2", "integracoes", 120),

        new("novidades", "Novidades", "/novidades", "bi-megaphone", null, 60),
        new("conta", "Minha conta / Segurança", "/minha-conta", "bi-shield-lock", null, 70)
    ];

    public static MenuItemDefinition? Find(string key) =>
        All.FirstOrDefault(item => string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase));

    /// <summary>Árvore completa do catálogo (fallback da UI quando a API não responde).</summary>
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
                true))
            .ToList();
        return MenuVisibilityRules.BuildTree(flat, new HashSet<int>());
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
    bool IsActive);

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
