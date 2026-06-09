using System.Net;
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

/// <summary>Utilitários de telemetria para cliques em links encurtados.</summary>
public static class LinkClickTelemetryHelper
{
    public static string MaskIpAddress(string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return "desconhecido";
        }

        var normalized = ipAddress.Trim();
        if (normalized.StartsWith("::ffff:", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized["::ffff:".Length..];
        }

        if (IPAddress.TryParse(normalized, out var parsed) && parsed.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            var parts = normalized.Split('.');
            if (parts.Length == 4)
            {
                return $"{parts[0]}.{parts[1]}.{parts[2]}.***";
            }
        }

        return "***";
    }

    public static string DetectDeviceType(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return "Desconhecido";
        }

        var ua = userAgent.ToLowerInvariant();

        if (ua.Contains("ipad", StringComparison.Ordinal) || ua.Contains("tablet", StringComparison.Ordinal))
        {
            return "Tablet";
        }

        if (ua.Contains("mobile", StringComparison.Ordinal)
            || ua.Contains("android", StringComparison.Ordinal)
            || ua.Contains("iphone", StringComparison.Ordinal))
        {
            return "Mobile";
        }

        return "Desktop";
    }

    public static string? NormalizeReferrer(string? referrerUrl)
    {
        if (string.IsNullOrWhiteSpace(referrerUrl))
        {
            return null;
        }

        if (!Uri.TryCreate(referrerUrl, UriKind.Absolute, out var uri))
        {
            return referrerUrl.Length <= 2048 ? referrerUrl : referrerUrl[..2048];
        }

        var host = uri.Host.ToLowerInvariant();

        if (host.Contains("instagram", StringComparison.Ordinal))
        {
            return "Instagram";
        }

        if (host.Contains("whatsapp", StringComparison.Ordinal) || host.Contains("wa.me", StringComparison.Ordinal))
        {
            return "WhatsApp";
        }

        if (host.Contains("facebook", StringComparison.Ordinal) || host.Contains("fb.", StringComparison.Ordinal))
        {
            return "Facebook";
        }

        if (host.Contains("tiktok", StringComparison.Ordinal))
        {
            return "TikTok";
        }

        if (host.Contains("t.me", StringComparison.Ordinal) || host.Contains("telegram", StringComparison.Ordinal))
        {
            return "Telegram";
        }

        return referrerUrl.Length <= 2048 ? referrerUrl : referrerUrl[..2048];
    }
}
