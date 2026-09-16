using System.Security.Cryptography;

namespace TecFlow.Infrastructure.Services.ShortLinks;

/// <summary>Gera códigos alfanuméricos curtos para o encurtador TecFlow.</summary>
public static class ShortLinkCodeGenerator
{
    private const string Alphabet = "abcdefghijklmnopqrstuvwxyz0123456789";

    public static string Generate(int length)
    {
        if (length is < 6 or > 8)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Comprimento deve estar entre 6 e 8 caracteres.");
        }

        Span<char> buffer = stackalloc char[length];
        Span<byte> randomBytes = stackalloc byte[length];

        RandomNumberGenerator.Fill(randomBytes);

        for (var i = 0; i < length; i++)
        {
            buffer[i] = Alphabet[randomBytes[i] % Alphabet.Length];
        }

        return new string(buffer);
    }
}
