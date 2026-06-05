namespace TecFlow.Business.Messaging;

public class EngagementKeywordTriageOptions
{
    public const string SectionName = "EngagementTriage";

    public string[] Keywords { get; set; } =
    [
        "eu quero",
        "link",
        "quero",
        "me manda"
    ];
}
