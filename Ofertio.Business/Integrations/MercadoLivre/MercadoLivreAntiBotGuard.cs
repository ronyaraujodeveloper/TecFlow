using Ofertio.Business.Service.Radar;

namespace Ofertio.Business.Integrations.MercadoLivre;

/// <summary>
/// Depois que a página pública do Mercado Livre devolve o desafio anti-robô, suspende os GETs nela
/// (anúncio e vitrine) por <see cref="Cooldown"/>; nesse período só a API oficial é consultada.
/// O bloqueio é por IP do servidor, por isso a trava é única (singleton) para todos os inquilinos.
/// </summary>
public sealed class MercadoLivreAntiBotGuard
{
    public static readonly TimeSpan Cooldown = TimeSpan.FromMinutes(15);

    public const string AntiBotSource = "AntiBot";

    private readonly TimeProvider _clock;
    private long _blockedUntilUtcTicks;

    public MercadoLivreAntiBotGuard(TimeProvider? clock = null)
    {
        _clock = clock ?? TimeProvider.System;
    }

    public bool IsBlocked => _clock.GetUtcNow().UtcTicks < Interlocked.Read(ref _blockedUntilUtcTicks);

    public DateTimeOffset BlockedUntil => new(Interlocked.Read(ref _blockedUntilUtcTicks), TimeSpan.Zero);

    public void Trip() =>
        Interlocked.Exchange(ref _blockedUntilUtcTicks, _clock.GetUtcNow().Add(Cooldown).UtcTicks);

    /// <summary>Desafio anti-robô (account-verification, bot_challenge...) no lugar da página do anúncio.</summary>
    public static bool IsChallengePage(string? finalUrl, string? html) =>
        OfficialCatalogSearchRules.IsMercadoLivreVerificationPage(finalUrl, null)
        || (OfficialCatalogSearchRules.IsMercadoLivreVerificationPage(null, html)
            && !html!.Contains("ui-pdp", StringComparison.OrdinalIgnoreCase));
}
