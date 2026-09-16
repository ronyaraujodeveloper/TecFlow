using TecFlow.Business.Interfaces.Services;

namespace TecFlow.Business.Service.LinkStrategies;

/// <summary>Contexto scoped da requisição de geração de link de afiliado.</summary>
public sealed class AffiliateLinkGenerationContext : IAffiliateLinkGenerationContext
{
    public int UserId { get; set; }

    public string? CustomNickname { get; set; }

    public string? ClientIpAddress { get; set; }

    public string? UserAgent { get; set; }

    public string? ReferrerUrl { get; set; }
}
