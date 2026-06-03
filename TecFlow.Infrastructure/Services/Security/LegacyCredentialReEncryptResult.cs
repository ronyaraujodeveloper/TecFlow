namespace TecFlow.Infrastructure.Services.Security;

public sealed class LegacyCredentialReEncryptResult
{
    public int UsersScanned { get; init; }

    public int UsersUpdated { get; init; }

    public int FieldsEncrypted { get; init; }

    public bool DryRun { get; init; }
}
