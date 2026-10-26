using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Ofertio.Business.Dto;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Master;
using Ofertio.Core.Entities;
using Ofertio.Database;

namespace Ofertio.Infrastructure.Services.Master;

/// <summary>Árvore de menus persistida, matriz de visibilidade por plano e reorganização por drag &amp; drop.</summary>
public sealed class MenuPermissionService : IMenuPermissionService
{
    private const string TargetType = "MenuItem";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(60);
    private static int _cacheVersion;

    private readonly AppDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly IMasterAuditService? _audit;

    public MenuPermissionService(AppDbContext context, IMemoryCache cache, IMasterAuditService? audit = null)
    {
        _context = context;
        _cache = cache;
        _audit = audit;
    }

    public async Task<MenuPermissionMatrixDto> GetMatrixAsync(CancellationToken cancellationToken = default)
    {
        await EnsureCatalogAsync(cancellationToken);
        var items = await LoadSourcesAsync(onlyActive: false, cancellationToken);
        var plans = await _context.SaasPlans
            .AsNoTracking()
            .OrderBy(plan => plan.SortOrder)
            .ThenBy(plan => plan.Id)
            .Select(plan => new MenuPlanColumnDto { Id = plan.Id, Code = plan.Code, Name = plan.Name })
            .ToListAsync(cancellationToken);
        var permissions = await _context.PlanMenuPermissions
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var lookup = permissions.ToDictionary(item => (item.PlanId, item.MenuItemId), item => item.IsVisible);
        var byKey = items
            .GroupBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        return new MenuPermissionMatrixDto
        {
            Plans = plans,
            Rows = MenuVisibilityRules.Flatten(items)
                .Select(entry => new MenuPermissionRowDto
                {
                    IsFeatureToggle = DefaultMenuCatalog.FeatureDependencies.ContainsKey(entry.Item.Key)
                        || DefaultMenuCatalog.Find(entry.Item.Key)?.IsFeatureToggle == true,
                    ControlledTitles = DefaultMenuCatalog.FeatureDependencies.TryGetValue(entry.Item.Key, out var dependents)
                        ? dependents.Where(byKey.ContainsKey).Select(key => byKey[key].Title).ToList()
                        : [],
                    RequiresFeatureId = MenuVisibilityRules.FeatureFor(entry.Item.Key) is { } featureKey
                        && byKey.TryGetValue(featureKey, out var feature) ? feature.Id : null,
                    RequiresFeatureTitle = MenuVisibilityRules.FeatureFor(entry.Item.Key) is { } requiredKey
                        && byKey.TryGetValue(requiredKey, out var required) ? required.Title : null,
                    MenuItemId = entry.Item.Id,
                    Key = entry.Item.Key,
                    Title = entry.Item.Title,
                    Route = entry.Item.Route,
                    Icon = entry.Item.Icon,
                    Badge = entry.Item.Badge,
                    ParentId = entry.Item.ParentId,
                    Depth = entry.Depth,
                    IsGroup = entry.Item.IsFolder,
                    IsCustomFolder = entry.Item.IsFolder && entry.Item.Key.StartsWith(DefaultMenuCatalog.CustomFolderPrefix, StringComparison.Ordinal),
                    IsActive = entry.Item.IsActive,
                    Visibility = plans.ToDictionary(
                        plan => plan.Id,
                        plan => !lookup.TryGetValue((plan.Id, entry.Item.Id), out var visible) || visible)
                })
                .ToList()
        };
    }

    public async Task<MasterOperationResultDto> SaveMatrixAsync(
        MasterActor actor,
        MenuPermissionSaveDto request,
        CancellationToken cancellationToken = default)
    {
        var itemIds = await _context.MenuItems.Select(item => item.Id).ToListAsync(cancellationToken);
        var planIds = await _context.SaasPlans.Select(plan => plan.Id).ToListAsync(cancellationToken);
        var validItems = itemIds.ToHashSet();
        var validPlans = planIds.ToHashSet();
        var existing = await _context.PlanMenuPermissions.ToListAsync(cancellationToken);
        var byKey = existing.ToDictionary(item => (item.PlanId, item.MenuItemId));
        var changed = 0;

        foreach (var cell in request.Cells.Where(cell => validItems.Contains(cell.MenuItemId) && validPlans.Contains(cell.PlanId)))
        {
            if (byKey.TryGetValue((cell.PlanId, cell.MenuItemId), out var row))
            {
                if (row.IsVisible != cell.IsVisible)
                {
                    row.IsVisible = cell.IsVisible;
                    row.Touch();
                    changed++;
                }

                continue;
            }

            var created = new PlanMenuPermission { PlanId = cell.PlanId, MenuItemId = cell.MenuItemId, IsVisible = cell.IsVisible };
            _context.PlanMenuPermissions.Add(created);
            byKey[(cell.PlanId, cell.MenuItemId)] = created;
            changed++;
        }

        if (request.ActiveItems.Count > 0)
        {
            var ids = request.ActiveItems.Keys.ToList();
            var items = await _context.MenuItems.Where(item => ids.Contains(item.Id)).ToListAsync(cancellationToken);
            foreach (var item in items.Where(item => item.IsActive != request.ActiveItems[item.Id]))
            {
                item.IsActive = request.ActiveItems[item.Id];
                item.Touch();
                changed++;
            }
        }

        if (changed == 0)
        {
            return new MasterOperationResultDto { Success = true, Message = "Nenhuma alteração no menu." };
        }

        await _context.SaveChangesAsync(cancellationToken);
        InvalidateCache();
        await AuditAsync(actor, MasterAuditActions.MenuPermissionsSaved, null,
            $"{changed} alteração(ões) na matriz de menus por plano.", cancellationToken);

        return new MasterOperationResultDto
        {
            Success = true,
            Message = $"{changed} alteração(ões) salva(s). A sidebar dos clientes já reflete a mudança."
        };
    }

    public async Task<IReadOnlyList<ClientMenuNodeDto>> GetClientMenuAsync(
        int userId,
        bool clientView = false,
        CancellationToken cancellationToken = default)
    {
        var (isMaster, planId) = await ResolveAudienceAsync(userId, cancellationToken);
        var seesEverything = isMaster && !clientView;
        var audience = seesEverything ? "master" : planId?.ToString() ?? "sem-plano";
        var cacheKey = $"ofertio:menu:{Volatile.Read(ref _cacheVersion)}:{audience}";
        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<ClientMenuNodeDto>? cached) && cached is not null)
        {
            return cached;
        }

        await EnsureCatalogAsync(cancellationToken);
        var items = await LoadSourcesAsync(onlyActive: true, cancellationToken);
        var hidden = seesEverything
            ? new HashSet<int>()
            : MenuVisibilityRules.ApplyFeatureDependencies(
                items,
                planId is int id
                    ? await HiddenForPlanAsync(id, cancellationToken)
                    : await HiddenForEveryPlanAsync(cancellationToken));

        IReadOnlyList<ClientMenuNodeDto> tree = MenuVisibilityRules.BuildTree(items, hidden);
        _cache.Set(cacheKey, tree, CacheDuration);
        return tree;
    }

    public async Task<MasterOperationResultDto> MoveAsync(MasterActor actor, MenuMoveDto request, CancellationToken cancellationToken = default)
    {
        var items = await _context.MenuItems.ToListAsync(cancellationToken);
        var item = items.FirstOrDefault(entry => entry.Id == request.ItemId);
        var target = items.FirstOrDefault(entry => entry.Id == request.TargetId);
        if (item is null || target is null)
        {
            return Fail("Item do menu não encontrado. Recarregue a página.");
        }

        if (item.Id == target.Id)
        {
            return Fail("Solte o item em outra posição.");
        }

        if (DefaultMenuCatalog.FeatureDependencies.ContainsKey(item.Key) && request.Position == MenuDropPositions.Into)
        {
            return Fail("Chaves de permissão não podem ser movidas para dentro de pastas.");
        }

        int? newParentId;
        List<int> order;
        switch (request.Position)
        {
            case MenuDropPositions.Into:
                if (!target.IsFolder)
                {
                    return Fail("Para agrupar dois itens, crie uma nova pasta.");
                }

                newParentId = target.Id;
                order = Siblings(items, target.Id).Where(id => id != item.Id).Append(item.Id).ToList();
                break;
            case MenuDropPositions.Before:
            case MenuDropPositions.After:
                newParentId = target.ParentId;
                order = MenuTreeEditRules.InsertRelativeTo(
                    Siblings(items, newParentId), item.Id, target.Id, request.Position == MenuDropPositions.After);
                break;
            default:
                return Fail("Posição de destino inválida.");
        }

        if (newParentId is int parentId && MenuTreeEditRules.IsSelfOrDescendant(ParentMap(items), parentId, item.Id))
        {
            return Fail("Não é possível mover uma pasta para dentro dela mesma.");
        }

        if (item.ParentId != newParentId)
        {
            item.ParentId = newParentId;
            item.Parent = newParentId is null ? null : items.First(entry => entry.Id == newParentId);
        }

        ApplyOrder(items, order);
        item.IsCustomized = true;
        item.Touch();
        await _context.SaveChangesAsync(cancellationToken);
        InvalidateCache();

        var destination = newParentId is null ? "raiz do menu" : $"pasta \"{items.First(entry => entry.Id == newParentId).Title}\"";
        await AuditAsync(actor, MasterAuditActions.MenuStructureChanged, item.Id.ToString(),
            $"\"{item.Title}\" movido ({request.Position} \"{target.Title}\") para {destination}.", cancellationToken);
        return new MasterOperationResultDto { Success = true, Message = $"\"{item.Title}\" movido para {destination}." };
    }

    public async Task<MasterOperationResultDto> CreateFolderAsync(
        MasterActor actor,
        MenuFolderCreateDto request,
        CancellationToken cancellationToken = default)
    {
        var titleError = MenuTreeEditRules.ValidateFolderTitle(request.Title);
        if (titleError is not null)
        {
            return Fail(titleError);
        }

        var items = await _context.MenuItems.ToListAsync(cancellationToken);
        var item = items.FirstOrDefault(entry => entry.Id == request.ItemId);
        var target = items.FirstOrDefault(entry => entry.Id == request.TargetId);
        if (item is null || target is null)
        {
            return Fail("Item do menu não encontrado. Recarregue a página.");
        }

        if (item.Id == target.Id)
        {
            return Fail("Solte o item sobre outro item para criar a pasta.");
        }

        if (target.IsFolder)
        {
            return await MoveAsync(actor, new MenuMoveDto { ItemId = item.Id, TargetId = target.Id, Position = MenuDropPositions.Into }, cancellationToken);
        }

        if (MenuTreeEditRules.IsSelfOrDescendant(ParentMap(items), target.Id, item.Id))
        {
            return Fail("Não é possível agrupar uma pasta com um item que está dentro dela.");
        }

        var folder = new MenuItem
        {
            Key = DefaultMenuCatalog.CustomFolderPrefix + Guid.NewGuid().ToString("N")[..12],
            Title = request.Title.Trim(),
            Icon = "bi-folder2",
            IsFolder = true,
            ParentId = target.ParentId,
            OrderIndex = target.OrderIndex,
            IsCustomized = true
        };
        _context.MenuItems.Add(folder);

        target.Parent = folder;
        target.OrderIndex = MenuTreeEditRules.OrderStep;
        target.IsCustomized = true;
        target.Touch();
        item.Parent = folder;
        item.OrderIndex = MenuTreeEditRules.OrderStep * 2;
        item.IsCustomized = true;
        item.Touch();

        await _context.SaveChangesAsync(cancellationToken);
        InvalidateCache();
        await AuditAsync(actor, MasterAuditActions.MenuStructureChanged, folder.Id.ToString(),
            $"Pasta \"{folder.Title}\" criada com \"{target.Title}\" e \"{item.Title}\".", cancellationToken);
        return new MasterOperationResultDto { Success = true, Message = $"Pasta \"{folder.Title}\" criada com \"{target.Title}\" e \"{item.Title}\"." };
    }

    public async Task<MasterOperationResultDto> DeleteFolderAsync(MasterActor actor, int folderId, CancellationToken cancellationToken = default)
    {
        var folder = await _context.MenuItems.FirstOrDefaultAsync(item => item.Id == folderId, cancellationToken);
        if (folder is null || !folder.IsFolder || !folder.Key.StartsWith(DefaultMenuCatalog.CustomFolderPrefix, StringComparison.Ordinal))
        {
            return Fail("Somente pastas criadas no Master podem ser excluídas.");
        }

        if (await _context.MenuItems.AnyAsync(item => item.ParentId == folderId, cancellationToken))
        {
            return Fail("Mova os itens para fora da pasta antes de excluí-la.");
        }

        var permissions = await _context.PlanMenuPermissions.Where(item => item.MenuItemId == folderId).ToListAsync(cancellationToken);
        _context.PlanMenuPermissions.RemoveRange(permissions);
        _context.MenuItems.Remove(folder);
        await _context.SaveChangesAsync(cancellationToken);
        InvalidateCache();
        await AuditAsync(actor, MasterAuditActions.MenuStructureChanged, folderId.ToString(), $"Pasta \"{folder.Title}\" excluída.", cancellationToken);
        return new MasterOperationResultDto { Success = true, Message = $"Pasta \"{folder.Title}\" excluída." };
    }

    /// <summary>
    /// Sincroniza MenuItems com o catálogo padrão (idempotente): insere itens novos e alinha título, rota, ícone e selo.
    /// Pai e ordem só seguem o catálogo enquanto o item não foi reposicionado no Master (IsCustomized).
    /// </summary>
    public async Task EnsureCatalogAsync(CancellationToken cancellationToken = default)
    {
        var existing = await _context.MenuItems.ToListAsync(cancellationToken);
        var byKey = existing.ToDictionary(item => item.Key, StringComparer.OrdinalIgnoreCase);
        List<SaasPlan>? plans = null;
        var changed = false;

        foreach (var definition in DefaultMenuCatalog.All)
        {
            var parent = definition.ParentKey is not null && byKey.TryGetValue(definition.ParentKey, out var found) ? found : null;
            if (!byKey.TryGetValue(definition.Key, out var item))
            {
                item = new MenuItem
                {
                    Key = definition.Key,
                    Title = definition.Title,
                    Route = definition.Route,
                    Icon = definition.Icon,
                    Badge = definition.Badge,
                    IsFolder = definition.IsFolder,
                    OrderIndex = definition.Order,
                    Parent = parent
                };
                _context.MenuItems.Add(item);
                byKey[item.Key] = item;
                changed = true;

                plans ??= await _context.SaasPlans.ToListAsync(cancellationToken);
                foreach (var plan in plans.Where(plan => definition.HiddenForAllPlans
                    || definition.HiddenForPlans?.Contains(plan.Code, StringComparer.OrdinalIgnoreCase) == true))
                {
                    _context.PlanMenuPermissions.Add(new PlanMenuPermission { Plan = plan, MenuItem = item, IsVisible = false });
                }

                continue;
            }

            var contentChanged = item.Title != definition.Title
                || item.Route != definition.Route
                || item.Icon != definition.Icon
                || item.Badge != definition.Badge
                || item.IsFolder != definition.IsFolder;
            var sameParent = parent is null ? item.ParentId is null : parent.Id != 0 && item.ParentId == parent.Id;
            var structureChanged = !item.IsCustomized && (!sameParent || item.OrderIndex != definition.Order);
            if (!contentChanged && !structureChanged)
            {
                continue;
            }

            item.Title = definition.Title;
            item.Route = definition.Route;
            item.Icon = definition.Icon;
            item.Badge = definition.Badge;
            item.IsFolder = definition.IsFolder;
            if (structureChanged)
            {
                item.OrderIndex = definition.Order;
                item.Parent = parent;
                if (parent is null)
                {
                    item.ParentId = null;
                }
            }

            item.Touch();
            changed = true;
        }

        if (!changed)
        {
            return;
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            InvalidateCache();
        }
        catch (DbUpdateException)
        {
            _context.ChangeTracker.Clear();
        }
    }

    private static List<int> Siblings(IEnumerable<MenuItem> items, int? parentId) =>
        items.Where(entry => entry.ParentId == parentId)
            .OrderBy(entry => entry.OrderIndex)
            .ThenBy(entry => entry.Title)
            .Select(entry => entry.Id)
            .ToList();

    private static Dictionary<int, int?> ParentMap(IEnumerable<MenuItem> items) =>
        items.ToDictionary(entry => entry.Id, entry => entry.ParentId);

    private static void ApplyOrder(IReadOnlyCollection<MenuItem> items, IReadOnlyList<int> order)
    {
        for (var index = 0; index < order.Count; index++)
        {
            var entry = items.First(item => item.Id == order[index]);
            var orderIndex = (index + 1) * MenuTreeEditRules.OrderStep;
            if (entry.OrderIndex == orderIndex && entry.IsCustomized)
            {
                continue;
            }

            entry.OrderIndex = orderIndex;
            entry.IsCustomized = true;
            entry.Touch();
        }
    }

    private async Task<List<MenuNodeSource>> LoadSourcesAsync(bool onlyActive, CancellationToken cancellationToken)
    {
        var query = _context.MenuItems.AsNoTracking();
        if (onlyActive)
        {
            query = query.Where(item => item.IsActive);
        }

        return await query
            .Select(item => new MenuNodeSource(
                item.Id, item.Key, item.Title, item.Route, item.Icon, item.ParentId, item.OrderIndex, item.IsActive, item.IsFolder, item.Badge))
            .ToListAsync(cancellationToken);
    }

    private async Task<HashSet<int>> HiddenForPlanAsync(int planId, CancellationToken cancellationToken) =>
        (await _context.PlanMenuPermissions
            .AsNoTracking()
            .Where(item => item.PlanId == planId && !item.IsVisible)
            .Select(item => item.MenuItemId)
            .ToListAsync(cancellationToken)).ToHashSet();

    /// <summary>Itens que nenhum plano ativo enxerga (ex.: roadmap) — aplicado a quem não tem plano resolvido.</summary>
    private async Task<HashSet<int>> HiddenForEveryPlanAsync(CancellationToken cancellationToken)
    {
        var activePlans = await _context.SaasPlans.AsNoTracking().Where(plan => plan.IsActive).Select(plan => plan.Id).ToListAsync(cancellationToken);
        if (activePlans.Count == 0)
        {
            return [];
        }

        var hidden = await _context.PlanMenuPermissions
            .AsNoTracking()
            .Where(item => !item.IsVisible && activePlans.Contains(item.PlanId))
            .GroupBy(item => item.MenuItemId)
            .Where(group => group.Count() == activePlans.Count)
            .Select(group => group.Key)
            .ToListAsync(cancellationToken);
        return hidden.ToHashSet();
    }

    private async Task<(bool IsMaster, int? PlanId)> ResolveAudienceAsync(int userId, CancellationToken cancellationToken)
    {
        if (userId <= 0)
        {
            return (false, null);
        }

        var user = await _context.UserAccounts
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(item => item.Id == userId)
            .Select(item => new { item.TenantId, item.Plan, item.Role })
            .FirstOrDefaultAsync(cancellationToken);
        if (user is null)
        {
            return (false, null);
        }

        if (MasterRoles.IsMaster(user.Role))
        {
            return (true, null);
        }

        var tenantPlan = await _context.Tenants
            .AsNoTracking()
            .Where(item => item.Id == user.TenantId)
            .Select(item => item.PlanCode)
            .FirstOrDefaultAsync(cancellationToken);
        var code = (string.IsNullOrWhiteSpace(tenantPlan) ? user.Plan : tenantPlan)?.Trim();
        if (string.IsNullOrWhiteSpace(code))
        {
            return (false, null);
        }

        var planId = await _context.SaasPlans
            .AsNoTracking()
            .Where(plan => plan.Code == code && plan.IsActive)
            .Select(plan => (int?)plan.Id)
            .FirstOrDefaultAsync(cancellationToken);
        return (false, planId);
    }

    private static void InvalidateCache() => Interlocked.Increment(ref _cacheVersion);

    private async Task AuditAsync(MasterActor actor, string action, string? targetId, string detail, CancellationToken cancellationToken)
    {
        if (_audit is not null)
        {
            await _audit.LogAsync(actor, action, TargetType, targetId, detail, cancellationToken);
        }
    }

    private static MasterOperationResultDto Fail(string message) => new() { Success = false, Message = message };
}
