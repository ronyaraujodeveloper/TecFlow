using Microsoft.Extensions.Configuration;

namespace Ofertio.E2E;

/// <summary>
/// Homologação do robô. A senha não entra no git: User Secrets deste projeto ou variáveis de ambiente.
/// </summary>
public sealed class E2ESettings
{
    public const string RecordPrefix = "E2E-";

    public E2ESettings(Uri baseUrl, string email, string password, bool headed)
    {
        BaseUrl = baseUrl;
        Email = email;
        Password = password;
        Headed = headed;
    }

    public Uri BaseUrl { get; }

    public string Email { get; }

    public string Password { get; }

    public bool Headed { get; }

    public Uri LoginUri => new(BaseUrl, "/");

    public static string RecordName(string label) => RecordPrefix + label.Trim();

    public static bool TryLoad(out E2ESettings? settings)
    {
        settings = null;
        var config = new ConfigurationBuilder()
            .AddUserSecrets(typeof(E2ESettings).Assembly, optional: true)
            .AddEnvironmentVariables()
            .Build();

        var baseUrl = First(config["E2E:BaseUrl"], config["OFERTIO_E2E_BASE_URL"]);
        var email = First(config["E2E:Email"], config["OFERTIO_E2E_EMAIL"]);
        var password = First(config["E2E:Password"], config["OFERTIO_E2E_PASSWORD"]);
        if (string.IsNullOrWhiteSpace(baseUrl)
            || string.IsNullOrWhiteSpace(email)
            || string.IsNullOrWhiteSpace(password)
            || !Uri.TryCreate(baseUrl.Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        var headed = IsTruthy(First(config["E2E:Headed"], config["OFERTIO_E2E_HEADED"]));
        settings = new E2ESettings(uri, email.Trim(), password, headed);
        return true;
    }

    private static string? First(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private static bool IsTruthy(string? value) =>
        value is "1" or "true" or "True" or "yes" or "sim";
}
