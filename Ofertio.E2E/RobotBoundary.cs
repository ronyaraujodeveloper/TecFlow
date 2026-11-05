namespace Ofertio.E2E;

/// <summary>
/// Limite do robô de tela. Os fontes de Ofertio.E2E não podem pedir autorização de loja,
/// QR ou sessão de WhatsApp, código do Telegram, webhook nem cobrança real.
/// A carga em massa usa outro prefixo, para a limpeza não apagá-la junto com o cenário.
/// </summary>
public static class RobotBoundary
{
    public const string BulkPrefix = "CARGA-";

    public const string SimulatedChargeNotice = "nenhuma cobrança é feita";

    public static readonly string[] ForbiddenText =
    [
        "Conectar via OAuth",
        "Gerar Conexão WhatsApp",
        "Conectar WhatsApp",
        "Validar e Conectar Bot",
        "Gerar assinatura ASAAS",
        "Recriar assinatura ASAAS",
        "accounts.google.com",
        "appleid.apple.com",
        "facebook.com/dialog/oauth",
        "auth.mercadolivre.com",
        "partner.shopeemobile.com",
        "open.tiktok.com",
        "business-api.tiktok.com",
        "asaas.com",
        "webhook"
    ];

    public static IReadOnlyList<string> FindViolations(string projectDirectory)
    {
        var violations = new List<string>();
        foreach (var file in Directory.GetFiles(projectDirectory, "*.cs"))
        {
            var name = Path.GetFileName(file);
            if (name.Equals(nameof(RobotBoundary) + ".cs", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            foreach (var forbidden in ForbiddenText)
            {
                if (text.Contains(forbidden, StringComparison.OrdinalIgnoreCase))
                {
                    violations.Add($"{name} contém \"{forbidden}\".");
                }
            }

            if (text.Contains("Assinar", StringComparison.Ordinal)
                && !text.Contains(SimulatedChargeNotice, StringComparison.Ordinal))
            {
                violations.Add($"{name} menciona Assinar sem o aviso de cobrança simulada.");
            }
        }

        return violations;
    }
}
