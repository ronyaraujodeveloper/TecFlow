using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using TecFlow.Business.Security;

namespace TecFlow.API.Security;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class WebhookSecurityAttribute : TypeFilterAttribute
{
    public WebhookSecurityAttribute()
        : base(typeof(WebhookSecurityFilter))
    {
    }
}

public sealed class WebhookSecurityFilter : IAsyncActionFilter
{
    private readonly WebhookSecurityOptions _options;

    public WebhookSecurityFilter(IOptions<WebhookSecurityOptions> options)
    {
        _options = options.Value;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var provided = context.HttpContext.Request.Headers[WebhookSecurityOptions.HeaderName].ToString();
        if (!SecretsEqual(_options.Secret, provided))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        await next();
    }

    public static bool SecretsEqual(string? expected, string? provided)
    {
        if (string.IsNullOrWhiteSpace(expected) || string.IsNullOrWhiteSpace(provided))
        {
            return false;
        }

        var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(expected));
        var providedHash = SHA256.HashData(Encoding.UTF8.GetBytes(provided));
        return CryptographicOperations.FixedTimeEquals(expectedHash, providedHash);
    }
}
