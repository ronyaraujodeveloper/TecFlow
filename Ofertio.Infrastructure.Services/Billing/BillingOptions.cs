namespace Ofertio.Infrastructure.Services.Billing;

/// <summary>Seção "Billing" do appsettings.</summary>
public sealed class BillingOptions
{
    public const string SectionName = "Billing";

    /// <summary>
    /// E-mails de contas Master liberadas para percorrer Meu plano como cliente (escolher e pagar).
    /// Só para teste em homologação com o gateway simulado; deixe vazio em produção.
    /// </summary>
    public List<string> MasterCheckoutTestEmails { get; set; } = [];

    public bool IsMasterCheckoutTester(string? email) =>
        !string.IsNullOrWhiteSpace(email)
        && MasterCheckoutTestEmails.Any(item => string.Equals(item?.Trim(), email.Trim(), StringComparison.OrdinalIgnoreCase));
}
