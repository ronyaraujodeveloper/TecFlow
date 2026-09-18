namespace TecFlow.Business.Dto;

/// <summary>Contrato base opcional; preferir [Nome]ResponseDto por entidade.</summary>
public class ResponseDto
{
    public bool Status { get; set; }

    public string Descricao { get; set; } = string.Empty;

    public static ResponseDto Ok(string descricao = "OK") =>
        new() { Status = true, Descricao = descricao };

    public static ResponseDto Fail(string descricao) =>
        new() { Status = false, Descricao = descricao };
}
