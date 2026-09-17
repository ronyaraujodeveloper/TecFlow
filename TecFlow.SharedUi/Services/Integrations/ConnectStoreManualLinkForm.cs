using System.Globalization;
using TecFlow.Core.Enums;

namespace TecFlow.SharedUi.Services.Integrations;

/// <summary>Validação do formulário de vinculação manual (Authorization code + Shop ID).</summary>
public static class ConnectStoreManualLinkForm
{
    public static bool TryValidate(
        MarketplaceType platform,
        string? friendlyName,
        string? authorizationCode,
        string? shopId,
        out string authorizationCodeTrimmed,
        out string shopIdNormalized,
        out string? errorMessage)
    {
        authorizationCodeTrimmed = string.Empty;
        shopIdNormalized = string.Empty;
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(friendlyName))
        {
            errorMessage = "Informe um apelido para identificar a loja.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(authorizationCode) || string.IsNullOrWhiteSpace(shopId))
        {
            errorMessage = "Informe authorization code e Shop ID para vinculação manual.";
            return false;
        }

        authorizationCodeTrimmed = authorizationCode.Trim();
        shopIdNormalized = shopId.Trim();

        if (platform == MarketplaceType.Shopee)
        {
            if (!long.TryParse(
                    shopIdNormalized,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var shopIdValue)
                || shopIdValue <= 0)
            {
                errorMessage = "Shop ID da Shopee deve ser um número inteiro (ex.: 123456).";
                return false;
            }

            shopIdNormalized = shopIdValue.ToString(CultureInfo.InvariantCulture);
        }

        return true;
    }
}
