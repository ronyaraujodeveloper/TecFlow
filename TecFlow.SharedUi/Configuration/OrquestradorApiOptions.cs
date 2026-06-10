namespace TecFlow.SharedUi.Configuration;

public class OrquestradorApiOptions
{
    public const string SectionName = "OrquestradorApi";

    public string BaseUrl { get; set; } = "https://localhost:7001/";
    public int TimeoutSeconds { get; set; } = 30;
}
