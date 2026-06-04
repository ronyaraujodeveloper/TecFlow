namespace TecFlow.Business.Dto;

public class TenantDto
{
    public string Name { get; set; } = string.Empty;
    public string? DocumentNumber { get; set; }
    public bool IsActive { get; set; } = true;
}
