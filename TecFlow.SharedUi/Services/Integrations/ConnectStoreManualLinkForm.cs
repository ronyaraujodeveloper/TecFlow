using System.Globalization;
using TecFlow.Core.Enums;

namespace TecFlow.SharedUi.Services.Integrations;

/// <summary>Validação do formulário de vinculação manual (Authorization code + Shop ID).</summary>
public static class ConnectStoreManualLinkForm
{
    public const long HomologDefaultShopId = 123456;
    public const string HomologDefaultAuthorizationCode = "code_teste";

    public static bool TryValidate(
        MarketplaceType platform,
        string? friendlyName,
        string? authorizationCode,
        string? shopId,
        out string authorizationCodeTrimmed,
        out string shopIdNormalized,
        out string? errorMessage,
        bool useHomologFallbacks = false)
    {
        authorizationCodeTrimmed = string.Empty;
        shopIdNormalized = string.Empty;
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(friendlyName))
        {
            errorMessage = "Informe um apelido para identificar a loja.";
            return false;
        }

        if (platform == MarketplaceType.Shopee)
        {
            authorizationCodeTrimmed = authorizationCode?.Trim() ?? string.Empty;
            shopIdNormalized = shopId?.Trim() ?? string.Empty;
            return true;
        }

        var shopIdInput = shopId?.Trim() ?? string.Empty;
        if (!long.TryParse(
                shopIdInput,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var shopIdParsed)
            || shopIdParsed <= 0)
        {
            if (useHomologFallbacks)
            {
                shopIdParsed = HomologDefaultShopId;
            }
            else if (string.IsNullOrWhiteSpace(shopIdInput) || platform == MarketplaceType.Shopee)
            {
                errorMessage = string.IsNullOrWhiteSpace(shopIdInput)
                    ? "Informe authorization code e Shop ID para vinculação manual."
                    : "Shop ID da Shopee deve ser um número inteiro (ex.: 123456).";
                return false;
            }
        }

        var code = authorizationCode?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(code))
        {
            if (useHomologFallbacks)
            {
                code = HomologDefaultAuthorizationCode;
            }
            else
            {
                errorMessage = "Informe authorization code e Shop ID para vinculação manual.";
                return false;
            }
        }

        authorizationCodeTrimmed = code;
        shopIdNormalized = shopIdParsed > 0
            ? shopIdParsed.ToString(CultureInfo.InvariantCulture)
            : shopIdInput;

        return true;
    }
}
