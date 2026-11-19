namespace Ofertio.Business.Interfaces.Services;

/// <summary>Região estimada pela localização do IP. Nada aqui identifica a pessoa.</summary>
public sealed record GeoLocation(string? Country, string? Region, string? City)
{
    public bool IsEmpty => string.IsNullOrWhiteSpace(Country) && string.IsNullOrWhiteSpace(Region) && string.IsNullOrWhiteSpace(City);
}

/// <summary>Descobre país, estado e cidade a partir do IP, com uma base de localização no próprio servidor (o IP não sai daqui).</summary>
public interface IGeoLocationService
{
    /// <summary>Região estimada, ou nulo se não houver base de localização, o IP for interno ou não for encontrado.</summary>
    GeoLocation? Lookup(string? ipAddress);
}

/// <summary>Dados do visitante que chegam à API pelo encaminhamento da WebUi: usados só para contar e estimar a região.</summary>
public sealed record PublicVisitorInfo(string? IpAddress, string? UserAgent);

/// <summary>Registra visitas, conversões e entradas em grupo da página pública, só para contagem.</summary>
public interface IPublicPageEventService
{
    Task RecordAsync(
        Guid tenantId,
        int userId,
        string kind,
        PublicVisitorInfo visitor,
        int? joinLinkId = null,
        string? channel = null,
        CancellationToken cancellationToken = default);
}
