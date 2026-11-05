namespace Ofertio.E2E;

/// <summary>Valores da fase de conta. Nenhum deles é uma senha nem um cartão real.</summary>
public static class AccountPilot
{
    public const string InvalidEmail = "sem-arroba";

    public const string UntouchedEmail = "e2e-nao-cadastrar@example.com";

    public const string InvalidCpf = "111.111.111-11";

    public const string InvalidCnpj = "11.111.111/1111-11";

    public const string InvalidCep = "123";

    public const string InvalidCard = "4111111111111112";

    public const string TestCard = "4111 1111 1111 1111";

    public const string TestHolder = "E2E ROBOT";

    public const string TestExpiry = "12/30";

    public const string TestCvv = "123";
}
