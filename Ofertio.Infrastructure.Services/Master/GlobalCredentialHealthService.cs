using Microsoft.Extensions.Configuration;
using Ofertio.Business.Dto;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Master;

namespace Ofertio.Infrastructure.Services.Master;

/// <summary>Testa as credenciais de marketplace do cofre global (SystemSettings + appsettings) em uso pela API.</summary>
public sealed class GlobalCredentialHealthService : IGlobalCredentialHealthService
{
    private readonly IDeveloperAccountHealthChecker _checker;
    private readonly IConfiguration _configuration;

    public GlobalCredentialHealthService(IDeveloperAccountHealthChecker checker, IConfiguration configuration)
    {
        _checker = checker;
        _configuration = configuration;
    }

    public async Task<IReadOnlyList<DeveloperAccountCheckDto>> CheckAsync(CancellationToken cancellationToken = default)
    {
        var targets = new[]
        {
            (DeveloperPlatforms.MercadoLivre,
                Config("Integrations:MercadoLivre:AppId"),
                Config("Integrations:MercadoLivre:SecretKey"),
                (string?)null),
            (DeveloperPlatforms.Shopee,
                Config("Integrations:Shopee:AffiliateAppId", "Integrations:Shopee:AppKey", "Integrations:Shopee:PartnerId"),
                Config("Integrations:Shopee:AffiliateSecret", "Integrations:Shopee:AppSecret", "Integrations:Shopee:PartnerKey"),
                null),
            (DeveloperPlatforms.Amazon,
                Config("Integrations:AmazonPaApi:AccessKey"),
                Config("Integrations:AmazonPaApi:SecretKey"),
                Config("Integrations:AmazonPaApi:PartnerTag")),
            (DeveloperPlatforms.TikTokShop,
                Config("Integrations:TikTokShop:AppKey"),
                Config("Integrations:TikTokShop:AppSecret"),
                null)
        };

        var checks = await Task.WhenAll(targets.Select(async target =>
        {
            var result = await _checker.CheckAsync(target.Item1, target.Item2, target.Item3, target.Item4, cancellationToken);
            return new DeveloperAccountCheckDto
            {
                Platform = target.Item1,
                Source = "Chave global do cofre em uso pela API",
                Success = result.Success,
                Message = result.Message,
                LatencyMs = result.LatencyMs,
                CheckedAt = DateTime.UtcNow
            };
        }));
        return checks;
    }

    private string? Config(params string[] keys) =>
        keys.Select(key => _configuration[key]).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();
}
