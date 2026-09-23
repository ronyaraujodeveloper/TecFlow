namespace TecFlow.Business.Configuration;

/// <summary>Configuração do encurtador interno TecFlow (ex.: tflow.link/{storeSlug}/{code}).</summary>
public class ShortLinkOptions
{
    public const string SectionName = "TecFlow:ShortLinks";

    /// <summary>Host público do redirect (sem barra final). Ex.: http://localhost:5001</summary>
    public string PublicBaseUrl { get; set; } = "http://localhost:5001";

    public int ShortCodeLength { get; set; } = 7;
}
