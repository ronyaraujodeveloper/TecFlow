using System.Text.Json;
using Ofertio.Business.Interfaces.Services;

namespace Ofertio.Business.Service.Security;

/// <summary>Quando um login social pode ser ligado sozinho a uma conta existente com o mesmo e-mail.</summary>
public static class SocialAccountLinkRules
{
    public const string EmailNotVerifiedMessage =
        "Já existe uma conta com este e-mail. Entre com sua senha e vincule o provedor em Segurança da conta.";

    /// <summary>
    /// Só vincula pelo e-mail quando o provedor garante que o e-mail é do dono do token.
    /// Sem isso, quem cria uma conta social com o e-mail da vítima herdaria a conta dela.
    /// </summary>
    public static bool CanAutoLinkByEmail(SocialAuthTokenPayload payload) => payload.EmailVerified;

    /// <summary>Aceita o claim email_verified como booleano ou como texto ("true"), formatos usados por Google e Apple.</summary>
    public static bool IsTrue(object? value) => value switch
    {
        bool flag => flag,
        string text => bool.TryParse(text.Trim(), out var parsed) && parsed,
        JsonElement { ValueKind: JsonValueKind.True } => true,
        JsonElement { ValueKind: JsonValueKind.String } element => IsTrue(element.GetString()),
        _ => false
    };
}
