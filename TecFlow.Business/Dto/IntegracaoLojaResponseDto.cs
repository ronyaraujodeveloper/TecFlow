using TecFlow.Database.Entity;

namespace TecFlow.Business.Dto;

public class IntegracaoLojaResponseDto
{
    public bool Status { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public IntegracaoLoja? Data { get; set; }
    public List<IntegracaoLoja>? DataList { get; set; }
    public PagingInfoDto? Paging { get; set; }
}
