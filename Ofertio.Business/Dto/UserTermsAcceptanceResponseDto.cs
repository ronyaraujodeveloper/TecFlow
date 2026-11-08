using Ofertio.Core.Entities;

namespace Ofertio.Business.Dto;

public class UserTermsAcceptanceResponseDto
{
    public bool Status { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public UserTermsAcceptance? Data { get; set; }
    public List<UserTermsAcceptance>? DataList { get; set; }
    public bool RequiresReacceptance { get; set; }
    public bool RequiresExternalApiConsent { get; set; }
    public string? ActiveTermsVersion { get; set; }
    public string? ActivePrivacyVersion { get; set; }
}
