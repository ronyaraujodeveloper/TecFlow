using Microsoft.Extensions.Logging;
using Ofertio.Business.Service.Common;
using Ofertio.SharedUi.Services.Auth;

namespace Ofertio.SharedUi.Services.Time;

/// <summary>
/// Relógio do cliente no circuito Blazor: horários chegam em UTC e são exibidos/digitados no fuso do estado
/// escolhido em Minha Conta (Brasília quando não há estado), independente do fuso do servidor.
/// </summary>
public interface IUserClock
{
    string? StateCode { get; }

    BrazilStateZone Zone { get; }

    /// <summary>Agora, no horário do cliente.</summary>
    DateTime Now { get; }

    /// <summary>Carrega a UF da conta uma única vez por circuito; falhas mantêm Brasília.</summary>
    Task EnsureLoadedAsync(CancellationToken cancellationToken = default);

    void SetStateCode(string? stateCode);

    DateTime ToLocal(DateTime utc);

    DateTime? ToLocal(DateTime? utc);

    DateTime ToUtc(DateTime local);
}

public sealed class UserClock : IUserClock
{
    private readonly IAccountSecurityApiService _accountApi;
    private readonly ILogger<UserClock> _logger;
    private Task? _loading;

    public UserClock(IAccountSecurityApiService accountApi, ILogger<UserClock> logger)
    {
        _accountApi = accountApi;
        _logger = logger;
    }

    public string? StateCode { get; private set; }

    public BrazilStateZone Zone => BrazilTimeZoneRules.Resolve(StateCode);

    public DateTime Now => ToLocal(DateTime.UtcNow);

    public Task EnsureLoadedAsync(CancellationToken cancellationToken = default) =>
        _loading ??= LoadAsync(cancellationToken);

    public void SetStateCode(string? stateCode)
    {
        StateCode = BrazilTimeZoneRules.NormalizeStateCode(stateCode);
        _loading ??= Task.CompletedTask;
    }

    public DateTime ToLocal(DateTime utc) => BrazilTimeZoneRules.ToLocal(utc, StateCode);

    public DateTime? ToLocal(DateTime? utc) => utc is { } value ? ToLocal(value) : null;

    public DateTime ToUtc(DateTime local) => BrazilTimeZoneRules.ToUtc(local, StateCode);

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        try
        {
            var status = await _accountApi.GetStatusQuietlyAsync(cancellationToken);
            if (status.Status)
            {
                StateCode = BrazilTimeZoneRules.NormalizeStateCode(status.Data?.StateCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Não foi possível carregar o estado da conta; usando o horário de Brasília.");
        }
    }
}
