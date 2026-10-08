namespace TecFlow.Business.Service.Groups;

public static class StrictImageIngestionPolicy
{
    public const string ProductsRoot = "/uploads/products";
    public const string FramesRoot = "/uploads/frames";

    public static bool AllowsWrite(string? webRelativePath)
    {
        if (string.IsNullOrWhiteSpace(webRelativePath) || webRelativePath.Contains("..", StringComparison.Ordinal))
        {
            return false;
        }

        var value = webRelativePath.Replace('\\', '/').Trim();
        if (!value.StartsWith('/'))
        {
            value = "/" + value;
        }

        return value.StartsWith(ProductsRoot + "/", StringComparison.OrdinalIgnoreCase)
            || value.Equals(ProductsRoot, StringComparison.OrdinalIgnoreCase)
            || value.StartsWith(FramesRoot + "/", StringComparison.OrdinalIgnoreCase)
            || value.Equals(FramesRoot, StringComparison.OrdinalIgnoreCase);
    }
}

public static class ImageOptimizationRules
{
    public const int MaxEdgePx = 1080;
    public const int MinQuality = 75;
    public const int MaxQuality = 80;
    public const int MinTargetBytes = 80 * 1024;
    public const int MaxTargetBytes = 250 * 1024;
    public const double MinSafeAspect = 0.45;
    public const double MaxSafeAspect = 2.2;
    public const double WhatsAppPortraitAspect = 0.8;
    public const double WhatsAppLandscapeAspect = 1.91;
    public const string NeutralCanvasHex = "F4F4F4";
    public const string OutputExtension = ".webp";

    public static bool NeedsNeutralCanvas(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            return false;
        }

        var ratio = width / (double)height;
        return ratio > MaxSafeAspect || ratio < MinSafeAspect;
    }

    public static int CanvasSide(int width, int height)
    {
        var side = Math.Max(width, height);
        if (side <= 0)
        {
            return MaxEdgePx;
        }

        return Math.Min(MaxEdgePx, side);
    }

    public static int ChooseQuality(long encodedBytes, int currentQuality)
    {
        if (encodedBytes > MaxTargetBytes && currentQuality > MinQuality)
        {
            return MinQuality;
        }

        return currentQuality;
    }

    public static string BuildOutputFileName(long messageId) =>
        $"{(messageId > 0 ? messageId : 0)}{OutputExtension}";
}
