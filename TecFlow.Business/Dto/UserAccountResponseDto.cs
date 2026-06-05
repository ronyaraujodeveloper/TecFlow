using TecFlow.Core.Entities;

namespace TecFlow.Business.Dto;

public class UserAccountResponseDto
{
    public bool Status { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public UserAccount? Data { get; set; }
    public List<UserAccount>? DataList { get; set; }
}
