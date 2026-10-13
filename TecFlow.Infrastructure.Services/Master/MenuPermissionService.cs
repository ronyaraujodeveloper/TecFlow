using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Master;
using TecFlow.Core.Entities;
using TecFlow.Database;

namespace TecFlow.Infrastructure.Services.Master;

/// <summary>Árvore de menus persistida e matriz de visibilidade por plano.</summary>
public sealed class MenuPermissionService : IMenuPermissionService
{
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

        return new MenuPermissionMatrixDto
        {
            Plans = plans,
            Rows = MenuVisibilityRules.Flatten(items)
                .Select(entry => new MenuPermissionRowDto
                {
                    MenuItemId = entry.Item.Id,
                    Key = entry.Item.Key,
                    Title = entry.Item.Title,
                    Route = entry.Item.Route,
                    Icon = entry.Item.Icon,
                    Depth = entry.Depth,
                    IsGroup = string.IsNullOrWhiteSpace(entry.Item.Route),
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
        Interlocked.Increment(ref _cacheVersion);
        if (_audit is not null)
        {
            await _audit.LogAsync(actor, MasterAuditActions.MenuPermissionsSaved, "MenuItem", null,
                $"{changed} alteração(ões) na matriz de menus por plano.", cancellationToken);
        }

        return new MasterOperationResultDto
        {
            Success = true,
            Message = $"{changed} alteração(ões) salva(s). A sidebar dos clientes atualiza em até 1 minuto."
        };
    }

    public async Task<IReadOnlyList<ClientMenuNodeDto>> GetClientMenuAsync(int userId, CancellationToken cancellationToken = default)
    {
        var planId = await ResolvePlanIdAsync(userId, cancellationToken);
        var cacheKey = $"tecflow:menu:{Volatile.Read(ref _cacheVersion)}:{planId?.ToString() ?? "all"}";
        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<ClientMenuNodeDto>? cached) && cached is not null)
        {
            return cached;
        }

        await EnsureCatalogAsync(cancellationToken);
        var items = await LoadSourcesAsync(onlyActive: true, cancellationToken);
        var hidden = planId is null
            ? new HashSet<int>()
            : (await _context.PlanMenuPermissions
                .AsNoTracking()
                .Where(item => item.PlanId == planId && !item.IsVisible)
                .Select(item => item.MenuItemId)
                .ToListAsync(cancellationToken)).ToHashSet();

        IReadOnlyList<ClientMenuNodeDto> tree = MenuVisibilityRules.BuildTree(items, hidden);
        _cache.Set(cacheKey, tree, CacheDuration);
        return tree;
    }

    /// <summary>
    /// Sincroniza MenuItems com o catálogo padrão (idempotente): insere itens novos e alinha título, rota, ícone,
    /// ordem e pai dos existentes. Visibilidade por plano e IsActive continuam sob controle do Master.
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
                    Order = definition.Order,
                    Parent = parent
                };
                _context.MenuItems.Add(item);
                byKey[item.Key] = item;
                changed = true;

                plans ??= await _context.SaasPlans.ToListAsync(cancellationToken);
                foreach (var plan in plans.Where(plan => definition.HiddenForPlans?.Contains(plan.Code, StringComparer.OrdinalIgnoreCase) == true))
                {
                    _context.PlanMenuPermissions.Add(new PlanMenuPermission { Plan = plan, MenuItem = item, IsVisible = false });
                }

                continue;
            }

            var sameParent = parent is null ? item.ParentId is null : parent.Id != 0 && item.ParentId == parent.Id;
            if (sameParent
                && item.Title == definition.Title
                && item.Route == definition.Route
                && item.Icon == definition.Icon
                && item.Order == definition.Order)
            {
                continue;
            }

            item.Title = definition.Title;
            item.Route = definition.Route;
            item.Icon = definition.Icon;
            item.Order = definition.Order;
            item.Parent = parent;
            if (parent is null)
            {
                item.ParentId = null;
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
            Interlocked.Increment(ref _cacheVersion);
        }
        catch (DbUpdateException)
        {
            _context.ChangeTracker.Clear();
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
            .Select(item => new MenuNodeSource(item.Id, item.Key, item.Title, item.Route, item.Icon, item.ParentId, item.Order, item.IsActive))
            .ToListAsync(cancellationToken);
    }

    private async Task<int?> ResolvePlanIdAsync(int userId, CancellationToken cancellationToken)
    {
        if (userId <= 0)
        {
            return null;
        }

        var user = await _context.UserAccounts
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(item => item.Id == userId)
            .Select(item => new { item.TenantId, item.Plan, item.Role })
            .FirstOrDefaultAsync(cancellationToken);
        if (user is null || MasterRoles.IsMaster(user.Role))
        {
            return null;
        }

        var tenantPlan = await _context.Tenants
            .AsNoTracking()
            .Where(item => item.Id == user.TenantId)
            .Select(item => item.PlanCode)
            .FirstOrDefaultAsync(cancellationToken);
        var code = (string.IsNullOrWhiteSpace(tenantPlan) ? user.Plan : tenantPlan)?.Trim();
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        return await _context.SaasPlans
            .AsNoTracking()
            .Where(plan => plan.Code == code && plan.IsActive)
            .Select(plan => (int?)plan.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
