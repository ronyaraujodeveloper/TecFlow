namespace TecFlow.SharedUi.Configuration;

public class OrquestradorApiOptions
{
    public const string SectionName = "OrquestradorApi";

    public string BaseUrl { get; set; } = "https://localhost:57399/";
    public int TimeoutSeconds { get; set; } = 30;
}
