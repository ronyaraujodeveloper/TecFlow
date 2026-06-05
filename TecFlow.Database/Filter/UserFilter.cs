namespace TecFlow.Database.Filter;

public class UserFilter
{
    public Guid? Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
}
