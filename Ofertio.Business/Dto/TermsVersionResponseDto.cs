using Ofertio.Core.Entities;

namespace Ofertio.Business.Dto;

public class TermsVersionResponseDto
{
    public bool Status { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public TermsVersion? Data { get; set; }
    public List<TermsVersion>? DataList { get; set; }
}
