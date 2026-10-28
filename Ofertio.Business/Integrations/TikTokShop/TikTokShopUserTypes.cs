namespace Ofertio.Business.Integrations.TikTokShop;

/// <summary>Valores de <c>user_type</c> devolvidos na troca do token do TikTok Shop.</summary>
public static class TikTokShopUserTypes
{
    public const int Seller = 0;

    public const int Creator = 1;

    public static bool IsCreator(int? userType) => userType == Creator;
}
