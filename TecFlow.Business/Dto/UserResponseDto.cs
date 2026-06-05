using TecFlow.Database.Entity;

namespace TecFlow.Business.Dto;

public class UserResponseDto
{
    public bool Status { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public UserEntity? Data { get; set; }
    public List<UserEntity>? DataList { get; set; }
}
