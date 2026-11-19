using Microsoft.AspNetCore.Components.Authorization;
using Ofertio.Business.Dto;
using Ofertio.Business.Service.Master;
using Ofertio.SharedUi.Services.Auth;
using Ofertio.SharedUi.Services.Master;

namespace Ofertio.SharedUi.Services.Navigation;

/// <summary>
/// Acesso efetivo do usuário à hierarquia Grupo → Tela → Componente de /master/menu-permissions.
/// Uma instância por circuito: sidebar, guarda de rota e AccessGate compartilham o mesmo resultado.
/// </summary>
public interface IAccessControlService
{
    /// <summary>Carrega (ou reaproveita, se recente) o acesso do usuário logado.</summary>
    Task<ClientAccessDto?> EnsureLoadedAsync(bool forceReload = false);

    /// <summary>False só quando a chave está negada; sem resposta da API, libera (a API é a barreira real).</summary>
    bool IsAllowed(string key);

    bool IsRouteDenied(string path);
}

public sealed class AccessControlService : IAccessControlService
{
    private static readonly TimeSpan Freshness = TimeSpan.FromSeconds(30);

    private readonly ISystemCommunicationApiService _api;
    private readonly AuthenticationStateProvider _authentication;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private ClientAccessDto? _access;
    private HashSet<string> _deniedKeys = new(StringComparer.OrdinalIgnoreCase);
    private DateTime _loadedAt;

    public AccessControlService(ISystemCommunicationApiService api, AuthenticationStateProvider authentication)
    {
        _api = api;
        _authentication = authentication;
    }

    public async Task<ClientAccessDto?> EnsureLoadedAsync(bool forceReload = false)
    {
        if (!forceReload && _access is not null && DateTime.UtcNow - _loadedAt < Freshness)
        {
            return _access;
        }

        await _gate.WaitAsync();
        try
        {
            if (!forceReload && _access is not null && DateTime.UtcNow - _loadedAt < Freshness)
            {
                return _access;
            }

            var user = (await _authentication.GetAuthenticationStateAsync()).User;
            if (user.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            var clientView = MasterAccess.IsMaster(user) && !MasterAccess.IsMasterView(user);
            var result = await _api.AccessAsync(clientView);
            if (result.Success && result.Data is not null)
            {
                _access = result.Data;
                _deniedKeys = new HashSet<string>(result.Data.DeniedKeys, StringComparer.OrdinalIgnoreCase);
                _loadedAt = DateTime.UtcNow;
            }

            return _access;
        }
        finally
        {
            _gate.Release();
        }
    }

    public bool IsAllowed(string key) =>
        _access is null || _access.Unrestricted || !_deniedKeys.Contains(key);

    public bool IsRouteDenied(string path) =>
        _access is { Unrestricted: false } access
        && MenuHierarchyRules.IsRouteDenied(path, access.AllowedRoutes, access.DeniedRoutes);
}
