namespace TecFlow.Business.Configuration;

/// <summary>Configuração do encurtador interno TecFlow (ex.: tflow.link/r/xyz).</summary>
public class ShortLinkOptions
{
    public const string SectionName = "TecFlow:ShortLinks";

    /// <summary>Base pública do redirect (sem barra final). Ex.: http://localhost:5001/r</summary>
    public string PublicBaseUrl { get; set; } = "http://localhost:5001/r";

    public int ShortCodeLength { get; set; } = 7;
}
