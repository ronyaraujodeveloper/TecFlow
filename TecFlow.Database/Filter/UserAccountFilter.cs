namespace TecFlow.Database.Filter;

public class UserAccountFilter
{
    public int? Id { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Plan { get; set; }
    public string? WhatsAppPhone { get; set; }
}
