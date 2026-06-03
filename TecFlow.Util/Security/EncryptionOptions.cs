namespace TecFlow.Util.Security;

public class EncryptionOptions
{
    public const string SectionName = "Encryption";

    /// <summary>
    /// Chave AES-256 em Base64 (32 bytes decodificados).
    /// </summary>
    public string Key { get; set; } = string.Empty;
}
