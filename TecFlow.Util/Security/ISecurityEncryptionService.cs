namespace TecFlow.Util.Security;

/// <summary>Criptografia em repouso via ASP.NET Core Data Protection (tokens de marketplace).</summary>
public interface ISecurityEncryptionService
{
    string Protect(string plainText);

    /// <summary>Abre valores <c>DP1:</c>, aceita o legado AES <c>ENC1:</c> e devolve texto puro legado como está.</summary>
    string Unprotect(string storedValue);
}
