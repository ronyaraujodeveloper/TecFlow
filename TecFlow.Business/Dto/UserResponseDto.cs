namespace TecFlow.Business.Dto;

public class UserResponseDto
{
    public bool Status { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public UserDto? Data { get; set; }
    public List<UserDto>? DataList { get; set; }
}
