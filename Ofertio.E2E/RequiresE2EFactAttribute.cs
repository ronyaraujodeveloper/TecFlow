namespace Ofertio.E2E;

/// <summary>
/// Pula o teste de navegador quando a homologação não está configurada, para o <c>dotnet test</c> da solução não abrir o Chrome.
/// </summary>
public sealed class RequiresE2EFactAttribute : FactAttribute
{
    public RequiresE2EFactAttribute()
    {
        if (!E2ESettings.TryLoad(out _))
        {
            Skip = "Defina E2E:BaseUrl, E2E:Email e E2E:Password nos User Secrets do Ofertio.E2E, ou as variáveis OFERTIO_E2E_BASE_URL, OFERTIO_E2E_EMAIL e OFERTIO_E2E_PASSWORD.";
        }
    }
}
