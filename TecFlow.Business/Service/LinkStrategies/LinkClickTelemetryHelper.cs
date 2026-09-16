using System.Net;

namespace TecFlow.Business.Service.LinkStrategies;

/// <summary>Utilitários de telemetria para geração e cliques de links de afiliado.</summary>
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
            return Truncate(referrerUrl, 2048);
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

        return Truncate(referrerUrl, 2048);
    }

    public static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
