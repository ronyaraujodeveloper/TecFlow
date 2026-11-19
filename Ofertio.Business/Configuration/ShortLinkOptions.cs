namespace Ofertio.Business.Configuration;

/// <summary>Configuração do encurtador interno Ofertio (ex.: tflow.link/{storeSlug}/{code}).</summary>
public class ShortLinkOptions
{
    public const string SectionName = "Ofertio:ShortLinks";

    /// <summary>Host público do redirect (sem barra final). Ex.: http://localhost:5001</summary>
    public string PublicBaseUrl { get; set; } = "http://localhost:5001";

    public int ShortCodeLength { get; set; } = 7;

    /// <summary>Segredo da chave curta de grupo (?g=). Definir por ambiente fora do Git e não trocar depois de em uso.</summary>
    public string GroupKeySecret { get; set; } = string.Empty;
}
