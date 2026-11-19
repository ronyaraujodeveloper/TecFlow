namespace Ofertio.Business.Service.PublicPages;

/// <summary>
/// Validação da foto do afiliado. Confere o conteúdo do arquivo (não só a extensão) e aceita apenas JPEG, PNG e WebP.
/// SVG é recusado de propósito: pode carregar script.
/// </summary>
public static class AvatarImageRules
{
    public const int MaxBytes = 2 * 1024 * 1024;

    public const string TooLargeMessage = "A foto precisa ter até 2 MB.";
    public const string InvalidTypeMessage = "Envie uma foto JPG, PNG ou WebP.";

    /// <summary>Extensão (com ponto) pelo conteúdo do arquivo; nulo se não for um formato aceito.</summary>
    public static string? DetectExtension(ReadOnlySpan<byte> header)
    {
        if (header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        {
            return ".jpg";
        }

        if (header.Length >= 8
            && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47
            && header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A)
        {
            return ".png";
        }

        if (header.Length >= 12
            && header[0] == (byte)'R' && header[1] == (byte)'I' && header[2] == (byte)'F' && header[3] == (byte)'F'
            && header[8] == (byte)'W' && header[9] == (byte)'E' && header[10] == (byte)'B' && header[11] == (byte)'P')
        {
            return ".webp";
        }

        return null;
    }

    /// <summary>Mensagem de erro se a foto não serve; nulo se está tudo certo.</summary>
    public static string? Validate(long length, ReadOnlySpan<byte> header)
    {
        if (length <= 0)
        {
            return InvalidTypeMessage;
        }

        if (length > MaxBytes)
        {
            return TooLargeMessage;
        }

        return DetectExtension(header) is null ? InvalidTypeMessage : null;
    }

    /// <summary>Nome do arquivo no disco: aleatório, sem nada do arquivo original.</summary>
    public static string BuildFileName(string extension) => Guid.NewGuid().ToString("N") + extension;

    public static string BuildPublicUrl(Guid tenantId, string fileName) =>
        $"/uploads/avatars/{tenantId:N}/{fileName}";

    /// <summary>Caminho relativo seguro dentro da pasta de uploads, a partir da URL pública; nulo se não for uma foto nossa.</summary>
    public static string? ToRelativeStoragePath(string? publicUrl)
    {
        const string prefix = "/uploads/avatars/";
        if (string.IsNullOrWhiteSpace(publicUrl) || !publicUrl.StartsWith(prefix, StringComparison.Ordinal) || publicUrl.Contains("..", StringComparison.Ordinal))
        {
            return null;
        }

        return publicUrl["/uploads/".Length..].Replace('/', Path.DirectorySeparatorChar);
    }
}
