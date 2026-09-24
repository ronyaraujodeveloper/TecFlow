using TecFlow.Core.Enums;

namespace TecFlow.SharedUi.Services.Integrations;

/// <summary>Textos e URLs oficiais para o atalho "Como pegar meu ID?" no cadastro de loja.</summary>
public static class AffiliateTrackingIdHelp
{
    public static string GetFormatHint(MarketplaceType platform) => platform switch
    {
        MarketplaceType.Amazon =>
            "Cole sua Tag de Associados. O formato termina com -20, por exemplo sualoja-20. Se colar um link com tag=..., extraímos a tag automaticamente.",
        MarketplaceType.Shopee =>
            "Use o ID numérico do afiliado Shopee (somente dígitos, ex.: 6512300000). Pode colar um link com affiliate_id ou sub_id.",
        MarketplaceType.TikTokShop =>
            "Use o Tracking ID / sub_id da conta TikTok Shop (ex.: 6512300000). Cole o ID ou um link com sub_id=.",
        MarketplaceType.MercadoLivre =>
            "Use o Matt Tool ID (número do parâmetro matt_tool, ex.: 123456789). Cole o ID ou um link de afiliado do Mercado Livre.",
        MarketplaceType.MagazineLuiza =>
            "Use o slug da loja Magazine Você (ex.: magazinematos) ou o parâmetro parceiro. Cole o ID ou a URL completa da loja.",
        MarketplaceType.Kabum =>
            "Use o Tracking ID do programa de afiliados Kabum! (injetado em sub_id). Cole o ID ou um link com sub_id=.",
        MarketplaceType.CasasBahia =>
            "Use o ID de Parceiro Casas Bahia (parâmetro parceiro, ex.: tecflow_cb). Cole o ID ou um link com parceiro=.",
        _ => "Informe o ID de afiliado da plataforma. Se colar uma URL com o parâmetro, extraímos o valor automaticamente."
    };

    public static string GetOfficialPanelUrl(MarketplaceType platform) => platform switch
    {
        MarketplaceType.Amazon => "https://associados.amazon.com.br/",
        MarketplaceType.Shopee => "https://affiliate.shopee.com.br/",
        MarketplaceType.TikTokShop => "https://affiliate-id.tiktok.com/",
        MarketplaceType.MercadoLivre => "https://www.mercadolivre.com.br/afiliados",
        MarketplaceType.MagazineLuiza => "https://www.magazinevoce.com.br/",
        MarketplaceType.Kabum => "https://www.kabum.com.br/",
        MarketplaceType.CasasBahia => "https://www.casasbahia.com.br/",
        _ => "https://www.google.com/search?q=painel+afiliados"
    };
}
