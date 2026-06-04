namespace TecFlow.Business.Dto;

public class InvoicePreparationResponseDto
{
    public bool Status { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public InvoicePayloadDto? Data { get; set; }
}
