using System.Net;
using MaxMind.GeoIP2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Ofertio.Business.Interfaces.Services;

namespace Ofertio.Infrastructure.Services.Geo;

/// <summary>
/// Região do IP pela base GeoLite2-City (arquivo .mmdb) instalada no servidor, em <c>GeoIp:DatabasePath</c>.
/// Sem o arquivo, a região fica em branco e o resto do sistema segue normalmente.
/// </summary>
public sealed class MaxMindGeoLocationService : IGeoLocationService, IDisposable
{
    public const string DatabasePathSetting = "GeoIp:DatabasePath";

    private readonly DatabaseReader? _reader;

    public MaxMindGeoLocationService(IConfiguration configuration, ILogger<MaxMindGeoLocationService> logger)
    {
        var path = configuration[DatabasePathSetting];
        if (string.IsNullOrWhiteSpace(path))
        {
            logger.LogInformation("Região por IP desativada: {Setting} não configurado.", DatabasePathSetting);
            return;
        }

        try
        {
            var full = Path.GetFullPath(path.Trim());
            if (!File.Exists(full))
            {
                logger.LogWarning("Base de localização não encontrada em {Path}. A região por IP ficará em branco.", full);
                return;
            }

            _reader = new DatabaseReader(full);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Não foi possível abrir a base de localização. A região por IP ficará em branco.");
        }
    }

    public GeoLocation? Lookup(string? ipAddress)
    {
        if (_reader is null || string.IsNullOrWhiteSpace(ipAddress) || !IPAddress.TryParse(ipAddress.Trim(), out var address) || IsPrivate(address))
        {
            return null;
        }

        try
        {
            if (!_reader.TryCity(address, out var city) || city is null)
            {
                return null;
            }

            var location = new GeoLocation(
                city.Country?.IsoCode ?? city.Country?.Name,
                city.MostSpecificSubdivision?.Names.TryGetValue("pt-BR", out var regionPt) == true
                    ? regionPt
                    : city.MostSpecificSubdivision?.Name,
                city.City?.Names.TryGetValue("pt-BR", out var cityPt) == true ? cityPt : city.City?.Name,
                city.MostSpecificSubdivision?.IsoCode);
            return location.IsEmpty ? null : location;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public void Dispose() => _reader?.Dispose();

    private static bool IsPrivate(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            return address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6UniqueLocal;
        }

        var bytes = address.GetAddressBytes();
        return bytes[0] == 10
            || (bytes[0] == 172 && bytes[1] is >= 16 and <= 31)
            || (bytes[0] == 192 && bytes[1] == 168)
            || (bytes[0] == 169 && bytes[1] == 254)
            || bytes[0] == 0;
    }
}
