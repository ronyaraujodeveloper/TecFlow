namespace TecFlow.Business.Dto;

/// <summary>Contrato base opcional; preferir [Nome]ResponseDto por entidade.</summary>
public class ResponseDto
{
    public bool Status { get; set; }
    public string Descricao { get; set; } = string.Empty;
}
