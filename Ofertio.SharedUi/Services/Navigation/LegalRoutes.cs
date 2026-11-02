namespace Ofertio.SharedUi.Services.Navigation;

/// <summary>Páginas legais públicas (sem login) e dados da empresa responsável pelo Ofertio.</summary>
public static class LegalRoutes
{
    public const string Terms = "/legal/termos";
    public const string Privacy = "/legal/privacidade";
    public const string Security = "/legal/seguranca";

    public const string ProductName = "Ofertio";
    public const string CompanyName = "TECSO TECNOLOGIA DE INFORMATICA LTDA";
    public const string CompanyCnpj = "52.609.371/0001-79";
    public const string CompanyCity = "São Paulo - SP";
    public const string DataProtectionOfficer = "Rony Faria de Araujo";

    /// <summary>Provisório até existir um e-mail do domínio ofertio.com.br.</summary>
    public const string ContactEmail = "rony.araujo@gmail.com";

    public const string ProductionUrl = "https://ofertio.com.br";
    public const string LastUpdated = "2 de novembro de 2026";
}
