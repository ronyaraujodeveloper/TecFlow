namespace TecFlow.Business.Integrations.Notifications;

public class FirebaseOptions
{
    public const string SectionName = "Firebase";

    public string? ProjectId { get; set; }
    public string? CredentialsPath { get; set; }
    public string? CredentialsJson { get; set; }
}
